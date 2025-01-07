//Be Naame Khoda
//FileName: PrayTimeScheduler.cs

using System;
using System.Timers;

public class PrayTimeScheduler
{
    private Timer _timer;
    private PrayTime _prayTime;

    public PrayTimeScheduler()
    {
        _prayTime = new PrayTime();

        // تنظیم تایمر برای فعال شدن هر 24 ساعت
        _timer = new Timer(24 * 60 * 60 * 1000); // 24 ساعت
        _timer.Elapsed += OnTimerElapsed;
        _timer.AutoReset = true;
        _timer.Enabled = true;

        // اولین بار زمان‌های شرعی را محاسبه و تنظیم کنید
        SetNextPrayTime();
    }

    private void OnTimerElapsed(object sender, ElapsedEventArgs e)
    {
        // هر 24 ساعت زمان‌های شرعی را محاسبه و تنظیم کنید
        SetNextPrayTime();
    }

    private void SetNextPrayTime()
    {
        // محاسبه زمان‌های شرعی و تنظیم CurrentTrigger
        // (این بخش باید با توجه به نیاز شما پیاده‌سازی شود)
    }
}