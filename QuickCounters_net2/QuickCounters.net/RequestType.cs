// Scott Colestock / www.traceofthought.net
// Use subject to: http://www.codeplex.com/Project/License.aspx?ProjectName=quickcounters

using System;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;
using System.Timers;
using System.Threading;

namespace QuickCounters
{
   /// <summary>
   /// Instances of this class are used to instrument the various requests within your application.
   /// A factory method called "Attach" is provided to obtain an instance of a RequestType.
   /// Typical usage might be:
   /// RequestType request = RequestType.Attach("QuickCountersUnitTest","Fake Work",true);
   /// request.BeginRequest();
   /// ... do some work ...
   /// request.SetComplete();  // (or request.SetAbort(), often in a finally block)
   /// </summary>
   [Serializable]
   public sealed class RequestType : ISerializable
   {
      // Used for request execution time timings
      [DllImport("kernel32.dll"),SuppressUnmanagedCodeSecurity]
      static extern bool QueryPerformanceCounter(out long performanceCountValue);

      // Used for request execution time timings
      [DllImport("kernel32.dll")]
      static extern bool QueryPerformanceFrequency(out long frequency);
      static long _counterFrequency;

      #region Static state required for performance optimizations and timer work.

      // Initialization lock
      static object _initializationLock = new object();

      // One PerformanceCounter entry in the hashtable for each "component"
      // (see InstrumentedApplication.cs) we are supporting.
      static Hashtable _processUptimeHash = new Hashtable();

      // One PerformanceCounter entry in the hashtable for each request type we are supporting.
      static Hashtable _requestsStartedHash = new Hashtable();
      static Hashtable _requestsExecutingHash = new Hashtable();
      static Hashtable _requestsCompletedHash = new Hashtable();
      static Hashtable _requestsFailedHash = new Hashtable();
      static Hashtable _requestExecutionTimeHash = new Hashtable();
      static Hashtable _requestsPerSecondHash = new Hashtable();
      static Hashtable _requestsPerMinuteHash = new Hashtable();
      static Hashtable _requestsPerHourHash = new Hashtable();

      // For handling appdomain uptime counters.
      static System.Timers.Timer _timerForCounters = new System.Timers.Timer();
      static int _timerIntervalSec = 5;

      // For help with per minute and per hour calculations.  One ArrayList in the hashtable for each
      // request type we are supporting.
      static Hashtable _requestPerMinTimestampsHash = new Hashtable();
      static Hashtable _requestPerHourTimestampsHash = new Hashtable();
      static Hashtable _requestPerMinPreviousValueHash = new Hashtable();
      static Hashtable _requestPerHourPreviousValueHash = new Hashtable();

      // This value should be tuned to reflect typical number of requests in a minute for a given
      // request type.
      static int _initialPerMinTimeStampArraySize = 50*60;
      static int _initialPerHourTimeStampArraySize = 20*3600;

      static TimeSpan _minute = new TimeSpan(0,0,1,0,0);
      static TimeSpan _hour = new TimeSpan(0,1,0,0,0);

      static DateTime _hostProcessStartTime;

      #endregion

      private static readonly string _biztalkSupport = "QuickCounters.BizTalk, Version=1.0.0.0, Culture=neutral, PublicKeyToken=401c7ea1618cbd56";
      private static bool _biztalkSupportLoaded = false;
      private static MethodInfo _trackOrchestrationIdMethod;

      // Request-level counters associated with this request type.
      PerformanceCounter _requestsStartedCounter;
      PerformanceCounter _requestsExecutingCounter;
      PerformanceCounter _requestsCompletedCounter;
      PerformanceCounter _requestsFailedCounter;
      PerformanceCounter _requestExecutionTimeCounter;
      PerformanceCounter _requestsPerSecondCounter;
      PerformanceCounter _requestsPerMinuteCounter;
      PerformanceCounter _requestsPerHourCounter;

      // Arrays of timestamps, to support per-min/per-hour counters.
      ArrayList _requestTimestampsForPerMin;
      ArrayList _requestTimestampsForPerHour;

      // These members are the state we need across serialization boundaries...
      string _componentName;
      string _requestTypeName;
      bool _resetCounterValuesOnAppDomainInit;
      long _performanceCountStartValue;
      DateTime _startTime;
      bool _useHighResolutionTime = true;
      int _openRequestCount;
      string _orchestrationId;

