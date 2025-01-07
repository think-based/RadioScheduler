//Be Naame Khoda
//FileName: PrayTimeScheduler.cs

using System;
using System.Timers;

public class PrayTimeScheduler
{
    private Timer _timer;
    private PrayTime _prayTime;
    private CurrentTrigger _currentTrigger;

    public PrayTimeScheduler()
    {
        _prayTime = new PrayTime();
        _currentTrigger = new CurrentTrigger();

        // تنظیم تایمر پویا
        _timer = new Timer();
        _timer.Elapsed += OnTimerElapsed;
        _timer.AutoReset = false; // تایمر فقط یک بار فعال می‌شود

        // اولین بار زمان‌های شرعی را محاسبه و تنظیم کنید
        SetNextPrayTime();
    }

    private void OnTimerElapsed(object sender, ElapsedEventArgs e)
    {
        // زمان شرعی بعدی را تنظیم کنید
        SetNextPrayTime();
    }

    private void SetNextPrayTime()
    {
        // تاریخ و زمان فعلی
        DateTime now = DateTime.Now;
        int year = now.Year;
        int month = now.Month;
        int day = now.Day;

        // استفاده از تنظیمات کاربر
        double latitude = Settings.Latitude;
        double longitude = Settings.Longitude;
        double timeZone = Settings.TimeZone;

        // محاسبه زمان‌های شرعی برای امروز
        string[] prayerTimes = _prayTime.getPrayerTimes(year, month, day, latitude, longitude, (int)timeZone);

        // زمان‌های شرعی به ترتیب: فجر، طلوع آفتاب، ظهر، عصر، غروب آفتاب، مغرب، عشاء
        DateTime fajrTime = ParseTime(prayerTimes[0]);
        DateTime dhuhrTime = ParseTime(prayerTimes[2]);
        DateTime maghribTime = ParseTime(prayerTimes[5]);

        // زمان فعلی
        DateTime currentTime = DateTime.Now;

        // بررسی زمان شرعی بعدی
        DateTime nextPrayTime;
        string nextPrayName;

        if (currentTime < fajrTime)
        {
            nextPrayTime = fajrTime;
            nextPrayName = "Fajr";
        }
        else if (currentTime < dhuhrTime)
        {
            nextPrayTime = dhuhrTime;
            nextPrayName = "Dhuhr";
        }
        else if (currentTime < maghribTime)
        {
            nextPrayTime = maghribTime;
            nextPrayName = "Maghrib";
        }
        else
        {
            // اگر زمان فعلی بعد از مغرب باشد، زمان شرعی بعدی فردا فجر است
            nextPrayTime = fajrTime.AddDays(1);
            nextPrayName = "Fajr";
        }

        // تنظیم CurrentTrigger
        SetCurrentTrigger(nextPrayName, nextPrayTime);

        // تنظیم تایمر برای فعال شدن در زمان شرعی بعدی
        double timeUntilNextPray = (nextPrayTime - currentTime).TotalMilliseconds;
        _timer.Interval = timeUntilNextPray;
        _timer.Start();

        // چاپ اطلاعات برای بررسی
        Logger.LogMessage($"Next prayer: {nextPrayName} at {nextPrayTime}");
    }

    private void SetCurrentTrigger(string eventName, DateTime triggerTime)
    {
        _currentTrigger.Event = eventName;
        _currentTrigger.Time = triggerTime;

        // چاپ اطلاعات برای بررسی
        Logger.LogMessage($"CurrentTrigger set to: {eventName} at {triggerTime}");
    }

    private DateTime ParseTime(string time)
    {
        // تبدیل زمان از رشته به DateTime
        DateTime now = DateTime.Now;
        return DateTime.ParseExact(time, "HH:mm:ss", null)
                       .AddDays(now.Date > DateTime.ParseExact(time, "HH:mm:ss", null).Date ? 1 : 0);
    }
}