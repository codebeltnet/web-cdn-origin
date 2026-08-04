using System.Collections.Concurrent;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.Extensions.Primitives;

namespace Codebelt.Cdn.Origin;

/// <summary>
/// Resolves physical file and directory segments without regard to request-path casing, then delegates
/// file metadata, streams, directory contents, and change notifications to <see cref="PhysicalFileProvider"/>.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="PhysicalFileProvider"/> maps request segments directly to the underlying file system. That means
/// lookup casing follows the operating system and mounted volume. This decorator preserves the historical
/// case-insensitive URL contract on both case-sensitive and case-insensitive file systems.
/// </para>
/// <para>
/// If a case-sensitive file system contains multiple entries whose names differ only by case, an ambiguous
/// request is not resolved to an arbitrary entry.
/// </para>
/// </remarks>
/// <param name="root">The absolute directory to use as the provider root.</param>
/// <param name="filters">The file-system exclusion filters.</param>
public sealed class CaseInsensitivePhysicalFileProvider(string root, ExclusionFilters filters = ExclusionFilters.Sensitive) : IFileProvider, IDisposable
{
    private readonly PhysicalFileProvider _provider = new(root, filters);
    private readonly ConcurrentDictionary<string, string> _paths = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public IFileInfo GetFileInfo(string subpath) => _provider.GetFileInfo(ResolvePath(subpath, finalSegmentIsDirectory: false));

    /// <inheritdoc />
    public IDirectoryContents GetDirectoryContents(string subpath) => _provider.GetDirectoryContents(ResolvePath(subpath, finalSegmentIsDirectory: true));

    /// <inheritdoc />
    public IChangeToken Watch(string filter)
    {
        ArgumentNullException.ThrowIfNull(filter);

        // PhysicalFileProvider uses '*' to identify glob patterns. Resolve literal paths so
        // case-sensitive file systems watch the same physical entry as the other operations,
        // while leaving glob filters intact.
        if (filter.Length == 0 || filter.Contains('*'))
        {
            return _provider.Watch(filter);
        }

        bool finalSegmentIsDirectory = filter[^1] is '/' or '\\';
        string resolvedFilter = ResolvePath(filter, finalSegmentIsDirectory);

        if (finalSegmentIsDirectory && resolvedFilter.Length > 0 && resolvedFilter[^1] is not ('/' or '\\'))
        {
            resolvedFilter += Path.DirectorySeparatorChar;
        }

        return _provider.Watch(resolvedFilter);
    }

    /// <summary>
    /// Releases the underlying physical file provider.
    /// </summary>
    public void Dispose() => _provider.Dispose();

    private string ResolvePath(string subpath, bool finalSegmentIsDirectory)
    {
        if (string.IsNullOrEmpty(subpath))
        {
            return subpath;
        }

        if (_paths.TryGetValue(subpath, out string? cachedPath))
        {
            return cachedPath;
        }

        string[] segments = subpath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
        {
            return subpath;
        }

        string currentPath = _provider.Root;
        for (int i = 0; i < segments.Length; i++)
        {
            string requestedName = segments[i];
            bool isDirectory = i < segments.Length - 1 || finalSegmentIsDirectory;
            string? actualName = FindMatchingName(requestedName, currentPath, isDirectory);
            if (actualName is null)
            {
                return subpath;
            }

            segments[i] = actualName;
            currentPath = Path.Combine(currentPath, actualName);
        }

        string resolvedPath = string.Join(Path.DirectorySeparatorChar, segments);
        _paths.TryAdd(subpath, resolvedPath);
        return resolvedPath;
    }

    private static string? FindMatchingName(string requestedName, string directoryPath, bool directory)
    {
        try
        {
            IEnumerable<string> entries = directory
                ? Directory.EnumerateDirectories(directoryPath)
                : Directory.EnumerateFiles(directoryPath);

            string? exactMatch = null;
            string? caseInsensitiveMatch = null;
            int caseInsensitiveMatches = 0;

            foreach (string entry in entries)
            {
                string name = Path.GetFileName(entry);
                if (string.Equals(name, requestedName, StringComparison.Ordinal))
                {
                    exactMatch = name;
                }

                if (string.Equals(name, requestedName, StringComparison.OrdinalIgnoreCase))
                {
                    caseInsensitiveMatch = name;
                    caseInsensitiveMatches++;
                }
            }

            if (exactMatch is not null)
            {
                return exactMatch;
            }

            return caseInsensitiveMatches == 1 ? caseInsensitiveMatch : null;
        }
        catch (Exception ex) when (ex is ArgumentException or DirectoryNotFoundException or IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}
