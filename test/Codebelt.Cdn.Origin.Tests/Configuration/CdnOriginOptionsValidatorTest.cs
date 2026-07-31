using Codebelt.Extensions.Xunit;
using Xunit;

namespace Codebelt.Cdn.Origin.Configuration;

public class CdnOriginOptionsValidatorTest : Test
{
    private readonly CdnOriginOptionsValidator _sut = new();

    public CdnOriginOptionsValidatorTest(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void Validate_ShouldThrow_WhenOptionsAreNull()
    {
        Assert.Throws<ArgumentNullException>(() => _sut.Validate(null, null!));
    }

    [Fact]
    public void Validate_ShouldSucceed_ForDefaultOptions()
    {
        var result = _sut.Validate(null, new CdnOriginOptions());

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ShouldFail_WhenContentRootIsBlank(string? contentRoot)
    {
        var options = new CdnOriginOptions { ContentRoot = contentRoot! };

        var result = _sut.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("ContentRoot", result.FailureMessage);
    }

    [Fact]
    public void Validate_ShouldFail_WhenDefaultDocumentIsBlank()
    {
        var options = new CdnOriginOptions { DefaultDocuments = ["index.html", "  "] };

        var result = _sut.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("DefaultDocuments", result.FailureMessage);
    }

    [Fact]
    public void Validate_ShouldIgnoreCors_WhenDisabled()
    {
        var options = new CdnOriginOptions();
        options.Cors.Enabled = false;
        options.Cors.AllowCredentials = true;
        options.Cors.AllowedOrigins = [CorsPolicyOptions.AnyOrigin];

        var result = _sut.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_ShouldSucceed_WithSpecificOrigins()
    {
        var options = new CdnOriginOptions();
        options.Cors.AllowedOrigins = ["https://example.com"];

        var result = _sut.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_ShouldFail_WhenWildcardCombinedWithCredentials()
    {
        var options = new CdnOriginOptions();
        options.Cors.AllowedOrigins = [CorsPolicyOptions.AnyOrigin];
        options.Cors.AllowCredentials = true;

        var result = _sut.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("AllowCredentials", result.FailureMessage);
    }

    [Fact]
    public void Validate_ShouldFail_WhenWildcardCombinedWithExplicitOrigins()
    {
        var options = new CdnOriginOptions();
        options.Cors.AllowedOrigins = [CorsPolicyOptions.AnyOrigin, "https://example.com"];

        var result = _sut.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("explicit origins", result.FailureMessage);
    }

    [Fact]
    public void Validate_ShouldFail_WhenImmutablePrefixDoesNotStartWithSlash()
    {
        var options = new CdnOriginOptions();
        options.Cache.ImmutablePathPrefixes.Add("assets/");

        var result = _sut.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("ImmutablePathPrefixes", result.FailureMessage);
    }

    [Fact]
    public void Validate_ShouldFail_WhenImmutablePrefixIsBlank()
    {
        var options = new CdnOriginOptions();
        options.Cache.ImmutablePathPrefixes.Add("   ");

        var result = _sut.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("ImmutablePathPrefixes", result.FailureMessage);
    }

    [Fact]
    public void Validate_ShouldFail_WhenNoStoreCombinedWithSharedMaxAgeOnly()
    {
        var options = new CdnOriginOptions();
        options.Cache.Revalidate.NoStore = true;
        options.Cache.Revalidate.MaxAge = null;
        options.Cache.Revalidate.SharedMaxAge = TimeSpan.FromHours(1);

        var result = _sut.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("no-store", result.FailureMessage);
    }

    [Fact]
    public void Validate_ShouldSucceed_WhenNoStoreWithoutDurations()
    {
        var options = new CdnOriginOptions();
        options.Cache.Revalidate.NoStore = true;
        options.Cache.Revalidate.MaxAge = null;
        options.Cache.Revalidate.SharedMaxAge = null;
        options.Cache.Revalidate.MustRevalidate = false;

        var result = _sut.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_ShouldFail_WhenNoStoreCombinedWithMaxAge()
    {
        var options = new CdnOriginOptions();
        options.Cache.Revalidate.NoStore = true;

        var result = _sut.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("no-store", result.FailureMessage);
    }

    [Fact]
    public void Validate_ShouldFail_WhenImmutableCombinedWithMustRevalidate()
    {
        var options = new CdnOriginOptions();
        options.Cache.Immutable.MustRevalidate = true;

        var result = _sut.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("must-revalidate", result.FailureMessage);
    }

    [Fact]
    public void Validate_ShouldFail_WhenImmutableCombinedWithNoCache()
    {
        var options = new CdnOriginOptions();
        options.Cache.Immutable.NoCache = true;

        var result = _sut.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("no-cache", result.FailureMessage);
    }

    [Fact]
    public void Validate_ShouldFail_WhenMaxAgeIsNegative()
    {
        var options = new CdnOriginOptions();
        options.Cache.Revalidate.MaxAge = TimeSpan.FromSeconds(-1);

        var result = _sut.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("MaxAge", result.FailureMessage);
    }

    [Fact]
    public void Validate_ShouldFail_WhenSharedMaxAgeIsNegative()
    {
        var options = new CdnOriginOptions();
        options.Cache.Revalidate.SharedMaxAge = TimeSpan.FromSeconds(-1);

        var result = _sut.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("SharedMaxAge", result.FailureMessage);
    }

    [Fact]
    public void Validate_ShouldFail_WhenStaleWhileRevalidateIsNegative()
    {
        var options = new CdnOriginOptions();
        options.Cache.Revalidate.StaleWhileRevalidate = TimeSpan.FromSeconds(-1);

        var result = _sut.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("StaleWhileRevalidate", result.FailureMessage);
    }

    [Fact]
    public void Validate_ShouldFail_WhenStaleIfErrorIsNegative()
    {
        var options = new CdnOriginOptions();
        options.Cache.Revalidate.StaleIfError = TimeSpan.FromSeconds(-1);

        var result = _sut.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("StaleIfError", result.FailureMessage);
    }

    [Fact]
    public void Validate_ShouldFail_WhenServeUnknownFileTypesWithoutDefaultContentType()
    {
        var options = new CdnOriginOptions();
        options.ContentTypes.ServeUnknownFileTypes = true;

        var result = _sut.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("DefaultContentType", result.FailureMessage);
    }

    [Fact]
    public void Validate_ShouldSucceed_WhenServeUnknownFileTypesWithDefaultContentType()
    {
        var options = new CdnOriginOptions();
        options.ContentTypes.ServeUnknownFileTypes = true;
        options.ContentTypes.DefaultContentType = "application/octet-stream";

        var result = _sut.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_ShouldFail_WhenMappingKeyHasNoLeadingDot()
    {
        var options = new CdnOriginOptions();
        options.ContentTypes.Mappings["foo"] = "application/x-foo";

        var result = _sut.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("must start with '.'", result.FailureMessage);
    }

    [Fact]
    public void Validate_ShouldFail_WhenMappingKeyIsBlank()
    {
        var options = new CdnOriginOptions();
        options.ContentTypes.Mappings["   "] = "application/x-foo";

        var result = _sut.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("must start with '.'", result.FailureMessage);
    }

    [Fact]
    public void Validate_ShouldFail_WhenMappingValueIsBlank()
    {
        var options = new CdnOriginOptions();
        options.ContentTypes.Mappings[".foo"] = "  ";

        var result = _sut.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("MIME value", result.FailureMessage);
    }

    [Fact]
    public void Validate_ShouldIgnoreHealth_WhenDisabled()
    {
        var options = new CdnOriginOptions();
        options.Health.Enabled = false;
        options.Health.LivePath = "live";
        options.Health.ReadyPath = "live";

        var result = _sut.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_ShouldFail_WhenLivePathDoesNotStartWithSlash()
    {
        var options = new CdnOriginOptions();
        options.Health.LivePath = "live";

        var result = _sut.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("LivePath", result.FailureMessage);
    }

    [Fact]
    public void Validate_ShouldFail_WhenLivePathIsBlank()
    {
        var options = new CdnOriginOptions();
        options.Health.LivePath = "   ";

        var result = _sut.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("LivePath", result.FailureMessage);
    }

    [Fact]
    public void Validate_ShouldFail_WhenReadyPathDoesNotStartWithSlash()
    {
        var options = new CdnOriginOptions();
        options.Health.ReadyPath = "ready";

        var result = _sut.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("ReadyPath", result.FailureMessage);
    }

    [Fact]
    public void Validate_ShouldFail_WhenReadyPathIsBlank()
    {
        var options = new CdnOriginOptions();
        options.Health.ReadyPath = "   ";

        var result = _sut.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("ReadyPath", result.FailureMessage);
    }

    [Fact]
    public void Validate_ShouldFail_WhenLiveAndReadyPathsAreEqual()
    {
        var options = new CdnOriginOptions();
        options.Health.LivePath = "/health";
        options.Health.ReadyPath = "/health";

        var result = _sut.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("must differ", result.FailureMessage);
    }
}
