using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Abstractions;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    internal sealed class DefaultCredentialResolver : ICredentialResolver
    {
        private readonly IPrimaryCredentialResolver _primaryResolver;

        public DefaultCredentialResolver(IPrimaryCredentialResolver primaryResolver)
        {
            _primaryResolver = primaryResolver;
        }

        public ResolvedCredential? Resolve(HttpContext context, EffectiveAuthResponse response)
        {
            var kind = _primaryResolver.Resolve(context);

            return kind switch
            {
                PrimaryCredentialKind.Stateful => ResolveSession(context, response),
                PrimaryCredentialKind.Stateless => ResolveAccessToken(context, response),

                _ => null
            };
        }

        private static ResolvedCredential? ResolveSession(HttpContext context, EffectiveAuthResponse response)
        {
            var delivery = response.SessionIdDelivery;

            if (delivery.Mode != TokenResponseMode.Cookie)
                return null;

            var cookie = delivery.Cookie;
            if (cookie is null)
                return null;

            if (!context.Request.Cookies.TryGetValue(cookie.Name, out var raw))
                return null;

            if (string.IsNullOrWhiteSpace(raw))
                return null;

            return new ResolvedCredential
            {
                Kind = PrimaryCredentialKind.Stateful,
                Value = raw.Trim(),
                TenantId = context.GetTenantContext().TenantId,
                Device = context.GetDevice()
            };
        }

        private static ResolvedCredential? ResolveAccessToken(HttpContext context, EffectiveAuthResponse response)
        {
            var delivery = response.AccessTokenDelivery;

            if (delivery.Mode != TokenResponseMode.Header)
                return null;

            var headerName = delivery.Name ?? "Authorization";

            if (!context.Request.Headers.TryGetValue(headerName, out var header))
                return null;

            var value = header.ToString();

            if (delivery.HeaderFormat == HeaderTokenFormat.Bearer &&
                value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
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
