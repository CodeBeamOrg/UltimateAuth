using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Options;

namespace CodeBeam.UltimateAuth.Tests.Unit.Server.ResourceApi;

public sealed class UAuthResourceApiOptionsTests
{
    [Fact]
    public void Defaults_ShouldBeInitializedCorrectly()
    {
        var options = new UAuthResourceApiOptions();

        Assert.NotNull(options.Auth);
        Assert.NotNull(options.Hub);
        Assert.NotNull(options.Cors);
        Assert.NotNull(options.MultiTenant);

        Assert.Null(options.Auth.Mode);
        Assert.Null(options.Auth.Jwt);

        Assert.Equal(GrantDeliveryMode.Cookie, options.Auth.Session.DeliveryMode);

        Assert.Equal("uas", options.Auth.Session.CookieName);

        Assert.Equal(GrantDeliveryMode.Header, options.Auth.AccessToken.DeliveryMode);

        Assert.Equal("uat", options.Auth.AccessToken.CookieName);
        Assert.Equal("Authorization", options.Auth.AccessToken.HeaderName);
        Assert.Equal(HeaderTokenFormat.Bearer, options.Auth.AccessToken.HeaderFormat);

        Assert.Equal(DeviceMismatchBehavior.Reject, options.Auth.Device.MismatchBehavior);

        Assert.Equal(string.Empty, options.Hub.BaseUrl);
        Assert.Equal("UAuthResource", options.Cors.PolicyName);
        Assert.Empty(options.Cors.AllowedOrigins);
    }

    [Fact]
    public void Clone_ShouldPreserveConfiguredValues()
    {
        var source = CreateConfiguredOptions();

        var clone = source.Clone();

        Assert.Equal(source.Auth.Mode, clone.Auth.Mode);

        Assert.Equal(
            source.Auth.Session.DeliveryMode,
            clone.Auth.Session.DeliveryMode);

        Assert.Equal(
            source.Auth.Session.CookieName,
            clone.Auth.Session.CookieName);

        Assert.Equal(
            source.Auth.AccessToken.DeliveryMode,
            clone.Auth.AccessToken.DeliveryMode);

        Assert.Equal(
            source.Auth.AccessToken.CookieName,
            clone.Auth.AccessToken.CookieName);

        Assert.Equal(
            source.Auth.AccessToken.HeaderName,
            clone.Auth.AccessToken.HeaderName);

        Assert.Equal(
            source.Auth.AccessToken.HeaderFormat,
            clone.Auth.AccessToken.HeaderFormat);

        Assert.Equal(
            source.Auth.Device.MismatchBehavior,
            clone.Auth.Device.MismatchBehavior);

        Assert.Equal(source.Hub.BaseUrl, clone.Hub.BaseUrl);

        Assert.Equal(source.Cors.PolicyName, clone.Cors.PolicyName);

        Assert.Equal(
            source.Cors.AllowedOrigins.Order(),
            clone.Cors.AllowedOrigins.Order());

        Assert.Equal(
            source.MultiTenant.Enabled,
            clone.MultiTenant.Enabled);

        Assert.Equal(
            source.MultiTenant.EnableHeader,
            clone.MultiTenant.EnableHeader);

        Assert.Equal(
            source.MultiTenant.HeaderName,
            clone.MultiTenant.HeaderName);

        Assert.NotNull(clone.Auth.Jwt);

        Assert.Equal(
            source.Auth.Jwt!.Issuer,
            clone.Auth.Jwt!.Issuer);

        Assert.Equal(
            source.Auth.Jwt.Audience,
            clone.Auth.Jwt.Audience);

        Assert.Equal(
            source.Auth.Jwt.ValidAlgorithms,
            clone.Auth.Jwt.ValidAlgorithms);

        Assert.Equal(
            source.Auth.Jwt.ClockSkew,
            clone.Auth.Jwt.ClockSkew);

        Assert.Equal(
            source.Auth.Jwt.MaximumTokenSizeInBytes,
            clone.Auth.Jwt.MaximumTokenSizeInBytes);
    }

    [Fact]
    public void Clone_ShouldCreateIndependentNestedObjects()
    {
        var source = CreateConfiguredOptions();

        var clone = source.Clone();

        Assert.NotSame(source, clone);
        Assert.NotSame(source.Auth, clone.Auth);
        Assert.NotSame(source.Auth.Session, clone.Auth.Session);
        Assert.NotSame(source.Auth.AccessToken, clone.Auth.AccessToken);
        Assert.NotSame(source.Auth.Device, clone.Auth.Device);
        Assert.NotSame(source.Auth.Jwt, clone.Auth.Jwt);
        Assert.NotSame(source.Hub, clone.Hub);
        Assert.NotSame(source.Cors, clone.Cors);
        Assert.NotSame(source.MultiTenant, clone.MultiTenant);
        Assert.NotSame(
            source.Cors.AllowedOrigins,
            clone.Cors.AllowedOrigins);
    }

    [Fact]
    public void Clone_ModifyingClone_ShouldNotModifySource()
    {
        var source = CreateConfiguredOptions();
        var clone = source.Clone();

        clone.Auth.Mode = UAuthMode.PureOpaque;
        clone.Auth.Session.CookieName = "changed-session";
        clone.Auth.AccessToken.CookieName = "changed-access";
        clone.Auth.AccessToken.HeaderName = "X-Changed";
        clone.Auth.Device.MismatchBehavior =
            DeviceMismatchBehavior.Reject;
        clone.Hub.BaseUrl = "https://other.example.com";
        clone.Cors.PolicyName = "ChangedPolicy";
        clone.Cors.AllowedOrigins.Add("https://new.example.com");
        clone.MultiTenant.HeaderName = "X-Other-Tenant";

        Assert.Equal(UAuthMode.PureJwt, source.Auth.Mode);
        Assert.Equal("session-custom", source.Auth.Session.CookieName);
        Assert.Equal("access-custom", source.Auth.AccessToken.CookieName);
        Assert.Equal("X-Access-Token", source.Auth.AccessToken.HeaderName);
        Assert.Equal(
            DeviceMismatchBehavior.Allow,
            source.Auth.Device.MismatchBehavior);
        Assert.Equal("https://hub.example.com", source.Hub.BaseUrl);
        Assert.Equal("CustomResourcePolicy", source.Cors.PolicyName);
        Assert.DoesNotContain(
            "https://new.example.com",
            source.Cors.AllowedOrigins);
        Assert.Equal("X-Tenant", source.MultiTenant.HeaderName);
    }

