      //Be Naame Khoda
//FileName: ISchedulerConfigManager.cs
using System;
using System.Collections.Generic;

public interface ISchedulerConfigManager
{
    List<ScheduleItem> ScheduleItems { get; }
    event Action ConfigReloaded;
    void ReloadScheduleConfig();
    void ReloadScheduleItem(int configId); // Add this method
                                           // Add the new method definition
    void ProcessScheduleItem(ScheduleItem item);
    void ReloadScheduleItemById(int itemId);
}