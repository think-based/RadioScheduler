// Be Naame Khoda
// FileName: Scheduler.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Speech.Synthesis;
using System.Timers;
using NAudio.Wave;
using Newtonsoft.Json;
using RadioScheduler.Entities;

public class Scheduler
{
    private List<ScheduleItem> _scheduleItems;
    private string _configFilePath;
    private AudioPlayer _audioPlayer;
    private Timer _checkTimer; // Single timer to check schedules every second
    private Timer _configCheckTimer;
    private string _currentFileHash;

    public Scheduler()
    {
        _scheduleItems = new List<ScheduleItem>();
        _configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "audio.conf");
        _audioPlayer = new AudioPlayer();


        // Set up the single timer to check schedules every second
        _checkTimer = new Timer(1000); // 1-second interval
        _checkTimer.Elapsed += OnCheckTimerElapsed;
        _checkTimer.AutoReset = true;
        _checkTimer.Enabled = true;

        // Set up the timer for checking config file changes
        _configCheckTimer = new Timer(60000); // 1-minute interval
        _configCheckTimer.Elapsed += OnConfigFileCheckTimerElapsed;
        _configCheckTimer.AutoReset = true;
        _configCheckTimer.Enabled = true;

        ReloadScheduleConfig(); // Load the schedule configuration on initialization
        _currentFileHash = CalculateConfigHash();
    }

    /// <summary>
    /// Reloads the schedule configuration from the audio.conf file.
    /// </summary>
    public void ReloadScheduleConfig()
    {
        try
        {
            if (File.Exists(_configFilePath))
            {
                string json = File.ReadAllText(_configFilePath);
                var newItems = JsonConvert.DeserializeObject<List<ScheduleItem>>(json);

                // Clear existing schedule items
                _scheduleItems.Clear();

                // Add and validate new schedule items
                foreach (var item in newItems)
                {
                    item.Validate();
                    item.NextOccurrence = GetNextOccurrence(item, DateTime.Now);
                    item.TotalDuration = CalculateTotalDuration(item.FilePaths); // Calculate total duration
                    _scheduleItems.Add(item);
                }

                Logger.LogMessage($"Loaded {newItems.Count} items from {_configFilePath}.");
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
    /// <summary>
    /// Handles the timer tick event to check for scheduled tasks.
    /// </summary>
    private void OnCheckTimerElapsed(object sender, ElapsedEventArgs e)
    {
        DateTime now = DateTime.Now;
        DateTime nowTruncated = TruncateDateTimeToSeconds(now);
        string currentEvent = CurrentTrigger.Event; // Get current event from CurrentTrigger


        foreach (var item in _scheduleItems)
        {
            DateTime nextTruncated = TruncateDateTimeToSeconds(item.NextOccurrence);

            if (nextTruncated <= nowTruncated)
            {
                if (item.Type == "Periodic")
                {
                    HandlePeriodicItem(item, now, nowTruncated, nextTruncated);
                }
                else
                {
                    HandleNonPeriodicItem(item, nowTruncated, nextTruncated, currentEvent);
                }
            }
        }
    }

    private void HandlePeriodicItem(ScheduleItem item, DateTime now, DateTime nowTruncated, DateTime nextTruncated)
    {
        if (nextTruncated == nowTruncated)
        {
            OnPlaylistStart(item);
        }
        item.NextOccurrence = GetNextOccurrence(item, now);
    }

    private void HandleNonPeriodicItem(ScheduleItem item, DateTime nowTruncated, DateTime nextTruncated, string currentEvent)
    {
        if (nextTruncated == nowTruncated &&
                  !string.IsNullOrEmpty(item.Trigger) &&
                    item.Trigger.Equals(currentEvent, StringComparison.OrdinalIgnoreCase))

        {
            OnPlaylistStart(item);
            item.NextOccurrence = DateTime.MaxValue;
            CurrentTrigger.Event = null;
        }
    }

    private DateTime TruncateDateTimeToSeconds(DateTime dateTime)
    {
        return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, dateTime.Second);
    }

    /// <summary>
    /// Handles the timer tick event to check for configuration file changes.
    /// </summary>
    private void OnConfigFileCheckTimerElapsed(object sender, ElapsedEventArgs e)
    {
        var newFileHash = CalculateConfigHash();

        if (newFileHash != _currentFileHash)
        {
            Logger.LogMessage("Config file changed, reloading configuration.");
            ReloadScheduleConfig();
            _currentFileHash = newFileHash;

        }
    }
    /// <summary>
    /// Calculates the hash of the audio config file.
    /// </summary>
    private string CalculateConfigHash()
    {
        if (!File.Exists(_configFilePath))
            return "";
        return FileHashHelper.CalculateFileHash(_configFilePath);
    }

    /// <summary>
    /// Handles the start of a scheduled playlist.
    /// </summary>
    private void OnPlaylistStart(ScheduleItem scheduleItem)
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
        return new List<ScheduleItem>(_scheduleItems);
    }

    /// <summary>
    /// Calculates the total duration of a playlist.
    /// </summary>
    private TimeSpan CalculateTotalDuration(List<FilePathItem> filePaths)
    {
        TimeSpan totalDuration = TimeSpan.Zero;
        var ttsEngine = new SpeechSynthesizer(); // Initialize TTS engine

        foreach (var filePathItem in filePaths)
        {
            if (!string.IsNullOrEmpty(filePathItem.Text)) // Handle TTS
            {
                filePathItem.Duration = CalculateTtsDuration(filePathItem.Text, ttsEngine);
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

        ttsEngine.Dispose(); // Clean up TTS engine
        return totalDuration;
    }

    /// <summary>
    /// Calculates the exact duration of TTS playback using the SpeechSynthesizer.
    /// </summary>
    /// <param name="text">The TTS text.</param>
    /// <param name="ttsEngine">The TTS engine.</param>
    /// <returns>The duration of the TTS playback.</returns>
    private TimeSpan CalculateTtsDuration(string text, SpeechSynthesizer ttsEngine)
    {
        try
        {
            // Use the TTS engine to get the exact duration
            var prompt = new PromptBuilder();
            prompt.AppendText(text);

            // Measure the duration
            var ttsStream = new MemoryStream();
            ttsEngine.SetOutputToWaveStream(ttsStream);
            ttsEngine.Speak(prompt);

            // Calculate duration based on the audio stream
            ttsStream.Position = 0; // Reset stream position
            using (var reader = new WaveFileReader(ttsStream))
            {
                return reader.TotalTime; // Exact duration of the TTS audio
            }
        }
        catch (Exception ex)
        {
            Logger.LogMessage($"Error calculating TTS duration: {ex.Message}");
            return TimeSpan.FromSeconds(text.Length / 10.0); // Fallback to estimation
        }
    }
    /// <summary>
    /// Calculates the next occurrence of a schedule item.
    /// </summary>
    private DateTime GetNextOccurrence(ScheduleItem item, DateTime now)
    {
        if (item.Type == "Periodic")
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