    [Fact]
    public void Clone_JwtAlgorithms_ShouldNotShareCollection()
    {
        var source = CreateConfiguredOptions();

        var clone = source.Clone();

        Assert.NotNull(clone.Auth.Jwt);

        Assert.NotSame(
            source.Auth.Jwt!.ValidAlgorithms,
            clone.Auth.Jwt!.ValidAlgorithms);

        Assert.Equal(
            source.Auth.Jwt.ValidAlgorithms,
            clone.Auth.Jwt.ValidAlgorithms);
    }

    [Fact]
    public void Clone_JwtAlgorithms_ShouldRemainIndependentAfterMutation()
    {
        var algorithms = new List<string> { "HS256", "RS256" };

        var source = CreateConfiguredOptions();

        source.Auth.Jwt = new UAuthJwtValidationOptions
        {
            Issuer = "UAuth",
            Audience = "UAuthClient",
            ValidAlgorithms = algorithms
        };

        var clone = source.Clone();

        algorithms.Add("ES256");

        Assert.Equal(
            new[] { "HS256", "RS256" },
            clone.Auth.Jwt!.ValidAlgorithms);

        Assert.Equal(3, source.Auth.Jwt.ValidAlgorithms.Count);
    }

    [Fact]
    public void Clone_WithNullJwt_ShouldKeepJwtNull()
    {
        var source = CreateConfiguredOptions();
        source.Auth.Jwt = null;

        var clone = source.Clone();

        Assert.Null(clone.Auth.Jwt);
        Assert.NotSame(source.Auth, clone.Auth);
    }

    [Fact]
    public void Clone_WithUnspecifiedMode_ShouldKeepModeUnspecified()
    {
        var source = CreateConfiguredOptions();
        source.Auth.Mode = null;

        var clone = source.Clone();

        Assert.Null(clone.Auth.Mode);
    }

    [Fact]
    public void Clone_CorsAllowedOrigins_ShouldPreserveComparer()
    {
        var source = CreateConfiguredOptions();

        source.Cors.AllowedOrigins = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase)
        {
            "https://client.example.com"
        };

        var clone = source.Clone();

        Assert.Contains("HTTPS://CLIENT.EXAMPLE.COM", clone.Cors.AllowedOrigins);

        Assert.NotSame(
            source.Cors.AllowedOrigins,
            clone.Cors.AllowedOrigins);
    }

    [Fact]
    public void Clone_CorsAllowedOrigins_ShouldRemainIndependent()
    {
        var source = CreateConfiguredOptions();

        var clone = source.Clone();

        clone.Cors.AllowedOrigins.Clear();

        Assert.NotEmpty(source.Cors.AllowedOrigins);
        Assert.Empty(clone.Cors.AllowedOrigins);
    }

    [Fact]
    public void Clone_ShouldSupportRepeatedCloning()
    {
        var source = CreateConfiguredOptions();

        var first = source.Clone();
        var second = first.Clone();

        Assert.NotSame(source.Auth, first.Auth);
        Assert.NotSame(first.Auth, second.Auth);

        Assert.Equal(
            source.Auth.Jwt!.Issuer,
            second.Auth.Jwt!.Issuer);

        Assert.Equal(
            source.Hub.BaseUrl,
            second.Hub.BaseUrl);
    }

    private static UAuthResourceApiOptions CreateConfiguredOptions()
    {
        return new UAuthResourceApiOptions
        {
            Auth = new UAuthResourceAuthOptions
            {
                Mode = UAuthMode.PureJwt,

                Session = new UAuthResourceSessionOptions
                {
                    DeliveryMode = GrantDeliveryMode.Cookie,
                    CookieName = "session-custom"
                },

                AccessToken = new UAuthResourceAccessTokenOptions
                {
                    DeliveryMode = GrantDeliveryMode.Cookie,
                    CookieName = "access-custom",
                    HeaderName = "X-Access-Token",
                    HeaderFormat = HeaderTokenFormat.Bearer
                },

                Device = new UAuthResourceDeviceOptions
                {
                    MismatchBehavior = DeviceMismatchBehavior.Allow
                },

                Jwt = new UAuthJwtValidationOptions
                {
                    Issuer = "CustomIssuer",
                    Audience = "CustomAudience",
                    ValidAlgorithms = ["HS256", "RS256"],
                    ClockSkew = TimeSpan.FromMinutes(2),
                    MaximumTokenSizeInBytes = 32768
                }
            },

            Hub = new UAuthResourceHubOptions
            {
                BaseUrl = "https://hub.example.com"
            },

            Cors = new UAuthResourceCorsOptions
            {
                PolicyName = "CustomResourcePolicy",
                AllowedOrigins =
                [
                    "https://client.example.com",
                    "https://admin.example.com"
                ]
            },

            MultiTenant = new UAuthMultiTenantOptions
            {
                Enabled = true,
                EnableHeader = true,
                EnableRoute = false,
                EnableDomain = false,
                HeaderName = "X-Tenant"
            }
        };
    }
}
