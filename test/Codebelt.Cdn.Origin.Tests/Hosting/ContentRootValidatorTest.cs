using Codebelt.Extensions.Xunit;
using Xunit;

namespace Codebelt.Cdn.Origin.Hosting;

public class ContentRootValidatorTest : Test
{
    public ContentRootValidatorTest(ITestOutputHelper output) : base(output)
    {
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Evaluate_ShouldFail_WhenContentRootIsMissing(string? contentRoot)
    {
        var result = ContentRootValidator.Evaluate(contentRoot, new ContentRootProbe(string.Empty, false, false, false, false));

        Assert.False(result.Succeeded);
        Assert.Contains("must be configured", result.ErrorMessage);
    }

    [Fact]
    public void Evaluate_ShouldFail_WhenContentRootDoesNotExist()
    {
        var result = ContentRootValidator.Evaluate("/cdnroot", new ContentRootProbe("/cdnroot", false, false, false, false));

        Assert.False(result.Succeeded);
        Assert.Contains("does not exist", result.ErrorMessage);
    }

    [Fact]
    public void Evaluate_ShouldFail_WhenContentRootIsNotADirectory()
    {
        var result = ContentRootValidator.Evaluate("/cdnroot", new ContentRootProbe("/cdnroot", true, false, false, false));

        Assert.False(result.Succeeded);
        Assert.Contains("is not a directory", result.ErrorMessage);
    }

    [Fact]
    public void Evaluate_ShouldFail_WhenContentRootIsNotReadable()
    {
        var result = ContentRootValidator.Evaluate("/cdnroot", new ContentRootProbe("/cdnroot", true, true, false, false));

        Assert.False(result.Succeeded);
        Assert.Contains("is not readable", result.ErrorMessage);
    }

    [Fact]
    public void Evaluate_ShouldFail_WhenContentRootExposesApplicationFiles()
    {
        var result = ContentRootValidator.Evaluate("/cdnroot", new ContentRootProbe("/cdnroot", true, true, true, true));

        Assert.False(result.Succeeded);
        Assert.Contains("would expose application files", result.ErrorMessage);
    }

    [Fact]
    public void Evaluate_ShouldSucceed_WhenProbeIsValid()
    {
        var result = ContentRootValidator.Evaluate("/cdnroot", new ContentRootProbe("/cdnroot", true, true, true, false));

        Assert.True(result.Succeeded);
        Assert.Null(result.ErrorMessage);
        Assert.Equal("/cdnroot", result.ResolvedPath);
    }

    [Fact]
    public void IsDirectoryReadable_ShouldReturnTrue_ForExistingDirectory()
    {
        using var temp = new TempDirectory();

        Assert.True(ContentRootValidator.IsDirectoryReadable(temp.Path));
    }

    [Fact]
    public void IsDirectoryReadable_ShouldReturnFalse_ForMissingDirectory()
    {
        var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        Assert.False(ContentRootValidator.IsDirectoryReadable(missing));
    }

    [Fact]
    public void IsDirectoryReadable_ShouldThrow_ForNullPath()
    {
        Assert.Throws<ArgumentNullException>(() => ContentRootValidator.IsDirectoryReadable(null!));
    }

    [Fact]
    public void ExposesApplicationFiles_ShouldReturnTrue_WhenApplicationIsInsideContentRoot()
    {
        using var temp = new TempDirectory();
        var applicationDirectory = Path.Combine(temp.Path, "app");

        Assert.True(ContentRootValidator.ExposesApplicationFiles(temp.Path, applicationDirectory));
    }

    [Fact]
    public void ExposesApplicationFiles_ShouldReturnTrue_WhenPathsAreEqual()
    {
        using var temp = new TempDirectory();

        Assert.True(ContentRootValidator.ExposesApplicationFiles(temp.Path, temp.Path));
    }

    [Fact]
    public void ExposesApplicationFiles_ShouldReturnFalse_WhenDirectoriesAreSeparate()
    {
        using var content = new TempDirectory();
        using var application = new TempDirectory();

        Assert.False(ContentRootValidator.ExposesApplicationFiles(content.Path, application.Path));
    }

    [Fact]
    public void ExposesApplicationFiles_ShouldReturnTrue_WhenContentRootIsSymbolicLinkToApplicationDirectory()
    {
        using var application = new TempDirectory();
        using var linkHost = new TempDirectory();
        var contentRoot = Path.Combine(linkHost.Path, "content");
        CreateDirectorySymbolicLinkOrSkip(contentRoot, application.Path);

        try
        {
            Assert.True(ContentRootValidator.ExposesApplicationFiles(contentRoot, application.Path));

            var probe = ContentRootValidator.Probe(contentRoot, application.Path);
            Assert.Equal(Path.GetFullPath(application.Path), probe.ResolvedPath);
        }
        finally
        {
            Directory.Delete(contentRoot);
        }
    }

    [Fact]
    public void Validate_ShouldFail_WhenContentRootIsSymbolicLinkToApplicationParent()
    {
        using var applicationParent = new TempDirectory();
        var applicationDirectory = Directory.CreateDirectory(Path.Combine(applicationParent.Path, "app")).FullName;
        using var linkHost = new TempDirectory();
        var contentRoot = Path.Combine(linkHost.Path, "content");
        CreateDirectorySymbolicLinkOrSkip(contentRoot, applicationParent.Path);

        try
        {
            var result = ContentRootValidator.Validate(contentRoot, applicationDirectory);

            Assert.False(result.Succeeded);
            Assert.Contains("would expose application files", result.ErrorMessage);
        }
        finally
        {
            Directory.Delete(contentRoot);
        }
    }

    [Fact]
    public void Validate_ShouldFail_WhenContentRootTraversesSymbolicLinkToApplicationParent()
    {
        using var applicationParent = new TempDirectory();
        var applicationDirectory = Directory.CreateDirectory(Path.Combine(applicationParent.Path, "app")).FullName;
        using var linkHost = new TempDirectory();
        var linkedParent = Path.Combine(linkHost.Path, "parent");
        var contentRoot = Path.Combine(linkedParent, "app");
        CreateDirectorySymbolicLinkOrSkip(linkedParent, applicationParent.Path);

        try
        {
            var result = ContentRootValidator.Validate(contentRoot, applicationDirectory);

            Assert.False(result.Succeeded);
            Assert.Contains("would expose application files", result.ErrorMessage);
        }
        finally
        {
            Directory.Delete(linkedParent);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Probe_ShouldReportMissing_WhenContentRootIsBlank(string? contentRoot)
    {
        var probe = ContentRootValidator.Probe(contentRoot, AppContext.BaseDirectory);

        Assert.False(probe.Exists);
        Assert.False(probe.IsDirectory);
    }

    [Fact]
    public void Probe_ShouldReportMissing_WhenContentRootIsInvalid()
    {
        var probe = ContentRootValidator.Probe("invalid\0path", AppContext.BaseDirectory);

        Assert.False(probe.Exists);
        Assert.False(probe.IsDirectory);
    }

    [Fact]
    public void Probe_ShouldReportDirectory_ForExistingReadableDirectory()
    {
        using var temp = new TempDirectory();
        using var application = new TempDirectory();

        var probe = ContentRootValidator.Probe(temp.Path, application.Path);

        Assert.True(probe.Exists);
        Assert.True(probe.IsDirectory);
        Assert.True(probe.IsReadable);
        Assert.False(probe.ExposesApplicationFiles);
    }

    [Fact]
    public void Probe_ShouldReportFile_ForExistingFile()
    {
        using var temp = new TempDirectory();
        var file = Path.Combine(temp.Path, "content.txt");
        File.WriteAllText(file, "hello");

        var probe = ContentRootValidator.Probe(file, AppContext.BaseDirectory);

        Assert.True(probe.Exists);
        Assert.False(probe.IsDirectory);
        Assert.False(probe.IsReadable);
    }

    [Fact]
    public void Probe_ShouldReportMissing_ForNonexistentPath()
    {
        var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        var probe = ContentRootValidator.Probe(missing, AppContext.BaseDirectory);

        Assert.False(probe.Exists);
        Assert.False(probe.IsDirectory);
    }

    [Fact]
    public void Validate_ShouldSucceed_ForValidContentRoot()
    {
        using var content = new TempDirectory();
        using var application = new TempDirectory();

        var result = ContentRootValidator.Validate(content.Path, application.Path);

        Assert.True(result.Succeeded);
        TestOutput.WriteLine(result.ResolvedPath);
    }

    [Fact]
    public void Validate_ShouldFail_ForMissingContentRoot()
    {
        var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        var result = ContentRootValidator.Validate(missing, AppContext.BaseDirectory);

        Assert.False(result.Succeeded);
        Assert.Contains("does not exist", result.ErrorMessage);
    }

    [Fact]
    public void Validate_ShouldFail_WhenContentRootExposesApplicationFiles()
    {
        using var content = new TempDirectory();
        var application = Directory.CreateDirectory(Path.Combine(content.Path, "app")).FullName;

        var result = ContentRootValidator.Validate(content.Path, application);

        Assert.False(result.Succeeded);
        Assert.Contains("would expose application files", result.ErrorMessage);
    }

    private static void CreateDirectorySymbolicLinkOrSkip(string linkPath, string targetPath)
    {
        try
        {
            Directory.CreateSymbolicLink(linkPath, targetPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or PlatformNotSupportedException)
        {
            Assert.Skip($"Directory symbolic links are unavailable: {ex.Message}");
        }
    }
}