      static void Trace(string format, params object[] param)
      {
         //System.Diagnostics.Trace.WriteLine(string.Format(format,param));
      }

      /// <summary>
      /// Initialize necessary timers when the appdomain loads.  It is safe for the timer events
      /// to run before any instances of this type are created.
      /// </summary>
      static RequestType()
      {
         _timerForCounters.Elapsed+=new ElapsedEventHandler(OnTimedEvent);
			
         _timerForCounters.Interval = _timerIntervalSec * 1000;
         _timerForCounters.Enabled = true;

         QueryPerformanceFrequency(out _counterFrequency);

         _hostProcessStartTime = DateTime.Now;   // unfortunately this call requires elevated security: Process.GetCurrentProcess().StartTime;
      }

      /// <summary>
      /// Called when deserialization occurs.  Note that initialization is identical to the normal case -
      /// we either build up our static cache of state, or "attach" to what is already there if another instance
      /// of RequestType is pointed at the same performanc counter.
      /// </summary>
      /// <param name="info"></param>
      /// <param name="context"></param>
      private RequestType(
         SerializationInfo info, 
         StreamingContext context)
      {
         // Trace("RequestType serialization constructor called.");

         _componentName = (string)info.GetValue("_componentName",typeof(string));
         _requestTypeName = (string)info.GetValue("_requestTypeName",typeof(string));
         _resetCounterValuesOnAppDomainInit  = (bool)info.GetValue("_resetCounterValuesOnAppDomainInit",typeof(bool));
         _performanceCountStartValue = (long)info.GetValue("_performanceCountStartValue",typeof(long));
         _startTime = (DateTime)info.GetValue("_startTime", typeof(DateTime));
         _useHighResolutionTime = (bool)info.GetValue("_useHighResolutionTime", typeof(bool));
         _openRequestCount = (int)info.GetValue("_openRequestCount", typeof(int));
         _orchestrationId = (string)info.GetValue("_orchestrationId", typeof(string));

         Initialize();

         // If we were tracking this RequestType logical instance with an orchestration, then
         // we want to keep doing that.  Likewise, increment requests executing by _openRequestCount
         // since when the orchestration was dehydrated, we decremented.
         if (_orchestrationId != null)
         {
            _requestsExecutingCounter.IncrementBy(_openRequestCount);
            TrackOrchestrationId(_orchestrationId, this);
         }

         Trace("RequestType deserialization constructor called with _openRequestCount: " + _openRequestCount.ToString());
      }

      /// <summary>
      /// We force users of this class to use the Attach method to emphasize that multiple instances
      /// using the same performance object name and request type are pointed at a common performance counter.
      /// </summary>
      /// <param name="componentName">Name of component</param>
      /// <param name="requestTypeName">Name of request type</param>
      /// <param name="resetCounterValuesOnAppDomainInit">True to zero out counters on app domain initialization</param>
      private RequestType(
         string componentName,
         string requestTypeName,
         bool resetCounterValuesOnAppDomainInit)
      {
         Trace("RequestType constructor called."); 

         _componentName = componentName;
         _requestTypeName = requestTypeName;
         _resetCounterValuesOnAppDomainInit = resetCounterValuesOnAppDomainInit;

         Initialize();
      }

    
      private static void TrackOrchestrationId(
         string orchestrationId,
         RequestType requestType)
      {
         // This is all in the name of avoiding a hard reference to BizTalk-specific assemblies.
         if (!_biztalkSupportLoaded)
         {
            lock (_initializationLock)
            {
               if (!_biztalkSupportLoaded)
               {
                  // Do this reflection work just once.
                  Assembly biztalkSupportAssem = Assembly.Load(_biztalkSupport);
                  Type helperType = biztalkSupportAssem.GetType("QuickCounters.BizTalk.BizTalkSupport", true);
                  _trackOrchestrationIdMethod = helperType.GetMethod("TrackOrchestrationId");
                  _biztalkSupportLoaded = true;
               }
            }
         }

         _trackOrchestrationIdMethod.Invoke(null, new object[] { orchestrationId, requestType });
      }

