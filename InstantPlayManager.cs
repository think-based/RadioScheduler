// Be Naame Khoda
// FileName: InstantPlayManager.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Timers;

public class InstantPlayManager
{
    private Timer _instantPlayTimer;
    private AudioPlayer _audioPlayer;
    private string _instantPlayFolderPath;

    public InstantPlayManager(string instantPlayFolderPath)
    {
        _instantPlayFolderPath = instantPlayFolderPath;
        _audioPlayer = new AudioPlayer();

        // Initialize the timer
        _instantPlayTimer = new Timer(1000); // Check every second
        _instantPlayTimer.Elapsed += OnInstantPlayTimerElapsed;
        _instantPlayTimer.AutoReset = true;
        _instantPlayTimer.Enabled = true;

        Logger.LogMessage("InstantPlayManager initialized.");
    }

    private void OnInstantPlayTimerElapsed(object sender, ElapsedEventArgs e)
    {
        CheckAndPlayInstantPlayFiles();
    }

    private void CheckAndPlayInstantPlayFiles()
    {
        try
        {
            // Check if the folder exists
            if (!Directory.Exists(_instantPlayFolderPath))
            {
                Logger.LogMessage($"InstantPlay folder not found: {_instantPlayFolderPath}");
                return;
            }

            // Get all audio files in the folder
            var audioFiles = Directory.GetFiles(_instantPlayFolderPath, "*.mp3");

            if (audioFiles.Length == 0)
            {
                return; // No files to play
            }

            // Play the first audio file found
            string audioFile = audioFiles[0];
            Logger.LogMessage($"Playing instant play file: {Path.GetFileName(audioFile)}");

            // Play the file
            _audioPlayer.Play(new List<FilePathItem> { new FilePathItem { Path = audioFile } });

            // Wait for playback to finish (optional, depending on your AudioPlayer implementation)
            while (_audioPlayer.IsPlaying)
            {
                System.Threading.Thread.Sleep(100);
            }

            // Delete the file after playback
            File.Delete(audioFile);
            Logger.LogMessage($"Deleted instant play file: {Path.GetFileName(audioFile)}");
        }
        catch (Exception ex)
        {
            Logger.LogMessage($"Error in InstantPlayManager: {ex.Message}");
        }
    }
}