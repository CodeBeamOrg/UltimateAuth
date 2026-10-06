using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Runtime;
using CodeBeam.UltimateAuth.Policies.Abstractions;
using CodeBeam.UltimateAuth.Server;
using CodeBeam.UltimateAuth.Server.Authentication;
using CodeBeam.UltimateAuth.Server.Authorization;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Server.ResourceApi;
using CodeBeam.UltimateAuth.Server.Runtime;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Tests.Unit.Server.ResourceApi;

public sealed class UAuthResourceApiRegistrationTests
{
    [Fact]
    public void AddUltimateAuthResourceApi_ShouldRegisterResourceRuntimeMarker()
    {
        using var provider = CreateProvider();

        var marker = provider.GetRequiredService<IUAuthRuntimeMarker>();

        marker.Should().BeOfType<ResourceRuntimeMarker>();
    }

    [Fact]
    public void AddUltimateAuthResourceApi_ShouldRegisterRemoteSessionValidator()
    {
        using var provider = CreateProvider();

        using var scope = provider.CreateScope();

        var validator =
            scope.ServiceProvider.GetRequiredService<ISessionValidator>();

        validator.Should().BeOfType<RemoteSessionValidator>();
    }

    [Fact]
    public void AddUltimateAuthResourceApi_ShouldRegisterResourceAccessOrchestrator()
    {
        using var provider = CreateProvider();

        using var scope = provider.CreateScope();

        var orchestrator =
            scope.ServiceProvider.GetRequiredService<IAccessOrchestrator>();

        orchestrator.Should()
            .BeOfType<UAuthResourceAccessOrchestrator>();
    }

    [Fact]
    public void AddUltimateAuthResourceApi_ShouldRegisterTransportCredentialResolver()
    {
        using var provider = CreateProvider();

        using var scope = provider.CreateScope();

        var resolver =
            scope.ServiceProvider.GetRequiredService<ITransportCredentialResolver>();

        resolver.Should()
            .BeOfType<TransportCredentialResolver>();
    }

    [Fact]
    public void AddUltimateAuthResourceApi_ShouldRegisterDeviceContextFactory()
    {
        using var provider = CreateProvider();

        using var scope = provider.CreateScope();

        var factory =
            scope.ServiceProvider.GetRequiredService<IDeviceContextFactory>();

        factory.Should()
            .BeOfType<DeviceContextFactory>();
    }

    [Fact]
    public void AddUltimateAuthResourceApi_ShouldRegisterAuthorizationInfrastructure()
    {
        using var provider = CreateProvider();

        using var scope = provider.CreateScope();

        provider
            .GetRequiredService<IAuthorizationPolicyProvider>()
            .Should()
            .BeOfType<UAuthPolicyProvider>();

        scope.ServiceProvider
            .GetServices<IAuthorizationHandler>()
            .Should()
            .Contain(x => x is UAuthAuthorizationHandler);
    }

    [Fact]
    public void AddUltimateAuthResourceApi_ShouldConfigureUltimateAuthAsDefaultAuthenticationScheme()
    {
        using var provider = CreateProvider();

        var options =
            provider.GetRequiredService<IOptions<AuthenticationOptions>>()
                .Value;

        options.DefaultAuthenticateScheme.Should()
            .Be(UAuthConstants.SchemeDefaults.GlobalScheme);

        options.DefaultChallengeScheme.Should()
            .Be(UAuthConstants.SchemeDefaults.GlobalScheme);
    }

    [Fact]
    public async Task AddUltimateAuthResourceApi_ShouldRegisterAuthenticationScheme()
    {
        using var provider = CreateProvider();

        var schemeProvider = provider.GetRequiredService<IAuthenticationSchemeProvider>();

        var scheme = await schemeProvider.GetSchemeAsync(UAuthConstants.SchemeDefaults.GlobalScheme);

        scheme.Should().NotBeNull();

        scheme!.HandlerType.Should().Be(typeof(UAuthResourceAuthenticationHandler));
    }

    [Fact]
    public void AddUltimateAuthResourceApi_ShouldRegisterResourceUserAccessor()
    {
        using var provider = CreateProvider();

        using var scope = provider.CreateScope();

        var accessor =
            scope.ServiceProvider.GetRequiredService<IUserAccessor<UserKey>>();

        accessor.Should()
            .BeOfType<ResourceUserAccessor<UserKey>>();
    }

    [Fact]
    public void AddUltimateAuthResourceApi_ShouldRegisterAccessPolicyProvider()
    {
        using var provider = CreateProvider();

        using var scope = provider.CreateScope();

        var policyProvider =
            scope.ServiceProvider.GetRequiredService<IAccessPolicyProvider>();

        policyProvider.Should()
            .BeOfType<AccessPolicyProvider>();
    }

