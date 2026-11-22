using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Abstractions;
using CodeBeam.UltimateAuth.Server.Auth;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

internal sealed class CredentialResponseWriter : ICredentialResponseWriter
{
    private readonly IAuthFlowContextAccessor _authContext;
    private readonly IUAuthCookieManager _cookieManager;
    private readonly IUAuthCookiePolicyBuilder _cookiePolicy;
    private readonly IUAuthHeaderPolicyBuilder _headerPolicy;

    public CredentialResponseWriter(
        IAuthFlowContextAccessor authContext,
        IUAuthCookieManager cookieManager,
        IUAuthCookiePolicyBuilder cookiePolicy,
        IUAuthHeaderPolicyBuilder headerPolicy)
    {
        _authContext = authContext;
        _cookieManager = cookieManager;
        _cookiePolicy = cookiePolicy;
        _headerPolicy = headerPolicy;
    }

    public void Write(HttpContext context, GrantKind kind, AuthSessionId sessionId)
        => WriteInternal(context, kind, sessionId.ToString());

    public void Write(HttpContext context, GrantKind kind, AccessToken token)
    {
        ArgumentNullException.ThrowIfNull(token);

        WriteInternal(context, kind, token.Token);

        if (kind != GrantKind.AccessToken)
            return;

        var auth = _authContext.Current;
        var delivery = auth.Response.AccessTokenDelivery;

        if (delivery.Mode != GrantDeliveryMode.Header)
            return;

        var headerName = delivery.Name;

        if (string.IsNullOrWhiteSpace(headerName))
            throw new InvalidOperationException("Access token header name is missing.");

        var headers = context.Response.Headers;

        headers["X-UAuth-Access-Header"] = headerName;
        headers["X-UAuth-Access-Expires"] = token.ExpiresAt.ToUniversalTime().ToString("O");
        headers["X-UAuth-Access-Format"] = token.Format.ToString().ToLowerInvariant();
        headers["X-UAuth-Access-HeaderFormat"] = delivery.HeaderFormat.ToString().ToLowerInvariant();

        headers.CacheControl = "no-store";
    }

    public void Write(HttpContext context, GrantKind kind, RefreshTokenInfo token)
        => WriteInternal(context, kind, token.Token);

    public void WriteInternal(HttpContext context, GrantKind kind, string value)
    {
        var auth = _authContext.Current;
        var delivery = ResolveDelivery(auth.Response, kind);

        if (delivery.Kind != kind)
        {
            throw new InvalidOperationException(
                $"Resolved grant kind '{delivery.Kind}' does not match '{kind}'.");
        }

        switch (delivery.Mode)
        {
            case GrantDeliveryMode.None:
                return;

            case GrantDeliveryMode.Cookie:
                WriteCookie(context, kind, value, delivery, auth);
                return;

            case GrantDeliveryMode.Header:
                WriteHeader(context, value, delivery, auth);
                return;

            case GrantDeliveryMode.Body:
                throw new NotSupportedException(
                    $"Body grant delivery is not supported for '{kind}'.");

            default:
                throw new InvalidOperationException(
                    $"Unsupported grant delivery mode '{delivery.Mode}'.");
        }
    }

    private void WriteCookie(
        HttpContext context,
        GrantKind kind,
        string value,
        EffectiveGrantDelivery delivery,
        AuthFlowContext auth)
    {
        var cookie = delivery.Cookie
            ?? throw new InvalidOperationException(
                $"Cookie options missing for grant '{kind}'.");

        var cookieOptions = _cookiePolicy.Build(delivery, auth, kind);

        _cookieManager.Write(
            context,
            cookie.Name,
            value,
            cookieOptions);
    }

    private void WriteHeader(
        HttpContext context,
        string value,
        EffectiveGrantDelivery delivery,
        AuthFlowContext auth)
    {
        var headerName = delivery.Name;

        if (string.IsNullOrWhiteSpace(headerName))
        {
            throw new InvalidOperationException(
                $"Header name missing for grant '{delivery.Kind}'.");
        }

        var formatted = _headerPolicy.BuildHeaderValue(
            value,
            delivery,
            auth);

        context.Response.Headers[headerName] = formatted;
    }

    private static EffectiveGrantDelivery ResolveDelivery(
        EffectiveAuthResponse response,
        GrantKind kind)
        => kind switch
        {
            GrantKind.Session => response.SessionIdDelivery,
            GrantKind.AccessToken => response.AccessTokenDelivery,
            GrantKind.RefreshToken => response.RefreshTokenDelivery,
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
}