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

        // تنظیم مسیر کامل فایل کانفیگ
        _configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "audio.conf");

        // اگر فایل کانفیگ وجود نداشت، آن را ایجاد کن
        EnsureConfigFileExists();

        _lastConfigHash = FileHashHelper.CalculateFileHash(_configFilePath);

        // تنظیم FileSystemWatcher
        _configWatcher = new FileSystemWatcher
        {
            Path = Path.GetDirectoryName(_configFilePath), // مسیر دایرکتوری فایل کانفیگ
            Filter = Path.GetFileName(_configFilePath), // نام فایل کانفیگ
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
                // ایجاد یک فایل کانفیگ خالی
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
                // بررسی تغییرات فایل کانفیگ
                if (!HasConfigChanged())
                {
                    Logger.LogMessage("Config file has not changed. Skipping reload.");
                    return;
                }

                if (File.Exists(_configFilePath))
                {
                    string json = File.ReadAllText(_configFilePath);
                    var newItems = JsonConvert.DeserializeObject<List<ScheduleItem>>(json);
                    _scheduleItems = newItems;

                    // تایمرهای قبلی را متوقف و پاک کن
                    foreach (var timer in _timers)
                    {
                        timer.Stop();
                        timer.Dispose();
                    }
                    _timers.Clear();

                    // تایمرهای جدید را تنظیم کن
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
        DateTime endTime = now.AddHours(24); // ۲۴ ساعت آینده

        foreach (var scheduleItem in _scheduleItems)
        {
            DateTime nextOccurrence = GetNextOccurrence(scheduleItem, now, endTime);
            if (nextOccurrence != DateTime.MinValue)
            {
                // محاسبه مدت زمان کل لیست فایل‌ها
                TimeSpan totalDuration = _audioPlayer.CalculateTotalDuration(scheduleItem.FilePaths);

                // تعیین زمان شروع پخش بر اساس TriggerType
                DateTime startTime;
                if (scheduleItem.TriggerType == "Immediate")
                {
                    // برای Immediate، پخش در زمان CurrentTrigger.Time شروع می‌شود
                    startTime = nextOccurrence;
                }
                else if (scheduleItem.TriggerType == "Timed")
                {
                    // برای Timed، پخش باید در زمان CurrentTrigger.Time به پایان برسد
                    startTime = nextOccurrence - totalDuration;
                }
                else
                {
                    // اگر TriggerType نامعتبر باشد، از این آیتم صرف‌نظر می‌کنیم
                    Logger.LogMessage($"Invalid TriggerType for ItemID: {scheduleItem.ItemId}");
                    continue;
                }

                // اگر زمان شروع در گذشته باشد، از این آیتم صرف‌نظر می‌کنیم
                if (startTime < now)
                {
                    Logger.LogMessage($"Skipping ItemID: {scheduleItem.ItemId} - Start time is in the past.");
                    continue;
                }

                // محاسبه زمان باقی‌مانده تا پخش
                double delay = (startTime - now).TotalMilliseconds;

                // ایجاد تایمر برای پلی‌لیست
                Timer timer = new Timer(delay);
                timer.AutoReset = false; // تایمر فقط یک بار فعال می‌شود
                timer.Elapsed += (sender, e) => OnPlaylistStart(scheduleItem);
                timer.Start();
                _timers.Add(timer);

                // نمایش اطلاعات تایمر
                Logger.LogMessage($"Timer Created - ItemID: {scheduleItem.ItemId}, Type: {scheduleItem.TriggerType}, StartTime: {startTime:yyyy-MM-dd HH:mm:ss}, EndTime: {nextOccurrence:yyyy-MM-dd HH:mm:ss}, Trigger: {scheduleItem.Trigger ?? "N/A"}");
            }
        }
    }

    private DateTime GetNextOccurrence(ScheduleItem scheduleItem, DateTime now, DateTime endTime)
    {
        DateTime nextOccurrence = DateTime.MinValue;

        // بررسی تقویم (ماه و روز هفته)
        DateTime convertedDate = CalendarHelper.ConvertDate(now, scheduleItem.CalendarType);
        if (!MatchesCronField(scheduleItem.DayOfMonth, convertedDate.Day.ToString())) return nextOccurrence;
        if (!MatchesCronField(scheduleItem.Month, convertedDate.Month.ToString())) return nextOccurrence;
        if (!MatchesCronField(scheduleItem.DayOfWeek, ((int)convertedDate.DayOfWeek).ToString())) return nextOccurrence;

        // بررسی نوع زمان‌بندی
        if (scheduleItem.Type == "Periodic")
        {
            // بررسی زمان دقیق (ساعت، دقیقه، ثانیه)
            if (!MatchesCronField(scheduleItem.Second, now.Second.ToString())) return nextOccurrence;
            if (!MatchesCronField(scheduleItem.Minute, now.Minute.ToString())) return nextOccurrence;
            if (!MatchesCronField(scheduleItem.Hour, now.Hour.ToString())) return nextOccurrence;

            nextOccurrence = now;
        }
        else if (scheduleItem.Type == "NonPeriodic")
        {
            // بررسی تریگر
            if (string.IsNullOrEmpty(scheduleItem.Trigger) || scheduleItem.Trigger != _currentTrigger.Event) return nextOccurrence;

            nextOccurrence = now;
        }

        // اگر زمان پخش در ۲۴ ساعت آینده باشد، بازگردانده می‌شود
        if (nextOccurrence >= now && nextOccurrence <= endTime)
        {
            return nextOccurrence;
        }

        return DateTime.MinValue;
    }

    private bool MatchesCronField(string cronField, string value)
    {
        if (cronField == "*") return true; // هر مقداری قابل قبول است
        return cronField == value; // بررسی تطابق دقیق
    }

    private void OnPlaylistStart(ScheduleItem scheduleItem)
    {
        // فعال کردن اونت "قبل از پخش"
        BeforePlayback?.Invoke();

        // متوقف کردن پلی‌لیست قبلی و شروع پلی‌لیست جدید
        _audioPlayer.Play(scheduleItem.FilePaths);

        // فعال کردن اونت "بعد از پخش"
        AfterPlayback?.Invoke();
    }

    private void OnPlaylistFinished()
    {
        // فعال کردن اونت "بعد از پخش"
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