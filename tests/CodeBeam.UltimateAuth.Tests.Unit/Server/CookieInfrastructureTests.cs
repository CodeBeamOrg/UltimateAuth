using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Tests.Unit.Server;

public sealed class CookieInfrastructureTests
{
    private readonly UAuthCookiePolicyBuilder _builder = new();
    private readonly UAuthCookieManager _manager = new();

    // ---------------------------------------------------------
    // COOKIE POLICY - VALIDATION
    // ---------------------------------------------------------

    [Fact]
    public void Build_should_throw_when_cookie_options_are_missing()
    {
        var context = AuthFlowTestFactory.New();

        var response = CredentialResponseOptions.Disabled(
            GrantKind.Session);

        var act = () => _builder.Build(
            response,
            context,
            GrantKind.Session);

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*Cookie options are null*");
    }

    // ---------------------------------------------------------
    // COOKIE POLICY - SECURITY OPTIONS
    // ---------------------------------------------------------

    [Fact]
    public void Build_should_preserve_explicit_cookie_settings()
    {
        var cookie = CreateCookie();
        cookie.HttpOnly = true;
        cookie.SecurePolicy = CookieSecurePolicy.Always;
        cookie.Path = "/auth";
        cookie.Domain = ".example.com";
        cookie.SameSite = SameSiteMode.Strict;

        var options = Build(
            GrantKind.Session,
            cookie);

        options.HttpOnly.Should().BeTrue();
        options.Secure.Should().BeTrue();
        options.Path.Should().Be("/auth");
        options.Domain.Should().Be(".example.com");
        options.SameSite.Should().Be(SameSiteMode.Strict);
    }

    [Fact]
    public void Build_should_support_non_http_only_cookie_configuration()
    {
        var cookie = CreateCookie();
        cookie.HttpOnly = false;

        var options = Build(
            GrantKind.Session,
            cookie);

        options.HttpOnly.Should().BeFalse();
    }

    [Fact]
    public void Build_should_disable_secure_when_policy_is_not_always()
    {
        var cookie = CreateCookie();
        cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;

        var options = Build(
            GrantKind.Session,
            cookie);

        options.Secure.Should().BeFalse();
    }

    [Fact]
    public void Build_should_preserve_host_only_cookie_configuration()
    {
        var cookie = CreateCookie();
        cookie.Domain = null;
        cookie.Path = "/";

        var options = Build(
            GrantKind.Session,
            cookie);

        options.Domain.Should().BeNull();
        options.Path.Should().Be("/");
    }

    // ---------------------------------------------------------
    // COOKIE POLICY - SAME SITE
    // ---------------------------------------------------------

    [Theory]
    [InlineData(
        UAuthHubDeploymentMode.Embedded,
        SameSiteMode.Strict)]
    [InlineData(
        UAuthHubDeploymentMode.Integrated,
        SameSiteMode.Lax)]
    [InlineData(
        UAuthHubDeploymentMode.External,
        SameSiteMode.None)]
    [InlineData(
        (UAuthHubDeploymentMode)999,
        SameSiteMode.Lax)]
    public void Build_should_resolve_same_site_from_deployment_mode(
        UAuthHubDeploymentMode deploymentMode,
        SameSiteMode expected)
    {
        var cookie = CreateCookie();
        cookie.SameSite = null;

        var context = AuthFlowTestFactory.New();
        context.OriginalOptions.HubDeploymentMode = deploymentMode;

        var options = _builder.Build(
            CreateResponse(GrantKind.Session, cookie),
            context,
            GrantKind.Session);

        options.SameSite.Should().Be(expected);
    }

    [Theory]
    [InlineData(SameSiteMode.Strict)]
    [InlineData(SameSiteMode.Lax)]
    [InlineData(SameSiteMode.None)]
    public void Explicit_same_site_should_override_deployment_mode(
        SameSiteMode explicitMode)
    {
        var cookie = CreateCookie();
        cookie.SameSite = explicitMode;

        var context = AuthFlowTestFactory.New();
        context.OriginalOptions.HubDeploymentMode =
            UAuthHubDeploymentMode.External;

        var options = _builder.Build(
            CreateResponse(GrantKind.Session, cookie),
            context,
            GrantKind.Session);

        options.SameSite.Should().Be(explicitMode);
    }

