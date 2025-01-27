      //Be Naame Khoda
//FileName: Settings.cs

using System;

public static class Settings
{
    // Static properties
    public static double Latitude { get; set; }
    public static double Longitude { get; set; }
    public static string TimeZoneId { get; set; }
    public static double TimeZoneOffset { get; set; }
    public static PrayTime.CalculationMethod CalculationMethod { get; set; }
    public static PrayTime.AsrMethods AsrMethod { get; set; }
    public static PrayTime.TimeFormat TimeFormat { get; set; }
    public static PrayTime.AdjustingMethod AdjustHighLats { get; set; }
    public static string Region { get; set; } // Added Region property
    public static int TimerIntervalInMinutes { get; set; }
    public static bool AmplifierEnabled { get; set; }
    public static string AmplifierApiUrl { get; set; }
    private static double _fajrAngle;
    private static double _ishaAngle;


    public static double FajrAngle => _fajrAngle;
    public static double IshaAngle => _ishaAngle;

    public static int TimedDelay = 1;
    public static void AutoSetAngles()
    {
        if (Math.Abs(Latitude) <= 30)
        {
            _fajrAngle = 18;
            _ishaAngle = 18;
        }
        else
        {
            _fajrAngle = 17;
            _ishaAngle = 14;
        }
    }
       public static double GetTimeZoneOffset()
    {
       return TimeZoneOffset;
    }
}
    