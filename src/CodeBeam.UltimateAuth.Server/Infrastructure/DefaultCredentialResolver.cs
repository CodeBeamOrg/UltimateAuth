using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Abstractions;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    internal sealed class DefaultCredentialResolver : ICredentialResolver
    {
        private readonly IPrimaryCredentialResolver _primaryResolver;
        private readonly UAuthServerOptions _options;

        public DefaultCredentialResolver(
            IPrimaryCredentialResolver primaryResolver,
            IOptions<UAuthServerOptions> options)
        {
            _primaryResolver = primaryResolver;
            _options = options.Value;
        }

        public ResolvedCredential? Resolve(HttpContext context)
        {
            var primary = _primaryResolver.Resolve(context);

            return primary switch
            {
                PrimaryCredentialKind.Stateful => ResolveSession(context),
                PrimaryCredentialKind.Stateless => ResolveToken(context),
                _ => null
            };
        }

        private ResolvedCredential? ResolveSession(HttpContext context)
        {
            if (!context.Request.Cookies.TryGetValue(
                _options.Cookie.Name,
                out var sessionId))
            {
                return null;
            }

            return new ResolvedCredential
            {
                Kind = PrimaryCredentialKind.Stateful,
                Value = sessionId,
                TenantId = context.GetTenantContext().TenantId,
                Device = context.GetDevice()
            };
        }

        private ResolvedCredential? ResolveToken(HttpContext context)
        {
            if (!context.Request.Headers.TryGetValue("Authorization", out var header))
                return null;

            var value = header.ToString();

            if (value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                value = value["Bearer ".Length..].Trim();
            }

            if (string.IsNullOrWhiteSpace(value))
                return null;

            return new ResolvedCredential
            {
                Kind = PrimaryCredentialKind.Stateless,
                Value = value,
                TenantId = context.GetTenantContext().TenantId,
                Device = context.GetDevice()
            };
        }
    }
}
