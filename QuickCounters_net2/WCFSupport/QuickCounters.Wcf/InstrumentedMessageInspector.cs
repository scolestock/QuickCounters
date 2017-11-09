// Use subject to: http://www.codeplex.com/Project/License.aspx?ProjectName=quickcounters
//
// InstrumentedMessageInspector.cs
//
// Author:
//    Tomas Restrepo (tomas@winterdom.com)
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Text;

using QuickCounters;


namespace QuickCounters.Wcf
{
   /// <summary>
   /// Dispatch Message Inspector that instruments operation
   /// calls using the QuickCounters.net library.
   /// </summary>
   internal class InstrumentedMessageInspector 
      : IDispatchMessageInspector
   {
      private string _service;
      private Dictionary<string, string> _operationsMap;
      private string _unmatchedMessageHandler;

      /// <summary>
      /// Create a new instance of the class
      /// </summary>
      /// <param name="service">The name of the Service/Component</param>
      /// <param name="description">ServiceDescription object</param>
      public InstrumentedMessageInspector(string service, ServiceDescription description)
      {
         if ( String.IsNullOrEmpty(service) )
            throw new ArgumentNullException("service");
         if ( description == null )
            throw new ArgumentNullException("description");

         //
         // Build a map of SOAP Action -> Operation Name
         // we only consider operations with an Input message
         // in order to avoid trouble with callback contracts
         //
         _service = service;
         _operationsMap = new Dictionary<string, string>();
         foreach ( ServiceEndpoint endpoint in description.Endpoints )
         {
            ContractDescription contract = endpoint.Contract;
            foreach ( OperationDescription operation in contract.Operations )
            {
               MessageDescription message = operation.Messages[0];
               if ( message.Direction == MessageDirection.Input )
               {
                  if ( message.Action != MetadataUris.UMH )
                     _operationsMap[message.Action] = operation.Name;
                  else
                     _unmatchedMessageHandler = operation.Name;
               }
            }
         }
      }


      #region IDispatchMessageInspector Members
      //
      // IDispatchMessageInspector Members
      //

      public object AfterReceiveRequest(
         ref Message requestMsg, IClientChannel channel, 
         InstanceContext instanceContext
         )
      {
         string operation = GetOperation(requestMsg.Headers.Action);
         RequestType request =
            RequestType.Attach(_service, operation, true);
         request.BeginRequest();
         return request;
      }

      public void BeforeSendReply(ref Message reply, object correlationState)
      {
         RequestType request = (RequestType)correlationState;
         // 
         // reply will be null if this is a one-way operation.
         // Fortunately, if it throws a fault we'll still see it
         // here!
         //
         if ( reply != null && reply.IsFault )
         {
            request.SetAbort();
         } else
         {
            request.SetComplete();
         }
      }

      #endregion // IDispatchMessageInspector Members

      #region Private Methods
      //
      // Private Methods
      //

      private string GetOperation(string action)
      {
         // if action is null, or we cannot find it in the map,
         // we're dealing with an operation with Action="*"
         if ( String.IsNullOrEmpty(action) )
            return _unmatchedMessageHandler;

         string operation;
         if ( _operationsMap.TryGetValue(action, out operation) )
            return operation;
         else
            return _unmatchedMessageHandler;
      }

      #endregion // Private Methods

   } // class InstrumentedMessageInspector

} // namespace QuickCounters.Wcf
