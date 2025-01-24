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
using System.Linq;

public class SchedulerConfigManager : ISchedulerConfigManager
{
    private string _configFilePath;
    private string _currentFileHash;
    private Timer _configCheckTimer;
    public event Action ConfigReloaded;

    public List<ScheduleItem> ScheduleItems { get; private set; } = new List<ScheduleItem>();

    public SchedulerConfigManager(string configFilePath)
    {
        _configFilePath = configFilePath;
        _configCheckTimer = new Timer(60000);
        _configCheckTimer.Elapsed += OnConfigFileCheckTimerElapsed;
        _configCheckTimer.AutoReset = true;
        _configCheckTimer.Enabled = true;

        ReloadScheduleConfig();
        _currentFileHash = CalculateConfigHash();
        ConfigReloaded?.Invoke();
    }

    public void ReloadScheduleConfig()
    {
        try
        {
            if (File.Exists(_configFilePath))
            {
                string json = File.ReadAllText(_configFilePath);
                var newItems = JsonConvert.DeserializeObject<List<ScheduleItem>>(json);

                ScheduleItems.Clear();

                foreach (var item in newItems)
                {
                    try
                    {
                        ProcessScheduleItem(item);
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

   public void ReloadScheduleItem(int itemId)
    {
        try
        {
            if (File.Exists(_configFilePath))
            {
                string json = File.ReadAllText(_configFilePath);
                var newItems = JsonConvert.DeserializeObject<List<ScheduleItem>>(json);

                var newItem = newItems.FirstOrDefault(i => i.ItemId == itemId);

                if (newItem != null)
                {
                    try
                    {
                        ProcessScheduleItem(newItem);

                        var existingItemIndex = ScheduleItems.FindIndex(i => i.ItemId == itemId);

                        if (existingItemIndex >= 0)
                        {
                            ScheduleItems[existingItemIndex] = newItem;
                            Logger.LogMessage($"Reloaded item with ItemId: {itemId}");
                        }
                        else
                        {
                             ScheduleItems.Add(newItem);
                             Logger.LogMessage($"Added new item with ItemId: {itemId}");
                        }

                         ConfigReloaded?.Invoke();
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
                        Logger.LogMessage($"Error processing schedule item: {ex.Message}");
                    }
                }
                 else
                {
                    Logger.LogMessage($"Item with ItemId {itemId} not found in config file.");
                }
            }
            else
            {
                Logger.LogMessage($"Config file not found: {_configFilePath}");
            }
        }
        catch (Exception ex)
        {
            Logger.LogMessage($"Error reloading schedule item: {ex.Message}");
        }
    }

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

    private string CalculateConfigHash()
    {
        if (!File.Exists(_configFilePath))
            return "";
        return FileHashHelper.CalculateFileHash(_configFilePath);
    }

   private void ProcessScheduleItem(ScheduleItem item)
    {
        // Convert string values to enums with error handling
        item.Type = Enums.ParseEnum<ScheduleType>(item.Type.ToString(), "ScheduleType");
        item.CalendarType = Enums.ParseEnum<CalendarTypes>(item.CalendarType.ToString(), "CalendarTypes");
        item.TriggerType = Enums.ParseEnum<TriggerTypes>(item.TriggerType.ToString(), "TriggerTypes");

          if (!item.Priority.HasValue)
        {
            item.Priority = Priority.Low;
        }
         else if (!Enum.IsDefined(typeof(Priority), item.Priority.Value))
        {
            item.Priority = Priority.Low;
             Logger.LogMessage($"Priority {item.Priority.Value} from ItemId '{item.ItemId}' is invalid. Setting it to Low");
        }


        item.Status = ScheduleStatus.TimeWaiting;
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

           item.PlayList.Clear();

             foreach (var filePathItem in item.FilePaths)
        {
            if (!string.IsNullOrEmpty(filePathItem.Text))
            {
                 item.PlayList.Add($"TTS:{filePathItem.Text}");
            }
            else if (File.Exists(filePathItem.Path))
            {
                 item.PlayList.Add(filePathItem.Path);
            }
            else if (Directory.Exists(filePathItem.Path))
            {
                 var audioFiles = Directory.GetFiles(filePathItem.Path, "*.mp3").OrderBy(f => f).ToList();
                  if(audioFiles.Count == 0)
                    {
                         Logger.LogMessage($"No audio files found for folder '{filePathItem.Path}' and schedule item '{item.Name}'");
                    }
                if (filePathItem.FolderPlayMode == "Random")
                {
                   if(audioFiles.Any())
                    {
                        var random = new Random();
                        item.PlayList.Add(audioFiles[random.Next(audioFiles.Count)]);
                    }

                }
                else if (filePathItem.FolderPlayMode == "Que")
                {
                     if (audioFiles.Count > 0)
                    {
                         int lastPlayedIndex = -1;

                        if (!string.IsNullOrEmpty(filePathItem.LastPlayedFile))
                        {
                             lastPlayedIndex = audioFiles.IndexOf(filePathItem.LastPlayedFile);
                        }
                        int nextIndex = (lastPlayedIndex + 1) % audioFiles.Count;

                        item.PlayList.Add(audioFiles[nextIndex]);
                        filePathItem.LastPlayedFile = audioFiles[nextIndex]; // Store last played file in FilePathItem

                    }

                }
                  else
                {
                   item.PlayList.AddRange(audioFiles);
                 }
            }

        }
          item.TotalDuration = CalculateTotalDuration(item.PlayList);
    }

    private TimeSpan CalculateTotalDuration(List<string> playList)
    {
        TimeSpan totalDuration = TimeSpan.Zero;
        var ttsEngine = new SpeechSynthesizer();

          foreach (var item in playList)
        {
            if (item.StartsWith("TTS:"))
            {
                 string text = item.Substring(4);
                 totalDuration += CalculateTtsDuration(text, ttsEngine);

            }
             else if (File.Exists(item))
            {
                 try
                {
                    using (var reader = new AudioFileReader(item))
                    {
                        totalDuration += reader.TotalTime;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogMessage($"Error calculating duration for file {item}: {ex.Message}");
                }
            }

        }

        ttsEngine.Dispose();
        return totalDuration;
    }

    private TimeSpan CalculateTtsDuration(string text, SpeechSynthesizer ttsEngine)
    {
        try
        {
            var prompt = new PromptBuilder();
            prompt.AppendText(text);

            var ttsStream = new MemoryStream();
            ttsEngine.SetOutputToWaveStream(ttsStream);
            ttsEngine.Speak(prompt);

            ttsStream.Position = 0;
            using (var reader = new WaveFileReader(ttsStream))
            {
                return reader.TotalTime;
            }
        }
        catch (Exception ex)
        {
            Logger.LogMessage($"Error calculating TTS duration: {ex.Message}");
            return TimeSpan.FromSeconds(text.Length / 10.0);
        }
    }
}
    