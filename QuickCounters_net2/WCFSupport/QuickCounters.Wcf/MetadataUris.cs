// Use subject to: http://www.codeplex.com/Project/License.aspx?ProjectName=quickcounters
//
// MetadataUris.cs
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

   internal static class MetadataUris
   {
      public const string MEX = "http://schemas.microsoft.com/2006/04/mex";
      public const string WSDL = "http://schemas.microsoft.com/2006/04/http/metadata";
      public const string QC = "urn:schemas-winterdom-com:performance";
      public const string UMH = "*";

   } // class MetadataUris

} // namespace QuickCounters.Wcf
