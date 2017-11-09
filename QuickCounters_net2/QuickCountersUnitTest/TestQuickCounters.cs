// Scott Colestock / www.traceofthought.net
// Use subject to: http://www.codeplex.com/Project/License.aspx?ProjectName=quickcounters

using System;
using System.Configuration.Install;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Soap;

using NUnit.Framework;

using QuickCounters;

namespace QuickCountersUnitTest
{
   [TestFixture]
   public class TestQuickCounters
   {

      [TestFixtureSetUp]
      public void Init()
      {
         TestInstallation();
      }

      [TestFixtureTearDown]
      public void Dispose()
      {
         TestUnInstallation();
      }

      [Test]
      public void TestConfigDeserialization()
      {
         InstrumentedApplication application =
            InstrumentedApplication.LoadFromFile("testcounters.xml");
         Console.WriteLine("Component count: {0}", application.Components.GetLength(0));

         Assert.IsTrue(application.Components.GetLength(0) == 2);

         for (int i = 0; i < application.Components.GetLength(0); i++)
         {
            Console.WriteLine("For perf object {0}, request type count is: {1}", i, application.Components[i].RequestTypes.GetLength(0));
         }
      }

      [Test]
      public void TestConfigDeserializationFromReader()
      {
         using ( StreamReader reader = new StreamReader("testcounters.xml") )
         {
            InstrumentedApplication application =
               InstrumentedApplication.LoadFromReader(reader);
            Console.WriteLine("Component count: {0}", application.Components.GetLength(0));

            Assert.IsTrue(application.Components.GetLength(0) == 2);

            for ( int i = 0; i < application.Components.GetLength(0); i++ )
            {
               Console.WriteLine("For perf object {0}, request type count is: {1}", i, application.Components[i].RequestTypes.GetLength(0));
            }
         }
      }


      [Test]
      public void TestGetCounterDataSet()
      {
         InstrumentedApplication application =
            InstrumentedApplication.LoadFromFile("testcounters.xml");
         Console.WriteLine("Component count: {0}", application.Components.GetLength(0));

         DataSet dataSet = application.GetCounterDataSet(Environment.MachineName);
         Console.WriteLine(dataSet.GetXml());


         InstrumentedApplication.UpdateCounterDataSet(dataSet);
         Console.WriteLine(dataSet.GetXml());
      }

      //[Test]
      public void TestInstallation()
      {
         Console.WriteLine("Trying install...");

         string[] args = 
            {
               String.Format("/quickctrconfig={0}", "testcounters.xml"),
               "QuickCounters.net.dll"
            };

         ManagedInstallerClass.InstallHelper(args);
      }

      //[Test]
      public void TestUnInstallation()
      {
         Console.WriteLine("Trying uninstall...");

         string[] args = 
            {
               "/uninstall",
               String.Format("/quickctrconfig={0}", "testcounters.xml"),
               "QuickCounters.net.dll"
            };

         ManagedInstallerClass.InstallHelper(args);
      }

      [Test]
      public void TestRequestsCompletedAndStarted()
      {
         RequestType fakeWorkRequest = AttachCounter();

         int iterations = 50;
         long initialComplete = fakeWorkRequest.RequestsCompleted;
         long initialStarted = fakeWorkRequest.RequestsStarted;

         for (int i = 0; i < iterations; i++)
         {
            // Increments requests executing, requests started
            fakeWorkRequest.BeginRequest();

            // Increments requests completed.  Decrements requests executing.
            // Sets request execution time.
            // Affects persec/permin/perhour
            fakeWorkRequest.SetComplete();
         }

         Assert.IsTrue(fakeWorkRequest.RequestsCompleted == iterations + initialComplete);
         Assert.IsTrue(fakeWorkRequest.RequestsStarted == iterations + initialStarted);
      }


      static int completion = 0;
      [Test]
      public void TestRequestsCompletedMultiThread()
      {
         RequestType fakeWorkRequest = AttachCounter();
         long init = fakeWorkRequest.RequestsCompleted;
         int iterations = 500;

         for (int i = 0; i < iterations; i++)
         {
            WaitCallback wc = new WaitCallback(UpdateCounter);
            System.Threading.ThreadPool.QueueUserWorkItem(wc);
         }

         while (completion < iterations)
         {
            Thread.Sleep(500);
            Console.WriteLine("waiting for queued work to complete");
         }

         Assert.IsTrue(fakeWorkRequest.RequestsCompleted == init + iterations);

      }

