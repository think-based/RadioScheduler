//Be Naame Khoda
//FileName: AudioPlayer.cs

using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;

public class AudioPlayer
{
    private WaveOutEvent _audioPlayerWaveOut;
    private AudioFileReader _currentAudioFile;
    private List<string> _currentPlaylist;
    private int _currentIndex;

    // رویداد اتمام پخش کل لیست
    public event Action PlaylistFinished;

    public AudioPlayer()
    {
        _audioPlayerWaveOut = new WaveOutEvent();
        _audioPlayerWaveOut.PlaybackStopped += OnPlaybackStopped;
    }

    public void Play(List<string> audioFilePaths)
    {
        // توقف پلی‌لیست قبلی (اگر در حال پخش باشد)
        Stop();

        // شروع پلی‌لیست جدید
        _currentPlaylist = audioFilePaths;
        _currentIndex = 0;

        // پخش اولین فایل
        PlayNextFile();
    }

    public void Stop()
    {
        if (_audioPlayerWaveOut != null)
        {
            _audioPlayerWaveOut.Stop();
            _audioPlayerWaveOut.Dispose();
        }
        if (_currentAudioFile != null)
        {
            _currentAudioFile.Dispose();
        }
        _currentPlaylist = null; // پلی‌لیست فعلی را پاک کن
    }

    private void PlayNextFile()
    {
        if (_currentIndex >= _currentPlaylist.Count)
        {
            Stop();
            PlaylistFinished?.Invoke(); // اطلاع‌رسانی اتمام پلی‌لیست
            return;
        }

        // بررسی وجود فایل
        string currentFile = _currentPlaylist[_currentIndex];
        if (!File.Exists(currentFile))
        {
            Logger.LogMessage($"File not found: {currentFile}");
            _currentIndex++; // از این فایل صرف‌نظر کرده و به فایل بعدی برو
            PlayNextFile();
            return;
        }

        // توقف پخش قبلی (اگر در حال پخش باشد)
        if (_audioPlayerWaveOut != null)
        {
            _audioPlayerWaveOut.Stop();
            _audioPlayerWaveOut.Dispose();
        }

        // بارگذاری فایل صوتی جدید
        try
        {
            _currentAudioFile = new AudioFileReader(currentFile);
            _audioPlayerWaveOut = new WaveOutEvent();
            _audioPlayerWaveOut.Init(_currentAudioFile);
            _audioPlayerWaveOut.Play();
        }
        catch (Exception ex)
        {
            Logger.LogMessage($"Error playing file {currentFile}: {ex.Message}");
            _currentIndex++; // از این فایل صرف‌نظر کرده و به فایل بعدی برو
            PlayNextFile();
        }
    }

    private void OnPlaybackStopped(object sender, StoppedEventArgs e)
    {
        // پخش فایل بعدی در پلی‌لیست
        _currentIndex++;
        PlayNextFile();
    }

    public TimeSpan CalculateTotalDuration(List<string> audioFilePaths)
    {
        TimeSpan totalDuration = TimeSpan.Zero;
        foreach (var file in audioFilePaths)
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