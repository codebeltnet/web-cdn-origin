using System.IO.Compression;
using Codebelt.Cdn.Origin.Configuration;
using Codebelt.Extensions.Xunit;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Xunit;

namespace Codebelt.Cdn.Origin.Hosting;

public class CdnOriginServiceCollectionExtensionsTest : Test
{
    public CdnOriginServiceCollectionExtensionsTest(ITestOutputHelper output) : base(output)
    {
    }

    private static IConfiguration BuildConfiguration(IDictionary<string, string?> values)
    {
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    [Fact]
    public void AddCdnOrigin_ShouldThrow_WhenServicesAreNull()
    {
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddCdnOrigin(BuildConfiguration(new Dictionary<string, string?>())));
    }

    [Fact]
    public void AddCdnOrigin_ShouldThrow_WhenConfigurationIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddCdnOrigin(null!));
    }

    [Fact]
    public void AddCdnOrigin_ShouldRegisterServices()
    {
        var services = new ServiceCollection();

        services.AddCdnOrigin(BuildConfiguration(new Dictionary<string, string?>()));

        Assert.Contains(services, d => d.ServiceType == typeof(IValidateOptions<CdnOriginOptions>));
        Assert.Contains(services, d => d.ServiceType == typeof(CachePolicyResolver));
        Assert.Contains(services, d => d.ServiceType == typeof(ICorsService));
        Assert.Contains(services, d => d.ServiceType == typeof(IResponseCompressionProvider));
        Assert.Contains(services, d => d.ServiceType == typeof(HealthCheckService));
    }

    [Fact]
    public void AddCdnOrigin_ShouldBuildWildcardCorsPolicy_ForPublicMode()
    {
        var services = new ServiceCollection();
        services.AddCdnOrigin(BuildConfiguration(new Dictionary<string, string?>()));

        using var provider = services.BuildServiceProvider();
        var policy = provider.GetRequiredService<IOptions<CorsOptions>>().Value.GetPolicy(CorsPolicyOptions.PolicyName);

        Assert.NotNull(policy);
        Assert.True(policy!.AllowAnyOrigin);
        Assert.False(policy.SupportsCredentials);
        Assert.Empty(policy.ExposedHeaders);
        Assert.Contains("GET", policy.Methods);
    }

    [Fact]
    public void AddCdnOrigin_ShouldBuildRestrictedCorsPolicy_WithCredentialsAndExposedHeaders()
    {
        var services = new ServiceCollection();
        services.AddCdnOrigin(BuildConfiguration(new Dictionary<string, string?>
        {
            ["CdnOrigin:Cors:AllowedOrigins:0"] = "https://a.example",
            ["CdnOrigin:Cors:AllowedOrigins:1"] = "https://b.example",
            ["CdnOrigin:Cors:AllowCredentials"] = "true",
            ["CdnOrigin:Cors:ExposedHeaders:0"] = "X-Custom"
        }));

        using var provider = services.BuildServiceProvider();
        var policy = provider.GetRequiredService<IOptions<CorsOptions>>().Value.GetPolicy(CorsPolicyOptions.PolicyName);

        Assert.NotNull(policy);
        Assert.False(policy!.AllowAnyOrigin);
        Assert.Contains("https://a.example", policy.Origins);
        Assert.Contains("https://b.example", policy.Origins);
        Assert.True(policy.SupportsCredentials);
        Assert.Contains("X-Custom", policy.ExposedHeaders);
    }

    [Fact]
    public void AddCdnOrigin_ShouldConfigureCompression()
    {
        var services = new ServiceCollection();
        services.AddCdnOrigin(BuildConfiguration(new Dictionary<string, string?>
        {
            ["CdnOrigin:Compression:AdditionalMimeTypes:0"] = "application/x-custom"
        }));

        using var provider = services.BuildServiceProvider();
        var compression = provider.GetRequiredService<IOptions<ResponseCompressionOptions>>().Value;

        Assert.True(compression.EnableForHttps);
        Assert.Contains("application/json", compression.MimeTypes);
        Assert.Contains("application/x-custom", compression.MimeTypes);
        Assert.Equal(CompressionLevel.Fastest, provider.GetRequiredService<IOptions<BrotliCompressionProviderOptions>>().Value.Level);
        Assert.Equal(CompressionLevel.Fastest, provider.GetRequiredService<IOptions<GzipCompressionProviderOptions>>().Value.Level);
    }
}
