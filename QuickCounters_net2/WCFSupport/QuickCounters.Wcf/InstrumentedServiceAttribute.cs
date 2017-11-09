// Use subject to: http://www.codeplex.com/Project/License.aspx?ProjectName=quickcounters
//
// InstrumentedServiceAttribute.cs
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
   /// Marks a service as being subject to 
   /// performance instrumentation on its operations.
   /// </summary>
   /// <remarks>
   /// You can use the [InstrumentedService] attribute
   /// to inject a custom IDispatchMessageInspector that will
   /// generate a set of performance counters for your
   /// operations, giving you automatic permormance
   /// traceability.
   /// 
   /// <example>
   /// <![CDATA[
   ///   [InstrumentedService("MyService", 
   ///      ApplicationName="MyApplication", Address="qc",
   ///      EnableHttpGet="true")]
   ///   public class ServiceClass()
   ///   {
   ///   }
   /// ]]>
   /// </example>
   /// 
   /// <para>
   /// You can also enable the behavior through configuration, 
   /// like this:
   /// </para>
   /// <example>
   /// <![CDATA[
   ///  <extensions>
   ///     <behaviorExtensions>
   ///         <add name="instrumentedService" 
   ///              type="QuickCounters.Wcf.Configuration.InstrumentedServiceElement, 
   ///               QuickCounters.Wcf, Version=1.0.0.0, Culture=neutral, PublicKeyToken=401c7ea1618cbd56"
   ///              />
   ///      </behaviorExtensions>
   ///   </extensions>
   /// ...
   ///   <serviceBehaviors>
   ///      <behavior name="ServiceBehaviors" >
   ///         <instrumentedService 
   ///            service="MyService"
   ///            enableHttpGet="true"
   ///            address="qc"
   ///            applicationName="MyApplication"
   ///         />
   ///      </behavior>
   ///   </serviceBehaviors>
   /// ]]>
   /// </example>
   /// <para>
   /// If the enableHttpGet property is enabled, then the behavior will also
   /// automatically add a new endpoint to the instrumented WCF service. This
   /// endpoint will expose an auto-generated QuickCounters.net XML 
   /// configuration file for the service which you can use directly from
   /// the QuickCounteView tool. The endpoint will be published at the same
   /// address as the original server with the /&lt;address&gt; suffix. In the 
   /// example above, it might be published at 
   /// http://localhost/VoucherService/Service.svc/qc
   /// </para>
   /// </remarks>
   public class InstrumentedServiceAttribute 
      : Attribute, IServiceBehavior
   {
      private string _service;
      private bool _enableHttpGet;
      private string _address;
      private string _applicationName;

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

      /// <summary>
      /// Enable HTTP Get of the instrumentation
      /// metadata
      /// </summary>
      public bool EnableHttpGet
      {
         get { return _enableHttpGet; }
         set { _enableHttpGet = value; }
      }

      /// <summary>
      /// Address, as a relative URI, where 
      /// to publish the instrumentation metadata
      /// </summary>
      public string Address
      {
         get { return _address; }
         set { _address = value; }
      }

      
      /// <summary>
      /// Name of the application to publish
      /// </summary>
      public string ApplicationName
      {
         get { return _applicationName; }
         set { _applicationName = value; }
      }

      #endregion // Public Properties


      /// <summary>
      /// Creates a new instance of the attribute.
      /// </summary>
      /// <remarks>
      /// Will use the service implementation type name
      /// as the prefix and support no HTTP Get metadata access
      /// </remarks>
      public InstrumentedServiceAttribute() : this(null)
      {
      }

      /// <summary>
      /// Creates a new instance of the attribute
      /// </summary>
      /// <param name="service">Name of the service to instrument</param>
      public InstrumentedServiceAttribute(string service)
      {
         _service = service;
      }


      #region IServiceBehavior Members
      //
      // IServiceBehavior Members
      //

      public void AddBindingParameters(
         ServiceDescription serviceDescription, 
         ServiceHostBase serviceHostBase, 
         Collection<ServiceEndpoint> endpoints, 
         BindingParameterCollection bindingParameters
         )
      {
      }

      public void ApplyDispatchBehavior(
         ServiceDescription serviceDescription, 
         ServiceHostBase serviceHostBase
         )
      {
         string service = Service;
         if ( String.IsNullOrEmpty(service) )
         {
            service = serviceDescription.Name;
         }

         // look for candidate endpoints and add a message
         // inspector to them
         foreach ( ChannelDispatcher dispatcher in serviceHostBase.ChannelDispatchers )
         {
            foreach ( EndpointDispatcher endpoint in dispatcher.Endpoints )
            {
               if ( IsInstrumentableEndpoint(endpoint) )
               {
                  endpoint.DispatchRuntime.MessageInspectors.Add(
                     new InstrumentedMessageInspector(service, serviceDescription)
                  );
               }
            }
         }
         if ( EnableHttpGet )
         {
            AddHttpGetDispatcher(serviceDescription, serviceHostBase);
         }
      }

      public void Validate(
         ServiceDescription serviceDescription, 
         ServiceHostBase serviceHostBase
         )
      {
         if ( EnableHttpGet && String.IsNullOrEmpty(Address) )
         {
            throw new InvalidOperationException(Resources.NoAddressSpecified);
         }
      }

      #endregion // IServiceBehavior Members


      #region Private Methods
      //
      // Private Methods
      //

      /// <summary>
      /// Filter out contracts implemented
      /// by the service so that we don't instrument
      /// built-in metadata endpoints
      /// </summary>
      /// <param name="endpoint">Service Endpoint</param>
      /// <returns>True if the endpoint can be instrumented</returns>
      private bool IsInstrumentableEndpoint(EndpointDispatcher endpoint)
      {
         string ns = endpoint.ContractNamespace;
         return ns != MetadataUris.MEX
            && ns != MetadataUris.WSDL
            && ns != MetadataUris.QC;
      }

      /// <summary>
      /// Checks if the given endpoint is an 
      /// IInstrumentationMetadata endpoint
      /// </summary>
      /// <param name="endpoint">Endpoint to check</param>
      /// <returns>True if it implements IInstrumentationMetadata</returns>
      private bool IsInstrumentationMetadata(EndpointDispatcher endpoint)
      {
         string ns = endpoint.ContractNamespace;
         return ns == MetadataUris.QC;
      }

      /// <summary>
      /// Adds a new HTTP Get dispatcher to the
      /// service host to service metadata requests
      /// </summary>
      /// <param name="serviceDescription">Service Description</param>
      /// <param name="serviceHostBase">The service host</param>
      private void AddHttpGetDispatcher(
         ServiceDescription serviceDescription, 
         ServiceHostBase serviceHostBase
         )
      {
         Uri listeningUri = GetMetadataAddress(serviceDescription);
         EndpointAddress address = new EndpointAddress(listeningUri);
         Binding binding = CreateHttpGetBinding();

         ContractDescription contract = 
            ContractDescription.GetContract(typeof(IInstrumentationMetadata));
         
         // build a dispatcher to service the requests 
         // and add it to the service host
         InstrumentationMetadata metadata = new InstrumentationMetadata();
         metadata.ServiceName = Service;
         metadata.ApplicationName = ApplicationName;
         // todo: can we check any other contract? which one should we use?
         metadata.ServiceContract = serviceDescription.Endpoints[0].Contract;
         DispatchBuilder builder = new DispatchBuilder(serviceHostBase);
         ChannelDispatcher dispatcher = builder.Build(
            address, contract, binding, 
            metadata
         );

         serviceHostBase.ChannelDispatchers.Add(dispatcher);
      }

      /// <summary>
      /// Create a custom HTTP binding for POX
      /// (MessageVersion.None)
      /// </summary>
      /// <returns>A custom binding</returns>
      private Binding CreateHttpGetBinding()
      {
         HttpTransportBindingElement httpTransport = 
            new HttpTransportBindingElement();

         TextMessageEncodingBindingElement element = 
            new TextMessageEncodingBindingElement();
         element.MessageVersion = MessageVersion.None;
         return new CustomBinding(
            new BindingElement[] { element, httpTransport }
            );
      }

      /// <summary>
      /// Returns the Uri on which to host the metadata endpoint
      /// </summary>
      /// <param name="serviceDescription">Description of the Service</param>
      /// <returns>The Uri to host the metadata</returns>
      private Uri GetMetadataAddress(ServiceDescription serviceDescription)
      {
         Uri baseUri = serviceDescription.Endpoints[0].Address.Uri;
         if ( baseUri.Scheme != Uri.UriSchemeHttp && 
              baseUri.Scheme != Uri.UriSchemeHttps )
            throw new InvalidOperationException(Resources.InvalidUri);

         return new Uri(baseUri.ToString() + "/" + Address);
         //return baseUri;
      }

      #endregion // Private Methods

   } // class InstrumentedServiceAttribute


} // namespace QuickCounters.Wcf

