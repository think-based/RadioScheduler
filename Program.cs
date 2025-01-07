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
            // تنظیم زمان‌زون پیش‌فرض
            Settings.TimeZone = 3.5; // منطقه زمانی پیش‌فرض (UTC+3.5)

            // تنظیم موقعیت جغرافیایی (شیراز)
            Settings.Latitude = 29.5916; // عرض جغرافیایی شیراز
            Settings.Longitude = 52.5837; // طول جغرافیایی شیراز

            // تنظیم خودکار زاویه‌های فجر و عشاء
            Settings.AutoSetAngles();

            // تنظیم روش محاسبه
            Settings.CalculationMethod = PrayTime.CalculationMethod.Tehran;
            Settings.AsrMethod = PrayTime.AsrMethods.Shafii;
            Settings.TimeFormat = PrayTime.TimeFormat.Time24;

            // تنظیمات مناطق با عرض جغرافیایی بالا
            Settings.AdjustHighLats = PrayTime.AdjustingMethod.MidNight;

            // ایجاد نمونه‌ای از Scheduler
            var scheduler = new Scheduler();

            // ایجاد نمونه‌ای از PrayTimeScheduler
            var prayTimeScheduler = new PrayTimeScheduler();

            // راه‌اندازی وب سرور
            var webServer = new WebServer(scheduler);
            webServer.Start();
            Console.WriteLine("Web server started. Press Ctrl+C to stop...");

            // نمایش زمان‌های شرعی در کنسول
            DisplayPrayerTimes();

            // منتظر سیگنال برای توقف برنامه
            Console.CancelKeyPress += (sender, e) =>
            {
                Console.WriteLine("Stopping Radio Scheduler Service...");
                _waitHandle.Set(); // سیگنال برای توقف برنامه
            };

            _waitHandle.WaitOne(); // منتظر بمان تا سیگنال دریافت شود
        }

        private static void DisplayPrayerTimes()
        {
            // تاریخ و زمان فعلی
            DateTime now = DateTime.Now;
            int year = now.Year;
            int month = now.Month;
            int day = now.Day;

            // محاسبه زمان‌های شرعی برای امروز
            string[] prayerTimes = new PrayTime().getPrayerTimes(year, month, day, Settings.Latitude, Settings.Longitude, (int)Settings.TimeZone);

            // نمایش زمان‌های شرعی
            Console.WriteLine("Prayer Times for Today:");
            Console.WriteLine($"Fajr: {prayerTimes[0]}");
            Console.WriteLine($"Sunrise: {prayerTimes[1]}");
            Console.WriteLine($"Dhuhr: {prayerTimes[2]}");
            Console.WriteLine($"Asr: {prayerTimes[3]}");
            Console.WriteLine($"Sunset: {prayerTimes[4]}");
            Console.WriteLine($"Maghrib: {prayerTimes[5]}");
            Console.WriteLine($"Isha: {prayerTimes[6]}");
        }
    }
}