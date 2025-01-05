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
            if (File.Exists(filePath))
            {
                byte[] buffer = File.ReadAllBytes(filePath);
                response.ContentType = GetContentType(filePath);
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
            }
            else
            {
                response.StatusCode = (int)HttpStatusCode.NotFound;
                string notFoundMessage = "صفحه مورد نظر یافت نشد.";
                byte[] buffer = Encoding.UTF8.GetBytes(notFoundMessage);
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

    private string GetContentType(string filePath)
    {
        switch (Path.GetExtension(filePath).ToLower())
        {
            case ".html": return "text/html";
            case ".css": return "text/css";
            case ".js": return "application/javascript";
            default: return "application/octet-stream";
        }
    }
}