using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.Extensions.Options;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Unit.ResourceApi;

public sealed class UAuthResourceApiOptionsValidatorTests
{
    private static readonly IEffectiveAuthModeResolver DefaultResolver =
        new EffectiveAuthModeResolver();

    [Theory]
    [InlineData(UAuthMode.PureOpaque)]
    [InlineData(UAuthMode.Hybrid)]
    [InlineData(UAuthMode.SemiHybrid)]
    [InlineData(UAuthMode.PureJwt)]
    public void ValidExplicitMode_ShouldSucceed(UAuthMode mode)
    {
        var options = ValidOptions(mode);

        AssertValid(options);
    }

    [Fact]
    public void DefaultResourceProfile_WithoutJwt_ShouldSucceed()
    {
        var options = ValidOptions(UAuthMode.Hybrid);
        options.Auth.Mode = null;
        options.Auth.Jwt = null;

        AssertValid(options);
    }

    [Fact]
    public void DefaultResourceProfile_WithoutHubBaseUrl_ShouldFail()
    {
        var options = ValidOptions(UAuthMode.Hybrid);
        options.Auth.Mode = null;
        options.Auth.Jwt = null;
        options.Hub.BaseUrl = string.Empty;

        AssertInvalid(options, "Hub.BaseUrl is required for remote session validation.");
    }

    [Fact]
    public void CustomResolver_ShouldDetermineValidationMode()
    {
        var resolver = new Mock<IEffectiveAuthModeResolver>();

        resolver
            .Setup(x => x.Resolve(It.IsAny<AuthModeResolutionContext>()))
            .Returns(UAuthMode.PureOpaque);

        var options = ValidOptions(UAuthMode.PureOpaque);
        options.Auth.Mode = null;
        options.Auth.Jwt = null;

        AssertValid(options, resolver.Object);

        resolver.Verify(
            x => x.Resolve(It.Is<AuthModeResolutionContext>(
                c => c.ApplicationProfile == UAuthApplicationProfile.ResourceApi)),
            Times.Once);
    }

    [Fact]
    public void ExplicitMode_ShouldNotInvokeCustomResolver()
    {
        var resolver = new Mock<IEffectiveAuthModeResolver>(
            MockBehavior.Strict);

        var options = ValidOptions(UAuthMode.PureOpaque);

        AssertValid(options, resolver.Object);

        resolver.VerifyNoOtherCalls();
    }

    [Fact]
    public void NullOptions_ShouldFail()
    {
        AssertInvalid(null, "Resource API options cannot be null.");
    }

    [Fact]
    public void NullAuth_ShouldFail()
    {
        var options = ValidOptions();
        options.Auth = null!;

        AssertInvalid(options, "Resource API Auth options cannot be null.");
    }

    [Theory]
    [InlineData("Hub", "Hub must be configured.")]
    [InlineData("Cors", "Cors must be configured.")]
    [InlineData("MultiTenant", "MultiTenant must be configured.")]
    public void MissingRootSection_ShouldFail(
        string section,
        string expectedError)
    {
        var options = ValidOptions();

        switch (section)
        {
            case "Hub":
                options.Hub = null!;
                break;
            case "Cors":
                options.Cors = null!;
                break;
            case "MultiTenant":
                options.MultiTenant = null!;
                break;
        }

        AssertInvalid(options, expectedError);
    }

    [Fact]
    public void MissingRootSections_ShouldReportAllErrors()
    {
        var options = ValidOptions();

        options.Hub = null!;
        options.Cors = null!;
        options.MultiTenant = null!;

        AssertErrors(
            options,
            "Hub must be configured.",
            "Cors must be configured.",
            "MultiTenant must be configured.");
    }

    [Theory]
    [InlineData("Session", "Auth.Session must be configured.")]
    [InlineData("AccessToken", "Auth.AccessToken must be configured.")]
    [InlineData("Device", "Auth.Device must be configured.")]
    public void MissingAuthSection_ShouldFail(
        string section,
        string expectedError)
    {
        var options = ValidOptions();

        switch (section)
        {
            case "Session":
                options.Auth.Session = null!;
                break;
            case "AccessToken":
                options.Auth.AccessToken = null!;
                break;
            case "Device":
                options.Auth.Device = null!;
                break;
        }

        AssertInvalid(options, expectedError);
    }

