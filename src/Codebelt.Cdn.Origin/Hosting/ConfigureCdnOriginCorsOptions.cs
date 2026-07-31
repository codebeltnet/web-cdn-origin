using Codebelt.Cdn.Origin.Configuration;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Options;

namespace Codebelt.Cdn.Origin.Hosting;

/// <summary>
/// Configures the named CORS policy for the Static Content Provider from the bound <see cref="CdnOriginOptions"/>.
/// </summary>
/// <seealso cref="IConfigureOptions{TOptions}" />
internal sealed class ConfigureCdnOriginCorsOptions(IOptions<CdnOriginOptions> options) : IConfigureOptions<CorsOptions>
{
    public void Configure(CorsOptions corsOptions)
    {
        CorsPolicyOptions cors = options.Value.Cors;

        corsOptions.AddPolicy(CorsPolicyOptions.PolicyName, policy =>
        {
            bool wildcard = cors.AllowedOrigins.Count == 0 || cors.AllowedOrigins.Contains(CorsPolicyOptions.AnyOrigin);

            if (wildcard)
            {
                policy.AllowAnyOrigin();
            }
            else
            {
                policy.WithOrigins([.. cors.AllowedOrigins]);
            }

            policy.WithMethods(HttpMethods.Get, HttpMethods.Head, HttpMethods.Options);
            policy.AllowAnyHeader();

            if (cors.ExposedHeaders.Count > 0)
            {
                policy.WithExposedHeaders([.. cors.ExposedHeaders]);
            }

            if (cors.AllowCredentials && !wildcard)
            {
                policy.AllowCredentials();
            }
        });
    }
}
