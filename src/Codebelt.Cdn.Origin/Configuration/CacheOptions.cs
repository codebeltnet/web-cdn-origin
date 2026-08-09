namespace Codebelt.Cdn.Origin.Configuration;

/// <summary>
/// Specifies the cache policy options, expressed as two explicit profiles: a revalidated profile for mutable
/// URLs and an immutable profile for versioned or content-addressed URLs.
/// </summary>
/// <remarks>
/// The following table shows the initial property values for an instance of <see cref="CacheOptions"/>.
/// <list type="table">
///     <listheader>
///         <term>Property</term>
///         <description>Initial Value</description>
///     </listheader>
///     <item>
///         <term><see cref="ImmutablePathPrefixes"/></term>
///         <description>Empty (all content uses the <see cref="Revalidate"/> profile)</description>
///     </item>
///     <item>
///         <term><see cref="Revalidate"/></term>
///         <description><c>public</c>, <c>max-age=12h</c>, <c>s-maxage=7d</c>, <c>must-revalidate</c></description>
///     </item>
///     <item>
///         <term><see cref="Immutable"/></term>
///         <description><c>public</c>, <c>max-age=365d</c>, <c>immutable</c></description>
///     </item>
/// </list>
/// </remarks>
public sealed class CacheOptions
{
    /// <summary>
    /// Gets or sets the request path prefixes that select the <see cref="Immutable"/> cache profile.
    /// </summary>
    /// <value>The request path prefixes served with the immutable cache profile. Matching is case-insensitive.</value>
    /// <remarks>Prefixes should represent versioned or content-addressed URLs whose content never changes.</remarks>
    public IList<string> ImmutablePathPrefixes { get; set; } = [];

    /// <summary>
    /// Gets or sets the cache profile applied to mutable content that should be revalidated.
    /// </summary>
    /// <value>The revalidated cache profile.</value>
    public CacheProfileOptions Revalidate { get; set; } = new()
    {
        Public = true,
        MaxAge = TimeSpan.FromHours(12),
        SharedMaxAge = TimeSpan.FromDays(7),
        MustRevalidate = true
    };

    /// <summary>
    /// Gets or sets the cache profile applied to versioned or content-addressed immutable content.
    /// </summary>
    /// <value>The immutable cache profile.</value>
    public CacheProfileOptions Immutable { get; set; } = new()
    {
        Public = true,
        MaxAge = TimeSpan.FromDays(365),
        Immutable = true
    };
}
