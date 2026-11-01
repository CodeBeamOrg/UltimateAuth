using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Credentials;
using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Server.RateLimiting;
using CodeBeam.UltimateAuth.Tests.Integration.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CodeBeam.UltimateAuth.Tests.Integration;

public sealed class RateLimitingProofFlowTests
{
    private static readonly TimeSpan Window = TimeSpan.FromSeconds(2);

    [Fact]
    public async Task PkceCompletion_RejectedRequestPreservesAuthorizationProof()
    {
        using var factory = AuthServerFactory.Create(o =>
        { o.RateLimiting.Enabled = true; o.RateLimiting.Login = new() { PermitLimit = 1, Window = Window }; });
        var user = await factory.CreateLoginUserAsync();
        using var client = Client(factory);
        static string Encode(byte[] value) => Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var verifier = Encode(RandomNumberGenerator.GetBytes(32));
        var authorize = await client.PostAsync("/auth/pkce/authorize", new FormUrlEncodedContent(new Dictionary<string, string>
        { ["code_challenge"] = Encode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier))), ["challenge_method"] = "S256", ["redirect_uri"] = "/callback" }));
        Assert.Equal(HttpStatusCode.OK, authorize.StatusCode);
        var proof = await authorize.Content.ReadFromJsonAsync<PkceAuthorizeResponse>();
        Assert.NotNull(proof);
        // Malformed payload consumes request quota without supplying the valid authorization proof.
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/auth/pkce/complete", new FormUrlEncodedContent(new Dictionary<string, string>()))).StatusCode);
        Task<HttpResponseMessage> Complete() => client.PostAsync("/auth/pkce/complete", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["authorization_code"] = proof.AuthorizationCode,
            ["code_verifier"] = verifier,
            ["Identifier"] = user.Identifier,
            ["Secret"] = user.Secret,
            ["return_url"] = "/callback"
        }));
        var rejected = await Complete();
        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.False(rejected.Headers.Contains("Set-Cookie"));
        var completed = await AfterReplenishment(Complete);
        Assert.NotEqual(HttpStatusCode.TooManyRequests, completed.StatusCode);
        Assert.True(completed.Headers.Contains("Set-Cookie"));
    }

    [Fact]
    public async Task Refresh_RejectedRequestPreservesRefreshToken_ThenSameTokenRotates()
    {
        using var factory = AuthServerFactory.Create(o =>
        { o.RateLimiting.Enabled = true; o.RateLimiting.Endpoints[UAuthEndpoint.Refresh] = new() { PermitLimit = 1, Window = Window }; });
        var user = await factory.CreateLoginUserAsync();
        using var client = Client(factory);
        client.DefaultRequestHeaders.Add("X-UAuth-ClientProfile", "BlazorWasm");
        var login = await client.PostAsJsonAsync("/auth/login", new { identifier = user.Identifier, secret = user.Secret });
        var cookies = Cookies(login);
        var session = cookies.Split("; ").Single(c => c.StartsWith("uas="));
        var refresh = cookies.Split("; ").Single(c => c.StartsWith("uar="))[4..];
        using var scope = factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IRefreshTokenStoreFactory>().Create(TenantKey.Single);
        var hash = scope.ServiceProvider.GetRequiredService<ITokenHasher>().Hash(refresh);
        var before = await store.FindByHashAsync(hash);
        Assert.NotNull(before);
        client.DefaultRequestHeaders.Add("Cookie", session);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync("/auth/refresh", null)).StatusCode);
        client.DefaultRequestHeaders.Remove("Cookie");
        client.DefaultRequestHeaders.Add("Cookie", cookies);
        var rejected = await client.PostAsync("/auth/refresh", null);
        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.False(rejected.Headers.Contains("Set-Cookie"));
        Assert.Equal(before, await store.FindByHashAsync(hash));
        var renewed = await AfterReplenishment(() => client.PostAsync("/auth/refresh", null));
        Assert.Equal(HttpStatusCode.NoContent, renewed.StatusCode);
        Assert.True(renewed.Headers.Contains("Set-Cookie"));
        Assert.False((await store.FindByHashAsync(hash))!.IsActive(factory.Clock.UtcNow));
    }

    [Fact]
    public async Task Recovery_RejectedRequestPreservesResetProof_ThenSameProofChangesPassword()
    {
        var notifier = new RecordingResetNotifier();
        using var factory = AuthServerFactory.Create(o =>
        { o.RateLimiting.Enabled = true; o.RateLimiting.Endpoints[UAuthEndpoint.CredentialResetCompleteAnonymous] = new() { PermitLimit = 1, Window = Window }; }, services =>
        { services.RemoveAll<ICredentialResetNotifier>(); services.AddSingleton<ICredentialResetNotifier>(notifier); });
        var user = await factory.CreateLoginUserAsync();
        using var client = Client(factory);
        var begin = await client.PostAsJsonAsync("/auth/me/credentials/reset/begin", new BeginResetCredentialRequest
        { Identifier = user.Identifier, CredentialType = CredentialType.Password, ResetCodeType = ResetCodeType.Token });
        Assert.Equal(HttpStatusCode.OK, begin.StatusCode);
        Assert.NotNull(notifier.Last);
        // Consume quota using an unknown account, never presenting the issued proof.
        await client.PostAsJsonAsync("/auth/me/credentials/reset/complete", new CompleteResetCredentialRequest
        { Identifier = "unknown-account", CredentialType = CredentialType.Password, ResetToken = "invalid", NewSecret = "Changed-password-123!" });
        var request = new CompleteResetCredentialRequest
        { Identifier = user.Identifier, CredentialType = CredentialType.Password, ResetToken = notifier.Last.Token, NewSecret = "Changed-password-123!" };
        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.PostAsJsonAsync("/auth/me/credentials/reset/complete", request)).StatusCode);
        var completed = await AfterReplenishment(() => client.PostAsJsonAsync("/auth/me/credentials/reset/complete", request));
        Assert.Equal(HttpStatusCode.OK, completed.StatusCode);
        var login = await client.PostAsJsonAsync("/auth/login", new { identifier = user.Identifier, secret = request.NewSecret });
        Assert.True(login.Headers.Contains("Set-Cookie"));
    }

    [Fact]
    public async Task AuthenticatedUsersOnSameConnectionIpHaveIndependentValidationBudgets()
    {
        using var factory = AuthServerFactory.Create(o =>
        { o.RateLimiting.Enabled = true; o.RateLimiting.Endpoints[UAuthEndpoint.Validate] = new() { PermitLimit = 1 }; });
        using var first = Client(factory);
        using var second = Client(factory);
        foreach (var client in new[] { first, second })
        {
            var user = await factory.CreateLoginUserAsync();
            var login = await client.PostAsJsonAsync("/auth/login", new { identifier = user.Identifier, secret = user.Secret });
            client.DefaultRequestHeaders.Add("Cookie", Cookies(login));
            var validation = await client.PostAsync("/auth/validate", null);
            Assert.Equal(HttpStatusCode.OK, validation.StatusCode);
            Assert.True((await validation.Content.ReadFromJsonAsync<AuthValidationResult>())!.IsValid);
            Assert.Equal(HttpStatusCode.TooManyRequests, (await client.PostAsync("/auth/validate", null)).StatusCode);
        }
    }

    private static async Task<HttpResponseMessage> AfterReplenishment(Func<Task<HttpResponseMessage>> send)
    {
        // Native limiter time is independent of the authentication test clock. Its heartbeat
        // can replenish segments later than the nominal window, so wait on observable behavior.
        var elapsed = System.Diagnostics.Stopwatch.StartNew();
        while (true)
        {
            await Task.Delay(100);
            var response = await send();
            if (response.StatusCode != HttpStatusCode.TooManyRequests || elapsed.Elapsed > TimeSpan.FromSeconds(10))
                return response;
            response.Dispose();
        }
    }

    private static HttpClient Client(AuthServerFactory factory)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = false });
        client.DefaultRequestHeaders.Add("Origin", "https://localhost:6130");
        client.DefaultRequestHeaders.Add("X-UDID", "rate-proof-device-1234567890");
        return client;
    }
    private static string Cookies(HttpResponseMessage response)
    {
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookies));
        return string.Join("; ", cookies!.Select(c => c.Split(';')[0]));
    }
    private sealed class RecordingResetNotifier : ICredentialResetNotifier
    {
        internal CredentialResetNotification? Last { get; private set; }
        public Task NotifyAsync(CredentialResetNotification notification, CancellationToken ct = default)
        { Last = notification; return Task.CompletedTask; }
    }
}
