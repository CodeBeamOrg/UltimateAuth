using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Abstractions;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Contracts;
using CodeBeam.UltimateAuth.Server.Extensions;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

internal sealed class ValidateCredentialResolver : IValidateCredentialResolver
{
    private readonly IPrimaryCredentialResolver _primaryResolver;

    public ValidateCredentialResolver(IPrimaryCredentialResolver primaryResolver)
    {
        _primaryResolver = primaryResolver;
    }

    public async Task<ResolvedCredential?> ResolveAsync(HttpContext context, EffectiveAuthResponse response)
    {
        var kind = _primaryResolver.Resolve(context);

        return kind switch
        {
            PrimaryGrantKind.Stateful => await ResolveSession(context, response),
            PrimaryGrantKind.Stateless => await ResolveAccessToken(context, response),

            _ => null
        };
    }

    private static async Task<ResolvedCredential?> ResolveSession(HttpContext context, EffectiveAuthResponse response)
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
            Kind = PrimaryGrantKind.Stateful,
            Value = raw.Trim(),
            Tenant = context.GetTenant(),
            Device = await context.GetDeviceAsync()
        };
    }

    private static async Task<ResolvedCredential?> ResolveAccessToken(HttpContext context, EffectiveAuthResponse response)
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
            Kind = PrimaryGrantKind.Stateless,
            Value = value,
            Tenant = context.GetTenant(),
            Device = await context.GetDeviceAsync()
        };
    }
}
