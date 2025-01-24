      // Be Naame Khoda
// FileName: ScheduleItem.cs

using RadioScheduler.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using NAudio.Wave;
using System.Speech.Synthesis;
using System.Linq;

using static Enums;

public class ScheduleItem
{
    // Define a new internal class to represent playlist items
    internal class PlayListItem
    {
        public string Path { get; set; }
        public double Duration { get; set; } // Duration in milliseconds
    }
    public int ItemId { get; set; }
    public string Name { get; set; }
    public ScheduleType Type { get; set; }
    public List<FilePathItem> FilePaths { get; set; }
    public string Second { get; set; }
    public string Minute { get; set; }
    public string Hour { get; set; }
    public string DayOfMonth { get; set; }
    public string Month { get; set; }
    public string DayOfWeek { get; set; }
    public string Trigger { get; set; }
    public CalendarTypes CalendarType { get; set; }
    public string Region { get; set; }
    public TriggerTypes TriggerType { get; set; }
    public string DelayTime { get; set; }
    public DateTime NextOccurrence { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public ScheduleStatus Status { get; set; }
    public DateTime? LastPlayTime { get; set; }
    public DateTime? TriggerTime { get; set; }
    public Enums.Priority? Priority { get; set; }

    // Add the EndTime property
    public DateTime EndTime
    {
        get
        {
            return NextOccurrence.Add(TotalDuration);
        }
    }

    // New internal property for the playlist
    internal List<PlayListItem> PlayList { get; set; } = new List<PlayListItem>();


    public void Validate()
    {
        if (Type == ScheduleType.Periodic && Trigger != null)
        {
            throw new ArgumentException("Trigger should not be set for Periodic items.");
        }

        if (Type == ScheduleType.NonPeriodic && (Second != null || Minute != null || Hour != null ))
        {
            throw new ArgumentException("Periodic fields (Second, Minute, Hour, DayOfMonth, Month, DayOfWeek) should not be set for NonPeriodic items.");
        }
        if (TriggerType != TriggerTypes.Delayed && !string.IsNullOrEmpty(DelayTime))
        {
            DelayTime = null;
        }

        if (TriggerType == TriggerTypes.Delayed && string.IsNullOrEmpty(DelayTime))
        {
            throw new ArgumentException("DelayTime must be set when TriggerType is Delayed.");
        }
        if (TriggerType == TriggerTypes.Delayed && !string.IsNullOrEmpty(DelayTime))
        {
            //Validate format of DelayTime
            if (!TimeSpan.TryParse(DelayTime, out _))
                throw new ArgumentException("Invalid format for DelayTime. Use 'hours:minutes:seconds'");
        }


        if (FilePaths == null || FilePaths.Count == 0)
        {
            throw new ArgumentException("FilePaths cannot be null or empty.");
        }

        foreach (var filePathItem in FilePaths)
        {
            filePathItem.Validate();
        }
    }

    internal void CalculateIndividualItemDuration()
    {
        var ttsEngine = new SpeechSynthesizer(); // Initialize TTS engine
        var tempPlaylist = new List<PlayListItem>();
         foreach (var playListItem in this.PlayList)
        {
             if (playListItem.Path.StartsWith("TTS:"))
             {
                 string text = playListItem.Path.Substring(4);
                 var duration = CalculateTtsDuration(text, ttsEngine);
                  tempPlaylist.Add(new PlayListItem {Path = playListItem.Path , Duration = duration.TotalMilliseconds});
             }
             else if (File.Exists(playListItem.Path))
            {
                 try
                {
                    using (var reader = new AudioFileReader(playListItem.Path))
                    {
                           tempPlaylist.Add(new PlayListItem {Path = playListItem.Path, Duration = reader.TotalTime.TotalMilliseconds});
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogMessage($"Error calculating duration for file {playListItem.Path}: {ex.Message}");
                     tempPlaylist.Add(new PlayListItem {Path = playListItem.Path, Duration = 0});

                }
            }
        }
         this.PlayList = tempPlaylist; // Assign the new playList that has duration information for each item
         ttsEngine.Dispose();
     }


     internal TimeSpan CalculateTotalDuration()
    {
        TimeSpan totalDuration = TimeSpan.Zero;
        foreach (var item in this.PlayList)
        {
            totalDuration += TimeSpan.FromMilliseconds(item.Duration);
        }
        return totalDuration;
    }

     private TimeSpan CalculateTtsDuration(string text, SpeechSynthesizer ttsEngine)
    {
        try
        {
            var prompt = new PromptBuilder();
            prompt.AppendText(text);

            var ttsStream = new MemoryStream();
            ttsEngine.SetOutputToWaveStream(ttsStream);
            ttsEngine.Speak(prompt);

            ttsStream.Position = 0;
            using (var reader = new WaveFileReader(ttsStream))
            {
                return reader.TotalTime;
            }
        }
        catch (Exception ex)
        {
            Logger.LogMessage($"Error calculating TTS duration: {ex.Message}");
            return TimeSpan.FromSeconds(text.Length / 10.0);
        }
    }
}
    