//Be Naame Khoda
//FileName: InstantPlayManager.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Timers;

public class InstantPlayManager
{
    private Timer _instantPlayTimer;
    private AudioPlayer _audioPlayer;
    private string _instantPlayFolderPath;
    private bool _isProcessing = false; // Flag to prevent re-entrancy

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

    private async void OnInstantPlayTimerElapsed(object sender, ElapsedEventArgs e)
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
                return; // Folder not found, skip processing
            }

            // Get all audio files in the folder
            var audioFiles = Directory.GetFiles(_instantPlayFolderPath, "*.mp3");

            if (audioFiles.Length == 0)
            {
                return; // No files to play
            }

            // Play the first audio file found
            string audioFile = audioFiles[0];
            Logger.LogMessage($"Playing file: {Path.GetFileName(audioFile)}");

            // Play the file asynchronously
            _audioPlayer.Play(new List<FilePathItem> { new FilePathItem { Path = audioFile } });

            // Wait for the PlaylistFinished event to be triggered
            await WaitForPlaybackCompletion();

            // Delete the file after playback
            File.Delete(audioFile);
            Logger.LogMessage($"Deleted file: {Path.GetFileName(audioFile)}");
        }
        catch (Exception ex)
        {
            Logger.LogMessage($"Error: {ex.Message}");
        }
        finally
        {
            // Restart the timer after processing is complete
            _isProcessing = false;
            _instantPlayTimer.Start();
        }
    }

    private Task WaitForPlaybackCompletion()
    {
        // Create a TaskCompletionSource to wait for the PlaylistFinished event
        var tcs = new TaskCompletionSource<bool>();

        // Subscribe to the PlaylistFinished event
        Action playbackFinishedHandler = null;
        playbackFinishedHandler = () =>
        {
            _audioPlayer.PlaylistFinished -= playbackFinishedHandler; // Unsubscribe
            tcs.SetResult(true); // Signal completion
        };
        _audioPlayer.PlaylistFinished += playbackFinishedHandler;

        return tcs.Task;
    }

    private void OnPlaylistFinished()
    {
        // No logging here, as we only log when a file is played or deleted
    }
}