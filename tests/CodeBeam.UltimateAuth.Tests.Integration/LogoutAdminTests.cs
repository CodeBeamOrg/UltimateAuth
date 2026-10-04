using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Tests.Integration.Infrastructure;
using CodeBeam.UltimateAuth.Users.Contracts;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;

namespace CodeBeam.UltimateAuth.Tests.Integration;

public sealed class LogoutAdminTests : IClassFixture<AuthServerFactory>
{
    private readonly AuthServerFactory _factory;

    public LogoutAdminTests(AuthServerFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task LogoutDeviceAdmin_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        _factory.Clock.Reset();

        var target = await _factory.CreateLoginUserAsync();

        using var targetClient = CreateClient(
            $"admin-logout-device-target-{Guid.NewGuid():N}");

        var targetCookie = GetSessionCookie(
            await LoginAsync(
                targetClient,
                target.Identifier,
                target.Secret));

        targetClient.DefaultRequestHeaders.Add(
            "Cookie",
            targetCookie);

        var chainsResponse = await GetChainsAsync(targetClient);

        chainsResponse.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        var chains = await chainsResponse.Content
            .ReadFromJsonAsync<PagedResult<SessionChainSummary>>();

        chains.Should().NotBeNull();

        var targetChain = chains!.Items
            .Single(x => x.IsCurrentDevice);

        using var anonymousClient = CreateClient(
            $"admin-logout-device-anonymous-{Guid.NewGuid():N}");

        var response = await anonymousClient.PostAsJsonAsync(
            $"/auth/admin/users/{target.UserKey.Value}/logout-device",
            new LogoutDeviceRequest
            {
                ChainId = targetChain.ChainId
            });

        response.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);

        var targetVerification =
            await GetChainsAsync(targetClient);

        targetVerification.StatusCode.Should()
            .Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task LogoutDeviceAdmin_WithoutRequiredPermission_ShouldReturnForbidden()
    {
        _factory.Clock.Reset();

        var actor = await _factory.CreateLoginUserAsync();
        var target = await _factory.CreateLoginUserAsync();

        using var actorClient = CreateClient(
            $"admin-logout-device-unprivileged-{Guid.NewGuid():N}");

        using var targetClient = CreateClient(
            $"admin-logout-device-target-{Guid.NewGuid():N}");

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

        var targetChainsResponse =
            await GetChainsAsync(targetClient);

        targetChainsResponse.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        var targetChains = await targetChainsResponse.Content
            .ReadFromJsonAsync<PagedResult<SessionChainSummary>>();

        targetChains.Should().NotBeNull();

        var targetChain = targetChains!.Items
            .Single(x => x.IsCurrentDevice);

        var response = await actorClient.PostAsJsonAsync(
            $"/auth/admin/users/{target.UserKey.Value}/logout-device",
            new LogoutDeviceRequest
            {
                ChainId = targetChain.ChainId
            });

        response.StatusCode.Should()
            .Be(HttpStatusCode.Forbidden);

        var targetVerification =
            await GetChainsAsync(targetClient);

        targetVerification.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        var actorVerification =
            await GetChainsAsync(actorClient);

        actorVerification.StatusCode.Should()
            .Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task LogoutAllAdmin_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        _factory.Clock.Reset();

        var target = await _factory.CreateLoginUserAsync();

        using var targetClient = CreateClient(
            $"admin-logout-all-target-{Guid.NewGuid():N}");

        var targetCookie = GetSessionCookie(
            await LoginAsync(
                targetClient,
                target.Identifier,
                target.Secret));

        targetClient.DefaultRequestHeaders.Add(
            "Cookie",
            targetCookie);

        using var anonymousClient = CreateClient(
            $"admin-logout-all-anonymous-{Guid.NewGuid():N}");

        var response = await anonymousClient.PostAsync(
            $"/auth/admin/users/{target.UserKey.Value}/logout-all",
            null);

        response.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);