    // ---------------------------------------------------------
    // COOKIE POLICY - LIFETIME PRECEDENCE
    // ---------------------------------------------------------

    [Fact]
    public void Explicit_max_age_should_override_all_other_lifetimes()
    {
        var cookie = CreateCookie();
        cookie.MaxAge = TimeSpan.FromMinutes(30);
        cookie.Lifetime.AbsoluteLifetimeOverride =
            TimeSpan.FromHours(2);
        cookie.Lifetime.IdleBuffer =
            TimeSpan.FromMinutes(5);

        var context = AuthFlowTestFactory.New();

        context.EffectiveOptions.Options.Session.IdleTimeout =
            TimeSpan.FromHours(4);

        var options = _builder.Build(
            CreateResponse(GrantKind.Session, cookie),
            context,
            GrantKind.Session);

        options.MaxAge.Should().Be(TimeSpan.FromMinutes(35));
    }

    [Fact]
    public void Absolute_lifetime_override_should_take_precedence_over_grant_lifetime()
    {
        var cookie = CreateCookie();
        cookie.MaxAge = null;
        cookie.Lifetime.AbsoluteLifetimeOverride =
            TimeSpan.FromMinutes(45);
        cookie.Lifetime.IdleBuffer =
            TimeSpan.FromMinutes(5);

        var context = AuthFlowTestFactory.New();

        context.EffectiveOptions.Options.Session.IdleTimeout =
            TimeSpan.FromHours(4);

        var options = _builder.Build(
            CreateResponse(GrantKind.Session, cookie),
            context,
            GrantKind.Session);

        // Current implementation adds IdleBuffer even
        // when AbsoluteLifetimeOverride is specified.
        options.MaxAge.Should().Be(TimeSpan.FromMinutes(50));
    }

    [Fact]
    public void Null_idle_buffer_should_be_treated_as_zero()
    {
        var cookie = CreateCookie();
        cookie.MaxAge = TimeSpan.FromMinutes(30);
        cookie.Lifetime.IdleBuffer = null;

        var options = Build(
            GrantKind.Session,
            cookie);

        options.MaxAge.Should().Be(TimeSpan.FromMinutes(30));
    }

    [Fact]
    public void Zero_idle_buffer_should_not_extend_lifetime()
    {
        var cookie = CreateCookie();
        cookie.MaxAge = TimeSpan.FromMinutes(30);
        cookie.Lifetime.IdleBuffer = TimeSpan.Zero;

        var options = Build(
            GrantKind.Session,
            cookie);

        options.MaxAge.Should().Be(TimeSpan.FromMinutes(30));
    }

    // ---------------------------------------------------------
    // COOKIE POLICY - SESSION LIFETIME
    // ---------------------------------------------------------

    [Fact]
    public void Pure_opaque_session_should_use_idle_timeout()
    {
        var cookie = CreateCookie();

        var context = AuthFlowTestFactory.New(
            mode: UAuthMode.PureOpaque);

        context.EffectiveOptions.Options.Session.IdleTimeout =
            TimeSpan.FromMinutes(20);

        context.EffectiveOptions.Options.Token.RefreshTokenLifetime =
            TimeSpan.FromDays(7);

        var options = _builder.Build(
            CreateResponse(GrantKind.Session, cookie),
            context,
            GrantKind.Session);

        options.MaxAge.Should().Be(TimeSpan.FromMinutes(25));
    }

