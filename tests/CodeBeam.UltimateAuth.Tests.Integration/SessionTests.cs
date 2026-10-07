using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Tests.Integration.Infrastructure;
using CodeBeam.UltimateAuth.Users.Contracts;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;

namespace CodeBeam.UltimateAuth.Tests.Integration;

public sealed class SessionTests : IClassFixture<AuthServerFactory>
{
    private readonly AuthServerFactory _factory;

    public SessionTests(AuthServerFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ListChainsSelf_ShouldReturnOnlyAuthenticatedUsersChains()
    {
        _factory.Clock.Reset();

        var user1 = await _factory.CreateLoginUserAsync();
        var user2 = await _factory.CreateLoginUserAsync();

        using var user1Device1 = CreateClient(
            $"session-user1-device1-{Guid.NewGuid():N}");

        using var user1Device2 = CreateClient(
            $"session-user1-device2-{Guid.NewGuid():N}");

        using var user2Device = CreateClient(
            $"session-user2-device-{Guid.NewGuid():N}");

        var user1Cookie1 = GetSessionCookie(
            await LoginAsync(user1Device1, user1.Identifier, user1.Secret));

        await LoginAsync(
            user1Device2,
            user1.Identifier,
            user1.Secret);

        await LoginAsync(
            user2Device,
            user2.Identifier,
            user2.Secret);

        user1Device1.DefaultRequestHeaders.Add(
            "Cookie",
            user1Cookie1);

        var response = await GetChainsAsync(user1Device1);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var page = await response.Content
            .ReadFromJsonAsync<PagedResult<SessionChainSummary>>();

        page.Should().NotBeNull();

        page!.Items.Should().HaveCount(2);
        page.Items.Count(x => x.IsCurrentDevice).Should().Be(1);
    }

    [Fact]
    public async Task GetChainSelf_ShouldReturnRequestedOwnedChain()
    {
        _factory.Clock.Reset();

        var user = await _factory.CreateLoginUserAsync();

        using var client = CreateClient(
            $"session-detail-{Guid.NewGuid():N}");

        var cookie = GetSessionCookie(
            await LoginAsync(
                client,
                user.Identifier,
                user.Secret));

        client.DefaultRequestHeaders.Add(
            "Cookie",
            cookie);

        var chainsResponse = await GetChainsAsync(client);

        chainsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var page = await chainsResponse.Content
            .ReadFromJsonAsync<PagedResult<SessionChainSummary>>();

        page.Should().NotBeNull();

        var chain = page!.Items.Single(x => x.IsCurrentDevice);

        var detailResponse = await client.PostAsync(
            $"/auth/me/sessions/chains/{chain.ChainId.Value}",
            null);

        detailResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = await detailResponse.Content
            .ReadFromJsonAsync<SessionChainDetail>();

        detail.Should().NotBeNull();
        detail!.ChainId.Should().Be(chain.ChainId);
        detail.ActiveSessionId.Should().NotBeNull();
        detail.Sessions.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetChainSelf_ShouldNotAllowReadingAnotherUsersChain()
    {
        _factory.Clock.Reset();

        var attacker = await _factory.CreateLoginUserAsync();
        var victim = await _factory.CreateLoginUserAsync();

        using var attackerClient = CreateClient(
            $"session-attacker-{Guid.NewGuid():N}");

        using var victimClient = CreateClient(
            $"session-victim-{Guid.NewGuid():N}");

        var attackerCookie = GetSessionCookie(
            await LoginAsync(
                attackerClient,
                attacker.Identifier,
                attacker.Secret));

        var victimCookie = GetSessionCookie(
            await LoginAsync(
                victimClient,
                victim.Identifier,
                victim.Secret));

        attackerClient.DefaultRequestHeaders.Add(
            "Cookie",
            attackerCookie);

        victimClient.DefaultRequestHeaders.Add(
            "Cookie",
            victimCookie);

        var victimChainsResponse =
            await GetChainsAsync(victimClient);

        var victimChains = await victimChainsResponse.Content
            .ReadFromJsonAsync<PagedResult<SessionChainSummary>>();

        victimChains.Should().NotBeNull();

        var victimChain =
            victimChains!.Items.Single(x => x.IsCurrentDevice);

        var attack = await attackerClient.PostAsync(
            $"/auth/me/sessions/chains/{victimChain.ChainId.Value}",
            null);

        attack.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.Forbidden,
            HttpStatusCode.NotFound,
            HttpStatusCode.Unauthorized);

        // Most important assertion:
        // victim must still be authenticated.
        var victimVerification =
            await GetChainsAsync(victimClient);

        victimVerification.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RevokeChainSelf_ShouldRevokeOwnedTargetChain()
    {
        _factory.Clock.Reset();

        var user = await _factory.CreateLoginUserAsync();

        var currentDevice =
            $"session-current-{Guid.NewGuid():N}";

        var targetDevice =
            $"session-target-{Guid.NewGuid():N}";

        using var currentClient = CreateClient(currentDevice);
        using var targetClient = CreateClient(targetDevice);

        var currentCookie = GetSessionCookie(
            await LoginAsync(
                currentClient,
                user.Identifier,
                user.Secret));

        var targetCookie = GetSessionCookie(
            await LoginAsync(
                targetClient,
                user.Identifier,
                user.Secret));

        currentClient.DefaultRequestHeaders.Add(
            "Cookie",
            currentCookie);

        targetClient.DefaultRequestHeaders.Add(
            "Cookie",
            targetCookie);

        var targetChainsResponse =
            await GetChainsAsync(targetClient);

        var targetChains = await targetChainsResponse.Content
            .ReadFromJsonAsync<PagedResult<SessionChainSummary>>();

        targetChains.Should().NotBeNull();

        var targetChain =
            targetChains!.Items.Single(x => x.IsCurrentDevice);

        var revoke = await currentClient.PostAsync(
            $"/auth/me/sessions/chains/{targetChain.ChainId.Value}/revoke",
            null);

        revoke.StatusCode.Should().Be(HttpStatusCode.OK);

        //
        // Target session must immediately stop working.
        //
        var targetVerification =
            await GetChainsAsync(targetClient);

        targetVerification.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);

        //
        // Actor's own chain must remain usable.
        //
        var actorVerification =
            await GetChainsAsync(currentClient);

        actorVerification.StatusCode.Should()
            .Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RevokeChainSelf_ShouldNotAllowRevokingAnotherUsersChain()
    {
        _factory.Clock.Reset();

        var attacker = await _factory.CreateLoginUserAsync();
        var victim = await _factory.CreateLoginUserAsync();

        using var attackerClient = CreateClient(
            $"session-revoke-attacker-{Guid.NewGuid():N}");

        using var victimClient = CreateClient(
            $"session-revoke-victim-{Guid.NewGuid():N}");

        var attackerCookie = GetSessionCookie(
            await LoginAsync(
                attackerClient,
                attacker.Identifier,
                attacker.Secret));

        var victimCookie = GetSessionCookie(
            await LoginAsync(
                victimClient,
                victim.Identifier,
                victim.Secret));

        attackerClient.DefaultRequestHeaders.Add(
            "Cookie",
            attackerCookie);

        victimClient.DefaultRequestHeaders.Add(
            "Cookie",
            victimCookie);

        var victimChainsResponse =
            await GetChainsAsync(victimClient);

        var victimChains = await victimChainsResponse.Content
            .ReadFromJsonAsync<PagedResult<SessionChainSummary>>();

        victimChains.Should().NotBeNull();

        var victimChain =
            victimChains!.Items.Single(x => x.IsCurrentDevice);

        var attack = await attackerClient.PostAsync(
            $"/auth/me/sessions/chains/{victimChain.ChainId.Value}/revoke",
            null);

        attack.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.Forbidden,
            HttpStatusCode.NotFound,
            HttpStatusCode.Unauthorized);

        //
        // Security invariant:
        // victim's session must NOT have been revoked.
        //
        var victimVerification =
            await GetChainsAsync(victimClient);

        victimVerification.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        //
        // Attacker must also remain unaffected.
        //
        var attackerVerification =
            await GetChainsAsync(attackerClient);

        attackerVerification.StatusCode.Should()
            .Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SessionSelfEndpoints_ShouldRejectUnauthenticatedRequests()
    {
        using var client = CreateClient(
            $"session-anonymous-{Guid.NewGuid():N}");

        var list = await client.PostAsJsonAsync(
            "/auth/me/sessions/chains",
            new PageRequest());

        list.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);

        var randomChain = SessionChainId.New();

        var detail = await client.PostAsync(
            $"/auth/me/sessions/chains/{randomChain.Value}",
            null);

        detail.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);

        var revoke = await client.PostAsync(
            $"/auth/me/sessions/chains/{randomChain.Value}/revoke",
            null);

        revoke.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);

