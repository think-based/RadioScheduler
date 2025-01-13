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
                while (!response.OutputStream.IsClosed)
                {
                    await Task.Delay(1000); // Keep the connection alive
                }
            }
        }
    }

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