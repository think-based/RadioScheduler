      //Be Naame Khoda
//FileName: ScheduleCalculator.cs

using System;

public class GregorianScheduleCalculator : IScheduleCalculator
{
    public DateTime GetNextOccurrence(ScheduleItem item, DateTime now)
    {
         DateTime nextOccurrence = now;

            // Handle Second field
            if (item.Second.StartsWith("*/"))
            {
                int interval = int.Parse(item.Second.Substring(2));
                nextOccurrence = nextOccurrence.AddSeconds(interval);
            }
            else if (item.Second != "*")
            {
                int targetSecond = int.Parse(item.Second);
                if (targetSecond <= now.Second)
                {
                    nextOccurrence = nextOccurrence.AddMinutes(1);
                    nextOccurrence = new DateTime(nextOccurrence.Year, nextOccurrence.Month, nextOccurrence.Day, nextOccurrence.Hour, nextOccurrence.Minute, targetSecond);

                }
                else
                {
                    nextOccurrence = new DateTime(nextOccurrence.Year, nextOccurrence.Month, nextOccurrence.Day, nextOccurrence.Hour, nextOccurrence.Minute, targetSecond);
                }
            }

            // Handle Minute field
            if (item.Minute.StartsWith("*/"))
            {
                int interval = int.Parse(item.Minute.Substring(2));
                nextOccurrence = nextOccurrence.AddMinutes(interval);
            }
            else if (item.Minute != "*")
            {
                int targetMinute = int.Parse(item.Minute);
                if (targetMinute <= now.Minute)
                {
                    nextOccurrence = nextOccurrence.AddHours(1);
                    nextOccurrence = new DateTime(nextOccurrence.Year, nextOccurrence.Month, nextOccurrence.Day, nextOccurrence.Hour, targetMinute, nextOccurrence.Second);
                }
                else
                {
                    nextOccurrence = new DateTime(nextOccurrence.Year, nextOccurrence.Month, nextOccurrence.Day, nextOccurrence.Hour, targetMinute, nextOccurrence.Second);

                }
            }
            // Handle Hour field
            if (item.Hour.StartsWith("*/"))
            {
                int interval = int.Parse(item.Hour.Substring(2));
                nextOccurrence = nextOccurrence.AddHours(interval);
            }
            else if (item.Hour != "*")
            {
                int targetHour = int.Parse(item.Hour);
                if (targetHour <= now.Hour)
                {
                    nextOccurrence = nextOccurrence.AddDays(1);
                    nextOccurrence = new DateTime(nextOccurrence.Year, nextOccurrence.Month, nextOccurrence.Day, targetHour, nextOccurrence.Minute, nextOccurrence.Second);

                }
                else
                {
                    nextOccurrence = new DateTime(nextOccurrence.Year, nextOccurrence.Month, nextOccurrence.Day, targetHour, nextOccurrence.Minute, nextOccurrence.Second);

                }
            }
            // Handle DayOfMonth field
            if (item.DayOfMonth != "*")
            {
                int targetDay = int.Parse(item.DayOfMonth);
                if (targetDay <= now.Day)
                {
                    nextOccurrence = nextOccurrence.AddMonths(1);

                }
                nextOccurrence = new DateTime(nextOccurrence.Year, nextOccurrence.Month, targetDay, nextOccurrence.Hour, nextOccurrence.Minute, nextOccurrence.Second);


            }

            // Handle Month field
            if (item.Month != "*")
            {
                int targetMonth = int.Parse(item.Month);
                if (targetMonth <= now.Month)
                {
                    nextOccurrence = nextOccurrence.AddYears(1);
                }

                nextOccurrence = new DateTime(nextOccurrence.Year, targetMonth, nextOccurrence.Day, nextOccurrence.Hour, nextOccurrence.Minute, nextOccurrence.Second);

            }

             // Handle DayOfWeek field
            if (item.DayOfWeek != "*")
             {
                int targetDayOfWeek = int.Parse(item.DayOfWeek);
                int currentDayOfWeek = CalendarHelper.GetDayOfWeek(now, item.Region);

                 // Calculate the next occurrence of the target day of the week
                int daysToAdd = (targetDayOfWeek - currentDayOfWeek + 7) % 7;
                 if (daysToAdd == 0 && nextOccurrence <= now)
                {
                     daysToAdd = 7; // Move to the next week
                }

                  nextOccurrence = nextOccurrence.AddDays(daysToAdd);
              }

            // Ensure the next occurrence is in the future
            if (nextOccurrence <= now)
            {
                return DateTime.MaxValue; // No valid occurrence found
            }

            return nextOccurrence;
    }
   public bool IsNonPeriodicTriggerValid(ScheduleItem item, DateTime now)
    {
      // Apply the filters
        bool dayOfMonthMatch = item.DayOfMonth == "*" || int.Parse(item.DayOfMonth) == now.Day;
        bool monthMatch = item.Month == "*" || int.Parse(item.Month) == now.Month;
        bool dayOfWeekMatch = item.DayOfWeek == "*" || int.Parse(item.DayOfWeek) == CalendarHelper.GetDayOfWeek(now, item.Region);


        // Return true only if all filters match
        return dayOfMonthMatch && monthMatch && dayOfWeekMatch;

    }
}

