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
                timer.AutoReset = false; // Ensure this is set to false for one-time triggers
                timer.Elapsed += (sender, e) => OnPlaylistStart(scheduleItem);
                timer.Start();
                _timers.Add(timer);

                Logger.LogMessage($"Timer set for Item {scheduleItem.ItemId} at {startTime}");
            }
        }
    }

    private void OnPlaylistStart(ScheduleItem scheduleItem)
    {
        BeforePlayback?.Invoke();
        _audioPlayer.Play(scheduleItem.FilePaths);
        AfterPlayback?.Invoke();

        // If the schedule is periodic, set up the next timer
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

                Logger.LogMessage($"Next timer set for Item {scheduleItem.ItemId} at {nextOccurrence}");
            }
        }
    }

    private DateTime GetNextOccurrence(ScheduleItem scheduleItem, DateTime now, DateTime endTime)
    {
        DateTime nextOccurrence = DateTime.MinValue;

        if (scheduleItem.Type == "Periodic")
        {
            // Handle periodic schedules
            if (scheduleItem.Minute.StartsWith("*/"))
            {
                int interval = int.Parse(scheduleItem.Minute.Substring(2)); // Extract the interval (e.g., 1)
                int currentMinute = now.Minute;
                int nextMinute = (currentMinute / interval + 1) * interval; // Calculate the next minute

                if (nextMinute >= 60)
                {
                    nextMinute = 0;
                    now = now.AddHours(1);
                }

                nextOccurrence = new DateTime(now.Year, now.Month, now.Day, now.Hour, nextMinute, 0);
            }
            else
            {
                // Handle fixed minute values
                if (!MatchesCronField(scheduleItem.Minute, now.Minute.ToString()))
                {
                    now = now.AddMinutes(1);
                    nextOccurrence = new DateTime(now.Year, now.Month, now.Day, now.Hour, int.Parse(scheduleItem.Minute), 0);
                }
                else
                {
                    nextOccurrence = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0);
                }
            }
        }

        return nextOccurrence;
    }

    private bool MatchesCronField(string cronField, string value)
    {
        if (cronField == "*") return true;
        if (cronField.StartsWith("*/"))
        {
            int interval = int.Parse(cronField.Substring(2)); // Extract the interval (e.g., 1)
            int currentValue = int.Parse(value);
            return currentValue % interval == 0; // Check if the current value is a multiple of the interval
        }
        return cronField == value;
    }

    private void OnPlaylistFinished()
    {
        AfterPlayback?.Invoke();
    }

    public List<ScheduleItem> GetScheduledItems()
    {
        return _scheduleItems;
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