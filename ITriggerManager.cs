      //Be Naame Khoda
//FileName: ITriggerManager.cs

using System;
using System.Collections.Generic;
using static ActiveTriggers;

public interface ITriggerManager
{
    List<(string Event, DateTime? Time, TriggerSource Type)> Triggers { get; }
    bool ContainsTrigger(string eventName);
}
    