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
        SetNextPrayTime();
    }

    private void OnTimerElapsed(object sender, ElapsedEventArgs e)
    {
        // زمان شرعی بعدی را تنظیم کنید
        SetNextPrayTime();
    }

    private void SetNextPrayTime()
    {
          // تاریخ و زمان فعلی (با توجه به زمان‌زون اوقات شرعی)
        DateTime now = DateTime.Now;
        var timeZoneOffset = Settings.GetTimeZoneOffset();
         TimeZoneInfo systemTimeZone = TimeZoneInfo.Local;
        TimeZoneInfo prayTimeZone = TimeZoneInfo.CreateCustomTimeZone(
        "PrayTimeZone",
        TimeSpan.FromHours(timeZoneOffset),
        "PrayTimeZone",
        "PrayTimeZone");
         DateTime convertedNow =  TimeZoneInfo.ConvertTime(now, systemTimeZone, prayTimeZone);
        int year = convertedNow.Year;
        int month = convertedNow.Month;
        int day = convertedNow.Day;

        // استفاده از تنظیمات کاربر
        double latitude = Settings.Latitude;
        double longitude = Settings.Longitude;
        
        // محاسبه زمان‌های شرعی برای امروز
        string[] prayerTimes = _prayTime.getPrayerTimes(year, month, day, latitude, longitude, (int)timeZoneOffset);

        // زمان‌های شرعی به ترتیب: فجر، طلوع آفتاب، ظهر، عصر، غروب آفتاب، مغرب، عشاء
        DateTime fajrTime = ParseTime(prayerTimes[0], convertedNow);
        DateTime sunriseTime = ParseTime(prayerTimes[1], convertedNow);
        DateTime dhuhrTime = ParseTime(prayerTimes[2], convertedNow);
        DateTime asrTime = ParseTime(prayerTimes[3], convertedNow);
        DateTime sunsetTime = ParseTime(prayerTimes[4], convertedNow);
        DateTime maghribTime = ParseTime(prayerTimes[5], convertedNow);
        DateTime ishaTime = ParseTime(prayerTimes[6], convertedNow);

        // زمان فعلی (با توجه به زمان‌زون اوقات شرعی)
         DateTime currentTime = convertedNow;

        // بررسی زمان شرعی بعدی
        DateTime nextPrayTime = DateTime.MaxValue;
        string nextPrayName = "";

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
    