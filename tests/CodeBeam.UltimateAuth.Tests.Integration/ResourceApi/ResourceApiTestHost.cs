using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Server;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

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
