using Codebelt.Cdn.Origin.Configuration;
using Codebelt.Extensions.Xunit;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Xunit;

namespace Codebelt.Cdn.Origin.Hosting;

public class ContentRootHealthCheckTest : Test
{
    public ContentRootHealthCheckTest(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenOptionsAccessorIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new ContentRootHealthCheck((IOptions<CdnOriginOptions>)null!));
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenOptionsAreNull()
    {
        Assert.Throws<ArgumentNullException>(() => new ContentRootHealthCheck((CdnOriginOptions)null!, AppContext.BaseDirectory));
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldReturnHealthy_WhenContentRootIsValid()
    {
        using var content = new TempDirectory();
        using var application = new TempDirectory();
        var check = new ContentRootHealthCheck(new CdnOriginOptions { ContentRoot = content.Path }, application.Path);

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
        TestOutput.WriteLine(result.Description);
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldReturnUnhealthy_WhenContentRootIsMissing()
    {
        var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var check = new ContentRootHealthCheck(new CdnOriginOptions { ContentRoot = missing }, AppContext.BaseDirectory);

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Contains("does not exist", result.Description);
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldUseOptionsAccessor()
    {
        using var content = new TempDirectory();
        var check = new ContentRootHealthCheck(Options.Create(new CdnOriginOptions { ContentRoot = content.Path }));

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }
}
