      //Be Naame Khoda
//FileName: IAudioPlayer.cs

using System.Collections.Generic;

public interface IAudioPlayer
{
    void Play(ScheduleItem item);
    void Stop();
    bool IsPlaying { get; }
    ScheduleItem GetCurrentScheduledItem(); // Add this line
}
    