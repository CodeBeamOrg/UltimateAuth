using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using CodeBeam.UltimateAuth.Tests.Integration.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CodeBeam.UltimateAuth.Tests.Integration.Security;

public sealed class CsrfAttackTests
{
    [Fact]
    public async Task Login_WithoutCsrfToken_MustNotCreateSession()
    {
        await using var factory = new AuthServerFactory();
        var attacker = await factory.CreateLoginUserAsync();

        using var victimBrowser = CreateBrowser(factory);

        using var response = await victimBrowser.PostAsJsonAsync(
            "/auth/login",
            new
            {
                identifier = attacker.Identifier,
                secret = attacker.Secret
            });

        await AssertCsrfRejected(response);
        AssertNoSessionCookie(response);
    }

    [Fact]
    public async Task Login_WithForgedCsrfToken_MustNotCreateSession()
    {
        await using var factory = new AuthServerFactory();
        var attacker = await factory.CreateLoginUserAsync();

        using var victimBrowser = CreateBrowser(factory);

        using var request = new HttpRequestMessage(
            HttpMethod.Post, "/auth/login")
        {
            Content = JsonContent.Create(new
            {
                identifier = attacker.Identifier,
                secret = attacker.Secret
            })
        };

        request.Headers.TryAddWithoutValidation(
            "RequestVerificationToken",
            "attacker-forged-token");

        using var response = await victimBrowser.SendAsync(request);

        await AssertCsrfRejected(response);
        AssertNoSessionCookie(response);
    }

    [Fact]
    public async Task Login_WithTokenFromAnotherBrowser_MustBeRejected()
    {
        await using var factory = new AuthServerFactory();
        var attacker = await factory.CreateLoginUserAsync();

        using var attackerBrowser = CreateBrowser(factory);
        using var victimBrowser = CreateBrowser(factory);

        var attackerTokens = await GetTokens(attackerBrowser);
        var victimTokens = await GetTokens(victimBrowser);

        // Victim browser has its own antiforgery cookie,
        // but attacker supplies a token from a different browser.
        using var request = new HttpRequestMessage(
            HttpMethod.Post, "/auth/login")
        {
            Content = JsonContent.Create(new
            {
                identifier = attacker.Identifier,
                secret = attacker.Secret
            })
        };

        request.Headers.TryAddWithoutValidation(
            attackerTokens.HeaderName,
            attackerTokens.RequestToken);

        request.Headers.TryAddWithoutValidation(
            "Cookie",
            victimTokens.Cookie);

        using var response = await victimBrowser.SendAsync(request);

        await AssertCsrfRejected(response);
        AssertNoSessionCookie(response);
    }

    [Fact]
    public async Task Login_WithValidCsrfToken_ShouldSucceed()
    {
        await using var factory = new AuthServerFactory();
        var user = await factory.CreateLoginUserAsync();

        using var browser = CreateBrowser(factory);
        var tokens = await GetTokens(browser);

        using var request = new HttpRequestMessage(
            HttpMethod.Post, "/auth/login")
        {
            Content = JsonContent.Create(new
            {
                identifier = user.Identifier,
                secret = user.Secret
            })
        };

        request.Headers.TryAddWithoutValidation(
            tokens.HeaderName,
            tokens.RequestToken);

        request.Headers.TryAddWithoutValidation(
            "Cookie",
            tokens.Cookie);

        using var response = await browser.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Found);

        response.Headers.TryGetValues(
            "Set-Cookie", out var cookies).Should().BeTrue();

