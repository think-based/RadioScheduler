//Be Naame Khoda
//FileName: Settings.cs

using System;

public static class Settings
{
    // Static properties
    public static double Latitude { get; set; }
    public static double Longitude { get; set; }
    public static double TimeZone { get; set; }
    public static PrayTime.CalculationMethod CalculationMethod { get; set; }
    public static PrayTime.AsrMethods AsrMethod { get; set; }
    public static PrayTime.TimeFormat TimeFormat { get; set; }
    public static PrayTime.AdjustingMethod AdjustHighLats { get; set; }
    public static int TimerIntervalInMinutes { get; set; }
    public static bool AmplifierEnabled { get; set; }
    public static string AmplifierApiUrl { get; set; }

    // Angles for Fajr and Isha
    private static double _fajrAngle;
    private static double _ishaAngle;

    public static double FajrAngle => _fajrAngle;
    public static double IshaAngle => _ishaAngle;

    // Method to automatically set angles based on latitude
    public static void AutoSetAngles()
    {
        if (Math.Abs(Latitude) <= 30) // Moderate regions
        {
            _fajrAngle = 18; // Fajr angle
            _ishaAngle = 18; // Isha angle
        }
        else // High-latitude regions
        {
            _fajrAngle = 17; // Fajr angle
            _ishaAngle = 14; // Isha angle
        }
    }
}