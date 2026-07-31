using Codebelt.Cdn.Origin.Configuration;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace Codebelt.Cdn.Origin.Hosting;

/// <summary>
/// Extension methods for the <see cref="WebApplication"/> class that configure the Static Content Provider pipeline.
/// </summary>
public static partial class CdnOriginApplicationBuilderExtensions
{
    private const string AllowedMethods = "GET, HEAD, OPTIONS";

    /// <summary>
    /// Configures the request pipeline of the Static Content Provider and validates the content root at startup.
    /// </summary>
    /// <param name="app">The <see cref="WebApplication"/> to configure.</param>
    /// <returns>A reference to <paramref name="app"/> after the operation has completed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="app"/> cannot be null.</exception>
    /// <exception cref="InvalidOperationException">The configured content root is invalid.</exception>
    public static WebApplication UseCdnOrigin(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        CdnOriginOptions options = app.Services.GetRequiredService<IOptions<CdnOriginOptions>>().Value;
        CachePolicyResolver resolver = app.Services.GetRequiredService<CachePolicyResolver>();

        ContentRootValidationResult validation = ContentRootValidator.Validate(options.ContentRoot, AppContext.BaseDirectory);
        if (!validation.Succeeded)
        {
            throw new InvalidOperationException($"Invalid Static Content Provider configuration. {validation.ErrorMessage}");
        }

        var fileProvider = new PhysicalFileProvider(validation.ResolvedPath);

        if (options.Compression.Enabled)
        {
            app.UseResponseCompression();
        }

        if (options.Cors.Enabled)
        {
            app.UseCors(CorsPolicyOptions.PolicyName);
        }

        if (options.Health.Enabled)
        {
            MapHealthEndpoints(app, options.Health);
        }

        IList<string> defaultDocuments = options.DefaultDocuments.Count > 0
            ? options.DefaultDocuments
            : [.. CdnOriginOptions.StandardDefaultDocuments];
        app.UseDefaultFiles(CreateDefaultFilesOptions(fileProvider, defaultDocuments));

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = fileProvider,
            ContentTypeProvider = ContentTypeProviderFactory.Create(options.ContentTypes),
            ServeUnknownFileTypes = options.ContentTypes.ServeUnknownFileTypes,
            DefaultContentType = options.ContentTypes.DefaultContentType,
            RedirectToAppendTrailingSlash = false,
            OnPrepareResponse = context => ApplyAssetHeaders(context, resolver, options.Cors)
        });

        app.Use((context, next) => HandleTerminalAsync(context, fileProvider, next));

        LogEffectiveConfiguration(app, options);

        return app;
    }

    private static DefaultFilesOptions CreateDefaultFilesOptions(IFileProvider fileProvider, IList<string> defaultDocuments)
    {
        var defaultFilesOptions = new DefaultFilesOptions
        {
            FileProvider = fileProvider,
            RedirectToAppendTrailingSlash = false
        };

        defaultFilesOptions.DefaultFileNames.Clear();
        foreach (string document in defaultDocuments)
        {
            defaultFilesOptions.DefaultFileNames.Add(document);
        }

        return defaultFilesOptions;
    }

    private static void MapHealthEndpoints(IEndpointRouteBuilder endpoints, HealthOptions health)
    {
        endpoints.MapHealthChecks(health.LivePath, new HealthCheckOptions
        {
            Predicate = static _ => false,
            ResponseWriter = WriteHealthResponseAsync
        });

        endpoints.MapHealthChecks(health.ReadyPath, new HealthCheckOptions
        {
            Predicate = static registration => registration.Tags.Contains(ContentRootHealthCheck.ReadyTag),
            ResponseWriter = WriteHealthResponseAsync
        });
    }

    private static Task WriteHealthResponseAsync(HttpContext context, HealthReport report)
    {
        context.Response.Headers.CacheControl = "no-store";
        context.Response.ContentType = "text/plain";
        return context.Response.WriteAsync(report.Status.ToString());
    }

    private static void ApplyAssetHeaders(StaticFileResponseContext context, CachePolicyResolver resolver, CorsPolicyOptions cors)
    {
        IHeaderDictionary headers = context.Context.Response.Headers;
        headers.CacheControl = resolver.Resolve(context.Context.Request.Path);

        if (!string.IsNullOrEmpty(cors.CrossOriginResourcePolicy))
        {
            headers["Cross-Origin-Resource-Policy"] = cors.CrossOriginResourcePolicy;
        }

        if (cors.TimingAllowOrigin)
        {
            bool wildcard = cors.AllowedOrigins.Count == 0 || cors.AllowedOrigins.Contains(CorsPolicyOptions.AnyOrigin);
            headers["Timing-Allow-Origin"] = wildcard
                ? CorsPolicyOptions.AnyOrigin
                : string.Join(", ", cors.AllowedOrigins);
        }
    }

    private static Task HandleTerminalAsync(HttpContext context, IFileProvider fileProvider, RequestDelegate next)
    {
        if (context.GetEndpoint() is not null)
        {
            return next(context);
        }

        HttpRequest request = context.Request;
        bool isReadMethod = HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method);

        if (!isReadMethod && FileExists(fileProvider, request.Path))
        {
            context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
            context.Response.Headers.Allow = AllowedMethods;
            return Task.CompletedTask;
        }

        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return Task.CompletedTask;
    }

    private static bool FileExists(IFileProvider fileProvider, PathString path)
    {
        IFileInfo fileInfo = fileProvider.GetFileInfo(path.Value!);
        return fileInfo.Exists && !fileInfo.IsDirectory;
    }

    private static void LogEffectiveConfiguration(WebApplication app, CdnOriginOptions options)
    {
        ILogger logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Codebelt.Cdn.Origin");
        LogReady(
            logger,
            options.ContentRoot,
            options.DefaultDocuments.Count,
            options.Cors.Enabled,
            options.Cors.AllowedOrigins.Count,
            options.Compression.Enabled,
            options.Cache.ImmutablePathPrefixes.Count,
            options.Health.Enabled);
    }

    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Information,
        Message = "Static Content Provider ready. ContentRoot={ContentRoot}; DefaultDocuments={DefaultDocuments}; CorsEnabled={CorsEnabled}; AllowedOrigins={AllowedOrigins}; Compression={Compression}; ImmutablePrefixes={ImmutablePrefixes}; HealthChecks={HealthChecks}")]
    private static partial void LogReady(ILogger logger, string contentRoot, int defaultDocuments, bool corsEnabled, int allowedOrigins, bool compression, int immutablePrefixes, bool healthChecks);
}
