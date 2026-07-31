using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Codebelt.Extensions.Xunit;
using Xunit;

namespace Codebelt.Cdn.Origin;

public class HttpCachingTest : Test
{
    public HttpCachingTest(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async Task Get_ShouldEmitRevalidateCacheControl()
    {
        await using var application = new CdnOriginTestApplication();
        using var client = application.CreateClient();

        using var response = await client.GetAsync("/book.txt");

        Assert.True(response.Headers.TryGetValues("Cache-Control", out var values));
        var cacheControl = string.Join(", ", values);
        Assert.Contains("public", cacheControl);
        Assert.Contains("max-age=43200", cacheControl);
        Assert.Contains("s-maxage=604800", cacheControl);
        Assert.Contains("must-revalidate", cacheControl);
    }

    [Fact]
    public async Task Get_ShouldEmitImmutableCacheControl_ForConfiguredPrefix()
    {
        await using var application = new CdnOriginTestApplication(new Dictionary<string, string?>
        {
            ["CdnOrigin:Cache:ImmutablePathPrefixes:0"] = "/assets/"
        });
        using var client = application.CreateClient();

        using var response = await client.GetAsync("/assets/app.4f2c.js");

        Assert.True(response.Headers.TryGetValues("Cache-Control", out var values));
        var cacheControl = string.Join(", ", values);
        Assert.Contains("immutable", cacheControl);
        Assert.Contains("max-age=31536000", cacheControl);
    }

    [Fact]
    public async Task Get_ShouldEmitEntityTagAndLastModified()
    {
        await using var application = new CdnOriginTestApplication();
        using var client = application.CreateClient();

        using var response = await client.GetAsync("/book.txt");

        Assert.NotNull(response.Headers.ETag);
        Assert.NotNull(response.Content.Headers.LastModified);
    }

    [Fact]
    public async Task Get_ShouldReturnNotModified_ForMatchingIfNoneMatch()
    {
        await using var application = new CdnOriginTestApplication();
        using var client = application.CreateClient();

        using var first = await client.GetAsync("/book.txt");
        var etag = first.Headers.ETag!;

        using var request = new HttpRequestMessage(HttpMethod.Get, "/book.txt");
        request.Headers.IfNoneMatch.Add(etag);
        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotModified, response.StatusCode);
        Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Get_ShouldReturnNotModified_ForIfModifiedSince()
    {
        await using var application = new CdnOriginTestApplication();
        using var client = application.CreateClient();

        using var first = await client.GetAsync("/book.txt");
        var lastModified = first.Content.Headers.LastModified!.Value;

        using var request = new HttpRequestMessage(HttpMethod.Get, "/book.txt");
        request.Headers.IfModifiedSince = lastModified;
        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotModified, response.StatusCode);
    }

    [Fact]
    public async Task Get_ShouldReturnPartialContent_ForRangeRequest()
    {
        await using var application = new CdnOriginTestApplication();
        using var client = application.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/book.txt");
        request.Headers.Range = new RangeHeaderValue(0, 99);
        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.PartialContent, response.StatusCode);
        Assert.Equal(100, response.Content.Headers.ContentLength);
        Assert.Equal(1024, response.Content.Headers.ContentRange?.Length);
        Assert.Equal(new string('A', 100), await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Get_ShouldReturnRangeNotSatisfiable_ForUnsatisfiableRange()
    {
        await using var application = new CdnOriginTestApplication();
        using var client = application.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/book.txt");
        request.Headers.Range = new RangeHeaderValue(100000, 200000);
        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.RequestedRangeNotSatisfiable, response.StatusCode);
    }

    [Fact]
    public async Task Get_ShouldReturnPartialContent_ForIfRangeWithMatchingEtag()
    {
        await using var application = new CdnOriginTestApplication();
        using var client = application.CreateClient();

        using var first = await client.GetAsync("/book.txt");
        var etag = first.Headers.ETag!;

        using var request = new HttpRequestMessage(HttpMethod.Get, "/book.txt");
        request.Headers.Range = new RangeHeaderValue(0, 9);
        request.Headers.IfRange = new RangeConditionHeaderValue(etag);
        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.PartialContent, response.StatusCode);
        Assert.Equal(10, response.Content.Headers.ContentLength);
    }

    [Fact]
    public async Task Get_ShouldReturnFullContent_ForIfRangeWithStaleEtag()
    {
        await using var application = new CdnOriginTestApplication();
        using var client = application.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/book.txt");
        request.Headers.Range = new RangeHeaderValue(0, 9);
        request.Headers.IfRange = new RangeConditionHeaderValue(new EntityTagHeaderValue("\"stale\""));
        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1024, response.Content.Headers.ContentLength);
    }
}
