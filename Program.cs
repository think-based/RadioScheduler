//Be Naame Khoda
//FileName: Program.cs

using System;
using System.ServiceProcess;
using System.Threading;

namespace RadioSchedulerService
{
    static class Program
    {
        private static ManualResetEvent _waitHandle = new ManualResetEvent(false);

        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new RadioSchedulerService()
            };

            // راه‌اندازی وب سرور
            var scheduler = new Scheduler();
            var webServer = new WebServer(scheduler);
            webServer.Start();
            Console.WriteLine("Web server started. Press Enter to stop...");
            Console.ReadLine();
            Console.WriteLine("Web server started. Press Ctrl+C to stop...");

            // منتظر سیگنال برای توقف برنامه
            Console.CancelKeyPress += (sender, e) =>
            {
                Console.WriteLine("Stopping Radio Scheduler Service...");
                _waitHandle.Set(); // سیگنال برای توقف برنامه
            };

            _waitHandle.WaitOne(); // منتظر بمان تا سیگنال دریافت شود
            // ServiceBase.Run(ServicesToRun);
        }
    }
}