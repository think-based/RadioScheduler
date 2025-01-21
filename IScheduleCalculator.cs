      //Be Naame Khoda
//FileName: IScheduleCalculator.cs

using System;

public interface IScheduleCalculator
{
    DateTime GetNextOccurrence(ScheduleItem item, DateTime now);
    bool IsNonPeriodicTriggerValid(ScheduleItem item, DateTime now);
}
    