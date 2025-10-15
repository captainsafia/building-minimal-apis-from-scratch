#: property PublishAoT=false
#: package Microsoft.Extensions.Hosting@10.0.0-rc.2.25502.107

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder();

builder.Services.AddSingleton<IServer, Server>();
builder.Services.AddHostedService<GenericWebHostService>();

var app = builder.Build();

app.UseRouting();
app.UseEndpoints();

app.Map("/", () => new OkResult("Hello world!"));
app.Map("/bye", () => new OkResult("Bye world!"));
app.Map("/hello", (string name) =>
{
    return new OkResult($"Hello {name}!");
});
app.Map("/age", (int year) =>
{
    return new OkResult($"You are {DateTime.Now.Year - year} years old!");
});
using var cts = new CancellationTokenSource();

await app.RunAsync();

public interface IServer
{
    Task StartAsync(RequestDelegate handler, CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}

public class GenericWebHostService(IServer server, IHostApplicationLifetime hostApplicationLifetime, IApplicationBuilder applicationBuilder) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var requestDelegate = applicationBuilder.Build();
        await server.StartAsync(requestDelegate, cancellationToken);
        hostApplicationLifetime.ApplicationStopping.Register(async () =>
        {
            await server.StopAsync(cancellationToken);
        });
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await server.StopAsync(cancellationToken);
    }
}

public class WebApplicationBuilder : IHostApplicationBuilder
{
    private readonly HostApplicationBuilder innerBuilder = new();

    public IDictionary<object, object> Properties => ((IHostApplicationBuilder)innerBuilder).Properties;

    public Microsoft.Extensions.Configuration.IConfigurationManager Configuration => ((IHostApplicationBuilder)innerBuilder).Configuration;

    public IHostEnvironment Environment => ((IHostApplicationBuilder)innerBuilder).Environment;

    public ILoggingBuilder Logging => ((IHostApplicationBuilder)innerBuilder).Logging;

    public IMetricsBuilder Metrics => ((IHostApplicationBuilder)innerBuilder).Metrics;

    public IServiceCollection Services => ((IHostApplicationBuilder)innerBuilder).Services;

    public void ConfigureContainer<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory, Action<TContainerBuilder>? configure = null) where TContainerBuilder : notnull
    {
        ((IHostApplicationBuilder)innerBuilder).ConfigureContainer(factory, configure);
    }

    public WebApplication Build()
    {
        innerBuilder.Services.AddSingleton<IApplicationBuilder>(new ApplicationBuilder());
        var host = innerBuilder.Build();
        return new WebApplication(host);
    }
}

public interface IRouteEndpointBuilder
{
    Dictionary<string, RouteEndpoint> Endpoints { get; }
    void AddHandler(string route, Delegate handler);
}

public interface IApplicationBuilder
{
    void Use(Func<RequestDelegate, RequestDelegate> middleware);
    RequestDelegate Build();
}
public class ApplicationBuilder : IApplicationBuilder
{
    private static readonly List<Func<RequestDelegate, RequestDelegate>> _middlewares = [];
    public RequestDelegate Build()
    {
        RequestDelegate handler = context => Task.CompletedTask;
        for (var i = _middlewares.Count - 1; i >= 0; i--)
        {
            handler = _middlewares[i](handler);
        }
        return handler;
    }

    public void Use(Func<RequestDelegate, RequestDelegate> middleware)
    {
        _middlewares.Add(middleware);
    }
}

public class WebApplication(IHost host) : IHost, IRouteEndpointBuilder, IApplicationBuilder
{
    private IApplicationBuilder _applicationBuilder = host.Services.GetRequiredService<IApplicationBuilder>();
    public IServiceProvider Services => host.Services;

    public Dictionary<string, RouteEndpoint> Endpoints { get; } = [];

    public static WebApplicationBuilder CreateBuilder() => new WebApplicationBuilder();

    public void Dispose()
    {
        host.Dispose();
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        return host.StartAsync();
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        return host.StopAsync();
    }

    void IRouteEndpointBuilder.AddHandler(string route, Delegate handler)
    {
        var requestDelegate = RequestDelegateFactory.Create(handler);
        Endpoints.Add(route, new RouteEndpoint { Route = route, Handler = requestDelegate });
    }

    RequestDelegate IApplicationBuilder.Build()
    {
        return _applicationBuilder.Build();
    }

    void IApplicationBuilder.Use(Func<RequestDelegate, RequestDelegate> middleware)
    {
        _applicationBuilder.Use(middleware);
    }
}

public static class RoutingExtensions
{
    public static IRouteEndpointBuilder Map(this IRouteEndpointBuilder server, string route, Delegate handler)
    {
        var requestDelegate = RequestDelegateFactory.Create(handler);
        server.AddHandler(route, requestDelegate);
        return server;
    }
}

public class MappedParameter
{
    public required string Name { get; init; }
    public bool IsString { get; set; }
    public bool IsHttpContext { get; set; }
    [MemberNotNullWhen(true, nameof(TryParseMethod))]
    public bool IsParsable { get; set; }
    public MethodInfo? TryParseMethod { get; set; }
}

