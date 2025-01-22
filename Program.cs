      // Be Naame Khoda
// FileName: Program.cs

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace RadioSchedulerService
{
    class Program
    {
        private PrayTimeScheduler _prayTimeScheduler;
        private InstantPlayManager _instantPlayManager;
        private WebServer _webServer;
        private Scheduler _scheduler;
        private CancellationTokenSource _cancellationTokenSource;

        public Program()
        {
            _cancellationTokenSource = new CancellationTokenSource();

            // Create concrete implementations
            IAudioPlayer audioPlayer = new AudioPlayer();
            ISchedulerConfigManager configManager = new SchedulerConfigManager(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "audio.conf"));
            IScheduleCalculatorFactory scheduleCalculatorFactory = new ScheduleCalculatorFactory();
            ITriggerManager triggerManager = new ActiveTriggersManager();


            _scheduler = new Scheduler(audioPlayer, configManager, scheduleCalculatorFactory, triggerManager);
            _instantPlayManager = new InstantPlayManager(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "InstantPlay"));
            _prayTimeScheduler = new PrayTimeScheduler();
             _webServer = new WebServer(_scheduler);
        }

        public async Task RunAsync()
        {
            try
            {
                // Load settings from the configuration file
                LoadSettingsFromConfig();

                // Start the WebServer
                _webServer.Start();

                // Display prayer times in the console
                DisplayPrayerTimes();

                // Handle Ctrl+C to gracefully stop the application
                Console.CancelKeyPress += OnConsoleCancelKeyPress;

                // Keep the application running until cancellation is requested
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    await Task.Delay(1000, _cancellationTokenSource.Token); // Check every second
                }
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

        private void OnConsoleCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            Console.WriteLine("Stopping Radio Scheduler Service...");

            // Cancel the token to stop the application
            _cancellationTokenSource.Cancel();

            // Prevent the application from terminating immediately
            e.Cancel = true;
        }

        private void LoadSettingsFromConfig()
        {
            try
            {
                // Load settings from the configuration file
                var config = AppConfigManager.LoadConfig();
                var appSettings = config.Application;

                // Set application settings
                Settings.Latitude = appSettings.Latitude;
                Settings.Longitude = appSettings.Longitude;
                Settings.TimeZoneId = appSettings.TimeZoneId;
                Settings.TimeZoneOffset = appSettings.TimeZoneOffset;
                  Settings.Region = appSettings.Region;
                Settings.TimerIntervalInMinutes = appSettings.TimerIntervalInMinutes;
                Settings.AmplifierEnabled = appSettings.AmplifierEnabled;
                Settings.AmplifierApiUrl = appSettings.AmplifierApiUrl;

                // Set calculation method and time format
                Settings.CalculationMethod = (PrayTime.CalculationMethod)Enum.Parse(typeof(PrayTime.CalculationMethod), appSettings.CalculationMethod);
                Settings.AsrMethod = (PrayTime.AsrMethods)Enum.Parse(typeof(PrayTime.AsrMethods), appSettings.AsrMethod);
                Settings.TimeFormat = (PrayTime.TimeFormat)Enum.Parse(typeof(PrayTime.TimeFormat), appSettings.TimeFormat);
                Settings.AdjustHighLats = (PrayTime.AdjustingMethod)Enum.Parse(typeof(PrayTime.AdjustingMethod), appSettings.AdjustHighLats);
                 Settings.AutoSetAngles();
                Logger.LogMessage("Settings loaded from configuration file.");
            }
            catch (Exception ex)
            {
                Logger.LogMessage($"Error loading settings from config: {ex.Message}");
                throw;
            }
        }

        private void DisplayPrayerTimes()
        {
            // Get current date and time
            DateTime now = DateTime.Now;
            int year = now.Year;
            int month = now.Month;
            int day = now.Day;

            // Calculate prayer times for today
            string[] prayerTimes = new PrayTime().getPrayerTimes(year, month, day, Settings.Latitude, Settings.Longitude, (int)Settings.TimeZoneOffset);

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

        static async Task Main(string[] args)
        {
            Program program = new Program();
            await program.RunAsync();
        }
    }
}
    