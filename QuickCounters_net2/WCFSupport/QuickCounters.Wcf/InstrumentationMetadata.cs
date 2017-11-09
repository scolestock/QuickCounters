// Use subject to: http://www.codeplex.com/Project/License.aspx?ProjectName=quickcounters
//
// InstrumentationMetada.cs
//
// Author:
//    Tomas Restrepo (tomas@winterdom.com)
//

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.Text;
using System.Xml;
using WSDL = System.Web.Services.Description;
using QuickCounters.Wcf.Properties;


namespace QuickCounters.Wcf
{

   /// <summary>
   /// Implementation of the IInstrumentationMetadata contract
   /// that generates the QuickCounters.net configuration
   /// file for this service
   /// </summary>
   internal class InstrumentationMetadata : IInstrumentationMetadata
   {
      private string _applicationName;
      private string _serviceName;
      private ContractDescription _serviceContract;

      #region Public Properties
      //
      // Public Properties
      //

      public string ApplicationName
      {
         get { return _applicationName; }
         set { _applicationName = value; }
      }

      public string ServiceName
      {
         get { return _serviceName; }
         set { _serviceName = value; }
      }

      public ContractDescription ServiceContract
      {
         get { return _serviceContract; }
         set { _serviceContract = value; }
      }

      #endregion // Public Properties

      public Message Get()
      {
         WSDL.ServiceDescription wsdl = GetServiceDescription();
         string unmatchedHandler = GetUnmatchedHandler();

         if ( wsdl == null )
            throw new InvalidOperationException(Resources.MetadataDisabled);

         BodyWriter bodyWriter = new DelegatedBodyWriter(
            delegate(XmlDictionaryWriter writer)
            {
               WsdlVisitor visitor = new WsdlVisitor();
               visitor.ApplicationName = ApplicationName;
               visitor.ServiceName = ServiceName;
               visitor.UnmatchedMessageHandler = unmatchedHandler;
               visitor.Visit(wsdl, writer);
            }
            );

         Message msg = 
            Message.CreateMessage(MessageVersion.None, "", bodyWriter);
         return msg;
      }

      private WSDL.ServiceDescription GetServiceDescription()
      {
         OperationContext context = OperationContext.Current;
         ServiceMetadataExtension mex =
            context.Host.Extensions.Find<ServiceMetadataExtension>();
         // find the wsdl
         foreach ( MetadataSection section in mex.Metadata.MetadataSections )
         {
            if ( section.Dialect == MetadataSection.ServiceDescriptionDialect )
            {
               // there are multiple WSDL entries, one of them
               // has a Service included
               WSDL.ServiceDescription desc =
                  (WSDL.ServiceDescription)section.Metadata;
               if ( desc.Services.Count > 0 )
                  return (WSDL.ServiceDescription)section.Metadata;
            }
         }
         return null;
      }

      private string GetUnmatchedHandler()
      {
         ContractDescription contract = ServiceContract;
         foreach ( OperationDescription op in contract.Operations )
         {
            MessageDescription message = op.Messages[0];
            if ( message.Direction == MessageDirection.Input )
            {
               if ( message.Action == MetadataUris.UMH )
               {
                  return op.Name;
               }
            }
         }
         return null;
      }

      class DelegatedBodyWriter : BodyWriter
      {
         public delegate void WriteBody(XmlDictionaryWriter writer);
         private WriteBody _writeBody;

         public DelegatedBodyWriter(WriteBody writeBody)
            : base(true)
         {
            _writeBody = writeBody;
         }

         protected override void OnWriteBodyContents(XmlDictionaryWriter writer)
         {
            _writeBody(writer);
         }
      } // class DelegatedBodyWriter

   } // class InstrumentationMetadata

} // namespace QuickCounters.Wcf
