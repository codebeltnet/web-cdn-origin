using Codebelt.Extensions.Xunit;
using Cuemon.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Codebelt.Cdn.Origin;

public class PortablePhysicalFileProviderTest : Test
{
    public PortablePhysicalFileProviderTest(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void GetFileInfo_ShouldResolveMixedCaseFile_AndReuseResolvedPath()
    {
        using var content = CreateContent();
        using var provider = new PortablePhysicalFileProvider(content.Path);

        IFileInfo first = provider.GetFileInfo("/styles/SITE.CSS");
        IFileInfo second = provider.GetFileInfo("/styles/site.css");

        Assert.True(first.Exists);
        Assert.True(second.Exists);
        Assert.Equal(first.PhysicalPath, second.PhysicalPath);
    }

    [Fact]
    public void GetDirectoryContents_ShouldResolveMixedCaseDirectory()
    {
        using var content = CreateContent();
        using var provider = new PortablePhysicalFileProvider(content.Path);

        IDirectoryContents contents = provider.GetDirectoryContents("/StYlEs");

        Assert.True(contents.Exists);
        Assert.Contains(contents, file => file.Name == "site.css");
    }

    [Fact]
    public void GetFileInfo_ShouldReturnNotFound_ForEmptyOrMissingPath()
    {
        using var content = CreateContent();
        using var provider = new PortablePhysicalFileProvider(content.Path);

        Assert.False(provider.GetFileInfo(string.Empty).Exists);
        Assert.False(provider.GetFileInfo("/").Exists);
        Assert.False(provider.GetFileInfo("/missing.css").Exists);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenRootDoesNotExist()
    {
        var missingRoot = Path.Combine(Path.GetTempPath(), "cdn-origin-missing", Guid.NewGuid().ToString("N"));

        Assert.Throws<DirectoryNotFoundException>(() => new PortablePhysicalFileProvider(missingRoot));
    }

    [Fact]
    public void Watch_ShouldResolveMixedCaseLiteralPath()
    {
        using var content = CreateContent();
        using var provider = new PortablePhysicalFileProvider(content.Path);

        IChangeToken token = provider.Watch("STYLES/SITE.CSS");

        File.AppendAllText(Path.Combine(content.Path, "styles", "site.css"), "\nbody{}");

        Assert.True(SpinWait.SpinUntil(() => token.HasChanged, TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public void Watch_ShouldPreserveGlobFilter()
    {
        using var content = CreateContent();
        using var provider = new PortablePhysicalFileProvider(content.Path);

        IChangeToken token = provider.Watch("STYLES/*.CSS");

        Assert.NotNull(token);
    }

    [Fact]
    public void Watch_ShouldReturnToken_ForEmptyFilter()
    {
        using var content = CreateContent();
        using var provider = new PortablePhysicalFileProvider(content.Path);

        Assert.NotNull(provider.Watch(string.Empty));
    }

    private static TempDirectory CreateContent()
    {
        var content = new TempDirectory();
        string styles = Directory.CreateDirectory(Path.Combine(content.Path, "styles")).FullName;
        File.WriteAllText(Path.Combine(styles, "site.css"), "body{}");
        return content;
    }
}
