      //Be Naame Khoda
//FileName: ApiRequestHandler.cs

using System;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using RadioSchedulerService;
using static ActiveTriggers;

public class ApiRequestHandler
{
    private readonly ITriggerManager _triggerManager;
    public ApiRequestHandler(ITriggerManager triggerManager)
    {
        _triggerManager = triggerManager;
    }

    public void ServeTimezone(HttpListenerResponse response)
    {
        try
        {
            // Create an anonymous object to hold the prayer times
            var timeZoneObject = new
            {
                TimeZone = Settings.TimeZoneId,
                TimeZoneOffset = Settings.TimeZoneOffset
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
            WriteStringResponse(response, $"Error serving time zone: {ex.Message}");
        }
    }
    /// <summary>
    /// Serves all triggers.
    /// </summary>
    /// <param name="response">The HTTP listener response object.</param>
    public void ServeTriggers(HttpListenerResponse response)
    {
        try
        {
            var triggers = _triggerManager.Triggers.Select(item => new
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
            WriteStringResponse(response, $"Error serving triggers: {ex.Message}");
        }
    }
    /// <summary>
    /// Serves a single trigger by name.
    /// </summary>
    /// <param name="request">The HTTP request object.</param>
    /// <param name="response">The HTTP response object.</param>
    public void ServeTriggerByName(HttpListenerRequest request, HttpListenerResponse response)
    {
        try
        {
             string eventName = request.Url.Segments[request.Url.Segments.Length - 1]; //Access last element using array index
           var trigger = _triggerManager.Triggers.FirstOrDefault(t => t.Event.Equals(eventName, StringComparison.OrdinalIgnoreCase));
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
            WriteStringResponse(response, $"Error serving trigger by name: {ex.Message}");
        }
    }
    /// <summary>
    /// Adds a manual trigger
    /// </summary>
    /// <param name="request">The HTTP request object.</param>
    /// <param name="response">The HTTP response object.</param>
    public void AddTrigger(HttpListenerRequest request, HttpListenerResponse response)
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
                message = "The triggerEvent cannot be empty";
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
                      if(parsedTime.Kind != DateTimeKind.Utc) {
                           parsedTime = DateTime.SpecifyKind(parsedTime, DateTimeKind.Local); // If not already local, then set it local
                         }

                    try
                    {
                          triggerTime = CalendarHelper.ConvertToLocalTimeZone(parsedTime, Settings.Region);
                    }
                    catch (Exception ex)
                    {
                        response.StatusCode = (int)HttpStatusCode.BadRequest;
                        message = $"Time zone Conversion error,  {ex.Message}";
                        response.StatusDescription = message;
                        WriteStringResponse(response, message);
                        return;
                    }

                }
                else if (DateTime.TryParse(data.time.ToString(), out parsedTime))
                {
                     parsedTime = DateTime.SpecifyKind(parsedTime, DateTimeKind.Local);

                    try
                    {
                          triggerTime = CalendarHelper.ConvertToLocalTimeZone(parsedTime, Settings.Region);
                    }
                    catch (Exception ex)
                    {
                        response.StatusCode = (int)HttpStatusCode.BadRequest;
                        message = $"Time zone Conversion error,  {ex.Message}";
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
            message = $"Error adding trigger: {ex.Message}";
            response.StatusDescription = message;
            WriteStringResponse(response, message);
        }
    }
    /// <summary>
    /// Edits a manual trigger
    /// </summary>
    /// <param name="request">The HTTP request object.</param>
    /// <param name="response">The HTTP response object.</param>
    public void EditTrigger(HttpListenerRequest request, HttpListenerResponse response)
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
                message = "The triggerEvent cannot be empty";
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
                      if(parsedTime.Kind != DateTimeKind.Utc) {
                           parsedTime = DateTime.SpecifyKind(parsedTime, DateTimeKind.Local); // If not already local, then set it local
                         }
                    try
                    {
                          triggerTime = CalendarHelper.ConvertToLocalTimeZone(parsedTime, Settings.Region);
                    }
                   catch (Exception ex)
                    {
                        response.StatusCode = (int)HttpStatusCode.BadRequest;
                        message = $"Time zone Conversion error,  {ex.Message}";
                        response.StatusDescription = message;
                        WriteStringResponse(response, message);
                        return;
                    }
                }
                 else if (DateTime.TryParse(data.time.ToString(), out parsedTime))
                {
                     parsedTime = DateTime.SpecifyKind(parsedTime, DateTimeKind.Local);

                    try
                    {
                         triggerTime = CalendarHelper.ConvertToLocalTimeZone(parsedTime, Settings.Region);
                    }
                     catch (Exception ex)
                    {
                        response.StatusCode = (int)HttpStatusCode.BadRequest;
                        message = $"Time zone Conversion error,  {ex.Message}";
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
            message = $"Error editing trigger: {ex.Message}";
            response.StatusDescription = message;
            WriteStringResponse(response, message);
        }
    }
    /// <summary>
    /// Deletes a trigger
    /// </summary>
    /// <param name="request">The HTTP request object.</param>
    /// <param name="response">The HTTP response object.</param>
    public void DeleteTrigger(HttpListenerRequest request, HttpListenerResponse response)
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
                message = "The triggerEvent cannot be empty";
                response.StatusDescription = message;
                WriteStringResponse(response, message);
                return;
            }

            string eventName = data.triggerEvent.ToString();
            ActiveTriggers.RemoveTrigger(eventName);
            response.StatusCode = (int)HttpStatusCode.OK;
            message = "Trigger deleted successfully.";
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
    protected void WriteStringResponse(HttpListenerResponse response, string message)
    {
         byte[] buffer = Encoding.UTF8.GetBytes(message);
            response.ContentType = "text/plain";
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
    }
}
    