using System;
using System.Collections.Generic;
using System.Text;
using QuickCounters;

namespace QCConsoleApp
{
   class Program
   {
      static void Main(string[] args)
      {
         while (true)
         {
            DoWork();
         }
      }

      public static void DoWork()
      {
         RequestType request = RequestType.Attach("QCConsoleAppComponent",
            "DoWork");

         try
         {
            request.BeginRequest();
            ConsoleKeyInfo keyInfo = Console.ReadKey();
            if (keyInfo.KeyChar == 'c')
               request.SetComplete();
            else
               throw (new Exception());
         }
         catch
         {
            request.SetAbort();
         }

      }
   }
}
