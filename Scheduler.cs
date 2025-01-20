// Be Naame Khoda
// FileName: Scheduler.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Speech.Synthesis;
using System.Timers;
using NAudio.Wave;
using Newtonsoft.Json;
using RadioScheduler.Entities;
using static Enums;

public class Scheduler
{
    private AudioPlayer _audioPlayer;
    private Timer _checkTimer; // Single timer to check schedules every second
    public SchedulerConfigManager _configManager;


    public Scheduler()
    {
        _audioPlayer = new AudioPlayer();
        _configManager = new SchedulerConfigManager(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "audio.conf"));


        // Set up the single timer to check schedules every second
        _checkTimer = new Timer(1000); // 1-second interval
        _checkTimer.Elapsed += OnCheckTimerElapsed;
        _checkTimer.AutoReset = true;
        _checkTimer.Enabled = true;


        _configManager.ConfigReloaded += OnConfigReloaded;

    }
    private void OnConfigReloaded()
    {
        Logger.LogMessage("Configuration reloaded.");
        foreach (var item in _configManager.ScheduleItems)
        {
            item.NextOccurrence = GetNextOccurrence(item, DateTime.Now);
        }
    }
    /// <summary>
    /// Handles the timer tick event to check for scheduled tasks.
    /// </summary>
    private void OnCheckTimerElapsed(object sender, ElapsedEventArgs e)
    {
        DateTime now = DateTime.Now;
        DateTime currentDateTimeTruncated = TruncateDateTimeToSeconds(now);


        foreach (var item in _configManager.ScheduleItems)
        {
            //Check if item is canceled
            if (item.Status == ScheduleStatus.Canceled)
                continue;

            DateTime nextOccurrenceTruncated = TruncateDateTimeToSeconds(item.NextOccurrence);

            if (nextOccurrenceTruncated <= currentDateTimeTruncated)
            {
                if (item.Type == ScheduleType.Periodic)
                {
                    HandlePeriodicItem(item, now, currentDateTimeTruncated, nextOccurrenceTruncated);
                }
                else
                {
                    UpdateNonPeriodicNextOccurrence(item, currentDateTimeTruncated, nextOccurrenceTruncated, now);
                    if (nextOccurrenceTruncated == currentDateTimeTruncated)
                    {
                        HandlePlayback(item, now);
                    }
                }
            }
            else
            {
                if (item.Type == ScheduleType.Periodic)
                {
                    item.Status = ScheduleStatus.TimeWaiting;
                }
                else
                {
                    item.Status = ScheduleStatus.EventWaiting;
                }
            }
        }
    }

    private void HandlePeriodicItem(ScheduleItem item, DateTime now, DateTime currentDateTimeTruncated, DateTime nextOccurrenceTruncated)
    {
        if (nextOccurrenceTruncated == currentDateTimeTruncated)
        {
            HandlePlayback(item, now);
        }
        item.NextOccurrence = GetNextOccurrence(item, now);
    }
    private void HandlePlayback(ScheduleItem item, DateTime now)
    {
        if (_audioPlayer.IsPlaying)
        {
            OnConflictOccurred(item);
            _audioPlayer.Stop(); // Stop the current playback
        }
        item.Status = ScheduleStatus.Playing;
        HandlePlaylistPlayback(item);
        item.LastPlayTime = now;
    }
    private void UpdateNonPeriodicNextOccurrence(ScheduleItem item, DateTime currentDateTimeTruncated, DateTime nextOccurrenceTruncated, DateTime now)
    {
        foreach (var trigger in ActiveTriggers.Triggers)
        {
            if (nextOccurrenceTruncated == currentDateTimeTruncated &&
                 !string.IsNullOrEmpty(item.Trigger) &&
                    item.Trigger.Equals(trigger.Event, StringComparison.OrdinalIgnoreCase))
            {
                if (item.TriggerTime != trigger.Time)
                {
                    item.TriggerTime = trigger.Time;
                    if (item.TriggerType == TriggerTypes.Immediate)
                    {
                        item.NextOccurrence = trigger.Time.Value;
                    }
                    else if (item.TriggerType == TriggerTypes.Delayed)
                    {
                        if (TimeSpan.TryParse(item.DelayTime, out TimeSpan delay))
                        {
                            item.NextOccurrence = trigger.Time.Value.Add(delay);
                        }
                        else
                        {
                            Logger.LogMessage($"Invalid DelayTime '{item.DelayTime}' for  schedule item  '{item.Name}'.");
                            item.NextOccurrence = DateTime.MaxValue;
                        }

                    }
                    else if (item.TriggerType == TriggerTypes.Timed)
                    {
                        item.NextOccurrence = trigger.Time.Value.Add(-item.TotalDuration);
                    }
                }
                else
                {
                    if (item.TriggerType == TriggerTypes.Immediate || item.TriggerType == TriggerTypes.Timed)
                    {
                        item.NextOccurrence = trigger.Time.Value;
                    }
                    else if (item.TriggerType == TriggerTypes.Delayed)
                    {
                        if (TimeSpan.TryParse(item.DelayTime, out TimeSpan delay))
                        {
                            item.NextOccurrence = trigger.Time.Value.Add(delay);
                        }
                        else
                        {
                            Logger.LogMessage($"Invalid DelayTime '{item.DelayTime}' for  schedule item  '{item.Name}'.");
                            item.NextOccurrence = DateTime.MaxValue;
                        }
                    }
                }

                break;
            }
        }
    }
    private void OnConflictOccurred(object conflictData)
    {
        if (conflictData is ScheduleItem item)
        {
            string conflictMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Conflict: Playlist '{item.Name}' scheduled but another playlist is already playing.";
            Logger.LogMessage(conflictMessage);
            Console.WriteLine(conflictMessage);
        }
        else
        {
            Logger.LogMessage($"Conflict detected, but no data was provided");
            Console.WriteLine("Conflict detected, but no data was provided");
        }
    }
    private DateTime TruncateDateTimeToSeconds(DateTime dateTime)
    {
        return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, dateTime.Second);
    }


    /// <summary>
    /// Handles the start of a scheduled playlist.
    /// </summary>
    private void HandlePlaylistPlayback(ScheduleItem scheduleItem)
    {
        // Log the playlist name and time to the Visual Studio console
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Playing playlist: {scheduleItem.Name}");

        // Log the event to the application log
        Logger.LogMessage($"Starting playback for schedule: {scheduleItem.Name}");

        // Stop the current playlist (if any)
        _audioPlayer.Stop();

        // Start the new playlist
        _audioPlayer.Play(scheduleItem.FilePaths);
    }

    /// <summary>
    /// Gets the list of scheduled items.
    /// </summary>
    /// <returns>A list of scheduled items.</returns>
    public List<ScheduleItem> GetScheduledItems()
    {
        // Return a copy of the list to avoid modifying the original
        return _configManager.ScheduleItems
             .OrderBy(item => item.NextOccurrence)
            .Select(item => {
                if (item.Status != ScheduleStatus.Played && item.NextOccurrence <= DateTime.Now)
                    item.Status = ScheduleStatus.TimeWaiting;

                return item;
            })
             .ToList();
    }

    /// <summary>
    /// Calculates the next occurrence of a schedule item.
    /// </summary>
    private DateTime GetNextOccurrence(ScheduleItem item, DateTime now)
    {
        if (item.Type == ScheduleType.Periodic)
        {
            DateTime nextOccurrence = now;

            // Handle Second field
            if (item.Second.StartsWith("*/"))
            {
                int interval = int.Parse(item.Second.Substring(2));
                nextOccurrence = nextOccurrence.AddSeconds(interval);
            }
            else if (item.Second != "*")
            {
                int targetSecond = int.Parse(item.Second);
                if (targetSecond <= now.Second)
                {
                    nextOccurrence = nextOccurrence.AddMinutes(1);
                    nextOccurrence = new DateTime(nextOccurrence.Year, nextOccurrence.Month, nextOccurrence.Day, nextOccurrence.Hour, nextOccurrence.Minute, targetSecond);

                }
                else
                {
                    nextOccurrence = new DateTime(nextOccurrence.Year, nextOccurrence.Month, nextOccurrence.Day, nextOccurrence.Hour, nextOccurrence.Minute, targetSecond);
                }
            }

            // Handle Minute field
            if (item.Minute.StartsWith("*/"))
            {
                int interval = int.Parse(item.Minute.Substring(2));
                nextOccurrence = nextOccurrence.AddMinutes(interval);
            }
            else if (item.Minute != "*")
            {
                int targetMinute = int.Parse(item.Minute);
                if (targetMinute <= now.Minute)
                {
                    nextOccurrence = nextOccurrence.AddHours(1);
                    nextOccurrence = new DateTime(nextOccurrence.Year, nextOccurrence.Month, nextOccurrence.Day, nextOccurrence.Hour, targetMinute, nextOccurrence.Second);
                }
                else
                {
                    nextOccurrence = new DateTime(nextOccurrence.Year, nextOccurrence.Month, nextOccurrence.Day, nextOccurrence.Hour, targetMinute, nextOccurrence.Second);

                }
            }
            // Handle Hour field
            if (item.Hour.StartsWith("*/"))
            {
                int interval = int.Parse(item.Hour.Substring(2));
                nextOccurrence = nextOccurrence.AddHours(interval);
            }
            else if (item.Hour != "*")
            {
                int targetHour = int.Parse(item.Hour);
                if (targetHour <= now.Hour)
                {
                    nextOccurrence = nextOccurrence.AddDays(1);
                    nextOccurrence = new DateTime(nextOccurrence.Year, nextOccurrence.Month, nextOccurrence.Day, targetHour, nextOccurrence.Minute, nextOccurrence.Second);

                }
                else
                {
                    nextOccurrence = new DateTime(nextOccurrence.Year, nextOccurrence.Month, nextOccurrence.Day, targetHour, nextOccurrence.Minute, nextOccurrence.Second);

                }
            }
            // Handle DayOfMonth field
            if (item.DayOfMonth != "*")
            {
                int targetDay = int.Parse(item.DayOfMonth);
                if (targetDay <= now.Day)
                {
                    nextOccurrence = nextOccurrence.AddMonths(1);

                }
                nextOccurrence = new DateTime(nextOccurrence.Year, nextOccurrence.Month, targetDay, nextOccurrence.Hour, nextOccurrence.Minute, nextOccurrence.Second);


            }

            // Handle Month field
            if (item.Month != "*")
            {
                int targetMonth = int.Parse(item.Month);
                if (targetMonth <= now.Month)
                {
                    nextOccurrence = nextOccurrence.AddYears(1);
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
        return DateTime.MaxValue;
    }
}