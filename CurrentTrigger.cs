using System;

public class CurrentTrigger
{
    public string Event { get; set; } // نام تریگر (مانند "afternoon")
    public DateTime? Time { get; set; } // زمان پایان پخش (nullable)
}