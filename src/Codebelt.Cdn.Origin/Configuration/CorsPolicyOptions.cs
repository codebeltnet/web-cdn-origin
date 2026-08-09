namespace Codebelt.Cdn.Origin.Configuration;

/// <summary>
/// Specifies the cross-origin resource sharing (CORS) options for public asset hosting.
/// </summary>
/// <remarks>
/// The following table shows the initial property values for an instance of <see cref="CorsPolicyOptions"/>.
/// <list type="table">
///     <listheader>
///         <term>Property</term>
///         <description>Initial Value</description>
///     </listheader>
///     <item>
///         <term><see cref="Enabled"/></term>
///         <description><c>true</c></description>
///     </item>
///     <item>
///         <term><see cref="AllowedOrigins"/></term>
///         <description><c>*</c> (public content mode)</description>
///     </item>
///     <item>
///         <term><see cref="AllowCredentials"/></term>
///         <description><c>false</c></description>
///     </item>
///     <item>
///         <term><see cref="CrossOriginResourcePolicy"/></term>
///         <description><c>cross-origin</c></description>
///     </item>
/// </list>
/// </remarks>
public sealed class CorsPolicyOptions
{
    /// <summary>
    /// The name of the CORS policy registered for the Static Content Provider.
    /// </summary>
    public const string PolicyName = "CdnOrigin";

    /// <summary>
    /// The wildcard origin value that enables public content mode.
    /// </summary>
    public const string AnyOrigin = "*";

    /// <summary>
    /// Gets or sets a value indicating whether CORS handling is enabled.
    /// </summary>
    /// <value><c>true</c> to enable CORS handling; otherwise <c>false</c>. The default is <c>true</c>.</value>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the allowed origins.
    /// </summary>
    /// <value>The allowed origins. When empty, or when it contains a single <see cref="AnyOrigin"/> entry, public content mode is used.</value>
    public IList<string> AllowedOrigins { get; set; } = [];

    /// <summary>
    /// Gets or sets the response headers exposed to the browser (<c>Access-Control-Expose-Headers</c>).
    /// </summary>
    /// <value>The exposed response headers.</value>
    public IList<string> ExposedHeaders { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether credentialed requests are allowed (<c>Access-Control-Allow-Credentials</c>).
    /// </summary>
    /// <value><c>true</c> to allow credentialed requests; otherwise <c>false</c>. Cannot be combined with <see cref="AnyOrigin"/>.</value>
    public bool AllowCredentials { get; set; }

    /// <summary>
    /// Gets or sets the value of the <c>Cross-Origin-Resource-Policy</c> response header emitted for served assets.
    /// </summary>
    /// <value>The <c>Cross-Origin-Resource-Policy</c> value, or <c>null</c> to omit the header. The default is <c>cross-origin</c>.</value>
    public string? CrossOriginResourcePolicy { get; set; } = "cross-origin";

    /// <summary>
    /// Gets or sets a value indicating whether a <c>Timing-Allow-Origin</c> response header is emitted for served assets.
    /// </summary>
    /// <value><c>true</c> to emit <c>Timing-Allow-Origin</c>; otherwise <c>false</c>.</value>
    public bool TimingAllowOrigin { get; set; }
}
