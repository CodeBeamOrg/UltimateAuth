using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Flows;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public sealed class RefreshTokenResolverTests
{
    [Fact]
    public void Resolve_WhenCustomCookieNameIsConfigured_ReturnsCookieToken()
    {
        var context = TestHttpContext.Create();
        context.Request.Headers.Cookie = "custom-refresh=refresh-token";

        var result = Resolve(
            context,
            CookieDelivery("custom-refresh"));

        Assert.Equal("refresh-token", result);
    }

    [Fact]
    public void Resolve_WhenBearerAndRefreshHeaderExist_PrefersRefreshHeader()
    {
        var context = TestHttpContext.Create();

        context.Request.Headers.Authorization = "Bearer session-id";
        context.Request.Headers["X-Refresh-Token"] = "refresh-token";

        var result = Resolve(
            context,
            CookieDelivery());

        Assert.Equal("refresh-token", result);
    }

    [Fact]
    public void Resolve_WhenMauiRefreshHeaderIsConfigured_ReturnsRefreshToken()
    {
        var context = TestHttpContext.Create();

        context.Request.Headers["uar"] = "Bearer refresh-token";

        var result = Resolve(
            context,
            HeaderDelivery("uar"));

        Assert.Equal("refresh-token", result);
    }

    [Fact]
    public void Resolve_WhenCookieContainsToken_ReturnsCookieToken()
    {
        var context = TestHttpContext.Create();
        context.Request.Headers.Cookie = "uar=cookie-token";

        var result = Resolve(context, CookieDelivery("uar"));

        Assert.Equal("cookie-token", result);
    }

    [Fact]
    public void Resolve_WhenAuthorizationContainsBearerToken_ReturnsNull()
    {
        var context = CreateContext();
        context.Request.Headers.Authorization = "Bearer bearer-token";

        var result = Resolve(context, CredentialResponseOptions.Disabled(GrantKind.RefreshToken));

        Assert.Null(result);
    }

    [Theory]
    [InlineData("Bearer bearer-token")]
    [InlineData("bEaReR bearer-token")]
    public void Resolve_WhenAuthorizationIsNotConfiguredAsRefreshHeader_ReturnsNull(string authorization)
    {
        var context = TestHttpContext.Create();
        context.Request.Headers.Authorization = authorization;

        var result = Resolve(context, HeaderDelivery("uar"));

        Assert.Null(result);
    }

    [Theory]
    [InlineData("Bearer bearer-token")]
    [InlineData("bEaReR bearer-token")]
    public void Resolve_WhenAuthorizationIsConfiguredAsRefreshHeader_ReturnsToken(string authorization)
    {
        var context = TestHttpContext.Create();
        context.Request.Headers.Authorization = authorization;

        var result = Resolve(context, HeaderDelivery("Authorization"));

        Assert.Equal("bearer-token", result);
    }

    [Theory]
    [InlineData("header-token", "header-token")]
    [InlineData("Bearer header-token", "header-token")]
    [InlineData("bEaReR header-token", "header-token")]
    public void Resolve_WhenRefreshHeaderContainsToken_ReturnsHeaderToken(
        string header,
        string expected)
    {
        var context = CreateContext();
        context.Request.Headers["X-Refresh-Token"] = header;

        var result = Resolve(context, HeaderDelivery("X-Refresh-Token"));

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Resolve_WhenNoTokenExists_ReturnsNull()
    {
        var context = CreateContext();

        var result = Resolve(context, CredentialResponseOptions.Disabled(GrantKind.RefreshToken));

        Assert.Null(result);
    }

    [Fact]
    public void Resolve_WhenCookieAndBearerExist_PrefersCookie()
    {
        var context = CreateContext();
        context.Request.Headers.Cookie = "uar=cookie-token";
        context.Request.Headers.Authorization = "Bearer bearer-token";

        var result = Resolve(context, CookieDelivery("uar"));

        Assert.Equal("cookie-token", result);
    }

    [Fact]
    public void Resolve_WhenCookieAndRefreshHeaderExist_PrefersRefreshHeader()
    {
        var context = TestHttpContext.Create();

        context.Request.Headers.Cookie = "uar=cookie-token";
        context.Request.Headers["X-Refresh-Token"] = "header-token";

        var result = Resolve(context, CookieDelivery("uar"));

        Assert.Equal("header-token", result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Basic abc")]
    [InlineData("Token abc")]
    [InlineData("Bearer")]
    [InlineData("Bearer ")]
    [InlineData("Bearer    ")]
    public void Resolve_WhenAuthorizationIsPresentWithoutRefreshCredential_ReturnsNull(
        string authorization)
    {
        var context = CreateContext();
        context.Request.Headers.Authorization = authorization;

        var result = Resolve(context, CredentialResponseOptions.Disabled(GrantKind.RefreshToken));

        Assert.Null(result);
    }

    [Fact]
    public void Resolve_WhenBearerTokenIsEmpty_FallsBackToRefreshHeader()
    {
        var context = CreateContext();
        context.Request.Headers.Authorization = "Bearer   ";
        context.Request.Headers["X-Refresh-Token"] = "header-token";

        var result = Resolve(context, HeaderDelivery("X-Refresh-Token"));

        Assert.Equal("header-token", result);
    }

    [Fact]
    public void Resolve_WhenCookieIsEmpty_DoesNotUseAuthorizationAsRefreshToken()
    {
        var context = TestHttpContext.Create();

        context.Request.Headers.Cookie = "uar=";
        context.Request.Headers.Authorization = "Bearer bearer-token";

        var result = Resolve(context, CookieDelivery("uar"));

        Assert.Null(result);
    }

    [Fact]
    public void Resolve_WhenCustomCookieIsConfigured_DoesNotReadDefaultCookie()
    {
        var context = TestHttpContext.Create();

        context.Request.Headers.Cookie = "uar=wrong-token";

        var result = Resolve(context, CookieDelivery("custom-refresh"));

        Assert.Null(result);
    }

    [Fact]
    public void Resolve_WhenExplicitRefreshHeaderIsEmpty_FallsBackToCookie()
    {
        var context = TestHttpContext.Create();

        context.Request.Headers.Cookie = "uar=cookie-token";
        context.Request.Headers["X-Refresh-Token"] = "   ";

        var result = Resolve(context, CookieDelivery("uar"));

        Assert.Equal("cookie-token", result);
    }

    [Fact]
    public void Resolve_WhenBearerSessionAndRefreshCookieExist_ReturnsRefreshCookie()
    {
        var context = TestHttpContext.Create();

        context.Request.Headers.Authorization = "Bearer session-id";
        context.Request.Headers.Cookie = "uar=refresh-token";

        var result = Resolve(context, CookieDelivery("uar"));

        Assert.Equal("refresh-token", result);
    }


    private static DefaultHttpContext CreateContext()
        => new();

    private static string? Resolve(HttpContext context, CredentialResponseOptions refreshDelivery)
    {
        var response = new EffectiveAuthResponse(
            sessionIdDelivery: CredentialResponseOptions.Disabled(GrantKind.Session),
            accessTokenDelivery: CredentialResponseOptions.Disabled(GrantKind.AccessToken),
            refreshTokenDelivery: refreshDelivery,
            redirect: EffectiveRedirectResponse.Disabled
        );

        var flow = AuthFlowTestFactory.New(response: response, mode: UAuthMode.Hybrid);

        var httpAccessor = new HttpContextAccessor
        {
            HttpContext = context
        };

        var authAccessor = new AuthFlowContextAccessor(httpAccessor);
        authAccessor.Set(flow);

        var resolver = new RefreshTokenResolver(authAccessor);

        return resolver.Resolve(context);
    }

    private static CredentialResponseOptions CookieDelivery(string cookieName = "uar")
    {
        return new CredentialResponseOptions
        {
            Kind = GrantKind.RefreshToken,
            Mode = TokenResponseMode.Cookie,
            TokenFormat = TokenFormat.Opaque
        }.WithCookie(new UAuthCookieOptions
        {
            Name = cookieName
        });
    }

    private static CredentialResponseOptions HeaderDelivery(string headerName = "uar")
    {
        return new CredentialResponseOptions
        {
            Kind = GrantKind.RefreshToken,
            Mode = TokenResponseMode.Header,
            TokenFormat = TokenFormat.Opaque,
            HeaderFormat = HeaderTokenFormat.Bearer,
            Name = headerName
        };
    }
}
