      //Be Naame Khoda
//FileName: AppSettings.cs

public class AppSettings
{
    public ApplicationSettings Application { get; set; }
}

public class ApplicationSettings
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string TimeZoneId { get; set; }
    public double TimeZoneOffset { get; set; }
    public string CalculationMethod { get; set; }
    public string AsrMethod { get; set; }
    public string TimeFormat { get; set; }
    public string AdjustHighLats { get; set; }
    public int TimerIntervalInMinutes { get; set; }
    public bool AmplifierEnabled { get; set; }
    public string AmplifierApiUrl { get; set; }
}
    