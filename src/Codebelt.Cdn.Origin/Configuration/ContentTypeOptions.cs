namespace Codebelt.Cdn.Origin.Configuration;

/// <summary>
/// Specifies the content-type mapping options that govern which file types are served.
/// </summary>
/// <remarks>
/// Unknown file types are rejected by default. Additional file types can be served by adding explicit
/// extension-to-MIME <see cref="Mappings"/> rather than by enabling every unknown extension.
/// <list type="table">
///     <listheader>
///         <term>Property</term>
///         <description>Initial Value</description>
///     </listheader>
///     <item>
///         <term><see cref="ServeUnknownFileTypes"/></term>
///         <description><c>false</c></description>
///     </item>
/// </list>
/// </remarks>
public sealed class ContentTypeOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether files with an unknown extension are served.
    /// </summary>
    /// <value><c>true</c> to serve unknown file types using <see cref="DefaultContentType"/>; otherwise <c>false</c>. The default is <c>false</c>.</value>
    public bool ServeUnknownFileTypes { get; set; }

    /// <summary>
    /// Gets or sets the content type used when <see cref="ServeUnknownFileTypes"/> is <c>true</c> and a file has an unknown extension.
    /// </summary>
    /// <value>The default content type, or <c>null</c> when unknown file types are rejected.</value>
    public string? DefaultContentType { get; set; }

    /// <summary>
    /// Gets or sets additional extension-to-MIME mappings, keyed by file extension (including the leading dot).
    /// </summary>
    /// <value>The additional extension-to-MIME mappings.</value>
    public IDictionary<string, string> Mappings { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
