using Codebelt.Extensions.Xunit;
using Codebelt.Extensions.Xunit.Hosting.AspNetCore;
using Xunit;

namespace Codebelt.Cdn.Origin;

public class StartupValidationTest : Test
{
    public StartupValidationTest(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async Task Startup_ShouldFail_WhenContentRootDoesNotExist()
    {
        var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        await using var application = new CdnOriginTestApplication(TestOutput, new Dictionary<string, string?>
        {
            ["CdnOrigin:ContentRoot"] = missing
        });


        var exception = Assert.ThrowsAny<Exception>(() => application.CreateClient());

        Assert.Contains("content root", FlattenMessages(exception), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Startup_ShouldFail_WhenWildcardCombinedWithCredentials()
    {
        await using var application = new CdnOriginTestApplication(TestOutput, new Dictionary<string, string?>
        {
            ["CdnOrigin:Cors:AllowedOrigins:0"] = "*",
            ["CdnOrigin:Cors:AllowCredentials"] = "true"
        });

        Assert.ThrowsAny<Exception>(() => application.CreateClient());
    }

    private static string FlattenMessages(Exception exception)
    {
        var messages = new List<string>();
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            messages.Add(current.Message);
        }

        return string.Join(" | ", messages);
    }
}
