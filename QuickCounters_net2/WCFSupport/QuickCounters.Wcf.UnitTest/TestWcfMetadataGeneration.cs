// Use subject to: http://www.codeplex.com/Project/License.aspx?ProjectName=quickcounters
//
// TestWcfMetadataGeneration.cs
//
// Author:
//    Tomas Restrepo (tomas@winterdom.com)
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration.Install;
using System.Data;
using System.IO;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.Text;

using NUnit.Framework;
using QuickCounters;
using QuickCounters.Wcf;

namespace QuickCounters.Wcf.UnitTest
{
   /// <summary>
   /// Tests that we generate the QC XML from the 
   /// service behavior correctly
   /// </summary>
   [TestFixture]
   public class TestWcfMetadataGeneration
   {
      ServiceHostHelper _host;
      private readonly static Uri SVC_URI = ServiceHostHelper.SVC_URI;

      [SetUp]
      public void Setup()
      {
         _host = new ServiceHostHelper();
         _host.Start(
            delegate(ServiceHostBase theHost)
            {
               InstrumentedServiceAttribute isa = 
                  new InstrumentedServiceAttribute();
               isa.Service = "CoolService";
               isa.Address = "qc";
               isa.ApplicationName = "MyApp";
               isa.EnableHttpGet = true;
               theHost.Description.Behaviors.Add(isa);
            }
         );

      }
      
      [TearDown]
      public void TearDown()
      {
         _host.Stop();
      }

      [Test]
      public void TestCanGenerateQCXml()
      {
         string qcuri = SVC_URI.ToString() + "/qc";
         InstrumentedApplication app = 
            InstrumentedApplication.LoadFromFile(qcuri);
         Assert.AreEqual(1, app.Components.Length);
         
         Component component = app.Components[0];
         Assert.AreEqual("CoolService", component.Name);
         Assert.AreEqual("Method1", component.RequestTypes[0].Name);
         Assert.AreEqual("Method2", component.RequestTypes[1].Name);
         Assert.AreEqual("Method3", component.RequestTypes[2].Name);
      }

   } // class TestWcfMetadataGeneration

} // namespace QuickCounters.Wcf.UnitTest
