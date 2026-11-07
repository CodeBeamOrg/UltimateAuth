using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CodeBeam.UltimateAuth.Tests.Integration.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CodeBeam.UltimateAuth.Tests.Integration.Security;

public sealed class UAuthAntiforgeryEndpointTests
{
    private sealed record TokenResponse(
        string RequestToken,
        string HeaderName,
        string FormFieldName);

    [Fact]
    public async Task TokenEndpoint_ReturnsValidTokenAndNoStore()
    {
        await using var factory = new AuthServerFactory();

        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            "/auth/antiforgery/token", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Assert.Contains(
            "no-store",
            response.Headers.CacheControl?.ToString() ?? "");

        var token = await response.Content
            .ReadFromJsonAsync<TokenResponse>(cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(token);
        Assert.False(string.IsNullOrWhiteSpace(token.RequestToken));
        Assert.False(string.IsNullOrWhiteSpace(token.HeaderName));
        Assert.False(string.IsNullOrWhiteSpace(token.FormFieldName));

        Assert.Contains(
            response.Headers.TryGetValues("Set-Cookie", out var cookies)
                ? cookies
                : Array.Empty<string>(),
            cookie => cookie.Contains("Antiforgery"));
    }

    [Fact]
    public async Task Login_WithoutToken_IsRejected()
    {
        await using var factory = new AuthServerFactory();

        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = true
            });

        using var response = await client.PostAsJsonAsync(
            "/auth/login",
            new
            {
                identifier = "csrf-test-user",
                secret = "irrelevant"
            }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains(
            "invalid_antiforgery_token",
            body);
    }

    [Fact]
    public async Task Login_WithInvalidToken_IsRejected()
    {
        await using var factory = new AuthServerFactory();

        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = true
            });

        await client.GetAsync("/auth/antiforgery/token", TestContext.Current.CancellationToken);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/auth/login")
        {
            Content = JsonContent.Create(new
            {
                identifier = "csrf-test-user",
                secret = "irrelevant"
            })
        };

        request.Headers.Add(
            "X-UAuth-CSRF",
            "invalid-token");

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidToken_PassesCsrfValidation()
    {
        await using var factory = new AuthServerFactory();

        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = true
            });

        var token = await client.GetFromJsonAsync<TokenResponse>(
            "/auth/antiforgery/token", cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(token);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/auth/login")
        {
            Content = JsonContent.Create(new
            {
                identifier = "nonexistent-user",
                secret = "invalid-password"
            })
        };

        request.Headers.Add(
            token.HeaderName,
            token.RequestToken);

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.DoesNotContain(
            "invalid_antiforgery_token",
            body);
    }

    [Fact]
    public async Task TokenFromAnotherClient_IsRejected()
    {
        await using var factory = new AuthServerFactory();

        using var firstClient = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                HandleCookies = true,
                AllowAutoRedirect = false
            });

        using var secondClient = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                HandleCookies = true,
                AllowAutoRedirect = false
            });

        var firstToken =
            await firstClient.GetFromJsonAsync<TokenResponse>(
                "/auth/antiforgery/token", cancellationToken: TestContext.Current.CancellationToken);

        // Ensure the second client has its own antiforgery cookie.
        await secondClient.GetAsync("/auth/antiforgery/token", TestContext.Current.CancellationToken);

        Assert.NotNull(firstToken);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/auth/login")
        {
            Content = JsonContent.Create(new
            {
                identifier = "csrf-test-user",
                secret = "irrelevant"
            })
        };

        request.Headers.Add(
            firstToken.HeaderName,
            firstToken.RequestToken);

        using var response = await secondClient.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains(
            "invalid_antiforgery_token",
            body);
    }
}
