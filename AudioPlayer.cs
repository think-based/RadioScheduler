// Be Naame Khoda
// FileName: AudioPlayer.cs

using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Speech.Synthesis;
using System.Threading;
using System.Threading.Tasks;
using RadioScheduler.Entities;
using static Enums; // Reference the Enums namespace

public class AudioPlayer : IAudioPlayer
{
    private WaveOutEvent _audioPlayerWaveOut;
    private AudioFileReader _currentAudioFile;
    private List<string> _currentPlaylist;
    private int _currentIndex;
    private SpeechSynthesizer _ttsPlayer;
    private readonly object _lock = new object(); // Lock for thread safety

    private Queue<ScheduleItem> _playlistQueue = new Queue<ScheduleItem>(); // Updated to use ScheduleItem
    private Task _playbackTask;
    private CancellationTokenSource _playbackCancellationTokenSource;

    public bool IsPlaying { get; private set; }
    public string CurrentFile { get; private set; }

    public event Action PlaylistFinished;

    public AudioPlayer()
    {
        _audioPlayerWaveOut = new WaveOutEvent();
        _audioPlayerWaveOut.PlaybackStopped += OnPlaybackStopped;
        _ttsPlayer = new SpeechSynthesizer();
        _ttsPlayer.SpeakCompleted += OnTtsCompleted;
        IsPlaying = false;
        CurrentFile = null;

        _playbackCancellationTokenSource = new CancellationTokenSource();
        _playbackTask = Task.Run(() => PlaybackLoop(_playbackCancellationTokenSource.Token));
    }

    /// <summary>
    /// Plays the provided schedule item.
    /// </summary>
    public void Play(ScheduleItem item) // Updated to accept ScheduleItem
    {
        lock (_lock)
        {
            _playlistQueue.Enqueue(item); // Add the new schedule item to the queue
        }
    }

    /// <summary>
    /// Stops the current playback and cleans up resources.
    /// </summary>
    public void Stop()
    {
        lock (_lock)
        {
            _playlistQueue.Clear(); // Clear the playlist queue

            if (_audioPlayerWaveOut != null)
            {
                _audioPlayerWaveOut.Stop();
                _audioPlayerWaveOut.Dispose();
                _audioPlayerWaveOut = null;
            }

            if (_currentAudioFile != null)
            {
                _currentAudioFile.Dispose();
                _currentAudioFile = null;
            }

            if (_ttsPlayer != null)
            {
                _ttsPlayer.SpeakAsyncCancelAll();
            }

            // Clear the current playlist and reset state
            _currentPlaylist = null;
            IsPlaying = false;
            CurrentFile = null;

            // Notify that the playlist has finished
            OnPlaylistFinished();
        }
    }

    /// <summary>
    /// Main playback loop to process the playlist queue.
    /// </summary>
    private async Task PlaybackLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            ScheduleItem nextItem = null;

            lock (_lock)
            {
                if (_playlistQueue.Count > 0)
                {
                    nextItem = _playlistQueue.Dequeue(); // Get the next schedule item from the queue
                }
            }

            if (nextItem != null)
            {
                await PlayPlaylist(nextItem, cancellationToken);
            }

