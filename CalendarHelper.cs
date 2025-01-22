      //Be Naame Khoda
//FileName: CalendarHelper.cs

using System;
using System.Globalization;

public static class CalendarHelper
{
    public static DateTime ConvertDate(DateTime date, string calendarType)
    {
        switch (calendarType.ToLower())
        {
            case "hijri":
                HijriCalendar hijri = new HijriCalendar();
                return new DateTime(hijri.GetYear(date), hijri.GetMonth(date), hijri.GetDayOfMonth(date), date.Hour, date.Minute, date.Second);
            case "persian":
                PersianCalendar persian = new PersianCalendar();
                return new DateTime(persian.GetYear(date), persian.GetMonth(date), persian.GetDayOfMonth(date), date.Hour, date.Minute, date.Second);
            default: // Gregorian
                return date;
        }
    }

    public static int GetDayOfWeek(DateTime date, string region)
    {
        try
        {
            var cultureInfo = new CultureInfo(region);
            DayOfWeek firstDayOfWeek = cultureInfo.DateTimeFormat.FirstDayOfWeek;
            int dayOfWeek = ((int)date.DayOfWeek - (int)firstDayOfWeek + 7) % 7;
            return dayOfWeek;
        }
        catch (CultureNotFoundException)
        {
            throw new ArgumentException($"Unsupported region: {region}");
        }
    }

    public static DateTime ConvertToLocalTimeZone(DateTime dateTime, double timeZoneOffset, string region)
    {
        // Convert system time to the local time zone based on settings
         TimeZoneInfo systemTimeZone = TimeZoneInfo.Local;
        TimeZoneInfo targetTimeZone = TimeZoneInfo.CreateCustomTimeZone(
            "PrayTimeZone",
            TimeSpan.FromHours(timeZoneOffset),
            "PrayTimeZone",
            "PrayTimeZone");

        try
        {
            return TimeZoneInfo.ConvertTime(dateTime, systemTimeZone, targetTimeZone);
        }
        catch (TimeZoneNotFoundException)
        {
             Logger.LogMessage($"Invalid time zone with value {timeZoneOffset}.");
            return dateTime;
        }
    }
}
    