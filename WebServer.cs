//Be Naame Khoda
//FileName: WebServer.cs

using System;
using System.Collections.Generic;
using System.Globalization;
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

        if (request.Url.PathAndQuery == "/")
        {
            responseString = GenerateStatusPage();
        }
        else if (request.Url.PathAndQuery == "/clearlog")
        {
            Logger.ClearLog();
            responseString = "Log file cleared.";
        }
        else
        {
            responseString = "404 - Page not found.";
            response.StatusCode = 404;
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
        html.Append("<html><head><title>Radio Scheduler Status</title></head><body>");
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

        // دکمه پاک کردن لاگ
        html.Append("<h2>Log Management</h2>");
        html.Append("<form action='/clearlog' method='get'>");
        html.Append("<button type='submit'>Clear Log</button>");
        html.Append("</form>");

        html.Append("</body></html>");
        return html.ToString();
    }
}