    [Fact]
    public void AddUltimateAuthResourceApi_WhenNoTenantResolverIsEnabled_ShouldUseNullTenantResolver()
    {
        using var provider = CreateProvider(options =>
        {
            options.MultiTenant.EnableRoute = false;
            options.MultiTenant.EnableHeader = false;
            options.MultiTenant.EnableDomain = false;
        });

        var resolver =
            provider.GetRequiredService<ITenantIdResolver>();

        resolver.Should()
            .BeOfType<NullTenantResolver>();
    }

    [Fact]
    public async Task AddUltimateAuthResourceApi_WhenNoTenantResolverIsEnabled_ShouldResolveNoTenant()
    {
        using var provider = CreateProvider(options =>
        {
            options.MultiTenant.EnableRoute = false;
            options.MultiTenant.EnableHeader = false;
            options.MultiTenant.EnableDomain = false;
        });

        var resolver =
            provider.GetRequiredService<ITenantIdResolver>();

        var tenantId = await resolver.ResolveTenantIdAsync(
            TenantResolutionContext.Empty);

        tenantId.Should().BeNull();
    }

    [Fact]
    public void AddUltimateAuthResourceApi_WhenAllowedOriginsConfigured_ShouldRegisterCorsPolicy()
    {
        using var provider = CreateProvider(options =>
        {
            options.AllowedClientOrigins =
            [
                "https://client.example.com"
            ];
        });

        var options =
            provider.GetRequiredService<IOptions<Microsoft.AspNetCore.Cors.Infrastructure.CorsOptions>>()
                .Value;

        var policy = options.GetPolicy(
            provider.GetRequiredService<IOptions<UAuthResourceApiOptions>>()
                .Value
                .CorsPolicyName);

        policy.Should().NotBeNull();

        policy!.Origins.Should()
            .ContainSingle()
            .Which.Should()
            .Be("https://client.example.com");

        policy.AllowAnyHeader.Should().BeTrue();
        policy.AllowAnyMethod.Should().BeTrue();
        policy.SupportsCredentials.Should().BeTrue();
    }

    [Fact]
    public void AddUltimateAuthResourceApi_WithHubBaseUrl_ShouldResolveSessionValidator()
    {
        using var provider = CreateProvider(options =>
        {
            options.UAuthHubBaseUrl = "https://hub.example.com";
        });

        using var scope = provider.CreateScope();

        var act = () =>
            scope.ServiceProvider.GetRequiredService<ISessionValidator>();

        act.Should().NotThrow();

        act().Should()
            .BeOfType<RemoteSessionValidator>();
    }

    [Fact]
    public void AddUltimateAuthResourceApi_ShouldPreserveMultiTenantConfiguration()
    {
        var services = new ServiceCollection();

        var configuration = new ConfigurationBuilder()
            .Build();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();

        services.AddUltimateAuthResourceApi(options =>
        {
            options.UAuthHubBaseUrl = "https://uauth.test";

            options.MultiTenant.Enabled = true;
            options.MultiTenant.EnableHeader = true;
            options.MultiTenant.EnableRoute = false;
            options.MultiTenant.EnableDomain = false;
            options.MultiTenant.HeaderName = "X-Tenant";
        });

        using var provider = services.BuildServiceProvider();

        var options = provider
            .GetRequiredService<IOptions<UAuthResourceApiOptions>>()
            .Value;

        options.MultiTenant.Enabled.Should().BeTrue();
        options.MultiTenant.EnableHeader.Should().BeTrue();
        options.MultiTenant.EnableRoute.Should().BeFalse();
        options.MultiTenant.EnableDomain.Should().BeFalse();
        options.MultiTenant.HeaderName.Should().Be("X-Tenant");
    }

    [Fact]
    public async Task AddUltimateAuthResourceApi_WithHeaderTenantEnabled_ShouldResolveHeaderTenant()
    {
        var services = new ServiceCollection();

        var configuration = new ConfigurationBuilder()
            .Build();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();

        services.AddUltimateAuthResourceApi(options =>
        {
            options.UAuthHubBaseUrl = "https://uauth.test";

            options.MultiTenant.Enabled = true;
            options.MultiTenant.EnableHeader = true;
            options.MultiTenant.HeaderName = "X-Tenant";
        });

        using var provider = services.BuildServiceProvider();

        var resolver =
            provider.GetRequiredService<ITenantIdResolver>();

        var context = TenantResolutionContext.Create(
            headers: new Dictionary<string, string>
            {
                ["X-Tenant"] = "tenant-a"
            });

        var result =
            await resolver.ResolveTenantIdAsync(context);

        result.Should().Be("tenant-a");
    }


    private static ServiceProvider CreateProvider(Action<UAuthResourceApiOptions>? configure = null)
    {
        var services = new ServiceCollection();

        var configuration = new ConfigurationBuilder()
            .Build();

        services.AddSingleton<IConfiguration>(configuration);

        services.AddLogging();

        services.AddUltimateAuthResourceApi(options =>
        {
            options.UAuthHubBaseUrl = "https://uauth.test";

            configure?.Invoke(options);
        });

        return services.BuildServiceProvider();
    }
}
