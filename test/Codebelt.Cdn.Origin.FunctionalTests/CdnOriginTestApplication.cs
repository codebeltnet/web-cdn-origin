using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Codebelt.Cdn.Origin;

/// <summary>
/// Hosts the real Static Content Provider pipeline over an isolated temporary content directory using
/// <see cref="WebApplicationFactory{TEntryPoint}"/>.
/// </summary>
public sealed class CdnOriginTestApplication : WebApplicationFactory<Program>
{
    private readonly Dictionary<string, string?> _settings;

    public CdnOriginTestApplication(IDictionary<string, string?>? settings = null)
    {
        Content = new TempContent();
        _settings = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["CdnOrigin:ContentRoot"] = Content.Root
        };

        if (settings is not null)
        {
            foreach (var setting in settings)
            {
                _settings[setting.Key] = setting.Value;
            }
        }
    }

    public TempContent Content { get; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Production);
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(_settings));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            Content.Dispose();
        }
    }
}
