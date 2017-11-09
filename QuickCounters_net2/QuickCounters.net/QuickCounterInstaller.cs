// Scott Colestock / www.traceofthought.net
// Use subject to: http://www.codeplex.com/Project/License.aspx?ProjectName=quickcounters

using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Configuration.Install;
using System.Xml.Serialization;


namespace QuickCounters
{
   /// <summary>
   /// Used to install counters defined in a InstrumentedApplication xml file.
   /// </summary>
   [System.ComponentModel.RunInstaller(true)]
   public class QuickCountersInstaller : Installer
   {
      /// <summary>
      /// 
      /// </summary>
      /// <param name="stateSaver"></param>
      public override void Install(System.Collections.IDictionary stateSaver)
      {
         RetrieveArgsAndAddInstallers();
         base.Install(stateSaver);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="savedState"></param>
      public override void Uninstall(System.Collections.IDictionary savedState)
      {
         RetrieveArgsAndAddInstallers();
         base.Uninstall(savedState);
      }

      private void RetrieveArgsAndAddInstallers()
      {
         StringDictionary installContext = Context.Parameters;

         string perfcounterXmlFilePath = installContext["quickctrconfig"]; //FindPerfmonCounterXmlFilePathInArgs();
         Console.WriteLine(perfcounterXmlFilePath);

         if (perfcounterXmlFilePath == null || perfcounterXmlFilePath.Length == 0)
         {
            throw new Exception("Missing quickctrconfig data.");
         }
         else
         {
            InstallPerfmonCounters(perfcounterXmlFilePath);
         }
      }

      private void InstallPerfmonCounters(string filePath)
      {
         //System.Diagnostics.Debugger.Break();

         InstrumentedApplication application =
            InstrumentedApplication.LoadFromFile(filePath);

         foreach (Component component in application.Components)
         {
            PerformanceCounterInstaller installer = new PerformanceCounterInstaller();
            installer.CategoryName = component.Name;
            installer.CategoryHelp = component.Description;

            string processUpTimeHelp = "Number of seconds the associated process for " + component.Name + " has been running.";

            installer.Counters.Add(
               new CounterCreationData(Component.ProcessUptime, processUpTimeHelp,
               PerformanceCounterType.NumberOfItems32));

            Installers.Add(installer);

            foreach (RequestTypeDefinition requestType in component.RequestTypes)
            {
               PerformanceCounterInstaller requestInstaller = new PerformanceCounterInstaller();
               requestInstaller.CategoryName = string.Format("{0}:{1}", component.Name, requestType.Name);
               requestInstaller.CategoryHelp = component.Description;

               string requestsStartedHelp = string.Format("Total requests started for {0}", requestType.Name);
               string requestsExecutingHelp = string.Format("Requests currently executing (in flight) for {0}", requestType.Name);
               string requestsCompletedHelp = string.Format("Total requests completed (success only) for {0}", requestType.Name);
               string requestsFailedHelp = string.Format("Total requests with a handled failure for {0}", requestType.Name);
               string requestExecutionTimeHelp = string.Format("Last observed execution time in milliseconds for {0}", requestType.Name);
               string requestsPerSecondHelp = string.Format("Requests executed per second for {0}", requestType.Name);
               string requestsPerMinuteHelp = string.Format("Requests executed within last minute for {0}", requestType.Name);
               string requestsPerHourHelp = string.Format("Requests executed within last hour for {0}", requestType.Name);


               requestInstaller.Counters.Add(
                   new CounterCreationData(RequestTypeDefinition.RequestsStarted, requestsStartedHelp,
                       PerformanceCounterType.NumberOfItems32));

               requestInstaller.Counters.Add(
                   new CounterCreationData(RequestTypeDefinition.RequestsExecuting, requestsExecutingHelp,
                       PerformanceCounterType.NumberOfItems32));

               requestInstaller.Counters.Add(
                  new CounterCreationData(RequestTypeDefinition.RequestsCompleted, requestsCompletedHelp,
                     PerformanceCounterType.NumberOfItems32));

               requestInstaller.Counters.Add(
                  new CounterCreationData(RequestTypeDefinition.RequestsFailed, requestsFailedHelp,
                     PerformanceCounterType.NumberOfItems32));

               requestInstaller.Counters.Add(
                   new CounterCreationData(RequestTypeDefinition.RequestExecutionTime,
                       requestExecutionTimeHelp, PerformanceCounterType.NumberOfItems32));

               requestInstaller.Counters.Add(
                  new CounterCreationData(RequestTypeDefinition.RequestsPerSec, requestsPerSecondHelp,
                     PerformanceCounterType.RateOfCountsPerSecond32));

               requestInstaller.Counters.Add(
                  new CounterCreationData(RequestTypeDefinition.RequestsPerMin, requestsPerMinuteHelp,
                  PerformanceCounterType.NumberOfItems32));

               requestInstaller.Counters.Add(
                  new CounterCreationData(RequestTypeDefinition.RequestsPerHour, requestsPerHourHelp,
                  PerformanceCounterType.NumberOfItems32));

               Installers.Add(requestInstaller);
            }


         }
      }
      
   }
}
