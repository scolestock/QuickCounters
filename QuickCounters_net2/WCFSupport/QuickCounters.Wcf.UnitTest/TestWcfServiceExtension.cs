// Use subject to: http://www.codeplex.com/Project/License.aspx?ProjectName=quickcounters
//
// TestWcfServiceExtension.cs
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
   /// Tests that that we instrumenta service properly.
   /// </summary>
   [TestFixture]
   public class TestWcfServiceExtension
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
         InstallCounters();

      }
      
      [TearDown]
      public void TearDown()
      {
         UninstallCounters();

         _host.Stop();
      }


      [Test]
      public void TestCounters()
      {
         ISimpleService client = 
            ChannelFactory<ISimpleService>.CreateChannel(
            new BasicHttpBinding(), new EndpointAddress(SVC_URI)
            );
         client.Method1();
         client.Method1();
         client.Method2();

         string qcuri = SVC_URI.ToString() + "/qc";
         InstrumentedApplication app = 
            InstrumentedApplication.LoadFromFile(qcuri);

         DataSet dataSet = app.GetCounterDataSet(Environment.MachineName);
         DataTable table = dataSet.Tables[0];
         // all methods registered
         Assert.AreEqual(3, table.Rows.Count);
         // verify all executed
         Assert.AreEqual(2, Convert.ToInt32(table.Rows[0][2]));
         Assert.AreEqual(1, Convert.ToInt32(table.Rows[1][2]));
         // verify Method2() call failed
         Assert.AreEqual(1, Convert.ToInt32(table.Rows[1][5]));
      }

      [Test]
      public void TestCountersOnUnmatchedHandlers()
      {
         ISimpleService client =
            ChannelFactory<ISimpleService>.CreateChannel(
            new BasicHttpBinding(), new EndpointAddress(SVC_URI)
            );
         // Call method marked with [Action="*"]
         Message msg = Message.CreateMessage(MessageVersion.Soap11, "NoSuchAction");
         client.Method3(msg);

         string qcuri = SVC_URI.ToString() + "/qc";
         InstrumentedApplication app =
            InstrumentedApplication.LoadFromFile(qcuri);

         DataSet dataSet = app.GetCounterDataSet(Environment.MachineName);
         DataTable table = dataSet.Tables[0];
         // all methods present
         Assert.AreEqual(3, table.Rows.Count);
         // verify all executed
         Assert.AreEqual(1, Convert.ToInt32(table.Rows[2][2]));
      }

      #region Installation Stuff
      //
      // Installation Stuff
      //
      private void InstallCounters()
      {
         string[] args = 
            {
               String.Format("/quickctrconfig={0}/qc", SVC_URI),
               "/AssemblyName",
               typeof(QuickCountersInstaller).Assembly.FullName
            };
         ManagedInstallerClass.InstallHelper(args);
      }

      private void UninstallCounters()
      {
         string[] args = 
            {
               "/uninstall",
               String.Format("/quickctrconfig={0}/qc", SVC_URI),
               "/AssemblyName",
               typeof(QuickCountersInstaller).Assembly.FullName
            };
         ManagedInstallerClass.InstallHelper(args);
      }
      #endregion // Installation Stuff

   } // class TestWcfServiceExtension

} // namespace QuickCounters.Wcf.UnitTest
