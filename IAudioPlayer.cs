//Be Naame Khoda
//FileName: IAudioPlayer.cs

using System;

public interface IAudioPlayer
{
    void Play(ScheduleItem item);
    void Stop();
    bool IsPlaying { get; }
    ScheduleItem GetCurrentScheduledItem(); // Add this line
    event Action<ScheduleItem> PlaylistFinished;
}
