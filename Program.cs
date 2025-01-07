//Be Naame Khoda
//FileName: Program.cs

using System;
using System.Runtime;
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

            Settings.Latitude = 36.2972; // عرض جغرافیایی تبریز
            Settings.Longitude = 59.6067; // طول جغرافیایی تبریز
            Settings.TimeZone = 3.5; // منطقه زمانی تبریز
            Settings.CalculationMethod = PrayTime.CalculationMethod.Tehran;
            Settings.AsrMethod = PrayTime.AsrMethods.Shafii;
            Settings.TimeFormat = PrayTime.TimeFormat.Time12;

            // ایجاد نمونه‌ای از PrayTimeScheduler
            PrayTimeScheduler scheduler = new PrayTimeScheduler();

            _waitHandle.WaitOne(); // منتظر بمان تا سیگنال دریافت شود
            // ServiceBase.Run(ServicesToRun);
        }
    }
}