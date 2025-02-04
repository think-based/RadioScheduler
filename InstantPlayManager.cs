      // Be Naame Khoda
// FileName: InstantPlayManager.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Timers;
using static Enums;

public class InstantPlayManager
{
    private Timer _instantPlayTimer;
    private AudioPlayer _audioPlayer;
    private string _instantPlayFolderPath;
    private bool _isProcessing = false;
    private string _currentAudioFile;
    private bool _folderExists = true;

    public InstantPlayManager(string instantPlayFolderPath, ISchedulerConfigManager configManager)
    {
        _instantPlayFolderPath = instantPlayFolderPath;
        _audioPlayer = new AudioPlayer(configManager);

        _audioPlayer.PlaylistFinished += OnPlaylistFinished;

        _instantPlayTimer = new Timer(1000);
        _instantPlayTimer.Elapsed += OnInstantPlayTimerElapsed;
        _instantPlayTimer.AutoReset = true;
        _instantPlayTimer.Enabled = true;
    }

    private void OnInstantPlayTimerElapsed(object sender, ElapsedEventArgs e)
    {
        if (_isProcessing) return;
        _isProcessing = true;

        _instantPlayTimer.Stop();

        try
        {
            if (!Directory.Exists(_instantPlayFolderPath))
            {
                _folderExists = false;
                _isProcessing = false;
                _instantPlayTimer.Start();
                return;
            }
            else if (!_folderExists)
            {
                _folderExists = true;
            }

            var audioFiles = Directory.GetFiles(_instantPlayFolderPath, "*.mp3");

            if (audioFiles.Length == 0)
            {
                _currentAudioFile = null;
                _isProcessing = false;
                _instantPlayTimer.Start();
                return;
            }

            _currentAudioFile = audioFiles[0];
            Logger.LogMessage($"Playing file: {Path.GetFileName(_currentAudioFile)}");

            // Create a ScheduleItem and populate PlayList with the file path and let ScheduleItem calculate duration
            var scheduleItem = new ScheduleItem
            {
                Name = "Instant Play",
                Type = ScheduleType.NonPeriodic,
                TriggerType = TriggerTypes.Immediate,
                //Ensure using forward slashes
                PlayList = new List<ScheduleItem.PlayListItem>() { new ScheduleItem.PlayListItem {Path = _currentAudioFile.Replace('\\', '/')} }

            };

             // Calculate total duration from the playlist
            scheduleItem.CalculateIndividualItemDuration();
             scheduleItem.TotalDuration = scheduleItem.CalculateTotalDuration();
            _audioPlayer.Play(scheduleItem);
        }
        catch (Exception ex)
        {
            Logger.LogMessage($"Error: {ex.Message}");
            _isProcessing = false;
            _instantPlayTimer.Start();
            Logger.LogMessage("Timer restarted after error.");
        }
    }

    private void OnPlaylistFinished(ScheduleItem item)
    {
        try
        {
            if (!string.IsNullOrEmpty(_currentAudioFile) && File.Exists(_currentAudioFile))
            {
                File.Delete(_currentAudioFile);
                Logger.LogMessage($"Deleted file: {Path.GetFileName(_currentAudioFile)}");
            }
        }
        catch (Exception ex)
        {
            Logger.LogMessage($"Error deleting file: {ex.Message}");
        }
        finally
        {
            _isProcessing = false;
            _instantPlayTimer.Start();
        }
    }
}
    