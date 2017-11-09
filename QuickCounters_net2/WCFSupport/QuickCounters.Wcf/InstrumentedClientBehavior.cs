// Use subject to: http://www.codeplex.com/Project/License.aspx?ProjectName=quickcounters
//
// InstrumentedClientBehavior.cs
//
// Author:
//    Tomas Restrepo (tomas@winterdom.com)
//

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Text;

using QuickCounters.Wcf;
using QuickCounters.Wcf.Properties;

namespace QuickCounters.Wcf
{
   /// <summary>
   /// Marks a client-side proxy as being subject to 
   /// performance instrumentation on its operations.
   /// </summary>
   /// <remarks>
   /// </remarks>
   public class InstrumentedClientBehavior : IEndpointBehavior
   {
      private string _service;


      #region Public Properties
      //
      // Public Properties
      //

      /// <summary>
      /// Name of the service to instrument
      /// </summary>
      public string Service
      {
         get { return _service; }
         set { _service = value; }
      }
      #endregion // Public Properties


      /// <summary>
      /// Creates a new instance of the attribute.
      /// </summary>
      /// <remarks>
      /// Will use the service implementation type name
      /// as the prefix
      /// </remarks>
      public InstrumentedClientBehavior() : this(null)
      {
      }

      /// <summary>
      /// Creates a new instance of the attribute
      /// </summary>
      /// <param name="service">Name of the service to instrument</param>
      public InstrumentedClientBehavior(string service)
      {
         _service = service;
      }


      #region IEndpointBehavior Implementation
      //
      // IEndpointBehavior Implementation
      //

      public void AddBindingParameters(
         ServiceEndpoint endpoint, 
         BindingParameterCollection bindingParameters
         )
      {
         // not implemented
      }

      public void ApplyClientBehavior(
         ServiceEndpoint endpoint, 
         ClientRuntime clientRuntime
         )
      {
         string service = Service;
         if ( String.IsNullOrEmpty(service) )
         {
            service = endpoint.Name;
         }

         // 
         // inject a client message inspector
         //
         clientRuntime.MessageInspectors.Add(
            new InstrumentedClientMessageInspector(service, endpoint.Contract)
            );
      }

      public void ApplyDispatchBehavior(
         ServiceEndpoint endpoint, 
         EndpointDispatcher endpointDispatcher
         )
      {
         // not implemented
      }

      public void Validate(ServiceEndpoint endpoint)
      {
         // not implemented
      }

      #endregion // IEndpointBehavior Implementation

   } // class InstrumentedClientBehavior

} // namespace QuickCounters.Wcf