        //
        // Target authority must survive rejected request.
        //
        (await GetChainsAsync(targetClient))
            .StatusCode.Should()
            .Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task LogoutAllAdmin_WithoutRequiredPermission_ShouldReturnForbidden()
    {
        _factory.Clock.Reset();

        var actor = await _factory.CreateLoginUserAsync();
        var target = await _factory.CreateLoginUserAsync();

        using var actorClient = CreateClient(
            $"admin-logout-all-unprivileged-{Guid.NewGuid():N}");

        using var targetClient = CreateClient(
            $"admin-logout-all-target-{Guid.NewGuid():N}");

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

        var response = await actorClient.PostAsync(
            $"/auth/admin/users/{target.UserKey.Value}/logout-all",
            null);

        response.StatusCode.Should()
            .Be(HttpStatusCode.Forbidden);

        //
        // Denied admin command must be mutation-free.
        //
        (await GetChainsAsync(targetClient))
            .StatusCode.Should()
            .Be(HttpStatusCode.OK);

        (await GetChainsAsync(actorClient))
            .StatusCode.Should()
            .Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task LogoutDeviceAdmin_WithPermission_ShouldInvalidateTargetDeviceSession()
    {
        _factory.Clock.Reset();

        var admin = await _factory.CreateLoginUserAsync();
        var target = await _factory.CreateLoginUserAsync();

        await _factory.GrantPermissionsAsync(
            admin.UserKey,
            [
                UAuthActions.Flows.LogoutDeviceAdmin
            ]);

        var adminDevice =
            $"logout-admin-{Guid.NewGuid():N}";

        var targetDevice =
            $"logout-target-{Guid.NewGuid():N}";

        using var adminClient =
            CreateClient(adminDevice);

        using var targetClient =
            CreateClient(targetDevice);

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

        //
        // Resolve target's chain.
        //
        var targetChainsResponse =
            await GetChainsAsync(targetClient);

        targetChainsResponse.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        var targetChains = await targetChainsResponse.Content
            .ReadFromJsonAsync<PagedResult<SessionChainSummary>>();

        targetChains.Should().NotBeNull();

        var targetChain = targetChains!.Items
            .Single(x => x.IsCurrentDevice);

        //
        // Admin logs out target device.
        //
        var response = await adminClient.PostAsJsonAsync(
            $"/auth/admin/users/{target.UserKey.Value}/logout-device",
            new LogoutDeviceRequest
            {
                ChainId = targetChain.ChainId
            });

        response.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        //
        // Target's existing session must now be unusable.
        //
        var targetVerification =
            await GetChainsAsync(targetClient);

        targetVerification.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);

        //
        // Administrative action must not destroy actor's session.
        //
        var adminVerification =
            await GetChainsAsync(adminClient);

        adminVerification.StatusCode.Should()
            .Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task LogoutDeviceAdmin_ShouldNotInvalidateTargetUsersOtherDevice()
    {
        _factory.Clock.Reset();

        var admin = await _factory.CreateLoginUserAsync();
        var target = await _factory.CreateLoginUserAsync();

        await _factory.GrantPermissionsAsync(
            admin.UserKey,
            [
                UAuthActions.Flows.LogoutDeviceAdmin
            ]);

        using var adminClient = CreateClient(
            $"logout-admin-{Guid.NewGuid():N}");

        using var targetClient1 = CreateClient(
            $"logout-target-1-{Guid.NewGuid():N}");

        using var targetClient2 = CreateClient(
            $"logout-target-2-{Guid.NewGuid():N}");

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

        adminClient.DefaultRequestHeaders.Add(
            "Cookie",
            adminCookie);

        targetClient1.DefaultRequestHeaders.Add(
            "Cookie",
            targetCookie1);

        targetClient2.DefaultRequestHeaders.Add(
            "Cookie",
            targetCookie2);

        //
        // Resolve device 1 chain.
        //
        var chainsResponse =
            await GetChainsAsync(targetClient1);

        chainsResponse.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        var chains = await chainsResponse.Content
            .ReadFromJsonAsync<PagedResult<SessionChainSummary>>();

        chains.Should().NotBeNull();

        var targetChain = chains!.Items
            .Single(x => x.IsCurrentDevice);

        //
        // Admin terminates only device 1.
        //
        var response = await adminClient.PostAsJsonAsync(
            $"/auth/admin/users/{target.UserKey.Value}/logout-device",
            new LogoutDeviceRequest
            {
                ChainId = targetChain.ChainId
            });

        response.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        //
        // Device 1 is dead.
        //
        (await GetChainsAsync(targetClient1))
            .StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);

        //
        // Device 2 must survive.
        //
        (await GetChainsAsync(targetClient2))
            .StatusCode.Should()
            .Be(HttpStatusCode.OK);

        //
        // Admin must survive as well.
        //
        (await GetChainsAsync(adminClient))
            .StatusCode.Should()
            .Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task LogoutDeviceAdmin_ShouldNotAllowChainFromDifferentTargetUser()
    {
        _factory.Clock.Reset();

        var admin = await _factory.CreateLoginUserAsync();
        var targetA = await _factory.CreateLoginUserAsync();
        var targetB = await _factory.CreateLoginUserAsync();

        await _factory.GrantPermissionsAsync(
            admin.UserKey,
            [
                UAuthActions.Flows.LogoutDeviceAdmin
            ]);

        using var adminClient = CreateClient(
            $"logout-admin-{Guid.NewGuid():N}");

        using var targetAClient = CreateClient(
            $"logout-target-a-{Guid.NewGuid():N}");

        using var targetBClient = CreateClient(
            $"logout-target-b-{Guid.NewGuid():N}");

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
        var targetBChainsResponse =
            await GetChainsAsync(targetBClient);

        targetBChainsResponse.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        var targetBChains = await targetBChainsResponse.Content
            .ReadFromJsonAsync<PagedResult<SessionChainSummary>>();

        targetBChains.Should().NotBeNull();

        var targetBChain = targetBChains!.Items
            .Single(x => x.IsCurrentDevice);

        //
        // URL says target A, but supplied chain belongs to B.
        //
        var response = await adminClient.PostAsJsonAsync(
            $"/auth/admin/users/{targetA.UserKey.Value}/logout-device",
            new LogoutDeviceRequest
            {
                ChainId = targetBChain.ChainId
            });

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Forbidden,
            HttpStatusCode.NotFound);

        //
        // Most important assertion:
        // B's session must NOT have been mutated.
        //
        (await GetChainsAsync(targetBClient))
            .StatusCode.Should()
            .Be(HttpStatusCode.OK);

        //
        // A must also remain untouched.
        //
        (await GetChainsAsync(targetAClient))
            .StatusCode.Should()
            .Be(HttpStatusCode.OK);

        //
        // Admin remains authenticated.
        //
        (await GetChainsAsync(adminClient))
            .StatusCode.Should()
            .Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task LogoutOthersAdmin_ShouldLogoutAllTargetDevicesExceptSpecifiedChain()
    {
        _factory.Clock.Reset();

        var admin = await _factory.CreateLoginUserAsync();
        var target = await _factory.CreateLoginUserAsync();

        await _factory.GrantPermissionsAsync(
            admin.UserKey,
            [
                UAuthActions.Flows.LogoutOthersAdmin
            ]);

        using var adminClient = CreateClient($"logout-others-admin-{Guid.NewGuid():N}");

        using var targetClient1 = CreateClient($"logout-others-target-1-{Guid.NewGuid():N}");

        using var targetClient2 = CreateClient($"logout-others-target-2-{Guid.NewGuid():N}");

        using var targetClient3 = CreateClient($"logout-others-target-3-{Guid.NewGuid():N}");

        var adminCookie = GetSessionCookie(await LoginAsync(adminClient, admin.Identifier, admin.Secret));

        var cookie1 = GetSessionCookie(await LoginAsync(targetClient1, target.Identifier, target.Secret));

        var cookie2 = GetSessionCookie(await LoginAsync(targetClient2, target.Identifier, target.Secret));

        var cookie3 = GetSessionCookie(await LoginAsync(targetClient3, target.Identifier, target.Secret));

        adminClient.DefaultRequestHeaders.Add("Cookie", adminCookie);
        targetClient1.DefaultRequestHeaders.Add("Cookie", cookie1);
        targetClient2.DefaultRequestHeaders.Add("Cookie", cookie2);
        targetClient3.DefaultRequestHeaders.Add("Cookie", cookie3);

        var chainsResponse = await GetChainsAsync(targetClient1);

        chainsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var chains = await chainsResponse.Content.ReadFromJsonAsync<PagedResult<SessionChainSummary>>();

        chains.Should().NotBeNull();

        var currentChain = chains!.Items.Single(x => x.IsCurrentDevice);

        var response = await adminClient.PostAsJsonAsync(
            $"/auth/admin/users/{target.UserKey.Value}/logout-others",
            new LogoutOtherDevicesRequest
            {
                CurrentChainId = currentChain.ChainId
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        (await GetChainsAsync(targetClient1)).StatusCode.Should().Be(HttpStatusCode.OK);

        (await GetChainsAsync(targetClient2)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        (await GetChainsAsync(targetClient3)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        (await GetChainsAsync(adminClient)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task LogoutOthersAdmin_WithoutRequiredPermission_ShouldReturnForbiddenAndNotMutateSessions()
    {
        _factory.Clock.Reset();

        var actor = await _factory.CreateLoginUserAsync();
        var target = await _factory.CreateLoginUserAsync();

        using var actorClient = CreateClient(
            $"logout-others-actor-{Guid.NewGuid():N}");

        using var targetClient1 = CreateClient(
            $"logout-others-target-1-{Guid.NewGuid():N}");

        using var targetClient2 = CreateClient(
            $"logout-others-target-2-{Guid.NewGuid():N}");

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

        var chainsResponse =
            await GetChainsAsync(targetClient1);

        var chains = await chainsResponse.Content
            .ReadFromJsonAsync<PagedResult<SessionChainSummary>>();

        chains.Should().NotBeNull();

        var currentChain = chains!.Items
            .Single(x => x.IsCurrentDevice);

        var response = await actorClient.PostAsJsonAsync(
            $"/auth/admin/users/{target.UserKey.Value}/logout-others",
            new LogoutOtherDevicesRequest
            {
                CurrentChainId = currentChain.ChainId
            });

        response.StatusCode.Should()
            .Be(HttpStatusCode.Forbidden);

        //
        // Authorization denial must be mutation-free.
        //
        (await GetChainsAsync(targetClient1))
            .StatusCode.Should()
            .Be(HttpStatusCode.OK);

        (await GetChainsAsync(targetClient2))
            .StatusCode.Should()
            .Be(HttpStatusCode.OK);

        (await GetChainsAsync(actorClient))
            .StatusCode.Should()
            .Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task LogoutOthersAdmin_ShouldRejectCurrentChainFromDifferentUser()
    {
        _factory.Clock.Reset();

        var admin = await _factory.CreateLoginUserAsync();
        var target = await _factory.CreateLoginUserAsync();
        var otherUser = await _factory.CreateLoginUserAsync();

        await _factory.GrantPermissionsAsync(
            admin.UserKey,
            [
                UAuthActions.Flows.LogoutOthersAdmin
            ]);

        using var adminClient = CreateClient(
            $"logout-others-admin-{Guid.NewGuid():N}");

        using var targetClient1 = CreateClient(
            $"logout-others-target-1-{Guid.NewGuid():N}");

        using var targetClient2 = CreateClient(
            $"logout-others-target-2-{Guid.NewGuid():N}");

        using var otherClient = CreateClient(
            $"logout-others-other-{Guid.NewGuid():N}");

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

        var otherCookie = GetSessionCookie(
            await LoginAsync(
                otherClient,
                otherUser.Identifier,
                otherUser.Secret));

        adminClient.DefaultRequestHeaders.Add("Cookie", adminCookie);
        targetClient1.DefaultRequestHeaders.Add("Cookie", targetCookie1);
        targetClient2.DefaultRequestHeaders.Add("Cookie", targetCookie2);
        otherClient.DefaultRequestHeaders.Add("Cookie", otherCookie);

        //
        // Get a chain belonging to a completely different user.
        //
        var otherChainsResponse =
            await GetChainsAsync(otherClient);

        var otherChains = await otherChainsResponse.Content
            .ReadFromJsonAsync<PagedResult<SessionChainSummary>>();

        otherChains.Should().NotBeNull();

        var foreignChain = otherChains!.Items
            .Single(x => x.IsCurrentDevice);

        //
        // URL target = target
        // CurrentChainId = otherUser's chain
        //
        var response = await adminClient.PostAsJsonAsync(
            $"/auth/admin/users/{target.UserKey.Value}/logout-others",
            new LogoutOtherDevicesRequest
            {
                CurrentChainId = foreignChain.ChainId
            });

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Forbidden,
            HttpStatusCode.NotFound);

        //
        // Invalid target/chain combination must cause NO mutation.
        //
        (await GetChainsAsync(targetClient1))
            .StatusCode.Should()
            .Be(HttpStatusCode.OK);

        (await GetChainsAsync(targetClient2))
            .StatusCode.Should()
            .Be(HttpStatusCode.OK);

        (await GetChainsAsync(otherClient))
            .StatusCode.Should()
            .Be(HttpStatusCode.OK);

        (await GetChainsAsync(adminClient))
            .StatusCode.Should()
            .Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task LogoutOthersAdmin_WithSingleTargetDevice_ShouldPreserveThatDevice()
    {
        _factory.Clock.Reset();

        var admin = await _factory.CreateLoginUserAsync();
        var target = await _factory.CreateLoginUserAsync();

        await _factory.GrantPermissionsAsync(
            admin.UserKey,
            [
                UAuthActions.Flows.LogoutOthersAdmin
            ]);

        using var adminClient = CreateClient(
            $"logout-others-admin-{Guid.NewGuid():N}");

        using var targetClient = CreateClient(
            $"logout-others-target-{Guid.NewGuid():N}");

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

        var chainsResponse =
            await GetChainsAsync(targetClient);

        chainsResponse.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        var chains = await chainsResponse.Content
            .ReadFromJsonAsync<PagedResult<SessionChainSummary>>();

        chains.Should().NotBeNull();

        var currentChain = chains!.Items
            .Single(x => x.IsCurrentDevice);

        var response = await adminClient.PostAsJsonAsync(
            $"/auth/admin/users/{target.UserKey.Value}/logout-others",
            new LogoutOtherDevicesRequest
            {
                CurrentChainId = currentChain.ChainId
            });

        response.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        //
        // There were no "other" devices.
        // The preserved device must remain usable.
        //
        (await GetChainsAsync(targetClient))
            .StatusCode.Should()
            .Be(HttpStatusCode.OK);

        //
        // Administrative actor must also remain unaffected.
        //
        (await GetChainsAsync(adminClient))
            .StatusCode.Should()
            .Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task LogoutOthersAdmin_WhenRepeated_ShouldRemainIdempotent()
    {
        _factory.Clock.Reset();

        var admin = await _factory.CreateLoginUserAsync();
        var target = await _factory.CreateLoginUserAsync();

        await _factory.GrantPermissionsAsync(
            admin.UserKey,
            [
                UAuthActions.Flows.LogoutOthersAdmin
            ]);

        using var adminClient = CreateClient(
            $"logout-others-admin-{Guid.NewGuid():N}");

        using var targetClient1 = CreateClient(
            $"logout-others-target-1-{Guid.NewGuid():N}");

        using var targetClient2 = CreateClient(
            $"logout-others-target-2-{Guid.NewGuid():N}");

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

        adminClient.DefaultRequestHeaders.Add(
            "Cookie",
            adminCookie);

        targetClient1.DefaultRequestHeaders.Add(
            "Cookie",
            targetCookie1);

        targetClient2.DefaultRequestHeaders.Add(
            "Cookie",
            targetCookie2);

        var chainsResponse =
            await GetChainsAsync(targetClient1);

        var chains = await chainsResponse.Content
            .ReadFromJsonAsync<PagedResult<SessionChainSummary>>();

        chains.Should().NotBeNull();

        var preservedChain = chains!.Items
            .Single(x => x.IsCurrentDevice);

        var request = new LogoutOtherDevicesRequest
        {
            CurrentChainId = preservedChain.ChainId
        };

        var url =
            $"/auth/admin/users/{target.UserKey.Value}/logout-others";

        var first = await adminClient.PostAsJsonAsync(
            url,
            request);

        first.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        var second = await adminClient.PostAsJsonAsync(
            url,
            request);

        second.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        //
        // Preserved chain survives both operations.
        //
        (await GetChainsAsync(targetClient1))
            .StatusCode.Should()
            .Be(HttpStatusCode.OK);

        //
        // Revoked/detached device must not become usable again.
        //
        (await GetChainsAsync(targetClient2))
            .StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);

        //
        // Admin remains authenticated.
        //
        (await GetChainsAsync(adminClient))
            .StatusCode.Should()
            .Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task LogoutAllAdmin_ShouldLogoutAllTargetDevices()
    {
        _factory.Clock.Reset();

        var admin = await _factory.CreateLoginUserAsync();
        var target = await _factory.CreateLoginUserAsync();

        await _factory.GrantPermissionsAsync(
            admin.UserKey,
            [
                UAuthActions.Flows.LogoutAllAdmin
            ]);

        using var adminClient = CreateClient(
            $"logout-all-admin-{Guid.NewGuid():N}");

        using var targetClient1 = CreateClient(
            $"logout-all-target-1-{Guid.NewGuid():N}");

        using var targetClient2 = CreateClient(
            $"logout-all-target-2-{Guid.NewGuid():N}");

        using var targetClient3 = CreateClient(
            $"logout-all-target-3-{Guid.NewGuid():N}");

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

        var targetCookie3 = GetSessionCookie(
            await LoginAsync(
                targetClient3,
                target.Identifier,
                target.Secret));

        adminClient.DefaultRequestHeaders.Add(
            "Cookie",
            adminCookie);

        targetClient1.DefaultRequestHeaders.Add(
            "Cookie",
            targetCookie1);

        targetClient2.DefaultRequestHeaders.Add(
            "Cookie",
            targetCookie2);

        targetClient3.DefaultRequestHeaders.Add(
            "Cookie",
            targetCookie3);

        //
        // Verify all target sessions are initially usable.
        //
        (await GetChainsAsync(targetClient1))
            .StatusCode.Should()
            .Be(HttpStatusCode.OK);

        (await GetChainsAsync(targetClient2))
            .StatusCode.Should()
            .Be(HttpStatusCode.OK);

        (await GetChainsAsync(targetClient3))
            .StatusCode.Should()
            .Be(HttpStatusCode.OK);

        //
        // Admin logs out every device belonging to target.
        //
        var response = await adminClient.PostAsync(
            $"/auth/admin/users/{target.UserKey.Value}/logout-all",
            null);

        response.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        //
        // Every target session must now be unusable.
        //
        (await GetChainsAsync(targetClient1))
            .StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);

        (await GetChainsAsync(targetClient2))
            .StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);

        (await GetChainsAsync(targetClient3))
            .StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LogoutAllAdmin_ShouldNotInvalidateAdminSession()
    {
        _factory.Clock.Reset();

        var admin = await _factory.CreateLoginUserAsync();
        var target = await _factory.CreateLoginUserAsync();

        await _factory.GrantPermissionsAsync(
            admin.UserKey,
            [
                UAuthActions.Flows.LogoutAllAdmin
            ]);

        using var adminClient = CreateClient(
            $"logout-all-admin-{Guid.NewGuid():N}");

        using var targetClient = CreateClient(
            $"logout-all-target-{Guid.NewGuid():N}");

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

        var response = await adminClient.PostAsync(
            $"/auth/admin/users/{target.UserKey.Value}/logout-all",
            null);

        response.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        //
        // Target must be logged out.
        //
        (await GetChainsAsync(targetClient))
            .StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);

        //
        // Administrative actor is a different user and
        // must remain authenticated.
        //
        (await GetChainsAsync(adminClient))
            .StatusCode.Should()
            .Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task LogoutAllAdmin_WithoutRequiredPermission_ShouldReturnForbiddenAndNotMutateTargetSessions()
    {
        _factory.Clock.Reset();

        var actor = await _factory.CreateLoginUserAsync();
        var target = await _factory.CreateLoginUserAsync();

        using var actorClient = CreateClient(
            $"logout-all-actor-{Guid.NewGuid():N}");

        using var targetClient1 = CreateClient(
            $"logout-all-target-1-{Guid.NewGuid():N}");

        using var targetClient2 = CreateClient(
            $"logout-all-target-2-{Guid.NewGuid():N}");

        var actorCookie = GetSessionCookie(
            await LoginAsync(
                actorClient,
                actor.Identifier,
                actor.Secret));

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

        actorClient.DefaultRequestHeaders.Add(
            "Cookie",
            actorCookie);

        targetClient1.DefaultRequestHeaders.Add(
            "Cookie",
            targetCookie1);

        targetClient2.DefaultRequestHeaders.Add(
            "Cookie",
            targetCookie2);

        //
        // Establish precondition.
        //
        (await GetChainsAsync(targetClient1))
            .StatusCode.Should()
            .Be(HttpStatusCode.OK);

        (await GetChainsAsync(targetClient2))
            .StatusCode.Should()
            .Be(HttpStatusCode.OK);

        //
        // Actor is authenticated but does NOT have
        // flows.logoutall.admin.
        //
        var response = await actorClient.PostAsync(
            $"/auth/admin/users/{target.UserKey.Value}/logout-all",
            null);

        response.StatusCode.Should()
            .Be(HttpStatusCode.Forbidden);

        //
        // Authorization failure must be mutation-free.
        //
        (await GetChainsAsync(targetClient1))
            .StatusCode.Should()
            .Be(HttpStatusCode.OK);

        (await GetChainsAsync(targetClient2))
            .StatusCode.Should()
            .Be(HttpStatusCode.OK);

        //
        // Actor must also remain authenticated.
        //
        (await GetChainsAsync(actorClient))
            .StatusCode.Should()
            .Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task LogoutAllAdmin_WhenRepeated_ShouldRemainIdempotent()
    {
        _factory.Clock.Reset();

        var admin = await _factory.CreateLoginUserAsync();
        var target = await _factory.CreateLoginUserAsync();

        await _factory.GrantPermissionsAsync(
            admin.UserKey,
            [
                UAuthActions.Flows.LogoutAllAdmin
            ]);

        using var adminClient = CreateClient(
            $"logout-all-admin-{Guid.NewGuid():N}");

        using var targetClient1 = CreateClient(
            $"logout-all-target-1-{Guid.NewGuid():N}");

        using var targetClient2 = CreateClient(
            $"logout-all-target-2-{Guid.NewGuid():N}");

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

        adminClient.DefaultRequestHeaders.Add(
            "Cookie",
            adminCookie);

        targetClient1.DefaultRequestHeaders.Add(
            "Cookie",
            targetCookie1);

        targetClient2.DefaultRequestHeaders.Add(
            "Cookie",
            targetCookie2);

        var url =
            $"/auth/admin/users/{target.UserKey.Value}/logout-all";

        var first = await adminClient.PostAsync(
            url,
            null);

        first.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        var second = await adminClient.PostAsync(
            url,
            null);

        second.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        //
        // Sessions must remain logged out.
        //
        (await GetChainsAsync(targetClient1))
            .StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);

        (await GetChainsAsync(targetClient2))
            .StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);

        //
        // Repeating the operation must not affect
        // the administrative actor.
        //
        (await GetChainsAsync(adminClient))
            .StatusCode.Should()
            .Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task LogoutAllAdmin_ShouldNotAffectUnrelatedUser()
    {
        _factory.Clock.Reset();

        var admin = await _factory.CreateLoginUserAsync();
        var target = await _factory.CreateLoginUserAsync();
        var unrelated = await _factory.CreateLoginUserAsync();

        await _factory.GrantPermissionsAsync(
            admin.UserKey,
            [
                UAuthActions.Flows.LogoutAllAdmin
            ]);

        using var adminClient = CreateClient(
            $"logout-all-admin-{Guid.NewGuid():N}");

        using var targetClient = CreateClient(
            $"logout-all-target-{Guid.NewGuid():N}");

        using var unrelatedClient = CreateClient(
            $"logout-all-unrelated-{Guid.NewGuid():N}");

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

        adminClient.DefaultRequestHeaders.Add(
            "Cookie",
            adminCookie);

        targetClient.DefaultRequestHeaders.Add(
            "Cookie",
            targetCookie);

        unrelatedClient.DefaultRequestHeaders.Add(
            "Cookie",
            unrelatedCookie);

        var response = await adminClient.PostAsync(
            $"/auth/admin/users/{target.UserKey.Value}/logout-all",
            null);

        response.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        //
        // Target is logged out.
        //
        (await GetChainsAsync(targetClient))
            .StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);

        //
        // Completely unrelated user remains authenticated.
        //
        (await GetChainsAsync(unrelatedClient))
            .StatusCode.Should()
            .Be(HttpStatusCode.OK);

        //
        // Administrative actor remains authenticated.
        //
        (await GetChainsAsync(adminClient))
            .StatusCode.Should()
            .Be(HttpStatusCode.OK);
    }


    // ---------------------------------------------------------
    // Helpers
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

    private static async Task<HttpResponseMessage> LoginAsync(HttpClient client, string identifier, string secret)
    {
        return await client.PostAsJsonAsync(
            "/auth/login",
            new
            {
                identifier,
                secret
            });
    }

    private static async Task<HttpResponseMessage> GetChainsAsync(HttpClient client)
    {
        return await client.PostAsJsonAsync(
            "/auth/me/sessions/chains",
            new PageRequest
            {
                PageNumber = 1,
                PageSize = 50
            });
    }

    private static string GetSessionCookie(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.Found);
        response.Headers.TryGetValues("Set-Cookie", out var values).Should().BeTrue();

        var cookie = values!.FirstOrDefault(x => x.StartsWith("uas=", StringComparison.OrdinalIgnoreCase));
        cookie.Should().NotBeNullOrWhiteSpace();

        return cookie!;
    }
}
