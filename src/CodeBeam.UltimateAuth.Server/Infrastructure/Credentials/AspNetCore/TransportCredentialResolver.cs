using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

internal sealed class TransportCredentialResolver : ITransportCredentialResolver
{
    private readonly IOptionsMonitor<UAuthServerOptions> _server;

    public TransportCredentialResolver(IOptionsMonitor<UAuthServerOptions> server)
    {
        _server = server;
    }

    public async ValueTask<TransportCredential?> ResolveAsync(HttpContext context)
    {
        var cookies = _server.CurrentValue.Cookie;

        return await TryFromAuthorizationHeaderAsync(context)
            ?? await TryFromCookiesAsync(context, cookies)
            ?? await TryFromQueryAsync(context)
            ?? await TryFromBodyAsync(context)
            ?? await TryFromHubAsync(context);
    }

    // TODO: Make scheme configurable, shouldn't be hard coded
    private static async ValueTask<TransportCredential?> TryFromAuthorizationHeaderAsync(HttpContext ctx)
    {
        if (!ctx.Request.Headers.TryGetValue("Authorization", out var header))
            return null;

        var value = header.ToString();
        if (!value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return null;

        var token = value["Bearer ".Length..].Trim();
        if (string.IsNullOrWhiteSpace(token))
            return null;

        return new TransportCredential
        {
            Kind = TransportCredentialKind.AccessToken,
            Value = token,
            TenantId = ctx.GetTenant().Value,
            Device = await ctx.GetDeviceAsync()
        };
    }

    private static async ValueTask<TransportCredential?> TryFromCookiesAsync(HttpContext ctx, UAuthCookiePolicyOptions cookieSet)
    {
        if (TryReadCookie(ctx, cookieSet.Session.Name, out var session))
            return await BuildAsync(ctx, TransportCredentialKind.Session, session);

        if (TryReadCookie(ctx, cookieSet.RefreshToken.Name, out var refresh))
            return await BuildAsync(ctx, TransportCredentialKind.RefreshToken, refresh);

        if (TryReadCookie(ctx, cookieSet.AccessToken.Name, out var access))
            return await BuildAsync(ctx, TransportCredentialKind.AccessToken, access);

        return null;
    }

    private static async ValueTask<TransportCredential?> TryFromQueryAsync(HttpContext ctx)
    {
        if (!ctx.Request.Query.TryGetValue("access_token", out var token))
            return null;

        var value = token.ToString();
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return new TransportCredential
        {
            Kind = TransportCredentialKind.AccessToken,
            Value = value,
            TenantId = ctx.GetTenant().Value,
            Device = await ctx.GetDeviceAsync()
        };
    }

    private static ValueTask<TransportCredential?> TryFromBodyAsync(HttpContext ctx)
    {
        // intentionally empty for now
        // body parsing is expensive and opt-in later

        return ValueTask.FromResult<TransportCredential?>(null);
    }

    private static ValueTask<TransportCredential?> TryFromHubAsync(HttpContext ctx)
    {
        // UAuthHub detection can live here later

        return ValueTask.FromResult<TransportCredential?>(null);
    }

    private static bool TryReadCookie(HttpContext ctx, string name, out string value)
    {
        value = string.Empty;

        if (string.IsNullOrWhiteSpace(name))
            return false;

        if (!ctx.Request.Cookies.TryGetValue(name, out var raw))
            return false;

        raw = raw?.Trim();
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        value = raw;
        return true;
    }

    private static async Task<TransportCredential> BuildAsync(HttpContext ctx, TransportCredentialKind kind, string value)
        => new()
        {
            Kind = kind,
            Value = value,
            TenantId = ctx.GetTenant().Value,
            Device = await ctx.GetDeviceAsync()
        };
}
