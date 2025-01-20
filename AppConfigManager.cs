//Be Naame Khoda
//FileName: AppConfigManager.cs

using Newtonsoft.Json;
using System;
using System.IO;

public static class AppConfigManager
{
    private static readonly string ConfigFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AppSettings.conf");

    // بارگذاری تنظیمات از فایل JSON
    public static AppSettings LoadConfig()
    {
        if (!File.Exists(ConfigFilePath))
        {
            throw new FileNotFoundException("Configuration file not found.", ConfigFilePath);
        }

        try
        {
            string json = File.ReadAllText(ConfigFilePath);
            return JsonConvert.DeserializeObject<AppSettings>(json);
        }
        catch (Exception ex)
        {
            Logger.LogMessage($"Error loading config: {ex.Message}");
            throw;
        }
    }

    // ذخیره‌سازی تنظیمات در فایل JSON
    public static void SaveConfig(AppSettings settings)
    {
        try
        {
            string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            File.WriteAllText(ConfigFilePath, json);
        }
        catch (Exception ex)
        {
            Logger.LogMessage($"Error saving config: {ex.Message}");
            throw;
        }
    }
}