        cookies!.Should().Contain(x =>
            x.StartsWith("uas=", StringComparison.OrdinalIgnoreCase));
    }


    [Fact]
    public async Task RevokeOthers_CsrfAttack_MustNotRevokeSessions()
    {
        await using var factory = new AuthServerFactory();
        var user = await factory.CreateLoginUserAsync();

        using var currentBrowser = CreateBrowser(factory);
        using var otherBrowser = CreateBrowser(factory);

        // Create two independent authenticated sessions.
        var currentCookie = await LoginAndGetSessionCookie(
            currentBrowser, user);

        var otherCookie = await LoginAndGetSessionCookie(
            otherBrowser, user);

        SetSessionCookie(currentBrowser, currentCookie);
        SetSessionCookie(otherBrowser, otherCookie);

        // Verify both sessions are initially usable.
        await AssertAuthenticated(currentBrowser);
        await AssertAuthenticated(otherBrowser);

        // ATTACK 1: Authenticated request without CSRF token.
        using (var missingToken = await currentBrowser.PostAsync(
            "/auth/me/sessions/revoke-others", null))
        {
            await AssertCsrfRejected(missingToken);
        }

        // The attack must not revoke the other session.
        await AssertAuthenticated(otherBrowser);

        // ATTACK 2: Forged antiforgery token.
        using (var forgedRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "/auth/me/sessions/revoke-others"))
        {
            forgedRequest.Headers.TryAddWithoutValidation(
                "RequestVerificationToken",
                "forged-token");

            using var forged = await currentBrowser.SendAsync(
                forgedRequest);

            await AssertCsrfRejected(forged);
        }

        // The second attack must not revoke the other session.
        await AssertAuthenticated(otherBrowser);

        // CONTROL: Valid CSRF token must allow the operation.
        using (var valid = await CsrfTestClient.PostAsync(
            currentBrowser,
            "/auth/me/sessions/revoke-others"))
        {
            valid.IsSuccessStatusCode.Should().BeTrue(
                $"valid revoke failed: " +
                await valid.Content.ReadAsStringAsync());
        }

        // The current session must survive revoke-others.
        await AssertAuthenticated(currentBrowser);

        // The other session must now be revoked.
        using var revokedCheck = await CsrfTestClient.PostJsonAsync(
            otherBrowser,
            "/auth/me/identifiers/get",
            new { });

        revokedCheck.StatusCode.Should().Be(
            HttpStatusCode.Unauthorized);
    }

    private static async Task<string> LoginAndGetSessionCookie(
        HttpClient browser,
        IntegrationTestUser user)
    {
        var tokens = await GetTokens(browser);

        using var request = new HttpRequestMessage(
            HttpMethod.Post, "/auth/login")
        {
            Content = JsonContent.Create(new
            {
                identifier = user.Identifier,
                secret = user.Secret
            })
        };

        request.Headers.TryAddWithoutValidation(
            tokens.HeaderName,
            tokens.RequestToken);

        request.Headers.TryAddWithoutValidation(
            "Cookie",
            tokens.Cookie);

        using var response = await browser.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Found);

        response.Headers.TryGetValues(
            "Set-Cookie", out var cookies).Should().BeTrue();

        return cookies!
            .Select(x => x.Split(';', 2)[0])
            .Single(x => x.StartsWith(
                "uas=",
                StringComparison.OrdinalIgnoreCase));
    }

    private static void SetSessionCookie(
        HttpClient browser,
        string sessionCookie)
    {
        browser.DefaultRequestHeaders.Remove("Cookie");
        browser.DefaultRequestHeaders.TryAddWithoutValidation(
            "Cookie",
            sessionCookie);
    }

    private static async Task AssertAuthenticated(
        HttpClient browser)
    {
        using var response = await CsrfTestClient.PostJsonAsync(
            browser,
            "/auth/me/identifiers/get",
            new { });

        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            $"session should remain authenticated: " +
            await response.Content.ReadAsStringAsync());
    }



    private static HttpClient CreateBrowser(AuthServerFactory factory)
    {
        var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = false
            });

        client.DefaultRequestHeaders.Add(
            "Origin", "https://localhost:6130");

        client.DefaultRequestHeaders.Add(
            "X-UDID", $"csrf-attack-{Guid.NewGuid():N}");

        return client;
    }

    private static async Task<CsrfTokens> GetTokens(HttpClient client)
    {
        using var response = await client.GetAsync(
            "/auth/antiforgery/token");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var json = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        var token = json.RootElement
            .GetProperty("requestToken").GetString()!;

        var headerName = json.RootElement
            .GetProperty("headerName").GetString()!;

        response.Headers.TryGetValues(
            "Set-Cookie", out var cookies).Should().BeTrue();

        var cookie = cookies!
            .Select(x => x.Split(';', 2)[0])
            .Single(x => x.StartsWith(
                ".AspNetCore.Antiforgery.",
                StringComparison.OrdinalIgnoreCase));

        return new CsrfTokens(token, headerName, cookie);
    }

    private static async Task AssertCsrfRejected(
        HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadAsStringAsync();

        body.Should().Contain("invalid_antiforgery_token");
    }

    private static void AssertNoSessionCookie(
        HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var cookies))
            return;

        cookies.Should().NotContain(x =>
            x.StartsWith("uas=", StringComparison.OrdinalIgnoreCase));
    }

    private sealed record CsrfTokens(
        string RequestToken,
        string HeaderName,
        string Cookie);
}
