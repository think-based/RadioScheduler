// Be Naame Khoda
// FileName: InstantPlayManager.cs

using RadioScheduler.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Timers;

public class InstantPlayManager
{
    private Timer _instantPlayTimer;
    private AudioPlayer _audioPlayer;
    private string _instantPlayFolderPath;
    private bool _isProcessing = false; // Flag to prevent re-entrancy
    private string _currentAudioFile; // Track the current audio file being played
    private bool _folderExists = true; // Track if the folder exists to avoid repetitive checks

    public InstantPlayManager(string instantPlayFolderPath)
    {
        _instantPlayFolderPath = instantPlayFolderPath;
        _audioPlayer = new AudioPlayer();

        // Subscribe to the PlaylistFinished event
        _audioPlayer.PlaylistFinished += OnPlaylistFinished;

        // Initialize the timer
        _instantPlayTimer = new Timer(1000); // Check every second
        _instantPlayTimer.Elapsed += OnInstantPlayTimerElapsed;
        _instantPlayTimer.AutoReset = true;
        _instantPlayTimer.Enabled = true;
    }

    private void OnInstantPlayTimerElapsed(object sender, ElapsedEventArgs e)
    {
        // Prevent re-entrancy
        if (_isProcessing) return;
        _isProcessing = true;

        // Stop the timer while processing the file
        _instantPlayTimer.Stop();

        try
        {
            // Check if the folder exists
            if (!Directory.Exists(_instantPlayFolderPath))
            {
                _folderExists = false; // Update flag to avoid repetitive checks
                _isProcessing = false;
                _instantPlayTimer.Start(); // Restart the timer
                return; // Folder not found, skip processing
            }
            else if (!_folderExists)
            {
                _folderExists = true; // Update flag to avoid repetitive checks
            }

            // Get all audio files in the folder
            var audioFiles = Directory.GetFiles(_instantPlayFolderPath, "*.mp3");

            if (audioFiles.Length == 0)
            {
                _currentAudioFile = null; // Reset to avoid repetitive checks
                _isProcessing = false;
                _instantPlayTimer.Start(); // Restart the timer
                return; // No files to play
            }

            // Play the first audio file found
            _currentAudioFile = audioFiles[0];
            Logger.LogMessage($"Playing file: {Path.GetFileName(_currentAudioFile)}");

            // Play the file
            _audioPlayer.Play(new List<FilePathItem> { new FilePathItem { Path = _currentAudioFile } });
        }
        catch (Exception ex)
        {
            Logger.LogMessage($"Error: {ex.Message}");
            // Restart the timer in case of an error
            _isProcessing = false;
            _instantPlayTimer.Start();
            Logger.LogMessage("Timer restarted after error.");
        }
    }

    private void OnPlaylistFinished()
    {
        try
        {
            // Delete the file after playback
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
            // Restart the timer after playback and file deletion are complete
            _isProcessing = false;
            _instantPlayTimer.Start();
        }
    }
}