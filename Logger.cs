//Be Naame Khoda
//FileName: Logger.cs

using System;
using System.IO;

public static class Logger
{
    // تغییر سطح دسترسی به public
    public static readonly string LogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.log");

    static Logger()
    {
        // اگر فایل لاگ وجود نداشت، آن را ایجاد کن
        if (!File.Exists(LogFilePath))
        {
            try
            {
                File.Create(LogFilePath).Close(); // ایجاد فایل و بستن آن
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating log file: {ex.Message}");
            }
        }
    }

    public static void LogMessage(string message)
    {
        try
        {
            string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}";
            using (StreamWriter writer = new StreamWriter(LogFilePath, true))
            {
                writer.WriteLine(logEntry);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error writing to log file: {ex.Message}");
        }
    }

    public static void ClearLog()
    {
        try
        {
            File.WriteAllText(LogFilePath, string.Empty);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error clearing log file: {ex.Message}");
        }
    }
}