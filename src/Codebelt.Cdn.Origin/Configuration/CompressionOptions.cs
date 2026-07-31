namespace Codebelt.Cdn.Origin.Configuration;

/// <summary>
/// Specifies the response compression options.
/// </summary>
/// <remarks>
/// Compression is disabled by default because edge compression is normally preferred when the provider runs
/// behind a CDN. When enabled, only compressible content types are compressed.
/// <list type="table">
///     <listheader>
///         <term>Property</term>
///         <description>Initial Value</description>
///     </listheader>
///     <item>
///         <term><see cref="Enabled"/></term>
///         <description><c>false</c></description>
///     </item>
///     <item>
///         <term><see cref="EnableForHttps"/></term>
///         <description><c>true</c></description>
///     </item>
/// </list>
/// </remarks>
public sealed class CompressionOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether origin response compression is enabled.
    /// </summary>
    /// <value><c>true</c> to enable origin compression; otherwise <c>false</c>. The default is <c>false</c>.</value>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether compression is applied to responses served over HTTPS.
    /// </summary>
    /// <value><c>true</c> to compress HTTPS responses; otherwise <c>false</c>. The default is <c>true</c>.</value>
    public bool EnableForHttps { get; set; } = true;

    /// <summary>
    /// Gets or sets additional compressible MIME types beyond the built-in defaults.
    /// </summary>
    /// <value>The additional compressible MIME types.</value>
    public IList<string> AdditionalMimeTypes { get; set; } = [];
}
