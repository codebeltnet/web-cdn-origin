using System.Globalization;
using Codebelt.Cdn.Origin.Configuration;

namespace Codebelt.Cdn.Origin.Hosting;

/// <summary>
/// Resolves the <c>Cache-Control</c> header value for a served asset by selecting either the revalidated or the
/// immutable cache profile based on the request path.
/// </summary>
/// <remarks>
/// The header strings for both profiles are computed once at construction, so resolving a request performs only a
/// prefix comparison and never allocates or hashes file content.
/// </remarks>
public sealed class CachePolicyResolver
{
    private readonly string _revalidateHeader;
    private readonly string _immutableHeader;
    private readonly string[] _immutablePrefixes;

    /// <summary>
    /// Initializes a new instance of the <see cref="CachePolicyResolver"/> class.
    /// </summary>
    /// <param name="options">The <see cref="CdnOriginOptions"/> that describe the cache profiles.</param>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> cannot be null.</exception>
    public CachePolicyResolver(CdnOriginOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _revalidateHeader = BuildCacheControl(options.Cache.Revalidate);
        _immutableHeader = BuildCacheControl(options.Cache.Immutable);
        _immutablePrefixes =
        [
            .. options.Cache.ImmutablePathPrefixes.Where(static prefix => !string.IsNullOrWhiteSpace(prefix))
        ];
    }

    /// <summary>
    /// Resolves the <c>Cache-Control</c> header value for the specified request <paramref name="path"/>.
    /// </summary>
    /// <param name="path">The request path of the served asset.</param>
    /// <returns>The <c>Cache-Control</c> header value of the immutable profile when the path matches a configured immutable prefix; otherwise the revalidated profile value.</returns>
    public string Resolve(PathString path)
    {
        return path.HasValue && MatchesImmutablePrefix(path.Value!)
            ? _immutableHeader
            : _revalidateHeader;
    }

    private bool MatchesImmutablePrefix(string path)
    {
        for (int i = 0; i < _immutablePrefixes.Length; i++)
        {
            if (path.StartsWith(_immutablePrefixes[i], StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Builds the <c>Cache-Control</c> header value for the specified <paramref name="profile"/>.
    /// </summary>
    /// <param name="profile">The <see cref="CacheProfileOptions"/> to render.</param>
    /// <returns>The rendered <c>Cache-Control</c> header value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="profile"/> cannot be null.</exception>
    public static string BuildCacheControl(CacheProfileOptions profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        if (profile.NoStore)
        {
            return "no-store";
        }

        var directives = new List<string>(8)
        {
            profile.Public ? "public" : "private"
        };

        if (profile.NoCache)
        {
            directives.Add("no-cache");
        }

        AppendSeconds(directives, "max-age", profile.MaxAge);
        AppendSeconds(directives, "s-maxage", profile.SharedMaxAge);

        if (profile.MustRevalidate)
        {
            directives.Add("must-revalidate");
        }

        if (profile.Immutable)
        {
            directives.Add("immutable");
        }

        AppendSeconds(directives, "stale-while-revalidate", profile.StaleWhileRevalidate);
        AppendSeconds(directives, "stale-if-error", profile.StaleIfError);

        if (profile.NoTransform)
        {
            directives.Add("no-transform");
        }

        return string.Join(", ", directives);
    }

    private static void AppendSeconds(List<string> directives, string directive, TimeSpan? value)
    {
        if (value is { } duration)
        {
            long seconds = (long)duration.TotalSeconds;
            directives.Add(string.Create(CultureInfo.InvariantCulture, $"{directive}={seconds}"));
        }
    }
}
