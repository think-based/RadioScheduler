      // Be Naame Khoda
// FileName: Enums.cs

using System;

public static class Enums
{
    public enum ScheduleType
    {
        Periodic,
        NonPeriodic
    }

    public enum CalendarTypes
    {
        Gregorian,
        Hijri,
        Persian
    }

    public enum TriggerTypes
    {
        Immediate,
        Delayed,
        Timed
    }

    public enum ScheduleStatus
    {
        Played,
        TimeWaiting,
        Playing,
        EventWaiting,
        Stoped,
        Canceled
    }

    // Add a new enum for Priority levels
    public enum Priority
    {
        Low = 1,
        Medium = 2,
        High = 3
    }

    public static T ParseEnum<T>(string value, string enumName) where T : struct, Enum
    {
        if (string.IsNullOrEmpty(value))
        {
            throw new ArgumentException($"Value for '{enumName}' is empty or null.");
        }
        try
        {
            return (T)Enum.Parse(typeof(T), value, true);
        }
        catch (ArgumentException)
        {
            throw new ArgumentException($"Invalid value '{value}' for enum '{enumName}'.");

        }

    }
}
    