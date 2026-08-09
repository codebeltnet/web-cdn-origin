namespace Codebelt.Cdn.Origin.Configuration;

/// <summary>
/// Specifies the root configuration options for the Static Content Provider.
/// </summary>
/// <remarks>
/// The following table shows the initial property values for an instance of <see cref="CdnOriginOptions"/>.
/// <list type="table">
///     <listheader>
///         <term>Property</term>
///         <description>Initial Value</description>
///     </listheader>
///     <item>
///         <term><see cref="ContentRoot"/></term>
///         <description><c>/cdnroot</c></description>
///     </item>
///     <item>
///         <term><see cref="DefaultDocuments"/></term>
///         <description><c>default.htm; default.html; index.htm; index.html</c></description>
///     </item>
/// </list>
/// </remarks>
public sealed class CdnOriginOptions
{
    /// <summary>
    /// The configuration section name used to bind <see cref="CdnOriginOptions"/>.
    /// </summary>
    public const string SectionName = "CdnOrigin";

    /// <summary>
    /// The default document file names served when <see cref="DefaultDocuments"/> is not configured.
    /// </summary>
    public static readonly IReadOnlyList<string> StandardDefaultDocuments =
    [
        "default.htm",
        "default.html",
        "index.htm",
        "index.html"
    ];

    /// <summary>
    /// Gets or sets the absolute path to the directory of physical files that are served as static content.
    /// </summary>
    /// <value>The absolute path to the static content directory.</value>
    /// <remarks>The directory must exist at startup; it may be an empty mount that is populated at runtime.</remarks>
    public string ContentRoot { get; set; } = "/cdnroot";

    /// <summary>
    /// Gets or sets the ordered list of default document file names served when a directory is requested.
    /// </summary>
    /// <value>The ordered list of default document file names. When empty, <see cref="StandardDefaultDocuments"/> is used.</value>
    public IList<string> DefaultDocuments { get; set; } = [];

    /// <summary>
    /// Gets or sets the cache policy options.
    /// </summary>
    /// <value>The cache policy options.</value>
    public CacheOptions Cache { get; set; } = new();

    /// <summary>
    /// Gets or sets the cross-origin resource sharing (CORS) options.
    /// </summary>
    /// <value>The cross-origin resource sharing options.</value>
    public CorsPolicyOptions Cors { get; set; } = new();

    /// <summary>
    /// Gets or sets the response compression options.
    /// </summary>
    /// <value>The response compression options.</value>
    public CompressionOptions Compression { get; set; } = new();

    /// <summary>
    /// Gets or sets the content-type mapping options.
    /// </summary>
    /// <value>The content-type mapping options.</value>
    public ContentTypeOptions ContentTypes { get; set; } = new();

    /// <summary>
    /// Gets or sets the health check options.
    /// </summary>
    /// <value>The health check options.</value>
    public HealthOptions Health { get; set; } = new();
}
