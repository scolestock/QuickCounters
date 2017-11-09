// Use subject to: http://www.codeplex.com/Project/License.aspx?ProjectName=quickcounters
//
// TestWcfClientExtension.cs
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
using System.ServiceModel.Configuration;
using System.Text;

using NUnit.Framework;
using QuickCounters;
using QuickCounters.Wcf;
using QuickCounters.Wcf.Configuration;
using OConfiguration = System.Configuration.Configuration;
using ConfigurationManager = System.Configuration.ConfigurationManager;

namespace QuickCounters.Wcf.UnitTest
{
   /// <summary>
   /// Tests that we can instrument a client-side
   /// proxy.
   /// </summary>
   [TestFixture]
   public class TestWcfClientExtension
   {
      ServiceHostHelper _host;
      private readonly static Uri SVC_URI = ServiceHostHelper.SVC_URI;

      [SetUp]
      public void Setup()
      {
         _host = new ServiceHostHelper();
         _host.Start(null);

         InstallCounters();
      }
      
      [TearDown]
      public void TearDown()
      {
         _host.Stop();
         UninstallCounters();
      }

      [Test]
      public void TestTwoWayCounters()
      {
         ISimpleService client = CreateClient();

         client.Method1();
         client.Method1();

         InstrumentedApplication app =
            InstrumentedApplication.LoadFromFile("TestClient.xml");

         DataSet dataSet = app.GetCounterDataSet(Environment.MachineName);
         DataTable table = dataSet.Tables[0];
         // test two calls to Method1();
         Assert.AreEqual(2, Convert.ToInt32(table.Rows[0][2]));
         // test operations show "completed"
         Assert.AreEqual(0, Convert.ToInt32(table.Rows[0][3]));

         // test two-way operation timing is > 0
         Assert.Greater(Convert.ToInt32(table.Rows[0][6]), 0);
      }

      [Test]
      public void TestOneWayCounters()
      {
         ISimpleService client = CreateClient();

         client.Method2();

         InstrumentedApplication app =
            InstrumentedApplication.LoadFromFile("TestClient.xml");

         DataSet dataSet = app.GetCounterDataSet(Environment.MachineName);
         DataTable table = dataSet.Tables[0];
         // single call of the method
         Assert.AreEqual(1, Convert.ToInt32(table.Rows[1][2]));
         // ensure one-way operations are not left "executing"
         Assert.AreEqual(0, Convert.ToInt32(table.Rows[1][3]));
      }


      #region Installation Stuff
      //
      // Installation Stuff
      //
      private void InstallCounters()
      {
         string[] args = 
            {
               "/quickctrconfig=TestClient.xml",
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
               "/quickctrconfig=TestClient.xml",
               "/AssemblyName",
               typeof(QuickCountersInstaller).Assembly.FullName
            };
         ManagedInstallerClass.InstallHelper(args);
      }
      #endregion // Installation Stuff

      
      #region Client-side Proxy
      //
      // Client-side Proxy
      //

      private ISimpleService CreateClient()
      {
         ClientProxy client = new ClientProxy(
                     new BasicHttpBinding(), SVC_URI
                     );
         // apply behavior
         client.Endpoint.Behaviors.Add(
            new InstrumentedClientBehavior("TestClient")
            );

         return client;
      }

      private class ClientProxy 
         : ClientBase<ISimpleService>, ISimpleService
      {
         public ClientProxy(Binding binding, Uri uri)
            : base(binding, new EndpointAddress(uri))
         {
         }

         public void Method1()
         {
            base.Channel.Method1();
         }

         public void Method2()
         {
            base.Channel.Method2();
         }

         public void Method3(Message msg)
         {
            base.Channel.Method3(msg);
         }
      }
      #endregion // Client-side Proxy

   } // class TestWcfClientExtension

} // namespace QuickCounters.Wcf.UnitTest
