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

            // Create a JSON response with all required fields
            var responseData = scheduleItems.Select(item => new
            {
                Name = item.Name,
                StartTime = item.NextOccurrence.ToString("yyyy-MM-dd HH:mm:ss"),
                EndTime = item.NextOccurrence.Add(item.TotalDuration).ToString("yyyy-MM-dd HH:mm:ss"),
                TotalDuration = item.TotalDuration.TotalMilliseconds, // Convert TimeSpan to milliseconds
                LastPlayTime = item.LastPlayTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A",
                TriggerTime = item.TriggerTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A",
                Status = item.Status.ToString()
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