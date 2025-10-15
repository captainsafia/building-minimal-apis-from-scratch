using System.Net;
using System.Text;

var port = 8080;
using var server = new Server(port);
using var cts = new CancellationTokenSource();

var serverTask = server.StartAsync(cts.Token);

Console.WriteLine($"Listening on http://localhost:{port}. Press `Ctrl+C` to close server...");
while (Console.ReadKey(true).Key != ConsoleKey.Escape) { }

Console.WriteLine("Stopping the server...");
cts.Cancel();
await serverTask;
Console.WriteLine("Server stopped. Exiting...");

public class Server(int port) : IDisposable
{
    private HttpListener? _listener;
    private bool _disposed;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{port}/");
        _listener.Start();

        Console.WriteLine("Server started successfully.");

        while (!cancellationToken.IsCancellationRequested && _listener.IsListening)
        {
            try
            {
                var context = await _listener.GetContextAsync();
                _ = Task.Run(() => HandleRequestAsync(context), cancellationToken);
            }
            catch (HttpListenerException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accepting connection: {ex.Message}");
            }
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext context)
    {
        try
        {
            using var response = context.Response;

            Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {context.Request.HttpMethod} {context.Request.Url?.PathAndQuery}");

            response.ContentType = "text/plain";
            response.StatusCode = 200;

            byte[] buffer = Encoding.UTF8.GetBytes("Hello world!");
            response.ContentLength64 = buffer.Length;

            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling request: {ex.Message}");
        }
    }

    public void Stop()
    {
        if (_listener != null && _listener.IsListening)
        {
            _listener.Stop();
            _listener.Close();
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Stop();
            _listener = null;
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}