using Codebelt.Cdn.Origin.Configuration;
using Microsoft.AspNetCore.StaticFiles;

namespace Codebelt.Cdn.Origin.Hosting;

/// <summary>
/// Creates the <see cref="FileExtensionContentTypeProvider"/> used to map file extensions to safe, explicit
/// content types, extended with any additional mappings from configuration.
/// </summary>
public static class ContentTypeProviderFactory
{
    /// <summary>
    /// Creates a <see cref="FileExtensionContentTypeProvider"/> from the specified <paramref name="options"/>.
    /// </summary>
    /// <param name="options">The <see cref="ContentTypeOptions"/> describing additional extension-to-MIME mappings.</param>
    /// <returns>A configured <see cref="FileExtensionContentTypeProvider"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> cannot be null.</exception>
    public static FileExtensionContentTypeProvider Create(ContentTypeOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var provider = new FileExtensionContentTypeProvider();

        foreach (KeyValuePair<string, string> mapping in options.Mappings)
        {
            provider.Mappings[NormalizeExtension(mapping.Key)] = mapping.Value;
        }

        return provider;
    }

    private static string NormalizeExtension(string extension)
    {
        return extension.StartsWith('.')
            ? extension
            : "." + extension;
    }
}
