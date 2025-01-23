      // Be Naame Khoda
// FileName: ScheduleItem.cs

using RadioScheduler.Entities;
using System;
using System.Collections.Generic;
using static Enums;

public class ScheduleItem
{
    public int ItemId { get; set; }
    public string Name { get; set; }
    public ScheduleType Type { get; set; }
    public List<FilePathItem> FilePaths { get; set; }
    public string Second { get; set; }
    public string Minute { get; set; }
    public string Hour { get; set; }
    public string DayOfMonth { get; set; }
    public string Month { get; set; }
    public string DayOfWeek { get; set; }
    public string Trigger { get; set; }
    public CalendarTypes CalendarType { get; set; }
    public string Region { get; set; }
    public TriggerTypes TriggerType { get; set; }
    public string DelayTime { get; set; } // Changed to string
    public DateTime NextOccurrence { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public ScheduleStatus Status { get; set; }
    public DateTime? LastPlayTime { get; set; }
    public DateTime? TriggerTime { get; set; }

    // Make Priority nullable
   public Enums.Priority? Priority { get; set; }

    public void Validate()
    {
        if (Type == ScheduleType.Periodic && Trigger != null)
        {
            throw new ArgumentException("Trigger should not be set for Periodic items.");
        }

        if (Type == ScheduleType.NonPeriodic && (Second != null || Minute != null || Hour != null ))
        {
            throw new ArgumentException("Periodic fields (Second, Minute, Hour, DayOfMonth, Month, DayOfWeek) should not be set for NonPeriodic items.");
        }
        if (TriggerType != TriggerTypes.Delayed && !string.IsNullOrEmpty(DelayTime))
        {
            DelayTime = null;
        }

        if (TriggerType == TriggerTypes.Delayed && string.IsNullOrEmpty(DelayTime))
        {
            throw new ArgumentException("DelayTime must be set when TriggerType is Delayed.");
        }
        if (TriggerType == TriggerTypes.Delayed && !string.IsNullOrEmpty(DelayTime))
        {
            //Validate format of DelayTime
            if (!TimeSpan.TryParse(DelayTime, out _))
                throw new ArgumentException("Invalid format for DelayTime. Use 'hours:minutes:seconds'");
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
    