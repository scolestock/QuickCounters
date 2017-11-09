// Scott Colestock / www.traceofthought.net
// Use subject to: http://www.codeplex.com/Project/License.aspx?ProjectName=quickcounters

using System;
using System.Collections;
using System.Data;
using System.Diagnostics;
using System.Reflection;
using System.IO;
using System.Xml;

namespace QuickCounters
{
    // This was originally all using System.Xml serialization, but that conflicted with our 
    // ability to use the class in conjunction with installutil.exe (and MSIs) when the class
    // was already in the GAC, given conflicting type identity issues.

   /// <summary>
   /// Used to deserialize the QuickCounters configuration file which describes all of the request types that
   /// we will be providing performance counters for.  Current users of this class include the installer
   /// infrastructure and the viewer.
   /// </summary>
   public class InstrumentedApplication
   {
      /// <summary>
      /// List of components within the instrumented application.
      /// </summary>
      public Component[] Components;

      /// <summary>
      /// Load a configuration file, and return an instance of this class.
      /// </summary>
      /// <param name="fileName">File containing InstrumentedApplication xml definition.</param>
      /// <returns>Populated InstrumentedApplication class.</returns>
      public static InstrumentedApplication LoadFromFile(string fileName)
      {
         XmlDocument doc = new XmlDocument();
         doc.Load(fileName);
         return LoadFromXml(doc);
      }

      /// <summary>
      /// Load configuration from a TextReader, and return an instance of this class.
      /// </summary>
      /// <param name="reader">TextReader containing the InstrumentedApplication xml definition.</param>
      /// <returns>Populated InstrumentedApplication class.</returns>
      public static InstrumentedApplication LoadFromReader(TextReader reader)
      {
         XmlDocument doc = new XmlDocument();
         doc.Load(reader);
         return LoadFromXml(doc);
      }

      private static InstrumentedApplication LoadFromXml(XmlDocument doc)
      {
         InstrumentedApplication application = new InstrumentedApplication();

         XmlNodeList list = doc.SelectNodes("/InstrumentedApplication/Component");
         application.Components = new Component[list.Count];

         int i = 0;
         foreach ( XmlNode node in list )
         {
            application.Components[i] = new Component();

            application.Components[i].Name =
               node.SelectSingleNode("Name").InnerText;

            application.Components[i].Description =
               node.SelectSingleNode("Description").InnerText;

            XmlNodeList requests = node.SelectNodes("RequestTypes/RequestType");
            application.Components[i].RequestTypes = new
               RequestTypeDefinition[requests.Count];
            int j = 0;
            foreach ( XmlNode request in requests )
            {
               application.Components[i].RequestTypes[j] = new RequestTypeDefinition();

               application.Components[i].RequestTypes[j].Name =
                  request.SelectSingleNode("Name").InnerText;

               application.Components[i].RequestTypes[j].Description =
                  request.SelectSingleNode("Description").InnerText;

               j++;
            }

            i++;
         }

         return application;
      }


      /// <summary>
      /// Returns a DataSet representing the current performance counter values for the
      /// instrumented application.  Counters must have been installed on the target
      /// machine using the QuickCounters.net installer class.
      /// </summary>
      /// <param name="machineName">Machine to retrieve a DataSet of performance counter values from.</param>
      /// <returns>Populated DataSet.</returns>
      public DataSet GetCounterDataSet(
         string machineName)
      {
         DataSet dataSet = new DataSet("InstrumentedApplication");
         dataSet.Tables.Add("RequestMetrics");
         Hashtable updateState = new Hashtable();

         DataTable dataTable = dataSet.Tables[0];

         dataTable.Columns.Add("Component", typeof(string)).ExtendedProperties.Add("SummaryActionIgnore", null);
         dataTable.Columns.Add("Request", typeof(string)).ExtendedProperties.Add("SummaryActionIgnore", null);
         dataTable.Columns.Add("Started", typeof(long));
         dataTable.Columns.Add("Executing", typeof(long));
         dataTable.Columns.Add("Completed", typeof(long));
         dataTable.Columns.Add("Failed", typeof(long));
         dataTable.Columns.Add("MSec", typeof(long)).ExtendedProperties.Add("SummaryActionAverage", null);
         dataTable.Columns.Add("PerSec", typeof(float));
         dataTable.Columns.Add("PerMin", typeof(long));
         dataTable.Columns.Add("PerHour", typeof(long));

         dataTable.ExtendedProperties["updateState"] = updateState;

         foreach (Component component in this.Components)
         {
            foreach (RequestTypeDefinition requestDefinition in component.RequestTypes)
            {
               string categoryName = string.Format("{0}:{1}", component.Name, requestDefinition.Name);
               PerformanceCounterCategory category = new PerformanceCounterCategory(
                  categoryName, machineName);

               object[] values = CreatePerfValueArray(
                  component.Name,
                  requestDefinition.Name,
                  category);

               DataRow dataRow = dataTable.Rows.Add(values);

               // Preserve this association
               updateState[dataRow] = category;

            }
         }

         return dataSet;
      }

