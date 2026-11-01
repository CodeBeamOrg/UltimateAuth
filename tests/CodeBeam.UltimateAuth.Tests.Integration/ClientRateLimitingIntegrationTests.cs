using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Client.Abstractions;
using CodeBeam.UltimateAuth.Client.Contracts;
using CodeBeam.UltimateAuth.Client.Diagnostics;
using CodeBeam.UltimateAuth.Client.Extensions;
using CodeBeam.UltimateAuth.Client.Infrastructure;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.RateLimiting;
using CodeBeam.UltimateAuth.Tests.Integration.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Tests.Integration;

public sealed class ClientRateLimitingIntegrationTests
{
    [Fact]
    public async Task NativeValidation429PreservesClientState_AndLaterRevocationStillClearsIt()
    {
        using var factory = AuthServerFactory.Create(o =>
        { o.RateLimiting.Enabled = true; o.RateLimiting.Endpoints[UAuthEndpoint.Validate] = new() { PermitLimit = 1 }; });
        var user = await factory.CreateLoginUserAsync();
        using var http = await Login(factory, user);
        var transport = new HttpTransport(http);
        using var services = ClientServices(factory, transport);
        using var scope = services.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<IUAuthStateManager>();
        await manager.EnsureAsync();
        Assert.True(manager.State.IsAuthenticated);
        var validatedAt = manager.State.LastValidatedAt;
        await manager.EnsureAsync(force: true);
        await manager.EnsureAsync(force: true);
        Assert.True(manager.State.IsAuthenticated);
        Assert.Equal(validatedAt, manager.State.LastValidatedAt);
        Assert.Equal(2, transport.Count);
        using (var serverScope = factory.Services.CreateScope())
        {
            var store = serverScope.ServiceProvider.GetRequiredService<ISessionStoreFactory>().Create(TenantKey.Single);
            await store.ExecuteAsync(ct => store.RevokeRootCascadeAsync(user.UserKey, factory.Clock.UtcNow, ct));
        }
        factory.Clock.Advance(TimeSpan.FromMinutes(2));
        await manager.EnsureAsync(force: true);
        Assert.False(manager.State.IsAuthenticated);
    }

    [Fact]
    public async Task NativeRefresh429PreservesCredentials_AndSuppressesImmediateRefreshLoop()
    {
        using var factory = AuthServerFactory.Create(o =>
        { o.RateLimiting.Enabled = true; o.RateLimiting.Endpoints[UAuthEndpoint.Refresh] = new() { PermitLimit = 1 }; });
        var user = await factory.CreateLoginUserAsync();
        using var http = await Login(factory, user);
        var credential = http.DefaultRequestHeaders.GetValues("Cookie").Single();
        var transport = new HttpTransport(http);
        using var services = ClientServices(factory, transport);
        using var scope = services.CreateScope();
        var flow = scope.ServiceProvider.GetRequiredService<IUAuthClient>().Flows;
        Assert.True((await flow.RefreshAsync()).IsSuccess);
        var throttle = await flow.RefreshAsync();
        Assert.Equal(429, throttle.Status);
        Assert.Equal("rate_limited", throttle.ErrorCode);
        Assert.Equal(RefreshOutcome.None, throttle.Outcome);
        Assert.Equal(credential, http.DefaultRequestHeaders.GetValues("Cookie").Single());
        await flow.RefreshAsync(isAuto: true);
        Assert.Equal(2, transport.Count);
        var session = await http.PostAsync("/auth/validate", null);
        Assert.Equal(HttpStatusCode.OK, session.StatusCode);
        Assert.True((await session.Content.ReadFromJsonAsync<CodeBeam.UltimateAuth.Core.Contracts.AuthValidationResult>())!.IsValid);
    }

    private static async Task<HttpClient> Login(AuthServerFactory factory, IntegrationTestUser user)
    {
        var http = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = false });
        http.DefaultRequestHeaders.Add("Origin", "https://localhost:6130");
        http.DefaultRequestHeaders.Add("X-UDID", "client-rate-device-123456789");
        http.DefaultRequestHeaders.Add("X-UAuth-ClientProfile", "BlazorServer");
        var login = await http.PostAsJsonAsync("/auth/login", new { identifier = user.Identifier, secret = user.Secret });
        Assert.True(login.Headers.TryGetValues("Set-Cookie", out var cookies));
        http.DefaultRequestHeaders.Add("Cookie", string.Join("; ", cookies!.Select(c => c.Split(';')[0])));
        return http;
    }

    private static ServiceProvider ClientServices(AuthServerFactory factory, HttpTransport transport)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddUltimateAuthClient(o => { o.ClientProfile = UAuthClientProfile.BlazorServer; o.AutoDetectClientProfile = false; });
        services.AddSingleton<UAuthClientDiagnostics>();
        services.AddSingleton<IClock>(factory.Clock);
        services.AddSingleton<IUAuthRequestClient>(transport);
        services.AddSingleton<IUAuthResourceRequestClient>(transport);
        services.AddSingleton<IClientDeviceProvider>(transport);
        services.AddSingleton<IReturnUrlProvider>(transport);
        return services.BuildServiceProvider();
    }

    // Thin HTTP test adapter: public SDK contracts against the real auth host and native middleware.
    private sealed class HttpTransport(HttpClient http) : IUAuthRequestClient, IUAuthResourceRequestClient, IClientDeviceProvider, IReturnUrlProvider
    {
        internal int Count { get; private set; }
        public Task NavigateAsync(string endpoint, IDictionary<string, string>? form = null, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<T> TryAndCommitAsync<T>(string tryEndpoint, string commitEndpoint, object request, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<DeviceContext> GetAsync() => Task.FromResult(DeviceContext.Anonymous());
        public string GetCurrentUrl() => "/";
        public async Task<UAuthTransportResult> SendFormAsync(string endpoint, IDictionary<string, string>? form = null, CancellationToken ct = default)
        {
            Count++;
            using var response = await http.PostAsync(endpoint, form is null ? null : new FormUrlEncodedContent(form), ct);
            return await Read(response, ct);
        }
        public async Task<UAuthTransportResult> SendJsonAsync(string endpoint, object? payload = null, CancellationToken ct = default)
        {
            Count++;
            using var response = await http.PostAsJsonAsync(endpoint, payload, ct);
            return await Read(response, ct);
        }
        private static async Task<UAuthTransportResult> Read(HttpResponseMessage response, CancellationToken ct)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            return new()
            {
                Status = (int)response.StatusCode,
                Ok = response.IsSuccessStatusCode,
                RetryAfter = response.Headers.TryGetValues("Retry-After", out var retry) ? retry.First() : null,
                RefreshOutcome = response.Headers.TryGetValues("X-UAuth-Refresh", out var outcome) ? outcome.First() : null,
                Body = string.IsNullOrWhiteSpace(body) ? null : JsonSerializer.Deserialize<JsonElement>(body)
            };
        }
        public Task<UAuthResourceTransportResult> SendAsync(string method, string endpoint, object? payload = null, CancellationToken ct = default) => throw new NotSupportedException();
    }
}
