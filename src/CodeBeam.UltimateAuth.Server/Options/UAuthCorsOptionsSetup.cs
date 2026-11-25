using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Options;

internal sealed class UAuthResourceCorsOptionsSetup(IOptions<UAuthResourceApiOptions> resourceOptions) : IConfigureOptions<CorsOptions>
{
    public void Configure(CorsOptions options)
    {
        var cors = resourceOptions.Value.Cors;

        if (cors.AllowedOrigins.Count == 0)
            return;

        options.AddPolicy(cors.PolicyName, policy =>
        {
            policy
                .WithOrigins(cors.AllowedOrigins.ToArray())
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    }
}
