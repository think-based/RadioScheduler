//Be Naame Khoda
//FileName: WebServer.cs

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
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", path.TrimStart('/'));

            // اگر مسیر ریشه (/) درخواست شد، به index.html هدایت شود
            if (path == "/")
            {
                filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "index.html");
            }

            if (File.Exists(filePath))
            {
                // خواندن فایل HTML
                string htmlContent = File.ReadAllText(filePath);

                // جایگزینی متغیرها در فایل HTML
                if (path == "/index.html" || path == "/")
                {
                    htmlContent = htmlContent
                        .Replace("{{GregorianDate}}", CalendarHelper.ConvertDateToString(DateTime.Now, "Gregorian"))
                        .Replace("{{PersianDate}}", CalendarHelper.ConvertDateToString(DateTime.Now, "Persian"))
                        .Replace("{{HijriDate}}", CalendarHelper.ConvertDateToString(DateTime.Now, "Hijri"));

                    // اضافه کردن آیتم‌های پلی‌لیست
                    var playlistItems = new StringBuilder();
                    foreach (var item in _scheduler.GetScheduledItems())
                    {
                        playlistItems.Append($"<li>Item ID: {item.ItemId}, Type: {item.Type}, Trigger: {item.Trigger ?? "N/A"}</li>");
                    }
                    htmlContent = htmlContent.Replace("{{PlaylistItems}}", playlistItems.ToString());
                }
                else if (path == "/viewlog.html")
                {
                    string logContent = File.Exists(Logger.LogFilePath) ? File.ReadAllText(Logger.LogFilePath) : "Log file not found.";
                    htmlContent = htmlContent.Replace("{{LogContent}}", logContent);
                }

                // ارسال پاسخ به کاربر
                byte[] buffer = Encoding.UTF8.GetBytes(htmlContent);
                response.ContentType = "text/html; charset=UTF-8";
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
            }
            else
            {
                response.StatusCode = (int)HttpStatusCode.NotFound;
                string notFoundMessage = "Page not found.";
                byte[] buffer = Encoding.UTF8.GetBytes(notFoundMessage);
                response.ContentType = "text/plain; charset=UTF-8";
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
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
}