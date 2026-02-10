using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Server.Services;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

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