      void UpdateCounter(object state)
      {
         RequestType fakeWorkRequest = AttachCounter();
         //Console.WriteLine("queued begin request");
         fakeWorkRequest.BeginRequest();
         Thread.Sleep(50);

         MemoryStream stream = new MemoryStream();
         SoapFormatter formatter = new SoapFormatter();
         formatter.Serialize(stream, fakeWorkRequest);

         stream.Seek(0, System.IO.SeekOrigin.Begin);
         SoapFormatter formatter2 = new SoapFormatter();

         RequestType deserialized = (RequestType)formatter2.Deserialize(stream);

         //Console.WriteLine("queued set complete on deserialized version");
         deserialized.SetComplete();

         System.Threading.Interlocked.Increment(ref completion);
      }


      [Test]
      public void TestSecondaryCounters()
      {
         RequestType notlegit = RequestType.Attach("QuickCountersUnitTest", "Illegitimate Work");
         RequestType fake2 = RequestType.Attach("QuickCountersUnitTest2", "Fake Work2");

         notlegit.BeginRequest();
         notlegit.SetComplete();

         Assert.IsTrue(notlegit.RequestsCompleted == 1);

         for (int i = 0; i < 100; i++)
         {
            notlegit.BeginRequest();
            Thread.Sleep(100);
            notlegit.SetComplete();
         }

         fake2.BeginRequest();
         fake2.SetAbort();
         Assert.IsTrue(fake2.RequestsFailed == 1);
      }

      [Test]
      public void TestRequestsCompletedWithMultiple()
      {
         RequestType fakeWorkRequest = AttachCounter();

         int iterations = 50;
         int multiple = 10;
         long initial = fakeWorkRequest.RequestsCompleted;

         for (int i = 0; i < iterations; i++)
         {
            fakeWorkRequest.BeginRequest(multiple);

            Thread.Sleep(100);

            fakeWorkRequest.SetComplete(multiple);
         }
         Assert.IsTrue(fakeWorkRequest.RequestsCompleted == iterations * multiple + initial);

         initial = fakeWorkRequest.RequestsFailed;
         for (int i = 0; i < iterations; i++)
         {
            fakeWorkRequest.BeginRequest(multiple);

            fakeWorkRequest.SetAbort(multiple);
         }
         Assert.IsTrue(fakeWorkRequest.RequestsFailed == iterations * multiple + initial);


      }

      [Test]
      public void TestMultipleAttaches()
      {
         RequestType fakeWorkRequest = AttachCounter();
         RequestType fakeWorkRequest2 = AttachCounter();

         Assert.IsTrue(fakeWorkRequest2.RequestsCompleted == fakeWorkRequest.RequestsCompleted);

         fakeWorkRequest.BeginRequest();
         fakeWorkRequest.SetComplete();

         Assert.IsTrue(fakeWorkRequest2.RequestsCompleted == fakeWorkRequest.RequestsCompleted);

         fakeWorkRequest2.BeginRequest();
         fakeWorkRequest2.SetComplete();

         Assert.IsTrue(fakeWorkRequest2.RequestsCompleted == fakeWorkRequest.RequestsCompleted);
      }

      [Test]
      public void TestRequestsFailed()
      {
         RequestType fakeWorkRequest = AttachCounter();

         int iterations = 50;
         long startCount = fakeWorkRequest.RequestsStarted;
         long failedCount = fakeWorkRequest.RequestsFailed;

         for (int i = 0; i < iterations; i++)
         {
            fakeWorkRequest.BeginRequest();

            fakeWorkRequest.SetAbort();
         }

         Console.Write("Should see value of {0} for requests failed in perfmon.", iterations);
         Assert.IsTrue(fakeWorkRequest.RequestsStarted == startCount + iterations);
         Assert.IsTrue(fakeWorkRequest.RequestsFailed == failedCount + iterations);
      }

      [Test]
      public void TestRequestsExecuting()
      {
         RequestType fakeWorkRequest = AttachCounter();

         Console.WriteLine("Current requests executing: {0}", fakeWorkRequest.RequestsExecuting);
         Assert.IsTrue(fakeWorkRequest.RequestsExecuting == 0);

         fakeWorkRequest.BeginRequest();
         fakeWorkRequest.BeginRequest();
         fakeWorkRequest.BeginRequest();

         Console.WriteLine("Should see value of 3 in perfmon for requests executing for next 5 seconds...");
         Assert.IsTrue(fakeWorkRequest.RequestsExecuting == 3);
         Thread.Sleep(5000);  // watch in perfmon 

         fakeWorkRequest.SetComplete();
         fakeWorkRequest.SetComplete();
         fakeWorkRequest.SetComplete();

         Assert.IsTrue(fakeWorkRequest.RequestsExecuting == 0);
      }

