using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Abstractions;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Options;
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
        => WriteInternal(context, kind, token.Token);

    public void Write(HttpContext context, GrantKind kind, RefreshToken token)
        => WriteInternal(context, kind, token.Token);

    public void WriteInternal(HttpContext context, GrantKind kind, string value)
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

    private void WriteCookie(HttpContext context, GrantKind kind, string value, CredentialResponseOptions options, AuthFlowContext auth)
    {
        if (options.Cookie is null)
            throw new InvalidOperationException($"Cookie options missing for credential '{kind}'.");

        var cookieOptions = _cookiePolicy.Build(options, auth, kind);
        _cookieManager.Write(context, options.Cookie.Name, value, cookieOptions);
    }

    private void WriteHeader(HttpContext context, string value, CredentialResponseOptions response, AuthFlowContext auth)
    {
        var headerName = response.Name ?? "Authorization";
        var formatted = _headerPolicy.BuildHeaderValue(value, response, auth);

        context.Response.Headers[headerName] = formatted;
    }

    private static CredentialResponseOptions ResolveDelivery(EffectiveAuthResponse response, GrantKind kind)
        => kind switch
        {
            GrantKind.Session => response.SessionIdDelivery,
            GrantKind.AccessToken => response.AccessTokenDelivery,
            GrantKind.RefreshToken => response.RefreshTokenDelivery,
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
}
