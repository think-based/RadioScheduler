// Be Naame Khoda
// FileName: Scheduler.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

public class Scheduler
{
    // Other properties and methods...

    private DateTime GetNextOccurrence(ScheduleItem scheduleItem, DateTime now, DateTime endTime)
    {
        DateTime nextOccurrence = DateTime.MinValue;

        Logger.LogMessage($"Calculating next occurrence for Item {scheduleItem.ItemId}...");

        // Convert the current date to the specified calendar type
        DateTime convertedDate = CalendarHelper.ConvertDate(now, scheduleItem.CalendarType);
        Logger.LogMessage($"Converted date: {convertedDate}");

        // Check if the current date matches the DayOfMonth and Month fields
        if (!MatchesCronField(scheduleItem.DayOfMonth, convertedDate.Day.ToString()))
        {
            Logger.LogMessage($"DayOfMonth mismatch: {scheduleItem.DayOfMonth} != {convertedDate.Day}");
            return nextOccurrence;
        }
        if (!MatchesCronField(scheduleItem.Month, convertedDate.Month.ToString()))
        {
            Logger.LogMessage($"Month mismatch: {scheduleItem.Month} != {convertedDate.Month}");
            return nextOccurrence;
        }

        // Check if the current day of the week matches the DayOfWeek field
        int currentDayOfWeek = CalendarHelper.GetDayOfWeek(convertedDate, scheduleItem.Region);
        if (!MatchesCronField(scheduleItem.DayOfWeek, currentDayOfWeek.ToString()))
        {
            Logger.LogMessage($"DayOfWeek mismatch: {scheduleItem.DayOfWeek} != {currentDayOfWeek}");
            return nextOccurrence;
        }

        if (scheduleItem.Type == "Periodic")
        {
            // For Periodic items, check the time fields (Second, Minute, Hour)
            if (!MatchesCronField(scheduleItem.Second, now.Second.ToString()))
            {
                Logger.LogMessage($"Second mismatch: {scheduleItem.Second} != {now.Second}");
                return nextOccurrence;
            }
            if (!MatchesCronField(scheduleItem.Minute, now.Minute.ToString()))
            {
                Logger.LogMessage($"Minute mismatch: {scheduleItem.Minute} != {now.Minute}");
                return nextOccurrence;
            }
            if (!MatchesCronField(scheduleItem.Hour, now.Hour.ToString()))
            {
                Logger.LogMessage($"Hour mismatch: {scheduleItem.Hour} != {now.Hour}");
                return nextOccurrence;
            }

            nextOccurrence = now;
        }
        else if (scheduleItem.Type == "NonPeriodic")
        {
            // For NonPeriodic items, check the trigger
            if (string.IsNullOrEmpty(scheduleItem.Trigger) || scheduleItem.Trigger != _currentTrigger.Event)
            {
                Logger.LogMessage($"Trigger mismatch: {scheduleItem.Trigger} != {_currentTrigger.Event}");
                return nextOccurrence;
            }

            nextOccurrence = now;
        }

        // If the next occurrence is within the valid range, return it
        if (nextOccurrence >= now && nextOccurrence <= endTime)
        {
            Logger.LogMessage($"Next occurrence for Item {scheduleItem.ItemId}: {nextOccurrence}");
            return nextOccurrence;
        }

        Logger.LogMessage($"Next occurrence for Item {scheduleItem.ItemId} is out of range.");
        return DateTime.MinValue;
    }

    private bool MatchesCronField(string cronField, string value)
    {
        if (cronField == "*") return true;
        return cronField == value;
    }

    // Other methods...
}