namespace Codebelt.Cdn.Origin;

/// <summary>
/// Creates an isolated, deterministic temporary static content directory for functional tests.
/// </summary>
public sealed class TempContent : IDisposable
{
    public static readonly DateTimeOffset KnownLastModified = new(2024, 1, 2, 3, 4, 5, TimeSpan.Zero);

    public TempContent()
    {
        Root = Path.Combine(Path.GetTempPath(), "cdn-origin-func", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Root);
        Directory.CreateDirectory(Path.Combine(Root, "styles"));
        Directory.CreateDirectory(Path.Combine(Root, "assets"));

        WriteText("index.html", "<!doctype html><title>index</title>");
        WriteText("styles/site.css", "body{color:red}");
        WriteText("assets/app.4f2c.js", "console.log('immutable');");
        WriteText("notes.unknownext", "unknown content type payload");
        WriteText("book.txt", new string('A', 1024));
        File.WriteAllBytes(Path.Combine(Root, "logo.png"), new byte[512]);

        foreach (var file in Directory.EnumerateFiles(Root, "*", SearchOption.AllDirectories))
        {
            File.SetLastWriteTimeUtc(file, KnownLastModified.UtcDateTime);
        }
    }

    public string Root { get; }

    private void WriteText(string relativePath, string content)
    {
        var path = Path.Combine(Root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        File.WriteAllText(path, content);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Best-effort cleanup.
        }
    }
}
