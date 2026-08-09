using System.Collections.Concurrent;
using System.Linq;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.Extensions.Primitives;

namespace Codebelt.Cdn.Origin;

/// <summary>
/// Test-only copy of the pre-2.0 path resolver used as the compatibility comparison baseline.
/// </summary>
internal sealed class LegacyCaseInsensitivePhysicalFileProvider : IFileProvider, IDisposable
{
    private readonly PhysicalFileProvider _provider;
    private readonly ConcurrentDictionary<string, string> _paths = new(StringComparer.OrdinalIgnoreCase);

    public LegacyCaseInsensitivePhysicalFileProvider(string root, ExclusionFilters filters = ExclusionFilters.Sensitive)
    {
        _provider = new PhysicalFileProvider(root, filters);
    }

    public IFileInfo GetFileInfo(string subpath)
    {
        return _provider.GetFileInfo(GetActualFilePath(subpath));
    }

    public IDirectoryContents GetDirectoryContents(string subpath)
    {
        return _provider.GetDirectoryContents(GetActualFilePath(subpath));
    }

    public IChangeToken Watch(string filter)
    {
        return _provider.Watch(filter);
    }

    public void Dispose()
    {
        _provider.Dispose();
    }

    private string GetActualFilePath(string path)
    {
        if (_paths.TryGetValue(path, out string? cachedPath))
        {
            return cachedPath;
        }

        string currentPath = _provider.Root;
        string[] segments = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < segments.Length; i++)
        {
            string part = segments[i];
            bool last = i == segments.Length - 1;

            if (part.Equals("~", StringComparison.Ordinal))
            {
                continue;
            }

            part = last ? GetFileName(part, currentPath) : GetDirectoryName(part, currentPath);
            if (part is null)
            {
                return path;
            }

            currentPath = Path.Combine(currentPath, part);
            segments[i] = part;
        }

        string actualPath = string.Join(Path.DirectorySeparatorChar, segments);
        _paths.TryAdd(path, actualPath);
        return actualPath;
    }

    private static string? GetFileName(string part, string folder)
    {
        return new DirectoryInfo(folder).GetFiles().FirstOrDefault(file => file.Name.Equals(part, StringComparison.OrdinalIgnoreCase))?.Name;
    }

    private static string? GetDirectoryName(string part, string folder)
    {
        return new DirectoryInfo(folder).GetDirectories().FirstOrDefault(directory => directory.Name.Equals(part, StringComparison.OrdinalIgnoreCase))?.Name;
    }
}
