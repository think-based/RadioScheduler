      //Be Naame Khoda
//FileName: ScheduleCalculatorFactory.cs

using System;

 public class ScheduleCalculatorFactory : IScheduleCalculatorFactory
    {
        public IScheduleCalculator CreateCalculator(Enums.CalendarTypes calendarType)
        {
            switch (calendarType)
            {
                case Enums.CalendarTypes.Gregorian:
                    return new GregorianScheduleCalculator();
                case Enums.CalendarTypes.Hijri:
                    return new HijriScheduleCalculator();
                case Enums.CalendarTypes.Persian:
                    return new PersianScheduleCalculator();
                default:
                    throw new ArgumentOutOfRangeException(nameof(calendarType), calendarType, "Invalid CalendarType");
            }
        }
    }
    