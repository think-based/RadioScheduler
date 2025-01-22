      //Be Naame Khoda
//FileName: Scheduler.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Timers;
using NAudio.Wave;
using static Enums;

public class Scheduler
{
    private readonly IAudioPlayer _audioPlayer;
    private readonly ISchedulerConfigManager _configManager;
    private readonly IScheduleCalculatorFactory _scheduleCalculatorFactory;
    private readonly ITriggerManager _triggerManager;
    private Timer _checkTimer; // Single timer to check schedules every second

    public Scheduler(IAudioPlayer audioPlayer, ISchedulerConfigManager configManager, IScheduleCalculatorFactory scheduleCalculatorFactory, ITriggerManager triggerManager)
    {
        _audioPlayer = audioPlayer;
        _configManager = configManager;
        _scheduleCalculatorFactory = scheduleCalculatorFactory;
        _triggerManager = triggerManager;
        // Set up the single timer to check schedules every second
        _checkTimer = new Timer(1000); // 1-second interval
        _checkTimer.Elapsed += OnCheckTimerElapsed;
        _checkTimer.AutoReset = true;
        _checkTimer.Enabled = true;

        _configManager.ConfigReloaded += OnConfigReloaded;
    }

    public ISchedulerConfigManager ConfigManager => _configManager;
    private void OnConfigReloaded()
    {
        Logger.LogMessage("Configuration reloaded.");
        CheckScheduleItems();
    }
    /// <summary>
    /// Handles the timer tick event to check for scheduled tasks.
    /// </summary>
    private void OnCheckTimerElapsed(object sender, ElapsedEventArgs e)
    {
        CheckScheduleItems();
    }

    private void CheckScheduleItems()
    {
        DateTime now = DateTime.Now;
        DateTime convertedNow = CalendarHelper.ConvertToLocalTimeZone(now, Settings.Region);
        List<ScheduleItem> dueItems = GetDueScheduleItems(convertedNow);
        foreach (var item in dueItems)
        {
            ProcessScheduleItem(item, convertedNow);
        }
    }

