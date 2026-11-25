using CodeBeam.UltimateAuth.Server.Infrastructure;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Options;

internal sealed class UAuthHubCorsOptionsSetup(IOptions<UAuthServerOptions> serverOptions) : IConfigureOptions<CorsOptions>
{
    public void Configure(CorsOptions options)
    {
        var server = serverOptions.Value;

        var origins = server.Hub.AllowedClientOrigins
            .Select(OriginHelper.Normalize)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        options.AddPolicy("UAuthHub", policy =>
        {
            if (origins.Length == 0)
                return;

            var accessHeader = server.GrantDelivery.AccessToken.HeaderName ?? "uat";

            var exposedHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "X-UAuth-Refresh",
                "X-UAuth-Access-Header",
                "X-UAuth-Access-Expires",
                "X-UAuth-Access-Format",
                "X-UAuth-Access-HeaderFormat",
                "Retry-After",
                accessHeader
            };

            policy
                .WithOrigins(origins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials()
                .WithExposedHeaders(exposedHeaders.ToArray());
        });
    }
}
