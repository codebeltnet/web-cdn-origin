using System.Net;
using System.Net.Http;
using Codebelt.Extensions.Xunit;
using Xunit;

namespace Codebelt.Cdn.Origin;

public class CorsTest : Test
{
    public CorsTest(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async Task Get_ShouldAllowAnyOrigin_InPublicMode()
    {
        await using var application = new CdnOriginTestApplication();
        using var client = application.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/styles/site.css");
        request.Headers.Add("Origin", "https://consumer.example");
        using var response = await client.SendAsync(request);

        Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Origin", out var origin));
        Assert.Equal("*", string.Join(string.Empty, origin));
        Assert.True(response.Headers.TryGetValues("Cross-Origin-Resource-Policy", out var corp));
        Assert.Equal("cross-origin", string.Join(string.Empty, corp));
    }

    [Fact]
    public async Task Get_ShouldEchoAllowedOrigin_InRestrictedMode()
    {
        await using var application = new CdnOriginTestApplication(new Dictionary<string, string?>
        {
            ["CdnOrigin:Cors:AllowedOrigins:0"] = "https://allowed.example"
        });
        using var client = application.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/styles/site.css");
        request.Headers.Add("Origin", "https://allowed.example");
        using var response = await client.SendAsync(request);

        Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Origin", out var origin));
        Assert.Equal("https://allowed.example", string.Join(string.Empty, origin));
    }

    [Fact]
    public async Task Get_ShouldNotAllowDisallowedOrigin_InRestrictedMode()
    {
        await using var application = new CdnOriginTestApplication(new Dictionary<string, string?>
        {
            ["CdnOrigin:Cors:AllowedOrigins:0"] = "https://allowed.example"
        });
        using var client = application.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/styles/site.css");
        request.Headers.Add("Origin", "https://evil.example");
        using var response = await client.SendAsync(request);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Options_ShouldHandlePreflight()
    {
        await using var application = new CdnOriginTestApplication();
        using var client = application.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Options, "/styles/site.css");
        request.Headers.Add("Origin", "https://consumer.example");
        request.Headers.Add("Access-Control-Request-Method", "GET");
        using var response = await client.SendAsync(request);

        Assert.True(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Get_ShouldEmitTimingAllowOrigin_WhenEnabled()
    {
        await using var application = new CdnOriginTestApplication(new Dictionary<string, string?>
        {
            ["CdnOrigin:Cors:TimingAllowOrigin"] = "true"
        });
        using var client = application.CreateClient();

        using var response = await client.GetAsync("/styles/site.css");

        Assert.True(response.Headers.TryGetValues("Timing-Allow-Origin", out var timing));
        Assert.Equal("*", string.Join(string.Empty, timing));
    }

    [Fact]
    public async Task Get_ShouldEmitTimingAllowOrigin_WithRestrictedOrigins()
    {
        await using var application = new CdnOriginTestApplication(new Dictionary<string, string?>
        {
            ["CdnOrigin:Cors:AllowedOrigins:0"] = "https://a.example",
            ["CdnOrigin:Cors:TimingAllowOrigin"] = "true"
        });
        using var client = application.CreateClient();

        using var response = await client.GetAsync("/styles/site.css");

        Assert.True(response.Headers.TryGetValues("Timing-Allow-Origin", out var timing));
        Assert.Equal("https://a.example", string.Join(string.Empty, timing));
    }

    [Fact]
    public async Task Get_ShouldOmitCorsHeaders_WhenDisabled()
    {
        await using var application = new CdnOriginTestApplication(new Dictionary<string, string?>
        {
            ["CdnOrigin:Cors:Enabled"] = "false"
        });
        using var client = application.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/styles/site.css");
        request.Headers.Add("Origin", "https://consumer.example");
        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Get_ShouldOmitCrossOriginResourcePolicy_WhenNotConfigured()
    {
        await using var application = new CdnOriginTestApplication(new Dictionary<string, string?>
        {
            ["CdnOrigin:Cors:CrossOriginResourcePolicy"] = ""
        });
        using var client = application.CreateClient();

        using var response = await client.GetAsync("/styles/site.css");

        Assert.False(response.Headers.Contains("Cross-Origin-Resource-Policy"));
    }
}
