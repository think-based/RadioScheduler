//Be Naame Khoda
//FileName: AudioPlayer.cs

using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class AudioPlayer
{
    private WaveOutEvent _audioPlayerWaveOut;
    private AudioFileReader _currentAudioFile;
    private List<string> _currentPlaylist;
    private int _currentIndex;

    public bool IsPlaying { get; private set; }
    public string CurrentFile { get; private set; }

    public event Action PlaylistFinished; // Event to signal when playback is finished

    public AudioPlayer()
    {
        _audioPlayerWaveOut = new WaveOutEvent();
        _audioPlayerWaveOut.PlaybackStopped += OnPlaybackStopped; // Subscribe to PlaybackStopped event
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

        _currentPlaylist = null;
        IsPlaying = false;
        CurrentFile = null;
    }

    private void PlayNextFile()
    {
        if (_currentIndex >= _currentPlaylist.Count)
        {
            Stop();
            PlaylistFinished?.Invoke(); // Raise the PlaylistFinished event
            return;
        }

        string currentFile = _currentPlaylist[_currentIndex];
        if (!File.Exists(currentFile))
        {
            Logger.LogMessage($"File not found: {currentFile}");
            _currentIndex++;
            PlayNextFile();
            return;
        }

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
            _currentAudioFile = new AudioFileReader(currentFile);
            _audioPlayerWaveOut = new WaveOutEvent();
            _audioPlayerWaveOut.PlaybackStopped += OnPlaybackStopped; // Re-subscribe to the event
            _audioPlayerWaveOut.Init(_currentAudioFile);
            _audioPlayerWaveOut.Play();

            IsPlaying = true;
            CurrentFile = currentFile;
        }
        catch (Exception ex)
        {
            Logger.LogMessage($"Error playing file {currentFile}: {ex.Message}");
            _currentIndex++;
            PlayNextFile();
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

    public List<string> ExpandFilePaths(List<FilePathItem> filePathItems)
    {
        var expandedPaths = new List<string>();

        foreach (var item in filePathItems)
        {
            if (Directory.Exists(item.Path))
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
            else if (File.Exists(item.Path))
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
            if (File.Exists(file))
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