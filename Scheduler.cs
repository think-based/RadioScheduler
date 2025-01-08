//Be Naame Khoda
//FileName: Scheduler.cs

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

        _lastConfigHash = FileHashHelper.CalculateFileHash(_configFilePath);

        Logger.LogMessage("Radio Scheduler Service started.");

        _configWatcher = new FileSystemWatcher
        {
            Path = Path.GetDirectoryName(_configFilePath),
            Filter = Path.GetFileName(_configFilePath),
            NotifyFilter = NotifyFilters.LastWrite
        };

        _configWatcher.Changed += OnConfigFileChanged;
        _configWatcher.EnableRaisingEvents = true;

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
        string currentHash = FileHashHelper.CalculateFileHash(_configFilePath);
        if (currentHash != _lastConfigHash)
        {
            _lastConfigHash = currentHash;
            return true;
        }
        return false;
    }

    public void ReloadScheduleConfig()
    {
        lock (_configLock)
        {
            try
            {
                if (!HasConfigChanged())
                {
                    return;
                }

                if (File.Exists(_configFilePath))
                {
                    string json = File.ReadAllText(_configFilePath);
                    var newItems = JsonConvert.DeserializeObject<List<ScheduleItem>>(json);

                    foreach (var item in newItems)
                    {
                        item.Validate();
                    }

                    _scheduleItems = newItems;

                    foreach (var timer in _timers)
                    {
                        timer.Stop();
                        timer.Dispose();
                    }
                    _timers.Clear();

                    SetupTimers();
                    Logger.LogMessage("Config reloaded successfully.");
                }
                else
                {
                    Logger.LogMessage("Config file not found!");
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
            }
        }
    }

    private DateTime GetNextOccurrence(ScheduleItem scheduleItem, DateTime now, DateTime endTime)
    {
        DateTime nextOccurrence = DateTime.MinValue;

        DateTime convertedDate = CalendarHelper.ConvertDate(now, scheduleItem.CalendarType);

        if (!MatchesCronField(scheduleItem.DayOfMonth, convertedDate.Day.ToString())) return nextOccurrence;
        if (!MatchesCronField(scheduleItem.Month, convertedDate.Month.ToString())) return nextOccurrence;
        if (!MatchesCronField(scheduleItem.DayOfWeek, ((int)convertedDate.DayOfWeek).ToString())) return nextOccurrence;

        if (scheduleItem.Type == "Periodic")
        {
            if (!MatchesCronField(scheduleItem.Second, now.Second.ToString())) return nextOccurrence;
            if (!MatchesCronField(scheduleItem.Minute, now.Minute.ToString())) return nextOccurrence;
            if (!MatchesCronField(scheduleItem.Hour, now.Hour.ToString())) return nextOccurrence;

            nextOccurrence = now;
        }
        else if (scheduleItem.Type == "NonPeriodic")
        {
            if (string.IsNullOrEmpty(scheduleItem.Trigger) || scheduleItem.Trigger != _currentTrigger.Event) return nextOccurrence;

            nextOccurrence = now;
        }

        if (nextOccurrence >= now && nextOccurrence <= endTime)
        {
            return nextOccurrence;
        }

        return DateTime.MinValue;
    }

    private bool MatchesCronField(string cronField, string value)
    {
        if (cronField == "*") return true;
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