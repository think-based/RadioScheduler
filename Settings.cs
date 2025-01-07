//Be Naame Khoda
//FileName: Settings.cs

using System;

public static class Settings
{
    // طول و عرض جغرافیایی (شیراز)
    public static double Latitude { get; set; } = 29.5916; // عرض جغرافیایی شیراز
    public static double Longitude { get; set; } = 52.5837; // طول جغرافیایی شیراز

    // منطقه زمانی (شیراز)
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
    private static double _fajrAngle = 18; // زاویه پیش‌فرض فجر
    private static double _ishaAngle = 18; // زاویه پیش‌فرض عشاء

    // متد تنظیم زاویه فجر
    public static void setFajrAngle(double angle)
    {
        _fajrAngle = angle;
    }

    // متد تنظیم زاویه عشاء
    public static void setIshaAngle(double angle)
    {
        _ishaAngle = angle;
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