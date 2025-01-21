      //Be Naame Khoda
//FileName: SchedulerConfigManager.cs

using System;
using System.IO;
using System.Timers;
using Newtonsoft.Json;
using System.Collections.Generic;
using static Enums;
using NAudio.Wave;
using RadioScheduler.Entities;
using System.Speech.Synthesis;

public class SchedulerConfigManager : ISchedulerConfigManager
{
    private string _configFilePath;
    private string _currentFileHash;
    private Timer _configCheckTimer;
    public event Action ConfigReloaded; // Event to notify when config is reloaded

    public List<ScheduleItem> ScheduleItems { get; private set; } = new List<ScheduleItem>();

    public SchedulerConfigManager(string configFilePath)
    {
        _configFilePath = configFilePath;
        _configCheckTimer = new Timer(60000); // 1-minute interval
        _configCheckTimer.Elapsed += OnConfigFileCheckTimerElapsed;
        _configCheckTimer.AutoReset = true;
        _configCheckTimer.Enabled = true;

        ReloadScheduleConfig();
        _currentFileHash = CalculateConfigHash();
        ConfigReloaded?.Invoke();
    }


    /// <summary>
    /// Reloads the schedule configuration from the audio.conf file.
    /// </summary>
    public void ReloadScheduleConfig()
    {
        try
        {

            if (File.Exists(_configFilePath))
            {
                string json = File.ReadAllText(_configFilePath);
                var newItems = JsonConvert.DeserializeObject<List<ScheduleItem>>(json);

                // Clear existing schedule items
                ScheduleItems.Clear();

                // Add and validate new schedule items
                foreach (var item in newItems)
                {
                    try
                    {
                        // Convert string values to enums with error handling
                        item.Type = Enums.ParseEnum<ScheduleType>(item.Type.ToString(), "ScheduleType");
                        item.CalendarType = Enums.ParseEnum<CalendarTypes>(item.CalendarType.ToString(), "CalendarTypes");
                        item.TriggerType = Enums.ParseEnum<TriggerTypes>(item.TriggerType.ToString(), "TriggerTypes");

                        item.Status = ScheduleStatus.TimeWaiting; // Initial status
                        item.LastPlayTime = null;
                        item.Validate();
                        if (item.TriggerType == TriggerTypes.Delayed && !string.IsNullOrEmpty(item.DelayTime))
                        {
                            if (!TimeSpan.TryParse(item.DelayTime, out _))
                            {
                                Logger.LogMessage($"Invalid DelayTime '{item.DelayTime}' for  schedule item  '{item.Name}'. Setting to 00:00:00.");
                                item.DelayTime = "00:00:00";
                            }

                        }
                        item.TotalDuration = CalculateTotalDuration(item.FilePaths);

                        ScheduleItems.Add(item);

                    }
                    catch (ArgumentException ex)
                    {
                        Logger.LogMessage($"Error loading schedule item: {ex.Message}");
                    }
                    catch (JsonSerializationException ex)
                    {
                        Logger.LogMessage($"Error deserializing schedule item: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        Logger.LogMessage($"Error general exception loading schedule item: {ex.Message}");
                    }
                }



                Logger.LogMessage($"Loaded {ScheduleItems.Count} items from {_configFilePath}.");
                ConfigReloaded?.Invoke();
            }
            else
            {
                Logger.LogMessage($"Config file not found: {_configFilePath}");
            }
        }
        catch (Exception ex)
        {
            Logger.LogMessage($"Error reloading schedule config: {ex.Message}");
        }

    }
    /// <summary>
    /// Handles the timer tick event to check for configuration file changes.
    /// </summary>
    private void OnConfigFileCheckTimerElapsed(object sender, ElapsedEventArgs e)
    {
        var newFileHash = CalculateConfigHash();

        if (newFileHash != _currentFileHash)
        {
            Logger.LogMessage("Config file changed, reloading configuration.");
            ReloadScheduleConfig();
            _currentFileHash = newFileHash;

        }
    }
    /// <summary>
    /// Calculates the hash of the audio config file.
    /// </summary>
    private string CalculateConfigHash()
    {
        if (!File.Exists(_configFilePath))
            return "";
        return FileHashHelper.CalculateFileHash(_configFilePath);
    }
    /// <summary>
    /// Calculates the total duration of a playlist.
    /// </summary>
    private TimeSpan CalculateTotalDuration(List<FilePathItem> filePaths)
    {
        TimeSpan totalDuration = TimeSpan.Zero;
        var ttsEngine = new SpeechSynthesizer(); // Initialize TTS engine

        foreach (var filePathItem in filePaths)
        {
            if (!string.IsNullOrEmpty(filePathItem.Text)) // Handle TTS
            {
                filePathItem.Duration = CalculateTtsDuration(filePathItem.Text, ttsEngine);
            }
            else if (File.Exists(filePathItem.Path)) // Handle audio file
            {
                try
                {
                    using (var reader = new AudioFileReader(filePathItem.Path))
                    {
                        totalDuration += reader.TotalTime;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogMessage($"Error calculating duration for file {filePathItem.Path}: {ex.Message}");
                }
            }
            else if (Directory.Exists(filePathItem.Path)) // Handle folder
            {
                var audioFiles = Directory.GetFiles(filePathItem.Path, "*.mp3");
                foreach (var audioFile in audioFiles)
                {
                    try
                    {
                        using (var reader = new AudioFileReader(audioFile))
                        {
                            totalDuration += reader.TotalTime;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogMessage($"Error calculating duration for file {audioFile}: {ex.Message}");
                    }
                }
            }

            totalDuration += filePathItem.Duration;
        }

        ttsEngine.Dispose(); // Clean up TTS engine
        return totalDuration;
    }

    /// <summary>
    /// Calculates the exact duration of TTS playback using the SpeechSynthesizer.
    /// </summary>
    /// <param name="text">The TTS text.</param>
    /// <param name="ttsEngine">The TTS engine.</param>
    /// <returns>The duration of the TTS playback.</returns>
    private TimeSpan CalculateTtsDuration(string text, SpeechSynthesizer ttsEngine)
    {
        try
        {
            // Use the TTS engine to get the exact duration
            var prompt = new PromptBuilder();
            prompt.AppendText(text);

            // Measure the duration
            var ttsStream = new MemoryStream();
            ttsEngine.SetOutputToWaveStream(ttsStream);
            ttsEngine.Speak(prompt);

            // Calculate duration based on the audio stream
            ttsStream.Position = 0; // Reset stream position
            using (var reader = new WaveFileReader(ttsStream))
            {
                return reader.TotalTime; // Exact duration of the TTS audio
            }
        }
        catch (Exception ex)
        {
            Logger.LogMessage($"Error calculating TTS duration: {ex.Message}");
            return TimeSpan.FromSeconds(text.Length / 10.0); // Fallback to estimation
        }
    }
}
    