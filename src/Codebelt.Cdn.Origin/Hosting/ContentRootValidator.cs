namespace Codebelt.Cdn.Origin.Hosting;

/// <summary>
/// Validates the configured static content root. The pure <see cref="Evaluate"/> decision is separated from the
/// filesystem <see cref="Probe(string, string)"/> so the decision logic is fully deterministic and testable.
/// </summary>
public static class ContentRootValidator
{
    /// <summary>
    /// Validates the configured <paramref name="contentRoot"/> against the specified application directory.
    /// </summary>
    /// <param name="contentRoot">The configured content root path.</param>
    /// <param name="applicationDirectory">The application base directory whose files must not be exposed.</param>
    /// <returns>A <see cref="ContentRootValidationResult"/> describing the outcome.</returns>
    public static ContentRootValidationResult Validate(string? contentRoot, string applicationDirectory)
    {
        ContentRootProbe probe = Probe(contentRoot, applicationDirectory);
        return Evaluate(contentRoot, probe);
    }

    /// <summary>
    /// Evaluates the validation decision for the specified <paramref name="probe"/>.
    /// </summary>
    /// <param name="contentRoot">The configured content root path.</param>
    /// <param name="probe">The observed content root snapshot.</param>
    /// <returns>A <see cref="ContentRootValidationResult"/> describing the outcome.</returns>
    public static ContentRootValidationResult Evaluate(string? contentRoot, ContentRootProbe probe)
    {
        if (string.IsNullOrWhiteSpace(contentRoot))
        {
            return ContentRootValidationResult.Failure(probe.ResolvedPath, "The content root must be configured.");
        }

        if (!probe.Exists)
        {
            return ContentRootValidationResult.Failure(probe.ResolvedPath, $"The content root '{probe.ResolvedPath}' does not exist.");
        }

        if (!probe.IsDirectory)
        {
            return ContentRootValidationResult.Failure(probe.ResolvedPath, $"The content root '{probe.ResolvedPath}' is not a directory.");
        }

        if (!probe.IsReadable)
        {
            return ContentRootValidationResult.Failure(probe.ResolvedPath, $"The content root '{probe.ResolvedPath}' is not readable.");
        }

        return probe.ExposesApplicationFiles
            ? ContentRootValidationResult.Failure(probe.ResolvedPath, $"The content root '{probe.ResolvedPath}' would expose application files.")
            : ContentRootValidationResult.Success(probe.ResolvedPath);
    }

    /// <summary>
    /// Observes the filesystem to build a <see cref="ContentRootProbe"/> for the specified <paramref name="contentRoot"/>.
    /// </summary>
    /// <param name="contentRoot">The configured content root path.</param>
    /// <param name="applicationDirectory">The application base directory whose files must not be exposed.</param>
    /// <returns>The observed <see cref="ContentRootProbe"/>.</returns>
    public static ContentRootProbe Probe(string? contentRoot, string applicationDirectory)
    {
        if (string.IsNullOrWhiteSpace(contentRoot))
        {
            return new ContentRootProbe(string.Empty, false, false, false, false);
        }

        string resolved;
        try
        {
            resolved = Path.GetFullPath(contentRoot);
        }
        catch (ArgumentException)
        {
            return new ContentRootProbe(contentRoot, false, false, false, false);
        }

        bool isDirectory = Directory.Exists(resolved);
        bool exists = isDirectory || File.Exists(resolved);
        bool isReadable = isDirectory && IsDirectoryReadable(resolved);
        bool exposesApplicationFiles = isDirectory && ExposesApplicationFiles(resolved, applicationDirectory);

        return new ContentRootProbe(resolved, exists, isDirectory, isReadable, exposesApplicationFiles);
    }

    /// <summary>
    /// Determines whether the specified directory can be enumerated.
    /// </summary>
    /// <param name="path">The directory path to probe.</param>
    /// <returns><c>true</c> if the directory can be enumerated; otherwise <c>false</c>.</returns>
    public static bool IsDirectoryReadable(string path)
    {
        try
        {
            using IEnumerator<string> enumerator = Directory.EnumerateFileSystemEntries(path).GetEnumerator();
            enumerator.MoveNext();
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <summary>
    /// Determines whether serving the specified <paramref name="contentRoot"/> would expose application files.
    /// </summary>
    /// <param name="contentRoot">The fully resolved content root path.</param>
    /// <param name="applicationDirectory">The application base directory.</param>
    /// <returns><c>true</c> if the application directory is the content root or nested within it; otherwise <c>false</c>.</returns>
    /// <remarks>Comparison is case-insensitive so the guard errs on the side of safety on every filesystem.</remarks>
    public static bool ExposesApplicationFiles(string contentRoot, string applicationDirectory)
    {
        string root = EnsureTrailingSeparator(Path.GetFullPath(contentRoot));
        string application = EnsureTrailingSeparator(Path.GetFullPath(applicationDirectory));
        return application.StartsWith(root, StringComparison.OrdinalIgnoreCase);
    }

    private static string EnsureTrailingSeparator(string path)
    {
        return path.EndsWith(Path.DirectorySeparatorChar)
            ? path
            : path + Path.DirectorySeparatorChar;
    }
}
