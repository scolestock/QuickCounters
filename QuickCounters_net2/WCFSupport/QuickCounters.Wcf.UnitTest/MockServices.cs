// Use subject to: http://www.codeplex.com/Project/License.aspx?ProjectName=quickcounters
//
// MockServices.cs
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

   #region Mock Service used by the tests
   //
   // Mock Service used by the tests
   //
   [ServiceContract]
   public interface ISimpleService
   {
      [OperationContract(Action="Whatever")]
      void Method1();
      [OperationContract(IsOneWay = true)]
      void Method2();
      [OperationContract(Action="*")]
      void Method3(Message msg);
   } // interface ISimpleService

   public class SimpleService : ISimpleService
   {
      public void Method1()
      {
         System.Threading.Thread.Sleep(1000);
      }

      public void Method2()
      {
         throw new NotImplementedException();
      }

      public void Method3(Message msg)
      {
      }
   }

   #endregion // Mock Service used by the tests


   /// <summary>
   /// Helper class for inproc hosting of the service
   /// </summary>
   internal class ServiceHostHelper
   {
      private ServiceHost _host;
      public readonly static Uri SVC_URI = new Uri("http://localhost:10289/SimpleService");

      public delegate void CustomizeHost(ServiceHostBase host);

      public void Start(CustomizeHost customizer)
      {
         _host = new ServiceHost(typeof(SimpleService), SVC_URI);
         Binding binding = new BasicHttpBinding();
         _host.AddServiceEndpoint(typeof(ISimpleService), binding, SVC_URI);

         ServiceMetadataBehavior bhv = new ServiceMetadataBehavior();
         bhv.HttpGetEnabled = true;
         _host.Description.Behaviors.Add(bhv);

         ServiceDebugBehavior dbg = 
            _host.Description.Behaviors.Find<ServiceDebugBehavior>();
         dbg.IncludeExceptionDetailInFaults = true;

         if ( customizer != null )
            customizer(_host);

         _host.Open();
      }

      public void Stop()
      {
         if ( _host.State == CommunicationState.Opened )
            _host.Close();
      }
   }


} // namespace QuickCounters.Wcf.UnitTest
