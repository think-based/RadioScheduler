//Be Naame Khoda
//FileName: Settings.cs

using System;

public static class Settings
{
    // طول و عرض جغرافیایی
    public static double Latitude { get; set; } = 35.6892; // عرض جغرافیایی پیش‌فرض (تهران)
    public static double Longitude { get; set; } = 51.3890; // طول جغرافیایی پیش‌فرض (تهران)

    // منطقه زمانی
    public static double TimeZone { get; set; } = 3.5; // منطقه زمانی پیش‌فرض (تهران)

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
}