      /// <summary>
      /// This method insures (via static caches) that we aren't creating multiple PerformanceCounter instances, either for 
      /// the same perf object name and request type, or across serialization bounadries.  
      /// </summary>
      private void Initialize()
      {
         // Note that this implementation implies appdomain uptime won't be available as a
         // counter to be incremented (as far as this class is concerned) until somebody 
         // attaches to a request type.
         lock (_processUptimeHash.SyncRoot)
         {
            if (!_processUptimeHash.ContainsKey(_componentName))
            {
               PerformanceCounter processUptimeCounter = new PerformanceCounter(
                  _componentName,
                  Component.ProcessUptime,
                  false);

               processUptimeCounter.RawValue = 0;

               _processUptimeHash[_componentName] = processUptimeCounter;
            }
         }

         string requestCounterKey = 
            string.Format("{0}:{1}",_componentName,_requestTypeName);
         
         // We want to ensure only one thread (across all instances of this class being
         // simultaneously initialized) is able to create the various performance counters
         // for this particular requestCounterKey.  This lock wouldn't have to be this coarse -
         // but if it wasn't, we'd have to use sync wrappers on all the hashtables.
         lock (_initializationLock)
         {
            if(!_requestsStartedHash.ContainsKey(requestCounterKey))
            {
               _requestsStartedCounter = new PerformanceCounter(
                  requestCounterKey,
                  RequestTypeDefinition.RequestsStarted,
                  false);
               _requestsStartedHash[requestCounterKey] = _requestsStartedCounter;

               _requestsExecutingCounter = new PerformanceCounter(
                  requestCounterKey,
                  RequestTypeDefinition.RequestsExecuting,
                  false);
               _requestsExecutingHash[requestCounterKey] = _requestsExecutingCounter;

               _requestsCompletedCounter = new PerformanceCounter(
                  requestCounterKey,
                  RequestTypeDefinition.RequestsCompleted,
                  false);
               _requestsCompletedHash[requestCounterKey] = _requestsCompletedCounter;

               _requestsFailedCounter = new PerformanceCounter(
                  requestCounterKey,
                  RequestTypeDefinition.RequestsFailed,
                  false);
               _requestsFailedHash[requestCounterKey] = _requestsFailedCounter;

               _requestExecutionTimeCounter = new PerformanceCounter(
                  requestCounterKey,
                  RequestTypeDefinition.RequestExecutionTime,
                  false);
               _requestExecutionTimeHash[requestCounterKey] = _requestExecutionTimeCounter;

               _requestsPerSecondCounter = new PerformanceCounter(
                  requestCounterKey,
                  RequestTypeDefinition.RequestsPerSec,
                  false);
               _requestsPerSecondHash[requestCounterKey] = _requestsPerSecondCounter;

               _requestsPerMinuteCounter = new PerformanceCounter(
                  requestCounterKey,
                  RequestTypeDefinition.RequestsPerMin,
                  false);
               _requestsPerMinuteHash[requestCounterKey] = _requestsPerMinuteCounter;

               // Track time stamps of requests to assist with per minute calculations.
               // We will be touching this arraylist from both a timer event handler and the
               // public APIs, on different threads.
               _requestTimestampsForPerMin = ArrayList.Synchronized(new ArrayList(_initialPerMinTimeStampArraySize));
               lock (_requestPerMinTimestampsHash.SyncRoot)
               {
                  _requestPerMinTimestampsHash[requestCounterKey] = _requestTimestampsForPerMin;
               }

               _requestsPerHourCounter = new PerformanceCounter(
                  requestCounterKey,
                  RequestTypeDefinition.RequestsPerHour,
                  false);
               _requestsPerHourHash[requestCounterKey] = _requestsPerHourCounter;

               // Track time stamps of requests to assist with per hour calculations.
               // We will be touching this arraylist from both a timer event handler and the
               // public APIs, on different threads.
               _requestTimestampsForPerHour = ArrayList.Synchronized(new ArrayList(_initialPerHourTimeStampArraySize));
               lock (_requestPerHourTimestampsHash.SyncRoot)
               {
                  _requestPerHourTimestampsHash[requestCounterKey] = _requestTimestampsForPerHour;
               }

               if(_resetCounterValuesOnAppDomainInit)
               {
                  // Reset values for this appdomain
                  _requestsStartedCounter.RawValue = 0;
                  _requestsExecutingCounter.RawValue = 0;
                  _requestsCompletedCounter.RawValue = 0;
                  _requestsFailedCounter.RawValue = 0;
                  _requestExecutionTimeCounter.RawValue = 0;
                  _requestsPerSecondCounter.RawValue = 0;
                  _requestsPerMinuteCounter.RawValue = 0;
                  _requestsPerHourCounter.RawValue = 0;
               }
            }
            else
            {
               _requestsStartedCounter = (PerformanceCounter)_requestsStartedHash[requestCounterKey];
               _requestsExecutingCounter = (PerformanceCounter)_requestsExecutingHash[requestCounterKey];
               _requestsCompletedCounter = (PerformanceCounter)_requestsCompletedHash[requestCounterKey];
               _requestsFailedCounter = (PerformanceCounter)_requestsFailedHash[requestCounterKey];
               _requestExecutionTimeCounter = (PerformanceCounter)_requestExecutionTimeHash[requestCounterKey];
               _requestsPerSecondCounter = (PerformanceCounter)_requestsPerSecondHash[requestCounterKey];
               _requestsPerMinuteCounter = (PerformanceCounter)_requestsPerMinuteHash[requestCounterKey];
               _requestsPerHourCounter = (PerformanceCounter)_requestsPerHourHash[requestCounterKey];
               _requestTimestampsForPerMin = (ArrayList)_requestPerMinTimestampsHash[requestCounterKey];
               _requestTimestampsForPerHour = (ArrayList)_requestPerHourTimestampsHash[requestCounterKey];
            }
         }

      }

      

