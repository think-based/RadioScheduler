//Be Naame Khoda
//FileName: WebServer.cs

using System;
using System.Collections.Generic;
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

        string responseString = "";

        // بررسی آدرس درخواست بدون در نظر گرفتن پارامترها
        string path = request.Url.AbsolutePath;

        if (path == "/")
        {
            responseString = GenerateStatusPage();
        }
        else if (path == "/clearlog")
        {
            Logger.ClearLog(); // فراخوانی متد پاک کردن لاگ
            responseString = GenerateStatusPage(); // بازگشت به صفحه اصلی
        }
        else if (path == "/viewlog")
        {
            responseString = ReadLogFile();
        }
        else
        {
            responseString = GenerateStatusPage(); // بازگشت به صفحه اصلی برای درخواست‌های نامعتبر
        }

        byte[] buffer = Encoding.UTF8.GetBytes(responseString);
        response.ContentLength64 = buffer.Length;
        using (var output = response.OutputStream)
        {
            output.Write(buffer, 0, buffer.Length);
        }
    }

    private string GenerateStatusPage()
    {
        var html = new StringBuilder();
        html.Append("<html><head><title>Radio Scheduler Status</title>");
        
        // اضافه کردن JavaScript برای ارسال درخواست GET بدون ?
        html.Append("<script>");
        html.Append("function clearLog() {");
        html.Append("  fetch('/clearlog', { method: 'GET' })");
        html.Append("    .then(response => window.location.reload());"); // رفرش صفحه پس از پاک کردن لاگ
        html.Append("}");
        html.Append("</script>");
        html.Append("</head><body>");
        html.Append("<h1>Radio Scheduler Status</h1>");

        // نمایش تاریخ و ساعت در تقویم‌های مختلف
        html.Append("<h2>Current Date and Time</h2>");
        html.Append("<ul>");
        html.Append($"<li>Gregorian: {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}</li>");
        html.Append($"<li>Persian: {CalendarHelper.ConvertDate(DateTime.Now, "Persian").ToString("yyyy-MM-dd HH:mm:ss")}</li>");
        html.Append($"<li>Hijri: {CalendarHelper.ConvertDate(DateTime.Now, "Hijri").ToString("yyyy-MM-dd HH:mm:ss")}</li>");
        html.Append("</ul>");

        // نمایش وضعیت پلی‌لیست‌ها
        html.Append("<h2>Playlist Status</h2>");
        html.Append("<ul>");
        foreach (var item in _scheduler.GetScheduledItems())
        {
            html.Append($"<li>Item ID: {item.ItemId}, Type: {item.Type}, Trigger: {item.Trigger ?? "N/A"}</li>");
        }
        html.Append("</ul>");

        // دکمه پاک کردن لاگ با JavaScript
        html.Append("<h2>Log Management</h2>");
        html.Append("<button onclick='clearLog()'>Clear Log</button>");

        // لینک مشاهده لاگ‌ها
        html.Append("<h2>View Logs</h2>");
        html.Append("<a href='/viewlog'>View Log File</a>");

        html.Append("</body></html>");
        return html.ToString();
    }

    private string ReadLogFile()
    {
        try
        {
            if (File.Exists(Logger.LogFilePath))
            {
                string logContent = File.ReadAllText(Logger.LogFilePath);
                return $"<html><head><title>Log File</title></head><body><pre>{logContent}</pre></body></html>";
            }
            return "Log file not found.";
        }
        catch (Exception ex)
        {
            return $"Error reading log file: {ex.Message}";
        }
    }
}