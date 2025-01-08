//Be Naame Khoda
//FileName: Program.cs

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace RadioSchedulerService
{
    static class Program
    {
        private static PrayTimeScheduler _prayTimeScheduler;
        private static InstantPlayManager _instantPlayManager;
        private static WebServer _webServer;
        private static Scheduler _scheduler; // Add Scheduler instance
        private static CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        static async Task Main()
        {
            try
            {
                // Load settings from the configuration file
                LoadSettingsFromConfig();

                // Initialize the InstantPlayManager
                string instantPlayFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "InstantPlay");
                _instantPlayManager = new InstantPlayManager(instantPlayFolderPath);

                // Initialize the PrayTimeScheduler
                _prayTimeScheduler = new PrayTimeScheduler();

                // Initialize the Scheduler
                _scheduler = new Scheduler();

                // Initialize the WebServer with the Scheduler
                _webServer = new WebServer(_scheduler);
                _webServer.Start();

                // Display prayer times in the console
                DisplayPrayerTimes();

                // Handle Ctrl+C to gracefully stop the application
                Console.CancelKeyPress += OnConsoleCancelKeyPress;

                // Keep the application running until cancellation is requested
                await RunApplicationAsync();
            }
            catch (Exception ex)
            {
                Logger.LogMessage($"Fatal error: {ex.Message}");
                Console.WriteLine($"Fatal error: {ex.Message}");
            }
            finally
            {
                // Clean up resources
                _prayTimeScheduler = null;
                _instantPlayManager = null;
                _scheduler = null;
                _webServer = null;
                _cancellationTokenSource.Dispose();
            }

            Console.WriteLine("Application stopped.");
        }

        private static async Task RunApplicationAsync()
        {
            Console.WriteLine("Radio Scheduler Service started. Press Ctrl+C to exit.");

            // Keep the application running until cancellation is requested
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                await Task.Delay(1000, _cancellationTokenSource.Token); // Check every second
            }
        }

        private static void OnConsoleCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            Console.WriteLine("Stopping Radio Scheduler Service...");

            // Cancel the token to stop the application
            _cancellationTokenSource.Cancel();

            // Prevent the application from terminating immediately
            e.Cancel = true;
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