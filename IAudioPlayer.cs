      //Be Naame Khoda
//FileName: IAudioPlayer.cs

using System.Collections.Generic;
using RadioScheduler.Entities;

public interface IAudioPlayer
{
    void Play(ScheduleItem item); // Updated to accept ScheduleItem
    void Stop();
    bool IsPlaying { get; }
}
    