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

                Logger.LogMessage($"Timer set for Item {scheduleItem.ItemId} at {startTime}");
            }
        }
    }

    private DateTime GetNextOccurrence(ScheduleItem scheduleItem, DateTime now, DateTime endTime)
    {
        DateTime nextOccurrence = DateTime.MinValue;

        // Convert the current date to the specified calendar type
        DateTime convertedDate = CalendarHelper.ConvertDate(now, scheduleItem.CalendarType);

        // Check if the current date matches the DayOfMonth and Month fields
        bool isDayOfMonthMatch = MatchesCronField(scheduleItem.DayOfMonth, convertedDate.Day.ToString());
        bool isMonthMatch = MatchesCronField(scheduleItem.Month, convertedDate.Month.ToString());

        // Check if the current day of the week matches the DayOfWeek field
        int currentDayOfWeek = CalendarHelper.GetDayOfWeek(convertedDate, scheduleItem.Region);
        bool isDayOfWeekMatch = MatchesCronField(scheduleItem.DayOfWeek, currentDayOfWeek.ToString());

        // If any of the date fields don't match, return DateTime.MinValue
        if (!isDayOfMonthMatch || !isMonthMatch || !isDayOfWeekMatch)
        {
            Logger.LogMessage($"Date fields do not match for Item {scheduleItem.ItemId}.");
            return nextOccurrence;
        }

        if (scheduleItem.Type == "Periodic")
        {
            // Handle Second field
            if (scheduleItem.Second.StartsWith("*/"))
            {
                int interval = int.Parse(scheduleItem.Second.Substring(2)); // Extract the interval (e.g., 10)
                int currentSecond = now.Second;
                int nextSecond = (currentSecond / interval + 1) * interval; // Calculate the next second

                if (nextSecond >= 60)
                {
                    // If the next second exceeds 59, move to the next minute
                    nextSecond = 0;
                    now = now.AddMinutes(1);
                }

                // Set the next occurrence
                nextOccurrence = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, nextSecond);
            }
            else
            {
                // Handle fixed second values
                if (!MatchesCronField(scheduleItem.Second, now.Second.ToString()))
                {
                    // If the current second doesn't match, adjust the time to the next valid second
                    now = now.AddSeconds(1);
                    nextOccurrence = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, int.Parse(scheduleItem.Second));
                }
                else
                {
                    // If the current second matches, set the next occurrence to the current time
                    nextOccurrence = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second);
                }
            }

            // Handle Minute field
            if (scheduleItem.Minute.StartsWith("*/"))
            {
                int interval = int.Parse(scheduleItem.Minute.Substring(2)); // Extract the interval (e.g., 1)
                int currentMinute = now.Minute;
                int nextMinute = (currentMinute / interval + 1) * interval; // Calculate the next minute

                if (nextMinute >= 60)
                {
                    // If the next minute exceeds 59, move to the next hour
                    nextMinute = 0;
                    now = now.AddHours(1);
                }

                // Set the next occurrence
                nextOccurrence = new DateTime(now.Year, now.Month, now.Day, now.Hour, nextMinute, nextOccurrence.Second);
            }
            else
            {
                // Handle fixed minute values
                if (!MatchesCronField(scheduleItem.Minute, now.Minute.ToString()))
                {
                    // If the current minute doesn't match, adjust the time to the next valid minute
                    now = now.AddMinutes(1);
                    nextOccurrence = new DateTime(now.Year, now.Month, now.Day, now.Hour, int.Parse(scheduleItem.Minute), nextOccurrence.Second);
                }
            }

            // Handle Hour field
            if (scheduleItem.Hour.StartsWith("*/"))
            {
                int interval = int.Parse(scheduleItem.Hour.Substring(2)); // Extract the interval (e.g., 2)
                int currentHour = now.Hour;
                int nextHour = (currentHour / interval + 1) * interval; // Calculate the next hour

                if (nextHour >= 24)
                {
                    // If the next hour exceeds 23, move to the next day
                    nextHour = 0;
                    now = now.AddDays(1);
                }

                // Set the next occurrence
                nextOccurrence = new DateTime(now.Year, now.Month, now.Day, nextHour, nextOccurrence.Minute, nextOccurrence.Second);
            }
            else
            {
                // Handle fixed hour values
                if (!MatchesCronField(scheduleItem.Hour, now.Hour.ToString()))
                {
                    // If the current hour doesn't match, adjust the time to the next valid hour
                    now = now.AddHours(1);
                    nextOccurrence = new DateTime(now.Year, now.Month, now.Day, int.Parse(scheduleItem.Hour), nextOccurrence.Minute, nextOccurrence.Second);
                }
            }

            // Ensure the next occurrence is within the valid range
            if (nextOccurrence >= now && nextOccurrence <= endTime)
            {
                Logger.LogMessage($"Next occurrence for Item {scheduleItem.ItemId}: {nextOccurrence}");
                return nextOccurrence;
            }
            else
            {
                Logger.LogMessage($"Next occurrence is out of range: {nextOccurrence}");
            }
        }
        else if (scheduleItem.Type == "NonPeriodic")
        {
            // For NonPeriodic items, check the trigger
            if (string.IsNullOrEmpty(scheduleItem.Trigger) || scheduleItem.Trigger != _currentTrigger.Event)
            {
                Logger.LogMessage($"Trigger mismatch: {scheduleItem.Trigger} != {_currentTrigger.Event}");
                return nextOccurrence;
            }

            nextOccurrence = now;
        }

        Logger.LogMessage($"No valid next occurrence found. Returning DateTime.MinValue.");
        return DateTime.MinValue;
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

    private void OnPlaylistStart(ScheduleItem scheduleItem)
    {
        BeforePlayback?.Invoke();
        _audioPlayer.Play(scheduleItem.FilePaths);
        AfterPlayback?.Invoke();
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