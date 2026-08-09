namespace Codebelt.Cdn.Origin.Configuration;

/// <summary>
/// Specifies the operational health check options.
/// </summary>
/// <remarks>
/// The following table shows the initial property values for an instance of <see cref="HealthOptions"/>.
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
///         <term><see cref="LivePath"/></term>
///         <description><c>/health/live</c></description>
///     </item>
///     <item>
///         <term><see cref="ReadyPath"/></term>
///         <description><c>/health/ready</c></description>
///     </item>
/// </list>
/// </remarks>
public sealed class HealthOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether the health check endpoints are mapped.
    /// </summary>
    /// <value><c>true</c> to map the health check endpoints; otherwise <c>false</c>. The default is <c>true</c>.</value>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the request path of the liveness endpoint.
    /// </summary>
    /// <value>The liveness endpoint path. The default is <c>/health/live</c>.</value>
    public string LivePath { get; set; } = "/health/live";

    /// <summary>
    /// Gets or sets the request path of the readiness endpoint.
    /// </summary>
    /// <value>The readiness endpoint path. The default is <c>/health/ready</c>.</value>
    public string ReadyPath { get; set; } = "/health/ready";
}