    private void ProcessScheduleItem(ScheduleItem item, DateTime now)
    {
         if (item.Status == ScheduleStatus.Canceled)
                 return;

         DateTime currentDateTimeTruncated = TruncateDateTimeToSeconds(now);
         DateTime nextOccurrenceTruncated = TruncateDateTimeToSeconds(item.NextOccurrence);
        if (item.Type == ScheduleType.Periodic)
        {
            ProcessPeriodicItem(item, now, currentDateTimeTruncated, nextOccurrenceTruncated);
        }
        else if (item.Type == ScheduleType.NonPeriodic)
        {
            ProcessNonPeriodicItem(item, now, currentDateTimeTruncated, nextOccurrenceTruncated);
        }
        
     }
    private void ProcessPeriodicItem(ScheduleItem item, DateTime now, DateTime currentDateTimeTruncated, DateTime nextOccurrenceTruncated)
    {
        if (nextOccurrenceTruncated == currentDateTimeTruncated)
        {
            HandleScheduledPlayback(item, now);
            return; // Exit the method as playback is handled
        }

        if (nextOccurrenceTruncated <= currentDateTimeTruncated)
        {
            var calculator = _scheduleCalculatorFactory.CreateCalculator(item.CalendarType);
            item.NextOccurrence = calculator.GetNextOccurrence(item, now);
            return; //Exit the method as next occurance has been set
        }

        item.Status = ScheduleStatus.TimeWaiting; // No update needed, wait for next time

    }
    private void ProcessNonPeriodicItem(ScheduleItem item, DateTime now, DateTime currentDateTimeTruncated, DateTime nextOccurrenceTruncated)
    {
        var trigger = ActiveTriggers.GetTrigger(item.Trigger);

        if (nextOccurrenceTruncated == currentDateTimeTruncated)
        {
            HandleScheduledPlayback(item, now);
            return; // Exit as we have handled the playback
        }

         if (nextOccurrenceTruncated <= currentDateTimeTruncated || !trigger.HasValue || (trigger.HasValue && item.TriggerTime != trigger.Value.Time))
       {
            var calculator = _scheduleCalculatorFactory.CreateCalculator(item.CalendarType);
            if (calculator.IsNonPeriodicTriggerValid(item, now))
                UpdateNonPeriodicNextOccurrence(item, currentDateTimeTruncated, nextOccurrenceTruncated, now);
             else
            {
                 item.Status = ScheduleStatus.EventWaiting;
            }
          return; // Exit as we have handled the updating
       }


        item.Status = ScheduleStatus.EventWaiting; // No condition matches. waiting for the event
    }
    private void HandleScheduledPlayback(ScheduleItem item, DateTime now)
    {
        if (_audioPlayer.IsPlaying)
        {
            OnConflictOccurred(item);
            _audioPlayer.Stop(); // Stop the current playback
        }
        // Log the playlist name and time to the Visual Studio console
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Playing playlist: {item.Name}");

        // Log the event to the application log
        Logger.LogMessage($"Starting playback for schedule: {item.Name}");

        // Stop the current playlist (if any)
        _audioPlayer.Stop();

        // Start the new playlist
        _audioPlayer.Play(item.FilePaths);
        item.Status = ScheduleStatus.Playing;
        item.LastPlayTime = now;
    }
     private void UpdateNonPeriodicNextOccurrence(ScheduleItem item, DateTime currentDateTimeTruncated, DateTime nextOccurrenceTruncated, DateTime now)
    {
          var trigger = ActiveTriggers.GetTrigger(item.Trigger);
          if(!trigger.HasValue) {
               if (item.NextOccurrence >= now)
                {
                      item.NextOccurrence = DateTime.MinValue;
                     Logger.LogMessage($"Triger '{item.Trigger}' has gone away!");
                }
             return;
        }
         if (item.TriggerTime != trigger.Value.Time)
        {
              item.TriggerTime = trigger.Value.Time;
                 if (item.TriggerType == TriggerTypes.Immediate)
                {
                    item.NextOccurrence = trigger.Value.Time.Value;
                }
                else if (item.TriggerType == TriggerTypes.Delayed)
                {
                   if (TimeSpan.TryParse(item.DelayTime, out TimeSpan delay))
                    {
                         item.NextOccurrence = trigger.Value.Time.Value.Add(delay);
                    }
                    else
                    {
                        Logger.LogMessage($"Invalid DelayTime '{item.DelayTime}' for  schedule item  '{item.Name}'.");
                        item.NextOccurrence = DateTime.MinValue;
                    }
                }
                else if (item.TriggerType == TriggerTypes.Timed)
                {
                    item.NextOccurrence = trigger.Value.Time.Value.Add(-item.TotalDuration);
                }

        }
        else
         {
               if (item.TriggerType == TriggerTypes.Immediate || item.TriggerType == TriggerTypes.Timed)
                {
                     item.NextOccurrence = trigger.Value.Time.Value;
                }
                 else if (item.TriggerType == TriggerTypes.Delayed)
                {
                    if (TimeSpan.TryParse(item.DelayTime, out TimeSpan delay))
                    {
                         item.NextOccurrence = trigger.Value.Time.Value.Add(delay);
                     }
                     else
                    {
                         Logger.LogMessage($"Invalid DelayTime '{item.DelayTime}' for  schedule item  '{item.Name}'.");
                         item.NextOccurrence = DateTime.MinValue;
                     }
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
    /// Gets the list of scheduled items.
    /// </summary>
    /// <returns>A list of scheduled items.</returns>
    public List<ScheduleItem> GetScheduledItems()
    {
           DateTime now = DateTime.Now;
           DateTime convertedNow = CalendarHelper.ConvertToLocalTimeZone(now,  Settings.Region);
           // Return a copy of the list to avoid modifying the original
         return _configManager.ScheduleItems
              .Where(item => item.NextOccurrence >= convertedNow)
             .OrderBy(item => item.NextOccurrence)
            .Take(30)
             .ToList();
     }
    private List<ScheduleItem> GetDueScheduleItems(DateTime now)
    {
        return _configManager.ScheduleItems
             .Where(item => TruncateDateTimeToSeconds(item.NextOccurrence) <= TruncateDateTimeToSeconds(now))
             .ToList();
    }
   private DateTime GetNextOccurrence(ScheduleItem item, DateTime now)
    {
        var calculator = _scheduleCalculatorFactory.CreateCalculator(item.CalendarType);
          return calculator.GetNextOccurrence(item,now);
    }
}
    