using Codebelt.Bootstrapper.Web;
using Codebelt.Cdn.Origin.Hosting;

namespace Codebelt.Cdn.Origin;

/// <summary>
/// The entry point of the Static Content Provider.
/// </summary>
public class Program : MinimalWebProgram
{
    /// <summary>
    /// The application entry point.
    /// </summary>
    /// <param name="args">The command line arguments.</param>
    /// <returns>A <see cref="Task"/> that represents the running application.</returns>
    public static Task Main(string[] args)
    {
        var builder = CreateHostBuilder(args);

        builder.Services.AddCdnOrigin(builder.Configuration);

        var app = builder.Build();

        app.UseCdnOrigin();

        return app.RunAsync();
    }
}
