using System;
using System.ServiceProcess;
using System.Timers;

namespace RadioSchedulerService
{
    public partial class RadioSchedulerService : ServiceBase
    {
        private Scheduler _scheduler;
        private Timer _timer;

        public RadioSchedulerService()
        {
            //InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            // مقداردهی اولیه Scheduler
            _scheduler = new Scheduler();

            // تنظیم تایمر برای بررسی زمان‌بندی‌ها (هر ۱ دقیقه)
            _timer = new Timer(60000); // 60000 میلی‌ثانیه = ۱ دقیقه
            _timer.Elapsed += OnTimerElapsed;
            _timer.AutoReset = true;
            _timer.Enabled = true;

            Logger.LogMessage("RadioSchedulerService started.");
        }

        protected override void OnStop()
        {
            // توقف تایمر و Scheduler
            _timer.Stop();
            _timer.Dispose();
            _scheduler = null;

            Logger.LogMessage("RadioSchedulerService stopped.");
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            // بررسی زمان‌بندی‌ها و اجرای پخش
            _scheduler.ReloadScheduleConfig();
        }
    }
}