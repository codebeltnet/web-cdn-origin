using Microsoft.Extensions.Options;

namespace Codebelt.Cdn.Origin.Configuration;

/// <summary>
/// Validates <see cref="CdnOriginOptions"/> so that invalid configuration fails fast at startup and the
/// emitted HTTP behaviour is always coherent.
/// </summary>
/// <seealso cref="IValidateOptions{TOptions}" />
public sealed class CdnOriginOptionsValidator : IValidateOptions<CdnOriginOptions>
{
    /// <summary>
    /// Validates the specified <paramref name="options"/>.
    /// </summary>
    /// <param name="name">The name of the options instance being validated, if any.</param>
    /// <param name="options">The <see cref="CdnOriginOptions"/> to validate.</param>
    /// <returns>A <see cref="ValidateOptionsResult"/> describing whether validation succeeded.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> cannot be null.</exception>
    public ValidateOptionsResult Validate(string? name, CdnOriginOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.ContentRoot))
        {
            failures.Add("CdnOrigin:ContentRoot must be configured.");
        }

        for (int i = 0; i < options.DefaultDocuments.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(options.DefaultDocuments[i]))
            {
                failures.Add($"CdnOrigin:DefaultDocuments[{i}] must not be empty.");
            }
        }

        ValidateCors(options.Cors, failures);
        ValidateCache(options.Cache, failures);
        ValidateContentTypes(options.ContentTypes, failures);
        ValidateHealth(options.Health, failures);

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static void ValidateCors(CorsPolicyOptions cors, List<string> failures)
    {
        if (!cors.Enabled) { return; }

        bool wildcard = cors.AllowedOrigins.Count == 0 || cors.AllowedOrigins.Contains(CorsPolicyOptions.AnyOrigin);

        if (wildcard && cors.AllowCredentials)
        {
            failures.Add("CdnOrigin:Cors cannot combine a wildcard (public) origin with AllowCredentials.");
        }

        if (cors.AllowedOrigins.Contains(CorsPolicyOptions.AnyOrigin) && cors.AllowedOrigins.Count > 1)
        {
            failures.Add("CdnOrigin:Cors cannot combine the wildcard origin with explicit origins.");
        }
    }

    private static void ValidateCache(CacheOptions cache, List<string> failures)
    {
        for (int i = 0; i < cache.ImmutablePathPrefixes.Count; i++)
        {
            string prefix = cache.ImmutablePathPrefixes[i];
            if (string.IsNullOrWhiteSpace(prefix) || !prefix.StartsWith('/'))
            {
                failures.Add($"CdnOrigin:Cache:ImmutablePathPrefixes[{i}] must start with '/'.");
            }
        }

        ValidateCacheProfile("Revalidate", cache.Revalidate, failures);
        ValidateCacheProfile("Immutable", cache.Immutable, failures);
    }

    private static void ValidateCacheProfile(string name, CacheProfileOptions profile, List<string> failures)
    {
        if (profile.NoStore && (profile.MaxAge.HasValue || profile.SharedMaxAge.HasValue))
        {
            failures.Add($"CdnOrigin:Cache:{name} cannot combine no-store with max-age or s-maxage.");
        }

        if (profile.Immutable && profile.MustRevalidate)
        {
            failures.Add($"CdnOrigin:Cache:{name} cannot combine immutable with must-revalidate.");
        }

        if (profile.Immutable && profile.NoCache)
        {
            failures.Add($"CdnOrigin:Cache:{name} cannot combine immutable with no-cache.");
        }

        AddIfNegative($"CdnOrigin:Cache:{name}:MaxAge", profile.MaxAge, failures);
        AddIfNegative($"CdnOrigin:Cache:{name}:SharedMaxAge", profile.SharedMaxAge, failures);
        AddIfNegative($"CdnOrigin:Cache:{name}:StaleWhileRevalidate", profile.StaleWhileRevalidate, failures);
        AddIfNegative($"CdnOrigin:Cache:{name}:StaleIfError", profile.StaleIfError, failures);
    }

    private static void AddIfNegative(string setting, TimeSpan? value, List<string> failures)
    {
        if (value is { } duration && duration < TimeSpan.Zero)
        {
            failures.Add($"{setting} cannot be negative.");
        }
    }

    private static void ValidateContentTypes(ContentTypeOptions contentTypes, List<string> failures)
    {
        if (contentTypes.ServeUnknownFileTypes && string.IsNullOrWhiteSpace(contentTypes.DefaultContentType))
        {
            failures.Add("CdnOrigin:ContentTypes:DefaultContentType must be set when ServeUnknownFileTypes is true.");
        }

        foreach (KeyValuePair<string, string> mapping in contentTypes.Mappings)
        {
            if (string.IsNullOrWhiteSpace(mapping.Key) || !mapping.Key.StartsWith('.'))
            {
                failures.Add($"CdnOrigin:ContentTypes:Mappings key '{mapping.Key}' must start with '.'.");
            }

            if (string.IsNullOrWhiteSpace(mapping.Value))
            {
                failures.Add($"CdnOrigin:ContentTypes:Mappings['{mapping.Key}'] must have a MIME value.");
            }
        }
    }

    private static void ValidateHealth(HealthOptions health, List<string> failures)
    {
        if (!health.Enabled) { return; }

        if (string.IsNullOrWhiteSpace(health.LivePath) || !health.LivePath.StartsWith('/'))
        {
            failures.Add("CdnOrigin:Health:LivePath must start with '/'.");
        }

        if (string.IsNullOrWhiteSpace(health.ReadyPath) || !health.ReadyPath.StartsWith('/'))
        {
            failures.Add("CdnOrigin:Health:ReadyPath must start with '/'.");
        }

        if (string.Equals(health.LivePath, health.ReadyPath, StringComparison.Ordinal))
        {
            failures.Add("CdnOrigin:Health:LivePath and ReadyPath must differ.");
        }
    }
}
