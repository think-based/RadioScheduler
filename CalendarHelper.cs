//Be Naame Khoda
//FileName: CalendarHelper.cs

using System;
using System.Globalization;

public static class CalendarHelper
{
    public static string ConvertDate(DateTime date, string calendarType)
    {
        switch (calendarType)
        {
            case "Hijri":
                HijriCalendar hijri = new HijriCalendar();
                return $"{hijri.GetYear(date):0000}-{hijri.GetMonth(date):00}-{hijri.GetDayOfMonth(date):00} {date.ToString("HH:mm:ss")}";
            case "Persian":
                PersianCalendar persian = new PersianCalendar();
                return $"{persian.GetYear(date):0000}-{persian.GetMonth(date):00}-{persian.GetDayOfMonth(date):00} {date.ToString("HH:mm:ss")}";
            default: // Gregorian
                return date.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }
}