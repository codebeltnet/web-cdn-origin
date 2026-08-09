namespace Codebelt.Cdn.Origin.Hosting;

/// <summary>
/// Represents an observed snapshot of the configured static content root used to make a validation decision.
/// </summary>
/// <param name="ResolvedPath">The fully resolved content root path.</param>
/// <param name="Exists">A value indicating whether the path exists as either a file or a directory.</param>
/// <param name="IsDirectory">A value indicating whether the path is a directory.</param>
/// <param name="IsReadable">A value indicating whether the directory can be enumerated.</param>
/// <param name="ExposesApplicationFiles">A value indicating whether serving the path would expose application files.</param>
public readonly record struct ContentRootProbe(
    string ResolvedPath,
    bool Exists,
    bool IsDirectory,
    bool IsReadable,
    bool ExposesApplicationFiles);
