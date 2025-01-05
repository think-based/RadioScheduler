//Be Naame Khoda
//FileName: ScheduleItem.cs

using System.Collections.Generic;

public class ScheduleItem
{
    public int ItemId { get; set; } // شناسه منحصر به فرد
    public string Type { get; set; } // نوع زمان‌بندی (Periodic یا NonPeriodic)
    public List<string> FilePaths { get; set; } // لیست فایل‌های صوتی
    public string Second { get; set; } // ثانیه (0-59) - فقط برای Periodic
    public string Minute { get; set; } // دقیقه (0-59) - فقط برای Periodic
    public string Hour { get; set; } // ساعت (0-23) - فقط برای Periodic
    public string DayOfMonth { get; set; } // روز ماه (1-31)
    public string Month { get; set; } // ماه (1-12)
    public string DayOfWeek { get; set; } // روز هفته (0-6)
    public string Trigger { get; set; } // نام تریگر - فقط برای NonPeriodic
    public string CalendarType { get; set; } // نوع تقویم
    public string TriggerType { get; set; } // نوع تریگر (Immediate یا Timed)
}