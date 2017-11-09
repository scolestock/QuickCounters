using System;
using System.Diagnostics;

namespace QuickCounterView
{
   /// <summary>
   /// For counters that are not calculated.
   /// </summary>
   public class NonCalculatedCounter : CounterBase
   {
      private long _currentValue = 0;

      public NonCalculatedCounter(string categoryName, string counterName, string instanceName, string machineName)
         :
         base(categoryName, counterName, instanceName, machineName)
      {
      }
      /// <summary>
      /// Gets the current value of the performance counter.
      /// </summary>
      public float CurrentValue
      {
         get
         {
            return _currentValue;
         }
      }
      /// <summary>
      /// Gets the next value of the performance counter.
      /// </summary>
      public long RawValue
      {
         get
         {
            _currentValue = CounterInstance.RawValue;

            return _currentValue;
         }
      }
   }
   /// <summary>
   /// For counters that are calculated.
   /// </summary>
   public class CalculatedCounter : CounterBase
   {
      private CounterSample _lastSample;
      private float _currentValue = 0.0F;

      public CalculatedCounter(string categoryName, string counterName, string instanceName, string machineName) :
         base (categoryName, counterName, instanceName, machineName)
      {
      }
      /// <summary>
      /// Gets the current value of the performance counter.
      /// </summary>
      public float CurrentValue
      {
         get
         {
            return _currentValue;
         }
      }
      /// <summary>
      /// Gets the next value of the performance counter.
      /// </summary>
      public float NextValue
      {
         get
         {
            CounterSample currentSample = CounterInstance.Sample;

            if (_lastSample != null)
            {
               _currentValue = CounterSampleCalculator.ComputeCounterValue(_lastSample, currentSample);
            }
            _lastSample = currentSample;

            return _currentValue;
         }
      }
   }
   /// <summary>
   /// Encapsulates the instantiating and reading of a performance counter.
   /// </summary>
   public class CounterBase
   {
      private const string SingleInstanceDefault = "systemdiagnosticsperfcounterlibsingleinstance";
      protected string _categoryName;
      protected string _counterName;
      protected string _instanceName;
      protected string _machineName;
      PerformanceCounterCategory _category;

      private CounterBase() { }

      protected CounterBase(string categoryName, string counterName, string instanceName, string machineName)
      {
         if (categoryName == null || categoryName.Length == 0)
         {
            throw new ArgumentNullException("categoryName");
         }
         else if (counterName == null || counterName.Length == 0)
         {
            throw new ArgumentNullException("counterName");
         }
         else if (machineName == null || machineName.Length == 0)
         {
            throw new ArgumentNullException("machineName");
         }
         else
         {
            _categoryName = categoryName;
            _counterName = counterName;
            _instanceName = instanceName;
            if ((_instanceName == null) || (_instanceName.Length == 0))
            {
               _instanceName = SingleInstanceDefault;
            }
            _machineName = machineName;
         }
      }
      /// <summary>
      /// Gets the InstanceData object for the counter after doing a read.
      /// </summary>
      protected InstanceData CounterInstance
      {
         get
         {
            InstanceDataCollectionCollection collection = Category.ReadCategory();

            return collection[_counterName][_instanceName];
         }
      }
      /// <summary>
      /// Gets the PerformanceCounterCategory object for the given category.
      /// </summary>
      private PerformanceCounterCategory Category
      {
         get
         {
            if (_category == null)
            {
               _category = new PerformanceCounterCategory(_categoryName, _machineName);
            }
            return _category;
         }
      }
   }
}
