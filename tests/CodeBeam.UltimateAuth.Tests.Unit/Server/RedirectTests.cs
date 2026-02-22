using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Constants;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class RedirectTests
{
    [Fact]
    public void LoginFlow_Uses_Configured_Redirect_Options()
    {
        var services = new ServiceCollection();

        services.AddOptions();
        services.Configure<UAuthServerOptions>(o =>
        {
            o.AuthResponse.Login.AllowReturnUrlOverride = false;
            o.AuthResponse.Login.SuccessRedirect = "/welcome";
        });

        services.AddSingleton<IEffectiveAuthModeResolver, TestAuthModeResolver>();
        services.AddSingleton<IEffectiveServerOptionsProvider, EffectiveServerOptionsProvider>();
        services.AddSingleton<AuthResponseOptionsModeTemplateResolver>();
        services.AddSingleton<ClientProfileAuthResponseAdapter>();
        services.AddSingleton<IAuthResponseResolver, AuthResponseResolver>();

        var provider = services.BuildServiceProvider();
        var optionsProvider = provider.GetRequiredService<IEffectiveServerOptionsProvider>();
        var resolver = provider.GetRequiredService<IAuthResponseResolver>();

        var effective = optionsProvider.GetEffective(TenantKey.Single, AuthFlowType.Login, UAuthClientProfile.BlazorServer);
        var response = resolver.Resolve(effective.Mode, AuthFlowType.Login, UAuthClientProfile.BlazorServer, effective);

        response.Redirect.AllowReturnUrlOverride.Should().BeFalse();
        response.Redirect.SuccessPath.Should().Be("/welcome");
    }

    [Fact]
    public void ClientProfile_Is_Read_From_Header()
    {
        var reader = new ClientProfileReader();
        var ctx = TestHttpContext.Create();
        ctx.Request.Headers[UAuthConstants.Headers.ClientProfile] = "BlazorServer";

        var profile = reader.Read(ctx);
        profile.Should().Be(UAuthClientProfile.BlazorServer);
    }

    [Theory]
    [InlineData(UAuthClientProfile.BlazorWasm, AuthFlowType.Login, UAuthMode.Hybrid)]
    [InlineData(UAuthClientProfile.BlazorServer, AuthFlowType.Login, UAuthMode.PureOpaque)]
    public void ClientProfile_Resolves_To_Correct_Mode(UAuthClientProfile profile, AuthFlowType flow, UAuthMode expected)
    {
        var resolver = new EffectiveAuthModeResolver();
        var mode = resolver.Resolve(profile, flow);
        mode.Should().Be(expected);
    }

    [Fact]
    public void Absolute_ReturnUrl_Is_Used_When_Override_Allowed()
    {
        var flow = AuthFlowTestFactory.LoginSuccess(
            returnUrlInfo: ReturnUrlParser.Parse("https://app.example.com/dashboard"),
            redirect: new EffectiveRedirectResponse(
                enabled: true,
                successPath: "/welcome",
                failurePath: null,
                failureQueryKey: null,
                failureCodes: null,
                allowReturnUrlOverride: true,
                includeLockoutTiming: true,
                includeRemainingAttempts: false
            )
        );

        var resolver = TestRedirectResolver.Create();
        var ctx = TestHttpContext.Create();
        var decision = resolver.ResolveSuccess(flow, ctx);
        decision.Enabled.Should().BeTrue();
        decision.TargetUrl.Should().Be("https://app.example.com/dashboard");
    }

    [Fact]
    public void Absolute_ReturnUrl_Is_Ignored_When_Override_Disabled()
    {
        var flow = AuthFlowTestFactory.LoginSuccess(
            returnUrlInfo: ReturnUrlParser.Parse("https://app.example.com/dashboard"),
            redirect: new EffectiveRedirectResponse(
                enabled: true,
                successPath: "/welcome",
                failurePath: null,
                failureQueryKey: null,
                failureCodes: null,
                allowReturnUrlOverride: false,
                includeLockoutTiming: true,
                includeRemainingAttempts: false
            )
        );

        var resolver = TestRedirectResolver.Create();
        var ctx = TestHttpContext.Create();
        var decision = resolver.ResolveSuccess(flow, ctx);
        decision.TargetUrl.Should().Be("https://app.example.com/welcome");
    }

    [Fact]
    public void Relative_ReturnUrl_Is_Combined_With_BaseAddress()
    {
        var flow = AuthFlowTestFactory.LoginSuccess(
            returnUrlInfo: ReturnUrlParser.Parse("/dashboard"),
            redirect: new EffectiveRedirectResponse(
                enabled: true,
                successPath: "/welcome",
                failurePath: null,
                failureQueryKey: null,
                failureCodes: null,
                allowReturnUrlOverride: true,
                includeLockoutTiming: true,
                includeRemainingAttempts: false
            )
        );

        var resolver = TestRedirectResolver.Create();
        var ctx = TestHttpContext.Create(); // https://app.example.com

        var decision = resolver.ResolveSuccess(flow, ctx);
        decision.TargetUrl.Should().Be("https://app.example.com/dashboard");
    }

    [Fact]
    public void SuccessPath_Is_Used_When_No_ReturnUrl()
    {
        var flow = AuthFlowTestFactory.LoginSuccess(
            returnUrlInfo: null,
            redirect: new EffectiveRedirectResponse(
                enabled: true,
                successPath: "/welcome",
                failurePath: null,
                failureQueryKey: null,
                failureCodes: null,
                allowReturnUrlOverride: true,
                includeLockoutTiming: true,
                includeRemainingAttempts: false
            )
        );

        var resolver = TestRedirectResolver.Create();
        var ctx = TestHttpContext.Create();
        var decision = resolver.ResolveSuccess(flow, ctx);
        decision.TargetUrl.Should().Be("https://app.example.com/welcome");
    }

    [Fact]
    public void Absolute_ReturnUrl_Outside_AllowedOrigins_Throws()
    {
        var flow = AuthFlowTestFactory.LoginSuccess(
            returnUrlInfo: ReturnUrlParser.Parse("https://evil.com"),
            redirect: new EffectiveRedirectResponse(
                enabled: true,
                successPath: "/welcome",
                failurePath: null,
                failureQueryKey: null,
                failureCodes: null,
                allowReturnUrlOverride: true,
                includeLockoutTiming: true,
                includeRemainingAttempts: false
            )
        );

        var resolver = TestRedirectResolver.Create();
        var ctx = TestHttpContext.Create();
        flow.OriginalOptions.Hub.AllowedClientOrigins.Add("https://app.example.com");
        Action act = () => resolver.ResolveSuccess(flow, ctx);
        act.Should().Throw<InvalidOperationException>().WithMessage("*not allowed*");
    }

    [Fact]
    public void Failure_Redirect_Contains_Mapped_Error_Code()
    {
        var redirect = new EffectiveRedirectResponse(
            enabled: true,
            successPath: "/welcome",
            failurePath: "/login",
            failureQueryKey: "error",
            failureCodes: new Dictionary<AuthFailureReason, string>
            {
                [AuthFailureReason.InvalidCredentials] = "bad_credentials"
            },
            allowReturnUrlOverride: false,
            includeLockoutTiming: true,
            includeRemainingAttempts: false
        );

        var flow = AuthFlowTestFactory.LoginSuccess(
            returnUrlInfo: null,
            redirect: redirect
        );

        var resolver = TestRedirectResolver.Create();
        var ctx = TestHttpContext.Create();
        var decision = resolver.ResolveFailure(flow, ctx, AuthFailureReason.InvalidCredentials);
        decision.TargetUrl.Should().Be("https://app.example.com/login?error=bad_credentials");
    }
}