public class HijriScheduleCalculator : IScheduleCalculator
{
      public DateTime GetNextOccurrence(ScheduleItem item, DateTime now)
    {
        DateTime nextOccurrence = now;
        DateTime convertedNow = CalendarHelper.ConvertDate(now, "hijri");

            // Handle Second field
            if (item.Second.StartsWith("*/"))
            {
                int interval = int.Parse(item.Second.Substring(2));
                nextOccurrence = nextOccurrence.AddSeconds(interval);
            }
            else if (item.Second != "*")
            {
                int targetSecond = int.Parse(item.Second);
                if (targetSecond <= convertedNow.Second)
                {
                    nextOccurrence = nextOccurrence.AddMinutes(1);
                    nextOccurrence = new DateTime(nextOccurrence.Year, nextOccurrence.Month, nextOccurrence.Day, nextOccurrence.Hour, nextOccurrence.Minute, targetSecond);

                }
                else
                {
                    nextOccurrence = new DateTime(nextOccurrence.Year, nextOccurrence.Month, nextOccurrence.Day, nextOccurrence.Hour, nextOccurrence.Minute, targetSecond);
                }
            }

            // Handle Minute field
            if (item.Minute.StartsWith("*/"))
            {
                int interval = int.Parse(item.Minute.Substring(2));
                nextOccurrence = nextOccurrence.AddMinutes(interval);
            }
            else if (item.Minute != "*")
            {
                int targetMinute = int.Parse(item.Minute);
                if (targetMinute <= convertedNow.Minute)
                {
                    nextOccurrence = nextOccurrence.AddHours(1);
                    nextOccurrence = new DateTime(nextOccurrence.Year, nextOccurrence.Month, nextOccurrence.Day, nextOccurrence.Hour, targetMinute, nextOccurrence.Second);
                }
                else
                {
                    nextOccurrence = new DateTime(nextOccurrence.Year, nextOccurrence.Month, nextOccurrence.Day, nextOccurrence.Hour, targetMinute, nextOccurrence.Second);

                }
            }
            // Handle Hour field
            if (item.Hour.StartsWith("*/"))
            {
                int interval = int.Parse(item.Hour.Substring(2));
                nextOccurrence = nextOccurrence.AddHours(interval);
            }
            else if (item.Hour != "*")
            {
                int targetHour = int.Parse(item.Hour);
                if (targetHour <= convertedNow.Hour)
                {
                    nextOccurrence = nextOccurrence.AddDays(1);
                     nextOccurrence = new DateTime(nextOccurrence.Year, nextOccurrence.Month, nextOccurrence.Day, targetHour, nextOccurrence.Minute, nextOccurrence.Second);
                }
                else
                {
                  nextOccurrence = new DateTime(nextOccurrence.Year, nextOccurrence.Month, nextOccurrence.Day, targetHour, nextOccurrence.Minute, nextOccurrence.Second);
                }
            }
           // Handle DayOfMonth field
            if (item.DayOfMonth != "*")
            {
                int targetDay = int.Parse(item.DayOfMonth);
                if (targetDay <= convertedNow.Day)
                {
                    nextOccurrence = nextOccurrence.AddMonths(1);

                }
                nextOccurrence = new DateTime(nextOccurrence.Year, nextOccurrence.Month, targetDay, nextOccurrence.Hour, nextOccurrence.Minute, nextOccurrence.Second);

            }

            // Handle Month field
             if (item.Month != "*")
            {
                int targetMonth = int.Parse(item.Month);
                if (targetMonth <= convertedNow.Month)
                {
                    nextOccurrence = nextOccurrence.AddYears(1);
                }

                nextOccurrence = new DateTime(nextOccurrence.Year, targetMonth, nextOccurrence.Day, nextOccurrence.Hour, nextOccurrence.Minute, nextOccurrence.Second);
            }

            // Handle DayOfWeek field
            if (item.DayOfWeek != "*")
            {
                 int targetDayOfWeek = int.Parse(item.DayOfWeek);
                  int currentDayOfWeek = CalendarHelper.GetDayOfWeek(convertedNow, item.Region);
                // Calculate the next occurrence of the target day of the week
                int daysToAdd = (targetDayOfWeek - currentDayOfWeek + 7) % 7;
                if (daysToAdd == 0 && nextOccurrence <= now)
                 {
                      daysToAdd = 7; // Move to the next week
                  }
                nextOccurrence = nextOccurrence.AddDays(daysToAdd);
             }

            // Ensure the next occurrence is in the future
            if (nextOccurrence <= now)
            {
                return DateTime.MaxValue; // No valid occurrence found
            }
             return nextOccurrence;
     }
     public bool IsNonPeriodicTriggerValid(ScheduleItem item, DateTime now)
    {
        DateTime convertedNow = CalendarHelper.ConvertDate(now, "hijri");
        // Apply the filters
         bool dayOfMonthMatch = item.DayOfMonth == "*" || int.Parse(item.DayOfMonth) == convertedNow.Day;
         bool monthMatch = item.Month == "*" || int.Parse(item.Month) == convertedNow.Month;
         bool dayOfWeekMatch = item.DayOfWeek == "*" || int.Parse(item.DayOfWeek) == CalendarHelper.GetDayOfWeek(convertedNow, item.Region);

        // Return true only if all filters match
         return dayOfMonthMatch && monthMatch && dayOfWeekMatch;
    }
}

