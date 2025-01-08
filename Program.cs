//Be Naame Khoda
//FileName: Program.cs

using System;
using System.Threading;

namespace RadioSchedulerService
{
    static class Program
    {
        private static ManualResetEvent _waitHandle = new ManualResetEvent(false);

        static void Main()
        {
            // Load settings from the configuration file
            LoadSettingsFromConfig();

            // تنظیم خودکار زاویه‌های فجر و عشاء
            Settings.AutoSetAngles();

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

        private static void LoadSettingsFromConfig()
        {
            try
            {
                // Load settings from the configuration file
                var config = ConfigManager.LoadConfig();
                var appSettings = config.Application;

                // تنظیم موقعیت جغرافیایی و زمان‌زون از فایل کانفیگ
                Settings.Latitude = appSettings.Latitude;
                Settings.Longitude = appSettings.Longitude;
                Settings.TimeZone = appSettings.TimeZone;
                Settings.TimerIntervalInMinutes = appSettings.TimerIntervalInMinutes;
                Settings.AmplifierEnabled = appSettings.AmplifierEnabled;
                Settings.AmplifierApiUrl = appSettings.AmplifierApiUrl;

                // تنظیم روش محاسبه و فرمت زمان از فایل کانفیگ
                Settings.CalculationMethod = (PrayTime.CalculationMethod)Enum.Parse(typeof(PrayTime.CalculationMethod), appSettings.CalculationMethod);
                Settings.AsrMethod = (PrayTime.AsrMethods)Enum.Parse(typeof(PrayTime.AsrMethods), appSettings.AsrMethod);
                Settings.TimeFormat = (PrayTime.TimeFormat)Enum.Parse(typeof(PrayTime.TimeFormat), appSettings.TimeFormat);
                Settings.AdjustHighLats = (PrayTime.AdjustingMethod)Enum.Parse(typeof(PrayTime.AdjustingMethod), appSettings.AdjustHighLats);

                Logger.LogMessage("Settings loaded from configuration file.");
            }
            catch (Exception ex)
            {
                Logger.LogMessage($"Error loading settings from config: {ex.Message}");
                throw;
            }
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