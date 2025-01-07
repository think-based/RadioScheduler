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
            // تنظیمات کاربر
            Settings.Latitude = 36.2972; // عرض جغرافیایی تبریز
            Settings.Longitude = 59.6067; // طول جغرافیایی تبریز
            Settings.TimeZone = 3.5; // منطقه زمانی تبریز
            Settings.CalculationMethod = PrayTime.CalculationMethod.Tehran;
            Settings.AsrMethod = PrayTime.AsrMethods.Shafii;
            Settings.TimeFormat = PrayTime.TimeFormat.Time12;

            // راه‌اندازی وب سرور
            var webScheduler = new Scheduler(); // تغییر نام متغیر برای جلوگیری از تداخل
            var webServer = new WebServer(webScheduler);
            webServer.Start();
            Console.WriteLine("Web server started. Press Ctrl+C to stop...");

            // ایجاد نمونه‌ای از PrayTimeScheduler
            var prayTimeScheduler = new PrayTimeScheduler(); // تغییر نام متغیر برای جلوگیری از تداخل

            // منتظر سیگنال برای توقف برنامه
            Console.CancelKeyPress += (sender, e) =>
            {
                Console.WriteLine("Stopping Radio Scheduler Service...");
                _waitHandle.Set(); // سیگنال برای توقف برنامه
            };

            _waitHandle.WaitOne(); // منتظر بمان تا سیگنال دریافت شود

            // اگر برنامه یک سرویس ویندوز است، از این خط استفاده کنید:
            // ServiceBase.Run(new ServiceBase[] { new RadioSchedulerService() });
        }
    }
}