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

    public enum AsrMethods
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

    public static string[] timeNames = { "Fajr", "Sunrise", "Dhuhr", "Asr", "Sunset", "Maghrib", "Isha" };
    private static string InvalidTime = "----";

    private CalculationMethod calcMethod = CalculationMethod.Tehran;
    private AsrMethods asrJuristic = AsrMethods.Shafii;
    private int dhuhrMinutes = 0;
    private AdjustingMethod adjustHighLats = AdjustingMethod.MidNight;
    private TimeFormat timeFormat = TimeFormat.Time24;
    private double adjustTime = 0.5;
    private double lat;
    private double lng;
    private double timeZone;
    private double JDate;
    private int[] times;

    private int numIterations = 1;

    private double[][] methodParams;

    public PrayTime()
    {
<<<<<<< HEAD
        _methodParams = new double[8][];
        _methodParams[0] = new double[] { 16, 0, 4, 0, 14 }; // Jafari
        _methodParams[1] = new double[] { 18, 1, 0, 0, 18 }; // Karachi
        _methodParams[2] = new double[] { 15, 1, 0, 0, 15 }; // ISNA
        _methodParams[3] = new double[] { 18, 1, 0, 0, 17 }; // MWL
        _methodParams[4] = new double[] { 18.5, 1, 0, 1, 90 }; // Makkah
        _methodParams[5] = new double[] { 19.5, 1, 0, 0, 17.5 }; // Egypt
        _methodParams[6] = new double[] { 17.7, 0, 4.5, 0, 14 }; // Tehran
        _methodParams[7] = new double[] { 18, 1, 0, 0, 17 }; // Custom
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
=======
        times = new int[7];
        methodParams = new double[8][];
        methodParams[(int)CalculationMethod.Jafari] = new double[] { 16, 0, 4, 0, 14 };
        methodParams[(int)CalculationMethod.Karachi] = new double[] { 18, 1, 0, 0, 18 };
        methodParams[(int)CalculationMethod.ISNA] = new double[] { 15, 1, 0, 0, 15 };
        methodParams[(int)CalculationMethod.MWL] = new double[] { 18, 1, 0, 0, 17 };
        methodParams[(int)CalculationMethod.Makkah] = new double[] { 18.5, 1, 0, 1, 90 };
        methodParams[(int)CalculationMethod.Egypt] = new double[] { 19.5, 1, 0, 0, 17.5 };
        methodParams[(int)CalculationMethod.Tehran] = new double[] { 17.7, 0, 4.5, 0, 14 };
        methodParams[(int)CalculationMethod.Custom] = new double[] { 18, 1, 0, 0, 17 };
    }

    public string[] getPrayerTimes(int year, int month, int day, double latitude, double longitude, int timeZone)
    {
        return this.getDatePrayerTimes(year, month, day, latitude, longitude, timeZone);
    }

    public void setCalcMethod(CalculationMethod method)
    {
        this.calcMethod = method;
    }

    public void setAsrMethod(AsrMethods method)
    {
        this.asrJuristic = method;
    }

    public void setFajrAngle(double angle)
    {
        this.setCustomParams(new int[] { (int)angle, -1, -1, -1, -1 });
    }

    public void setMaghribAngle(double angle)
    {
        this.setCustomParams(new int[] { -1, 0, (int)angle, -1, -1 });
    }

    public void setIshaAngle(double angle)
    {
        this.setCustomParams(new int[] { -1, -1, -1, 0, (int)angle });
    }

    public void setDhuhrMinutes(int minutes)
    {
        this.dhuhrMinutes = minutes;
    }

    public void setMaghribMinutes(int minutes)
    {
        this.setCustomParams(new int[] { -1, 1, minutes, -1, -1 });
    }

    public void setIshaMinutes(int minutes)
    {
        this.setCustomParams(new int[] { -1, -1, -1, 1, minutes });
    }

    public void setCustomParams(int[] param)
    {
        for (int i = 0; i < 5; i++)
        {
            if (param[i] == -1)
                this.methodParams[(int)CalculationMethod.Custom][i] = methodParams[(int)calcMethod][i];
            else
                this.methodParams[(int)CalculationMethod.Custom][i] = param[i];
>>>>>>> 6902863464ec5a8ea49a4487d52440bea58382ca
        }
        this.calcMethod = CalculationMethod.Custom;
    }

    public void setHighLatsMethod(AdjustingMethod method)
    {
        this.adjustHighLats = method;
    }

    public void setTimeFormat(TimeFormat timeFormat)
    {
        this.timeFormat = timeFormat;
    }

    public string floatToTime24(double time)
    {
        if (time < 0)
            return InvalidTime;
        time = this.FixHour(time + adjustTime / 60);
        double hours = Math.Floor(time);
        double minutes = Math.Floor((time - hours) * 60);
        double secounds = Math.Floor((time - (hours + (minutes / 60))) * 3600);
        return this.twoDigitsFormat((int)hours) + ":" + this.twoDigitsFormat((int)minutes) + ":" + this.twoDigitsFormat((int)secounds);
    }

    public TimeSpan floatToTimeSpan(double time)
    {
        if (time < 0)
            return TimeSpan.Zero;
        time = this.FixHour(time + adjustTime / 60);
        double hours = Math.Floor(time);
        double minutes = Math.Floor((time - hours) * 60);
        double secounds = Math.Floor((time - (hours + (minutes / 60))) * 3600);
        return new TimeSpan((int)hours, (int)minutes, (int)secounds);
    }

    public string floatToTime12(double time, bool noSuffix)
    {
        if (time < 0)
            return InvalidTime;
        time = this.FixHour(time + adjustTime / 60);
        double hours = Math.Floor(time);
        double minutes = Math.Floor((time - hours) * 60);
        string suffix = hours >= 12 ? " pm" : " am";
        hours = (hours + 12 - 1) % 12 + 1;
        return ((int)hours) + ":" + this.twoDigitsFormat((int)minutes) + (noSuffix ? "" : suffix);
    }

    public string floatToTime12NS(double time)
    {
        return this.floatToTime12(time, true);
    }

    public string[] getDatePrayerTimes(int year, int month, int day, double latitude, double longitude,
        double timeZone)
    {
        this.lat = latitude;
        this.lng = longitude;
        this.timeZone = timeZone;
        this.JDate = this.JulianDate(year, month, day) - longitude / (15 * 24);
        return this.computeDayTimes();
    }

    public TimeSpan[] getDatePrayerTimeSpans(int year, int month, int day, double latitude, double longitude,
      double timeZone)
    {
        this.lat = latitude;
        this.lng = longitude;
        this.timeZone = timeZone;
        this.JDate = this.JulianDate(year, month, day) - longitude / (15 * 24);
        return this.computeDayTimeSpans();
    }

    public double[] sunPosition(double jd)
    {
        double D = jd - 2451545.0;
        double g = this.FixAngle(357.529 + 0.98560028 * D);
        double q = this.FixAngle(280.459 + 0.98564736 * D);
        double L = this.FixAngle(q + 1.915 * this.dsin(g) + 0.020 * this.dsin(2 * g));
        double R = 1.00014 - 0.01671 * this.dcos(g) - 0.00014 * this.dcos(2 * g);
        double e = 23.439 - 0.00000036 * D;
        double d = this.darcsin(this.dsin(e) * this.dsin(L));
        double RA = this.darctan2(this.dcos(e) * this.dsin(L), this.dcos(L)) / 15;
        RA = this.FixHour(RA);
        double EqT = q / 15 - RA;
        return new double[] { d, EqT };
    }

    public double equationOfTime(double jd)
    {
        return this.sunPosition(jd)[1];
    }

    public double sunDeclination(double jd)
    {
        return this.sunPosition(jd)[0];
    }

    public double computeMidDay(double t)
    {
        double T = this.equationOfTime(this.JDate + t);
        double Z = this.FixHour(12 - T);
        return Z;
    }

    public double computeTime(double G, double t)
    {
        double D = this.sunDeclination(this.JDate + t);
        double Z = this.computeMidDay(t);
        double V = ((double)1 / 15) * this.darccos((-this.dsin(G) - this.dsin(D) * this.dsin(this.lat)) /
                                                    (this.dcos(D) * this.dcos(this.lat)));
        return Z + (G > 90 ? -V : V);
    }

    public double computeAsr(int step, double t)
    {
        double D = this.sunDeclination(this.JDate + t);
        double G = -this.darccot(step + this.dtan(Math.Abs(this.lat - D)));
        return this.computeTime(G, t);
    }

    public double[] computeTimes(double[] times)
    {
        double[] t = this.dayPortion(times);
        double Fajr = this.computeTime(180 - this.methodParams[(int)calcMethod][0], t[0]);
        double Sunrise = this.computeTime(180 - 0.833, t[1]);
        double Dhuhr = this.computeMidDay(t[2]);
        double Asr = this.computeAsr(1 + (int)asrJuristic, t[3]);
        double Sunset = this.computeTime(0.833, t[4]);
        double Maghrib = this.computeTime(this.methodParams[(int)calcMethod][2], t[5]);
        double Isha = this.computeTime(this.methodParams[(int)calcMethod][4], t[6]);
        return new double[] { Fajr, Sunrise, Dhuhr, Asr, Sunset, Maghrib, Isha };
    }

    public double[] adjustHighLatTimes(double[] times)
    {
        double nightTime = this.GetTimeDifference(times[4], times[1]);

        double FajrDiff = this.nightPortion(this.methodParams[(int)calcMethod][0]) * nightTime;
        if (this.GetTimeDifference(times[0], times[1]) > FajrDiff)
            times[0] = times[1] - FajrDiff;

        double IshaAngle = (this.methodParams[(int)calcMethod][3] == 0)
            ? this.methodParams[(int)calcMethod][4]
            : 18;
        double IshaDiff = this.nightPortion(IshaAngle) * nightTime;
        if (this.GetTimeDifference(times[4], times[6]) > IshaDiff)
            times[6] = times[4] + IshaDiff;

        double MaghribAngle = (methodParams[(int)calcMethod][1] == 0) ? this.methodParams[(int)calcMethod][2] : 4;
        double MaghribDiff = this.nightPortion(MaghribAngle) * nightTime;
        if (this.GetTimeDifference(times[4], times[5]) > MaghribDiff)
            times[5] = times[4] + MaghribDiff;

        return times;
    }

    public double nightPortion(double angle)
    {
<<<<<<< HEAD
        switch (_adjustHighLats)
        {
            case AdjustingMethod.AngleBased:
                return angle / 60.0;
            case AdjustingMethod.MidNight:
                return 0.5;
            case AdjustingMethod.OneSeventh:
                return 1.0 / 7.0;
            default:
                return 0;
        }
=======
        double val = 0;
        if (this.adjustHighLats == AdjustingMethod.AngleBased)
            val = 1.0 / 60.0 * angle;
        if (this.adjustHighLats == AdjustingMethod.MidNight)
            val = 1.0 / 2.0;
        if (this.adjustHighLats == AdjustingMethod.OneSeventh)
            val = 1.0 / 7.0;
        return val;
>>>>>>> 6902863464ec5a8ea49a4487d52440bea58382ca
    }

    public double[] dayPortion(double[] times)
    {
        for (int i = 0; i < times.Length; i++)
        {
            times[i] /= 24;
        }
        return times;
    }

    public string[] computeDayTimes()
    {
        double[] times = { 5, 6, 12, 13, 18, 18, 18 };
        for (int i = 0; i < this.numIterations; i++)
        {
            times = this.computeTimes(times);
        }
        times = this.adjustTimes(times);
        return this.adjustTimesFormat(times);
    }

    public TimeSpan[] computeDayTimeSpans()
    {
        double[] times = { 5, 6, 12, 13, 18, 18, 18 };
        for (int i = 0; i < this.numIterations; i++)
        {
            times = this.computeTimes(times);
        }
        times = this.adjustTimes(times);
        return this.adjustTimeSpansFormat(times);
    }

    public double[] adjustTimes(double[] times)
    {
        for (int i = 0; i < 7; i++)
        {
            times[i] += this.timeZone - this.lng / 15;
        }
        times[2] += this.dhuhrMinutes / 60;
        if (this.methodParams[(int)calcMethod][1] == 1)
            times[5] = times[4] + this.methodParams[(int)calcMethod][2] / 60.0;
        if (this.methodParams[(int)calcMethod][3] == 1)
            times[6] = times[5] + this.methodParams[(int)calcMethod][4] / 60.0;
        if (this.adjustHighLats != AdjustingMethod.None)
        {
            times = this.adjustHighLatTimes(times);
        }
        return times;
    }

    public string[] adjustTimesFormat(double[] times)
    {
        string[] formatted = new string[times.Length];
        if (this.timeFormat == TimeFormat.Floating)
        {
            for (int i = 0; i < times.Length; ++i)
            {
                formatted[i] = times[i] + "";
            }
            return formatted;
        }
        for (int i = 0; i < 7; i++)
        {
            if (this.timeFormat == TimeFormat.Time12)
                formatted[i] = this.floatToTime12(times[i], true);
            else if (this.timeFormat == TimeFormat.Time12NS)
                formatted[i] = this.floatToTime12NS(times[i]);
            else
                formatted[i] = this.floatToTime24(times[i]);
        }
        return formatted;
    }

    public TimeSpan[] adjustTimeSpansFormat(double[] times)
    {
        TimeSpan[] formatted = new TimeSpan[times.Length];
        for (int i = 0; i < 7; i++)
        {
            formatted[i] = this.floatToTimeSpan(times[i]);
        }
        return formatted;
    }

    public double GetTimeDifference(double c1, double c2)
    {
        double diff = this.FixHour(c2 - c1);
        return diff;
    }

    public string twoDigitsFormat(int num)
    {
        return (num < 10) ? "0" + num : num + "";
    }

    public double JulianDate(int year, int month, int day)
    {
        if (month <= 2)
        {
            year -= 1;
            month += 12;
        }
        double A = (double)Math.Floor(year / 100.0);
        double B = 2 - A + Math.Floor(A / 4);
        double JD = Math.Floor(365.25 * (year + 4716)) + Math.Floor(30.6001 * (month + 1)) + day + B - 1524.5;
        return JD;
    }

    public bool UseDayLightaving(int year, int month, int day)
    {
        return TimeZone.CurrentTimeZone.IsDaylightSavingTime(new DateTime(year, month, day));
    }

    public double dsin(double d)
    {
        return Math.Sin(this.DegreeToRadian(d));
    }

    public double dcos(double d)
    {
        return Math.Cos(this.DegreeToRadian(d));
    }

    public double dtan(double d)
    {
        return Math.Tan(this.DegreeToRadian(d));
    }

    public double darcsin(double x)
    {
        return this.RadianToDegree(Math.Asin(x));
    }

    public double darccos(double x)
    {
        return this.RadianToDegree(Math.Acos(x));
    }

    public double darctan(double x)
    {
        return this.RadianToDegree(Math.Atan(x));
    }

    public double darctan2(double y, double x)
    {
        return this.RadianToDegree(Math.Atan2(y, x));
    }

    public double darccot(double x)
    {
        return this.RadianToDegree(Math.Atan(1 / x));
    }

    public double RadianToDegree(double radian)
    {
        return (radian * 180.0) / Math.PI;
    }

    public double DegreeToRadian(double degree)
    {
        return (degree * Math.PI) / 180.0;
    }

    public double FixAngle(double angel)
    {
        angel = angel - 360.0 * (Math.Floor(angel / 360.0));
        angel = angel < 0 ? angel + 360.0 : angel;
        return angel;
    }

    public double FixHour(double hour)
    {
        hour = hour - 24.0 * (Math.Floor(hour / 24.0));
        hour = hour < 0 ? hour + 24.0 : hour;
        return hour;
    }
}