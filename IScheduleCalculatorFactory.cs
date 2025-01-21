      //Be Naame Khoda
//FileName: IScheduleCalculatorFactory.cs

public interface IScheduleCalculatorFactory
    {
        IScheduleCalculator CreateCalculator(Enums.CalendarTypes calendarType);
    }
    