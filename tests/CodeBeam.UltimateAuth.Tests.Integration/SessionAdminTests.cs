using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Tests.Integration.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;

namespace CodeBeam.UltimateAuth.Tests.Integration;

public sealed class SessionAdminTests : IClassFixture<AuthServerFactory>
{
    private readonly AuthServerFactory _factory;

    public SessionAdminTests(AuthServerFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ListChainsAdmin_ShouldReturnTargetUsersChains()
    {
        _factory.Clock.Reset();

        var admin = await _factory.CreateLoginUserAsync();
        var target = await _factory.CreateLoginUserAsync();
        var unrelated = await _factory.CreateLoginUserAsync();

        await _factory.GrantPermissionsAsync(
            admin.UserKey,
            [
                UAuthActions.Sessions.ListChainsAdmin
            ]);

        using var adminClient = CreateClient(
            $"session-admin-list-{Guid.NewGuid():N}");

        using var targetClient1 = CreateClient(
            $"session-target-1-{Guid.NewGuid():N}");

        using var targetClient2 = CreateClient(
            $"session-target-2-{Guid.NewGuid():N}");

        using var unrelatedClient = CreateClient(
            $"session-unrelated-{Guid.NewGuid():N}");

        var adminCookie = GetSessionCookie(
            await LoginAsync(
                adminClient,
                admin.Identifier,
                admin.Secret));

        await LoginAsync(
            targetClient1,
            target.Identifier,
            target.Secret);

        await LoginAsync(
            targetClient2,
            target.Identifier,
            target.Secret);

        await LoginAsync(
            unrelatedClient,
            unrelated.Identifier,
            unrelated.Secret);

        adminClient.DefaultRequestHeaders.Add(
            "Cookie",
            adminCookie);

        var response = await adminClient.PostAsJsonAsync(
            $"/auth/admin/users/{target.UserKey.Value}/sessions/chains",
            new PageRequest
            {
                PageNumber = 1,
                PageSize = 50
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var page = await response.Content
            .ReadFromJsonAsync<PagedResult<SessionChainSummary>>();

        page.Should().NotBeNull();

        page!.Items.Should().HaveCount(2);

        //
        // Admin is querying somebody else.
        // No chain should therefore be reported as admin's current device.
        //
        page.Items.Should()
            .OnlyContain(x => !x.IsCurrentDevice);

        page.Items.Should()
            .OnlyContain(x => x.ActiveSessionId != null);
    }

    [Fact]
    public async Task GetChainAdmin_ShouldReturnTargetUsersOwnedChain()
    {
        _factory.Clock.Reset();

        var admin = await _factory.CreateLoginUserAsync();
        var target = await _factory.CreateLoginUserAsync();

        await _factory.GrantPermissionsAsync(
            admin.UserKey,
            [
                UAuthActions.Sessions.GetChainAdmin
            ]);

        using var adminClient = CreateClient(
            $"session-admin-detail-{Guid.NewGuid():N}");

        using var targetClient = CreateClient(
            $"session-target-detail-{Guid.NewGuid():N}");

        var adminCookie = GetSessionCookie(
            await LoginAsync(
                adminClient,
                admin.Identifier,
                admin.Secret));

        var targetCookie = GetSessionCookie(
            await LoginAsync(
                targetClient,
                target.Identifier,
                target.Secret));

        adminClient.DefaultRequestHeaders.Add(
            "Cookie",
            adminCookie);

        targetClient.DefaultRequestHeaders.Add(
            "Cookie",
            targetCookie);

        var targetChains = await GetChainsAsync(targetClient);

        targetChains.StatusCode.Should().Be(HttpStatusCode.OK);

        var page = await targetChains.Content
            .ReadFromJsonAsync<PagedResult<SessionChainSummary>>();

        page.Should().NotBeNull();

        var targetChain =
            page!.Items.Single(x => x.IsCurrentDevice);

        var response = await adminClient.PostAsync(
            $"/auth/admin/users/{target.UserKey.Value}/sessions/chains/{targetChain.ChainId.Value}",
            null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = await response.Content
            .ReadFromJsonAsync<SessionChainDetail>();

        detail.Should().NotBeNull();

        detail!.ChainId.Should()
            .Be(targetChain.ChainId);

        detail.ActiveSessionId.Should()
            .Be(targetChain.ActiveSessionId);

        detail.Sessions.Should()
            .NotBeEmpty();
    }

    [Fact]
    public async Task GetChainAdmin_ShouldRejectChainOwnedByDifferentUser()
    {
        _factory.Clock.Reset();

        var admin = await _factory.CreateLoginUserAsync();

        var targetA = await _factory.CreateLoginUserAsync();
        var targetB = await _factory.CreateLoginUserAsync();

        await _factory.GrantPermissionsAsync(
            admin.UserKey,
            [
                UAuthActions.Sessions.GetChainAdmin
            ]);

        using var adminClient = CreateClient(
            $"session-admin-cross-user-{Guid.NewGuid():N}");

        using var targetAClient = CreateClient(
            $"session-target-a-{Guid.NewGuid():N}");

        using var targetBClient = CreateClient(
            $"session-target-b-{Guid.NewGuid():N}");

        var adminCookie = GetSessionCookie(
            await LoginAsync(
                adminClient,
                admin.Identifier,
                admin.Secret));

        var targetACookie = GetSessionCookie(
            await LoginAsync(
                targetAClient,
                targetA.Identifier,
                targetA.Secret));

        var targetBCookie = GetSessionCookie(
            await LoginAsync(
                targetBClient,
                targetB.Identifier,
                targetB.Secret));

        adminClient.DefaultRequestHeaders.Add(
            "Cookie",
            adminCookie);

        targetAClient.DefaultRequestHeaders.Add(
            "Cookie",
            targetACookie);

        targetBClient.DefaultRequestHeaders.Add(
            "Cookie",
            targetBCookie);

        //
        // Obtain B's chain.
        //
        var targetBChains =
            await GetChainsAsync(targetBClient);

        var targetBPage = await targetBChains.Content
            .ReadFromJsonAsync<PagedResult<SessionChainSummary>>();

        targetBPage.Should().NotBeNull();

        var targetBChain =
            targetBPage!.Items.Single(x => x.IsCurrentDevice);

        //
        // URL says A, ChainId belongs to B.
        //
        var response = await adminClient.PostAsync(
            $"/auth/admin/users/{targetA.UserKey.Value}/sessions/chains/{targetBChain.ChainId.Value}",
            null);

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.Forbidden,
            HttpStatusCode.NotFound);

        //
        // Critical invariant:
        // query failure must not mutate either user.
        //
        var targetAVerification =
            await GetChainsAsync(targetAClient);

        var targetBVerification =
            await GetChainsAsync(targetBClient);

        targetAVerification.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        targetBVerification.StatusCode.Should()
            .Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SessionAdminQueryEndpoints_ShouldRequireAdminPermission()
    {
        _factory.Clock.Reset();

        var actor = await _factory.CreateLoginUserAsync();
        var target = await _factory.CreateLoginUserAsync();

        using var actorClient = CreateClient(
            $"session-admin-no-permission-{Guid.NewGuid():N}");

        using var targetClient = CreateClient(
            $"session-admin-no-permission-target-{Guid.NewGuid():N}");

        var actorCookie = GetSessionCookie(
            await LoginAsync(
                actorClient,
                actor.Identifier,
                actor.Secret));

        var targetCookie = GetSessionCookie(
            await LoginAsync(
                targetClient,
                target.Identifier,
                target.Secret));

        actorClient.DefaultRequestHeaders.Add(
            "Cookie",
            actorCookie);

        targetClient.DefaultRequestHeaders.Add(
            "Cookie",
            targetCookie);

        var targetChains =
            await GetChainsAsync(targetClient);

        var targetPage = await targetChains.Content
            .ReadFromJsonAsync<PagedResult<SessionChainSummary>>();

        targetPage.Should().NotBeNull();

        var targetChain =
            targetPage!.Items.Single(x => x.IsCurrentDevice);

        var list = await actorClient.PostAsJsonAsync(
            $"/auth/admin/users/{target.UserKey.Value}/sessions/chains",
            new PageRequest());

        list.StatusCode.Should().BeOneOf(
            HttpStatusCode.Forbidden,
            HttpStatusCode.Unauthorized);

        var detail = await actorClient.PostAsync(
            $"/auth/admin/users/{target.UserKey.Value}/sessions/chains/{targetChain.ChainId.Value}",
            null);

        detail.StatusCode.Should().BeOneOf(
            HttpStatusCode.Forbidden,
            HttpStatusCode.Unauthorized);

        //
        // Denied query must have no side effects.
        //
        var verification =
            await GetChainsAsync(targetClient);

        verification.StatusCode.Should()
            .Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SessionAdminQueryEndpoints_ShouldRejectUnauthenticatedRequests()
    {
        var target = await _factory.CreateLoginUserAsync();

        using var anonymous = CreateClient(
            $"session-admin-anonymous-{Guid.NewGuid():N}");

        var randomChain = SessionChainId.New();

        var list = await anonymous.PostAsJsonAsync(
            $"/auth/admin/users/{target.UserKey.Value}/sessions/chains",
            new PageRequest());

        list.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);

        var detail = await anonymous.PostAsync(
            $"/auth/admin/users/{target.UserKey.Value}/sessions/chains/{randomChain.Value}",
            null);

        detail.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RevokeSessionAdmin_ShouldRevokeTargetUsersSession()
    {
        _factory.Clock.Reset();

        var admin = await _factory.CreateLoginUserAsync();
        var target = await _factory.CreateLoginUserAsync();

        await _factory.GrantPermissionsAsync(
            admin.UserKey,
            [
                UAuthActions.Sessions.RevokeSessionAdmin
            ]);

        using var adminClient = CreateClient(
            $"session-revoke-admin-{Guid.NewGuid():N}");

        using var targetClient = CreateClient(
            $"session-revoke-target-{Guid.NewGuid():N}");

        var adminCookie = GetSessionCookie(
            await LoginAsync(
                adminClient,
                admin.Identifier,
                admin.Secret));

        var targetCookie = GetSessionCookie(
            await LoginAsync(
                targetClient,
                target.Identifier,
                target.Secret));

        adminClient.DefaultRequestHeaders.Add("Cookie", adminCookie);
        targetClient.DefaultRequestHeaders.Add("Cookie", targetCookie);

        var chainsResponse = await GetChainsAsync(targetClient);

        chainsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var page = await chainsResponse.Content
            .ReadFromJsonAsync<PagedResult<SessionChainSummary>>();

        page.Should().NotBeNull();

        var chain = page!.Items.Single(x => x.IsCurrentDevice);

        chain.ActiveSessionId.Should().NotBeNull();

        var sessionId = chain.ActiveSessionId!.Value;

        var revoke = await adminClient.PostAsync(
            $"/auth/admin/users/{target.UserKey.Value}/sessions/{sessionId.Value}/revoke",
            null);

        revoke.StatusCode.Should().Be(HttpStatusCode.OK);

        //
        // Revoked session can no longer authenticate.
        //
        var targetVerification = await GetChainsAsync(targetClient);

        targetVerification.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);

        //
        // Admin's own authority must remain intact.
        //
        var adminVerification = await GetChainsAsync(adminClient);

        adminVerification.StatusCode.Should()
            .Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RevokeSessionAdmin_ShouldRejectSessionOwnedByDifferentUser()
    {
        _factory.Clock.Reset();

        var admin = await _factory.CreateLoginUserAsync();
        var targetA = await _factory.CreateLoginUserAsync();
        var targetB = await _factory.CreateLoginUserAsync();

        await _factory.GrantPermissionsAsync(
            admin.UserKey,
            [
                UAuthActions.Sessions.RevokeSessionAdmin
            ]);

        using var adminClient = CreateClient(
            $"session-revoke-cross-admin-{Guid.NewGuid():N}");

        using var targetAClient = CreateClient(
            $"session-revoke-cross-a-{Guid.NewGuid():N}");

        using var targetBClient = CreateClient(
            $"session-revoke-cross-b-{Guid.NewGuid():N}");

        var adminCookie = GetSessionCookie(
            await LoginAsync(
                adminClient,
                admin.Identifier,
                admin.Secret));

        var targetACookie = GetSessionCookie(
            await LoginAsync(
                targetAClient,
                targetA.Identifier,
                targetA.Secret));

        var targetBCookie = GetSessionCookie(
            await LoginAsync(
                targetBClient,
                targetB.Identifier,
                targetB.Secret));

        adminClient.DefaultRequestHeaders.Add("Cookie", adminCookie);
        targetAClient.DefaultRequestHeaders.Add("Cookie", targetACookie);
        targetBClient.DefaultRequestHeaders.Add("Cookie", targetBCookie);

        //
        // Obtain B's SessionId.
        //
        var bChainsResponse = await GetChainsAsync(targetBClient);

        bChainsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var bPage = await bChainsResponse.Content
            .ReadFromJsonAsync<PagedResult<SessionChainSummary>>();

        bPage.Should().NotBeNull();

        var bChain = bPage!.Items.Single(x => x.IsCurrentDevice);

        bChain.ActiveSessionId.Should().NotBeNull();

        var bSessionId = bChain.ActiveSessionId!.Value;

        //
        // URL claims target A, but SessionId belongs to B.
        //
        var attack = await adminClient.PostAsync(
            $"/auth/admin/users/{targetA.UserKey.Value}/sessions/{bSessionId.Value}/revoke",
            null);

        attack.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.Forbidden,
            HttpStatusCode.NotFound);

        //
        // Absolutely no mutation should have occurred.
        //
        var aVerification = await GetChainsAsync(targetAClient);
        var bVerification = await GetChainsAsync(targetBClient);
        var adminVerification = await GetChainsAsync(adminClient);

        aVerification.StatusCode.Should().Be(HttpStatusCode.OK);
        bVerification.StatusCode.Should().Be(HttpStatusCode.OK);
        adminVerification.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RevokeSessionAdmin_ShouldRequirePermission()
    {
        _factory.Clock.Reset();

        var actor = await _factory.CreateLoginUserAsync();
        var target = await _factory.CreateLoginUserAsync();

        using var actorClient = CreateClient(
            $"session-revoke-no-permission-{Guid.NewGuid():N}");

        using var targetClient = CreateClient(
            $"session-revoke-no-permission-target-{Guid.NewGuid():N}");

        var actorCookie = GetSessionCookie(
            await LoginAsync(
                actorClient,
                actor.Identifier,
                actor.Secret));

        var targetCookie = GetSessionCookie(
            await LoginAsync(
                targetClient,
                target.Identifier,
                target.Secret));

        actorClient.DefaultRequestHeaders.Add("Cookie", actorCookie);
        targetClient.DefaultRequestHeaders.Add("Cookie", targetCookie);

        var chainsResponse = await GetChainsAsync(targetClient);

        var page = await chainsResponse.Content
            .ReadFromJsonAsync<PagedResult<SessionChainSummary>>();

        page.Should().NotBeNull();

        var sessionId = page!.Items
            .Single(x => x.IsCurrentDevice)
            .ActiveSessionId;

        sessionId.Should().NotBeNull();

        var revoke = await actorClient.PostAsync(
            $"/auth/admin/users/{target.UserKey.Value}/sessions/{sessionId!.Value.Value}/revoke",
            null);

        revoke.StatusCode.Should().BeOneOf(
            HttpStatusCode.Forbidden,
            HttpStatusCode.Unauthorized);

        //
        // Denied mutation must really be mutation-free.
        //
        var targetVerification = await GetChainsAsync(targetClient);

        targetVerification.StatusCode.Should()
            .Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RevokeSessionAdmin_ShouldNotAffectOtherSessionOfSameUser()
    {
        _factory.Clock.Reset();

        var admin = await _factory.CreateLoginUserAsync();
        var target = await _factory.CreateLoginUserAsync();

        await _factory.GrantPermissionsAsync(
            admin.UserKey,
            [
                UAuthActions.Sessions.RevokeSessionAdmin
            ]);

        using var adminClient = CreateClient(
            $"session-single-admin-{Guid.NewGuid():N}");

        using var targetClient1 = CreateClient(
            $"session-single-target-1-{Guid.NewGuid():N}");

        using var targetClient2 = CreateClient(
            $"session-single-target-2-{Guid.NewGuid():N}");

        var adminCookie = GetSessionCookie(
            await LoginAsync(
                adminClient,
                admin.Identifier,
                admin.Secret));

        var targetCookie1 = GetSessionCookie(
            await LoginAsync(
                targetClient1,
                target.Identifier,
                target.Secret));

        var targetCookie2 = GetSessionCookie(
            await LoginAsync(
                targetClient2,
                target.Identifier,
                target.Secret));

        adminClient.DefaultRequestHeaders.Add("Cookie", adminCookie);
        targetClient1.DefaultRequestHeaders.Add("Cookie", targetCookie1);
        targetClient2.DefaultRequestHeaders.Add("Cookie", targetCookie2);

        //
        // Find client1's current chain/session.
        //
        var chainsResponse = await GetChainsAsync(targetClient1);

        var page = await chainsResponse.Content
            .ReadFromJsonAsync<PagedResult<SessionChainSummary>>();

        page.Should().NotBeNull();

        var chain1 = page!.Items.Single(x => x.IsCurrentDevice);

        chain1.ActiveSessionId.Should().NotBeNull();

        var revoke = await adminClient.PostAsync(
            $"/auth/admin/users/{target.UserKey.Value}/sessions/{chain1.ActiveSessionId!.Value.Value}/revoke",
            null);

        revoke.StatusCode.Should().Be(HttpStatusCode.OK);

        //
        // Exact targeted session dies.
        //
        var verification1 = await GetChainsAsync(targetClient1);

        verification1.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);

        //
        // Different chain/session of same user survives.
        //
        var verification2 = await GetChainsAsync(targetClient2);

        verification2.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        //
        // Admin survives.
        //
        var adminVerification = await GetChainsAsync(adminClient);

        adminVerification.StatusCode.Should()
            .Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RevokeChainAdmin_ShouldRevokeTargetChain()
    {
        _factory.Clock.Reset();

        var admin = await _factory.CreateLoginUserAsync();
        var target = await _factory.CreateLoginUserAsync();

        await _factory.GrantPermissionsAsync(
            admin.UserKey,
            [
                UAuthActions.Sessions.RevokeChainAdmin
            ]);

        using var adminClient = CreateClient(
            $"revoke-chain-admin-{Guid.NewGuid():N}");

        using var targetClient = CreateClient(
            $"revoke-chain-target-{Guid.NewGuid():N}");

        var adminCookie = GetSessionCookie(
            await LoginAsync(
                adminClient,
                admin.Identifier,
                admin.Secret));

        var targetCookie = GetSessionCookie(
            await LoginAsync(
                targetClient,
                target.Identifier,
                target.Secret));

        adminClient.DefaultRequestHeaders.Add("Cookie", adminCookie);
        targetClient.DefaultRequestHeaders.Add("Cookie", targetCookie);

        var chainsResponse = await GetChainsAsync(targetClient);

        chainsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var page = await chainsResponse.Content
            .ReadFromJsonAsync<PagedResult<SessionChainSummary>>();

        page.Should().NotBeNull();

        var targetChain =
            page!.Items.Single(x => x.IsCurrentDevice);

        var revoke = await adminClient.PostAsync(
            $"/auth/admin/users/{target.UserKey.Value}/sessions/chains/{targetChain.ChainId.Value}/revoke",
            null);

        revoke.StatusCode.Should().Be(HttpStatusCode.OK);

        //
        // Target chain is no longer usable.
        //
        var targetVerification =
            await GetChainsAsync(targetClient);

        targetVerification.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);

        //
        // Admin remains authenticated.
        //
        var adminVerification =
            await GetChainsAsync(adminClient);

        adminVerification.StatusCode.Should()
            .Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RevokeChainAdmin_ShouldRejectChainOwnedByDifferentUser()
    {
        _factory.Clock.Reset();

        var admin = await _factory.CreateLoginUserAsync();
        var targetA = await _factory.CreateLoginUserAsync();
        var targetB = await _factory.CreateLoginUserAsync();

        await _factory.GrantPermissionsAsync(
            admin.UserKey,
            [
                UAuthActions.Sessions.RevokeChainAdmin
            ]);

        using var adminClient = CreateClient(
            $"revoke-chain-cross-admin-{Guid.NewGuid():N}");

        using var targetAClient = CreateClient(
            $"revoke-chain-cross-a-{Guid.NewGuid():N}");

        using var targetBClient = CreateClient(
            $"revoke-chain-cross-b-{Guid.NewGuid():N}");

        var adminCookie = GetSessionCookie(
            await LoginAsync(
                adminClient,
                admin.Identifier,
                admin.Secret));

        var cookieA = GetSessionCookie(
            await LoginAsync(
                targetAClient,
                targetA.Identifier,
                targetA.Secret));

        var cookieB = GetSessionCookie(
            await LoginAsync(
                targetBClient,
                targetB.Identifier,
                targetB.Secret));

        adminClient.DefaultRequestHeaders.Add("Cookie", adminCookie);
        targetAClient.DefaultRequestHeaders.Add("Cookie", cookieA);
        targetBClient.DefaultRequestHeaders.Add("Cookie", cookieB);

        var bChainsResponse =
            await GetChainsAsync(targetBClient);

        bChainsResponse.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        var bPage = await bChainsResponse.Content
            .ReadFromJsonAsync<PagedResult<SessionChainSummary>>();

        bPage.Should().NotBeNull();

        var bChain =
            bPage!.Items.Single(x => x.IsCurrentDevice);

        //
        // URL target = A
        // Chain owner = B
        //
        var attack = await adminClient.PostAsync(
            $"/auth/admin/users/{targetA.UserKey.Value}/sessions/chains/{bChain.ChainId.Value}/revoke",
            null);

        attack.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.Forbidden,
            HttpStatusCode.NotFound);

        //
        // Failed ownership check must cause zero mutation.
        //
        var aVerification =
            await GetChainsAsync(targetAClient);

        var bVerification =
            await GetChainsAsync(targetBClient);

        var adminVerification =
            await GetChainsAsync(adminClient);

        aVerification.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        bVerification.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        adminVerification.StatusCode.Should()
            .Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RevokeChainAdmin_ShouldNotAffectOtherChainOfSameUser()
    {
        _factory.Clock.Reset();

        var admin = await _factory.CreateLoginUserAsync();
        var target = await _factory.CreateLoginUserAsync();

        await _factory.GrantPermissionsAsync(
            admin.UserKey,
            [
                UAuthActions.Sessions.RevokeChainAdmin
            ]);

        using var adminClient = CreateClient(
            $"revoke-chain-isolation-admin-{Guid.NewGuid():N}");

        using var targetClient1 = CreateClient(
            $"revoke-chain-device-1-{Guid.NewGuid():N}");

        using var targetClient2 = CreateClient(
            $"revoke-chain-device-2-{Guid.NewGuid():N}");

        var adminCookie = GetSessionCookie(
            await LoginAsync(
                adminClient,
                admin.Identifier,
                admin.Secret));

        var cookie1 = GetSessionCookie(
            await LoginAsync(
                targetClient1,
                target.Identifier,
                target.Secret));

        var cookie2 = GetSessionCookie(
            await LoginAsync(
                targetClient2,
                target.Identifier,
                target.Secret));

        adminClient.DefaultRequestHeaders.Add("Cookie", adminCookie);
        targetClient1.DefaultRequestHeaders.Add("Cookie", cookie1);
        targetClient2.DefaultRequestHeaders.Add("Cookie", cookie2);

        //
        // From device 1's perspective identify its own chain.
        //
        var chainsResponse =
            await GetChainsAsync(targetClient1);

        var page = await chainsResponse.Content
            .ReadFromJsonAsync<PagedResult<SessionChainSummary>>();

        page.Should().NotBeNull();

        page!.Items.Should().HaveCountGreaterThanOrEqualTo(2);

        var chain1 =
            page.Items.Single(x => x.IsCurrentDevice);

        var revoke = await adminClient.PostAsync(
            $"/auth/admin/users/{target.UserKey.Value}/sessions/chains/{chain1.ChainId.Value}/revoke",
            null);

        revoke.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        //
        // Revoked chain dies.
        //
        var device1Verification =
            await GetChainsAsync(targetClient1);

        device1Verification.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);

        //
        // Sibling chain survives.
        //
        var device2Verification =
            await GetChainsAsync(targetClient2);

        device2Verification.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        //
        // Actor remains usable.
        //
        var adminVerification =
            await GetChainsAsync(adminClient);

        adminVerification.StatusCode.Should()
            .Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RevokeChainAdmin_ShouldRequirePermissionWithoutMutation()
    {
        _factory.Clock.Reset();

        var actor = await _factory.CreateLoginUserAsync();
        var target = await _factory.CreateLoginUserAsync();

        using var actorClient = CreateClient(
            $"revoke-chain-denied-actor-{Guid.NewGuid():N}");

        using var targetClient = CreateClient(
            $"revoke-chain-denied-target-{Guid.NewGuid():N}");

        var actorCookie = GetSessionCookie(
            await LoginAsync(
                actorClient,
                actor.Identifier,
                actor.Secret));

        var targetCookie = GetSessionCookie(
            await LoginAsync(
                targetClient,
                target.Identifier,
                target.Secret));

        actorClient.DefaultRequestHeaders.Add("Cookie", actorCookie);
        targetClient.DefaultRequestHeaders.Add("Cookie", targetCookie);

        var chainsResponse =
            await GetChainsAsync(targetClient);

        var page = await chainsResponse.Content
            .ReadFromJsonAsync<PagedResult<SessionChainSummary>>();

        page.Should().NotBeNull();

        var targetChain =
            page!.Items.Single(x => x.IsCurrentDevice);

        var response = await actorClient.PostAsync(
            $"/auth/admin/users/{target.UserKey.Value}/sessions/chains/{targetChain.ChainId.Value}/revoke",
            null);

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Forbidden,
            HttpStatusCode.Unauthorized);

        //
        // Authorization denial must occur before mutation.
        //
        var targetVerification =
            await GetChainsAsync(targetClient);

        targetVerification.StatusCode.Should()
            .Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RevokeAllChainsAdmin_ShouldRevokeAllTargetUsersChains()
    {
        _factory.Clock.Reset();

        var admin = await _factory.CreateLoginUserAsync();
        var target = await _factory.CreateLoginUserAsync();

        await _factory.GrantPermissionsAsync(
            admin.UserKey,
            [UAuthActions.Sessions.RevokeAllChainsAdmin]);

        using var adminClient = CreateClient(
            $"revoke-all-admin-{Guid.NewGuid():N}");

        using var targetClient1 = CreateClient(
            $"revoke-all-target-1-{Guid.NewGuid():N}");

        using var targetClient2 = CreateClient(
            $"revoke-all-target-2-{Guid.NewGuid():N}");

        var adminCookie = GetSessionCookie(
            await LoginAsync(
                adminClient,
                admin.Identifier,
                admin.Secret));

        var cookie1 = GetSessionCookie(
            await LoginAsync(
                targetClient1,
                target.Identifier,
                target.Secret));

        var cookie2 = GetSessionCookie(
            await LoginAsync(
                targetClient2,
                target.Identifier,
                target.Secret));

        adminClient.DefaultRequestHeaders.Add("Cookie", adminCookie);
        targetClient1.DefaultRequestHeaders.Add("Cookie", cookie1);
        targetClient2.DefaultRequestHeaders.Add("Cookie", cookie2);

        //
        // Sanity check: both target chains are usable before revoke.
        //
        (await GetChainsAsync(targetClient1))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        (await GetChainsAsync(targetClient2))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var revoke = await adminClient.PostAsync(
            $"/auth/admin/users/{target.UserKey.Value}/sessions/revoke-all",
            null);

        revoke.StatusCode.Should().Be(HttpStatusCode.OK);

        //
        // Every currently active chain belonging to target must die.
        //
        (await GetChainsAsync(targetClient1))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        (await GetChainsAsync(targetClient2))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        //
        // Actor remains usable.
        //
        (await GetChainsAsync(adminClient))
            .StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RevokeAllChainsAdmin_ShouldNotAffectDifferentUser()
    {
        _factory.Clock.Reset();

        var admin = await _factory.CreateLoginUserAsync();
        var target = await _factory.CreateLoginUserAsync();
        var unrelated = await _factory.CreateLoginUserAsync();

        await _factory.GrantPermissionsAsync(
            admin.UserKey,
            [UAuthActions.Sessions.RevokeAllChainsAdmin]);

        using var adminClient = CreateClient(
            $"revoke-all-isolation-admin-{Guid.NewGuid():N}");

        using var targetClient = CreateClient(
            $"revoke-all-isolation-target-{Guid.NewGuid():N}");

        using var unrelatedClient = CreateClient(
            $"revoke-all-isolation-unrelated-{Guid.NewGuid():N}");

        var adminCookie = GetSessionCookie(
            await LoginAsync(
                adminClient,
                admin.Identifier,
                admin.Secret));

        var targetCookie = GetSessionCookie(
            await LoginAsync(
                targetClient,
                target.Identifier,
                target.Secret));

        var unrelatedCookie = GetSessionCookie(
            await LoginAsync(
                unrelatedClient,
                unrelated.Identifier,
                unrelated.Secret));

        adminClient.DefaultRequestHeaders.Add("Cookie", adminCookie);
        targetClient.DefaultRequestHeaders.Add("Cookie", targetCookie);
        unrelatedClient.DefaultRequestHeaders.Add("Cookie", unrelatedCookie);

        var revoke = await adminClient.PostAsync(
            $"/auth/admin/users/{target.UserKey.Value}/sessions/revoke-all",
            null);

        revoke.StatusCode.Should().Be(HttpStatusCode.OK);

        //
        // Target dies.
        //
        (await GetChainsAsync(targetClient))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        //
        // Completely unrelated user must survive.
        //
        (await GetChainsAsync(unrelatedClient))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        //
        // Admin must survive too.
        //
        (await GetChainsAsync(adminClient))
            .StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RevokeAllChainsAdmin_ShouldRequirePermissionWithoutMutation()
    {
        _factory.Clock.Reset();

        var actor = await _factory.CreateLoginUserAsync();
        var target = await _factory.CreateLoginUserAsync();

        using var actorClient = CreateClient(
            $"revoke-all-denied-actor-{Guid.NewGuid():N}");

        using var targetClient1 = CreateClient(
            $"revoke-all-denied-target-1-{Guid.NewGuid():N}");

        using var targetClient2 = CreateClient(
            $"revoke-all-denied-target-2-{Guid.NewGuid():N}");

        var actorCookie = GetSessionCookie(
            await LoginAsync(
                actorClient,
                actor.Identifier,
                actor.Secret));

        var cookie1 = GetSessionCookie(
            await LoginAsync(
                targetClient1,
                target.Identifier,
                target.Secret));

        var cookie2 = GetSessionCookie(
            await LoginAsync(
                targetClient2,
                target.Identifier,
                target.Secret));

        actorClient.DefaultRequestHeaders.Add("Cookie", actorCookie);
        targetClient1.DefaultRequestHeaders.Add("Cookie", cookie1);
        targetClient2.DefaultRequestHeaders.Add("Cookie", cookie2);

        var response = await actorClient.PostAsync(
            $"/auth/admin/users/{target.UserKey.Value}/sessions/revoke-all",
            null);

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Forbidden,
            HttpStatusCode.Unauthorized);

        //
        // Authorization denial must happen before mutation.
        //
        (await GetChainsAsync(targetClient1))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        (await GetChainsAsync(targetClient2))
            .StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RevokeAllChainsAdmin_ShouldRejectUnauthenticatedRequestWithoutMutation()
    {
        _factory.Clock.Reset();

        var target = await _factory.CreateLoginUserAsync();

        using var anonymousClient = CreateClient(
            $"revoke-all-anonymous-{Guid.NewGuid():N}");

        using var targetClient = CreateClient(
            $"revoke-all-anonymous-target-{Guid.NewGuid():N}");

        var targetCookie = GetSessionCookie(
            await LoginAsync(
                targetClient,
                target.Identifier,
                target.Secret));

        targetClient.DefaultRequestHeaders.Add(
            "Cookie",
            targetCookie);

        var response = await anonymousClient.PostAsync(
            $"/auth/admin/users/{target.UserKey.Value}/sessions/revoke-all",
            null);

        response.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);

        //
        // Anonymous request must produce zero mutation.
        //
        var verification =
            await GetChainsAsync(targetClient);

        verification.StatusCode.Should()
            .Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RevokeRootAdmin_ShouldInvalidateAllExistingTargetSessions()
    {
        _factory.Clock.Reset();

        var admin = await _factory.CreateLoginUserAsync();
        var target = await _factory.CreateLoginUserAsync();

        await _factory.GrantPermissionsAsync(
            admin.UserKey,
            [UAuthActions.Sessions.RevokeRootAdmin]);

        using var adminClient = CreateClient(
            $"root-admin-{Guid.NewGuid():N}");

        using var targetClient1 = CreateClient(
            $"root-target-1-{Guid.NewGuid():N}");

        using var targetClient2 = CreateClient(
            $"root-target-2-{Guid.NewGuid():N}");

        var adminCookie = GetSessionCookie(
            await LoginAsync(
                adminClient,
                admin.Identifier,
                admin.Secret));

        var targetCookie1 = GetSessionCookie(
            await LoginAsync(
                targetClient1,
                target.Identifier,
                target.Secret));

        var targetCookie2 = GetSessionCookie(
            await LoginAsync(
                targetClient2,
                target.Identifier,
                target.Secret));

        adminClient.DefaultRequestHeaders.Add("Cookie", adminCookie);
        targetClient1.DefaultRequestHeaders.Add("Cookie", targetCookie1);
        targetClient2.DefaultRequestHeaders.Add("Cookie", targetCookie2);

        (await GetChainsAsync(targetClient1))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        (await GetChainsAsync(targetClient2))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var revoke = await adminClient.PostAsync(
            $"/auth/admin/users/{target.UserKey.Value}/sessions/revoke-root",
            null);

        revoke.StatusCode.Should().Be(HttpStatusCode.OK);

        //
        // Root cascade must invalidate every existing target session.
        //
        (await GetChainsAsync(targetClient1))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        (await GetChainsAsync(targetClient2))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        //
        // Actor remains valid.
        //
        (await GetChainsAsync(adminClient))
            .StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RevokeRootAdmin_ShouldNotAffectDifferentUser()
    {
        _factory.Clock.Reset();

        var admin = await _factory.CreateLoginUserAsync();
        var target = await _factory.CreateLoginUserAsync();
        var unrelated = await _factory.CreateLoginUserAsync();

        await _factory.GrantPermissionsAsync(
            admin.UserKey,
            [UAuthActions.Sessions.RevokeRootAdmin]);

        using var adminClient = CreateClient(
            $"root-isolation-admin-{Guid.NewGuid():N}");

        using var targetClient = CreateClient(
            $"root-isolation-target-{Guid.NewGuid():N}");

        using var unrelatedClient = CreateClient(
            $"root-isolation-unrelated-{Guid.NewGuid():N}");

        var adminCookie = GetSessionCookie(
            await LoginAsync(
                adminClient,
                admin.Identifier,
                admin.Secret));

        var targetCookie = GetSessionCookie(
            await LoginAsync(
                targetClient,
                target.Identifier,
                target.Secret));

        var unrelatedCookie = GetSessionCookie(
            await LoginAsync(
                unrelatedClient,
                unrelated.Identifier,
                unrelated.Secret));

        adminClient.DefaultRequestHeaders.Add("Cookie", adminCookie);
        targetClient.DefaultRequestHeaders.Add("Cookie", targetCookie);
        unrelatedClient.DefaultRequestHeaders.Add("Cookie", unrelatedCookie);

        var revoke = await adminClient.PostAsync(
            $"/auth/admin/users/{target.UserKey.Value}/sessions/revoke-root",
            null);

        revoke.StatusCode.Should().Be(HttpStatusCode.OK);

        (await GetChainsAsync(targetClient))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        (await GetChainsAsync(unrelatedClient))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        (await GetChainsAsync(adminClient))
            .StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RevokeRootAdmin_ShouldRequirePermissionWithoutMutation()
    {
        _factory.Clock.Reset();

        var actor = await _factory.CreateLoginUserAsync();
        var target = await _factory.CreateLoginUserAsync();

        using var actorClient = CreateClient(
            $"root-denied-actor-{Guid.NewGuid():N}");

        using var targetClient = CreateClient(
            $"root-denied-target-{Guid.NewGuid():N}");

        var actorCookie = GetSessionCookie(
            await LoginAsync(
                actorClient,
                actor.Identifier,
                actor.Secret));

        var targetCookie = GetSessionCookie(await LoginAsync(targetClient, target.Identifier, target.Secret));

        actorClient.DefaultRequestHeaders.Add("Cookie", actorCookie);
        targetClient.DefaultRequestHeaders.Add("Cookie", targetCookie);

        var response = await actorClient.PostAsync($"/auth/admin/users/{target.UserKey.Value}/sessions/revoke-root", null);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.Unauthorized);

        (await GetChainsAsync(targetClient)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RevokeRootAdmin_ShouldRejectUnauthenticatedRequestWithoutMutation()
    {
        _factory.Clock.Reset();

        var target = await _factory.CreateLoginUserAsync();

        using var anonymousClient = CreateClient($"root-anonymous-{Guid.NewGuid():N}");
        using var targetClient = CreateClient($"root-anonymous-target-{Guid.NewGuid():N}");

        var targetCookie = GetSessionCookie(await LoginAsync(targetClient, target.Identifier, target.Secret));
        targetClient.DefaultRequestHeaders.Add("Cookie", targetCookie);

        var response = await anonymousClient.PostAsync($"/auth/admin/users/{target.UserKey.Value}/sessions/revoke-root", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await GetChainsAsync(targetClient)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RevokeRootAdmin_ShouldAllowSubsequentLoginWithNewAuthenticationRoot()
    {
        _factory.Clock.Reset();

        var admin = await _factory.CreateLoginUserAsync();
        var target = await _factory.CreateLoginUserAsync();

        await _factory.GrantPermissionsAsync(admin.UserKey, [UAuthActions.Sessions.RevokeRootAdmin]);

        var adminDevice = $"root-recreate-admin-{Guid.NewGuid():N}";

        var oldTargetDevice = $"root-recreate-target-old-{Guid.NewGuid():N}";

        using var adminClient = CreateClient(adminDevice);
        using var oldTargetClient = CreateClient(oldTargetDevice);

        var adminLogin = await LoginAsync(adminClient, admin.Identifier, admin.Secret);

        adminLogin.StatusCode.Should().Be(HttpStatusCode.Found);

        var adminCookie = GetSessionCookie(adminLogin);

        adminClient.DefaultRequestHeaders.Add("Cookie", adminCookie);

        var oldLogin = await LoginAsync(oldTargetClient, target.Identifier, target.Secret);

        oldLogin.StatusCode.Should().Be(HttpStatusCode.Found);

        var oldCookie = GetSessionCookie(oldLogin);

        oldTargetClient.DefaultRequestHeaders.Add("Cookie", oldCookie);

        var beforeRevoke = await GetChainsAsync(oldTargetClient);

        beforeRevoke.StatusCode.Should().Be(HttpStatusCode.OK);

        var revoke = await adminClient.PostAsync($"/auth/admin/users/{target.UserKey.Value}/sessions/revoke-root", null);

        revoke.StatusCode.Should().Be(HttpStatusCode.OK);

        var oldSessionAfterRevoke = await GetChainsAsync(oldTargetClient);
        oldSessionAfterRevoke.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        using var newTargetClient = CreateClient($"root-recreate-target-new-{Guid.NewGuid():N}");

        var newLogin = await LoginAsync(newTargetClient, target.Identifier, target.Secret);
        newLogin.StatusCode.Should().Be(HttpStatusCode.Found);

        var newCookie = GetSessionCookie(newLogin);

        newCookie.Should().NotBeNullOrWhiteSpace();
        newCookie.Should().NotBe(oldCookie);

        newTargetClient.DefaultRequestHeaders.Add("Cookie", newCookie);

        var newSessionVerification = await GetChainsAsync(newTargetClient);
        newSessionVerification.StatusCode.Should().Be(HttpStatusCode.OK);

        var oldSessionVerification = await GetChainsAsync(oldTargetClient);
        oldSessionVerification.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var adminVerification = await GetChainsAsync(adminClient);
        adminVerification.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RevokeRootAdmin_ConcurrentSubsequentLogins_ShouldCreateSingleActiveRoot()
    {
        _factory.Clock.Reset();

        var admin = await _factory.CreateLoginUserAsync();
        var target = await _factory.CreateLoginUserAsync();

        await _factory.GrantPermissionsAsync(
            admin.UserKey,
            [UAuthActions.Sessions.RevokeRootAdmin]);

        using var adminClient =
            CreateClient($"root-concurrent-admin-{Guid.NewGuid():N}");

        using var initialTargetClient =
            CreateClient($"root-concurrent-initial-{Guid.NewGuid():N}");

        //
        // Establish initial authentication root.
        //
        var adminLogin = await LoginAsync(
            adminClient,
            admin.Identifier,
            admin.Secret);

        adminLogin.StatusCode.Should().Be(HttpStatusCode.Found);

        adminClient.DefaultRequestHeaders.Add(
            "Cookie",
            GetSessionCookie(adminLogin));

        var initialLogin = await LoginAsync(
            initialTargetClient,
            target.Identifier,
            target.Secret);

        initialLogin.StatusCode.Should().Be(HttpStatusCode.Found);

        var initialCookie = GetSessionCookie(initialLogin);

        initialTargetClient.DefaultRequestHeaders.Add(
            "Cookie",
            initialCookie);

        //
        // Revoke the target's current authentication root.
        //
        var revoke = await adminClient.PostAsync(
            $"/auth/admin/users/{target.UserKey.Value}/sessions/revoke-root",
            null);

        revoke.StatusCode.Should().Be(HttpStatusCode.OK);

        var oldSessionVerification =
            await GetChainsAsync(initialTargetClient);

        oldSessionVerification.StatusCode
            .Should().Be(HttpStatusCode.Unauthorized);

        //
        // Two devices authenticate concurrently after there is
        // no active root.
        //
        using var clientA =
            CreateClient($"root-concurrent-a-{Guid.NewGuid():N}");

        using var clientB =
            CreateClient($"root-concurrent-b-{Guid.NewGuid():N}");

        var loginTaskA = LoginAsync(
            clientA,
            target.Identifier,
            target.Secret);

        var loginTaskB = LoginAsync(
            clientB,
            target.Identifier,
            target.Secret);

        var responses = await Task.WhenAll(
            loginTaskA,
            loginTaskB);

        responses.Should().OnlyContain(
            x => x.StatusCode == HttpStatusCode.Found);

        var cookieA = GetSessionCookie(responses[0]);
        var cookieB = GetSessionCookie(responses[1]);

        cookieA.Should().NotBeNullOrWhiteSpace();
        cookieB.Should().NotBeNullOrWhiteSpace();

        clientA.DefaultRequestHeaders.Add(
            "Cookie",
            cookieA);

        clientB.DefaultRequestHeaders.Add(
            "Cookie",
            cookieB);

        //
        // Both sessions must belong to the surviving active
        // authentication generation and remain usable.
        //
        var verificationA = await GetChainsAsync(clientA);
        var verificationB = await GetChainsAsync(clientB);

        verificationA.StatusCode.Should().Be(HttpStatusCode.OK);
        verificationB.StatusCode.Should().Be(HttpStatusCode.OK);

        var chainsA = await verificationA.Content
            .ReadFromJsonAsync<PagedResult<SessionChainSummary>>();

        chainsA.Should().NotBeNull();

        chainsA!.Items
            .Count(x => !x.IsRevoked)
            .Should().Be(2);

        chainsA.Items
            .Count(x => x.IsCurrentDevice)
            .Should().Be(1);

        var chainsB = await verificationB.Content
            .ReadFromJsonAsync<PagedResult<SessionChainSummary>>();

        chainsB.Should().NotBeNull();

        chainsB!.Items
            .Count(x => !x.IsRevoked)
            .Should().Be(2);

        chainsB.Items
            .Count(x => x.IsCurrentDevice)
            .Should().Be(1);

        //
        // Historical authentication material must remain invalid.
        //
        var oldVerification =
            await GetChainsAsync(initialTargetClient);

        oldVerification.StatusCode
            .Should().Be(HttpStatusCode.Unauthorized);

        //
        // Admin session must be unaffected.
        //
        var adminVerification =
            await GetChainsAsync(adminClient);

        adminVerification.StatusCode
            .Should().Be(HttpStatusCode.OK);
    }

    // ---------------------------------------------------------
    // Existing LogoutAdminTests helper can be moved to a shared
    // integration-test helper later if desired.
    // ---------------------------------------------------------

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

    private static Task<HttpResponseMessage> LoginAsync(HttpClient client, string identifier, string secret)
    {
        return client.PostAsJsonAsync(
            "/auth/login",
            new
            {
                identifier,
                secret
            });
    }

    private static string GetSessionCookie(HttpResponseMessage response)
    {
        response.StatusCode.Should()
            .Be(HttpStatusCode.Found);

        response.Headers.TryGetValues(
            "Set-Cookie",
            out var values).Should().BeTrue();

        var cookie = values!
            .First(x =>
                x.StartsWith(
                    "uas=",
                    StringComparison.OrdinalIgnoreCase));

        return cookie.Split(';', 2)[0];
    }

    private static Task<HttpResponseMessage> GetChainsAsync(HttpClient client)
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