    [Fact]
    public void Hybrid_session_should_use_refresh_lifetime_when_longer()
    {
        var cookie = CreateCookie();

        var context = AuthFlowTestFactory.New(
            mode: UAuthMode.Hybrid);

        context.EffectiveOptions.Options.Session.IdleTimeout =
            TimeSpan.FromMinutes(20);

        context.EffectiveOptions.Options.Token.RefreshTokenLifetime =
            TimeSpan.FromDays(7);

        var options = _builder.Build(
            CreateResponse(GrantKind.Session, cookie),
            context,
            GrantKind.Session);

        options.MaxAge.Should().Be(
            TimeSpan.FromDays(7) + TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void Hybrid_session_should_use_idle_timeout_when_longer()
    {
        var cookie = CreateCookie();

        var context = AuthFlowTestFactory.New(
            mode: UAuthMode.Hybrid);

        context.EffectiveOptions.Options.Session.IdleTimeout =
            TimeSpan.FromHours(12);

        context.EffectiveOptions.Options.Token.RefreshTokenLifetime =
            TimeSpan.FromHours(2);

        var options = _builder.Build(
            CreateResponse(GrantKind.Session, cookie),
            context,
            GrantKind.Session);

        options.MaxAge.Should().Be(
            TimeSpan.FromHours(12) + TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void Non_hybrid_session_should_use_idle_timeout()
    {
        var cookie = CreateCookie();

        var context = AuthFlowTestFactory.New(
            mode: UAuthMode.PureJwt);

        context.EffectiveOptions.Options.Session.IdleTimeout =
            TimeSpan.FromMinutes(15);

        context.EffectiveOptions.Options.Token.RefreshTokenLifetime =
            TimeSpan.FromDays(1);

        var options = _builder.Build(
            CreateResponse(GrantKind.Session, cookie),
            context,
            GrantKind.Session);

        options.MaxAge.Should().Be(TimeSpan.FromMinutes(20));
    }

    // ---------------------------------------------------------
    // COOKIE POLICY - TOKEN LIFETIMES
    // ---------------------------------------------------------

    [Fact]
    public void Access_token_cookie_should_use_access_token_lifetime()
    {
        var cookie = CreateCookie();

        var context = AuthFlowTestFactory.New();

        context.EffectiveOptions.Options.Token.AccessTokenLifetime =
            TimeSpan.FromMinutes(15);

        var options = _builder.Build(
            CreateResponse(GrantKind.AccessToken, cookie),
            context,
            GrantKind.AccessToken);

        options.MaxAge.Should().Be(TimeSpan.FromMinutes(20));
    }

    [Fact]
    public void Refresh_token_cookie_should_use_refresh_token_lifetime()
    {
        var cookie = CreateCookie();

        var context = AuthFlowTestFactory.New();

        context.EffectiveOptions.Options.Token.RefreshTokenLifetime =
            TimeSpan.FromDays(14);

        var options = _builder.Build(
            CreateResponse(GrantKind.RefreshToken, cookie),
            context,
            GrantKind.RefreshToken);

        options.MaxAge.Should().Be(
            TimeSpan.FromDays(14) + TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void Unknown_grant_kind_without_override_should_not_set_max_age()
    {
        var cookie = CreateCookie();
        cookie.MaxAge = null;
        cookie.Lifetime.AbsoluteLifetimeOverride = null;

        var options = Build(
            (GrantKind)999,
            cookie);

        options.MaxAge.Should().BeNull();
    }

    [Fact]
    public void Unknown_grant_kind_should_still_support_explicit_max_age()
    {
        var cookie = CreateCookie();
        cookie.MaxAge = TimeSpan.FromMinutes(10);

        var options = Build(
            (GrantKind)999,
            cookie);

        options.MaxAge.Should().Be(TimeSpan.FromMinutes(15));
    }

    // ---------------------------------------------------------
    // COOKIE MANAGER - WRITE
    // ---------------------------------------------------------

    [Fact]
    public void Write_should_append_cookie_to_http_response()
    {
        var context = new DefaultHttpContext();

        var options = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            Path = "/",
            SameSite = SameSiteMode.Strict,
            MaxAge = TimeSpan.FromMinutes(30)
        };

        _manager.Write(
            context,
            "uauth-session",
            "session-123",
            options);

        var header = context.Response.Headers.SetCookie.ToString();

        header.Should().Contain("uauth-session=session-123");
        header.Should().Contain("path=/");
        header.Should().Contain("secure");
        header.Should().Contain("httponly");
        header.Should().Contain("samesite=strict");
        header.Should().Contain("max-age=1800");
    }

    [Fact]
    public void Write_should_preserve_custom_cookie_path_and_domain()
    {
        var context = new DefaultHttpContext();

        var options = new CookieOptions
        {
            Path = "/auth",
            Domain = ".example.com",
            HttpOnly = true,
            Secure = true
        };

        _manager.Write(
            context,
            "uauth-refresh",
            "refresh-123",
            options);

        var header = context.Response.Headers.SetCookie
            .ToString()
            .ToLowerInvariant();

        header.Should().Contain("uauth-refresh=refresh-123");
        header.Should().Contain("path=/auth");
        header.Should().Contain("domain=.example.com");
    }

    [Fact]
    public void Write_should_append_multiple_cookies_without_overwriting()
    {
        var context = new DefaultHttpContext();

        _manager.Write(
            context,
            "uauth-session",
            "session-123",
            new CookieOptions());

        _manager.Write(
            context,
            "uauth-refresh",
            "refresh-456",
            new CookieOptions());

        var headers = context.Response.Headers.SetCookie;

        headers.Count.Should().Be(2);

        headers[0].Should().Contain("uauth-session=session-123");
        headers[1].Should().Contain("uauth-refresh=refresh-456");
    }

    // ---------------------------------------------------------
    // COOKIE MANAGER - READ
    // ---------------------------------------------------------

    [Fact]
    public void TryRead_should_return_cookie_value_when_present()
    {
        var context = new DefaultHttpContext();

        context.Request.Headers.Cookie =
            "uauth-session=session-123; other=value";

        var found = _manager.TryRead(
            context,
            "uauth-session",
            out var value);

        found.Should().BeTrue();
        value.Should().Be("session-123");
    }

    [Fact]
    public void TryRead_should_return_false_when_cookie_is_missing()
    {
        var context = new DefaultHttpContext();

        context.Request.Headers.Cookie =
            "other=value";

        var found = _manager.TryRead(
            context,
            "uauth-session",
            out _);

        found.Should().BeFalse();
    }

    [Fact]
    public void TryRead_should_not_confuse_similar_cookie_names()
    {
        var context = new DefaultHttpContext();

        context.Request.Headers.Cookie =
            "uauth-session-extra=wrong; uauth-session=correct";

        var found = _manager.TryRead(
            context,
            "uauth-session",
            out var value);

        found.Should().BeTrue();
        value.Should().Be("correct");
    }

    [Fact]
    public void TryRead_should_decode_cookie_value()
    {
        var context = new DefaultHttpContext();

        context.Request.Headers.Cookie =
            "uauth-session=hello%20world";

        var found = _manager.TryRead(
            context,
            "uauth-session",
            out var value);

        found.Should().BeTrue();
        value.Should().Be("hello world");
    }

    // ---------------------------------------------------------
    // COOKIE MANAGER - DELETE
    // ---------------------------------------------------------

    [Fact]
    public void Delete_should_append_expired_cookie_to_response()
    {
        var context = new DefaultHttpContext();

        _manager.Delete(
            context,
            "uauth-session");

        var header = context.Response.Headers.SetCookie
            .ToString()
            .ToLowerInvariant();

        header.Should().Contain("uauth-session=");
        header.Should().Contain("expires=");
    }

    [Fact]
    public void Delete_should_not_modify_unrelated_request_cookies()
    {
        var context = new DefaultHttpContext();

        context.Request.Headers.Cookie =
            "uauth-session=session-123; other=value";

        _manager.Delete(
            context,
            "uauth-session");

        // Deletion instructs the browser to remove the cookie.
        // It does not mutate the current HTTP request.
        _manager.TryRead(
            context,
            "other",
            out var other).Should().BeTrue();

        other.Should().Be("value");
    }

    // ---------------------------------------------------------
    // HELPERS
    // ---------------------------------------------------------

    private CookieOptions Build(
        GrantKind kind,
        UAuthCookieOptions cookie,
        UAuthMode mode = UAuthMode.PureOpaque)
    {
        var context = AuthFlowTestFactory.New(mode: mode);

        return _builder.Build(
            CreateResponse(kind, cookie),
            context,
            kind);
    }

    private static UAuthCookieOptions CreateCookie()
    {
        return new UAuthCookieOptions
        {
            Name = "uauth-test",
            HttpOnly = true,
            SecurePolicy = CookieSecurePolicy.Always,
            Path = "/",
            SameSite = null,
            MaxAge = null,
            Lifetime = new UAuthCookieLifetimeOptions
            {
                IdleBuffer = TimeSpan.FromMinutes(5)
            }
        };
    }

    private static CredentialResponseOptions CreateResponse(
        GrantKind kind,
        UAuthCookieOptions cookie)
    {
        return new CredentialResponseOptions
        {
            Kind = kind,
            Mode = TokenResponseMode.Cookie
        }.WithCookie(cookie);
    }
}
