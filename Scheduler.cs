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
                            item.NextOccurrence = GetNextOccurrence(item, DateTime.Now); // Initialize NextOccurrence
                        }

                        _scheduleItems = newItems;

                        Logger.LogMessage($"Loaded {_scheduleItems.Count} schedule items.");

                        // Clear existing timers
                        foreach (var timer in _timers)
                        {
                            timer.Stop();
                            timer.Dispose();
                        }
                        _timers.Clear();

                        // Set up timers for each schedule item
                        foreach (var scheduleItem in _scheduleItems)
                        {
                            SetupTimerForScheduleItem(scheduleItem);
                        }

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

    private void SetupTimerForScheduleItem(ScheduleItem scheduleItem)
    {
        DateTime now = DateTime.Now;
        DateTime nextOccurrence = GetNextOccurrence(scheduleItem, now);

        if (nextOccurrence > now)
        {
            double delay = (nextOccurrence - now).TotalMilliseconds;

            Timer timer = new Timer(delay);
            timer.AutoReset = false; // Only trigger once
            timer.Elapsed += (sender, e) => OnPlaylistStart(scheduleItem);
            timer.Start();
            _timers.Add(timer);

            Logger.LogMessage($"Timer set for schedule '{scheduleItem.Name}' at {nextOccurrence}");
        }
    }

    private DateTime GetNextOccurrence(ScheduleItem scheduleItem, DateTime now)
    {
        if (scheduleItem.Type == "Periodic")
        {
            if (scheduleItem.Second.StartsWith("*/"))
            {
                int interval = int.Parse(scheduleItem.Second.Substring(2)); // Extract "30" from "*/30"
                int currentSecond = now.Second;

                // Calculate the next occurrence
                int nextSecond = (currentSecond / interval + 1) * interval;
                DateTime nextOccurrence = now.Date // Start from the beginning of the day
                    .AddHours(now.Hour) // Add current hour
                    .AddMinutes(now.Minute) // Add current minute
                    .AddSeconds(nextSecond); // Add next second

                // Ensure the next occurrence is in the future
                if (nextOccurrence <= now)
                {
                    nextOccurrence = nextOccurrence.AddMinutes(1); // Move to the next minute
                }

                return nextOccurrence;
            }
        }

        // Default: Return a far future date if no valid schedule is found
        return DateTime.MaxValue;
    }

    private void OnPlaylistStart(ScheduleItem scheduleItem)
    {
        Logger.LogMessage($"Starting playback for schedule: {scheduleItem.Name}");
        BeforePlayback?.Invoke();

        // Stop the current playlist (if any)
        _audioPlayer.Stop();

        // Start the new playlist
        _audioPlayer.Play(scheduleItem.FilePaths);

        AfterPlayback?.Invoke();

        // If the schedule is periodic, set up the next occurrence
        if (scheduleItem.Type == "Periodic")
        {
            DateTime nextOccurrence = GetNextOccurrence(scheduleItem, DateTime.Now);
            if (nextOccurrence != DateTime.MaxValue)
            {
                SetupTimerForScheduleItem(scheduleItem);
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

        List<ScheduleItem> upcomingItems = new List<ScheduleItem>();

        foreach (var item in _scheduleItems)
        {
            DateTime nextOccurrence = GetNextOccurrence(item, now);
            if (nextOccurrence != DateTime.MaxValue)
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