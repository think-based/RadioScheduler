      //Be Naame Khoda
//FileName: Scheduler.cs
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
        _checkTimer.AutoReset = false;
        _checkTimer.Enabled = true;

        _configManager.ConfigReloaded += OnConfigReloaded;
        _audioPlayer.PlaylistFinished += OnPlaylistFinished;
         // Subscribe to the TriggersChanged event
        ActiveTriggers.TriggersChanged += OnTriggersChanged;
    }

    public ISchedulerConfigManager ConfigManager => _configManager;
    private void OnConfigReloaded()
    {
        Logger.LogMessage("Configuration reloaded.");
        CheckScheduleItems();
    }
     // Event handler for TriggersChanged
    private void OnTriggersChanged()
    {
        Logger.LogMessage("Triggers changed, checking schedule items.");
        CheckScheduleItems();
        //_configManager.ReloadScheduleConfig();
    }
    private void OnPlaylistFinished(ScheduleItem scheduleItem)
    {
        _configManager.ReloadScheduleItem(scheduleItem.ConfigId);
        CheckScheduleItems();
    }
    private void OnCheckTimerElapsed(object sender, ElapsedEventArgs e)
    {
        CheckScheduleItems();
    }

    private void CheckScheduleItems()
    {
         DateTime now = DateTime.Now;
        DateTime convertedNow = CalendarHelper.ConvertToAppTimeZone(now);
        List<ScheduleItem> dueItems = GetDueScheduleItems(convertedNow);
        foreach (var item in dueItems)
        {
            ProcessScheduleItem(item, convertedNow);
        }
        ScheduleItem earliestScheduleItem = GetEarliestScheduleItem();
        if (earliestScheduleItem != null && earliestScheduleItem.NextOccurrence > CalendarHelper.Now())
        {
            TimeSpan timeToNext = earliestScheduleItem.NextOccurrence - CalendarHelper.Now();

            // If the time is negative, do not change timer interval, use default 1000.
            if (timeToNext.TotalMilliseconds > 0)
            {
                _checkTimer.Interval = timeToNext.TotalMilliseconds;
                _checkTimer.Start();
                return;
            }
        }
        _checkTimer.Interval = 1000; //Set the default value for the timer interval
        _checkTimer.Start();
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
          if (nextOccurrenceTruncated == currentDateTimeTruncated)
        {
            HandleScheduledPlayback(item, now);
            return;
        }

        if (item.Triggers == null || !item.Triggers.Any())
        {
             item.Status = ScheduleStatus.EventWaiting;
            return;
        }
        ProcessTrigger(item, now, currentDateTimeTruncated, nextOccurrenceTruncated);
    }
    private void ProcessTrigger(ScheduleItem item, DateTime now, DateTime currentDateTimeTruncated, DateTime nextOccurrenceTruncated)
    {
        (DateTime? triggerTime, string triggerName) = GetSoonestTrigger(item);

        if (triggerTime.HasValue)
        {
            if (nextOccurrenceTruncated <= currentDateTimeTruncated || item.TriggerTime != triggerTime)
            {
               var calculator = _scheduleCalculatorFactory.CreateCalculator(item.CalendarType);
               if (calculator.IsNonPeriodicTriggerValid(item, now))
                  UpdateNonPeriodicNextOccurrence(item, currentDateTimeTruncated, nextOccurrenceTruncated, now, triggerTime, triggerName);
               else
                   item.Status = ScheduleStatus.EventWaiting;
            }
            else
            {
                item.Status = ScheduleStatus.EventWaiting;
            }
        }
        else
        {
             item.Status = ScheduleStatus.EventWaiting;
        }
    }
    private (DateTime? triggerTime, string triggerName) GetSoonestTrigger(ScheduleItem item)
    {
        DateTime? soonestTriggerTime = null;
        string soonestTriggerName = null;
        DateTime now = CalendarHelper.Now();


        foreach (var triggerName in item.Triggers)
        {
            var trigger = ActiveTriggers.GetTrigger(triggerName);

            if (trigger.HasValue && trigger.Value.Time.HasValue)
            {
                if (trigger.Value.Time > now)
                {
                    if (soonestTriggerTime == null || trigger.Value.Time < soonestTriggerTime)
                    {
                        soonestTriggerTime = trigger.Value.Time;
                        soonestTriggerName = triggerName;
                    }
                }

            }
        }

        return (soonestTriggerTime, soonestTriggerName);
    }
    private void HandleScheduledPlayback(ScheduleItem item, DateTime now)
    {
       ScheduleItem currentPlayingItem = _audioPlayer.GetCurrentScheduledItem();

         if (currentPlayingItem != null)
        {
             if (item.Priority >= currentPlayingItem.Priority)
            {
                 _audioPlayer.Stop();
                 Logger.LogMessage($"Conflict: Stopping {currentPlayingItem.Name} (Priority: {currentPlayingItem.Priority}) to play {item.Name} (Priority: {item.Priority})");
                _audioPlayer.Play(item);
                 item.LastPlayTime = now;
                //Set the last trigger name when it starts playing.
                 item.LastTriggerName = item.Triggers != null && item.Triggers.Any() ? item.Triggers.FirstOrDefault() : "N/A";

            }
            else
            {
                 Logger.LogMessage($"Conflict: Ignoring {item.Name} (Priority: {item.Priority}) because {currentPlayingItem.Name} (Priority: {currentPlayingItem.Priority}) is playing and has higher priority.");
                 _configManager.ReloadScheduleItemById(item.ItemId);
                  OnConflictOccurred(item);
                return;
            }
        }
        else
        {
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Playing playlist: {item.Name}");
            Logger.LogMessage($"Starting playback for schedule: {item.Name}");

           _audioPlayer.Play(item);
            item.LastPlayTime = now;
            //Set the last trigger name when it starts playing.
             item.LastTriggerName = item.Triggers != null && item.Triggers.Any() ? item.Triggers.FirstOrDefault() : "N/A";
        }
    }
    private void UpdateNonPeriodicNextOccurrence(ScheduleItem item, DateTime currentDateTimeTruncated, DateTime nextOccurrenceTruncated, DateTime now, DateTime? triggerTime, string triggerName)
    {
        if (!triggerTime.HasValue)
        {
            if (item.NextOccurrence >= now)
            {
                item.NextOccurrence = DateTime.MinValue;
                Logger.LogMessage($"Triger '{triggerName}' has gone away!");
            }
            return;
        }
        if (item.TriggerTime != triggerTime)
        {
            item.TriggerTime = triggerTime;
            item.NextOccurrence = GetNonPeriodicNextOccurrence(item, triggerTime.Value);
        }
        /*
        else
        {
            item.NextOccurrence = CalculateNextOccurrence(item, triggerTime.Value);
        }*/
        //Set the last trigger name in UpdateNonPeriodicNextOccurrence so we can see the trigger that caused the update.
        item.LastTriggerName = triggerName;
    }
    private DateTime GetNonPeriodicNextOccurrence(ScheduleItem item, DateTime triggerTime)
    {
        switch (item.TriggerType)
        {
            case TriggerTypes.Immediate:
                return triggerTime;
            case TriggerTypes.Delayed:
                if (TimeSpan.TryParse(item.DelayTime, out TimeSpan delay))
                {
                    return triggerTime.Add(delay);
                }
                else
                {
                    Logger.LogMessage($"Invalid DelayTime '{item.DelayTime}' for  schedule item  '{item.Name}'.");
                    return item.NextOccurrence;
                }
            case TriggerTypes.Timed:
                TimeSpan playTime = TimeSpan.FromSeconds(Settings.TimedDelay) + item.TotalDuration;
                return triggerTime.Add(-playTime);
            default:
                return item.NextOccurrence;
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
    public List<ScheduleItem> GetScheduledItems()
    {
          DateTime now = DateTime.Now;
          DateTime convertedNow = CalendarHelper.ConvertToAppTimeZone(now);
        // Get the list of scheduled items from the configuration
        var items = _configManager.ScheduleItems
            .Where(item => item.EndTime >= convertedNow)
             .OrderBy(item => item.NextOccurrence)
           .Take(30)
            .ToList();
            foreach (var item in items)
            {
                item.TimeToPlay = CalculateTimeToPlay(item.NextOccurrence, convertedNow);
            }
           return items;
    }
    private List<ScheduleItem> GetDueScheduleItems(DateTime now)
    {
         return _configManager.ScheduleItems
            .Where(item => TruncateDateTimeToSeconds(item.NextOccurrence) <= TruncateDateTimeToSeconds(now))
            .ToList();
    }
    private string CalculateTimeToPlay(DateTime nextOccurrence, DateTime now)
    {
         if (nextOccurrence == DateTime.MinValue)
               return "N/A";

           var timeDiff = nextOccurrence - now;

           if (timeDiff <= TimeSpan.Zero) {
               return "Playing or Due";
           }
        const double secondsInDay = 60 * 60 * 24;

        if (timeDiff.TotalDays >= 1) {
           return $"{(int)timeDiff.TotalDays}d {timeDiff.Hours}h {timeDiff.Minutes}m {timeDiff.Seconds}s";
        }
        else if (timeDiff.TotalHours >= 1) {
             return $"{timeDiff.Hours}h {timeDiff.Minutes}m {timeDiff.Seconds}s";
        }
        else if (timeDiff.TotalMinutes >= 1) {
              return $"{timeDiff.Minutes}m {timeDiff.Seconds}s";
        }
        else
             return $"{timeDiff.Seconds}s";
      }

    public ScheduleItem GetEarliestScheduleItem()
     {
          DateTime now = DateTime.Now;
          DateTime convertedNow = CalendarHelper.ConvertToAppTimeZone(now);
           return _configManager.ScheduleItems
            .Where(item => item.NextOccurrence > convertedNow)
            .OrderBy(item => item.NextOccurrence)
            .FirstOrDefault();
     }
}
    