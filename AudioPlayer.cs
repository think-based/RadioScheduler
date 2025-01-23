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
using static Enums;
using System.Diagnostics;

public class AudioPlayer : IAudioPlayer
{
    private WaveOutEvent _audioPlayerWaveOut;
    private AudioFileReader _currentAudioFile;
    private List<string> _currentPlaylist;
    private int _currentIndex;
    private SpeechSynthesizer _ttsPlayer;
    private readonly object _lock = new object();

    private Queue<ScheduleItem> _playlistQueue = new Queue<ScheduleItem>();
    private Task _playbackTask;
    private CancellationTokenSource _playbackCancellationTokenSource;
    private ScheduleItem _scheduleItem = null;
    private readonly ISchedulerConfigManager _configManager;

    public bool IsPlaying { get; private set; }
    public string CurrentFile { get; private set; }

    public event Action<ScheduleItem> PlaylistFinished;
    public event Action PlaylistStoped;


    public AudioPlayer(ISchedulerConfigManager configManager)
    {
        _configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));

        _audioPlayerWaveOut = new WaveOutEvent();
        _audioPlayerWaveOut.PlaybackStopped += OnPlaybackStopped;
        _ttsPlayer = new SpeechSynthesizer();
        _ttsPlayer.SpeakCompleted += OnTtsCompleted;
        IsPlaying = false;
        CurrentFile = null;

        _playbackCancellationTokenSource = new CancellationTokenSource();
        _playbackTask = Task.Run(() => PlaybackLoop(_playbackCancellationTokenSource.Token));
    }

    public void Play(ScheduleItem item)
    {
        _scheduleItem = item;
        lock (_lock)
        {
            _playlistQueue.Enqueue(item);
        }
    }

    public void Stop()
    {
        lock (_lock)
        {
            _playlistQueue.Clear();

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

            _currentPlaylist = null;
            IsPlaying = false;
            CurrentFile = null;
            if (_scheduleItem != null && _scheduleItem.Status == ScheduleStatus.Playing)
            {
                _scheduleItem.Status = ScheduleStatus.Stopped;
                PlaylistStoped?.Invoke();
                Logger.LogMessage($"Playlist Stoped: {_scheduleItem.Name}. Status updated to Stoped.");
                Debug.Print($"Playlist Stoped: {_scheduleItem.Name}. Status updated to Stoped.");

            }

        }
    }

    private async Task PlaybackLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            ScheduleItem nextItem = null;

            lock (_lock)
            {
                if (_playlistQueue.Count > 0)
                {
                    nextItem = _playlistQueue.Dequeue();
                }
            }

            if (nextItem != null)
            {
                await PlayPlaylist(nextItem, cancellationToken);
            }

            await Task.Delay(100, cancellationToken);
        }
    }

    private async Task PlayPlaylist(ScheduleItem item, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            Stop();

            var expandedFilePaths = ExpandFilePaths(item.FilePaths);

            if (expandedFilePaths == null || expandedFilePaths.Count == 0)
            {
                Logger.LogMessage("Playlist is null or empty.");
                return;
            }

            Logger.LogMessage($"Starting new playlist: {item.Name}");

            _currentPlaylist = expandedFilePaths;
            _currentIndex = 0;
        }
        _scheduleItem.Status = ScheduleStatus.Playing;

        PlayNextFile();
    }

    private void PlayNextFile()
    {
        lock (_lock)
        {
            if (_currentPlaylist == null || _currentPlaylist.Count == 0)
            {
                Logger.LogMessage("Playlist is null or empty. Stopping playback.");
                IsPlaying = false;
                CurrentFile = null;

                var currentItem = _playlistQueue.Count > 0 ? _playlistQueue.Peek() : null;
                OnPlaylistFinished();
                return;
            }

            if (_currentIndex < 0 || _currentIndex >= _currentPlaylist.Count)
            {
                Logger.LogMessage("Playlist index is out of bounds. Stopping playback.");
                IsPlaying = false;
                CurrentFile = null;

                var currentItem = _playlistQueue.Count > 0 ? _playlistQueue.Peek() : null;
                OnPlaylistFinished();
                return;
            }

            string nextFile = _currentPlaylist[_currentIndex];

            if (nextFile.StartsWith("TTS:"))
            {
                string text = nextFile.Substring(4);
                Logger.LogMessage($"Playing TTS: {text}");

                if (_ttsPlayer == null)
                {
                    Logger.LogMessage("TTS player is not initialized.");
                    return;
                }

                _ttsPlayer.SpeakAsyncCancelAll();

                _ttsPlayer.SpeakAsync(text);
                IsPlaying = true;
                CurrentFile = "TTS";
            }
            else if (File.Exists(nextFile))
            {
                try
                {
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

                    if (_currentAudioFile == null)
                    {
                        _currentAudioFile = new AudioFileReader(nextFile);
                    }

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

            _currentIndex++;
        }
    }

    private void OnPlaybackStopped(object sender, StoppedEventArgs e)
    {
        lock (_lock)
        {
            IsPlaying = false;
            CurrentFile = null;

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

            PlayNextFile();
        }
    }

    private void OnTtsCompleted(object sender, SpeakCompletedEventArgs e)
    {
        lock (_lock)
        {
            IsPlaying = false;
            CurrentFile = null;

            PlayNextFile();
        }
    }

    private void OnPlaylistFinished()
    {
        lock (_lock)
        {
            if (_scheduleItem != null && _scheduleItem.Status == ScheduleStatus.Playing)
            {
                _scheduleItem.Status = ScheduleStatus.Played;
                Logger.LogMessage($"Playlist finished: {_scheduleItem.Name}. Status updated to Played.");

                _configManager.ReloadScheduleItem(_scheduleItem.ItemId);
                PlaylistFinished?.Invoke(_scheduleItem);
            }

        }
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
            if (!string.IsNullOrEmpty(item.Text))
            {
                expandedPaths.Add($"TTS:{item.Text}");
            }
            else if (Directory.Exists(item.Path))
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

    public ScheduleItem GetCurrentScheduledItem()
    {
        lock (_lock)
        {
            if (_scheduleItem != null && _scheduleItem.Status == ScheduleStatus.Playing)
            {
                return _scheduleItem;
            }
            return null;
        }
    }
}
    