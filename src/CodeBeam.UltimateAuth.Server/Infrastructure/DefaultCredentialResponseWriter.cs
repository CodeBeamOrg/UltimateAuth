using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Abstractions;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Cookies;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server;

internal sealed class DefaultCredentialResponseWriter : ICredentialResponseWriter
{
    private readonly IAuthFlowContextAccessor _authContext;
    private readonly IUAuthCookieManager _cookieManager;
    private readonly IUAuthCookiePolicyBuilder _cookiePolicy;
    private readonly IUAuthHeaderPolicyBuilder _headerPolicy;

    public DefaultCredentialResponseWriter(
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

    public void Write(HttpContext context, CredentialKind kind, string value)
    {
        var auth = _authContext.Current;
        var delivery = ResolveDelivery(auth.Response, kind);


        if (delivery.Mode == TokenResponseMode.None)
            return;

        switch (delivery.Mode)
        {
            case TokenResponseMode.Cookie:
                WriteCookie(context, kind, value, delivery, auth);
                break;

            case TokenResponseMode.Header:
                WriteHeader(context, value, delivery, auth);
                break;

            case TokenResponseMode.Body:
                // TODO: Implement body writing if needed
                break;
        }
    }

    private void WriteCookie(HttpContext context, CredentialKind kind, string value, CredentialResponseOptions options, AuthFlowContext auth)
    {
        if (options.Cookie is null)
            throw new InvalidOperationException($"Cookie options missing for credential '{kind}'.");

        var logicalLifetime = ResolveLogicalLifetime(auth, kind);
        var cookieOptions = _cookiePolicy.Build(options, auth, logicalLifetime);

        _cookieManager.Write(context, options.Cookie.Name, value, cookieOptions);
    }

    private void WriteHeader(HttpContext context, string value, CredentialResponseOptions response, AuthFlowContext auth)
    {
        var headerName = response.Name ?? "Authorization";
        var formatted = _headerPolicy.BuildHeaderValue(value, response, auth);

        context.Response.Headers[headerName] = formatted;
    }

    private static CredentialResponseOptions ResolveDelivery(EffectiveAuthResponse response, CredentialKind kind)
        => kind switch
        {
            CredentialKind.Session => response.SessionIdDelivery,
            CredentialKind.AccessToken => response.AccessTokenDelivery,
            CredentialKind.RefreshToken => response.RefreshTokenDelivery,
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };

    private static TimeSpan? ResolveLogicalLifetime(AuthFlowContext auth, CredentialKind kind)
    {
        // TODO: Move this method to policy on implementing
        return kind switch
        {
            CredentialKind.Session
                => auth.EffectiveOptions.Options.Session.IdleTimeout + auth.OriginalOptions.Cookie.Session.Lifetime.IdleBuffer,

            CredentialKind.RefreshToken
                => auth.EffectiveOptions.Options.Tokens.RefreshTokenLifetime + auth.OriginalOptions.Cookie.RefreshToken.Lifetime.IdleBuffer,

            CredentialKind.AccessToken
                => auth.EffectiveOptions.Options.Tokens.AccessTokenLifetime + auth.OriginalOptions.Cookie.AccessToken.Lifetime.IdleBuffer,

            _ => null
        };
    }
}
