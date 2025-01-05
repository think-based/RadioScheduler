//Be Naame Khoda
//FileName: Logger.cs

using System;
using System.IO;

public static class Logger
{
    private static readonly string LogFilePath = "app.log";

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
}