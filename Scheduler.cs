// Be Naame Khoda
// FileName: Scheduler.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Timers;
using NAudio.Wave;
using Newtonsoft.Json;
using RadioScheduler.Entities;

public class Scheduler
{
    private readonly object _lock = new object();
    private List<ScheduleItem> _scheduleItems;
    private string _configFilePath;
    private List<Timer> _timers;
    private AudioPlayer _audioPlayer;

    public Scheduler()
    {
        _scheduleItems = new List<ScheduleItem>();
        _timers = new List<Timer>();
        _configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "audio.conf");
        _audioPlayer = new AudioPlayer(); // Initialize the AudioPlayer
        ReloadScheduleConfig(); // Load the schedule configuration on initialization
    }

    /// <summary>
    /// Reloads the schedule configuration from the audio.conf file.
    /// </summary>
    public void ReloadScheduleConfig()
    {
        lock (_lock)
        {
            try
            {
                if (File.Exists(_configFilePath))
                {
                    string json = File.ReadAllText(_configFilePath);
                    var newItems = JsonConvert.DeserializeObject<List<ScheduleItem>>(json);

                    // Log the number of items loaded
                    Logger.LogMessage($"Loaded {newItems.Count} items from {_configFilePath}.");

                    // Validate and update the schedule items
                    _scheduleItems.Clear();
                    foreach (var item in newItems)
                    {
                        item.Validate();
                        item.NextOccurrence = GetNextOccurrence(item, DateTime.Now);

                        // Calculate the total duration of the playlist
                        item.TotalDuration = CalculateTotalDuration(item.FilePaths);

                        _scheduleItems.Add(item);

                        // Log each item
                        Logger.LogMessage($"Added item: {item.Name}, NextOccurrence: {item.NextOccurrence}, TotalDuration: {item.TotalDuration}");
                    }

                    // Set up timers for periodic schedules
                    foreach (var timer in _timers)
                    {
                        timer.Stop();
                        timer.Dispose();
                    }
                    _timers.Clear();

                    foreach (var scheduleItem in _scheduleItems)
                    {
                        if (scheduleItem.Type == "Periodic")
                        {
                            SetupTimerForScheduleItem(scheduleItem);
                        }
                    }
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
    }

    /// <summary>
    /// Calculates the total duration of a playlist.
    /// </summary>
    /// <param name="filePaths">The list of file paths in the playlist.</param>
    /// <returns>The total duration of the playlist.</returns>
    private TimeSpan CalculateTotalDuration(List<FilePathItem> filePaths)
    {
        TimeSpan totalDuration = TimeSpan.Zero;

        foreach (var filePathItem in filePaths)
        {
            if (!string.IsNullOrEmpty(filePathItem.Text)) // Handle TTS
            {
                // Estimate TTS duration (e.g., 10 characters per second)
                filePathItem.Duration = TimeSpan.FromSeconds(filePathItem.Text.Length / 10.0);
            }
            else if (File.Exists(filePathItem.Path)) // Handle audio file
            {
                try
                {
                    using (var reader = new AudioFileReader(filePathItem.Path))
                    {
                        filePathItem.Duration = reader.TotalTime;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogMessage($"Error calculating duration for file {filePathItem.Path}: {ex.Message}");
                    filePathItem.Duration = TimeSpan.Zero; // Default to zero if an error occurs
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
                            filePathItem.Duration += reader.TotalTime;
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

        return totalDuration;
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

    private void OnPlaylistStart(ScheduleItem scheduleItem)
    {
        Logger.LogMessage($"Starting playback for schedule: {scheduleItem.Name}");

        // Stop the current playlist (if any)
        _audioPlayer.Stop();

        // Start the new playlist
        _audioPlayer.Play(scheduleItem.FilePaths);

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

    public void AddScheduleItem(ScheduleItem item)
    {
        lock (_lock)
        {
            // Calculate and cache the NextOccurrence when adding the item
            item.NextOccurrence = GetNextOccurrence(item, DateTime.Now);
            _scheduleItems.Add(item);
        }
    }

    public List<ScheduleItem> GetScheduledItems()
    {
        lock (_lock)
        {
            // Return a copy of the list to avoid modifying the original
            return new List<ScheduleItem>(_scheduleItems);
        }
    }

    /// <summary>
    /// Calculates the next occurrence of a schedule item.
    /// </summary>
    /// <param name="item">The schedule item.</param>
    /// <param name="now">The current date and time.</param>
    /// <returns>The next occurrence of the schedule item.</returns>
    private DateTime GetNextOccurrence(ScheduleItem item, DateTime now)
    {
        if (item.Type == "Periodic")
        {
            // Start with the current time
            DateTime nextOccurrence = now;

            // Handle Second field
            if (item.Second.StartsWith("*/"))
            {
                int interval = int.Parse(item.Second.Substring(2)); // Extract "30" from "*/30"
                int currentSecond = now.Second;

                // Calculate the next second
                int nextSecond = (currentSecond / interval + 1) * interval;
                if (nextSecond >= 60)
                {
                    nextSecond = 0; // Wrap around to the next minute
                    nextOccurrence = nextOccurrence.AddMinutes(1);
                }

                nextOccurrence = nextOccurrence.AddSeconds(nextSecond - currentSecond);
            }
            else if (item.Second != "*")
            {
                // Specific second (e.g., "30" for the 30th second)
                int targetSecond = int.Parse(item.Second);
                if (targetSecond < now.Second)
                {
                    nextOccurrence = nextOccurrence.AddMinutes(1); // Move to the next minute
                }
                nextOccurrence = nextOccurrence.AddSeconds(targetSecond - now.Second);
            }

            // Handle Minute field
            if (item.Minute.StartsWith("*/"))
            {
                int interval = int.Parse(item.Minute.Substring(2)); // Extract "5" from "*/5"
                int currentMinute = now.Minute;

                // Calculate the next minute
                int nextMinute = (currentMinute / interval + 1) * interval;
                if (nextMinute >= 60)
                {
                    nextMinute = 0; // Wrap around to the next hour
                    nextOccurrence = nextOccurrence.AddHours(1);
                }

                nextOccurrence = nextOccurrence.AddMinutes(nextMinute - currentMinute);
            }
            else if (item.Minute != "*")
            {
                // Specific minute (e.g., "15" for the 15th minute)
                int targetMinute = int.Parse(item.Minute);
                if (targetMinute < now.Minute)
                {
                    nextOccurrence = nextOccurrence.AddHours(1); // Move to the next hour
                }
                nextOccurrence = nextOccurrence.AddMinutes(targetMinute - now.Minute);
            }

            // Handle Hour field
            if (item.Hour.StartsWith("*/"))
            {
                int interval = int.Parse(item.Hour.Substring(2)); // Extract "2" from "*/2"
                int currentHour = now.Hour;

                // Calculate the next hour
                int nextHour = (currentHour / interval + 1) * interval;
                if (nextHour >= 24)
                {
                    nextHour = 0; // Wrap around to the next day
                    nextOccurrence = nextOccurrence.AddDays(1);
                }

                nextOccurrence = nextOccurrence.AddHours(nextHour - currentHour);
            }
            else if (item.Hour != "*")
            {
                // Specific hour (e.g., "14" for 2 PM)
                int targetHour = int.Parse(item.Hour);
                if (targetHour < now.Hour)
                {
                    nextOccurrence = nextOccurrence.AddDays(1); // Move to the next day
                }
                nextOccurrence = nextOccurrence.AddHours(targetHour - now.Hour);
            }

            // Handle DayOfMonth field
            if (item.DayOfMonth != "*")
            {
                int targetDay = int.Parse(item.DayOfMonth);
                if (targetDay < now.Day)
                {
                    nextOccurrence = nextOccurrence.AddMonths(1); // Move to the next month
                }
                nextOccurrence = new DateTime(nextOccurrence.Year, nextOccurrence.Month, targetDay, nextOccurrence.Hour, nextOccurrence.Minute, nextOccurrence.Second);
            }

            // Handle Month field
            if (item.Month != "*")
            {
                int targetMonth = int.Parse(item.Month);
                if (targetMonth < now.Month)
                {
                    nextOccurrence = nextOccurrence.AddYears(1); // Move to the next year
                }
                nextOccurrence = new DateTime(nextOccurrence.Year, targetMonth, nextOccurrence.Day, nextOccurrence.Hour, nextOccurrence.Minute, nextOccurrence.Second);
            }

            // Handle DayOfWeek field
            if (item.DayOfWeek != "*")
            {
                int targetDayOfWeek = int.Parse(item.DayOfWeek);
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
        else if (item.Type == "NonPeriodic")
        {
            // Non-periodic items are not automatically scheduled
            return DateTime.MaxValue;
        }

        // Default: No valid occurrence
        return DateTime.MaxValue;
    }
}