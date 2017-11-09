// Use subject to: http://www.codeplex.com/Project/License.aspx?ProjectName=quickcounters
//
// DispatchBuilder.cs
//
// Author:
//    Tomas Restrepo (tomas@winterdom.com)
//

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Text;

namespace QuickCounters.Wcf
{

   /// <summary>
   /// Helper class used to build a custom dispatcher
   /// for our HTTP endpoint that exposes instrumentation 
   /// metadata
   /// </summary>
   internal class DispatchBuilder
   {
      private ServiceHostBase _host;

      public DispatchBuilder(ServiceHostBase host)
      {
         _host = host;
      }

      /// <summary>
      /// Build a channel dispatcher for the metadata 
      /// endpoint
      /// </summary>
      /// <param name="address">Address to host the endpoint</param>
      /// <param name="contract">Interface contract</param>
      /// <param name="binding">Binding to use</param>
      /// <param name="implementation">Implementation object</param>
      /// <returns>A new ChannelDispatcher object</returns>
      public ChannelDispatcher Build(
         EndpointAddress address,
         ContractDescription contract,
         Binding binding,
         object implementation
         )
      {
         // create endpoint, assuming we want to process all messages
         EndpointDispatcher endpoint =
            new EndpointDispatcher(address, contract.Name, contract.Namespace);
         endpoint.ContractFilter = new MatchAllMessageFilter();
         endpoint.FilterPriority = 0;
         endpoint.AddressFilter = 
            new EndpointAddressMessageFilter(address, false);

         DispatchRuntime runtime = endpoint.DispatchRuntime;
         InstanceContext context = new InstanceContext(_host, implementation);
         runtime.SingletonInstanceContext = context;
            

         // binding collection
         BindingParameterCollection bps = GetBindingParameters();


         // create the listener and dispatcher
         IChannelListener listener =
            binding.BuildChannelListener<IReplyChannel>(address.Uri, bps);
         ChannelDispatcher dispatcher =
            new ChannelDispatcher(listener, binding.Name, binding);
         dispatcher.MessageVersion = binding.MessageVersion;
         dispatcher.Endpoints.Add(endpoint);
         dispatcher.IncludeExceptionDetailInFaults = CanIncludeExceptions();

         // create a DispatchOperation tailored to our needs
         foreach ( OperationDescription op in contract.Operations )
         {
            DispatchOperation operation =
               CreateDispatchOperation(runtime, op);
            runtime.Operations.Add(operation);
         }

         runtime.InstanceContextProvider = 
            new SingletonInstanceContextProvider(context);
         return dispatcher;
      }

      /// <summary>
      /// Get a set of parameters to tie the binding
      /// into the channel listener
      /// </summary>
      /// <returns>A collection of parameters</returns>
      private BindingParameterCollection GetBindingParameters()
      {
         BindingParameterCollection bps = new BindingParameterCollection();

         //
         // Copy the VirtualPathExtension extension
         // the collection. This is required for the new endpoint
         // to work if we're hosted on IIS.
         //
         object obj = _host.Extensions.Find<VirtualPathExtension>();
         if ( obj != null )
            bps.Add(obj);

         return bps;
      }

      /// <summary>
      /// Check to see if we should return full exceptions
      /// from this endpoint
      /// </summary>
      /// <returns>
      /// True if the ServiceDebugBehavior is found and
      /// IncludeExceptionDetailInFaults is enabled.
      /// </returns>
      private bool CanIncludeExceptions()
      {
         ServiceDebugBehavior debug = (ServiceDebugBehavior)
            _host.Description.Behaviors.Find<ServiceDebugBehavior>();
         return debug != null && debug.IncludeExceptionDetailInFaults;
      }

      /// <summary>
      /// Create the dispatch for an operation
      /// </summary>
      /// <param name="runtime">DispatchRuntime to use</param>
      /// <param name="op">Description of the operation</param>
      /// <returns>The dispatcher</returns>
      private DispatchOperation CreateDispatchOperation(
         DispatchRuntime runtime, OperationDescription op
         )
      {
         string input = op.Messages[0].Action;
         string output = op.Messages[1].Action;

         DispatchOperation operation =
            new DispatchOperation(runtime, op.Name, input, output);
         operation.Formatter = new MessageFormatter();

         operation.Invoker = new SyncMethodInvoker(op.SyncMethod);
         return operation;
      }

      internal class MessageFormatter : IDispatchMessageFormatter
      {
         #region IDispatchMessageFormatter Members

         public void DeserializeRequest(Message message, object[] parameters)
         {
            if ( parameters.Length > 0 )
               parameters[0] = message;
         }

         public Message SerializeReply
            (MessageVersion messageVersion,
            object[] parameters, object result
            )
         {
            return (Message)result;
         }

         #endregion
      } // class MessageFormatter

      internal class SyncMethodInvoker : IOperationInvoker
      {
         private MethodInfo _method;

         public SyncMethodInvoker(MethodInfo method)
         {
            _method = method;
         }

         #region IOperationInvoker Members

         public object[] AllocateInputs()
         {
            return new object[0];
         }

         public object Invoke(
            object instance, object[] inputs,
            out object[] outputs
            )
         {
            object ret = _method.Invoke(instance, inputs);
            outputs = (object[])inputs.Clone();
            return ret;
         }

         public IAsyncResult InvokeBegin(
            object instance, object[] inputs,
            AsyncCallback callback, object state
            )
         {
            throw new NotImplementedException();
         }

         public object InvokeEnd(
            object instance, out object[] outputs,
            IAsyncResult result
            )
         {
            throw new NotImplementedException();
         }

         public bool IsSynchronous
         {
            get { return true; }
         }

         #endregion
      } // class SyncMethodInvoker

      internal class SingletonInstanceContextProvider
         : IInstanceContextProvider
      {
         private InstanceContext _instance;

         public SingletonInstanceContextProvider(InstanceContext context)
         {
            _instance = context;
         }

         #region IInstanceContextProvider Members

         public InstanceContext GetExistingInstanceContext(
            Message message, IContextChannel channel
            )
         {
            return _instance;
         }

         public void InitializeInstanceContext(
            InstanceContext instanceContext,
            Message message, IContextChannel channel
            )
         {
            // do nothing
         }

         public bool IsIdle(InstanceContext instanceContext)
         {
            return false;
         }

         public void NotifyIdle
            (InstanceContextIdleCallback callback,
            InstanceContext instanceContext
            )
         {
            throw new NotSupportedException();
         }

         #endregion
      } // class SingletonInstanceContextProvider

   } // class DispatchBuilder

} // namespace QuickCounters.Wcf

