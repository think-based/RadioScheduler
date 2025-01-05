//Be Naame Khoda
//FileName: Program.cs

using System;
using System.ServiceProcess;

namespace RadioSchedulerService
{
    static class Program
    {
        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new RadioSchedulerService()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}