public class PersianScheduleCalculator : IScheduleCalculator
{
    public DateTime GetNextOccurrence(ScheduleItem item, DateTime now)
    {
       DateTime nextOccurrence = now;
       DateTime convertedNow = CalendarHelper.ConvertDate(now, "persian");

         // Handle Second field
            if (item.Second.StartsWith("*/"))
            {
                int interval = int.Parse(item.Second.Substring(2));
                nextOccurrence = nextOccurrence.AddSeconds(interval);
            }
            else if (item.Second != "*")
            {
                int targetSecond = int.Parse(item.Second);
                if (targetSecond <= convertedNow.Second)
                {
                    nextOccurrence = nextOccurrence.AddMinutes(1);
                    nextOccurrence = new DateTime(nextOccurrence.Year, nextOccurrence.Month, nextOccurrence.Day, nextOccurrence.Hour, nextOccurrence.Minute, targetSecond);

                }
                else
                {
                   nextOccurrence = new DateTime(nextOccurrence.Year, nextOccurrence.Month, nextOccurrence.Day, nextOccurrence.Hour, nextOccurrence.Minute, targetSecond);
                }
            }

            // Handle Minute field
            if (item.Minute.StartsWith("*/"))
            {
                int interval = int.Parse(item.Minute.Substring(2));
                nextOccurrence = nextOccurrence.AddMinutes(interval);
            }
            else if (item.Minute != "*")
            {
                int targetMinute = int.Parse(item.Minute);
                if (targetMinute <= convertedNow.Minute)
                {
                    nextOccurrence = nextOccurrence.AddHours(1);
                     nextOccurrence = new DateTime(nextOccurrence.Year, nextOccurrence.Month, nextOccurrence.Day, nextOccurrence.Hour, targetMinute, nextOccurrence.Second);

                }
                else
                {
                   nextOccurrence = new DateTime(nextOccurrence.Year, nextOccurrence.Month, nextOccurrence.Day, nextOccurrence.Hour, targetMinute, nextOccurrence.Second);
                }
            }
            // Handle Hour field
            if (item.Hour.StartsWith("*/"))
            {
                int interval = int.Parse(item.Hour.Substring(2));
               nextOccurrence = nextOccurrence.AddHours(interval);
            }
            else if (item.Hour != "*")
            {
                int targetHour = int.Parse(item.Hour);
                if (targetHour <= convertedNow.Hour)
                {
                     nextOccurrence = nextOccurrence.AddDays(1);
                      nextOccurrence = new DateTime(nextOccurrence.Year, nextOccurrence.Month, nextOccurrence.Day, targetHour, nextOccurrence.Minute, nextOccurrence.Second);
                 }
                else
                {
                    nextOccurrence = new DateTime(nextOccurrence.Year, nextOccurrence.Month, nextOccurrence.Day, targetHour, nextOccurrence.Minute, nextOccurrence.Second);
                }
            }
            // Handle DayOfMonth field
            if (item.DayOfMonth != "*")
            {
                int targetDay = int.Parse(item.DayOfMonth);
                if (targetDay <= convertedNow.Day)
                {
                   nextOccurrence = nextOccurrence.AddMonths(1);

                }
               nextOccurrence = new DateTime(nextOccurrence.Year, nextOccurrence.Month, targetDay, nextOccurrence.Hour, nextOccurrence.Minute, nextOccurrence.Second);

            }

            // Handle Month field
            if (item.Month != "*")
            {
                int targetMonth = int.Parse(item.Month);
                 if (targetMonth <= convertedNow.Month)
                {
                    nextOccurrence = nextOccurrence.AddYears(1);
                }

                nextOccurrence = new DateTime(nextOccurrence.Year, targetMonth, nextOccurrence.Day, nextOccurrence.Hour, nextOccurrence.Minute, nextOccurrence.Second);
            }
             // Handle DayOfWeek field
             if (item.DayOfWeek != "*")
            {
                 int targetDayOfWeek = int.Parse(item.DayOfWeek);
                 int currentDayOfWeek = CalendarHelper.GetDayOfWeek(convertedNow, item.Region);
                 // Calculate the next occurrence of the target day of the week
                int daysToAdd = (targetDayOfWeek - currentDayOfWeek + 7) % 7;
                if (daysToAdd == 0 && nextOccurrence <= now)
                {
                  daysToAdd = 7; // Move to the next week
               }
                nextOccurrence = nextOccurrence.AddDays(daysToAdd);
              }
            // Ensure the next occurrence is in the future
            if (nextOccurrence <= now)
            {
                return DateTime.MaxValue; // No valid occurrence found
            }

           return nextOccurrence;

    }
       public bool IsNonPeriodicTriggerValid(ScheduleItem item, DateTime now)
    {
        DateTime convertedNow = CalendarHelper.ConvertDate(now, "persian");
         // Apply the filters
          bool dayOfMonthMatch = item.DayOfMonth == "*" || int.Parse(item.DayOfMonth) == convertedNow.Day;
         bool monthMatch = item.Month == "*" || int.Parse(item.Month) == convertedNow.Month;
         bool dayOfWeekMatch = item.DayOfWeek == "*" || int.Parse(item.DayOfWeek) == CalendarHelper.GetDayOfWeek(convertedNow, item.Region);

        // Return true only if all filters match
         return dayOfMonthMatch && monthMatch && dayOfWeekMatch;
    }
}
    