using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Extensions;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

internal sealed class TransportCredentialResolver : ITransportCredentialResolver
{
    public async ValueTask<TransportCredential?> ResolveAsync(HttpContext context, UAuthMode mode, EffectiveAuthResponse response)
    {
        return mode switch
        {
            UAuthMode.PureJwt or UAuthMode.SemiHybrid =>
                await ResolveDeliveryAsync(
                    context,
                    response.AccessTokenDelivery,
                    TransportCredentialKind.AccessToken),

            UAuthMode.PureOpaque or UAuthMode.Hybrid =>
                await ResolveDeliveryAsync(
                    context,
                    response.SessionIdDelivery,
                    TransportCredentialKind.Session),

            _ => null
        };
    }

    private static async ValueTask<TransportCredential?> ResolveDeliveryAsync(HttpContext context, EffectiveGrantDelivery delivery, TransportCredentialKind kind)
    {
        string? value = delivery.Mode switch
        {
            GrantDeliveryMode.Cookie =>
                ReadCookie(context, delivery.Cookie?.Name),

            GrantDeliveryMode.Header
                when kind == TransportCredentialKind.AccessToken =>
                ReadHeader(
                    context,
                    "Authorization",
                    HeaderTokenFormat.Bearer),

            GrantDeliveryMode.Header =>
                ReadHeader(
                    context,
                    delivery.Name,
                    delivery.HeaderFormat),

            _ => null
        };

        if (string.IsNullOrWhiteSpace(value))
            return null;

        return new TransportCredential
        {
            Kind = kind,
            Value = value,
            TenantId = context.GetTenant().Value,
            Device = await context.GetDeviceAsync()
        };
    }

    private static string? ReadCookie(
        HttpContext context,
        string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        return context.Request.Cookies.TryGetValue(name, out var value)
            && !string.IsNullOrWhiteSpace(value)
                ? value.Trim()
                : null;
    }

    private static string? ReadHeader(
        HttpContext context,
        string? name,
        HeaderTokenFormat format)
    {
        if (string.IsNullOrWhiteSpace(name) ||
            !context.Request.Headers.TryGetValue(name, out var header))
        {
            return null;
        }

        var value = header.ToString().Trim();

        if (format == HeaderTokenFormat.Bearer)
        {
            const string prefix = "Bearer ";

            if (!value.StartsWith(
                    prefix,
                    StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            value = value[prefix.Length..].Trim();
        }
        else if (format != HeaderTokenFormat.Raw)
        {
            return null;
        }

        return string.IsNullOrWhiteSpace(value)
            ? null
            : value;
    }
}
