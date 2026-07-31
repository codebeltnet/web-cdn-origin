using Codebelt.Cdn.Origin.Configuration;
using Codebelt.Extensions.Xunit;
using Xunit;

namespace Codebelt.Cdn.Origin.Hosting;

public class ContentTypeProviderFactoryTest : Test
{
    public ContentTypeProviderFactoryTest(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void Create_ShouldThrow_WhenOptionsAreNull()
    {
        Assert.Throws<ArgumentNullException>(() => ContentTypeProviderFactory.Create(null!));
    }

    [Fact]
    public void Create_ShouldMapKnownExtensions_AndRejectUnknown()
    {
        var provider = ContentTypeProviderFactory.Create(new ContentTypeOptions());

        Assert.True(provider.TryGetContentType("index.html", out var html));
        Assert.Equal("text/html", html);
        Assert.False(provider.TryGetContentType("data.unknownext", out _));
    }

    [Fact]
    public void Create_ShouldAddMapping_WithLeadingDot()
    {
        var options = new ContentTypeOptions();
        options.Mappings[".foo"] = "application/x-foo";

        var provider = ContentTypeProviderFactory.Create(options);

        Assert.True(provider.TryGetContentType("file.foo", out var contentType));
        Assert.Equal("application/x-foo", contentType);
    }

    [Fact]
    public void Create_ShouldNormalizeMapping_WithoutLeadingDot()
    {
        var options = new ContentTypeOptions();
        options.Mappings["bar"] = "application/x-bar";

        var provider = ContentTypeProviderFactory.Create(options);

        Assert.True(provider.TryGetContentType("file.bar", out var contentType));
        Assert.Equal("application/x-bar", contentType);
    }
}
