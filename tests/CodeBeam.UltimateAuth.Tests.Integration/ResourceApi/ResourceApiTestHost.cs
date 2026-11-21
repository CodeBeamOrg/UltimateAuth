using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Integration.ResourceApi;

internal sealed class ResourceApiTestHost : IAsyncDisposable
{
    private readonly WebApplication _app;

    public HttpClient Client { get; }

    private ResourceApiTestHost(
        WebApplication app,
        HttpClient client)
    {
        _app = app;
        Client = client;
    }

    public static async Task<ResourceApiTestHost> CreateAsync(
        ISessionValidator sessionValidator,
        Action<IServiceCollection>? configureServices = null,
        Action<UAuthResourceApiOptions>? configureResourceApi = null)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.AddControllers()
            .AddApplicationPart(typeof(TestResourceController).Assembly);

        builder.Services.AddUltimateAuthResourceApi(options =>
        {
            options.Hub.BaseUrl = "https://uauth.test";
            options.Auth.Mode = UAuthMode.PureOpaque;
            options.Auth.Session.DeliveryMode = GrantDeliveryMode.Header;

            configureResourceApi?.Invoke(options);
        });

        builder.Services.AddScoped<ISessionValidator>(
            _ => sessionValidator);

        var applicationProfile = UAuthApplicationProfile.BlazorServer;

        var profileReader = new Mock<IApplicationProfileReader>();

        profileReader
            .Setup(x => x.ReadAsync(It.IsAny<HttpContext>()))
            .ReturnsAsync(applicationProfile);

        builder.Services.AddSingleton(profileReader.Object);

        var effective = new EffectiveUAuthServerOptions
        {
            Mode = UAuthMode.PureOpaque,
            Options = new UAuthServerOptions()
        };

        var effectiveOptionsProvider =
            new Mock<IEffectiveServerOptionsProvider>();

        effectiveOptionsProvider
            .Setup(x => x.GetEffective(
                It.IsAny<TenantKey>(),
                applicationProfile))
            .Returns(effective);

        builder.Services.AddSingleton(
            effectiveOptionsProvider.Object);

        // Resource API tests authenticate with:
        // Authorization: Bearer <session-id>
        var authenticationResponse = new EffectiveAuthResponse(
            new EffectiveGrantDelivery(
                GrantKind.Session,
                TokenFormat.Opaque,
                GrantDeliveryMode.Header,
                name: "Authorization",
                headerFormat: HeaderTokenFormat.Bearer),
            EffectiveGrantDelivery.Disabled(GrantKind.AccessToken),
            EffectiveGrantDelivery.Disabled(GrantKind.RefreshToken),
            EffectiveRedirectResponse.Disabled);

        var authResponseResolver = new Mock<IAuthResponseResolver>();

        authResponseResolver
            .Setup(x => x.ResolveForAuthentication(
                UAuthMode.PureOpaque,
                applicationProfile,
                effective))
            .Returns(authenticationResponse);

        builder.Services.AddSingleton(
            authResponseResolver.Object);

        // Allow individual tests to override registrations.
        configureServices?.Invoke(builder.Services);

        var app = builder.Build();

        app.UseUltimateAuthResourceApiWithAspNetCore();

        app.MapControllers();

        await app.StartAsync();

        return new ResourceApiTestHost(
            app,
            app.GetTestClient());
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();

        await _app.StopAsync();
        await _app.DisposeAsync();
    }
}