      /// <summary>
      /// Called for serialization.
      /// </summary>
      /// <param name="info"></param>
      /// <param name="context"></param>
      public void GetObjectData(SerializationInfo info, StreamingContext context)
      {
         // We'll be relying on a regular datetime stamp (in _startTime) since we are being serialized.
         _useHighResolutionTime = false;

         info.AddValue("_componentName",_componentName);
         info.AddValue("_requestTypeName",_requestTypeName);
         info.AddValue("_resetCounterValuesOnAppDomainInit",_resetCounterValuesOnAppDomainInit);
         info.AddValue("_performanceCountStartValue",_performanceCountStartValue);
         info.AddValue("_startTime", _startTime);
         info.AddValue("_useHighResolutionTime", _useHighResolutionTime);
         info.AddValue("_openRequestCount", _openRequestCount);
         info.AddValue("_orchestrationId", _orchestrationId);

         Trace("in get object data with state {1} and context {0}", context.Context, context.State.ToString());

         //SerializationInfoEnumerator e = info.GetEnumerator();
         //Trace("Values in the SerializationInfo:");
         //while (e.MoveNext())
         //{
         //    Trace("Name={0}, ObjectType={1}, Value={2}", e.Name, e.ObjectType, e.Value);
         //}

      }

      /// <summary>
      /// For internal use only.
      /// </summary>
      public void PreSerializeHelper()
      {
         Trace("Decrementing requests executing by {0}",
            _openRequestCount);

         for (int j = 0; j < _openRequestCount; j++)
            _requestsExecutingCounter.Decrement();
      }

      // Used just in OnTimedEvent to make sure we get a consistently updated value for
      // PerMin and PerHour counts even if multiple processes are sharing the same counter.
      private static Mutex perMinuteUpdateMutex = new Mutex(false, @"Global\perMinuteUpdateMutexe");
      private static Mutex perHourUpdateMutex = new Mutex(false, @"Global\perMinuteUpdateMutexe");

