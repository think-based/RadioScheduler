      // Be Naame Khoda
// FileName: WebServer.cs

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Linq;
using static ActiveTriggers;
using System.Globalization;

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
            // API endpoint to fetch schedule list
            else if (path == "/api/schedule-list")
            {
                ServeScheduleList(response);
            }
            // API endpoint to fetch prayer times
            else if (path == "/api/prayertimes")
            {
                ServePrayerTimes(request, response);
            }
             // API endpoint to get the timezone
            else if (path == "/api/timezone")
            {
                ServeTimezone(response);
            }
            // API endpoint to fetch, delete and add triggers
            else if (path == "/api/triggers")
            {
                switch (request.HttpMethod)
                {
                    case "GET":
                        ServeTriggers(response);
                        break;
                    case "POST":
                        AddTrigger(request, response);
                        break;
                    case "PUT":
                        EditTrigger(request, response);
                        break;
                    case "DELETE":
                        DeleteTrigger(request, response);
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
                    ServeTriggerByName(request, response);
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
             WriteStringResponse(response, response.StatusDescription);
        }
        finally
        {
            response.OutputStream.Close();
        }
    }
       private void ServeTimezone(HttpListenerResponse response)
    {
         try
        {
            // Create an anonymous object to hold the prayer times
            var timeZoneObject = new
            {
                 TimeZone = Settings.TimeZone
            };

            // Serialize the object into JSON
            string jsonResponse = JsonConvert.SerializeObject(timeZoneObject);

            // Set response headers and send the JSON content
            byte[] buffer = Encoding.UTF8.GetBytes(jsonResponse);
            response.ContentType = "application/json";
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);

        }
        catch (Exception ex)
        {
            Logger.LogMessage($"Error serving time zone: {ex.Message}");
            response.StatusCode = (int)HttpStatusCode.InternalServerError;
        }
    }
    /// <summary>
    /// Serves all triggers.
    /// </summary>
    /// <param name="response">The HTTP listener response object.</param>
    private void ServeTriggers(HttpListenerResponse response)
    {
        try
        {
            var triggers = ActiveTriggers.Triggers.Select(item => new
            {
                triggerEvent = item.Event,
                time = item.Time,
                type = item.Type.ToString()
            }).ToList();

            var jsonResponse = JsonConvert.SerializeObject(triggers);
            var buffer = Encoding.UTF8.GetBytes(jsonResponse);
            response.ContentType = "application/json";
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
        }
        catch (Exception ex)
        {
            Logger.LogMessage($"Error serving triggers: {ex.Message}");
            response.StatusCode = (int)HttpStatusCode.InternalServerError;
        }
    }
    /// <summary>
    /// Serves a single trigger by name.
    /// </summary>
    /// <param name="request">The HTTP request object.</param>
    /// <param name="response">The HTTP response object.</param>
    private void ServeTriggerByName(HttpListenerRequest request, HttpListenerResponse response)
    {
        try
        {
            string eventName = request.Url.Segments.Last();
            var trigger = ActiveTriggers.Triggers.FirstOrDefault(t => t.Event.Equals(eventName, StringComparison.OrdinalIgnoreCase));

            if (trigger.Equals(default((string, DateTime?, TriggerSource))))
            {
                response.StatusCode = (int)HttpStatusCode.NotFound;
                return;
            }

            var triggerObj = new
            {
                triggerEvent = trigger.Event,
                time = trigger.Time,
                type = trigger.Type.ToString()
            };

            string jsonResponse = JsonConvert.SerializeObject(triggerObj);
            byte[] buffer = Encoding.UTF8.GetBytes(jsonResponse);
            response.ContentType = "application/json";
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
        }
        catch (Exception ex)
        {
            Logger.LogMessage($"Error serving trigger by name: {ex.Message}");
            response.StatusCode = (int)HttpStatusCode.InternalServerError;
        }
    }
    /// <summary>
    /// Adds a manual trigger
    /// </summary>
    /// <param name="request">The HTTP request object.</param>
    /// <param name="response">The HTTP response object.</param>
  private void AddTrigger(HttpListenerRequest request, HttpListenerResponse response)
    {
        string message = "";
        try
        {
             string requestBody;
            using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
            {
                requestBody = reader.ReadToEnd();
            }
            dynamic data;
             try
            {
                  data = JsonConvert.DeserializeObject<dynamic>(requestBody);
             }
            catch (JsonSerializationException ex)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                 message =  "Invalid JSON format in the request body.";
                response.StatusDescription = message;
               WriteStringResponse(response, message);
                 return;
            }
            if (data == null || string.IsNullOrEmpty(data.triggerEvent?.ToString()))
             {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                message= "The triggerEvent cannot be empty";
                response.StatusDescription = message;
                WriteStringResponse(response, message);
                return;
             }

            string eventName = data.triggerEvent.ToString();
            DateTime? triggerTime = null;
             if (data.time != null && !string.IsNullOrEmpty(data.time.ToString()))
            {
                DateTime parsedTime;
                if (DateTime.TryParse(data.time.ToString(), null, System.Globalization.DateTimeStyles.AssumeUniversal, out parsedTime))
                 {
                     TimeZoneInfo systemTimeZone = TimeZoneInfo.Local;
                    TimeZoneInfo targetTimeZone = TimeZoneInfo.CreateCustomTimeZone(
                     "ApplicationTimeZone",
                     TimeSpan.FromHours(Settings.TimeZone),
                     "ApplicationTimeZone",
                     "ApplicationTimeZone");

                     try{
                          triggerTime = TimeZoneInfo.ConvertTime(parsedTime, TimeZoneInfo.Utc, targetTimeZone);
                    }
                   catch (Exception ex)
                    {
                         response.StatusCode = (int)HttpStatusCode.BadRequest;
                        message=  $"Time zone Conversion error,  {ex.Message}";
                        response.StatusDescription = message;
                          WriteStringResponse(response, message);
                        return;
                    }

                }
                else
                {
                     response.StatusCode = (int)HttpStatusCode.BadRequest;
                      message = "Invalid time format. Please provide a valid date and time.";
                     response.StatusDescription = message;
                      WriteStringResponse(response, message);
                    return;
                }
            }

             ActiveTriggers.AddTrigger(eventName, triggerTime, TriggerSource.Manual);
             response.StatusCode = (int)HttpStatusCode.OK;
              message = "Trigger added successfully.";
            response.StatusDescription = message;
             WriteStringResponse(response, message);
         }
        catch (Exception ex)
        {
            Logger.LogMessage($"Error adding trigger: {ex.Message}");
            response.StatusCode = (int)HttpStatusCode.InternalServerError;
             message =  $"Error adding trigger: {ex.Message}";
            response.StatusDescription = message;
            WriteStringResponse(response, message);
        }
    }
   /// <summary>
    /// Edits a manual trigger
    /// </summary>
    /// <param name="request">The HTTP request object.</param>
    /// <param name="response">The HTTP response object.</param>
    private void EditTrigger(HttpListenerRequest request, HttpListenerResponse response)
    {
        string message = "";
        try
        {
            string requestBody;
            using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
            {
                requestBody = reader.ReadToEnd();
            }
            dynamic data;
              try
            {
                  data = JsonConvert.DeserializeObject<dynamic>(requestBody);
            }
            catch (JsonSerializationException ex)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                 message = "Invalid JSON format in the request body.";
                response.StatusDescription = message;
                 WriteStringResponse(response, message);
                 return;
            }
            if (data == null || string.IsNullOrEmpty(data.triggerEvent?.ToString()))
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                 message= "The triggerEvent cannot be empty";
                response.StatusDescription = message;
                 WriteStringResponse(response, message);
                 return;
            }

           string eventName = data.triggerEvent.ToString();
             DateTime? triggerTime = null;
            if (data.time != null && !string.IsNullOrEmpty(data.time.ToString()))
             {
                DateTime parsedTime;
                 if (DateTime.TryParse(data.time.ToString(), null, System.Globalization.DateTimeStyles.AssumeUniversal, out parsedTime))
                  {
                       TimeZoneInfo systemTimeZone = TimeZoneInfo.Local;
                      TimeZoneInfo targetTimeZone = TimeZoneInfo.CreateCustomTimeZone(
                      "ApplicationTimeZone",
                      TimeSpan.FromHours(Settings.TimeZone),
                      "ApplicationTimeZone",
                      "ApplicationTimeZone");
                   try
                   {
                          triggerTime = TimeZoneInfo.ConvertTime(parsedTime, TimeZoneInfo.Utc, targetTimeZone);
                    }
                   catch (Exception ex)
                   {
                        response.StatusCode = (int)HttpStatusCode.BadRequest;
                      message=  $"Time zone Conversion error,  {ex.Message}";
                       response.StatusDescription = message;
                      WriteStringResponse(response, message);
                     return;
                    }
                 }
                else
                {
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    message = "Invalid time format. Please provide a valid date and time.";
                    response.StatusDescription = message;
                    WriteStringResponse(response, message);
                    return;
                }

            }
           ActiveTriggers.AddTrigger(eventName, triggerTime, TriggerSource.Manual);
           response.StatusCode = (int)HttpStatusCode.OK;
              message = "Trigger updated successfully.";
           response.StatusDescription = message;
               WriteStringResponse(response, message);
         }
       catch (Exception ex)
        {
             Logger.LogMessage($"Error editing trigger: {ex.Message}");
            response.StatusCode = (int)HttpStatusCode.InternalServerError;
              message =  $"Error editing trigger: {ex.Message}";
             response.StatusDescription = message;
              WriteStringResponse(response, message);
        }
    }
    /// <summary>
    /// Deletes a trigger
    /// </summary>
    /// <param name="request">The HTTP request object.</param>
    /// <param name="response">The HTTP response object.</param>
   private void DeleteTrigger(HttpListenerRequest request, HttpListenerResponse response)
    {
        string message = "";
         try
        {
             string requestBody;
            using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
            {
                requestBody = reader.ReadToEnd();
            }
            dynamic data;
              try
            {
                 data = JsonConvert.DeserializeObject<dynamic>(requestBody);
            }
            catch (JsonSerializationException ex)
             {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
               message= "Invalid JSON format in the request body.";
               response.StatusDescription = message;
                WriteStringResponse(response, message);
                 return;
            }
            if (data == null || string.IsNullOrEmpty(data.triggerEvent?.ToString()))
             {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                message= "The triggerEvent cannot be empty";
                 response.StatusDescription = message;
                 WriteStringResponse(response, message);
               return;
            }

           string eventName = data.triggerEvent.ToString();
            ActiveTriggers.RemoveTrigger(eventName);
             response.StatusCode = (int)HttpStatusCode.OK;
            message= "Trigger deleted successfully.";
            response.StatusDescription = message;
              WriteStringResponse(response, message);
         }
        catch (Exception ex)
        {
            Logger.LogMessage($"Error deleting trigger: {ex.Message}");
            response.StatusCode = (int)HttpStatusCode.InternalServerError;
              message = $"Error deleting trigger: {ex.Message}";
           response.StatusDescription = message;
              WriteStringResponse(response, message);
       }
    }
    /// <summary>
        /// Writes the string message to the response body
        /// </summary>
        /// <param name="response"></param>
        /// <param name="message"></param>
   private void WriteStringResponse(HttpListenerResponse response, string message)
        {
             byte[] buffer = Encoding.UTF8.GetBytes(message);
            response.ContentType = "text/plain";
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
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
                TotalDuration = item.TotalDuration.TotalMilliseconds, // Keep milliseconds here!
                LastPlayTime = item.LastPlayTime != null ? item.LastPlayTime.Value.ToString("yyyy-MM-dd HH:mm:ss") : "N/A",
                 TriggerTime = item.TriggerTime != null ? item.TriggerTime.Value.ToString("yyyy-MM-dd HH:mm:ss") : "N/A",
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
            string[] prayerTimes = prayTime.getPrayerTimes(year, month, day, Settings.Latitude, Settings.Longitude, (int)Settings.TimeZone);

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
        switch (extension)
        {
            case ".css": return "text/css";
            case ".js": return "application/javascript";
            case ".jpg": return "image/jpeg";
            case ".png": return "image/png";
            case ".html": return "text/html";
            case ".json": return "application/json";
            default: return "text/plain";
        }
    }
}
    