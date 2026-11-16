using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Contracts;
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

    public static async Task<ResourceApiTestHost> CreateAsync(ISessionValidator sessionValidator, Action<IServiceCollection>? configureServices = null, Action<UAuthResourceApiOptions>? configureResourceApi = null)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddControllers().AddApplicationPart(typeof(TestResourceController).Assembly);

        builder.Services.AddUltimateAuthResourceApi(options =>
        {
            options.UAuthHubBaseUrl = "https://uauth.test";
            configureResourceApi?.Invoke(options);
        });


        builder.Services.AddScoped<ISessionValidator>(
            _ => sessionValidator);

        // Temporary compatibility registrations for authorization tests.
        var profileReader = new Mock<IClientProfileReader>();

        profileReader
            .Setup(x => x.ReadAsync(It.IsAny<HttpContext>()))
            .ReturnsAsync(UAuthClientProfile.BlazorServer);

        builder.Services.AddSingleton(profileReader.Object);

        var effectiveOptionsProvider =
            new Mock<IEffectiveServerOptionsProvider>();

        effectiveOptionsProvider
            .Setup(x => x.GetEffective(
                It.IsAny<HttpContext>(),
                It.IsAny<UAuthClientProfile>()))
            .Returns(new EffectiveUAuthServerOptions
            {
                Mode = UAuthMode.PureOpaque,
                Options = new UAuthServerOptions()
            });

        builder.Services.AddSingleton(effectiveOptionsProvider.Object);

        configureServices?.Invoke(builder.Services);


        builder.Services.AddScoped<ISessionValidator>(_ => sessionValidator);

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
