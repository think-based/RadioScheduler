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

    // Add an event that will be triggered when triggers change
     public static event Action TriggersChanged;

    public static void AddTrigger(string eventName, DateTime? triggerTime, TriggerSource type)
    {
          var existingTrigger = Triggers.FirstOrDefault(t => t.Event.Equals(eventName, StringComparison.OrdinalIgnoreCase));
          if (existingTrigger != default((string, DateTime?, TriggerSource)))
          {
             int index = Triggers.IndexOf(existingTrigger);
            Triggers[index] = (eventName, triggerTime, type);
           }
          else
           {
              Triggers.Add((eventName, triggerTime, type));
            }

            // Trigger the event when a trigger is added or updated
           TriggersChanged?.Invoke();
    }


    public static void RemoveTrigger(string eventName)
    {
        Triggers.RemoveAll(t => t.Event == eventName);

        // Trigger the event when a trigger is removed
       TriggersChanged?.Invoke();
    }
    public static bool ContainsTrigger(string eventName)
    {
        return Triggers.Exists(t => t.Event.Equals(eventName, StringComparison.OrdinalIgnoreCase));
    }

    public static (string Event, DateTime? Time, TriggerSource Type)? GetTrigger(string eventName)
    {
        return Triggers.FirstOrDefault(t => t.Event.Equals(eventName, StringComparison.OrdinalIgnoreCase));
    }
    public static void ClearAll()
    {
        Triggers.Clear();
        // Trigger the event when all triggers are cleared
         TriggersChanged?.Invoke();
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
    