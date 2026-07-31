using Codebelt.Cdn.Origin.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Codebelt.Cdn.Origin.Hosting;

/// <summary>
/// A readiness <see cref="IHealthCheck"/> that verifies the configured static content root is available and readable.
/// </summary>
/// <seealso cref="IHealthCheck" />
public sealed class ContentRootHealthCheck : IHealthCheck
{
    /// <summary>
    /// The tag applied to readiness health checks.
    /// </summary>
    public const string ReadyTag = "ready";

    private readonly CdnOriginOptions _options;
    private readonly string _applicationDirectory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentRootHealthCheck"/> class.
    /// </summary>
    /// <param name="options">The monitored <see cref="CdnOriginOptions"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> cannot be null.</exception>
    public ContentRootHealthCheck(IOptions<CdnOriginOptions> options)
        : this((options ?? throw new ArgumentNullException(nameof(options))).Value, AppContext.BaseDirectory)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentRootHealthCheck"/> class.
    /// </summary>
    /// <param name="options">The monitored <see cref="CdnOriginOptions"/>.</param>
    /// <param name="applicationDirectory">The application base directory whose files must not be exposed.</param>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> cannot be null.</exception>
    public ContentRootHealthCheck(CdnOriginOptions options, string applicationDirectory)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options;
        _applicationDirectory = applicationDirectory;
    }

    /// <summary>
    /// Verifies that the configured static content root is available and readable.
    /// </summary>
    /// <param name="context">The <see cref="HealthCheckContext"/>.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="Task{TResult}"/> that yields the <see cref="HealthCheckResult"/>.</returns>
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        ContentRootValidationResult result = ContentRootValidator.Validate(_options.ContentRoot, _applicationDirectory);

        return Task.FromResult(result.Succeeded
            ? HealthCheckResult.Healthy($"Content root '{result.ResolvedPath}' is available.")
            : HealthCheckResult.Unhealthy(result.ErrorMessage));
    }
}
