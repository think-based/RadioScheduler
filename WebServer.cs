// Be Naame Khoda
// FileName: WebServer.cs

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Linq;

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
            // Serve schedule-list.html for /schedule-list.html
            else if (path == "/schedule-list.html")
            {
                ServeHtmlFile(response, Path.Combine("wwwroot", "schedule-list.html"));
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
            // API endpoint to fetch schedule list
            else if (path == "/api/schedule-list")
            {
                ServeScheduleList(response);
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

    /// <summary>
    /// Serves the schedule list as a JSON response.
    /// </summary>
    /// <param name="response">The HTTP listener response object.</param>
    private void ServeScheduleList(HttpListenerResponse response)
    {
        try
        {
            // Fetch the list of scheduled items directly from the Scheduler
            var scheduleItems = _scheduler.GetScheduledItems();

            // Create a JSON response
            var responseData = scheduleItems.Select(item => new
            {
                Name = item.Name, // Include the Name field
                Playlist = item.FilePaths.Count > 0 ? item.FilePaths[0].Path : "No files", // Use the first file path or a placeholder
                StartTime = item.NextOccurrence.ToString("yyyy-MM-dd HH:mm:ss"),
                EndTime = item.NextOccurrence.Add(item.Duration).ToString("yyyy-MM-dd HH:mm:ss"),
                TriggerEvent = item.Trigger,
                Status = item.NextOccurrence > DateTime.Now ? "Upcoming" : "In Progress"
            });

            // Serialize the response to JSON
            string jsonResponse = JsonConvert.SerializeObject(responseData);

            // Send the response
            byte[] buffer = Encoding.UTF8.GetBytes(jsonResponse);
            response.ContentType = "application/json";
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
        }
        catch (Exception ex)
        {
            Logger.LogMessage($"Error serving schedule list: {ex.Message}");
            response.StatusCode = (int)HttpStatusCode.InternalServerError;
        }
    }

    /// <summary>
    /// Serves prayer times as a JSON response.
    /// </summary>
    /// <param name="response">The HTTP listener response object.</param>
    /// <param name="request">The HTTP listener request object.</param>
    private void ServePrayerTimes(HttpListenerResponse response, HttpListenerRequest request)
    {
        try
        {
            // Parse query parameters for year, month, and day
            var query = request.QueryString;
            int year = int.Parse(query["year"]);
            int month = int.Parse(query["month"]);
            int day = int.Parse(query["day"]);

            // Calculate prayer times using the PrayTime class
            var prayTime = new PrayTime();
            string[] prayerTimes = prayTime.getPrayerTimes(year, month, day, Settings.Latitude, Settings.Longitude, (int)Settings.TimeZone);

            // Create a JSON response
            var responseData = new
            {
                Fajr = prayerTimes[0],
                Dhuhr = prayerTimes[2],
                Asr = prayerTimes[3],
                Maghrib = prayerTimes[5],
                Isha = prayerTimes[6]
            };

            // Serialize the response to JSON
            string jsonResponse = JsonConvert.SerializeObject(responseData);

            // Send the response
            byte[] buffer = Encoding.UTF8.GetBytes(jsonResponse);
            response.ContentType = "application/json";
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
        }
        catch (Exception ex)
        {
            Logger.LogMessage($"Error serving prayer times: {ex.Message}");
            response.StatusCode = (int)HttpStatusCode.InternalServerError;
        }
    }

    /// <summary>
    /// Handles real-time log streaming using Server-Sent Events (SSE).
    /// </summary>
    /// <param name="response">The HTTP listener response object.</param>
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

    /// <summary>
    /// Serves a static file (CSS, JS, images).
    /// </summary>
    /// <param name="response">The HTTP listener response object.</param>
    /// <param name="path">The path to the static file.</param>
    private void ServeStaticFile(HttpListenerResponse response, string path)
    {
        string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", path.TrimStart('/'));
        if (File.Exists(filePath))
        {
            byte[] fileBytes = File.ReadAllBytes(filePath);
            response.ContentType = GetMimeType(filePath);
            response.ContentLength64 = fileBytes.Length;
            response.OutputStream.Write(fileBytes, 0, fileBytes.Length);
        }
        else
        {
            response.StatusCode = (int)HttpStatusCode.NotFound;
        }
    }

    /// <summary>
    /// Serves an HTML file.
    /// </summary>
    /// <param name="response">The HTTP listener response object.</param>
    /// <param name="filePath">The path to the HTML file.</param>
    private void ServeHtmlFile(HttpListenerResponse response, string filePath)
    {
        if (File.Exists(filePath))
        {
            string htmlContent = File.ReadAllText(filePath);
            byte[] buffer = Encoding.UTF8.GetBytes(htmlContent);
            response.ContentType = "text/html; charset=UTF-8";
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
        }
        else
        {
            response.StatusCode = (int)HttpStatusCode.NotFound;
        }
    }

    /// <summary>
    /// Serves the content of the log file.
    /// </summary>
    /// <param name="response">The HTTP listener response object.</param>
    private void ServeLogContent(HttpListenerResponse response)
    {
        try
        {
            string logContent = File.Exists(Logger.LogFilePath) ? File.ReadAllText(Logger.LogFilePath) : "Log file not found.";
            byte[] buffer = Encoding.UTF8.GetBytes(logContent);
            response.ContentType = "text/plain";
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
        }
        catch (Exception ex)
        {
            Logger.LogMessage($"Error serving log content: {ex.Message}");
            response.StatusCode = (int)HttpStatusCode.InternalServerError;
        }
    }

    /// <summary>
    /// Clears the log file.
    /// </summary>
    /// <param name="response">The HTTP listener response object.</param>
    private void ClearLog(HttpListenerResponse response)
    {
        try
        {
            if (File.Exists(Logger.LogFilePath))
            {
                File.WriteAllText(Logger.LogFilePath, string.Empty);
                Logger.LogMessage("Log file cleared.");
            }

            // Return a simple success response
            response.StatusCode = (int)HttpStatusCode.OK;
        }
        catch (Exception ex)
        {
            Logger.LogMessage($"Error clearing log file: {ex.Message}");
            response.StatusCode = (int)HttpStatusCode.InternalServerError;
        }
    }

    /// <summary>
    /// Gets the MIME type for a file based on its extension.
    /// </summary>
    /// <param name="filePath">The path to the file.</param>
    /// <returns>The MIME type as a string.</returns>
    private string GetMimeType(string filePath)
    {
        string extension = Path.GetExtension(filePath).ToLower();
        switch (extension)
        {
            case ".css": return "text/css";
            case ".js": return "application/javascript";
            case ".jpg": return "image/jpeg";
            case ".png": return "image/png";
            case ".html": return "text/html";
            default: return "text/plain";
        }
    }
}