// Be Naame Khoda
// FileName: AudioPlayer.cs

using NAudio.Wave;
using RadioScheduler.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Speech.Synthesis; // Add this for TTS

public class AudioPlayer
{
    private WaveOutEvent _audioPlayerWaveOut;
    private AudioFileReader _currentAudioFile;
    private List<string> _currentPlaylist;
    private int _currentIndex;
    private SpeechSynthesizer _ttsPlayer; // TTS player

    public bool IsPlaying { get; private set; }
    public string CurrentFile { get; private set; }

    public event Action PlaylistFinished; // Event to signal when playback is finished

    public AudioPlayer()
    {
        _audioPlayerWaveOut = new WaveOutEvent();
        _audioPlayerWaveOut.PlaybackStopped += OnPlaybackStopped; // Subscribe to PlaybackStopped event
        _ttsPlayer = new SpeechSynthesizer(); // Initialize TTS player
        _ttsPlayer.SpeakCompleted += OnTtsCompleted; // Subscribe to TTS completion event
        IsPlaying = false;
        CurrentFile = null;
    }

    public void Play(List<FilePathItem> filePathItems)
    {
        Stop();
        var expandedFilePaths = ExpandFilePaths(filePathItems);
        _currentPlaylist = expandedFilePaths;
        _currentIndex = 0;
        PlayNextFile();
    }

    public void Stop()
    {
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
            _ttsPlayer.SpeakAsyncCancelAll(); // Stop any ongoing TTS
        }

        _currentPlaylist = null;
        IsPlaying = false;
        CurrentFile = null;
    }

    private void PlayNextFile()
    {
        // Check if _currentPlaylist is null or empty
        if (_currentPlaylist == null || _currentPlaylist.Count == 0)
        {
            Logger.LogMessage("Playlist is null or empty.");
            Stop();
            PlaylistFinished?.Invoke(); // Raise the PlaylistFinished event
            return;
        }

        // Check if we've reached the end of the playlist
        if (_currentIndex >= _currentPlaylist.Count)
        {
            Logger.LogMessage("End of playlist reached.");
            Stop();
            PlaylistFinished?.Invoke(); // Raise the PlaylistFinished event
            return;
        }

        string currentItem = _currentPlaylist[_currentIndex];

        if (currentItem.StartsWith("TTS:")) // Handle TTS
        {
            string text = currentItem.Substring(4); // Extract the text
            Logger.LogMessage($"Playing TTS: {text}");
            _ttsPlayer.SpeakAsync(text); // Play the text as speech
            IsPlaying = true;
            CurrentFile = "TTS";
        }
        else if (File.Exists(currentItem)) // Handle MP3 file
        {
            try
            {
                // Dispose of the previous audio file and player
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

                // Initialize new audio file and player
                _currentAudioFile = new AudioFileReader(currentItem);
                _audioPlayerWaveOut = new WaveOutEvent();
                _audioPlayerWaveOut.PlaybackStopped += OnPlaybackStopped; // Re-subscribe to the event
                _audioPlayerWaveOut.Init(_currentAudioFile);
                _audioPlayerWaveOut.Play();

                IsPlaying = true;
                CurrentFile = currentItem;
            }
            catch (Exception ex)
            {
                Logger.LogMessage($"Error playing file {currentItem}: {ex.Message}");
                _currentIndex++;
                PlayNextFile(); // Skip to the next file
            }
        }
        else
        {
            Logger.LogMessage($"File not found: {currentItem}");
            _currentIndex++;
            PlayNextFile(); // Skip to the next file
        }
    }

    private void OnPlaybackStopped(object sender, StoppedEventArgs e)
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
        _currentIndex++;
        PlayNextFile();
    }

    private void OnTtsCompleted(object sender, SpeakCompletedEventArgs e)
    {
        IsPlaying = false;
        CurrentFile = null;

        // Play the next file in the playlist
        _currentIndex++;
        PlayNextFile();
    }

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

    public TimeSpan CalculateTotalDuration(List<FilePathItem> filePathItems)
    {
        var expandedFilePaths = ExpandFilePaths(filePathItems);
        TimeSpan totalDuration = TimeSpan.Zero;
        foreach (var file in expandedFilePaths)
        {
            if (file.StartsWith("TTS:")) // Estimate TTS duration (e.g., 1 second per 10 characters)
            {
                string text = file.Substring(4);
                totalDuration += TimeSpan.FromSeconds(text.Length / 10.0);
            }
            else if (File.Exists(file))
            {
                try
                {
                    using (var reader = new AudioFileReader(file))
                    {
                        totalDuration += reader.TotalTime;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogMessage($"Error calculating duration for file {file}: {ex.Message}");
                }
            }
        }
        return totalDuration;
    }
}