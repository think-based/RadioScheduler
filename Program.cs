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
            // Load settings from the configuration file
            LoadSettingsFromConfig();
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


                // Start the WebServer
                _webServer.Start();

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

        static async Task Main(string[] args)
        {
            Program program = new Program();
            await program.RunAsync();
        }
    }
}
    