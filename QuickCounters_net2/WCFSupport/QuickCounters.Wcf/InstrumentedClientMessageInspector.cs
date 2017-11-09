// Use subject to: http://www.codeplex.com/Project/License.aspx?ProjectName=quickcounters
//
// InstrumentedClientMessageInspector.cs
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
   /// IClientMessageInspector implementation that 
   /// instruments client-side proxy calls
   /// </summary>
   internal class InstrumentedClientMessageInspector 
      : IClientMessageInspector
   {
      private string _service;
      private Dictionary<string, OP> _operationsMap;
      private ContractDescription _contract;

      public InstrumentedClientMessageInspector(
         string service, ContractDescription contract
         )
      {
         _service = service;
         _contract = contract;

         //
         // Build a map of SOAP Action -> Operation Name
         // we only consider operations with an Input message
         // in order to avoid trouble with callback contracts
         //
         _operationsMap = new Dictionary<string, OP>();
         foreach ( OperationDescription operation in contract.Operations )
         {
            MessageDescription message = operation.Messages[0];
            if ( message.Direction == MessageDirection.Input )
            {
               _operationsMap[message.Action] = 
                  new OP(operation.Name, operation.IsOneWay);
            }
         }
      }

      #region IClientMessageInspector Implementation
      //
      // IClientMessageInspector Implementation
      //

      public object BeforeSendRequest(ref Message requestMsg, IClientChannel channel)
      {
         OP operation;
         if ( _operationsMap.TryGetValue(requestMsg.Headers.Action, out operation) )
         {
            RequestType request =
               RequestType.Attach(_service, operation.Name, true);
            request.BeginRequest();
            //
            // for OneWay operations we won't get AfterReceiveReply called
            // so close it right away. We don't get full counters (and time
            // will almost always be Zero, but at least it gives as
            // some basic support
            //
            if ( operation.IsOneWay )
            {
               request.SetComplete();
            } else
            {
               return request;
            }
         }
         return null;
      }

      public void AfterReceiveReply(ref Message reply, object correlationState)
      {

         RequestType request = (RequestType)correlationState;
         if ( request == null )
            return;

         if ( reply != null && reply.IsFault )
         {
            request.SetAbort();
         } else
         {
            request.SetComplete();
         }
      }

      #endregion // IClientMessageInspector Implementation


      #region OP Struct
      //
      // OP Struct
      //

      internal struct OP
      {
         private string _name;
         private bool _isOneWay;

         public string Name
         {
            get { return _name; }
         }

         public bool IsOneWay
         {
            get { return _isOneWay; }
         }

         public OP(string name, bool isOneWay)
         {
            _name = name;
            _isOneWay = isOneWay;
         }

      } // struct OP

      #endregion // OP Struct

   } // class InstrumentedClientMessageInspector

} // namespace QuickCounters.Wcf

