//Be Naame Khoda
//FileName: Logger.cs

using System;
using System.IO;

public static class Logger
{
    public static readonly string LogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.log");
    private static readonly object _logLock = new object(); // Lock object for thread safety

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
            lock (_logLock) // Ensures exclusive access
            {
                string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}";
                using (FileStream fileStream = new FileStream(LogFilePath, FileMode.Append, FileAccess.Write, FileShare.Read))
                using (StreamWriter writer = new StreamWriter(fileStream))
                {
                    writer.WriteLine(logEntry);
                }
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
            lock (_logLock)
            {
                // پاک کردن محتوای فایل لاگ
                File.WriteAllText(LogFilePath, string.Empty);
            }
            Logger.LogMessage("Log file cleared."); // ثبت پیام در لاگ پس از پاک کردن
        }
        catch (Exception ex)
        {
            Logger.LogMessage($"Error clearing log file: {ex.Message}");
        }
    }
}