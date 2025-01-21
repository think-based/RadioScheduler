      //Be Naame Khoda
//FileName: IAudioPlayer.cs

using System.Collections.Generic;
using RadioScheduler.Entities;

public interface IAudioPlayer
{
    void Play(List<FilePathItem> filePathItems);
    void Stop();
    bool IsPlaying { get; }
}
    