      [Test]
      public void TestExecutionTime()
      {
         RequestType fakeWorkRequest = AttachCounter();

         fakeWorkRequest.BeginRequest();
         Thread.Sleep(5000);  // watch in perfmon 

         fakeWorkRequest.SetComplete();

         Console.WriteLine("Request execution time: {0}", fakeWorkRequest.RequestExecutionTime);

         Assert.IsTrue(fakeWorkRequest.RequestExecutionTime > 4900 &&
            fakeWorkRequest.RequestExecutionTime < 5100);
      }

      [Test]
      public void TestRequestsPerSec()
      {
         RequestType fakeWorkRequest = AttachCounter();

         int iterations = 100;
         float reqPerSec = 0;

         Console.WriteLine("Time prior to {0} iterations: {1}", iterations, DateTime.Now);
         Console.WriteLine("Should see value of 10 in perfmon for requests/sec");
         for (int i = 0; i < iterations; i++)
         {
            fakeWorkRequest.BeginRequest();

            // Should result in 10 per second
            Thread.Sleep(100);

            fakeWorkRequest.SetComplete();

            reqPerSec = fakeWorkRequest.RequestsPerSecond;
         }

         Console.WriteLine("{0} requests per second", reqPerSec);
         Assert.IsTrue(reqPerSec > 8.0 &&
            reqPerSec < 12.0);

         Console.WriteLine("Time after {0} iterations: {1}", iterations, DateTime.Now);
      }

      [Test]
      public void TestRequestsPerMinute()
      {
         RequestType fakeWorkRequest = AttachCounter();

         int iterations = 400;

         Console.WriteLine("Time prior to {0} iterations: {1}", iterations, DateTime.Now);
         //Console.WriteLine("Should see value of 10 in perfmon for requests/sec");
         for (int i = 0; i < iterations; i++)
         {
            fakeWorkRequest.BeginRequest();

            Thread.Sleep(250);

            fakeWorkRequest.SetComplete();
         }

         Console.WriteLine("{0} requests per minute", fakeWorkRequest.RequestsPerMinute);
         Assert.IsTrue(fakeWorkRequest.RequestsPerMinute > 210 &&
            fakeWorkRequest.RequestsPerMinute < 250);

         Console.WriteLine("Time after {0} iterations: {1}", iterations, DateTime.Now);
      }

      [Test]
      public void TestSerialization()
      {
         RequestType fakeWorkRequest = AttachCounter();

         fakeWorkRequest.BeginRequest();
         fakeWorkRequest.SetComplete();

         MemoryStream stream = new MemoryStream();
         SoapFormatter formatter = new SoapFormatter();
         formatter.Serialize(stream, fakeWorkRequest);

         System.Text.Encoding encoding = System.Text.Encoding.UTF8;
         string ser = encoding.GetString(stream.ToArray());
         Console.WriteLine(ser);

         stream.Seek(0, System.IO.SeekOrigin.Begin);
         SoapFormatter formatter2 = new SoapFormatter();

         RequestType deserialized = (RequestType)formatter2.Deserialize(stream);
         deserialized.BeginRequest();
         deserialized.SetComplete();

         Assert.IsTrue(fakeWorkRequest.RequestsCompleted == deserialized.RequestsCompleted);

      }

      [Test]
      public void TestDeserialization()
      {

         StreamReader sr = File.OpenText("SerializedRequestType.xml");
         string state = sr.ReadToEnd();
         sr.Close();
         MemoryStream stream = new MemoryStream();
         byte[] bytes = System.Text.UTF8Encoding.UTF8.GetBytes(state);
         stream.Write(bytes, 0, bytes.Length);
         stream.Seek(0, System.IO.SeekOrigin.Begin);

         SoapFormatter formatter = new SoapFormatter();

         RequestType deserialized = (RequestType)formatter.Deserialize(stream);

         deserialized.BeginRequest();
         deserialized.SetComplete();

         RequestType fakeWorkRequest = AttachCounter();
         fakeWorkRequest.BeginRequest();
         fakeWorkRequest.SetComplete();

         Assert.IsTrue(fakeWorkRequest.RequestsCompleted == deserialized.RequestsCompleted);

      }

      RequestType AttachCounter()
      {
         return RequestType.Attach("QuickCountersUnitTest", "Fake Work", true);
      }

#if false
		void SampleUsage()
		{
			RequestType someRequest = RequestType.Attach("MyApplication","someRequest");

			someRequest.BeginRequest();

			try
			{
				// Do useful work...

				someRequest.SetComplete();
			}
			catch
			{
				someRequest.SetAbort();
				throw;
			}

		}
#endif


   }
}
