//Be Naame Khoda
//FileName: CalendarHelper.cs

using System;
using System.Globalization;

public static class CalendarHelper
{
    // آرایه نام ماه‌های شمسی
    private static readonly string[] PersianMonthNames = new[]
    {
        "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور",
        "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند"
    };

    // آرایه نام ماه‌های قمری
    private static readonly string[] HijriMonthNames = new[]
    {
        "محرم", "صفر", "ربیع‌الاول", "ربیع‌الثانی", "جمادی‌الاول", "جمادی‌الثانی",
        "رجب", "شعبان", "رمضان", "شوال", "ذی‌القعده", "ذی‌الحجه"
    };

    /// <summary>
    /// تبدیل تاریخ به تقویم مورد نظر و برگرداندن به صورت رشته
    /// </summary>
    /// <param name="date">تاریخ ورودی</param>
    /// <param name="calendarType">نوع تقویم (Gregorian, Persian, Hijri)</param>
    /// <returns>تاریخ تبدیل‌شده به صورت رشته</returns>
    public static string ConvertDateToString(DateTime date, string calendarType)
    {
        switch (calendarType.ToLower())
        {
            case "hijri":
                HijriCalendar hijri = new HijriCalendar();
                return $"{hijri.GetYear(date):0000}-{hijri.GetMonth(date):00}-{hijri.GetDayOfMonth(date):00} {date.ToString("HH:mm:ss")}";
            case "persian":
                PersianCalendar persian = new PersianCalendar();
                return $"{persian.GetYear(date):0000}-{persian.GetMonth(date):00}-{persian.GetDayOfMonth(date):00} {date.ToString("HH:mm:ss")}";
            default: // Gregorian
                return date.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }

    /// <summary>
    /// تبدیل تاریخ به تقویم مورد نظر و برگرداندن به صورت DateTime
    /// </summary>
    /// <param name="date">تاریخ ورودی</param>
    /// <param name="calendarType">نوع تقویم (Gregorian, Persian, Hijri)</param>
    /// <returns>تاریخ تبدیل‌شده به صورت DateTime</returns>
    public static DateTime ConvertDateToDateTime(DateTime date, string calendarType)
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

    /// <summary>
    /// تبدیل تاریخ به تقویم مورد نظر (متد اصلی)
    /// </summary>
    /// <param name="date">تاریخ ورودی</param>
    /// <param name="calendarType">نوع تقویم (Gregorian, Persian, Hijri)</param>
    /// <returns>تاریخ تبدیل‌شده به صورت رشته</returns>
    public static string ConvertDate(DateTime date, string calendarType)
    {
        return ConvertDateToString(date, calendarType);
    }

    /// <summary>
    /// بررسی معتبر بودن تاریخ در تقویم مورد نظر
    /// </summary>
    /// <param name="year">سال</param>
    /// <param name="month">ماه</param>
    /// <param name="day">روز</param>
    /// <param name="calendarType">نوع تقویم (Gregorian, Persian, Hijri)</param>
    /// <returns>true اگر تاریخ معتبر باشد، در غیر این صورت false</returns>
    public static bool IsValidDate(int year, int month, int day, string calendarType)
    {
        try
        {
            switch (calendarType.ToLower())
            {
                case "hijri":
                    HijriCalendar hijri = new HijriCalendar();
                    hijri.ToDateTime(year, month, day, 0, 0, 0, 0);
                    return true;
                case "persian":
                    PersianCalendar persian = new PersianCalendar();
                    persian.ToDateTime(year, month, day, 0, 0, 0, 0);
                    return true;
                default: // Gregorian
                    new DateTime(year, month, day);
                    return true;
            }
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// دریافت نام ماه بر اساس نوع تقویم
    /// </summary>
    /// <param name="month">عدد ماه (1 تا 12)</param>
    /// <param name="calendarType">نوع تقویم (Gregorian, Persian, Hijri)</param>
    /// <returns>نام ماه</returns>
    public static string GetMonthName(int month, string calendarType)
    {
        if (month < 1 || month > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(month), "ماه باید بین ۱ تا ۱۲ باشد.");
        }

        switch (calendarType.ToLower())
        {
            case "hijri":
                return HijriMonthNames[month - 1]; // آرایه از ۰ شروع می‌شود
            case "persian":
                return PersianMonthNames[month - 1]; // آرایه از ۰ شروع می‌شود
            default: // Gregorian
                return new DateTime(2023, month, 1).ToString("MMMM", CultureInfo.InvariantCulture);
        }
    }
}