//Be Naame Khoda
//FileName: Scheduler.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Timers;
using Newtonsoft.Json;

public class Scheduler
{
    // اونت‌ها
    public event Action BeforePlayback;
   