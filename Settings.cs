//Be Naame Khoda
//FileName: Settings.cs

using System;

public class Settings
{
    // Singleton instance
    private static readonly Lazy<Settings> _instance = new Lazy<Settings>(() => new Settings());
    public static Settings Instance => _instance.Value;

    // Private constructor
    private Settings()
    {
        // Automatically set angles based on latitude
        AutoSetAngles();
    }

    // Properties
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double TimeZone { get; set; }
    public PrayTime.CalculationMethod CalculationMethod { get; set; }
    public PrayTime.AsrMethods AsrMethod { get; set; }
    public PrayTime.TimeFormat TimeFormat { get; set; }
    public PrayTime.AdjustingMethod AdjustHighLats { get; set; }
    public int TimerIntervalInMinutes { get; set; }
    public bool AmplifierEnabled { get; set; }
    public string AmplifierApiUrl { get; set; }

    // Angles for Fajr and Isha
    private double _fajrAngle;
    private double _ishaAngle;

    public double FajrAngle => _fajrAngle;
    public double IshaAngle => _ishaAngle;

    // Method to automatically set angles based on latitude
    public void AutoSetAngles()
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