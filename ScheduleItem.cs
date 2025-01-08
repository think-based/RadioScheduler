//Be Naame Khoda
//FileName: ScheduleItem.cs

public class ScheduleItem
{
    public int ItemId { get; set; } // شناسه منحصر به فرد
    public string Type { get; set; } // نوع زمان‌بندی (Periodic یا NonPeriodic)
    public List<FilePathItem> FilePaths { get; set; } // لیست فایل‌ها یا پوشه‌ها

    // Fields for Periodic items
    public string Second { get; set; } // ثانیه (0-59) - فقط برای Periodic
    public string Minute { get; set; } // دقیقه (0-59) - فقط برای Periodic
    public string Hour { get; set; } // ساعت (0-23) - فقط برای Periodic
    public string DayOfMonth { get; set; } // روز ماه (1-31) - فقط برای Periodic
    public string Month { get; set; } // ماه (1-12) - فقط برای Periodic
    public string DayOfWeek { get; set; } // روز هفته (0-6) - فقط برای Periodic

    // Fields for NonPeriodic items
    public string Trigger { get; set; } // نام تریگر - فقط برای NonPeriodic

    public string CalendarType { get; set; } // نوع تقویم
    public string TriggerType { get; set; } // نوع تریگر (Immediate یا Timed یا Delayed)
    public int? DelayMinutes { get; set; } // تعداد دقیقه‌های تاخیر برای TriggerType = Delayed

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
    }
}

public class FilePathItem
{
    public string Path { get; set; } // مسیر فایل یا پوشه
    public string FolderPlayMode { get; set; } // حالت پخش پوشه (All یا Single) - فقط برای پوشه‌ها
}