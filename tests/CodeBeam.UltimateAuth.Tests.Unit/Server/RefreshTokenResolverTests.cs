using CodeBeam.UltimateAuth.Server.Flows;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public sealed class RefreshTokenResolverTests
{
    private readonly RefreshTokenResolver _sut = new();

    [Fact]
    public void Resolve_WhenCookieContainsToken_ReturnsCookieToken()
    {
        var context = CreateContext();
        context.Request.Headers.Cookie = "uar=cookie-token";

        var result = _sut.Resolve(context);

        Assert.Equal("cookie-token", result);
    }

    [Fact]
    public void Resolve_WhenAuthorizationContainsBearerToken_ReturnsBearerToken()
    {
        var context = CreateContext();
        context.Request.Headers.Authorization = "Bearer bearer-token";

        var result = _sut.Resolve(context);

        Assert.Equal("bearer-token", result);
    }

    [Fact]
    public void Resolve_WhenBearerSchemeUsesDifferentCasing_ReturnsBearerToken()
    {
        var context = CreateContext();
        context.Request.Headers.Authorization = "bEaReR bearer-token";

        var result = _sut.Resolve(context);

        Assert.Equal("bearer-token", result);
    }

    [Fact]
    public void Resolve_WhenRefreshHeaderContainsToken_ReturnsHeaderToken()
    {
        var context = CreateContext();
        context.Request.Headers["X-Refresh-Token"] = "header-token";

        var result = _sut.Resolve(context);

        Assert.Equal("header-token", result);
    }

    [Fact]
    public void Resolve_WhenNoTokenExists_ReturnsNull()
    {
        var context = CreateContext();

        var result = _sut.Resolve(context);

        Assert.Null(result);
    }

    [Fact]
    public void Resolve_WhenCookieAndBearerExist_PrefersCookie()
    {
        var context = CreateContext();
        context.Request.Headers.Cookie = "uar=cookie-token";
        context.Request.Headers.Authorization = "Bearer bearer-token";

        var result = _sut.Resolve(context);

        Assert.Equal("cookie-token", result);
    }

    [Fact]
    public void Resolve_WhenBearerAndRefreshHeaderExist_PrefersBearer()
    {
        var context = CreateContext();
        context.Request.Headers.Authorization = "Bearer bearer-token";
        context.Request.Headers["X-Refresh-Token"] = "header-token";

        var result = _sut.Resolve(context);

        Assert.Equal("bearer-token", result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Basic abc")]
    [InlineData("Token abc")]
    [InlineData("Bearer")]
    [InlineData("Bearer ")]
    [InlineData("Bearer    ")]
    public void Resolve_WhenAuthorizationDoesNotContainValidBearerToken_ReturnsNull(
    string authorization)
    {
        var context = CreateContext();
        context.Request.Headers.Authorization = authorization;

        var result = _sut.Resolve(context);

        Assert.Null(result);
    }

    [Fact]
    public void Resolve_WhenBearerTokenIsEmpty_FallsBackToRefreshHeader()
    {
        var context = CreateContext();
        context.Request.Headers.Authorization = "Bearer   ";
        context.Request.Headers["X-Refresh-Token"] = "header-token";

        var result = _sut.Resolve(context);

        Assert.Equal("header-token", result);
    }

    [Fact]
    public void Resolve_WhenCookieIsEmpty_FallsBackToBearerToken()
    {
        var context = CreateContext();
        context.Request.Headers.Cookie = "uar=";
        context.Request.Headers.Authorization = "Bearer bearer-token";

        var result = _sut.Resolve(context);

        Assert.Equal("bearer-token", result);
    }

    private static DefaultHttpContext CreateContext()
        => new();
}