      /// <summary>
      /// Event handler for timer used to compute per minute and per hour counters.
      /// </summary>
      /// <param name="source"></param>
      /// <param name="e"></param>
      private static void OnTimedEvent(object source, ElapsedEventArgs e)
      {
         try
         {
            _timerForCounters.Enabled=false;

            // Take this lock to avoid colliding with work in Initialize method
            // that might take place by a separate thread in a separate instance of this class.
            lock (_processUptimeHash.SyncRoot)
            {
               foreach (PerformanceCounter counter in _processUptimeHash.Values)
               {
                  TimeSpan up = DateTime.Now - _hostProcessStartTime;
                  counter.RawValue = (long)(up.TotalSeconds);
                  Trace("Uptime: {0}", up.TotalSeconds);
               }
            }

            // Compute all per minute counts by walking an array of timestamps, seeing which
            // ones occurred in the last minute.
            // This hashtable can be added to by a different thread in a different instance
            // of this class that is executing the Initialize method (hence the lock)
            lock (_requestPerMinTimestampsHash.SyncRoot)
            {
               foreach (string key in _requestPerMinTimestampsHash.Keys)
               {
                  // Get per minute counter
                  PerformanceCounter counter = (PerformanceCounter)_requestsPerMinuteHash[key];

                  // Get array of timestamps
                  ArrayList timestamps = (ArrayList)_requestPerMinTimestampsHash[key];

                  // Look for index of a time stamp that is equal to one minute ago
                  object oneMinuteAgo = DateTime.Now - _minute;
                  int oneMinuteAgoIndex =
                     timestamps.BinarySearch(oneMinuteAgo);

                  // Get next largest object if no exact match (returned as bitwise complement)
                  if (oneMinuteAgoIndex < 0)
                     oneMinuteAgoIndex = ~oneMinuteAgoIndex;

                  //Trace("{0} elements in per-min timestamp array, pruning before element {1} value {2} -- cutoff: {3}",
                  //    timestamps.Count, oneMinuteAgoIndex, timestamps[oneMinuteAgoIndex], oneMinuteAgo);

                  // Prune everything older
                  timestamps.RemoveRange(0, oneMinuteAgoIndex);

                  // Get last value for this counter from previous timer iteration, so 
                  // we can compute a delta.
                  int previousPerMinCount = 
                     _requestPerMinPreviousValueHash[key] == null ? 0:(int)_requestPerMinPreviousValueHash[key];

                  try
                  {
                     // Ensure that we are getting consistent read of this counter from
                     // RawValue, so we can safely make a delta-based change to it.
                     // Otherwise, multiple processes sharing this counter
                     // can act to corrupt the value.
                     perMinuteUpdateMutex.WaitOne();
                     counter.RawValue += timestamps.Count - previousPerMinCount;
                     _requestPerMinPreviousValueHash[key] = timestamps.Count;
                  }
                  finally
                  {
                     perMinuteUpdateMutex.ReleaseMutex();
                  }

               }
            }

            // Compute all per hour counts by walking an array of timestamps, seeing which
            // ones occurred in the last hour.
            // This hashtable can be added to by a different thread in a different instance
            // this class that is executing the Initialize method (hence the lock)
            lock (_requestPerHourTimestampsHash.SyncRoot)
            {
               foreach (string key in _requestPerHourTimestampsHash.Keys)
               {
                  // Get per minute counter
                  PerformanceCounter counter = (PerformanceCounter)_requestsPerHourHash[key];

                  // Get array of timestamps
                  ArrayList timestamps = (ArrayList)_requestPerHourTimestampsHash[key];

                  // Look for index of a time stamp that is equal to one minute ago
                  object oneHourAgo = DateTime.Now - _hour;
                  int oneHourAgoIndex =
                     timestamps.BinarySearch(oneHourAgo);

                  // Get next largest object if no exact match (returned as bitwise complement)
                  if (oneHourAgoIndex < 0)
                     oneHourAgoIndex = ~oneHourAgoIndex;

                  //Trace("{0} elements in per-hour timestamp array, pruning before element {1} value {2} -- cutoff: {3}",
                  //    timestamps.Count, oneHourAgoIndex, timestamps[oneHourAgoIndex], oneHourAgo);

                  // Prune everything older
                  timestamps.RemoveRange(0, oneHourAgoIndex);

                  // Get last value for this counter from previous timer iteration, so 
                  // we can compute a delta.
                  int previousPerHourCount = 
                     _requestPerHourPreviousValueHash[key] == null ? 0 : (int)_requestPerHourPreviousValueHash[key];

                  try
                  {
                     // Ensure that we are getting consistent read of this counter from
                     // RawValue, so we can safely make a delta-based change to it.
                     // Otherwise, multiple processes sharing this counter
                     // can act to corrupt the value.
                     perHourUpdateMutex.WaitOne();
                     counter.RawValue += timestamps.Count - previousPerHourCount;
                     _requestPerHourPreviousValueHash[key] = timestamps.Count;
                  }
                  finally
                  {
                     perHourUpdateMutex.ReleaseMutex();
                  }


               }
            }
          
         }
         finally
         {
            _timerForCounters.Enabled=true;
         }
      }

      private void SetExecutionTime()
      {
         if (_useHighResolutionTime)
         {
            Trace("Using high resolution time for execution time...");

            long startTime = _performanceCountStartValue;
            long end;

            QueryPerformanceCounter(out end);

            double elapsedSec = (((double)(end - startTime)) / ((double)_counterFrequency));

            if (elapsedSec > 0)
               _requestExecutionTimeCounter.RawValue = (long)(elapsedSec * 1000);
         }
         else
         {
            Trace("Using low resolution time for execution time...");

            TimeSpan length = DateTime.Now - _startTime;

            _requestExecutionTimeCounter.RawValue = (long)(length.TotalMilliseconds);
         }
      }

