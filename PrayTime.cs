//Be Naame Khoda
//FileName: PrayTime.cs

using System;

public class PrayTime
{
    // سایر فیلدها و متدها

    private double adjustTime = 0.5; // 30 دقیقه تأخیر

    public string floatToTime24(double time)
    {
        if (time < 0)
            return InvalidTime;
        time = FixHour(time + adjustTime / 60); // اضافه کردن adjustTime
        int hours = (int)Math.Floor(time);
        int minutes = (int)Math.Floor((time - hours) * 60);
        int seconds = (int)Math.Floor((time - (hours + minutes / 60.0)) * 3600);
        return $"{hours:00}:{minutes:00}:{seconds:00}";
    }

    // سایر متدها
}