using System.Net;
using System.Net.Http.Json;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Server.RateLimiting;
using CodeBeam.UltimateAuth.Tests.Integration.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Tests.Integration;

public class RateLimitingFlowTests
{
    [Fact]
    public async Task RejectedLoginDoesNotLockAccount_AndRejectedLogoutDoesNotRevokeSession()
    {
        using var factory = AuthServerFactory.Create(o =>
        {
            o.RateLimiting.Enabled = true;
            o.RateLimiting.Login.PermitLimit = 2;
            o.RateLimiting.Endpoints[UAuthEndpoint.Logout] = new() { PermitLimit = 1 };
            o.Login.MaxFailedAttempts = 1;
        });
        var user = await factory.CreateLoginUserAsync();
        HttpClient Client(string device)
        {
            var client = factory.CreateClient(new WebApplicationFactoryClientOptions
            { AllowAutoRedirect = false, HandleCookies = false });
            client.DefaultRequestHeaders.Add("Origin", "https://localhost:6130");
            client.DefaultRequestHeaders.Add("X-UDID", device);
            return client;
        }
        using var first = Client("rate-limit-flow-first-123456");
        using var second = Client("rate-limit-flow-second-123456");
        foreach (var client in new[] { first, second })
        {
            var login = await client.PostAsJsonAsync("/auth/login", new { identifier = user.Identifier, secret = user.Secret });
            Assert.True(login.Headers.TryGetValues("Set-Cookie", out var cookies));
            client.DefaultRequestHeaders.Add("Cookie", cookies!.First());
        }
        var rejected = await first.PostAsJsonAsync("/auth/login", new { identifier = user.Identifier, secret = "wrong" });
        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.False(rejected.Headers.Contains("Set-Cookie"));
        using (var scope = factory.Services.CreateScope())
        {
            var security = scope.ServiceProvider.GetRequiredService<IAuthenticationSecurityManager>();
            var factor = await security.GetOrCreateFactorAsync(TenantKey.Single, user.UserKey, CredentialType.Password);
            Assert.Equal(0, factor.FailedAttempts);
        }
        Assert.NotEqual(HttpStatusCode.TooManyRequests, (await first.PostAsync("/auth/logout", null)).StatusCode);
        // Two validated sessions of the same actor share logout capacity.
        var logout = await second.PostAsync("/auth/logout", null);
        Assert.Equal(HttpStatusCode.TooManyRequests, logout.StatusCode);
        Assert.False(logout.Headers.Contains("Set-Cookie"));
        // The rejected logout did not revoke the second session.
        var validation = await second.PostAsync("/auth/validate", null);
        Assert.Equal(HttpStatusCode.OK, validation.StatusCode);
        var result = await validation.Content.ReadFromJsonAsync<CodeBeam.UltimateAuth.Core.Contracts.AuthValidationResult>();
        Assert.True(result?.IsValid);
        Assert.NotNull(result!.Snapshot);
    }
}
