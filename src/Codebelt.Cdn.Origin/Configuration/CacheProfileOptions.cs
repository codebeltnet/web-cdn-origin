namespace Codebelt.Cdn.Origin.Configuration;

/// <summary>
/// Specifies the directives of a single <c>Cache-Control</c> profile.
/// </summary>
/// <remarks>
/// Directives that would contradict one another (for example <see cref="NoStore"/> together with
/// <see cref="MaxAge"/>, or <see cref="Immutable"/> together with <see cref="MustRevalidate"/>) are rejected
/// during options validation so that the emitted header is always coherent.
/// </remarks>
public sealed class CacheProfileOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether the response may be stored by any cache (<c>public</c>) or only by a private cache (<c>private</c>).
    /// </summary>
    /// <value><c>true</c> to emit <c>public</c>; otherwise <c>private</c>. The default is <c>true</c>.</value>
    public bool Public { get; set; } = true;

    /// <summary>
    /// Gets or sets the freshness lifetime for private caches (<c>max-age</c>).
    /// </summary>
    /// <value>The freshness lifetime, or <c>null</c> to omit <c>max-age</c>.</value>
    public TimeSpan? MaxAge { get; set; }

    /// <summary>
    /// Gets or sets the freshness lifetime for shared caches such as a CDN (<c>s-maxage</c>).
    /// </summary>
    /// <value>The shared freshness lifetime, or <c>null</c> to omit <c>s-maxage</c>.</value>
    public TimeSpan? SharedMaxAge { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a stale response must be revalidated with the origin (<c>must-revalidate</c>).
    /// </summary>
    /// <value><c>true</c> to emit <c>must-revalidate</c>; otherwise <c>false</c>.</value>
    public bool MustRevalidate { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether caches must revalidate before reuse (<c>no-cache</c>).
    /// </summary>
    /// <value><c>true</c> to emit <c>no-cache</c>; otherwise <c>false</c>.</value>
    public bool NoCache { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the response may not be stored by any cache (<c>no-store</c>).
    /// </summary>
    /// <value><c>true</c> to emit <c>no-store</c>; otherwise <c>false</c>.</value>
    public bool NoStore { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the response will not change and need not be revalidated (<c>immutable</c>).
    /// </summary>
    /// <value><c>true</c> to emit <c>immutable</c>; otherwise <c>false</c>.</value>
    public bool Immutable { get; set; }

    /// <summary>
    /// Gets or sets the window during which a stale response may be served while it is revalidated in the background (<c>stale-while-revalidate</c>).
    /// </summary>
    /// <value>The stale-while-revalidate window, or <c>null</c> to omit the directive.</value>
    public TimeSpan? StaleWhileRevalidate { get; set; }

    /// <summary>
    /// Gets or sets the window during which a stale response may be served when the origin is unreachable (<c>stale-if-error</c>).
    /// </summary>
    /// <value>The stale-if-error window, or <c>null</c> to omit the directive.</value>
    public TimeSpan? StaleIfError { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether intermediaries are forbidden from transforming the response (<c>no-transform</c>).
    /// </summary>
    /// <value><c>true</c> to emit <c>no-transform</c>; otherwise <c>false</c>. The default is <c>false</c> so a CDN may optimize responses.</value>
    public bool NoTransform { get; set; }
}
