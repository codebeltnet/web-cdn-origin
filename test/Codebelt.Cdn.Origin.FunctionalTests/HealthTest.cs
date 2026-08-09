using System.Net;
using Codebelt.Extensions.Xunit;
using Codebelt.Extensions.Xunit.Hosting.AspNetCore;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Codebelt.Cdn.Origin;

public class HealthTest : Test
{
    public HealthTest(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async Task Live_ShouldReturnHealthy_WithNoStore()
    {
        await using var application = new CdnOriginTestApplication(TestOutput);
        using var client = application.CreateClient();

        using var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Ready_ShouldReturnHealthy_WhenContentRootAvailable()
    {
        await using var application = new CdnOriginTestApplication(TestOutput);
        using var client = application.CreateClient();

        using var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Ready_ShouldReturnUnhealthy_WhenContentRootDisappears()
    {
        await using var application = new CdnOriginTestApplication(TestOutput);
        using var client = application.CreateClient();

        using (var healthy = await client.GetAsync("/health/ready"))
        {
            Assert.Equal(HttpStatusCode.OK, healthy.StatusCode);
        }

        Directory.Delete(application.Content.Root, recursive: true);

        using var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
        Assert.Equal("Unhealthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Health_ShouldBeUnmapped_WhenDisabled()
    {
        await using var application = new CdnOriginTestApplication(TestOutput, new Dictionary<string, string?>()
        {
            ["CdnOrigin:Health:Enabled"] = "false"
        });

        using var client = application.CreateClient();

        using var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
