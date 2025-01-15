// Be Naame Khoda
// FileName: ScheduleItem.cs

using RadioScheduler.Entities;
using System;
using System.Collections.Generic;

public class ScheduleItem
{
    public int ItemId { get; set; }
    public string Name { get; set; } // New field
    public string Type { get; set; }
    public List<FilePathItem> FilePaths { get; set; }
    public string Second { get; set; }
    public string Minute { get; set; }
    public string Hour { get; set; }
    public string DayOfMonth { get; set; }
    public string Month { get; set; }
    public string DayOfWeek { get; set; }
    public string Trigger { get; set; }
    public string CalendarType { get; set; }
    public string Region { get; set; }
    public string TriggerType { get; set; }
    public int? DelayMinutes { get; set; }
    public DateTime NextOccurrence { get; set; }
    public TimeSpan TotalDuration { get; set; } // Total duration of the playlist

    public void Validate()
    {
        if (Type == "Periodic" && Trigger != null)
        {
            throw new ArgumentException("Trigger should not be set for Periodic items.");
        }

        if (Type == "NonPeriodic" && (Second != null || Minute != null || Hour != null || DayOfMonth != null || Month != null || DayOfWeek != null))
        {
            throw new ArgumentException("Periodic fields (Second, Minute, Hour, DayOfMonth, Month, DayOfWeek) should not be set for NonPeriodic items.");
        }

        if (TriggerType != "Delayed" && DelayMinutes != null)
        {
            throw new ArgumentException("DelayMinutes should only be set when TriggerType is Delayed.");
        }

        if (FilePaths == null || FilePaths.Count == 0)
        {
            throw new ArgumentException("FilePaths cannot be null or empty.");
        }

        foreach (var filePathItem in FilePaths)
        {
            filePathItem.Validate();
        }
    }
}