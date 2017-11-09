// Use subject to: http://www.codeplex.com/Project/License.aspx?ProjectName=quickcounters
//
// Program.cs
//
// Author:
//    Tomas Restrepo (tomas@winterdom.com)
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Web.Services.Description;

using QuickCounters.Wcf;

namespace QCFromWsdl
{
   class QcFromWsdlApp
   {
      private string _appname;
      private string _wsdl;
      private string _outFilename;
      private string _unmatchedHandler;
      private string _serviceName;

      #region Properties
      //
      // Properties
      //
      public string Appname
      {
         get { return _appname; }
         set { _appname = value; }
      }

      public string Wsdl
      {
         get { return _wsdl; }
         set { _wsdl = value; }
      }
      public string OutFilename
      {
         get { return _outFilename; }
         set { _outFilename = value; }
      }

      public string UnmatchedHandler
      {
         get { return _unmatchedHandler; }
         set { _unmatchedHandler = value; }
      }
      public string ServiceName
      {
         get { return _serviceName; }
         set { _serviceName = value; }
      }

      #endregion


      public void GenerateFile()
      {
         XmlTextWriter writer = new XmlTextWriter(OutFilename, Encoding.UTF8);
         using ( writer )
         {
            writer.WriteStartDocument();
            writer.Formatting = Formatting.Indented;
            using ( XmlReader reader = new XmlTextReader(Wsdl) )
            {
               ServiceDescription svcdesc = ServiceDescription.Read(reader);

               WsdlVisitor visitor = new WsdlVisitor();
               visitor.ApplicationName = Appname;
               visitor.ServiceName = ServiceName;
               visitor.UnmatchedMessageHandler = UnmatchedHandler;
               visitor.Visit(svcdesc, writer);
            }
         }
      }


      static void Main(string[] args)
      {
         if ( args.Length < 3 )
         {
            PrintUsage();
            return;
         }
         try
         {
            QcFromWsdlApp app = new QcFromWsdlApp();
            foreach ( string param in args )
            {
               if ( param.StartsWith("/appname:") )
                  app.Appname = GetValue(param);
               else if ( param.StartsWith("/svcname:") )
                  app.ServiceName = GetValue(param);
               else if ( param.StartsWith("/umh:") )
                  app.UnmatchedHandler = GetValue(param);
               else if ( param.StartsWith("/out:") )
                  app.OutFilename = GetValue(param);
               else
                  app.Wsdl = param;
            }
            app.GenerateFile();

         } catch ( Exception ex )
         {
            Console.WriteLine(ex.ToString());
         }
      }

      static string GetValue(string param)
      {
         int pos = param.IndexOf(":");
         return param.Substring(pos+1);
      }

      static void PrintUsage()
      {
         Console.WriteLine(
            "QCFromWsdl.exe /appname:<ApplicationName> "
            + "[/svcname:<ServiceName>] "
            + "[/umh:<Name>] "
            + "/out:<Filename> <WSDL URL>"
            );

         Console.WriteLine();
         Console.WriteLine("/appname\tspecifies the name of the application");
         Console.WriteLine("/svcname\tforce the given service name instead of the one in the WSDL");
         Console.WriteLine("/umh\tadd an unmatched message handler operation");
         Console.WriteLine("/out\tname of the output file");
      }
   }
}
