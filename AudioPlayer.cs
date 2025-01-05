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

    // ویژگی‌های جدید
    public bool IsPlaying { get; private set; }
    public string CurrentFile { get; private set; }

    public event Action PlaylistFinished;

    public AudioPlayer()
    {
        _audioPlayerWaveOut = new WaveOutEvent();
        _audioPlayerWaveOut.PlaybackStopped += OnPlaybackStopped;
        IsPlaying = false;
        CurrentFile = null;
    }

    public void Play(List<string> audioFilePaths)
    {
        Stop();
        _currentPlaylist = audioFilePaths;
        _currentIndex = 0;
        PlayNextFile();
    }

    public void Stop()
    {
        _audioPlayerWaveOut?.Stop();
        _audioPlayerWaveOut?.Dispose();
        _currentAudioFile?.Dispose();
        _currentPlaylist = null;
        IsPlaying = false;
        CurrentFile = null;
    }

    private void PlayNextFile()
    {
        if (_currentIndex >= _currentPlaylist.Count)
        {
            Stop();
            PlaylistFinished?.Invoke();
            return;
        }

        string currentFile = _currentPlaylist[_currentIndex];
        if (!File.Exists(currentFile))
        {
            Logger.LogMessage($"File not found: {currentFile}"); // ثبت لاگ برای فایل