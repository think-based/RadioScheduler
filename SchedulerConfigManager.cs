//Be Naame Khoda
//FileName: SchedulerConfigManager.cs

using System;
using System.IO;
using System.Timers;
using Newtonsoft.Json;
using System.Collections.Generic;
using static Enums;
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
    private int GetNextItemId()
    {
        if (ScheduleItems.Count == 0)
            return 1;
        return ScheduleItems.Max(i => i.ItemId) + 1;
    }

    public void ReloadScheduleConfig()
    {
        try
        {
            if (!File.Exists(_configFilePath))
            {
                Logger.LogMessage($"Config file not found: {_configFilePath}");
                ScheduleItems.Clear(); // Ensure the list is clear if the file doesn't exist.
                ConfigReloaded?.Invoke();
                return;
            }

            string json = File.ReadAllText(_configFilePath);
            var newItems = JsonConvert.DeserializeObject<List<ScheduleItem>>(json);

            ScheduleItems.Clear();

            if (newItems == null || !newItems.Any())
            {
                Logger.LogMessage($"No items found in {_configFilePath}.");
                ConfigReloaded?.Invoke();
                return;
            }

            foreach (var item in newItems)
            {
                // Skip disabled items
                if (item.Disabled)
                {
                    Logger.LogMessage($"Skipping disabled item: {item.Name} (ItemID: {item.ItemId})");
                    continue;
                }
                try
                {
                    AddScheduleItem(item);
                }
                catch (ArgumentException ex)
                {
                    Logger.LogMessage($"Error loading schedule item: {ex.Message} (ItemID: {item.ItemId})");
                }
                catch (JsonSerializationException ex)
                {
                    Logger.LogMessage($"Error deserializing schedule item: {ex.Message} (ItemID: {item.ItemId})");
                }
                catch (Exception ex)
                {
                    Logger.LogMessage($"Error general exception loading schedule item: {ex.Message} (ItemID: {item.ItemId})");
                }
            }

            Logger.LogMessage($"Loaded {ScheduleItems.Count} items from {_configFilePath}.");
            ConfigReloaded?.Invoke();

        }
        catch (FileNotFoundException ex)
        {
            Logger.LogMessage($"Configuration file not found: {ex.Message}");
            ScheduleItems.Clear(); // Ensure the list is clear if the file doesn't exist.
            ConfigReloaded?.Invoke();
        }
        catch (JsonSerializationException ex)
        {
            Logger.LogMessage($"Error deserializing the configuration file: {ex.Message}");
            ScheduleItems.Clear();
            ConfigReloaded?.Invoke();
        }
        catch (Exception ex)
        {
            Logger.LogMessage($"Error reloading schedule config: {ex.Message}");
        }

    }
    private void ResetScheduleItem(ScheduleItem item)
    {
        if (item.Triggers.Any() && item.Triggers.Count >= 1)
        {
            foreach (var trigger in item.Triggers)
            {
                ScheduleItem oldItem = GetScheduleItemByName(item.Name + "(" + trigger + ")");
                if (oldItem != null)
                {
                    ResetPlayList(oldItem);
                }
            }
        }
        else
        {
            ScheduleItem oldItem = GetScheduleItemByName(item.Name);
            if (oldItem != null)
            {
                ResetPlayList(oldItem);
            }
        }
    }
    private void ResetPlayList(ScheduleItem item)
    {
        item.NextOccurrence = DateTime.MinValue;
        item.TriggerTime = DateTime.MinValue;
        item.LastTriggerName = "";
        if (item.Type == ScheduleType.NonPeriodic)
        {
            item.Status = ScheduleStatus.EventWaiting;
        }
        else
        {
            item.Status = ScheduleStatus.TimeWaiting;
        }

        item.CurrentPlayingIndex = -1;

        item.PlayList.Clear();

        foreach (var filePathItem in item.FilePaths)
        {
            if (!string.IsNullOrEmpty(filePathItem.Text))
            {
                item.PlayList.Add(new ScheduleItem.PlayListItem { Path = $"TTS:{filePathItem.Text}" });
            }
            else if (File.Exists(filePathItem.Path))
            {
                item.PlayList.Add(new ScheduleItem.PlayListItem { Path = filePathItem.Path });
            }
            else if (Directory.Exists(filePathItem.Path))
            {
                ProcessFolder(item, filePathItem);
            }
        }
        item.CalculateIndividualItemDuration();
        item.TotalDuration = item.CalculateTotalDuration();
    }
    private void AddScheduleItem(ScheduleItem item)
    {
        if (item.Triggers.Any() && item.Triggers.Count >= 1)
        {
            foreach (var trigger in item.Triggers)
            {
                ScheduleItem subItem = ScheduleItem.Clone(item);
                subItem.Triggers.Add(trigger);
                subItem.Name = item.Name + "(" + trigger + ")";
                subItem.ItemId = GetNextItemId();
                ProcessScheduleItem(subItem);
                ScheduleItems.Add(subItem);
            }
        }
        else
        {
            item.ItemId = GetNextItemId();
            ProcessScheduleItem(item);
            ScheduleItems.Add(item);
        }
    }
    public void RemoveScheduleItems(int configId)
    {
        int removedItems = ScheduleItems.RemoveAll(item => item.ConfigId == configId);
    }
    public ScheduleItem GetScheduleItemById(int itemId)
    {
        return ScheduleItems.FirstOrDefault(item => item.ItemId == itemId);
    }
    public ScheduleItem GetScheduleItemByConfigId(int configId)
    {
        return ScheduleItems.FirstOrDefault(item => item.ConfigId == configId);
    }
    public ScheduleItem GetScheduleItemByName(string itemName)
    {
        return ScheduleItems.FirstOrDefault(item => item.Name == itemName);
    }
    public void ReloadScheduleItemById(int itemId)
    {
        var theItem = ScheduleItems.FirstOrDefault(item => item.ItemId == itemId);
        ResetPlayList(theItem);
        ConfigReloaded?.Invoke();
    }
    public void ReloadScheduleItem(int configId)
    {
        try
        {
            if (!File.Exists(_configFilePath))
            {
                Logger.LogMessage($"Config file not found: {_configFilePath}");
                return;
            }

            string json = File.ReadAllText(_configFilePath);
            var newItems = JsonConvert.DeserializeObject<List<ScheduleItem>>(json);

            var newItem = newItems?.FirstOrDefault(i => i.ConfigId == configId);
            if (newItem == null)
            {
                Logger.LogMessage($"Item with ConfigId {configId} not found in config file.");
                return;
            }
            // Skip disabled items
            if (newItem.Disabled)
            {
                Logger.LogMessage($"Skipping disabled item: {newItem.Name} (ConfigId: {newItem.ConfigId})");
                RemoveScheduleItems(configId);
                ConfigReloaded?.Invoke();
                return;
            }
            try
            {
                ResetScheduleItem(newItem);
                Logger.LogMessage($"Reloaded item with ConfigId: {configId}");

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
        catch (FileNotFoundException ex)
        {
            Logger.LogMessage($"Configuration file not found: {ex.Message}");
        }
        catch (JsonSerializationException ex)
        {
            Logger.LogMessage($"Error deserializing the configuration file: {ex.Message}");
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

    public void ProcessScheduleItem(ScheduleItem item)
    {
        // Convert string values to enums with error handling
        item.Type = Enums.ParseEnum<ScheduleType>(item.Type.ToString(), nameof(ScheduleType));
        item.CalendarType = Enums.ParseEnum<CalendarTypes>(item.CalendarType.ToString(), nameof(CalendarTypes));
        item.TriggerType = Enums.ParseEnum<TriggerTypes>(item.TriggerType.ToString(), nameof(TriggerTypes));
        if (!item.Priority.HasValue)
        {
            item.Priority = Priority.Low;
        }
        else if (!Enum.IsDefined(typeof(Priority), item.Priority.Value))
        {
            item.Priority = Priority.Low;
            Logger.LogMessage($"Priority {item.Priority.Value} from ItemId '{item.ItemId}' is invalid. Setting it to Low");
        }   

        item.Validate();
        if (item.TriggerType == TriggerTypes.Delayed && !string.IsNullOrEmpty(item.DelayTime))
        {
            if (!TimeSpan.TryParse(item.DelayTime, out _))
            {
                Logger.LogMessage($"Invalid DelayTime '{item.DelayTime}' for  schedule item  '{item.Name}'. Setting to 00:00:00.");
                item.DelayTime = "00:00:00";
            }

        }
        if (item.Triggers == null)
        {
            item.Triggers = new List<string>();
        }

        ResetPlayList(item);
    }
    private void ProcessFolder(ScheduleItem item, FilePathItem filePathItem)
    {
        try
        {
            var audioFiles = Directory.EnumerateFiles(filePathItem.Path, "*.mp3")
                .OrderBy(f => f)
                .ToList();

            if (audioFiles.Count == 0)
            {
                Logger.LogMessage($"No audio files found for folder '{filePathItem.Path}' and schedule item '{item.Name}'");
                return;
            }

            if (filePathItem.FolderPlayMode == "Random")
            {
                var random = new Random();
                var randomFile = audioFiles[random.Next(audioFiles.Count)].Replace('\\', '/');
                item.PlayList.Add(new ScheduleItem.PlayListItem { Path = randomFile });
            }
            else if (filePathItem.FolderPlayMode == "Que")
            {
                int lastPlayedIndex = -1;

                if (!string.IsNullOrEmpty(filePathItem.LastPlayedFile))
                {
                    lastPlayedIndex = audioFiles.IndexOf(filePathItem.LastPlayedFile);
                }
                int nextIndex = (lastPlayedIndex + 1) % audioFiles.Count;

                // Ensure using forward slashes
                item.PlayList.Add(new ScheduleItem.PlayListItem { Path = audioFiles[nextIndex].Replace('\\', '/') });
                filePathItem.LastPlayedFile = audioFiles[nextIndex];
            }
            else
            {
                foreach (var audioFile in audioFiles)
                {
                    item.PlayList.Add(new ScheduleItem.PlayListItem { Path = audioFile.Replace('\\', '/') });
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogMessage($"Error processing folder: {filePathItem.Path}, {ex.Message}");
        }
    }
}
