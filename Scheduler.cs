//Be Naame Khoda
//FileName: Scheduler.cs
//TODO : 1- single mode : ranadom , que
//TODO : 2- web play list files &/ reload schedual item button / high light current play
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using NAudio.Wave;
using static Enums;

public class Scheduler
{
    private readonly IAudioPlayer _audioPlayer;
    private readonly ISchedulerConfigManager _configManager;
    private readonly IScheduleCalculatorFactory _scheduleCalculatorFactory;
    private Timer _checkTimer;

    public Scheduler(IAudioPlayer audioPlayer, ISchedulerConfigManager configManager, IScheduleCalculatorFactory scheduleCalculatorFactory, ITriggerManager triggerManager)
    {
        _audioPlayer = audioPlayer;
        _configManager = configManager;
        _scheduleCalculatorFactory = scheduleCalculatorFactory;
        _checkTimer = new Timer(1000);
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
            return;
        }

        if (nextOccurrenceTruncated <= currentDateTimeTruncated)
        {
            var calculator = _scheduleCalculatorFactory.CreateCalculator(item.CalendarType);
            item.NextOccurrence = calculator.GetNextOccurrence(item, now);
            return;
        }

        item.Status = ScheduleStatus.TimeWaiting;

    }
    private void ProcessNonPeriodicItem(ScheduleItem item, DateTime now, DateTime currentDateTimeTruncated, DateTime nextOccurrenceTruncated)
    {
        var trigger = ActiveTriggers.GetTrigger(item.Trigger);

        if (nextOccurrenceTruncated == currentDateTimeTruncated)
        {
            HandleScheduledPlayback(item, now);
            return;
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
            return;
        }


        item.Status = ScheduleStatus.EventWaiting;
    }
    private void HandleScheduledPlayback(ScheduleItem item, DateTime now)
    {
        ScheduleItem currentPlayingItem = _audioPlayer.GetCurrentScheduledItem();

        if (currentPlayingItem != null)
        {
            OnConflictOccurred(item, currentPlayingItem, now);  // Pass currentPlayingItem and now
            return;
        }

        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Playing playlist: {item.Name}");
        Logger.LogMessage($"Starting playback for schedule: {item.Name}");

        _audioPlayer.Play(item);
        item.LastPlayTime = now;
    }
    private void UpdateNonPeriodicNextOccurrence(ScheduleItem item, DateTime currentDateTimeTruncated, DateTime nextOccurrenceTruncated, DateTime now)
    {
        var trigger = ActiveTriggers.GetTrigger(item.Trigger);
        if (!trigger.HasValue)
        {
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
    private void OnConflictOccurred(ScheduleItem newItem, ScheduleItem currentPlayingItem, DateTime now)
    {
        if (newItem.Priority >= currentPlayingItem.Priority)
        {
            _audioPlayer.Stop();
            Logger.LogMessage($"Conflict: Stopping {currentPlayingItem.Name} (Priority: {currentPlayingItem.Priority}) to play {newItem.Name} (Priority: {newItem.Priority})");
            _audioPlayer.Play(newItem);
            newItem.LastPlayTime = now; //Setting the last play time should be done right after calling _audioPlayer.Play(newItem); and it should only occur once
        }
        else
        {
            Logger.LogMessage($"Conflict: Ignoring {newItem.Name} (Priority: {newItem.Priority}) because {currentPlayingItem.Name} (Priority: {currentPlayingItem.Priority}) is playing and has higher priority.");
            _configManager.ReloadScheduleItem(newItem.ItemId);

        }

    }
    private DateTime TruncateDateTimeToSeconds(DateTime dateTime)
    {
        return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, dateTime.Second);
    }

    public List<ScheduleItem> GetScheduledItems()
    {
        DateTime now = DateTime.Now;
        DateTime convertedNow = CalendarHelper.ConvertToLocalTimeZone(now, Settings.Region);
        return _configManager.ScheduleItems
             .Where(item => item.EndTime >= convertedNow)
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
        return calculator.GetNextOccurrence(item, now);
    }
}
    