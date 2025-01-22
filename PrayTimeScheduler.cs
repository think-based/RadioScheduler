//Be Naame Khoda
//FileName: PrayTimeScheduler.cs

using System;
using System.Timers;
using static ActiveTriggers;
using static Enums;

public class PrayTimeScheduler
{
    private Timer _timer;
    private PrayTime _prayTime;
    public PrayTimeScheduler()
    {
        _prayTime = new PrayTime();

        // تنظیم تایمر پویا
        _timer = new Timer();
        _timer.Elapsed += OnTimerElapsed;
        _timer.AutoReset = false; // تایمر فقط یک بار فعال می‌شود

        // اولین بار زمان‌های شرعی را محاسبه و تنظیم کنید
        SetPrayTimes();
    }

    private void OnTimerElapsed(object sender, ElapsedEventArgs e)
    {
        // زمان شرعی بعدی را تنظیم کنید
        SetPrayTimes();
    }

    private void SetPrayTimes()
    {
        DateTime now = DateTime.Now;
        int year = now.Year;
        int month = now.Month;
        int day = now.Day;



        // محاسبه زمان‌های شرعی برای امروز
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

        // زمان‌های شرعی به ترتیب: فجر، طلوع آفتاب، ظهر، عصر، غروب آفتاب، مغرب، عشاء
        DateTime fajrTime = ParseTime(prayerTimes[0]);
        DateTime sunriseTime = ParseTime(prayerTimes[1]);
        DateTime dhuhrTime = ParseTime(prayerTimes[2]);
        DateTime asrTime = ParseTime(prayerTimes[3]);
        DateTime sunsetTime = ParseTime(prayerTimes[4]);
        DateTime maghribTime = ParseTime(prayerTimes[5]);
        DateTime ishaTime = ParseTime(prayerTimes[6]);

        SetTrigger("azan_sobh", fajrTime);
        SetTrigger("azan_zohr", dhuhrTime);
        SetTrigger("azan_maghreb", maghribTime);

        // زمان فعلی (با توجه به زمان‌زون اوقات شرعی)
        DateTime currentTime = now;

        // بررسی زمان شرعی بعدی
        DateTime nextPrayTime = DateTime.MaxValue;
        string nextPrayName = "";
        string nextTriggerName = "";

        // بررسی فجر
        if (currentTime < fajrTime && fajrTime < nextPrayTime)
        {
            nextPrayTime = fajrTime;
            nextPrayName = "Fajr";
        }

        // بررسی طلوع آفتاب
        if (currentTime < sunriseTime && sunriseTime < nextPrayTime)
        {
            nextPrayTime = sunriseTime;
            nextPrayName = "Sunrise";
        }

        // بررسی ظهر
        if (currentTime < dhuhrTime && dhuhrTime < nextPrayTime)
        {
            nextPrayTime = dhuhrTime;
            nextPrayName = "Dhuhr";
        }

        // بررسی عصر
        if (currentTime < asrTime && asrTime < nextPrayTime)
        {
            nextPrayTime = asrTime;
            nextPrayName = "Asr";
        }

        // بررسی غروب آفتاب
        if (currentTime < sunsetTime && sunsetTime < nextPrayTime)
        {
            nextPrayTime = sunsetTime;
            nextPrayName = "Sunset";
        }

        // بررسی مغرب
        if (currentTime < maghribTime && maghribTime < nextPrayTime)
        {
            nextPrayTime = maghribTime;
            nextPrayName = "Maghrib";
        }

        // بررسی عشاء
        if (currentTime < ishaTime && ishaTime < nextPrayTime)
        {
            nextPrayTime = ishaTime;
            nextPrayName = "Isha";
        }

        // اگر زمان شرعی بعدی پیدا نشد، برای فردا فجر تنظیم کنید
        if (nextPrayTime == DateTime.MaxValue)
        {
            nextPrayTime = fajrTime.AddDays(1);
            nextPrayName = "Fajr";
        }

        // تنظیم Trigger
        SetTrigger("NextPrayTime", nextPrayTime);

        // تنظیم تایمر برای فعال شدن در زمان شرعی بعدی
        double timeUntilNextPray = (nextPrayTime - currentTime).TotalMilliseconds;
        _timer.Interval = timeUntilNextPray;
        _timer.Start();

        // چاپ اطلاعات برای بررسی
        Logger.LogMessage($"Next prayer: {nextPrayName} at {nextPrayTime}");
    }

    private void SetTrigger(string eventName, DateTime triggerTime)
    {
        ActiveTriggers.AddTrigger(eventName, triggerTime, TriggerSource.Systematic);
        Logger.LogMessage($"CurrentTrigger set to: {eventName} at {triggerTime}");
    }

    private DateTime ParseTime(string time)
    {
        // تبدیل زمان از رشته به DateTime
        DateTime now = ConvertToPrayTimeZone(DateTime.Now);
        try
        {
            DateTime parsedTime = DateTime.ParseExact(time, "HH:mm:ss", null);
            if (parsedTime < now)
            {
                // اگر زمان گذشته باشد، برای فردا تنظیم کنید
                parsedTime = parsedTime.AddDays(1);
            }
            return parsedTime;
        }
        catch (FormatException)
        {
            // اگر فرمت زمان نادرست باشد، زمان پیش‌فرض برگردانید
            Logger.LogMessage($"Error parsing time: {time}");
            return now.AddHours(1); // زمان پیش‌فرض: ۱ ساعت بعد
        }
    }

    private DateTime ConvertToPrayTimeZone(DateTime dateTime)
    {
        // تبدیل زمان سیستم به زمان‌زون اوقات شرعی (UTC+3.5)
        TimeZoneInfo systemTimeZone = TimeZoneInfo.Local;
        TimeZoneInfo prayTimeZone = TimeZoneInfo.CreateCustomTimeZone(
            "PrayTimeZone",
            TimeSpan.FromHours(Settings.TimeZone),
            "PrayTimeZone",
            "PrayTimeZone");

        return TimeZoneInfo.ConvertTime(dateTime, systemTimeZone, prayTimeZone);
    }
}