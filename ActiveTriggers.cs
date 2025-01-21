      //Be Naame Khoda
//FileName: ActiveTriggers.cs

using System;
using System.Collections.Generic;
using System.Linq;

public static class ActiveTriggers
{
    public enum TriggerSource
    {
        Systematic,
        Manual
    }
    public static List<(string Event, DateTime? Time, TriggerSource Type)> Triggers { get; } = new List<(string Event, DateTime? Time, TriggerSource Type)>();
    public static void AddTrigger(string eventName, DateTime? triggerTime, TriggerSource type)
    {
        // Check if a trigger with the same name already exists
        int existingIndex = Triggers.FindIndex(trigger => trigger.Event.Equals(eventName, StringComparison.OrdinalIgnoreCase));

        if (existingIndex >= 0)
        {
            // Update the existing trigger with the new time
            Triggers[existingIndex] = (eventName, triggerTime, type);
        }
        else
        {
            // Add the new trigger
            Triggers.Add((eventName, triggerTime, type));
        }

    }


    public static void RemoveTrigger(string eventName)
    {
        Triggers.RemoveAll(trigger => trigger.Event == eventName);
    }
    public static bool ContainsTrigger(string eventName)
    {
        return Triggers.Exists(trigger => trigger.Event.Equals(eventName, StringComparison.OrdinalIgnoreCase));
    }

    public static (string Event, DateTime? Time, TriggerSource Type)? GetTrigger(string eventName)
    {
         var trigger = Triggers.FirstOrDefault(trigger => trigger.Event.Equals(eventName, StringComparison.OrdinalIgnoreCase));
           if(trigger.Equals(default((string, DateTime?, TriggerSource))))
              return null;
          return trigger;
    }
    public static void ClearAll()
    {
        Triggers.Clear();
    }
}
public class ActiveTriggersManager : ITriggerManager
{
    public List<(string Event, DateTime? Time, ActiveTriggers.TriggerSource Type)> Triggers => ActiveTriggers.Triggers;

    public bool ContainsTrigger(string eventName)
    {
        return ActiveTriggers.ContainsTrigger(eventName);
    }
}
    