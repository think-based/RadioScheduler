// Be Naame Khoda
// FileName: Scheduler.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Timers;
using Newtonsoft.Json;

public class Scheduler
{
    public event Action BeforePlayback;
    public event Action AfterPlayback;

    private AudioPlayer _audioPlayer;
    private CurrentTrigger _currentTrigger;
    private List<ScheduleItem> _scheduleItems;
    private readonly object _configLock = new object();
    private string _configFilePath;
    private List<Timer> _timers;
    private string _lastConfigHash;
    private FileSystemWatcher _configWatcher;

    public Scheduler()
    {
        _audioPlayer = new AudioPlayer();
        _audioPlayer.PlaylistFinished += OnPlaylistFinished;
        _currentTrigger = new CurrentTrigger();
        _scheduleItems = new List<ScheduleItem>();
        _timers = new List<Timer>();

        _configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "audio.conf");
        EnsureConfigFileExists();

        // Set _lastConfigHash to null initially
        _lastConfigHash = null;

        Logger.LogMessage("Radio Scheduler Service started.");

        _configWatcher = new FileSystemWatcher
        {
            Path = Path.GetDirectoryName(_configFilePath),
            Filter = Path.GetFileName(_configFilePath),
            NotifyFilter = NotifyFilters.LastWrite
        };

        _configWatcher.Changed += OnConfigFileChanged;
        _configWatcher.EnableRaisingEvents = true;

