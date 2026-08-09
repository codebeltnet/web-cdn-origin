using Codebelt.Cdn.Origin.Configuration;
using Codebelt.Extensions.Xunit;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Codebelt.Cdn.Origin.Hosting;

public class CachePolicyResolverTest : Test
{
    public CachePolicyResolverTest(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void BuildCacheControl_ShouldThrow_WhenProfileIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => CachePolicyResolver.BuildCacheControl(null!));
    }

    [Fact]
    public void BuildCacheControl_ShouldRenderRevalidateDefaults()
    {
        var profile = new CacheProfileOptions
        {
            Public = true,
            MaxAge = TimeSpan.FromHours(12),
            SharedMaxAge = TimeSpan.FromDays(7),
            MustRevalidate = true
        };

        var header = CachePolicyResolver.BuildCacheControl(profile);

        Assert.Equal("public, max-age=43200, s-maxage=604800, must-revalidate", header);
        TestOutput.WriteLine(header);
    }

    [Fact]
    public void BuildCacheControl_ShouldRenderImmutableDefaults()
    {
        var profile = new CacheProfileOptions
        {
            Public = true,
            MaxAge = TimeSpan.FromDays(365),
            Immutable = true
        };

        var header = CachePolicyResolver.BuildCacheControl(profile);

        Assert.Equal("public, max-age=31536000, immutable", header);
    }

    [Fact]
    public void BuildCacheControl_ShouldRenderNoStore_AndIgnoreOtherDirectives()
    {
        var profile = new CacheProfileOptions
        {
            NoStore = true,
            Public = true,
            MaxAge = TimeSpan.FromHours(1)
        };

        var header = CachePolicyResolver.BuildCacheControl(profile);

        Assert.Equal("no-store", header);
    }

    [Fact]
    public void BuildCacheControl_ShouldRenderPrivate_WhenNotPublic()
    {
        var profile = new CacheProfileOptions { Public = false };

        var header = CachePolicyResolver.BuildCacheControl(profile);

        Assert.Equal("private", header);
    }

    [Fact]
    public void BuildCacheControl_ShouldRenderAllOptionalDirectives()
    {
        var profile = new CacheProfileOptions
        {
            Public = false,
            NoCache = true,
            MaxAge = TimeSpan.FromMinutes(5),
            SharedMaxAge = TimeSpan.FromMinutes(10),
            MustRevalidate = true,
            Immutable = true,
            StaleWhileRevalidate = TimeSpan.FromSeconds(30),
            StaleIfError = TimeSpan.FromSeconds(60),
            NoTransform = true
        };

        var header = CachePolicyResolver.BuildCacheControl(profile);

        Assert.Equal("private, no-cache, max-age=300, s-maxage=600, must-revalidate, immutable, stale-while-revalidate=30, stale-if-error=60, no-transform", header);
    }

    [Fact]
    public void BuildCacheControl_ShouldRenderPublicOnly_WhenNoDurations()
    {
        var header = CachePolicyResolver.BuildCacheControl(new CacheProfileOptions());

        Assert.Equal("public", header);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenOptionsAreNull()
    {
        Assert.Throws<ArgumentNullException>(() => new CachePolicyResolver(null!));
    }

    [Fact]
    public void Resolve_ShouldReturnRevalidate_WhenNoImmutablePrefixConfigured()
    {
        var resolver = new CachePolicyResolver(new CdnOriginOptions());

        var header = resolver.Resolve("/index.html");

        Assert.Equal(CachePolicyResolver.BuildCacheControl(new CdnOriginOptions().Cache.Revalidate), header);
    }

    [Fact]
    public void Resolve_ShouldReturnImmutable_WhenPathMatchesImmutablePrefix()
    {
        var options = new CdnOriginOptions();
        options.Cache.ImmutablePathPrefixes.Add("/assets/");

        var resolver = new CachePolicyResolver(options);

        Assert.Equal(CachePolicyResolver.BuildCacheControl(options.Cache.Immutable), resolver.Resolve("/assets/app.4f2c.js"));
        Assert.Equal(CachePolicyResolver.BuildCacheControl(options.Cache.Revalidate), resolver.Resolve("/pages/home.html"));
    }

    [Fact]
    public void Resolve_ShouldReturnImmutable_WhenPathMatchesImmutablePrefixWithDifferentCasing()
    {
        var options = new CdnOriginOptions();
        options.Cache.ImmutablePathPrefixes.Add("/assets/");

        var resolver = new CachePolicyResolver(options);

        Assert.Equal(CachePolicyResolver.BuildCacheControl(options.Cache.Immutable), resolver.Resolve("/ASSETS/app.4f2c.js"));
    }

    [Fact]
    public void Resolve_ShouldIgnoreWhitespacePrefixes()
    {
        var options = new CdnOriginOptions();
        options.Cache.ImmutablePathPrefixes.Add("   ");
        options.Cache.ImmutablePathPrefixes.Add("/img/");

        var resolver = new CachePolicyResolver(options);

        Assert.Equal(CachePolicyResolver.BuildCacheControl(options.Cache.Immutable), resolver.Resolve("/img/logo.png"));
    }

    [Fact]
    public void Resolve_ShouldReturnRevalidate_WhenPathHasNoValue()
    {
        var options = new CdnOriginOptions();
        options.Cache.ImmutablePathPrefixes.Add("/assets/");

        var resolver = new CachePolicyResolver(options);

        Assert.Equal(CachePolicyResolver.BuildCacheControl(options.Cache.Revalidate), resolver.Resolve(new PathString()));
    }
}
