using System.Net.Http;
using System.Net.Http.Headers;
using Codebelt.Extensions.Xunit;
using Xunit;

namespace Codebelt.Cdn.Origin;

public class CompressionTest : Test
{
    public CompressionTest(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async Task Get_ShouldNotCompress_WhenCompressionDisabled()
    {
        await using var application = new CdnOriginTestApplication();
        using var client = application.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/styles/site.css");
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("br"));
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
        using var response = await client.SendAsync(request);

        Assert.Empty(response.Content.Headers.ContentEncoding);
    }

    [Fact]
    public async Task Get_ShouldCompressCompressibleContent_WhenEnabled()
    {
        await using var application = new CdnOriginTestApplication(new Dictionary<string, string?>
        {
            ["CdnOrigin:Compression:Enabled"] = "true"
        });
        using var client = application.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/styles/site.css");
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("br"));
        using var response = await client.SendAsync(request);

        Assert.Contains("br", response.Content.Headers.ContentEncoding);
        Assert.Contains("Accept-Encoding", response.Headers.Vary);
    }

    [Fact]
    public async Task Get_ShouldNotCompressPreCompressedContent_WhenEnabled()
    {
        await using var application = new CdnOriginTestApplication(new Dictionary<string, string?>
        {
            ["CdnOrigin:Compression:Enabled"] = "true"
        });
        using var client = application.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/logo.png");
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("br"));
        using var response = await client.SendAsync(request);

        Assert.Empty(response.Content.Headers.ContentEncoding);
    }
}