        var revokeOthers = await client.PostAsync(
            "/auth/me/sessions/revoke-others",
            null);

        revokeOthers.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);

        var revokeAll = await client.PostAsync(
            "/auth/me/sessions/revoke-all",
            null);

        revokeAll.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RevokeOthersSelf_ShouldRevokeAllOtherChainsButKeepCurrentChain()
    {
        _factory.Clock.Reset();

        var user = await _factory.CreateLoginUserAsync();

        using var currentClient = CreateClient(
            $"revoke-others-current-{Guid.NewGuid():N}");

        using var otherClient1 = CreateClient(
            $"revoke-others-other1-{Guid.NewGuid():N}");

        using var otherClient2 = CreateClient(
            $"revoke-others-other2-{Guid.NewGuid():N}");

        var currentCookie = GetSessionCookie(
            await LoginAsync(
                currentClient,
                user.Identifier,
                user.Secret));

        var otherCookie1 = GetSessionCookie(
            await LoginAsync(
                otherClient1,
                user.Identifier,
                user.Secret));

        var otherCookie2 = GetSessionCookie(
            await LoginAsync(
                otherClient2,
                user.Identifier,
                user.Secret));

        currentClient.DefaultRequestHeaders.Add(
            "Cookie",
            currentCookie);

        otherClient1.DefaultRequestHeaders.Add(
            "Cookie",
            otherCookie1);

        otherClient2.DefaultRequestHeaders.Add(
            "Cookie",
            otherCookie2);

        var revoke = await currentClient.PostAsync(
            "/auth/me/sessions/revoke-others",
            null);

        revoke.StatusCode.Should().Be(HttpStatusCode.OK);

        //
        // Current chain survives.
        //
        var currentVerification =
            await GetChainsAsync(currentClient);

        currentVerification.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        //
        // Every other chain becomes unusable.
        //
        var otherVerification1 =
            await GetChainsAsync(otherClient1);

        var otherVerification2 =
            await GetChainsAsync(otherClient2);

        otherVerification1.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);

        otherVerification2.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RevokeOthersSelf_ShouldNotAffectAnotherUsersChains()
    {
        _factory.Clock.Reset();

        var user = await _factory.CreateLoginUserAsync();
        var unrelatedUser = await _factory.CreateLoginUserAsync();

        using var currentClient = CreateClient(
            $"revoke-others-owner-current-{Guid.NewGuid():N}");

        using var otherClient = CreateClient(
            $"revoke-others-owner-other-{Guid.NewGuid():N}");

        using var unrelatedClient = CreateClient(
            $"revoke-others-unrelated-{Guid.NewGuid():N}");

        var currentCookie = GetSessionCookie(
            await LoginAsync(
                currentClient,
                user.Identifier,
                user.Secret));

        var otherCookie = GetSessionCookie(
            await LoginAsync(
                otherClient,
                user.Identifier,
                user.Secret));

        var unrelatedCookie = GetSessionCookie(
            await LoginAsync(
                unrelatedClient,
                unrelatedUser.Identifier,
                unrelatedUser.Secret));

        currentClient.DefaultRequestHeaders.Add(
            "Cookie",
            currentCookie);

        otherClient.DefaultRequestHeaders.Add(
            "Cookie",
            otherCookie);

        unrelatedClient.DefaultRequestHeaders.Add(
            "Cookie",
            unrelatedCookie);

        var revoke = await currentClient.PostAsync(
            "/auth/me/sessions/revoke-others",
            null);

        revoke.StatusCode.Should().Be(HttpStatusCode.OK);

        //
        // Same user's other chain is revoked.
        //
        var otherVerification =
            await GetChainsAsync(otherClient);

        otherVerification.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);

        //
        // Completely unrelated user must not be affected.
        //
        var unrelatedVerification =
            await GetChainsAsync(unrelatedClient);

        unrelatedVerification.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        //
        // Actor survives.
        //
        var currentVerification =
            await GetChainsAsync(currentClient);

        currentVerification.StatusCode.Should()
            .Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RevokeAllSelf_ShouldRevokeCurrentAndAllOtherChains()
    {
        _factory.Clock.Reset();

        var user = await _factory.CreateLoginUserAsync();

        using var client1 = CreateClient(
            $"revoke-all-1-{Guid.NewGuid():N}");

        using var client2 = CreateClient(
            $"revoke-all-2-{Guid.NewGuid():N}");

        using var client3 = CreateClient(
            $"revoke-all-3-{Guid.NewGuid():N}");

        var cookie1 = GetSessionCookie(
            await LoginAsync(
                client1,
                user.Identifier,
                user.Secret));

        var cookie2 = GetSessionCookie(
            await LoginAsync(
                client2,
                user.Identifier,
                user.Secret));

        var cookie3 = GetSessionCookie(
            await LoginAsync(
                client3,
                user.Identifier,
                user.Secret));

        client1.DefaultRequestHeaders.Add("Cookie", cookie1);
        client2.DefaultRequestHeaders.Add("Cookie", cookie2);
        client3.DefaultRequestHeaders.Add("Cookie", cookie3);

        //
        // Client1 revokes every chain, including itself.
        //
        var revoke = await client1.PostAsync(
            "/auth/me/sessions/revoke-all",
            null);

        revoke.StatusCode.Should().Be(HttpStatusCode.OK);

        var verification1 = await GetChainsAsync(client1);
        var verification2 = await GetChainsAsync(client2);
        var verification3 = await GetChainsAsync(client3);

        verification1.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);

        verification2.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);

        verification3.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RevokeAllSelf_ShouldNotAffectAnotherUser()
    {
        _factory.Clock.Reset();

        var user = await _factory.CreateLoginUserAsync();
        var unrelatedUser = await _factory.CreateLoginUserAsync();

        using var userClient = CreateClient(
            $"revoke-all-owner-{Guid.NewGuid():N}");

        using var unrelatedClient = CreateClient(
            $"revoke-all-unrelated-{Guid.NewGuid():N}");

        var userCookie = GetSessionCookie(
            await LoginAsync(
                userClient,
                user.Identifier,
                user.Secret));

        var unrelatedCookie = GetSessionCookie(
            await LoginAsync(
                unrelatedClient,
                unrelatedUser.Identifier,
                unrelatedUser.Secret));

        userClient.DefaultRequestHeaders.Add(
            "Cookie",
            userCookie);

        unrelatedClient.DefaultRequestHeaders.Add(
            "Cookie",
            unrelatedCookie);

        var revoke = await userClient.PostAsync(
            "/auth/me/sessions/revoke-all",
            null);

        revoke.StatusCode.Should().Be(HttpStatusCode.OK);

        //
        // Requesting user's session is gone.
        //
        var userVerification =
            await GetChainsAsync(userClient);

        userVerification.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);

        //
        // Different user's authority tree must remain untouched.
        //
        var unrelatedVerification =
            await GetChainsAsync(unrelatedClient);

        unrelatedVerification.StatusCode.Should()
            .Be(HttpStatusCode.OK);
    }


    private HttpClient CreateClient(string deviceId)
    {
        var client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = false
            });

        client.DefaultRequestHeaders.Add(
            "Origin",
            "https://localhost:6130");

        client.DefaultRequestHeaders.Add(
            "X-UDID",
            deviceId);

        return client;
    }

    private static async Task<HttpResponseMessage> LoginAsync(
        HttpClient client,
        string identifier,
        string secret)
    {
        return await client.PostAsJsonAsync(
            "/auth/login",
            new
            {
                identifier,
                secret
            });
    }

    private static string GetSessionCookie(
        HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.Found);

        response.Headers.TryGetValues(
            "Set-Cookie",
            out var values).Should().BeTrue();

        var cookie = values!
            .First(x =>
                x.StartsWith(
                    "uas=",
                    StringComparison.OrdinalIgnoreCase));

        var cookiePair = cookie
            .Split(';', 2)[0];

        cookiePair.Should().NotBeNullOrWhiteSpace();

        return cookiePair;
    }

    private static Task<HttpResponseMessage> GetChainsAsync(
        HttpClient client)
    {
        return client.PostAsJsonAsync(
            "/auth/me/sessions/chains",
            new PageRequest
            {
                PageNumber = 1,
                PageSize = 50
            });
    }
}
