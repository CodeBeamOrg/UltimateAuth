using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public sealed class CookieSessionIdResolverTests
{
    private const string ValidSessionId =
        "cookie-session-0000000000000000000000001";

    [Fact]
    public void Name_ShouldBeCookie()
    {
        var sut = CreateSut();

        sut.Name.Should().Be("cookie");
    }

    [Fact]
    public void Resolve_WhenCookieMissing_ShouldReturnNull()
    {
        var sut = CreateSut();
        var context = new DefaultHttpContext();

        var result = sut.Resolve(context);

        result.Should().BeNull();
    }

    [Fact]
    public void Resolve_WhenCookieIsEmpty_ShouldReturnNull()
    {
        var sut = CreateSut();

        var context = new DefaultHttpContext();

        context.Request.Headers.Cookie =
            "uauth_session=";

        var result = sut.Resolve(context);

        result.Should().BeNull();
    }

    [Fact]
    public void Resolve_WhenCookieContainsInvalidSessionId_ShouldReturnNull()
    {
        var sut = CreateSut();

        var context = new DefaultHttpContext();

        context.Request.Headers.Cookie =
            "uauth_session=invalid";

        var result = sut.Resolve(context);

        result.Should().BeNull();
    }

    [Fact]
    public void Resolve_WhenCookieContainsValidSessionId_ShouldReturnSessionId()
    {
        var sut = CreateSut();

        var context = new DefaultHttpContext();

        context.Request.Headers.Cookie =
            $"uauth_session={ValidSessionId}";

        var result = sut.Resolve(context);

        result.Should().NotBeNull();
        result!.Value.ToString().Should().Be(ValidSessionId);
    }

    [Fact]
    public void Resolve_ShouldUseConfiguredCookieName()
    {
        var options = TestServerOptions.Default();

        options.Cookie.Session.Name =
            "custom_session";

        var sut =
            new CookieSessionIdResolver(
                Options.Create(options));

        var context = new DefaultHttpContext();

        context.Request.Headers.Cookie =
            $"uauth_session=ignored-session-00000000000000000001; custom_session={ValidSessionId}";

        var result = sut.Resolve(context);

        result.Should().NotBeNull();
        result!.Value.ToString().Should().Be(ValidSessionId);
    }

    private static CookieSessionIdResolver CreateSut()
    {
        var options = TestServerOptions.Default();

        options.Cookie.Session.Name =
            "uauth_session";

        return new CookieSessionIdResolver(
            Options.Create(options));
    }
}
