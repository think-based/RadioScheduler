//Be Naame Khoda
//FileName: Program.cs

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        List<string> playlist = new List<string>
        {
            "audio1.mp3",
            "audio2.mp3",
            "audio3.mp3"
        };

        WebServer webServer = new WebServer(playlist);
        await webServer.StartAsync