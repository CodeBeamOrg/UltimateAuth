using CodeBeam.UltimateAuth.IdentityLab.Authorization;
using CodeBeam.UltimateAuth.IdentityLab.Endpoints;
using CodeBeam.UltimateAuth.IdentityLab.Services;
using CodeBeam.UltimateAuth.Server.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace CodeBeam.UltimateAuth.IdentityLab.Tests.Infrastructure;

internal sealed class LabHttpHost(WebApplication app) : IAsyncDisposable
{
    public IServiceProvider Services => app.Services;
    public static async Task<LabHttpHost> CreateAsync(LabDatabase database)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        LabRuntime.ConfigureServices(builder.Services, database);
        builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
        builder.Services.AddScoped<LabAccessContextProvider>();
        builder.Services.AddScoped<LabManagementAuthorizationFilter>();
        builder.Services.AddScoped<LabAdminAuthorizationFilter>();
        builder.Services.AddScoped<ILabAdminGuard, LabAdminGuard>();
        builder.Services.AddScoped<SandboxService>();
        builder.Services.AddScoped<SandboxAdminService>();
        builder.Services.AddScoped<SandboxAccessService>();
        builder.Services.AddScoped<LabUserProvisioningService>();
        builder.Services.AddScoped<SandboxUserService>();
        builder.Services.AddScoped<ActivityLogService>();
        builder.Services.AddScoped<LabRepairService>();
        builder.Services.AddScoped<LabOwnerRepairService>();
        builder.Services.AddScoped<LabDemoRoleService>();
        builder.Services.AddScoped<LabDemoStoreService>();
        // SandboxUserService's delete path uses the SDK; these tests never invoke it.
        builder.Services.AddSingleton(Moq.Mock.Of<CodeBeam.UltimateAuth.Client.IUAuthClient>());
        var app = builder.Build();
        app.UseDeveloperExceptionPage();
        app.UseUltimateAuthWithAspNetCore();
        app.UseAntiforgery();
        app.MapUltimateAuthEndpoints();
        app.MapLabManagementEndpoints();
        app.MapLabAdminEndpoints();
        await LabRuntime.SeedAsync(app.Services);
        await app.StartAsync();
        return new(app);
    }

    public HttpClient Browser(string? device = "identitylab-browser")
    {
        var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false, CookieContainer = new CookieContainer() })
        {
            BaseAddress = new Uri(app.Urls.Single())
        };
        client.DefaultRequestHeaders.Add("X-UAuth-ClientProfile", "BlazorServer");
        if (device is not null) client.DefaultRequestHeaders.Add("X-UDID", device);
        return client;
    }

    public static async Task<HttpResponseMessage> PostAsync(HttpClient client, string path, object? payload = null)
    {
        var ct = TestContext.Current.CancellationToken;
        using var tokenResponse = await client.GetAsync("/auth/antiforgery/token", ct);
        Assert.True(tokenResponse.IsSuccessStatusCode, await tokenResponse.Content.ReadAsStringAsync(ct));
        var token = await tokenResponse.Content.ReadFromJsonAsync<Token>(ct);
        using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(payload ?? new { }) };
        request.Headers.Add(token!.HeaderName, token.RequestToken);
        return await client.SendAsync(request, ct);
    }

    public static async Task<string> LoginAsync(HttpClient client, string identifier, string secret)
    {
        using var response = await PostAsync(client, "/auth/login", new { identifier, secret });
        Assert.True(response.StatusCode == HttpStatusCode.Found,
            $"Login returned {response.StatusCode}: {await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)}");
        var session = response.Headers.GetValues("Set-Cookie").Select(x => x.Split(';', 2)[0]).Single(x => x.StartsWith("uas="));
        // Kestrel is HTTP in this fixture; explicitly send the Secure session cookie,
        // as the repository's other HTTP integration clients do. Production stays Secure.
        client.DefaultRequestHeaders.Remove("Cookie");
        client.DefaultRequestHeaders.Add("Cookie", session);
        return session;
    }

    private sealed record Token(string HeaderName, string RequestToken);
    public async ValueTask DisposeAsync() { await app.StopAsync(); await app.DisposeAsync(); }
}
