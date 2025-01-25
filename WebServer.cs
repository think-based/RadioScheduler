      //Be Naame Khoda
//FileName: WebServer.cs
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RadioSchedulerService;
using System.Linq;
using System.Collections.Generic;

public class WebServer
{
    private HttpListener _listener;
    private readonly ApiRequestHandler _apiHandler;
    private readonly Scheduler _scheduler;
    public WebServer(Scheduler scheduler)
    {
        var triggerManager = new ActiveTriggersManager();
        _apiHandler = new ApiRequestHandler(triggerManager, scheduler);
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
            // Serve events.html for /triggers.html
            else if (path == "/triggers.html")
            {
                ServeHtmlFile(response, Path.Combine("wwwroot", "triggers.html"));
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
            // API endpoint to get the timezone
            else if (path == "/api/timezone")
            {
                _apiHandler.ServeTimezone(response);
            }
            // API endpoint to fetch schedule list
            else if (path == "/api/schedule-list")
            {
                switch (request.HttpMethod)
                {
                    case "GET":
                        _apiHandler.ServeScheduleList(response);
                        break;
                    case "POST":
                        _apiHandler.ReloadScheduleItem(request, response);
                        break;
                    default:
                        response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                        break;
                }

            }
            else if (path.StartsWith("/api/schedule-list/"))
            {
                if (request.HttpMethod == "GET")
                    _apiHandler.ServePlayListByScheduleItemId(request, response);
                else
                    response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
            }
            // API endpoint to fetch prayer times
            else if (path == "/api/prayertimes")
            {
                ServePrayerTimes(request, response);
            }
            // API endpoint to fetch, delete and add triggers
            else if (path == "/api/triggers")
            {
                switch (request.HttpMethod)
                {
                    case "GET":
                        _apiHandler.ServeTriggers(response);
                        break;
                    case "POST":
                        _apiHandler.AddTrigger(request, response);
                        break;
                    case "PUT":
                        _apiHandler.EditTrigger(request, response);
                        break;
                    case "DELETE":
                        _apiHandler.DeleteTrigger(request, response);
                        break;
                    default:
                        response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                        break;
                }
            }
            // API endpoint to fetch trigger by name
            else if (path.StartsWith("/api/triggers/"))
            {
                if (request.HttpMethod == "GET")
                    _apiHandler.ServeTriggerByName(request, response);
                else
                    response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
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
            response.StatusDescription = $"Error processing request: {ex.Message}";
            _apiHandler.WriteStringResponse(response, response.StatusDescription);
        }
        finally
        {
            response.OutputStream.Close();
        }
    }
    /// <summary>
    /// Serves the prayer times as a JSON response.
    /// </summary>
    /// <param name="request">The HTTP request object.</param>
    /// <param name="response">The HTTP response object.</param>
    private void ServePrayerTimes(HttpListenerRequest request, HttpListenerResponse response)
    {
        try
        {
            // Get the query parameters from the request
            int year = int.Parse(request.QueryString.Get("year"));
            int month = int.Parse(request.QueryString.Get("month"));
            int day = int.Parse(request.QueryString.Get("day"));

            // Calculate prayer times using PrayTime class
            var prayTime = new PrayTime();
            string[] prayerTimes = prayTime.getPrayerTimes(year, month, day, Settings.Latitude, Settings.Longitude, (int)Settings.TimeZoneOffset);
            // Create an anonymous object to hold the prayer times
            var prayerTimesObject = new
            {
                Fajr = prayerTimes[0],
                Sunrise = prayerTimes[1],
                Dhuhr = prayerTimes[2],
                Asr = prayerTimes[3],
                Sunset = prayerTimes[4],
                Maghrib = prayerTimes[5],
                Isha = prayerTimes[6]
            };

            // Serialize the object into JSON
            string jsonResponse = JsonConvert.SerializeObject(prayerTimesObject);

            // Set response headers and send the JSON content
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
        var mimeTypes = new Dictionary<string, string>
        {
            { ".css", "text/css" },
            { ".js", "application/javascript" },
            { ".jpg", "image/jpeg" },
            { ".png", "image/png" },
            { ".html", "text/html" },
            { ".json", "application/json" }
        };
        return mimeTypes.TryGetValue(extension, out string mimeType) ? mimeType : "text/plain";
    }
}
    