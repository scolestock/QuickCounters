// Use subject to: http://www.codeplex.com/Project/License.aspx?ProjectName=quickcounters
//
// WsdlVisitors.cs
//
// Author:
//    Tomas Restrepo (tomas@winterdom.com)
//

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Web.Services.Description;

namespace QuickCounters.Wcf
{
   /// <summary>
   /// Visits a WSDL file to generate the QuickCounters.Net
   /// instrumentation XML
   /// </summary>
   public class WsdlVisitor
   {
      private string _applicationName;
      private string _serviceName;
      private string _unmatchedHandler;

      /// <summary>
      /// Name of the application to write in the XML
      /// </summary>
      public string ApplicationName
      {
         get { return _applicationName; }
         set { _applicationName = value; }
      }
      /// <summary>
      /// Service name to overwrite the original one
      /// </summary>
      public string ServiceName
      {
         get { return _serviceName; }
         set { _serviceName = value; }
      }
      /// <summary>
      /// Name of the unmatched message handler operation (if any)
      /// </summary>
      public string UnmatchedMessageHandler
      {
         get { return _unmatchedHandler; }
         set { _unmatchedHandler = value; }
      }


      public void Visit(ServiceDescription wsdl, XmlWriter writer)
      {
         writer.WriteStartElement("InstrumentedApplication");
         writer.WriteElementString("Name", ApplicationName);
         writer.WriteElementString("Description", "");

         foreach ( Service svc in wsdl.Services )
         {
            (new ServiceVisitor()).Visit(svc, writer, 
               ServiceName, UnmatchedMessageHandler);
         }
         writer.WriteEndElement();
      }

   } // class WsdlVisitor

   /// <summary>
   /// Visits a Service element in the WSDL
   /// </summary>
   public class ServiceVisitor
   {
      public void Visit(Service obj, XmlWriter writer, string serviceName, string umh)
      {
         string svcname = String.IsNullOrEmpty(serviceName) 
            ? obj.Name : serviceName;
         writer.WriteStartElement("Component");
         writer.WriteElementString("Name", svcname);
         writer.WriteElementString("Description", obj.Documentation);

         // only consider the first port
         XmlQualifiedName name = obj.Ports[0].Binding;
         Binding binding = obj.ServiceDescription.Bindings[name.Name];

         writer.WriteStartElement("RequestTypes");
         foreach ( OperationBinding operation in binding.Operations )
         {
            (new OperationVisitor()).Visit(operation, writer);
         }
         if ( !String.IsNullOrEmpty(umh) )
         {
            OperationVisitor.Write(umh, writer);
         }
         writer.WriteEndElement();
         writer.WriteEndElement();
      }

   } // class ServiceVisitor

   /// <summary>
   /// Visits an OperationBinding in the WSDL
   /// </summary>
   public class OperationVisitor
   {
      public void Visit(OperationBinding obj, XmlWriter writer)
      {
         Write(obj.Name, writer);
      }

      public static void Write(string opName, XmlWriter writer)
      {
         writer.WriteStartElement("RequestType");
         writer.WriteElementString("Name", opName);
         writer.WriteElementString("Description", "");
         writer.WriteEndElement();
      }
   } // class OperationVisitor

} // namespace QuickCounters.Wcf