    [Fact]
    public void MissingAuthSections_ShouldReportAllErrors()
    {
        var options = ValidOptions();

        options.Auth.Session = null!;
        options.Auth.AccessToken = null!;
        options.Auth.Device = null!;

        AssertErrors(
            options,
            "Auth.Session must be configured.",
            "Auth.AccessToken must be configured.",
            "Auth.Device must be configured.");
    }

    [Fact]
    public void InvalidSessionDeliveryMode_ShouldFail()
    {
        var options = ValidOptions();
        options.Auth.Session.DeliveryMode = (GrantDeliveryMode)999;

        AssertInvalid(
            options,
            "Auth.Session.DeliveryMode is invalid.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MissingSessionCookieName_ShouldFail(string? value)
    {
        var options = ValidOptions();
        options.Auth.Session.CookieName = value!;

        AssertInvalid(
            options,
            "Auth.Session.CookieName is required.");
    }

    [Fact]
    public void InvalidAccessTokenDeliveryMode_ShouldFail()
    {
        var options = ValidOptions();
        options.Auth.AccessToken.DeliveryMode = (GrantDeliveryMode)999;

        AssertInvalid(
            options,
            "Auth.AccessToken.DeliveryMode is invalid.");
    }

    [Fact]
    public void InvalidAccessTokenHeaderFormat_ShouldFail()
    {
        var options = ValidOptions();
        options.Auth.AccessToken.HeaderFormat = (HeaderTokenFormat)999;

        AssertInvalid(
            options,
            "Auth.AccessToken.HeaderFormat is invalid.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MissingAccessTokenCookieName_ShouldFail(string? value)
    {
        var options = ValidOptions();
        options.Auth.AccessToken.CookieName = value!;

        AssertInvalid(
            options,
            "Auth.AccessToken.CookieName is required.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MissingAccessTokenHeaderName_ShouldFail(string? value)
    {
        var options = ValidOptions();
        options.Auth.AccessToken.HeaderName = value!;

        AssertInvalid(
            options,
            "Auth.AccessToken.HeaderName is required.");
    }

    [Fact]
    public void InvalidDeviceMismatchBehavior_ShouldFail()
    {
        var options = ValidOptions();
        options.Auth.Device.MismatchBehavior =
            (DeviceMismatchBehavior)999;

        AssertInvalid(
            options,
            "Auth.Device.MismatchBehavior is invalid.");
    }

    [Fact]
    public void PureJwt_DeviceRebinding_ShouldFail()
    {
        var options = ValidOptions(UAuthMode.PureJwt);

        options.Auth.Device.MismatchBehavior =
            DeviceMismatchBehavior.AllowAndRebind;

        AssertInvalid(
            options,
            "PureJwt does not support device rebinding.");
    }

    [Fact]
    public void PureOpaque_DeviceRebinding_ShouldPassCurrentValidation()
    {
        var options = ValidOptions(UAuthMode.PureOpaque);

        options.Auth.Device.MismatchBehavior =
            DeviceMismatchBehavior.AllowAndRebind;

        AssertValid(options);
    }

    [Fact]
    public void PureJwt_MissingJwt_ShouldFail()
    {
        var options = ValidOptions(UAuthMode.PureJwt);
        options.Auth.Jwt = null;

        AssertInvalid(
            options,
            "Auth.Jwt is required in PureJwt mode.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void PureJwt_MissingIssuer_ShouldFail(string? value)
    {
        var options = ValidOptions(UAuthMode.PureJwt);
        options.Auth.Jwt = Jwt(issuer: value!);

        AssertInvalid(
            options,
            "Auth.Jwt.Issuer is required.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void PureJwt_MissingAudience_ShouldFail(string? value)
    {
        var options = ValidOptions(UAuthMode.PureJwt);
        options.Auth.Jwt = Jwt(audience: value!);

        AssertInvalid(
            options,
            "Auth.Jwt.Audience is required.");
    }

    [Fact]
    public void PureJwt_NullAlgorithms_ShouldFail()
    {
        var options = ValidOptions(UAuthMode.PureJwt);
        options.Auth.Jwt = new UAuthJwtValidationOptions
        {
            Issuer = "UAuth",
            Audience = "UAuthClient",
            ValidAlgorithms = null!
        };

        AssertInvalid(
            options,
            "Auth.Jwt.ValidAlgorithms must contain valid algorithms.");
    }

    [Fact]
    public void PureJwt_EmptyAlgorithms_ShouldFail()
    {
        var options = ValidOptions(UAuthMode.PureJwt);
        options.Auth.Jwt = Jwt(algorithms: []);

        AssertInvalid(
            options,
            "Auth.Jwt.ValidAlgorithms must contain valid algorithms.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void PureJwt_InvalidAlgorithmEntry_ShouldFail(string? value)
    {
        var options = ValidOptions(UAuthMode.PureJwt);
        options.Auth.Jwt = Jwt(algorithms: ["HS256", value!]);

        AssertInvalid(
            options,
            "Auth.Jwt.ValidAlgorithms must contain valid algorithms.");
    }

    [Fact]
    public void PureJwt_NegativeClockSkew_ShouldFail()
    {
        var options = ValidOptions(UAuthMode.PureJwt);

        options.Auth.Jwt = Jwt(
            clockSkew: TimeSpan.FromSeconds(-1));

        AssertInvalid(
            options,
            "Auth.Jwt.ClockSkew cannot be negative.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void PureJwt_InvalidMaximumTokenSize_ShouldFail(int value)
    {
        var options = ValidOptions(UAuthMode.PureJwt);

        options.Auth.Jwt = Jwt(maximumTokenSize: value);

        AssertInvalid(
            options,
            "Auth.Jwt.MaximumTokenSizeInBytes must be positive.");
    }

    [Fact]
    public void PureJwt_ZeroClockSkewAndPositiveTokenSize_ShouldSucceed()
    {
        var options = ValidOptions(UAuthMode.PureJwt);

        options.Auth.Jwt = Jwt(
            clockSkew: TimeSpan.Zero,
            maximumTokenSize: 1);

        AssertValid(options);
    }

    [Fact]
    public void PureJwt_ShouldNotRequireHubBaseUrl()
    {
        var options = ValidOptions(UAuthMode.PureJwt);
        options.Hub.BaseUrl = string.Empty;

        AssertValid(options);
    }

    [Fact]
    public void PureOpaque_ShouldNotRequireJwt()
    {
        var options = ValidOptions(UAuthMode.PureOpaque);
        options.Auth.Jwt = null;

        AssertValid(options);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SessionMode_MissingHubBaseUrl_ShouldFail(string? value)
    {
        var options = ValidOptions();
        options.Hub.BaseUrl = value!;

        AssertInvalid(
            options,
            "Hub.BaseUrl is required for remote session validation.");
    }

    [Theory]
    [InlineData("relative/path")]
    [InlineData("not-a-url")]
    [InlineData("ftp://hub.example.com")]
    [InlineData("file:///tmp/hub")]
    public void SessionMode_InvalidHubBaseUrl_ShouldFail(string value)
    {
        var options = ValidOptions();
        options.Hub.BaseUrl = value;

        AssertInvalid(
            options,
            "Hub.BaseUrl must be an absolute HTTP(S) URL.");
    }

    [Theory]
    [InlineData("https://hub.example.com")]
    [InlineData("http://localhost:6110")]
    public void SessionMode_ValidHubBaseUrl_ShouldSucceed(string value)
    {
        var options = ValidOptions();
        options.Hub.BaseUrl = value;

        AssertValid(options);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MissingCorsPolicyName_ShouldFail(string? value)
    {
        var options = ValidOptions();
        options.Cors.PolicyName = value!;

        AssertInvalid(
            options,
            "Cors.PolicyName is required.");
    }

    [Fact]
    public void NullAllowedOrigins_ShouldFail()
    {
        var options = ValidOptions();
        options.Cors.AllowedOrigins = null!;

        AssertInvalid(
            options,
            "Cors.AllowedOrigins must not be null.");
    }

    [Fact]
    public void EmptyAllowedOrigins_ShouldSucceed()
    {
        var options = ValidOptions();
        options.Cors.AllowedOrigins = [];

        AssertValid(options);
    }

    [Fact]
    public void MultipleInvalidFields_ShouldReportAllApplicableErrors()
    {
        var options = ValidOptions(UAuthMode.PureJwt);

        options.Auth.Session.CookieName = "";
        options.Auth.AccessToken.HeaderName = "";
        options.Auth.Device.MismatchBehavior =
            DeviceMismatchBehavior.AllowAndRebind;
        options.Auth.Jwt = Jwt(
            issuer: "",
            audience: "",
            algorithms: [],
            clockSkew: TimeSpan.FromSeconds(-1),
            maximumTokenSize: 0);
        options.Cors.PolicyName = "";

        AssertErrors(
            options,
            "Auth.Session.CookieName is required.",
            "Auth.AccessToken.HeaderName is required.",
            "PureJwt does not support device rebinding.",
            "Auth.Jwt.Issuer is required.",
            "Auth.Jwt.Audience is required.",
            "Auth.Jwt.ValidAlgorithms must contain valid algorithms.",
            "Auth.Jwt.ClockSkew cannot be negative.",
            "Auth.Jwt.MaximumTokenSizeInBytes must be positive.",
            "Cors.PolicyName is required.");
    }

    [Fact]
    public void InvalidExplicitMode_ShouldFail()
    {
        var options = ValidOptions();
        options.Auth.Mode = (UAuthMode)999;

        AssertInvalid(
            options,
            "Invalid Resource API authentication mode");
    }

    [Fact]
    public void InvalidResolvedMode_ShouldFail()
    {
        var resolver = new Mock<IEffectiveAuthModeResolver>();

        resolver
            .Setup(x => x.Resolve(It.IsAny<AuthModeResolutionContext>()))
            .Returns((UAuthMode)999);

        var options = ValidOptions();
        options.Auth.Mode = null;

        AssertInvalid(
            options,
            "Invalid Resource API authentication mode",
            resolver.Object);
    }

    [Fact]
    public void ResolverException_ShouldBecomeValidationFailure()
    {
        var resolver = new Mock<IEffectiveAuthModeResolver>();

        resolver
            .Setup(x => x.Resolve(It.IsAny<AuthModeResolutionContext>()))
            .Throws(new InvalidOperationException("Resolver failed."));

        var options = ValidOptions();
        options.Auth.Mode = null;

        AssertInvalid(
            options,
            "Resolver failed.",
            resolver.Object);
    }

    private static UAuthResourceApiOptions ValidOptions(UAuthMode mode = UAuthMode.PureOpaque)
    {
        return new UAuthResourceApiOptions
        {
            Auth = new UAuthResourceAuthOptions
            {
                Mode = mode,
                Jwt = Jwt()
            },
            Hub = new UAuthResourceHubOptions
            {
                BaseUrl = "https://hub.example.com"
            }
        };
    }

    private static UAuthJwtValidationOptions Jwt(
        string issuer = "UAuth",
        string audience = "UAuthClient",
        IReadOnlyCollection<string>? algorithms = null,
        TimeSpan? clockSkew = null,
        int maximumTokenSize = 16 * 1024)
    {
        return new UAuthJwtValidationOptions
        {
            Issuer = issuer,
            Audience = audience,
            ValidAlgorithms = algorithms ?? ["HS256"],
            ClockSkew = clockSkew ?? TimeSpan.FromSeconds(30),
            MaximumTokenSizeInBytes = maximumTokenSize
        };
    }

    private static ValidateOptionsResult Validate(
        UAuthResourceApiOptions? options,
        IEffectiveAuthModeResolver? resolver = null)
    {
        var validator = new UAuthResourceApiOptionsValidator(
            resolver ?? DefaultResolver);

        return validator.Validate(null, options!);
    }

    private static void AssertValid(
        UAuthResourceApiOptions options,
        IEffectiveAuthModeResolver? resolver = null)
    {
        var result = Validate(options, resolver);

        Assert.True(
            result.Succeeded,
            string.Join("; ", result.Failures ?? []));
    }

    private static void AssertInvalid(
        UAuthResourceApiOptions? options,
        string expectedError,
        IEffectiveAuthModeResolver? resolver = null)
    {
        var result = Validate(options, resolver);

        Assert.True(result.Failed);
        Assert.Contains(
            result.Failures!,
            error => error.Contains(
                expectedError,
                StringComparison.Ordinal));
    }

    private static void AssertErrors(
        UAuthResourceApiOptions options,
        params string[] expectedErrors)
    {
        var result = Validate(options);

        Assert.True(result.Failed);
        Assert.Equal(
            expectedErrors,
            result.Failures!.ToArray());
    }
}
