//Be Naame Khoda
//FileName: Logger.cs

using System;
using System.IO;

public static class Logger
{
    private static readonly string LogFilePath = "app.log"; // مسیر فایل لاگ

    public static void LogMessage(string message)
    {
        try
        {
            // افزودن تاریخ و ساعت به پیام
            string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}";

            // نوشتن پیام در فایل لاگ
            using (StreamWriter writer = new StreamWriter(LogFilePath, true))
            {
                writer.WriteLine(logEntry);
            }
        }
        catch (Exception ex)
        {
            // اگر خطایی در نوشتن لاگ رخ داد، آن را در کنسول نمایش می‌دهیم
            Console.WriteLine($"Error writing to log file: {ex.Message}");
        }
    }
}