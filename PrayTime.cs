//Be Naame Khoda
//FileName: PrayTime.cs

using System;

public class PrayTime
{
    public enum CalculationMethod
    {
        Jafari,
        Karachi,
        ISNA,
        MWL,
        Makkah,
        Egypt,
        Custom,
        Tehran
    }

    public enum AsrMethod
    {
        Shafii,
        Hanafi
    }

    public enum AdjustingMethod
    {
        None,
        MidNight,
        OneSeventh,
        AngleBased
    }

    public enum TimeFormat
    {
        Time24,
        Time12,
        Time12NS,
        Floating
    }

    private static readonly string[] TimeNames = { "Fajr", "Sunrise", "Dhuhr", "Asr", "Sunset", "Maghrib", "Isha" };
    private const string InvalidTime = "----";

    private CalculationMethod _calcMethod = CalculationMethod.Tehran;
    private AsrMethod _asrMethod = AsrMethod.Shafii;
    private int _dhuhrMinutes;
    private AdjustingMethod _adjustHighLats = AdjustingMethod.MidNight;
    private TimeFormat _timeFormat = TimeFormat.Time24;
    private double _adjustTime = 0.5;
    private double _lat;
    private double _lng;
    private double _timeZone;
    private double _jDate;

    private readonly double[][] _methodParams;

    public PrayTime()
    {
        _methodParams = new[]
        {
            new[] { 16, 0, 4, 0, 14 }, // Jafari
            new[] { 18, 1, 0, 0, 18 }, // Karachi
            new[] { 15, 1, 0, 0, 15 }, // ISNA
            new[] { 18, 1, 0, 0, 17 }, // MWL
            new[] { 18.5, 1, 0, 1, 90 }, // Makkah
            new[] { 19.5, 1, 0, 0, 17.5 }, // Egypt
            new[] { 17.7, 0, 4.5, 0, 14 }, // Tehran
            new[] { 18, 1, 0, 0, 17 } // Custom
        };
    }

    public string[] GetPrayerTimes(int year, int month, int day, double latitude, double longitude, double timeZone)
    {
        SetLocation(latitude, longitude, timeZone);
        SetJulianDate(year, month, day);
        return ComputeDayTimes();
    }

    public TimeSpan[] GetPrayerTimeSpans(int year, int month, int day, double latitude, double longitude, double timeZone)
    {
        SetLocation(latitude, longitude, timeZone);
        SetJulianDate(year, month, day);
        return ComputeDayTimeSpans();
    }

    private void SetLocation(double latitude, double longitude, double timeZone)
    {
        _lat = latitude;
        _lng = longitude;
        _timeZone = timeZone;
    }

    private void SetJulianDate(int year, int month, int day)
    {
        _jDate = JulianDate(year, month, day) - _lng / (15 * 24);
    }

    private string[] ComputeDayTimes()
    {
        double[] times = { 5, 6, 12, 13, 18, 18, 18 };
        times = ComputeTimes(times);
        times = AdjustTimes(times);
        return FormatTimes(times);
    }

    private TimeSpan[] ComputeDayTimeSpans()
    {
        double[] times = { 5, 6, 12, 13, 18, 18, 18 };
        times = ComputeTimes(times);
        times = AdjustTimes(times);
        return FormatTimeSpans(times);
    }

    private double[] ComputeTimes(double[] times)
    {
        double[] t = DayPortion(times);
        double fajr = ComputeTime(180 - _methodParams[(int)_calcMethod][0], t[0]);
        double sunrise = ComputeTime(180 - 0.833, t[1]);
        double dhuhr = ComputeMidDay(t[2]);
        double asr = ComputeAsr(1 + (int)_asrMethod, t[3]);
        double sunset = ComputeTime(0.833, t[4]);
        double maghrib = ComputeTime(_methodParams[(int)_calcMethod][2], t[5]);
        double isha = ComputeTime(_methodParams[(int)_calcMethod][4], t[6]);
        return new[] { fajr, sunrise, dhuhr, asr, sunset, maghrib, isha };
    }

    private double[] AdjustTimes(double[] times)
    {
        for (int i = 0; i < 7; i++)
        {
            times[i] += _timeZone - _lng / 15;
        }
        times[2] += _dhuhrMinutes / 60;

        if (_methodParams[(int)_calcMethod][1] == 1)
            times[5] = times[4] + _methodParams[(int)_calcMethod][2] / 60.0;

        if (_methodParams[(int)_calcMethod][3] == 1)
            times[6] = times[5] + _methodParams[(int)_calcMethod][4] / 60.0;

        if (_adjustHighLats != AdjustingMethod.None)
        {
            times = AdjustHighLatTimes(times);
        }

        return times;
    }

    private string[] FormatTimes(double[] times)
    {
        string[] formatted = new string[times.Length];
        for (int i = 0; i < 7; i++)
        {
            formatted[i] = FormatTime(times[i]);
        }
        return formatted;
    }

    private TimeSpan[] FormatTimeSpans(double[] times)
    {
        TimeSpan[] formatted = new TimeSpan[times.Length];
        for (int i = 0; i < 7; i++)
        {
            formatted[i] = FloatToTimeSpan(times[i]);
        }
        return formatted;
    }

    private string FormatTime(double time)
    {
        if (_timeFormat == TimeFormat.Time12)
            return FloatToTime12(time, false);
        if (_timeFormat == TimeFormat.Time12NS)
            return FloatToTime12(time, true);
        return FloatToTime24(time);
    }

