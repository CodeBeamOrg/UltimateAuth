using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Abstractions;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    // TODO: Enhance class (endpoint-based, tenant-based, route-based)
    internal sealed class DefaultPrimaryCredentialResolver : IPrimaryCredentialResolver
    {
        private readonly UAuthServerOptions _options;

        public DefaultPrimaryCredentialResolver(IOptions<UAuthServerOptions> options)
        {
            _options = options.Value;
        }

        public PrimaryCredentialKind Resolve(HttpContext context)
        {
            if (IsApiRequest(context))
                return _options.PrimaryCredential.Api;

            return _options.PrimaryCredential.Ui;
        }

        private static bool IsApiRequest(HttpContext context)
        {
            if (context.Request.Path.StartsWithSegments("/api"))
                return true;

            if (context.Request.Headers.ContainsKey("Authorization"))
                return true;

            return false;
        }
    }

}
