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

   