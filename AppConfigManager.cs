      //Be Naame Khoda
//FileName: AppConfigManager.cs

using Newtonsoft.Json;
using System;
using System.IO;

public static class AppConfigManager
{
    private static readonly string ConfigFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AppSettings.conf");

    public static AppSettings LoadConfig()
    {
        if (!File.Exists(ConfigFilePath))
        {
            throw new FileNotFoundException("Configuration file not found.", ConfigFilePath);
        }

        try
        {
            string json = File.ReadAllText(ConfigFilePath);
            var settings = JsonConvert.DeserializeObject<AppSettings>(json);

            if (settings != null && !string.IsNullOrEmpty(settings.Application.TimeZoneId))
            {
                try
                {
                    TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById(settings.Application.TimeZoneId);
                    settings.Application.TimeZoneOffset = timeZone.BaseUtcOffset.TotalHours;
                }
                 catch (TimeZoneNotFoundException ex)
                {
                     Logger.LogMessage($"Error finding Time Zone {settings.Application.TimeZoneId} : {ex.Message}");
                     throw; // Re-throw the exception
                }

            }
              else
            {
                   Logger.LogMessage("Time Zone Id not found in config, default offset will be set to 0.");
                     settings.Application.TimeZoneOffset = 0;
            }

            return settings;
        }
        catch (Exception ex)
        {
            Logger.LogMessage($"Error loading config: {ex.Message}");
            throw;
        }
    }

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
    