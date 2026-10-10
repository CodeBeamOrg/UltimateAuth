using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using FluentAssertions;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class EffectiveServerOptionsProviderTests
{
    [Fact]
    public void Original_Options_Are_Not_Mutated()
    {
        var baseOptions = new UAuthServerOptions();

        var provider = TestHelpers.CreateEffectiveOptionsProvider(baseOptions);
        var ctx = TestHttpContext.Create();

        var effective = provider.GetEffective(ctx, AuthFlowType.Login, UAuthClientProfile.BlazorServer);
        effective.Options.Token.AccessTokenLifetime = TimeSpan.FromSeconds(10);

        Assert.NotEqual(baseOptions.Token.AccessTokenLifetime, effective.Options.Token.AccessTokenLifetime);
    }

    [Fact]
    public void EffectiveMode_Is_Determined_By_ModeResolver()
    {
        var baseOptions = new UAuthServerOptions();

        var provider = TestHelpers.CreateEffectiveOptionsProvider(baseOptions);
        var ctx = TestHttpContext.Create();
        var effective = provider.GetEffective(ctx, AuthFlowType.Login, UAuthClientProfile.Api);

        Assert.Equal(UAuthMode.PureJwt, effective.Mode);
    }

    [Fact]
    public void Mode_Defaults_Are_Applied_Before_Overrides()
    {
        var baseOptions = new UAuthServerOptions();

        var provider = TestHelpers.CreateEffectiveOptionsProvider(baseOptions);
        var ctx = TestHttpContext.Create();
        var effective = provider.GetEffective(ctx, AuthFlowType.Login, UAuthClientProfile.BlazorServer);

        Assert.True(effective.Options.Session.SlidingExpiration);
        Assert.NotNull(effective.Options.Session.IdleTimeout);
    }

    [Fact]
    public void ModeConfiguration_Overrides_Mode_Defaults()
    {
        var baseOptions = new UAuthServerOptions();

        baseOptions.ConfigureMode(UAuthMode.PureOpaque, o =>
        {
            o.Token.AccessTokenLifetime = TimeSpan.FromMinutes(1);
        });

        var provider = TestHelpers.CreateEffectiveOptionsProvider(baseOptions);
        var ctx = TestHttpContext.Create();
        var effective = provider.GetEffective(ctx, AuthFlowType.Login, UAuthClientProfile.BlazorServer);

        Assert.Equal(TimeSpan.FromMinutes(1), effective.Options.Token.AccessTokenLifetime);
    }

    [Fact]
    public void Each_Call_Returns_New_EffectiveOptions_Instance()
    {
        var baseOptions = new UAuthServerOptions();

        var provider = TestHelpers.CreateEffectiveOptionsProvider(baseOptions);
        var ctx = TestHttpContext.Create();

        var first = provider.GetEffective(ctx, AuthFlowType.Login, UAuthClientProfile.BlazorServer);
        var second = provider.GetEffective(ctx, AuthFlowType.Login, UAuthClientProfile.BlazorServer);

        Assert.NotSame(first.Options, second.Options);
    }


    [Theory]
    [InlineData(UAuthMode.PureOpaque)]
    [InlineData(UAuthMode.Hybrid)]
    [InlineData(UAuthMode.SemiHybrid)]
    [InlineData(UAuthMode.PureJwt)]
    public void Effective_options_should_apply_defaults_for_each_auth_mode(
        UAuthMode mode)
    {
        var original = new UAuthServerOptions();
        var provider = CreateProviderForMode(original, mode);

        var effective = provider.GetEffective(
            TenantKey.Single,
            AuthFlowType.Login,
            UAuthClientProfile.BlazorServer);

        effective.Mode.Should().Be(mode);
        effective.Options.Should().NotBeSameAs(original);
    }

    [Fact]
    public void SemiHybrid_should_apply_expected_session_defaults()
    {
        var original = new UAuthServerOptions
        {
            Session =
        {
            SlidingExpiration = true,
            TouchInterval = TimeSpan.FromMinutes(5)
        }
        };

        var provider = CreateProviderForMode(original, UAuthMode.SemiHybrid);

        var effective = provider.GetEffective(
            TenantKey.Single,
            AuthFlowType.Login,
            UAuthClientProfile.BlazorServer);

        effective.Options.Session.SlidingExpiration.Should().BeFalse();
        effective.Options.Session.TouchInterval.Should().BeNull();

        // The original configuration must remain unchanged.
        original.Session.SlidingExpiration.Should().BeTrue();
        original.Session.TouchInterval.Should().Be(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void SemiHybrid_should_apply_expected_token_defaults()
    {
        var original = new UAuthServerOptions
        {
            Token =
        {
            IssueJwt = false,
            IssueOpaque = false,
            AddJwtIdClaim = false,
            AccessTokenLifetime = TimeSpan.FromHours(1),
            RefreshTokenLifetime = TimeSpan.FromHours(2)
        }
        };

        var provider = CreateProviderForMode(original, UAuthMode.SemiHybrid);

        var effective = provider.GetEffective(
            TenantKey.Single,
            AuthFlowType.Login,
            UAuthClientProfile.BlazorServer);

        var token = effective.Options.Token;

        token.IssueJwt.Should().BeTrue();
        token.IssueOpaque.Should().BeTrue();
        token.AddJwtIdClaim.Should().BeTrue();
        token.AccessTokenLifetime.Should().Be(TimeSpan.FromMinutes(10));
        token.RefreshTokenLifetime.Should().Be(TimeSpan.FromDays(7));
    }

    [Fact]
    public void SemiHybrid_should_apply_expected_cookie_defaults()
    {
        var provider = CreateProviderForMode(
            new UAuthServerOptions(),
            UAuthMode.SemiHybrid);

        var effective = provider.GetEffective(
            TenantKey.Single,
            AuthFlowType.Login,
            UAuthClientProfile.BlazorServer);

        effective.Options.Cookie.AccessToken.Lifetime.IdleBuffer
            .Should().Be(TimeSpan.FromMinutes(5));

        effective.Options.Cookie.RefreshToken.Lifetime.IdleBuffer
            .Should().Be(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void PureOpaque_should_initialize_missing_optional_session_defaults()
    {
        var original = new UAuthServerOptions
        {
            Session =
        {
            IdleTimeout = null,
            TouchInterval = null
        }
        };

        var provider = CreateProviderForMode(original, UAuthMode.PureOpaque);

        var effective = provider.GetEffective(
            TenantKey.Single,
            AuthFlowType.Login,
            UAuthClientProfile.BlazorServer);

        effective.Options.Session.IdleTimeout.Should().Be(TimeSpan.FromDays(7));
        effective.Options.Session.TouchInterval.Should().Be(TimeSpan.FromDays(1));
        effective.Options.Session.MaxLifetime.Should().BeNull();
        effective.Options.Session.DeviceMismatchBehavior
            .Should().Be(DeviceMismatchBehavior.Allow);

        effective.Options.Token.IssueJwt.Should().BeFalse();
        effective.Options.Token.IssueOpaque.Should().BeFalse();
        effective.Options.Token.IssueRefresh.Should().BeFalse();

        effective.Options.Cookie.Session.Lifetime.IdleBuffer
            .Should().Be(TimeSpan.FromDays(2));

        effective.Options.AuthResponse.RefreshTokenDelivery.Mode
            .Should().Be(TokenResponseMode.None);

        effective.Options.AuthResponse.RefreshTokenDelivery.TokenFormat
            .Should().Be(TokenFormat.Opaque);
    }

    [Fact]
    public void PureOpaque_should_preserve_explicit_optional_session_values()
    {
        var original = new UAuthServerOptions
        {
            Session =
        {
            IdleTimeout = TimeSpan.FromHours(4),
            TouchInterval = TimeSpan.FromMinutes(15),
            MaxLifetime = TimeSpan.FromDays(30)
        }
        };

        var provider = CreateProviderForMode(original, UAuthMode.PureOpaque);

        var effective = provider.GetEffective(
            TenantKey.Single,
            AuthFlowType.Login,
            UAuthClientProfile.BlazorServer);

        effective.Options.Session.IdleTimeout.Should().Be(TimeSpan.FromHours(4));
        effective.Options.Session.TouchInterval.Should().Be(TimeSpan.FromMinutes(15));
        effective.Options.Session.MaxLifetime.Should().Be(TimeSpan.FromDays(30));
    }

    [Fact]
    public void Hybrid_should_apply_expected_defaults()
    {
        var provider = CreateProviderForMode(
            new UAuthServerOptions(),
            UAuthMode.Hybrid);

        var effective = provider.GetEffective(
            TenantKey.Single,
            AuthFlowType.Login,
            UAuthClientProfile.BlazorServer);

        var options = effective.Options;

        options.Session.SlidingExpiration.Should().BeTrue();
        options.Session.TouchInterval.Should().BeNull();

        options.Token.IssueJwt.Should().BeTrue();
        options.Token.IssueOpaque.Should().BeTrue();
        options.Token.AccessTokenLifetime.Should().Be(TimeSpan.FromMinutes(10));
        options.Token.RefreshTokenLifetime.Should().Be(TimeSpan.FromDays(7));

        options.Cookie.Session.Lifetime.IdleBuffer
            .Should().Be(TimeSpan.FromMinutes(5));

        options.Cookie.RefreshToken.Lifetime.IdleBuffer
            .Should().Be(TimeSpan.FromMinutes(5));

        options.AuthResponse.RefreshTokenDelivery.Mode
            .Should().Be(TokenResponseMode.Cookie);

        options.AuthResponse.RefreshTokenDelivery.TokenFormat
            .Should().Be(TokenFormat.Opaque);
    }

    [Fact]
    public void PureJwt_should_apply_stateless_defaults()
    {
        var original = new UAuthServerOptions
        {
            Session =
        {
            SlidingExpiration = true,
            IdleTimeout = TimeSpan.FromDays(7),
            MaxLifetime = TimeSpan.FromDays(30),
            TouchInterval = TimeSpan.FromMinutes(5)
        }
        };

        var provider = CreateProviderForMode(original, UAuthMode.PureJwt);

        var effective = provider.GetEffective(
            TenantKey.Single,
            AuthFlowType.Login,
            UAuthClientProfile.Api);

        var options = effective.Options;

        options.Session.SlidingExpiration.Should().BeFalse();
        options.Session.IdleTimeout.Should().BeNull();
        options.Session.MaxLifetime.Should().BeNull();
        options.Session.TouchInterval.Should().BeNull();

        options.Token.IssueJwt.Should().BeTrue();
        options.Token.IssueOpaque.Should().BeFalse();
        options.Token.AddJwtIdClaim.Should().BeTrue();
        options.Token.AccessTokenLifetime.Should().Be(TimeSpan.FromMinutes(10));
        options.Token.RefreshTokenLifetime.Should().Be(TimeSpan.FromDays(7));

        options.Cookie.AccessToken.Lifetime.IdleBuffer
            .Should().Be(TimeSpan.FromMinutes(5));

        options.Cookie.RefreshToken.Lifetime.IdleBuffer
            .Should().Be(TimeSpan.FromMinutes(5));

        // No mutation of original options.
        original.Session.MaxLifetime.Should().Be(TimeSpan.FromDays(30));
    }

    [Fact]
    public void Unsupported_auth_mode_should_throw()
    {
        var provider = CreateProviderForMode(
            new UAuthServerOptions(),
            (UAuthMode)999);

        Action act = () => provider.GetEffective(
            TenantKey.Single,
            AuthFlowType.Login,
            UAuthClientProfile.Api);

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*Unsupported UAuthMode*");
    }

    [Fact]
    public void SemiHybrid_mode_configuration_should_override_defaults()
    {
        var original = new UAuthServerOptions();

        original.ConfigureMode(UAuthMode.SemiHybrid, options =>
        {
            options.Token.AccessTokenLifetime = TimeSpan.FromMinutes(3);
            options.Session.SlidingExpiration = true;
        });

        var provider = CreateProviderForMode(original, UAuthMode.SemiHybrid);

        var effective = provider.GetEffective(
            TenantKey.Single,
            AuthFlowType.Login,
            UAuthClientProfile.BlazorServer);

        effective.Options.Token.AccessTokenLifetime
            .Should().Be(TimeSpan.FromMinutes(3));

        effective.Options.Session.SlidingExpiration.Should().BeTrue();

        // The base configuration is not mutated.
        original.Token.AccessTokenLifetime.Should().Be(TimeSpan.FromMinutes(10));
        original.Session.SlidingExpiration.Should().BeTrue();
    }

    private static EffectiveServerOptionsProvider CreateProviderForMode(
        UAuthServerOptions options,
        UAuthMode mode)
    {
        return new EffectiveServerOptionsProvider(
            Microsoft.Extensions.Options.Options.Create(options),
            new FixedAuthModeResolver(mode));
    }

    private sealed class FixedAuthModeResolver : IEffectiveAuthModeResolver
    {
        private readonly UAuthMode _mode;

        public FixedAuthModeResolver(UAuthMode mode)
        {
            _mode = mode;
        }

        public UAuthMode Resolve(
            UAuthClientProfile clientProfile,
            AuthFlowType flowType)
        {
            return _mode;
        }
    }


    // TODO: Discuss and enable
    //[Fact]
    //public void FlowType_Is_Passed_To_ModeResolver()
    //{
    //    var baseOptions = new UAuthServerOptions
    //    {
    //        Mode = null
    //    };

    //    var provider = TestHelpers.CreateEffectiveOptionsProvider(baseOptions);
    //    var ctx = new DefaultHttpContext();

    //    var login = provider.GetEffective(
    //        ctx,
    //        AuthFlowType.Login,
    //        UAuthClientProfile.Api);

    //    var api = provider.GetEffective(
    //        ctx,
    //        AuthFlowType.ApiAccess,
    //        UAuthClientProfile.Api);

    //    Assert.NotEqual(login.Mode, api.Mode);
    //}

}
