using System;
using System.Globalization;

public static class CalendarHelper
{
    public static DateTime ConvertDate(DateTime date, string calendarType)
    {
        switch (calendarType)
        {
            case "Hijri":
                HijriCalendar hijri = new HijriCalendar();
                return new DateTime(hijri.GetYear(date), hijri.GetMonth(date), hijri.GetDayOfMonth(date));
            case "Persian":
                PersianCalendar persian = new PersianCalendar();
                return new DateTime(persian.GetYear(date), persian.GetMonth(date), persian.GetDayOfMonth(date));
            default: // Gregorian
                return date;
        }
    }
}