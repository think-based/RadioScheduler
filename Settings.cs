//Be Naame Khoda
//FileName: Settings.cs

using System;

public static class Settings
{
    // طول و عرض جغرافیایی
    public static double Latitude { get; set; } = 29.5916; // عرض جغرافیایی شیراز
    public static double Longitude { get; set; } = 52.5837; // طول جغرافیایی شیراز

    // منطقه زمانی
    public static double TimeZone { get; set; } = 3.5; // منطقه زمانی شیراز

    // روش محاسبه
    public static PrayTime.CalculationMethod CalculationMethod { get; set; } = PrayTime.CalculationMethod.Tehran;

    // روش محاسبه عصر
    public static PrayTime.AsrMethods AsrMethod { get; set; } = PrayTime.AsrMethods.Shafii;

    // تنظیمات مربوط به زمان‌های شرعی
    public static int DhuhrMinutes { get; set; } = 0; // دقیقه‌های اضافی برای ظهر
    public static PrayTime.AdjustingMethod AdjustHighLats { get; set; } = PrayTime.AdjustingMethod.MidNight;
    public static PrayTime.TimeFormat TimeFormat { get; set; } = PrayTime.TimeFormat.Time24;

    // تنظیمات مربوط به تایمر
    public static int TimerInterval { get; set; } = 24 * 60 * 60 * 1000; // تایمر پیش‌فرض (24 ساعت)

    // زاویه‌های فجر و عشاء
    private static double _fajrAngle;
    private static double _ishaAngle;

    // متد تنظیم خودکار زاویه‌ها بر اساس عرض جغرافیایی
    public static void AutoSetAngles()
    {
        // قاعده‌ی تجربی برای تنظیم زاویه‌ها
        if (Math.Abs(Latitude) <= 30) // مناطق معتدل
        {
            _fajrAngle = 18; // زاویه فجر
            _ishaAngle = 18; // زاویه عشاء
        }
        else // مناطق با عرض جغرافیایی بالا
        {
            _fajrAngle = 17; // زاویه فجر
            _ishaAngle = 14; // زاویه عشاء
        }
    }

    // متد دریافت زاویه فجر
    public static double getFajrAngle()
    {
        return _fajrAngle;
    }

    // متد دریافت زاویه عشاء
    public static double getIshaAngle()
    {
        return _ishaAngle;
    }
}