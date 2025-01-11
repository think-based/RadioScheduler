using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

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
            // Serve static files from wwwroot
            if (path.StartsWith("/css/") || path.StartsWith("/js/") || path.StartsWith("/img/"))
            {
                string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", path.TrimStart('/'));
                if (File.Exists(filePath))
                {
                    byte[] fileBytes = File.ReadAllBytes(filePath);
                    response.ContentType = GetMimeType(filePath); // Set MIME type
                    response.ContentLength64 = fileBytes.Length;
                    response.OutputStream.Write(fileBytes, 0, fileBytes.Length);
                }
                else
                {
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                }
            }
            // Handle other routes
            else if (path == "/" || path == "/index.html")
            {
                string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "index.html");
                ServeHtmlFile(response, filePath);
            }
            else if (path == "/viewlog" || path == "/viewlog.html")
            {
                string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "viewlog.html");
                ServeHtmlFile(response, filePath);
            }
            else if (path == "/clearlog")
            {
                ClearLog(response);
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

    private void ServeHtmlFile(HttpListenerResponse response, string filePath)
    {
        if (File.Exists(filePath))
        {
            string htmlContent = File.ReadAllText(filePath);

            // Replace placeholders with dynamic content
            if (filePath.EndsWith("index.html"))
            {
                string[] prayerTimes = new PrayTime().getPrayerTimes(
                    DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day,
                    Settings.Latitude, Settings.Longitude, (int)Settings.TimeZone
                );

                htmlContent = htmlContent
                    .Replace("{{Fajr}}", prayerTimes[0])
                    .Replace("{{Sunrise}}", prayerTimes[1])
                    .Replace("{{Dhuhr}}", prayerTimes[2])
                    .Replace("{{Asr}}", prayerTimes[3])
                    .Replace("{{Sunset}}", prayerTimes[4])
                    .Replace("{{Maghrib}}", prayerTimes[5])
                    .Replace("{{Isha}}", prayerTimes[6]);
            }
            else if (filePath.EndsWith("viewlog.html"))
            {
                string logContent = File.Exists(Logger.LogFilePath) ? File.ReadAllText(Logger.LogFilePath) : "Log file not found.";
                htmlContent = htmlContent.Replace("{{LogContent}}", logContent);
            }

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

    private void ClearLog(HttpListenerResponse response)
    {
        try
        {
            if (File.Exists(Logger.LogFilePath))
            {
                File.WriteAllText(Logger.LogFilePath, string.Empty);
                Logger.LogMessage("Log file cleared.");
            }

            response.StatusCode = (int)HttpStatusCode.Found;
            response.RedirectLocation = "/";
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