    private string FloatToTime24(double time)
    {
        if (time < 0)
            return InvalidTime;
        time = FixHour(time + _adjustTime / 60);
        int hours = (int)Math.Floor(time);
        int minutes = (int)Math.Floor((time - hours) * 60);
        int seconds = (int)Math.Floor((time - (hours + minutes / 60.0)) * 3600);
        return $"{hours:00}:{minutes:00}:{seconds:00}";
    }

    private TimeSpan FloatToTimeSpan(double time)
    {
        if (time < 0)
            return TimeSpan.Zero;
        time = FixHour(time + _adjustTime / 60);
        int hours = (int)Math.Floor(time);
        int minutes = (int)Math.Floor((time - hours) * 60);
        int seconds = (int)Math.Floor((time - (hours + minutes / 60.0)) * 3600);
        return new TimeSpan(hours, minutes, seconds);
    }

    private string FloatToTime12(double time, bool noSuffix)
    {
        if (time < 0)
            return InvalidTime;
        time = FixHour(time + _adjustTime / 60);
        int hours = (int)Math.Floor(time);
        int minutes = (int)Math.Floor((time - hours) * 60);
        string suffix = hours >= 12 ? " pm" : " am";
        hours = (hours + 12 - 1) % 12 + 1;
        return $"{hours}:{minutes:00}{(noSuffix ? "" : suffix)}";
    }

    private double JulianDate(int year, int month, int day)
    {
        if (month <= 2)
        {
            year -= 1;
            month += 12;
        }
        double A = Math.Floor(year / 100.0);
        double B = 2 - A + Math.Floor(A / 4);
        return Math.Floor(365.25 * (year + 4716)) + Math.Floor(30.6001 * (month + 1)) + day + B - 1524.5;
    }

    private double ComputeMidDay(double t)
    {
        double T = EquationOfTime(_jDate + t);
        return FixHour(12 - T);
    }

    private double ComputeTime(double angle, double t)
    {
        double D = SunDeclination(_jDate + t);
        double Z = ComputeMidDay(t);
        double V = (1.0 / 15) * Darccos((-Dsin(angle) - Dsin(D) * Dsin(_lat)) / (Dcos(D) * Dcos(_lat)));
        return Z + (angle > 90 ? -V : V);
    }

    private double ComputeAsr(int step, double t)
    {
        double D = SunDeclination(_jDate + t);
        double G = -Darccot(step + Dtan(Math.Abs(_lat - D)));
        return ComputeTime(G, t);
    }

    private double[] AdjustHighLatTimes(double[] times)
    {
        double nightTime = GetTimeDifference(times[4], times[1]);
        double fajrDiff = NightPortion(_methodParams[(int)_calcMethod][0]) * nightTime;
        if (GetTimeDifference(times[0], times[1]) > fajrDiff)
            times[0] = times[1] - fajrDiff;

        double ishaAngle = _methodParams[(int)_calcMethod][3] == 0 ? _methodParams[(int)_calcMethod][4] : 18;
        double ishaDiff = NightPortion(ishaAngle) * nightTime;
        if (GetTimeDifference(times[4], times[6]) > ishaDiff)
            times[6] = times[4] + ishaDiff;

        double maghribAngle = _methodParams[(int)_calcMethod][1] == 0 ? _methodParams[(int)_calcMethod][2] : 4;
        double maghribDiff = NightPortion(maghribAngle) * nightTime;
        if (GetTimeDifference(times[4], times[5]) > maghribDiff)
            times[5] = times[4] + maghribDiff;

        return times;
    }

    private double NightPortion(double angle)
    {
        return _adjustHighLats switch
        {
            AdjustingMethod.AngleBased => angle / 60.0,
            AdjustingMethod.MidNight => 0.5,
            AdjustingMethod.OneSeventh => 1.0 / 7.0,
            _ => 0
        };
    }

    private double[] DayPortion(double[] times)
    {
        for (int i = 0; i < times.Length; i++)
        {
            times[i] /= 24;
        }
        return times;
    }

    private double GetTimeDifference(double time1, double time2)
    {
        return FixHour(time2 - time1);
    }

    private double FixHour(double hour)
    {
        hour = hour - 24 * Math.Floor(hour / 24);
        return hour < 0 ? hour + 24 : hour;
    }

    private double FixAngle(double angle)
    {
        angle = angle - 360 * Math.Floor(angle / 360);
        return angle < 0 ? angle + 360 : angle;
    }

    private double Dsin(double d) => Math.Sin(DegreeToRadian(d));
    private double Dcos(double d) => Math.Cos(DegreeToRadian(d));
    private double Dtan(double d) => Math.Tan(DegreeToRadian(d));
    private double Darcsin(double x) => RadianToDegree(Math.Asin(x));
    private double Darccos(double x) => RadianToDegree(Math.Acos(x));
    private double Darctan(double x) => RadianToDegree(Math.Atan(x));
    private double Darctan2(double y, double x) => RadianToDegree(Math.Atan2(y, x));
    private double Darccot(double x) => RadianToDegree(Math.Atan(1 / x));
    private double RadianToDegree(double radian) => radian * 180 / Math.PI;
    private double DegreeToRadian(double degree) => degree * Math.PI / 180;
}