      private static Hashtable _lastPerSecSamples = new Hashtable();
      private static object[] CreatePerfValueArray(
         string componentName,
         string requestName,
         PerformanceCounterCategory category)
      {

         string single = "systemdiagnosticsperfcounterlibsingleinstance";

         InstanceDataCollectionCollection collection = category.ReadCategory();

         long requestsStarted = collection[RequestTypeDefinition.RequestsStarted][single].RawValue;
         long requestsExecuting = collection[RequestTypeDefinition.RequestsExecuting][single].RawValue;
         long requestsCompleted = collection[RequestTypeDefinition.RequestsCompleted][single].RawValue;
         long requestsFailed = collection[RequestTypeDefinition.RequestsFailed][single].RawValue;
         long requestExecTime = collection[RequestTypeDefinition.RequestExecutionTime][single].RawValue;

         // Track last sample with the _lastPerSecSamples hashtable...
         CounterSample oldSample = new CounterSample();
         object oOldSample = _lastPerSecSamples[category];
         if (oOldSample != null)
            oldSample = (CounterSample)oOldSample;

         CounterSample newSample = collection[RequestTypeDefinition.RequestsPerSec][single].Sample;
         float requestsPerSec = CounterSampleCalculator.ComputeCounterValue(
             oldSample, newSample);
         _lastPerSecSamples[category] = newSample;

         long requestsPerMin = collection[RequestTypeDefinition.RequestsPerMin][single].RawValue;
         long requestsPerHour = collection[RequestTypeDefinition.RequestsPerHour][single].RawValue;


         object[] values = new object[] {  
															componentName,
															requestName,
                                                            requestsStarted,
															requestsExecuting,
															requestsCompleted,
															requestsFailed,
                                                            requestExecTime,
															requestsPerSec,
															requestsPerMin,
															requestsPerHour};


         return values;
      }

      /// <summary>
      /// Updates the supplied DataSet with fresh performance counter data for the instrumented
      /// application.
      /// </summary>
      /// <param name="dataSet">DataSet retrieved previously from GetCounterDataSet</param>
      public static void UpdateCounterDataSet(DataSet dataSet)
      {
         Hashtable updateState = (Hashtable)dataSet.Tables[0].ExtendedProperties["updateState"];

         foreach (object key in updateState.Keys)
         {
            DataRow dataRow = (DataRow)key;
            PerformanceCounterCategory category = (PerformanceCounterCategory)updateState[key];

            object[] values = CreatePerfValueArray(
               (string)dataRow[0],
               (string)dataRow[1],
                    category); ;

            dataRow.ItemArray = values;

         }

      }


   }

   /// <summary>
   /// Component within an InstrumentedApplication.  Typically, a component should
   /// NOT span multiple appdomains.
   /// </summary>
   public class Component
   {
      /// <summary>
      /// Counter display text for ProcessUptime
      /// </summary>
      public const string ProcessUptime = "Process uptime (sec)";

      /// <summary>
      /// Name of component
      /// </summary>
      public string Name;

      /// <summary>
      /// Description of component
      /// </summary>
      public string Description;

      /// <summary>
      /// List of RequestTypeDefinitions within this component.
      /// </summary>
      public RequestTypeDefinition[] RequestTypes;
   }

   /// <summary>
   /// RequestTypeDefinition within a Component.  There will be an instance
   /// for each request we want to instrument.
   /// </summary>
   public class RequestTypeDefinition
   {
      /// <summary> Name of Requests Started counter associated with RequestType </summary>
      public const string RequestsStarted = "Requests Started";
      /// <summary> Name of Requests Executing counter associated with RequestType </summary>
      public const string RequestsExecuting = "Requests Executing";
      /// <summary> Name of Requests Completed counter associated with RequestType </summary>
      public const string RequestsCompleted = "Requests Completed";
      /// <summary> Name of Requests Failed counter associated with RequestType </summary>
      public const string RequestsFailed = "Requests Failed";
      /// <summary> Name of Request Execution Time counter associated with RequestType </summary>
      public const string RequestExecutionTime = "Request Execution Time (msec)";
      /// <summary> Name of Requests per Second counter associated with RequestType </summary>
      public const string RequestsPerSec = "Requests/Sec";
      /// <summary> Name of Requests per Minute counter associated with RequestType </summary>
      public const string RequestsPerMin = "Requests/Min";
      /// <summary> Name of Requests per Hour counter associated with RequestType </summary>
      public const string RequestsPerHour = "Requests/Hour";

      /// <summary>
      /// Name of RequestType
      /// </summary>
      public string Name;

      /// <summary>
      /// Description of RequestType
      /// </summary>
      public string Description;
   }



}