        // Force the initial load of the config file
        ReloadScheduleConfig();
    }

    private void EnsureConfigFileExists()
    {
        if (!File.Exists(_configFilePath))
        {
            try
            {
                File.WriteAllText(_configFilePath, "[]");
                Logger.LogMessage("Config file created: audio.conf");
            }
            catch (Exception ex)
            {
                Logger.LogMessage($"Error creating config file: {ex.Message}");
            }
        }
    }

    private void OnConfigFileChanged(object sender, FileSystemEventArgs e)
    {
        Logger.LogMessage("Config file changed. Reloading...");
        ReloadScheduleConfig();
    }

    private bool HasConfigChanged()
    {
        if (_lastConfigHash == null)
        {
            return true; // Force reload if _lastConfigHash is null
        }

        string currentHash = FileHashHelper.CalculateFileHash(_configFilePath);
        return currentHash != _lastConfigHash;
    }

    public void ReloadScheduleConfig()
    {
        lock (_configLock)
        {
            try
            {
                // If _lastConfigHash is null, force a reload
                if (_lastConfigHash == null || HasConfigChanged())
                {
                    if (File.Exists(_configFilePath))
                    {
                        string json = File.ReadAllText(_configFilePath);
                        var newItems = JsonConvert.DeserializeObject<List<ScheduleItem>>(json);

                        foreach (var item in newItems)
                        {
                            item.Validate();
                            item.NextOccurrence = GetNextOccurrence(item, DateTime.Now, DateTime.Now.AddHours(24)); // Initialize NextOccurrence
                        }

                        _scheduleItems = newItems;

                        Logger.LogMessage($"Loaded {_scheduleItems.Count} schedule items.");

                        foreach (var timer in _timers)
                        {
                            timer.Stop();
                            timer.Dispose();
                        }
                        _timers.Clear();

                        SetupTimers();

                        // Calculate the hash after loading the config file
                        _lastConfigHash = FileHashHelper.CalculateFileHash(_configFilePath);

                        Logger.LogMessage("Config reloaded successfully.");
                    }
                    else
                    {
                        Logger.LogMessage("Config file not found!");
                    }
                }
                else
                {
                    Logger.LogMessage("Config file has not changed. Skipping reload.");
                }
            }
            catch (Exception ex)
            {
                Logger.LogMessage($"Error reloading config: {ex.Message}");
            }
        }
    }

    private void SetupTimers()
    {
        DateTime now = DateTime.Now;
        DateTime endTime = now.AddHours(24);

        foreach (var scheduleItem in _scheduleItems)
        {
            DateTime nextOccurrence = GetNextOccurrence(scheduleItem, now, endTime);
            if (nextOccurrence != DateTime.MinValue)
            {
                TimeSpan totalDuration = _audioPlayer.CalculateTotalDuration(scheduleItem.FilePaths);

                DateTime startTime;
                if (scheduleItem.TriggerType == "Immediate")
                {
                    startTime = nextOccurrence;
                }
                else if (scheduleItem.TriggerType == "Timed")
                {
                    startTime = nextOccurrence - totalDuration;
                }
                else if (scheduleItem.TriggerType == "Delayed")
                {
                    startTime = nextOccurrence.AddMinutes(scheduleItem.DelayMinutes ?? 0);
                }
                else
                {
                    Logger.LogMessage($"Invalid TriggerType for ItemID: {scheduleItem.ItemId}");
                    continue;
                }

                if (startTime < now)
                {
                    continue;
                }

                double delay = (startTime - now).TotalMilliseconds;

                Timer timer = new Timer(delay);
                timer.AutoReset = false;
                timer.Elapsed += (sender, e) => OnPlaylistStart(scheduleItem);
                timer.Start();
                _timers.Add(timer);

                Logger.LogMessage($"Timer set for schedule '{scheduleItem.Name}' at {startTime}");
            }
        }
    }

    private DateTime GetNextOccurrence(ScheduleItem scheduleItem, DateTime now, DateTime endTime)
    {
        if (scheduleItem.Type == "Periodic")
        {
            if (scheduleItem.Second.StartsWith("*/"))
            {
                int interval = int.Parse(scheduleItem.Second.Substring(2)); // Extract "90" from "*/90"
                int currentSecond = now.Second;

                // Calculate the total seconds since the last full interval
                int totalSeconds = now.Minute * 60 + currentSecond;
                int nextTotalSeconds = (totalSeconds / interval + 1) * interval;

                // Calculate the next occurrence
                DateTime nextOccurrence = now.Date // Start from the beginning of the day
                    .AddHours(now.Hour) // Add current hour
                    .AddMinutes(nextTotalSeconds / 60) // Add minutes
                    .AddSeconds(nextTotalSeconds % 60); // Add seconds

                // Ensure the next occurrence is within the valid range
                if (nextOccurrence < now || nextOccurrence > endTime)
                {
                    return DateTime.MinValue; // Out of range
                }

                return nextOccurrence;
            }
        }
        return DateTime.MinValue;
    }

    private void OnPlaylistStart(ScheduleItem scheduleItem)
    {
        Logger.LogMessage($"Starting playback for schedule: {scheduleItem.Name}");
        BeforePlayback?.Invoke();
        _audioPlayer.Play(scheduleItem.FilePaths);
        AfterPlayback?.Invoke();

        if (scheduleItem.Type == "Periodic")
        {
            DateTime now = DateTime.Now;
            DateTime nextOccurrence = GetNextOccurrence(scheduleItem, now, now.AddHours(24));

            if (nextOccurrence != DateTime.MinValue)
            {
                double delay = (nextOccurrence - now).TotalMilliseconds;
                Timer timer = new Timer(delay);
                timer.AutoReset = false;
                timer.Elapsed += (sender, e) => OnPlaylistStart(scheduleItem);
                timer.Start();
                _timers.Add(timer);

                Logger.LogMessage($"Next timer set for schedule '{scheduleItem.Name}' at {nextOccurrence}");
            }
        }
    }

    private void OnPlaylistFinished()
    {
        AfterPlayback?.Invoke();
    }

    public List<ScheduleItem> GetScheduledItems()
    {
        // Recalculate NextOccurrence for each item before returning the list
        DateTime now = DateTime.Now;
        DateTime endTime = now.AddHours(24);

        List<ScheduleItem> upcomingItems = new List<ScheduleItem>();

        foreach (var item in _scheduleItems)
        {
            DateTime nextOccurrence = GetNextOccurrence(item, now, endTime);
            if (nextOccurrence != DateTime.MinValue)
            {
                item.NextOccurrence = nextOccurrence;
                upcomingItems.Add(item);
            }
        }

        return upcomingItems;
    }

    public string GetCurrentPlaybackStatus()
    {
        if (_audioPlayer.IsPlaying)
        {
            return $"Currently playing: {_audioPlayer.CurrentFile}";
        }
        return "No playback in progress.";
    }
}