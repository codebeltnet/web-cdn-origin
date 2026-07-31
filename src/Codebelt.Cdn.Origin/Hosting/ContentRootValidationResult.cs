namespace Codebelt.Cdn.Origin.Hosting;

/// <summary>
/// Represents the outcome of validating the configured static content root.
/// </summary>
public sealed class ContentRootValidationResult
{
    private ContentRootValidationResult(bool succeeded, string resolvedPath, string? errorMessage)
    {
        Succeeded = succeeded;
        ResolvedPath = resolvedPath;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Gets a value indicating whether validation succeeded.
    /// </summary>
    /// <value><c>true</c> if the content root is valid; otherwise <c>false</c>.</value>
    public bool Succeeded { get; }

    /// <summary>
    /// Gets the fully resolved content root path.
    /// </summary>
    /// <value>The fully resolved content root path.</value>
    public string ResolvedPath { get; }

    /// <summary>
    /// Gets the error message describing why validation failed.
    /// </summary>
    /// <value>The error message, or <c>null</c> when <see cref="Succeeded"/> is <c>true</c>.</value>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Creates a successful <see cref="ContentRootValidationResult"/>.
    /// </summary>
    /// <param name="resolvedPath">The fully resolved content root path.</param>
    /// <returns>A successful <see cref="ContentRootValidationResult"/>.</returns>
    public static ContentRootValidationResult Success(string resolvedPath) => new(true, resolvedPath, null);

    /// <summary>
    /// Creates a failed <see cref="ContentRootValidationResult"/>.
    /// </summary>
    /// <param name="resolvedPath">The fully resolved content root path.</param>
    /// <param name="errorMessage">The error message describing why validation failed.</param>
    /// <returns>A failed <see cref="ContentRootValidationResult"/>.</returns>
    public static ContentRootValidationResult Failure(string resolvedPath, string errorMessage) => new(false, resolvedPath, errorMessage);
}
