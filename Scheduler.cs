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
                            if (scheduleItem.Type == "Periodic")
                            {
                                SetupTimerForScheduleItem(scheduleItem);
                            }
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
        else
        {
            Logger.LogMessage($"No valid occurrence found for schedule '{scheduleItem.Name}'.");
        }
    }

    private DateTime GetNextOccurrence(ScheduleItem scheduleItem, DateTime now)
    {
        if (scheduleItem.Type == "Periodic")
        {
            // Start with the current time
            DateTime nextOccurrence = now;

            // Handle Second field
            if (scheduleItem.Second.StartsWith("*/"))
            {
                int interval = int.Parse(scheduleItem.Second.Substring(2)); // Extract "30" from "*/30"
                int currentSecond = now.Second;

                // Calculate the next second
                int nextSecond = (currentSecond / interval + 1) * interval;
                if (nextSecond >= 60)
                {
                    nextSecond = 0; // Wrap around to the next minute
                    nextOccurrence = nextOccurrence.AddMinutes(1);
                }

                nextOccurrence = nextOccurrence.AddSeconds(nextSecond);
            }
            else if (scheduleItem.Second != "*")
            {
                // Specific second (e.g., "30" for the 30th second)
                int targetSecond = int.Parse(scheduleItem.Second);
                if (targetSecond < now.Second)
                {
                    nextOccurrence = nextOccurrence.AddMinutes(1); // Move to the next minute
                }
                nextOccurrence = nextOccurrence.AddSeconds(targetSecond - now.Second);
            }

            // Handle Minute field
            if (scheduleItem.Minute.StartsWith("*/"))
            {
                int interval = int.Parse(scheduleItem.Minute.Substring(2)); // Extract "5" from "*/5"
                int currentMinute = now.Minute;

                // Calculate the next minute
                int nextMinute = (currentMinute / interval + 1) * interval;
                if (nextMinute >= 60)
                {
                    nextMinute = 0; // Wrap around to the next hour
                    nextOccurrence = nextOccurrence.AddHours(1);
                }

                nextOccurrence = nextOccurrence.AddMinutes(nextMinute);
            }
            else if (scheduleItem.Minute != "*")
            {
                // Specific minute (e.g., "15" for the 15th minute)
                int targetMinute = int.Parse(scheduleItem.Minute);
                if (targetMinute < now.Minute)
                {
                    nextOccurrence = nextOccurrence.AddHours(1); // Move to the next hour
                }
                nextOccurrence = nextOccurrence.AddMinutes(targetMinute - now.Minute);
            }

            // Handle Hour field
            if (scheduleItem.Hour.StartsWith("*/"))
            {
                int interval = int.Parse(scheduleItem.Hour.Substring(2)); // Extract "2" from "*/2"
                int currentHour = now.Hour;

                // Calculate the next hour
                int nextHour = (currentHour / interval + 1) * interval;
                if (nextHour >= 24)
                {
                    nextHour = 0; // Wrap around to the next day
                    nextOccurrence = nextOccurrence.AddDays(1);
                }

                nextOccurrence = nextOccurrence.AddHours(nextHour);
            }
            else if (scheduleItem.Hour != "*")
            {
                // Specific hour (e.g., "14" for 2 PM)
                int targetHour = int.Parse(scheduleItem.Hour);
                if (targetHour < now.Hour)
                {
                    nextOccurrence = nextOccurrence.AddDays(1); // Move to the next day
                }
                nextOccurrence = nextOccurrence.AddHours(targetHour - now.Hour);
            }

            // Handle DayOfMonth field
            if (scheduleItem.DayOfMonth != "*")
            {
                int targetDay = int.Parse(scheduleItem.DayOfMonth);
                if (targetDay < now.Day)
                {
                    nextOccurrence = nextOccurrence.AddMonths(1); // Move to the next month
                }
                nextOccurrence = new DateTime(nextOccurrence.Year, nextOccurrence.Month, targetDay, nextOccurrence.Hour, nextOccurrence.Minute, nextOccurrence.Second);
            }

            // Handle Month field
            if (scheduleItem.Month != "*")
            {
                int targetMonth = int.Parse(scheduleItem.Month);
                if (targetMonth < now.Month)
                {
                    nextOccurrence = nextOccurrence.AddYears(1); // Move to the next year
                }
                nextOccurrence = new DateTime(nextOccurrence.Year, targetMonth, nextOccurrence.Day, nextOccurrence.Hour, nextOccurrence.Minute, nextOccurrence.Second);
            }

            // Handle DayOfWeek field
            if (scheduleItem.DayOfWeek != "*")
            {
                int targetDayOfWeek = int.Parse(scheduleItem.DayOfWeek);
                int currentDayOfWeek = (int)now.DayOfWeek;

                // Calculate the next occurrence of the target day of the week
                int daysToAdd = (targetDayOfWeek - currentDayOfWeek + 7) % 7;
                if (daysToAdd == 0 && nextOccurrence <= now)
                {
                    daysToAdd = 7; // Move to the next week
                }
                nextOccurrence = nextOccurrence.AddDays(daysToAdd);
            }

            // Ensure the next occurrence is in the future
            if (nextOccurrence <= now)
            {
                return DateTime.MaxValue; // No valid occurrence found
            }

            return nextOccurrence;
        }
        else if (scheduleItem.Type == "NonPeriodic")
        {
            // Non-periodic items are not automatically scheduled
            return DateTime.MaxValue;
        }

        // Default: No valid occurrence
        return DateTime.MaxValue;
    }

    /// <summary>
    /// Manually triggers a non-periodic schedule item by its ItemId.
    /// </summary>
    /// <param name="itemId">The ID of the schedule item to trigger.</param>
    public void TriggerNonPeriodicItem(int itemId)
    {
        lock (_configLock)
        {
            var scheduleItem = _scheduleItems.Find(item => item.ItemId == itemId);
            if (scheduleItem != null && scheduleItem.Type == "NonPeriodic")
            {
                Logger.LogMessage($"Manually triggering non-periodic item: {scheduleItem.Name}");
                OnPlaylistStart(scheduleItem);
            }
            else
            {
                Logger.LogMessage($"Non-periodic item with ID {itemId} not found.");
            }
        }
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