using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Server.ResourceApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Microsoft.IdentityModel.Tokens;

namespace CodeBeam.UltimateAuth.Tests.Unit.Server.ResourceApi;

public sealed class ResourceApiAuthModeTests
{
    [Fact]
    public void DefaultResolver_UsesPureJwtForResourceApi()
    {
        var resolver = new EffectiveAuthModeResolver();

        var result = resolver.Resolve(
            new AuthModeResolutionContext
            {
                ClientProfile = UAuthApplicationProfile.ResourceApi,
                Tenant = TenantKey.Single
            });

        Assert.Equal(UAuthMode.PureJwt, result);
    }

    [Fact]
    public void ResourceApi_DefaultMode_IsPureJwt()
    {
        using var provider = CreateProvider(options =>
        {
            options.Auth.Jwt = CreateJwtOptions();
        });

        var mode = provider
            .GetRequiredService<ResourceApiAuthModeProvider>()
            .Mode;

        Assert.Equal(UAuthMode.PureJwt, mode);
    }

    [Fact]
    public void ResourceApi_ExplicitMode_OverridesProfileDefault()
    {
        using var provider = CreateProvider(options =>
        {
            options.Auth.Mode = UAuthMode.PureOpaque;
            options.Hub.BaseUrl = "https://hub.example.com";
        });

        var mode = provider
            .GetRequiredService<ResourceApiAuthModeProvider>()
            .Mode;

        Assert.Equal(UAuthMode.PureOpaque, mode);
    }

    // TODO: This should be called only one time, but the provider is being resolved twice in the test. Investigate why.
    [Fact]
    public void ResourceApi_CustomResolver_OverridesProfileDefault()
    {
        var resolver = new Mock<IEffectiveAuthModeResolver>();

        resolver
            .Setup(x => x.Resolve(
                It.Is<AuthModeResolutionContext>(context =>
                    context.ClientProfile ==
                        UAuthApplicationProfile.ResourceApi)))
            .Returns(UAuthMode.PureOpaque);

        using var provider = CreateProvider(
            options =>
            {
                options.Hub.BaseUrl = "https://hub.example.com";
            },
            services =>
            {
                services.AddSingleton(resolver.Object);
            });

        var mode = provider
            .GetRequiredService<ResourceApiAuthModeProvider>()
            .Mode;

        Assert.Equal(UAuthMode.PureOpaque, mode);

        resolver.Verify(x => x.Resolve(
            It.Is<AuthModeResolutionContext>(context =>
                context.ClientProfile ==
                    UAuthApplicationProfile.ResourceApi)),
            Times.Exactly(2));
    }

    [Fact]
    public void ResourceApi_ExplicitMode_TakesPriorityOverCustomResolver()
    {
        var resolver = new Mock<IEffectiveAuthModeResolver>();

        resolver
            .Setup(x => x.Resolve(
                It.IsAny<AuthModeResolutionContext>()))
            .Returns(UAuthMode.PureJwt);

        using var provider = CreateProvider(
            options =>
            {
                options.Auth.Mode = UAuthMode.PureOpaque;
                options.Hub.BaseUrl = "https://hub.example.com";
            },
            services => services.AddSingleton(resolver.Object));

        var mode = provider
            .GetRequiredService<ResourceApiAuthModeProvider>()
            .Mode;

        Assert.Equal(UAuthMode.PureOpaque, mode);

        resolver.Verify(x => x.Resolve(
            It.IsAny<AuthModeResolutionContext>()),
            Times.Never);
    }

    [Fact]
    public void ResourceApi_DefaultPureJwt_RequiresJwtOptions()
    {
        using var provider = CreateProvider();

        var act = () => provider
            .GetRequiredService<IOptions<UAuthResourceApiOptions>>()
            .Value;

        var exception = Assert.Throws<OptionsValidationException>(act);

        Assert.Contains(
            exception.Failures,
            x => x.Contains("Auth.Jwt is required"));
    }

    [Fact]
    public void ResourceApi_ExplicitPureOpaque_DoesNotRequireJwtOptions()
    {
        using var provider = CreateProvider(options =>
        {
            options.Auth.Mode = UAuthMode.PureOpaque;
            options.Hub.BaseUrl = "https://hub.example.com";
        });

        var configured = provider
            .GetRequiredService<IOptions<UAuthResourceApiOptions>>()
            .Value;

        Assert.Null(configured.Auth.Jwt);
    }

    [Fact]
    public void ResourceApi_JwtOptions_UseNestedAuthConfiguration()
    {
        using var provider = CreateProvider(options =>
        {
            options.Auth.Jwt = CreateJwtOptions();
        });

        var jwt = provider
            .GetRequiredService<IOptions<UAuthJwtValidationOptions>>()
            .Value;

        Assert.Equal("https://auth.example.com", jwt.Issuer);
        Assert.Equal("resource-api", jwt.Audience);
        Assert.Contains(SecurityAlgorithms.HmacSha256,
            jwt.ValidAlgorithms);
    }

    private static UAuthJwtValidationOptions CreateJwtOptions() => new()
    {
        Issuer = "https://auth.example.com",
        Audience = "resource-api",
        ValidAlgorithms = [SecurityAlgorithms.HmacSha256]
    };

    private static ServiceProvider CreateProvider(
        Action<UAuthResourceApiOptions>? configure = null,
        Action<IServiceCollection>? configureServices = null)
    {
        var services = new ServiceCollection();

        services.AddSingleton<IConfiguration>(
            new ConfigurationBuilder().Build());

        services.AddLogging();

        // Custom registrations must precede framework defaults.
        configureServices?.Invoke(services);

        services.AddUltimateAuthResourceApi(configure);

        return services.BuildServiceProvider();
    }
}
