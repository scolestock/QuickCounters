// Scott Colestock / www.traceofthought.net
// Use subject to: http://www.codeplex.com/Project/License.aspx?ProjectName=quickcounters

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using QuickCounters;

namespace QuickCounters.BizTalk
{
   public class BizTalkSupport
   {
      // Initialization lock
      static object _initializationLock = new object();

      // Whether we have registered for orchestration events, which we'll only do when
      // someone calls TrackOrchestrationId at some point.
      private static bool _initializedOrchEvents = false;

      // Track which orchestration identifiers we are currently looking for
      // (for suspend/dehydrate events).  We will have multiple readers and writers
      // for this hashtable.
      private static Hashtable _trackedOrchestrationIds = Hashtable.Synchronized(new Hashtable());

      public static void TrackOrchestrationId(
         string orchestrationId,
         RequestType requestType)
      {
         if (!_initializedOrchEvents)
         {
            lock (_initializationLock)
            {
               if (!_initializedOrchEvents)
               {
                  // Dehydration and suspension events
                  Microsoft.XLANGs.Core.Service.Subscribe(10, new Microsoft.XLANGs.Core.ServiceEvent(OnOrchSuspendOrDehydrate));
                  Microsoft.XLANGs.Core.Service.Subscribe(6, new Microsoft.XLANGs.Core.ServiceEvent(OnOrchSuspendOrDehydrate));
                  Microsoft.XLANGs.Core.Service.Subscribe(9, new Microsoft.XLANGs.Core.ServiceEvent(OnOrchSuspendOrDehydrate));

                  Microsoft.XLANGs.Core.Service.Subscribe(8, new Microsoft.XLANGs.Core.ServiceEvent(OnOrchComplete));

                  _initializedOrchEvents = true;
               }
            }
         }

         // For a given orchestration ID, keep track of the instances of this class.
         // (We are not currently attempting to guard from simultaneous calls into this method for
         // a given orchestration ID.)
         ArrayList requestTypeInstances = (ArrayList)_trackedOrchestrationIds[orchestrationId];
         if (requestTypeInstances == null)
         {
            requestTypeInstances = new ArrayList();
            _trackedOrchestrationIds[orchestrationId] = requestTypeInstances;
         }
         requestTypeInstances.Add(requestType);

      }

      static void OnOrchSuspendOrDehydrate(Microsoft.XLANGs.Core.Service service)
      {
         Trace("Enter OnOrchSuspendOrDehydrate method: {0} {1}",
            service.Name,
            service.InstanceId.ToString());

         ArrayList requestTypeInstances = (ArrayList)_trackedOrchestrationIds[service.InstanceId.ToString()];
         if (requestTypeInstances != null)
         {
            for (int i = 0; i < requestTypeInstances.Count; i++)
            {
               RequestType requestType = (RequestType)requestTypeInstances[i];

               Trace("Found a requestType instance for this instanceId");

               requestType.PreSerializeHelper();
            }

            // OK, now stop tracking these RequestType instances.
            _trackedOrchestrationIds.Remove(service.InstanceId.ToString());
         }
         else
         {
            Trace("InstanceId of orch not found in internal hashtable.");
         }

      }

      static void OnOrchComplete(Microsoft.XLANGs.Core.Service service)
      {
         Trace("Enter OnOrchComplete method: {0} {1}",
            service.Name,
            service.InstanceId.ToString());

         // Stop tracking request type instances associated with this instanceId
         ArrayList requestTypeInstances = (ArrayList)_trackedOrchestrationIds[service.InstanceId.ToString()];
         if (requestTypeInstances != null)
         {
            Trace("Found a requestType instance for this instanceId - removing from our watch list");

            // OK, now stop tracking these RequestType instances.
            _trackedOrchestrationIds.Remove(service.InstanceId.ToString());
         }
      }

      static void Trace(string format, params object[] param)
      {
         //System.Diagnostics.Trace.WriteLine(string.Format(format,param));
      }

   }
}
