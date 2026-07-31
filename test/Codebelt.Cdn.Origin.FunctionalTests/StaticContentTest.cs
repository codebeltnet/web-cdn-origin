using System.Net;
using System.Net.Http;
using Codebelt.Extensions.Xunit;
using Xunit;

namespace Codebelt.Cdn.Origin;

public class StaticContentTest : Test
{
    public StaticContentTest(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async Task Get_ShouldServeExistingFile()
    {
        await using var application = new CdnOriginTestApplication();
        using var client = application.CreateClient();

        using var response = await client.GetAsync("/styles/site.css");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/css", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("body{color:red}", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Get_ShouldReturnNotFound_ForMissingFile()
    {
        await using var application = new CdnOriginTestApplication();
        using var client = application.CreateClient();

        using var response = await client.GetAsync("/does-not-exist.css");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_ShouldServeStandardDefaultDocument_ForRootRequest()
    {
        await using var application = new CdnOriginTestApplication();
        using var client = application.CreateClient();

        using var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<title>index</title>", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Get_ShouldServeConfiguredDefaultDocument()
    {
        await using var application = new CdnOriginTestApplication(new Dictionary<string, string?>
        {
            ["CdnOrigin:DefaultDocuments:0"] = "index.html"
        });
        using var client = application.CreateClient();

        using var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("index", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Head_ShouldReturnHeadersWithoutBody()
    {
        await using var application = new CdnOriginTestApplication();
        using var client = application.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Head, "/book.txt");
        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1024, response.Content.Headers.ContentLength);
        Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Get_ShouldRejectUnknownContentType_ByDefault()
    {
        await using var application = new CdnOriginTestApplication();
        using var client = application.CreateClient();

        using var response = await client.GetAsync("/notes.unknownext");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_ShouldServeUnknownContentType_WhenConfigured()
    {
        await using var application = new CdnOriginTestApplication(new Dictionary<string, string?>
        {
            ["CdnOrigin:ContentTypes:ServeUnknownFileTypes"] = "true",
            ["CdnOrigin:ContentTypes:DefaultContentType"] = "application/octet-stream"
        });
        using var client = application.CreateClient();

        using var response = await client.GetAsync("/notes.unknownext");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/octet-stream", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Get_ShouldServeCustomMimeMapping()
    {
        await using var application = new CdnOriginTestApplication(new Dictionary<string, string?>
        {
            ["CdnOrigin:ContentTypes:Mappings:.unknownext"] = "application/x-note"
        });
        using var client = application.CreateClient();

        using var response = await client.GetAsync("/notes.unknownext");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/x-note", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Get_ShouldBlockPathTraversal()
    {
        await using var application = new CdnOriginTestApplication();
        using var client = application.CreateClient();

        using var response = await client.GetAsync("/%2e%2e/%2e%2e/appsettings.json");

        Assert.True(response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.BadRequest);
        Assert.DoesNotContain("CdnOrigin", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Get_ShouldRespectFilesystemCaseSemantics()
    {
        await using var application = new CdnOriginTestApplication();
        using var client = application.CreateClient();

        using var exact = await client.GetAsync("/styles/site.css");
        using var wrongCase = await client.GetAsync("/styles/SITE.CSS");

        Assert.Equal(HttpStatusCode.OK, exact.StatusCode);

        if (OperatingSystem.IsLinux())
        {
            Assert.Equal(HttpStatusCode.NotFound, wrongCase.StatusCode);
        }
        else
        {
            Assert.Equal(HttpStatusCode.OK, wrongCase.StatusCode);
        }
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("DELETE")]
    public async Task UnsupportedMethod_ShouldReturnMethodNotAllowed_ForExistingFile(string method)
    {
        await using var application = new CdnOriginTestApplication();
        using var client = application.CreateClient();

        using var request = new HttpRequestMessage(new HttpMethod(method), "/styles/site.css");
        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        Assert.Contains("GET", response.Content.Headers.Allow);
        Assert.Contains("HEAD", response.Content.Headers.Allow);
    }

    [Fact]
    public async Task UnsupportedMethod_ShouldReturnNotFound_ForMissingFile()
    {
        await using var application = new CdnOriginTestApplication();
        using var client = application.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/missing.css");
        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
