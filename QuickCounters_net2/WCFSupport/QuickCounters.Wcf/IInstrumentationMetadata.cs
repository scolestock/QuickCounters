// Use subject to: http://www.codeplex.com/Project/License.aspx?ProjectName=quickcounters
//
// IInstrumentationMetada.cs
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

namespace QuickCounters.Wcf
{

   /// <summary>
   /// Interface used to describe the contract
   /// of our metadata publishing endpoint
   /// </summary>
   [ServiceContract(Namespace = MetadataUris.QC)]
   public interface IInstrumentationMetadata
   {
      [OperationContract(Action="*", ReplyAction="*")]
      Message Get();

   } // interface IInstrumentationMetadata

} // namespace QuickCounters.Wcf