      #region Public API
		
      /// <summary>
      /// Factory method for creating a RequestType instance used for indicating when a
      /// request begins, ends successfully, or fails.  All performance counter values are
      /// set accordingly.
      /// </summary>
      /// <param name="componentName">The name of a performance object installed via QuickCounters.net</param>
      /// <param name="requestTypeName">The name of a request type within this performance object</param>
      /// <param name="resetCounterValuesOnAppDomainInit">A boolean indicating if counter values should be reset when appdomain initializes</param>
      /// <returns>RequestType class with methods for indicating Begin/SetComplete/SetAbort as well as property values for reading current state.</returns>
      public static RequestType Attach(
         string componentName,
         string requestTypeName,
         bool resetCounterValuesOnAppDomainInit)
      {
         return new RequestType(
            componentName,
            requestTypeName,
            resetCounterValuesOnAppDomainInit);
      }

      /// <summary>
      /// Factory method for creating a RequestType instance used for indicating when a
      /// request begins, ends successfully, or fails.  All performance counter values are
      /// set accordingly.
      /// Counter values will be reset when appdomain initializes.
      /// </summary>
      /// <param name="componentName">The name of a performance object installed via QuickCounters.net</param>
      /// <param name="requestTypeName">The name of a request type within this performance object</param>
      /// <returns>RequestType class with methods for indicating Begin/SetComplete/SetAbort as well as property values for reading current state.</returns>
      public static RequestType Attach(
         string componentName,
         string requestTypeName)
      {
         return new RequestType(
            componentName,
            requestTypeName,
            true);
      }

      /// <summary>
      /// Factory method for creating a RequestType instance used for indicating when a
      /// request begins, ends successfully, or fails.  All performance counter values are
      /// set accordingly.
      /// Counter values will be reset when appdomain initializes.
      /// </summary>
      /// <param name="componentName">The name of a performance object installed via QuickCounters.net</param>
      /// <param name="requestTypeName">The name of a request type within this performance object</param>
      /// <param name="orchestrationId">The Id of an orchestration whose lifecycle should be used to update requests executing value.</param>
      /// <returns>RequestType class with methods for indicating Begin/SetComplete/SetAbort as well as property values for reading current state.</returns>
      public static RequestType Attach(
         string componentName,
         string requestTypeName,
         string orchestrationId)
      {
         RequestType requestType = new RequestType(
            componentName,
            requestTypeName,
            true);

         requestType._orchestrationId = orchestrationId;

         TrackOrchestrationId(orchestrationId, requestType);

         return requestType;
      }

      /// <summary>
      /// Factory method for creating a RequestType instance used for indicating when a
      /// request begins, ends successfully, or fails.  All performance counter values are
      /// set accordingly.
      /// </summary>
      /// <param name="componentName">The name of a performance object installed via QuickCounters.net</param>
      /// <param name="requestTypeName">The name of a request type within this performance object</param>
      /// <param name="resetCounterValuesOnAppDomainInit">A boolean indicating if counter values should be reset when appdomain initializes</param>
      /// <param name="orchestrationId">The Id of an orchestration whose lifecycle should be used to update requests executing value.</param>
      /// <returns>RequestType class with methods for indicating Begin/SetComplete/SetAbort as well as property values for reading current state.</returns>
      public static RequestType Attach(
         string componentName,
         string requestTypeName,
         bool resetCounterValuesOnAppDomainInit,
         string orchestrationId)
      {
         RequestType requestType = new RequestType(
            componentName,
            requestTypeName,
            resetCounterValuesOnAppDomainInit);

         requestType._orchestrationId = orchestrationId;

         TrackOrchestrationId(orchestrationId, requestType);

         return requestType;
      }

      /// <summary>
      /// Should be called when this request begins.
      /// </summary>
      /// <returns>Returns a string that should be passed to SetComplete or SetAbort.</returns>
      public void BeginRequest()
      {
         BeginRequest(1);
      }

      /// <summary>
      /// Should be called when this request ends successfully.
      /// </summary>
      public void SetComplete()
      {
         SetComplete(1);
      }

      /// <summary>
      /// Should be called when this request ends in failure.
      /// </summary>
      public void SetAbort()
      {
         SetAbort(1);
      }

