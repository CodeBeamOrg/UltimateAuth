using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Contracts;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using Microsoft.AspNetCore.Http;

internal sealed class ValidateCredentialResolver : IValidateCredentialResolver
{
    public Task<ResolvedCredential?> ResolveAsync(HttpContext context, EffectiveAuthResponse response)
    {
        return ResolveSession(context, response);
    }

    private static async Task<ResolvedCredential?> ResolveSession(HttpContext context, EffectiveAuthResponse response)
    {
        var delivery = response.SessionIdDelivery;

        if (delivery.Mode != GrantDeliveryMode.Cookie)
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
            Kind = PrimaryTokenKind.Session,
            Value = raw.Trim(),
            Tenant = context.GetTenant(),
            Device = await context.GetDeviceAsync()
        };
    }
}
