using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.Extensions.Primitives;

namespace Codebelt.Cdn.Origin
{
    public class CaseInsensitivePhysicalFileProvider : IFileProvider // kudos to Pierluc SS @ https://stackoverflow.com/questions/50096995/make-asp-net-core-server-kestrel-case-sensitive-on-windows
    {
        private readonly PhysicalFileProvider _provider;
        private static ConcurrentDictionary<string, string> _paths;
        
        public CaseInsensitivePhysicalFileProvider(string root, ExclusionFilters filters = ExclusionFilters.Sensitive)
        {
            _provider = new PhysicalFileProvider(root, filters);
            _paths = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public IFileInfo GetFileInfo(string subpath)
        {
            var actualPath = GetActualFilePath(subpath);
            return _provider.GetFileInfo(actualPath);
        }

        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            var actualPath = GetActualFilePath(subpath);
            return _provider.GetDirectoryContents(actualPath);
        }

        public IChangeToken Watch(string filter) => _provider.Watch(filter);

        // Determines (and caches) the actual path for a file
        private string GetActualFilePath(string path)
        {
            // Check if this has already been matched before
            if (_paths.ContainsKey(path)) return _paths[path];

            // Break apart the path and get the root folder to work from
            var currPath = _provider.Root;
            var segments = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            // Start stepping up the folders to replace with the correct cased folder name
            for (var i = 0; i < segments.Length; i++)
            {
                var part = segments[i];
                var last = i == segments.Length - 1;

                // Ignore the root
                if (part.Equals("~")) continue;

                // Process the file name if this is the last segment
                part = last ? GetFileName(part, currPath) : GetDirectoryName(part, currPath);

                // If no matches were found, just return the original string
                if (part == null) return path;

                // Update the actualPath with the correct name casing
                currPath = Path.Combine(currPath, part);
                segments[i] = part;
            }

            // Save this path for later use
            var actualPath = string.Join(Path.DirectorySeparatorChar, segments);
            _paths.TryAdd(path, actualPath);
            return actualPath;
        }

        // Searches for a matching file name in the current directory regardless of case
        private static string GetFileName(string part, string folder) =>
            new DirectoryInfo(folder).GetFiles().FirstOrDefault(file => file.Name.Equals(part, StringComparison.OrdinalIgnoreCase))?.Name;

        // Searches for a matching folder in the current directory regardless of case
        private static string GetDirectoryName(string part, string folder) =>
            new DirectoryInfo(folder).GetDirectories().FirstOrDefault(dir => dir.Name.Equals(part, StringComparison.OrdinalIgnoreCase))?.Name;
    }
}