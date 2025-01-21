      //Be Naame Khoda
//FileName: RadioSchedulerService.cs

using System;
using System.ServiceProcess;
using System.Timers;
using System.IO;

namespace RadioSchedulerService
{
    public partial class RadioSchedulerService : ServiceBase
    {
        private Scheduler _scheduler;
        private Timer _timer;
        private WebServer _webServer;

        public RadioSchedulerService()
        {
            //InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
             // Create concrete implementations
            IAudioPlayer audioPlayer = new AudioPlayer();
            ISchedulerConfigManager configManager = new SchedulerConfigManager(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "audio.conf"));
            IScheduleCalculatorFactory scheduleCalculatorFactory = new ScheduleCalculatorFactory();
             ITriggerManager triggerManager = ActiveTriggers.Triggers as ITriggerManager;

             _scheduler = new Scheduler(audioPlayer, configManager, scheduleCalculatorFactory, triggerManager);
            _webServer = new WebServer(_scheduler);
            _webServer.Start();

            _timer = new Timer(60000); // 1 minute
            _timer.Elapsed += OnTimerElapsed;
            _timer.AutoReset = true;
            _timer.Enabled = true;

            Logger.LogMessage("RadioSchedulerService started.");
        }

        protected override void OnStop()
        {
            _timer.Stop();
            _timer.Dispose();
            _scheduler = null;
            _webServer = null;

            Logger.LogMessage("RadioSchedulerService stopped.");
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            _scheduler.ConfigManager.ReloadScheduleConfig();
        }
    }
}
    