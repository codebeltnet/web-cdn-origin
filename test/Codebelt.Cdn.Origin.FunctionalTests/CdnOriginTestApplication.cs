using Codebelt.Extensions.Xunit;
using Codebelt.Extensions.Xunit.Hosting;
using Codebelt.Extensions.Xunit.Hosting.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Codebelt.Cdn.Origin;

/// <summary>
/// Hosts the real Static Content Provider pipeline over an isolated temporary content directory using
/// <see cref="WebApplicationFactory{TEntryPoint}"/>.
/// </summary>
public sealed class CdnOriginTestApplication : Test
{
    private readonly IHostTest _hostTest;

    public CdnOriginTestApplication(ITestOutputHelper output, IDictionary<string, string> settings = null) : base(output)
    {
        Content = new TempContent();
        var defaultSettings = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["CdnOrigin:ContentRoot"] = Content.Root
        };

        if (settings is not null)
        {
            foreach (var setting in settings)
            {
                defaultSettings[setting.Key] = setting.Value;
            }
        }

        _hostTest = WebApplicationTestFactory.Create<Program>(builder =>
        {
            builder.UseEnvironment(Environments.Production);
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(defaultSettings));
        }, new ManagedWebApplicationFixture<Program>());
    }

    public HttpClient CreateClient()
    {
        return _hostTest.Host.GetTestClient();
    }

    public TempContent Content { get; }

    protected override void OnDisposeManagedResources()
    {
        _hostTest?.Dispose();
        Content.Dispose();
        base.OnDisposeManagedResources();
    }

    protected override async ValueTask OnDisposeManagedResourcesAsync()
    {
        if (_hostTest is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync().ConfigureAwait(false);
        }
        else
        {
            _hostTest?.Dispose();
        }

        Content.Dispose();
        await base.OnDisposeManagedResourcesAsync().ConfigureAwait(false);
    }
}