public static class RequestDelegateFactory
{
    public static RequestDelegate Create(Delegate handler)
    {
        var parameters = handler.Method.GetParameters();
        var mappedParameters = new List<MappedParameter>();
        for (var i = 0; i < parameters.Count(); i++)
        {
            var parameter = parameters[i];
            Debug.Assert(parameter.Name != null, "Parameter name cannot be null.");
            if (parameter.ParameterType == typeof(string))
            {
                mappedParameters.Add(new MappedParameter { Name = parameter.Name, IsString = true });
            }
            if (parameter.ParameterType == typeof(HttpContext))
            {
                mappedParameters.Add(new MappedParameter { Name = parameter.Name, IsHttpContext = true });
            }
            if (ParameterBindingMethodCache.TryGetParseMethod(parameter.ParameterType, out var methodInfo))
            {
                mappedParameters.Add(new MappedParameter { Name = parameter.Name, IsParsable = true, TryParseMethod = methodInfo });
            }
        }
        RequestDelegate requestDelegate = async context =>
        {
            var arguments = new object[parameters.Length];
            for (var i = 0; i < parameters.Length; i++)
            {
                var mappedParameter = mappedParameters[i];
                if (mappedParameter.IsString)
                {
                    arguments[i] = context.Request.QueryString[mappedParameter.Name]!;
                }
                else if (mappedParameter.IsHttpContext)
                {
                    arguments[i] = context;
                }
                else if (mappedParameter.IsParsable)
                {
                    var rawValue = context.Request.QueryString[mappedParameter.Name]!;
                    object?[] invokedArgs = [rawValue, null];
                    mappedParameter.TryParseMethod.Invoke(null, invokedArgs);
                    arguments[i] = invokedArgs[1]!;
                }
            }
            var result = handler.DynamicInvoke(arguments);
            if (result is Task task)
            {
                await task;
            }
            if (result is IResult httpResult)
            {
                await httpResult.ExecuteAsync(context);
            }
        };
        return requestDelegate;
    }
}

public static class ParameterBindingMethodCache
{
    public static readonly Dictionary<Type, MethodInfo> _tryParseCache = [];
    public static bool TryGetParseMethod(Type parameterType, [NotNullWhen(true)] out MethodInfo? methodInfo)
    {
        if (_tryParseCache.TryGetValue(parameterType, out var cachedMethod))
        {
            methodInfo = cachedMethod;
            return true;
        }
        methodInfo = null;
        var tryParseMethod = parameterType.GetMethod("TryParse",
            BindingFlags.Public | BindingFlags.Static,

            [typeof(string), parameterType.MakeByRefType()]
        );
        if (tryParseMethod is not null)
        {
            methodInfo = tryParseMethod;
            _tryParseCache.Add(parameterType, tryParseMethod);
            return true;
        }
        return false;
    }
}

public interface IResult
{
    int StatusCode { get; }
    Task ExecuteAsync(HttpContext context);
}

public class OkResult(string message) : IResult
{
    public int StatusCode => 200;

    public async Task ExecuteAsync(HttpContext context)
    {
        await context.WritePlainTextResponse(message);
    }
}

public class RouteEndpoint
{
    public required string Route { get; init; }
    public required RequestDelegate Handler { get; set; }
}

public delegate Task RequestDelegate(HttpContext context);

public class HttpContext
{
    public required HttpListenerRequest Request { get; init; }
    public required HttpListenerResponse Response { get; init; }
    public Dictionary<string, object> FeatureCollection = [];
}

public class Server() : IServer, IDisposable
{
    private HttpListener? _listener;
    private bool _disposed;

    public async Task StartAsync(RequestDelegate handler, CancellationToken cancellationToken)
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:8080/");
        _listener.Start();

        Console.WriteLine("Server started successfully.");

        while (!cancellationToken.IsCancellationRequested && _listener.IsListening)
        {
            try
            {
                var context = await _listener.GetContextAsync();
                _ = Task.Run(() => HandleRequestAsync(handler, context), cancellationToken);
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

    private async Task HandleRequestAsync(RequestDelegate handler, HttpListenerContext context)
    {
        try
        {
            var httpContext = new HttpContext
            {
                Request = context.Request,
                Response = context.Response
            };
            var result = handler.DynamicInvoke(httpContext);
            if (result is Task task)
            {
                await task;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling request: {ex.Message}");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Stop();
        return Task.CompletedTask;
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

    public ValueTask DisposeAsync()
    {
        throw new NotImplementedException();
    }
}

public static class HttpContextExtensions
{
    public static async Task WritePlainTextResponse(this HttpContext context, string message, int statusCode = 200)
    {
        using var response = context.Response;

        Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {context.Request.HttpMethod} {context.Request.Url?.PathAndQuery}");

        response.ContentType = "text/plain";
        response.StatusCode = statusCode;

        byte[] buffer = Encoding.UTF8.GetBytes(message);
        response.ContentLength64 = buffer.Length;

        await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
    }
}

public static class MiddlewareExtensions
{
    public static IApplicationBuilder Use(this IApplicationBuilder server, Func<HttpContext, RequestDelegate, Task> middleware)
    {
        server.Use(next => context => middleware(context, next));
        return server;
    }

    public static IApplicationBuilder UseRouting(this IApplicationBuilder application)
    {
        application.Use(async (context, next) =>
        {
            var path = context.Request.Url?.AbsolutePath;
            if (path is not null &&
                application is IRouteEndpointBuilder endpointBuilder &&
                    endpointBuilder.Endpoints.TryGetValue(path, out var endpoint))
            {
                context.FeatureCollection.Add("Endpoint", endpoint);
            }
            await next(context);
        });
        return application;
    }

    public static IApplicationBuilder UseEndpoints(this IApplicationBuilder server)
    {
        server.Use(async (context, next) =>
        {
            if (context.FeatureCollection.TryGetValue("Endpoint", out var obj) &&
                obj is RouteEndpoint endpoint)
            {
                await endpoint.Handler(context);
            }
            else
            {
                await context.WritePlainTextResponse("Not found!", 404);
            }
        });
        return server;
    }
}