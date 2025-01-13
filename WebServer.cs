// Be Naame Khoda
// FileName: WebServer.cs

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class WebServer
{
    private HttpListener _listener;
    private Scheduler _scheduler;

    public WebServer(Scheduler scheduler)
    {
        _scheduler = scheduler;
        _listener = new HttpListener();
        _listener.Prefixes.Add("http://localhost:8080/");
    }

    public void Start()
    {
        _listener.Start();
        Task.Run(() => Listen());
    }

    private async void Listen()
    {
        while (_listener.IsListening)
        {
            var context = await _listener.GetContextAsync();
            ProcessRequest(context);
        }
    }

    private void ProcessRequest(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        string path = request.Url.AbsolutePath;

        try
        {
            // Serve static files (CSS, JS, images)
            if (path.StartsWith("/css/") || path.StartsWith("/js/") || path.StartsWith("/img/"))
            {
                ServeStaticFile(response, path);
            }
            // Serve index.html for the root URL
            else if (path == "/")
            {
                ServeHtmlFile(response, Path.Combine("wwwroot", "index.html"));
            }
            // Serve home.html for /home.html
            else if (path == "/home.html")
            {
                ServeHtmlFile(response, Path.Combine("wwwroot", "home.html"));
            }
            // Serve viewlog.html for /viewlog.html
            else if (path == "/viewlog.html")
            {
                ServeHtmlFile(response, Path.Combine("wwwroot", "viewlog.html"));
            }
            // Handle AJAX requests for dynamic content
            else if (path == "/clearlog")
            {
                ClearLog(response);
            }
            // API endpoint to fetch log content
            else if (path == "/api/logs")
            {
                ServeLogContent(response);
            }
            // API endpoint to stream log updates in real time
            else if (path == "/api/logs/stream")
            {
                HandleLogStream(response).Wait(); // Handle the SSE stream
            }
            // API endpoint to fetch prayer times
            else if (path.StartsWith("/api/prayertimes"))
            {
                ServePrayerTimes(response, request);
            }
            else
            {
                response.StatusCode = (int)HttpStatusCode.NotFound;
            }
        }
        catch (Exception ex)
        {
            Logger.LogMessage($"Error processing request: {ex.Message}");
            response.StatusCode = (int)HttpStatusCode.InternalServerError;
        }
        finally
        {
            response.OutputStream.Close();
        }
    }

    private async Task HandleLogStream(HttpListenerResponse response)
    {
        response.ContentType = "text/event-stream";
        response.AddHeader("Cache-Control", "no-cache");
        response.AddHeader("Connection", "keep-alive");

        using (var writer = new StreamWriter(response.OutputStream))
        {
            // Send initial log content
            string initialLogContent = File.Exists(Logger.LogFilePath) ? File.ReadAllText(Logger.LogFilePath) : "Log file not found.";
            await writer.WriteLineAsync($"data: {initialLogContent}\n\n");
            await writer.FlushAsync();

            // Watch the log file for changes
            using (var watcher = new FileSystemWatcher(Path.GetDirectoryName(Logger.LogFilePath), Path.GetFileName(Logger.LogFilePath)))
            {
                watcher.NotifyFilter = NotifyFilters.LastWrite;
                watcher.Changed += async (sender, e) =>
                {
                    try
                    {
                        string newLogContent = File.ReadAllText(Logger.LogFilePath);
                        await writer.WriteLineAsync($"data: {newLogContent}\n\n");
                        await writer.FlushAsync();
                    }
                    catch (Exception ex)
                    {
                        Logger.LogMessage($"Error streaming log updates: {ex.Message}");
                    }
                };
                watcher.EnableRaisingEvents = true;

                // Keep the connection open
                while (true)
                {
                    try
                    {
                        // Check if the stream is still open by attempting to write a heartbeat
                        await writer.WriteLineAsync(": heartbeat\n\n");
                        await writer.FlushAsync();
                        await Task.Delay(1000); // Wait for 1 second
                    }
                    catch (IOException)
                    {
                        // Stream is closed, exit the loop
                        break;
                    }
                    catch (ObjectDisposedException)
                    {
                        // Stream is disposed, exit the loop
                        break;
                    }
                }
            }
        }
    }

    // Other methods (ServeStaticFile, ServeHtmlFile, ServeLogContent, ServePrayerTimes, ClearLog, GetMimeType) remain unchanged
}