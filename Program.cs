//Be Naame Khoda
//FileName: Program.cs

using System;
using System.Threading;
using System.Threading.Tasks;

namespace RadioSchedulerService
{
    static class Program
    {
        private static PrayTimeScheduler _prayTimeScheduler;
        private static InstantPlayManager _instantPlayManager;
        private static CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        static async Task Main()
        {
            // Load settings from the configuration file
            LoadSettingsFromConfig();

            // Initialize the InstantPlayManager
            string instantPlayFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "InstantPlay");
            _instantPlayManager = new InstantPlayManager(instantPlayFolderPath);

            // Create and store the PrayTimeScheduler instance
            _prayTimeScheduler = new PrayTimeScheduler();

            // Display prayer times in the console
            DisplayPrayerTimes();

            // Handle Ctrl+C to stop the application
            Console.CancelKeyPress += (sender, e) =>
            {
                Console.WriteLine("Stopping Radio Scheduler Service...");

                // Dispose of resources explicitly
                _prayTimeScheduler = null;
                _instantPlayManager = null;

                // Cancel the token to stop the application
                _cancellationTokenSource.Cancel();

                // Prevent the application from terminating immediately
                e.Cancel = true;
            };

            // Keep the application running until cancellation is requested
            try
            {
                await Task.Delay(Timeout.Infinite, _cancellationTokenSource.Token);
            }
            catch (TaskCanceledException)
            {
                // Expected when the token is canceled
            }

            Console.WriteLine("Application stopped.");
        }

        private static void LoadSettingsFromConfig()
        {
            try
            {
                // Load settings from the configuration file
                var config = ConfigManager.LoadConfig();
                var appSettings = config.Application;

                // Set application settings
                Settings.Latitude = appSettings.Latitude;
                Settings.Longitude = appSettings.Longitude;
                Settings.TimeZone = appSettings.TimeZone;
                Settings.TimerIntervalInMinutes = appSettings.TimerIntervalInMinutes;
                Settings.AmplifierEnabled = appSettings.AmplifierEnabled;
                Settings.AmplifierApiUrl = appSettings.AmplifierApiUrl;

                // Set calculation method and time format
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
            // Get current date and time
            DateTime now = DateTime.Now;
            int year = now.Year;
            int month = now.Month;
            int day = now.Day;

            // Calculate prayer times for today
            string[] prayerTimes = new PrayTime().getPrayerTimes(year, month, day, Settings.Latitude, Settings.Longitude, (int)Settings.TimeZone);

            // Display prayer times
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