using Codebelt.Cdn.Origin.Configuration;
using Microsoft.Extensions.Options;

namespace Codebelt.Cdn.Origin.Hosting;

/// <summary>
/// Extension methods for the <see cref="IServiceCollection"/> interface that register the Static Content Provider services.
/// </summary>
public static class CdnOriginServiceCollectionExtensions
{
    /// <summary>
    /// The compressible MIME types enabled when origin compression is turned on.
    /// </summary>
    /// <remarks>Only text-based and other genuinely compressible types are included; already-compressed formats such as images, video, and WOFF2 fonts are intentionally excluded.</remarks>
    public static readonly IReadOnlyList<string> DefaultCompressibleMimeTypes =
    [
        "text/plain",
        "text/css",
        "text/html",
        "text/xml",
        "text/javascript",
        "application/javascript",
        "application/json",
        "application/xml",
        "application/manifest+json",
        "image/svg+xml",
        "image/x-icon",
        "application/wasm",
        "font/ttf",
        "font/otf"
    ];

    /// <summary>
    /// Adds the Static Content Provider services to the specified <paramref name="services"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to extend.</param>
    /// <param name="configuration">The <see cref="IConfiguration"/> used to bind <see cref="CdnOriginOptions"/>.</param>
    /// <returns>A reference to <paramref name="services"/> after the operation has completed.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="services"/> cannot be null - or - <paramref name="configuration"/> cannot be null.
    /// </exception>
    /// <remarks>
    /// CORS, response compression, and health check services are always registered and configured from the final
    /// bound options; whether their middleware runs is decided by <c>UseCdnOrigin</c>. This keeps service
    /// registration and pipeline configuration consistent regardless of configuration source ordering.
    /// </remarks>
    public static IServiceCollection AddCdnOrigin(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<CdnOriginOptions>()
            .Bind(configuration.GetSection(CdnOriginOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<CdnOriginOptions>, CdnOriginOptionsValidator>();

        services.AddSingleton(static sp => new CachePolicyResolver(sp.GetRequiredService<IOptions<CdnOriginOptions>>().Value));

        services.AddCors();
        services.ConfigureOptions<ConfigureCdnOriginCorsOptions>();

        services.AddResponseCompression();
        services.ConfigureOptions<ConfigureCdnOriginCompressionOptions>();

        services.AddHealthChecks()
            .AddCheck<ContentRootHealthCheck>("content-root", tags: [ContentRootHealthCheck.ReadyTag]);

        return services;
    }
}