      /// <summary>
      /// Should be called when multiple logical requests begin simultaneously.
      /// </summary>
      /// <param name="numberOfIterations">Indicates how many requests are being started at once.</param>
      public void BeginRequest(int numberOfIterations)
      {
         for(int i = 0; i<numberOfIterations; i++)
         {
            _requestsExecutingCounter.Increment();
            _requestsStartedCounter.Increment();
         }

         _openRequestCount += numberOfIterations;

         long performanceCountValue;
         QueryPerformanceCounter(out performanceCountValue);
         _performanceCountStartValue = performanceCountValue;
         _startTime = DateTime.Now;
      }

      /// <summary>
      /// Should be called when multiple logical requests end successfully.
      /// </summary>
      /// <param name="numberOfIterations">Indicates how many requests are being completed successfully at once.</param>
      public void SetComplete(int numberOfIterations)
      {
         for(int i = 0; i<numberOfIterations; i++)
         {
            _requestsCompletedCounter.Increment();
            _requestsExecutingCounter.Decrement();
            _requestTimestampsForPerMin.Add(DateTime.Now);
            _requestTimestampsForPerHour.Add(DateTime.Now);
            _requestsPerSecondCounter.Increment();
         }

         DecrementAndErrorCheck(numberOfIterations);

         SetExecutionTime();
      }

      /// <summary>
      /// Should be called when multiple logical requests end in failure.
      /// </summary>
      /// <param name="numberOfIterations">Indicates how many requests have failed at once.</param>
      public void SetAbort(int numberOfIterations)
      {
         for(int i = 0; i<numberOfIterations; i++)
         {
            // For now, lets not have requests completed be inclusive of failed requests.
            //_requestsCompletedCounter.Increment();

            _requestsFailedCounter.Increment();
            _requestsExecutingCounter.Decrement();
            _requestTimestampsForPerMin.Add(DateTime.Now);
            _requestTimestampsForPerHour.Add(DateTime.Now);
            _requestsPerSecondCounter.Increment();
         }

         DecrementAndErrorCheck(numberOfIterations);

         SetExecutionTime();
      }

      private void DecrementAndErrorCheck(int numberOfIterations)
      {
         // Anything less than zero for either of these values mean that 
         // things have gone awry.  By forcing zero, the values will "get correct"
         // eventually.  
         _openRequestCount -= numberOfIterations;
         if (_openRequestCount < 0)
         {
            Trace("openRequestCount was less than zero: " + _openRequestCount.ToString());
            _openRequestCount = 0;
         }

         if (_requestsExecutingCounter.RawValue < 0)
         {
            Trace("_requestsExecutingCounter.RawValue was less than zero: " + _requestsExecutingCounter.RawValue.ToString());
            _requestsExecutingCounter.RawValue = 0;
         }
      }

      /// <summary>
      /// Returns current value of Requests Total counter.
      /// </summary>
      public long RequestsStarted
      {
         get
         {
            return _requestsStartedCounter.RawValue;
         }
      }

      /// <summary>
      /// Returns current value of Requests Executing counter
      /// </summary>
      public long RequestsExecuting
      {
         get
         {
            return _requestsExecutingCounter.RawValue;
         }
      }

      /// <summary>
      /// Returns current value of Requests Total counter.
      /// </summary>
      public long RequestsCompleted
      {
         get
         {
            return _requestsCompletedCounter.RawValue;
         }
      }

      /// <summary>
      /// Returns current value of Requests Failed counter
      /// </summary>
      public long RequestsFailed
      {
         get
         {
            return _requestsFailedCounter.RawValue;
         }
      }

      /// <summary>
      /// Returns current value of Request Execution Time counter.
      /// </summary>
      public long RequestExecutionTime
      {
         get
         {
            return _requestExecutionTimeCounter.RawValue;
         }
      }

      /// <summary>
      /// Returns next sample of Requests/Sec counter.
      /// </summary>
      public float RequestsPerSecond
      {
         get
         {
            return _requestsPerSecondCounter.NextValue();
         }
      }

      /// <summary>
      /// Returns current value of Requests/Min counter
      /// </summary>
      public long RequestsPerMinute
      {
         get
         {
            return _requestsPerMinuteCounter.RawValue;
         }
      }

      /// <summary>
      /// Returns current value of Requests/Min counter
      /// </summary>
      public long RequestsPerHour
      {
         get
         {
            return _requestsPerHourCounter.RawValue;
         }
      }

      #endregion

   }
}