            await Task.Delay(100, cancellationToken); // Small delay to avoid busy-waiting
        }
    }

    /// <summary>
    /// Plays a single schedule item.
    /// </summary>
    private async Task PlayPlaylist(ScheduleItem item, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            Stop(); // Stop any ongoing playback

            // Expand file paths (including TTS items)
            var expandedFilePaths = ExpandFilePaths(item.FilePaths);

            // Check if the playlist is empty
            if (expandedFilePaths == null || expandedFilePaths.Count == 0)
            {
                Logger.LogMessage("Playlist is null or empty.");
                return;
            }

            Logger.LogMessage($"Starting new playlist: {item.Name}");

            // Initialize the new playlist
            _currentPlaylist = expandedFilePaths;
            _currentIndex = 0; // Reset the index
        }

        // Start playing the first file
        PlayNextFile();
    }

    /// <summary>
    /// Plays the next file in the playlist.
    /// </summary>
    private void PlayNextFile()
    {
        lock (_lock)
        {
            // Check if _currentPlaylist is null or empty
            if (_currentPlaylist == null || _currentPlaylist.Count == 0)
            {
                Logger.LogMessage("Playlist is null or empty. Stopping playback.");
                IsPlaying = false;
                CurrentFile = null;
                OnPlaylistFinished(); // Notify that the playlist has finished
                return;
            }

            // Check if _currentIndex is within bounds
            if (_currentIndex < 0 || _currentIndex >= _currentPlaylist.Count)
            {
                Logger.LogMessage("Playlist index is out of bounds. Stopping playback.");
                IsPlaying = false;
                CurrentFile = null;
                OnPlaylistFinished(); // Notify that the playlist has finished
                return;
            }

            string nextFile = _currentPlaylist[_currentIndex];

            // Handle TTS
            if (nextFile.StartsWith("TTS:"))
            {
                string text = nextFile.Substring(4); // Extract text after "TTS:"
                Logger.LogMessage($"Playing TTS: {text}");

                // Ensure _ttsPlayer is initialized
                if (_ttsPlayer == null)
                {
                    Logger.LogMessage("TTS player is not initialized.");
                    return;
                }

                // Stop any ongoing TTS playback
                _ttsPlayer.SpeakAsyncCancelAll();

                // Play the TTS
                _ttsPlayer.SpeakAsync(text);
                IsPlaying = true;
                CurrentFile = "TTS";
            }
            // Handle audio file
            else if (File.Exists(nextFile))
            {
                try
                {
                    // Dispose of the previous audio file and player if the file has changed
                    if (_currentAudioFile != null && _currentAudioFile.FileName != nextFile)
                    {
                        _currentAudioFile.Dispose();
                        _currentAudioFile = null;
                    }

                    if (_audioPlayerWaveOut != null)
                    {
                        _audioPlayerWaveOut.Dispose();
                        _audioPlayerWaveOut = null;
                    }

                    // Initialize new audio file (if necessary) and player
                    if (_currentAudioFile == null)
                    {
                        _currentAudioFile = new AudioFileReader(nextFile);
                    }

                    // Ensure _audioPlayerWaveOut is initialized
                    if (_audioPlayerWaveOut == null)
                    {
                        _audioPlayerWaveOut = new WaveOutEvent();
                        _audioPlayerWaveOut.PlaybackStopped += OnPlaybackStopped;
                    }

                    _audioPlayerWaveOut.Init(_currentAudioFile);
                    _audioPlayerWaveOut.Play();

                    IsPlaying = true;
                    CurrentFile = nextFile;
                }
                catch (Exception ex)
                {
                    Logger.LogMessage($"Error playing file {nextFile}: {ex.Message}");
                }
            }
            else
            {
                Logger.LogMessage($"File not found: {nextFile}");
            }

            // Increment the index for the next file
            _currentIndex++;
        }
    }

    /// <summary>
    /// Handles the PlaybackStopped event for audio files.
    /// </summary>
    private void OnPlaybackStopped(object sender, StoppedEventArgs e)
    {
        lock (_lock)
        {
            IsPlaying = false;
            CurrentFile = null;

            // Clean up resources
            if (_currentAudioFile != null)
            {
                _currentAudioFile.Dispose();
                _currentAudioFile = null;
            }

            if (_audioPlayerWaveOut != null)
            {
                _audioPlayerWaveOut.Dispose();
                _audioPlayerWaveOut = null;
            }

            // Play the next file in the playlist
            PlayNextFile();
        }
    }

    /// <summary>
    /// Handles the SpeakCompleted event for TTS.
    /// </summary>
    private void OnTtsCompleted(object sender, SpeakCompletedEventArgs e)
    {
        lock (_lock)
        {
            IsPlaying = false;
            CurrentFile = null;

            // Play the next file in the playlist
            PlayNextFile();
        }
    }

    /// <summary>
    /// Called when the playlist finishes or is stopped.
    /// </summary>
    private void OnPlaylistFinished()
    {
        lock (_lock)
        {
            if (_playlistQueue.Count > 0)
            {
                // Get the current item from the queue
                var currentItem = _playlistQueue.Peek();

                // Update the status of the current item
                if (currentItem != null)
                {
                    currentItem.Status = ScheduleStatus.Played; // Use ScheduleStatus enum
                    Logger.LogMessage($"Playlist finished: {currentItem.Name}. Status updated to Played.");
                }
            }

            // Trigger the PlaylistFinished event
            PlaylistFinished?.Invoke();
        }
    }

    /// <summary>
    /// Expands file paths and folders into a flat list of playable items.
    /// </summary>
    public List<string> ExpandFilePaths(List<FilePathItem> filePathItems)
    {
        var expandedPaths = new List<string>();

        if (filePathItems == null || filePathItems.Count == 0)
        {
            Logger.LogMessage("No file paths provided.");
            return expandedPaths;
        }

        foreach (var item in filePathItems)
        {
            if (!string.IsNullOrEmpty(item.Text)) // Handle TTS
            {
                expandedPaths.Add($"TTS:{item.Text}");
            }
            else if (Directory.Exists(item.Path)) // Handle folder
            {
                var audioFiles = Directory.GetFiles(item.Path, "*.mp3")
                                          .OrderBy(f => f)
                                          .ToList();

                if (item.FolderPlayMode == "Single" && audioFiles.Any())
                {
                    var random = new Random();
                    expandedPaths.Add(audioFiles[random.Next(audioFiles.Count)]);
                }
                else
                {
                    expandedPaths.AddRange(audioFiles);
                }
            }
            else if (File.Exists(item.Path)) // Handle single file
            {
                expandedPaths.Add(item.Path);
            }
            else
            {
                Logger.LogMessage($"File or folder not found: {item.Path}");
            }
        }

        return expandedPaths;
    }
}