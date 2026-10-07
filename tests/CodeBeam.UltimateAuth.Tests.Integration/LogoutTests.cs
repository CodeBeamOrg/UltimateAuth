using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Tests.Integration.Infrastructure;
using CodeBeam.UltimateAuth.Users.Contracts;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;

namespace CodeBeam.UltimateAuth.Tests.Integration;

public class LogoutTests : IClassFixture<AuthServerFactory>
{
    private readonly AuthServerFactory _factory;

    public LogoutTests(AuthServerFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Logout_WithAuthenticatedSession_ShouldSucceed()
    {
        _factory.Clock.Reset();

        var user = await _factory.CreateLoginUserAsync();
        var deviceId = $"logout-basic-{Guid.NewGuid():N}";

        using var client = CreateClient(deviceId);

        var loginResponse = await LoginAsync(
            client,
            user.Identifier,
            user.Secret);

        loginResponse.StatusCode.Should().Be(HttpStatusCode.Found);

        var cookie = GetSessionCookie(loginResponse);

        client.DefaultRequestHeaders.Add("Cookie", cookie);

        var logoutResponse = await client.PostAsync(
            "/auth/logout",
            null);

        logoutResponse.StatusCode.Should().Be(HttpStatusCode.Found);
    }

    [Fact]
    public async Task Logout_ShouldDeleteSessionCookie()
    {
        _factory.Clock.Reset();

        var user = await _factory.CreateLoginUserAsync();
        var deviceId = $"logout-cookie-{Guid.NewGuid():N}";

        using var client = CreateClient(deviceId);

        var loginResponse = await LoginAsync(
            client,
            user.Identifier,
            user.Secret);

        var cookie = GetSessionCookie(loginResponse);

        client.DefaultRequestHeaders.Add("Cookie", cookie);

        var logoutResponse = await client.PostAsync(
            "/auth/logout",
            null);

        logoutResponse.Headers
            .TryGetValues("Set-Cookie", out var cookies)
            .Should()
            .BeTrue();

        cookies.Should().NotBeNullOrEmpty();

        var deletionCookie = cookies!
            .FirstOrDefault(x =>
                x.Contains(
                    "uas",
                    StringComparison.OrdinalIgnoreCase));

        deletionCookie.Should().NotBeNullOrWhiteSpace();

        deletionCookie.Should().MatchRegex(
            "(?i)(expires=|max-age=0)");
    }

    [Fact]
    public async Task Logout_ShouldInvalidateSessionOnServer()
    {
        _factory.Clock.Reset();

        var user = await _factory.CreateLoginUserAsync();
        var deviceId = $"logout-invalidation-{Guid.NewGuid():N}";

        using var client = CreateClient(deviceId);

        var loginResponse = await LoginAsync(
            client,
            user.Identifier,
            user.Secret);

        var cookie = GetSessionCookie(loginResponse);

        client.DefaultRequestHeaders.Add("Cookie", cookie);

        var beforeLogout = await GetChainsAsync(client);
        beforeLogout.StatusCode.Should().Be(HttpStatusCode.OK);

        var logoutResponse = await client.PostAsync("/auth/logout", null);
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.Found);

        //
        // IMPORTANT:
        // Deliberately keep sending the old cookie.
        //
        // HandleCookies=false means the logout Set-Cookie response cannot
        // magically remove our manually supplied Cookie header.
        //
        // Therefore this proves server-side invalidation.
        //

        var afterLogout = await GetChainsAsync(client);

        afterLogout.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_OldSessionCookie_ShouldNotAuthenticateFromNewClient()
    {
        _factory.Clock.Reset();

        var user = await _factory.CreateLoginUserAsync();
        var deviceId = $"logout-replay-{Guid.NewGuid():N}";

        using var originalClient = CreateClient(deviceId);

        var loginResponse = await LoginAsync(
            originalClient,
            user.Identifier,
            user.Secret);

        var cookie = GetSessionCookie(loginResponse);

        originalClient.DefaultRequestHeaders.Add(
            "Cookie",
            cookie);

        var logoutResponse = await originalClient.PostAsync(
            "/auth/logout",
            null);

        logoutResponse.StatusCode.Should().Be(HttpStatusCode.Found);

        //
        // Simulate an attacker / stale browser / copied credential.
        //
        // A completely new HttpClient receives the original cookie.
        //

        using var replayClient = CreateClient(deviceId);

        replayClient.DefaultRequestHeaders.Add(
            "Cookie",
            cookie);

        var replayResponse = await GetChainsAsync(replayClient);

        replayResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_ShouldNotInvalidateOtherDeviceSession()
    {
        _factory.Clock.Reset();

        var user = await _factory.CreateLoginUserAsync();

        var device1 = $"logout-device-1-{Guid.NewGuid():N}";
        var device2 = $"logout-device-2-{Guid.NewGuid():N}";

        using var client1 = CreateClient(device1);
        using var client2 = CreateClient(device2);

        var login1 = await LoginAsync(
            client1,
            user.Identifier,
            user.Secret);

        var login2 = await LoginAsync(
            client2,
            user.Identifier,
            user.Secret);

        login1.StatusCode.Should().Be(HttpStatusCode.Found);
        login2.StatusCode.Should().Be(HttpStatusCode.Found);

        var cookie1 = GetSessionCookie(login1);
        var cookie2 = GetSessionCookie(login2);

        client1.DefaultRequestHeaders.Add(
            "Cookie",
            cookie1);

        client2.DefaultRequestHeaders.Add(
            "Cookie",
            cookie2);

        var before1 = await GetChainsAsync(client1);
        var before2 = await GetChainsAsync(client2);

        before1.StatusCode.Should().Be(HttpStatusCode.OK);
        before2.StatusCode.Should().Be(HttpStatusCode.OK);

        var logout = await client1.PostAsync(
            "/auth/logout",
            null);

        logout.StatusCode.Should().Be(HttpStatusCode.Found);

        var device1AfterLogout = await GetChainsAsync(client1);
        var device2AfterLogout = await GetChainsAsync(client2);

        device1AfterLogout.StatusCode
            .Should()
            .Be(HttpStatusCode.Unauthorized);

        device2AfterLogout.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Logout_ShouldNotInvalidateAnotherUsersSession()
    {
        _factory.Clock.Reset();

        var user1 = await _factory.CreateLoginUserAsync();
        var user2 = await _factory.CreateLoginUserAsync();

        using var client1 = CreateClient(
            $"logout-user-1-{Guid.NewGuid():N}");

        using var client2 = CreateClient(
            $"logout-user-2-{Guid.NewGuid():N}");

        var login1 = await LoginAsync(
            client1,
            user1.Identifier,
            user1.Secret);

        var login2 = await LoginAsync(
            client2,
            user2.Identifier,
            user2.Secret);

        var cookie1 = GetSessionCookie(login1);
        var cookie2 = GetSessionCookie(login2);

        client1.DefaultRequestHeaders.Add(
            "Cookie",
            cookie1);

        client2.DefaultRequestHeaders.Add(
            "Cookie",
            cookie2);

        var logout = await client1.PostAsync(
            "/auth/logout",
            null);

        logout.StatusCode.Should().Be(HttpStatusCode.Found);

        var user1Response = await GetChainsAsync(client1);
        var user2Response = await GetChainsAsync(client2);

        user1Response.StatusCode
            .Should()
            .Be(HttpStatusCode.Unauthorized);

        user2Response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Logout_ShouldDetachActiveSessionFromCurrentChain()
    {
        _factory.Clock.Reset();

        var user = await _factory.CreateLoginUserAsync();
        var device = $"logout-detach-{Guid.NewGuid():N}";

        using var client = CreateClient(device);

        var login = await LoginAsync(client, user.Identifier, user.Secret);

        var cookie = GetSessionCookie(login);
        client.DefaultRequestHeaders.Add("Cookie", cookie);

        var beforeResponse = await client.PostAsJsonAsync(
            "/auth/me/sessions/chains",
            new PageRequest
            {
                PageNumber = 1,
                PageSize = 50
            });

        beforeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var before = await beforeResponse.Content.ReadFromJsonAsync<PagedResult<SessionChainSummary>>();

        before.Should().NotBeNull();

        var currentBefore = before!.Items.Single(x => x.IsCurrentDevice);

        currentBefore.ActiveSessionId.Should().NotBeNull();
        currentBefore.IsRevoked.Should().BeFalse();

        var chainId = currentBefore.ChainId;
        var sessionId = currentBefore.ActiveSessionId;

        var logout = await client.PostAsync("/auth/logout", null);
        logout.StatusCode.Should().Be(HttpStatusCode.Found);


        var oldSessionResponse = await client.PostAsJsonAsync("/auth/me/sessions/chains", new PageRequest());
        oldSessionResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);


        using var reloginClient = CreateClient(device);
        var relogin = await LoginAsync(reloginClient, user.Identifier, user.Secret);

        var newCookie = GetSessionCookie(relogin);
        reloginClient.DefaultRequestHeaders.Add("Cookie", newCookie);

        var afterResponse = await reloginClient.PostAsJsonAsync(
            "/auth/me/sessions/chains",
            new PageRequest
            {
                PageNumber = 1,
                PageSize = 50
            });

        afterResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var after = await afterResponse.Content.ReadFromJsonAsync<PagedResult<SessionChainSummary>>();
        after.Should().NotBeNull();

        var currentAfter = after!.Items.Single(x => x.IsCurrentDevice);
        currentAfter.ChainId.Should().Be(chainId);

        currentAfter.IsRevoked.Should().BeFalse();
        currentAfter.RevokedAt.Should().BeNull();

        currentAfter.ActiveSessionId.Should().NotBeNull();
        currentAfter.ActiveSessionId.Should().NotBe(sessionId);
    }

    [Fact]
    public async Task Logout_OldSession_ShouldRemainInvalidAfterLoginAgain()
    {
        _factory.Clock.Reset();

        var user = await _factory.CreateLoginUserAsync();
        var device = $"logout-old-session-{Guid.NewGuid():N}";

        using var firstClient = CreateClient(device);
        var firstLogin = await LoginAsync(firstClient, user.Identifier, user.Secret);

        var oldCookie = GetSessionCookie(firstLogin);

        firstClient.DefaultRequestHeaders.Add("Cookie", oldCookie);

        var logout = await firstClient.PostAsync("/auth/logout", null);

        logout.StatusCode.Should().Be(HttpStatusCode.Found);

        using var secondClient = CreateClient(device);
        var secondLogin = await LoginAsync(secondClient, user.Identifier, user.Secret);
        secondLogin.StatusCode.Should().Be(HttpStatusCode.Found);

        var newCookie = GetSessionCookie(secondLogin);

        newCookie.Should().NotBe(oldCookie);

        secondClient.DefaultRequestHeaders.Add("Cookie", newCookie);

        var newSessionResponse = await secondClient.PostAsJsonAsync("/auth/me/sessions/chains", new PageRequest());

        newSessionResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var replayClient = CreateClient(device);
        replayClient.DefaultRequestHeaders.Add("Cookie", oldCookie);

        var replayResponse = await replayClient.PostAsJsonAsync("/auth/me/sessions/chains", new PageRequest());
        replayResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_ShouldDetachOnlyCurrentDeviceSession()
    {
        _factory.Clock.Reset();

        var user = await _factory.CreateLoginUserAsync();

        var device1 = $"logout-device-1-{Guid.NewGuid():N}";
        var device2 = $"logout-device-2-{Guid.NewGuid():N}";

        using var client1 = CreateClient(device1);
        using var client2 = CreateClient(device2);

        var login1 = await LoginAsync(
            client1,
            user.Identifier,
            user.Secret);

        var login2 = await LoginAsync(
            client2,
            user.Identifier,
            user.Secret);

        var cookie1 = GetSessionCookie(login1);
        var cookie2 = GetSessionCookie(login2);

        client1.DefaultRequestHeaders.Add("Cookie", cookie1);
        client2.DefaultRequestHeaders.Add("Cookie", cookie2);

        var beforeResponse = await client2.PostAsJsonAsync(
            "/auth/me/sessions/chains",
            new PageRequest
            {
                PageNumber = 1,
                PageSize = 50
            });

        beforeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var before = await beforeResponse.Content
            .ReadFromJsonAsync<PagedResult<SessionChainSummary>>();

        before.Should().NotBeNull();

        var device2ChainBefore = before!.Items.Single(x => x.IsCurrentDevice);

        var device2ChainId = device2ChainBefore.ChainId;
        var device2SessionId = device2ChainBefore.ActiveSessionId;

        device2SessionId.Should().NotBeNull();

        var logout = await client1.PostAsync("/auth/logout", null);

        logout.StatusCode.Should().Be(HttpStatusCode.Found);

        var device1Result = await client1.PostAsJsonAsync(
            "/auth/me/sessions/chains",
            new PageRequest());

        device1Result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var device2Result = await client2.PostAsJsonAsync(
            "/auth/me/sessions/chains",
            new PageRequest
            {
                PageNumber = 1,
                PageSize = 50
            });

        device2Result.StatusCode.Should().Be(HttpStatusCode.OK);

        var after = await device2Result.Content.ReadFromJsonAsync<PagedResult<SessionChainSummary>>();

        after.Should().NotBeNull();

        var device2ChainAfter = after!.Items.Single(x => x.IsCurrentDevice);

        device2ChainAfter.ChainId.Should().Be(device2ChainId);
        device2ChainAfter.ActiveSessionId.Should().Be(device2SessionId);
        device2ChainAfter.IsRevoked.Should().BeFalse();
    }

    [Fact]
    public async Task Logout_ShouldNotRevokeDeviceChain()
    {
        _factory.Clock.Reset();

        var user = await _factory.CreateLoginUserAsync();
        var device = $"logout-not-revoke-{Guid.NewGuid():N}";

        using var client = CreateClient(device);

        var login = await LoginAsync(
            client,
            user.Identifier,
            user.Secret);

        var cookie = GetSessionCookie(login);
        client.DefaultRequestHeaders.Add("Cookie", cookie);

        var beforeResponse = await client.PostAsJsonAsync(
            "/auth/me/sessions/chains",
            new PageRequest
            {
                PageNumber = 1,
                PageSize = 50
            });

        var before = await beforeResponse.Content
            .ReadFromJsonAsync<PagedResult<SessionChainSummary>>();

        before.Should().NotBeNull();

        var chainBefore = before!.Items.Single(x => x.IsCurrentDevice);
        var chainId = chainBefore.ChainId;

        await client.PostAsync("/auth/logout", null);

        using var reloginClient = CreateClient(device);

        var relogin = await LoginAsync(
            reloginClient,
            user.Identifier,
            user.Secret);

        var newCookie = GetSessionCookie(relogin);

        reloginClient.DefaultRequestHeaders.Add(
            "Cookie",
            newCookie);

        var afterResponse = await reloginClient.PostAsJsonAsync(
            "/auth/me/sessions/chains",
            new PageRequest
            {
                PageNumber = 1,
                PageSize = 50
            });

        var after = await afterResponse.Content
            .ReadFromJsonAsync<PagedResult<SessionChainSummary>>();

        after.Should().NotBeNull();

        var chainAfter = after!.Items.Single(x => x.IsCurrentDevice);

        chainAfter.ChainId.Should().Be(chainId);
        chainAfter.IsRevoked.Should().BeFalse();
        chainAfter.RevokedAt.Should().BeNull();
    }

    [Fact]
    public async Task Logout_WithoutAuthentication_ShouldBeSafeAndIdempotent()
    {
        _factory.Clock.Reset();

        using var client = CreateClient(
            $"logout-anonymous-{Guid.NewGuid():N}");

        var first = await client.PostAsync(
            "/auth/logout",
            null);

        var second = await client.PostAsync(
            "/auth/logout",
            null);

        first.StatusCode.Should().Be(HttpStatusCode.Found);
        second.StatusCode.Should().Be(HttpStatusCode.Found);
    }

    [Fact]
    public async Task Logout_WithAlreadyLoggedOutSession_ShouldRemainIdempotent()
    {
        _factory.Clock.Reset();

        var user = await _factory.CreateLoginUserAsync();
        var device = $"logout-idempotent-{Guid.NewGuid():N}";

        using var client = CreateClient(device);

        var login = await LoginAsync(
            client,
            user.Identifier,
            user.Secret);

        login.StatusCode.Should().Be(HttpStatusCode.Found);

        var cookie = GetSessionCookie(login);

        client.DefaultRequestHeaders.Add(
            "Cookie",
            cookie);

        var firstLogout = await client.PostAsync(
            "/auth/logout",
            null);

        firstLogout.StatusCode.Should().Be(HttpStatusCode.Found);

        var secondLogout = await client.PostAsync(
            "/auth/logout",
            null);

        secondLogout.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Found);

        var authenticatedRequest = await client.PostAsJsonAsync(
            "/auth/me/sessions/chains",
            new PageRequest());

        authenticatedRequest.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_WithInvalidSessionCookie_ShouldNotAffectValidSession()
    {
        _factory.Clock.Reset();

        var user = await _factory.CreateLoginUserAsync();

        var validDevice =
            $"logout-valid-{Guid.NewGuid():N}";

        var invalidDevice =
            $"logout-invalid-{Guid.NewGuid():N}";

        using var validClient = CreateClient(validDevice);
        using var invalidClient = CreateClient(invalidDevice);

        var login = await LoginAsync(
            validClient,
            user.Identifier,
            user.Secret);

        login.StatusCode.Should().Be(HttpStatusCode.Found);

        var validCookie = GetSessionCookie(login);

        validClient.DefaultRequestHeaders.Add(
            "Cookie",
            validCookie);

        //
        // Deliberately forged / nonexistent session credential.
        //
        invalidClient.DefaultRequestHeaders.Add(
            "Cookie",
            $"uas={Guid.NewGuid():N}");

        var invalidLogout = await invalidClient.PostAsync(
            "/auth/logout",
            null);

        invalidLogout.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Found);

        //
        // Existing legitimate session must be untouched.
        //
        var validSession = await validClient.PostAsJsonAsync(
            "/auth/me/sessions/chains",
            new PageRequest
            {
                PageNumber = 1,
                PageSize = 50
            });

        validSession.StatusCode.Should().Be(HttpStatusCode.OK);

        var chains = await validSession.Content
            .ReadFromJsonAsync<PagedResult<SessionChainSummary>>();

        chains.Should().NotBeNull();

        var current = chains!.Items.Single(x => x.IsCurrentDevice);

        current.ActiveSessionId.Should().NotBeNull();
        current.IsRevoked.Should().BeFalse();
    }

    [Fact]
    public async Task Logout_WithOldSessionAfterRelogin_ShouldNotLogoutNewSession()
    {
        _factory.Clock.Reset();

        var user = await _factory.CreateLoginUserAsync();
        var device = $"logout-stale-{Guid.NewGuid():N}";

        //
        // Session A
        //
        using var firstClient = CreateClient(device);

        var firstLogin = await LoginAsync(
            firstClient,
            user.Identifier,
            user.Secret);

        firstLogin.StatusCode.Should().Be(HttpStatusCode.Found);

        var oldCookie = GetSessionCookie(firstLogin);

        firstClient.DefaultRequestHeaders.Add(
            "Cookie",
            oldCookie);

        var firstLogout = await firstClient.PostAsync(
            "/auth/logout",
            null);

        firstLogout.StatusCode.Should().Be(HttpStatusCode.Found);

        //
        // Same device -> Session B
        //
        using var currentClient = CreateClient(device);

        var secondLogin = await LoginAsync(
            currentClient,
            user.Identifier,
            user.Secret);

        secondLogin.StatusCode.Should().Be(HttpStatusCode.Found);

        var newCookie = GetSessionCookie(secondLogin);

        newCookie.Should().NotBe(oldCookie);

        currentClient.DefaultRequestHeaders.Add(
            "Cookie",
            newCookie);

        var beforeReplay = await currentClient.PostAsJsonAsync(
            "/auth/me/sessions/chains",
            new PageRequest
            {
                PageNumber = 1,
                PageSize = 50
            });

        beforeReplay.StatusCode.Should().Be(HttpStatusCode.OK);

        var beforeChains = await beforeReplay.Content
            .ReadFromJsonAsync<PagedResult<SessionChainSummary>>();

        beforeChains.Should().NotBeNull();

        var currentBefore =
            beforeChains!.Items.Single(x => x.IsCurrentDevice);

        var currentChainId = currentBefore.ChainId;
        var currentSessionId = currentBefore.ActiveSessionId;

        currentSessionId.Should().NotBeNull();

        //
        // Replay Session A against /auth/logout.
        //
        using var staleClient = CreateClient(device);

        staleClient.DefaultRequestHeaders.Add(
            "Cookie",
            oldCookie);

        var staleLogout = await staleClient.PostAsync(
            "/auth/logout",
            null);

        staleLogout.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Found);

        //
        // Session B MUST still be valid.
        //
        var afterReplay = await currentClient.PostAsJsonAsync(
            "/auth/me/sessions/chains",
            new PageRequest
            {
                PageNumber = 1,
                PageSize = 50
            });

        afterReplay.StatusCode.Should().Be(HttpStatusCode.OK);

        var afterChains = await afterReplay.Content
            .ReadFromJsonAsync<PagedResult<SessionChainSummary>>();

        afterChains.Should().NotBeNull();

        var currentAfter =
            afterChains!.Items.Single(x => x.IsCurrentDevice);

        currentAfter.ChainId.Should().Be(currentChainId);
        currentAfter.ActiveSessionId.Should().Be(currentSessionId);
        currentAfter.IsRevoked.Should().BeFalse();
    }

    [Fact]
    public async Task RepeatedLogoutLoginCycles_ShouldPreserveChainAndRotateSessions()
    {
        _factory.Clock.Reset();

        var user = await _factory.CreateLoginUserAsync();
        var device = $"logout-cycle-{Guid.NewGuid():N}";

        SessionChainId? expectedChainId = null;
        var sessionIds = new List<AuthSessionId>();

        for (var i = 0; i < 3; i++)
        {
            using var client = CreateClient(device);

            var login = await LoginAsync(
                client,
                user.Identifier,
                user.Secret);

            login.StatusCode.Should().Be(HttpStatusCode.Found);

            var cookie = GetSessionCookie(login);

            client.DefaultRequestHeaders.Add(
                "Cookie",
                cookie);

            var chainsResponse = await client.PostAsJsonAsync(
                "/auth/me/sessions/chains",
                new PageRequest
                {
                    PageNumber = 1,
                    PageSize = 50
                });

            chainsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var chains = await chainsResponse.Content
                .ReadFromJsonAsync<PagedResult<SessionChainSummary>>();

            chains.Should().NotBeNull();

            var current =
                chains!.Items.Single(x => x.IsCurrentDevice);

            current.ActiveSessionId.Should().NotBeNull();
            current.IsRevoked.Should().BeFalse();

            if (expectedChainId is null)
            {
                expectedChainId = current.ChainId;
            }
            else
            {
                current.ChainId.Should().Be(expectedChainId.Value);
            }

            sessionIds.Add(current.ActiveSessionId!.Value);

            //
            // Keep final session active so we can inspect it.
            //
            if (i < 2)
            {
                var logout = await client.PostAsync(
                    "/auth/logout",
                    null);

                logout.StatusCode.Should().Be(HttpStatusCode.Found);
            }
        }

        sessionIds.Should().HaveCount(3);

        sessionIds.Distinct()
            .Should()
            .HaveCount(3);
    }

    [Fact]
    public async Task Logout_WithStaleCredential_ShouldNotAffectOtherDevice()
    {
        _factory.Clock.Reset();

        var user = await _factory.CreateLoginUserAsync();

        var device1 =
            $"logout-stale-device-1-{Guid.NewGuid():N}";

        var device2 =
            $"logout-stale-device-2-{Guid.NewGuid():N}";

        //
        // Device 1 login.
        //
        using var device1Client = CreateClient(device1);

        var login1 = await LoginAsync(
            device1Client,
            user.Identifier,
            user.Secret);

        var staleCookie = GetSessionCookie(login1);

        device1Client.DefaultRequestHeaders.Add(
            "Cookie",
            staleCookie);

        //
        // Device 2 login.
        //
        using var device2Client = CreateClient(device2);

        var login2 = await LoginAsync(
            device2Client,
            user.Identifier,
            user.Secret);

        var device2Cookie = GetSessionCookie(login2);

        device2Client.DefaultRequestHeaders.Add(
            "Cookie",
            device2Cookie);

        var device2BeforeResponse =
            await device2Client.PostAsJsonAsync(
                "/auth/me/sessions/chains",
                new PageRequest
                {
                    PageNumber = 1,
                    PageSize = 50
                });

        device2BeforeResponse.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var before = await device2BeforeResponse.Content
            .ReadFromJsonAsync<PagedResult<SessionChainSummary>>();

        before.Should().NotBeNull();

        var device2Before =
            before!.Items.Single(x => x.IsCurrentDevice);

        var device2ChainId = device2Before.ChainId;
        var device2SessionId = device2Before.ActiveSessionId;

        device2SessionId.Should().NotBeNull();

        var logout1 = await device1Client.PostAsync("/auth/logout", null);

        logout1.StatusCode.Should().Be(HttpStatusCode.Found);

        using var staleClient = CreateClient(device1);

        staleClient.DefaultRequestHeaders.Add("Cookie", staleCookie);

        var staleLogout = await staleClient.PostAsync("/auth/logout", null);

        staleLogout.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Found);

        var device2AfterResponse =
            await device2Client.PostAsJsonAsync(
                "/auth/me/sessions/chains",
                new PageRequest
                {
                    PageNumber = 1,
                    PageSize = 50
                });

        device2AfterResponse.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var after = await device2AfterResponse.Content
            .ReadFromJsonAsync<PagedResult<SessionChainSummary>>();

        after.Should().NotBeNull();

        var device2After =
            after!.Items.Single(x => x.IsCurrentDevice);

        device2After.ChainId.Should().Be(device2ChainId);
        device2After.ActiveSessionId.Should().Be(device2SessionId);
        device2After.IsRevoked.Should().BeFalse();
    }

    [Fact]
    public async Task Logout_WithAlreadyLoggedOutSession_ShouldRemainSafe()
    {
        _factory.Clock.Reset();

        var user = await _factory.CreateLoginUserAsync();
        var device = $"logout-idempotent-{Guid.NewGuid():N}";

        using var client = CreateClient(device);

        var login = await LoginAsync(
            client,
            user.Identifier,
            user.Secret);

        login.StatusCode.Should().Be(HttpStatusCode.Found);

        var cookie = GetSessionCookie(login);

        client.DefaultRequestHeaders.Add("Cookie", cookie);

        var firstLogout = await client.PostAsync("/auth/logout", null);

        firstLogout.StatusCode.Should().Be(HttpStatusCode.Found);

        var secondLogout = await client.PostAsync("/auth/logout", null);

        secondLogout.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Found);

        var authenticatedRequest = await client.PostAsJsonAsync(
            "/auth/me/sessions/chains",
            new PageRequest
            {
                PageNumber = 1,
                PageSize = 50
            });

        authenticatedRequest.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_ShouldNotReturnAuthenticationCredentials()
    {
        _factory.Clock.Reset();

        var user = await _factory.CreateLoginUserAsync();
        var device = $"logout-no-credentials-{Guid.NewGuid():N}";

        using var client = CreateClient(device);

        var login = await LoginAsync(
            client,
            user.Identifier,
            user.Secret);

        login.StatusCode.Should().Be(HttpStatusCode.Found);

        var sessionCookie = GetSessionCookie(login);

        client.DefaultRequestHeaders.Add(
            "Cookie",
            sessionCookie);

        var logout = await client.PostAsync(
            "/auth/logout",
            null);

        logout.StatusCode.Should().Be(HttpStatusCode.Found);

        //
        // Logout may legitimately contain Set-Cookie headers whose purpose
        // is credential deletion.
        //
        // It must not issue a new usable session credential.
        //
        if (logout.Headers.TryGetValues("Set-Cookie", out var setCookies))
        {
            var sessionCookies = setCookies
                .Where(x =>
                    x.StartsWith(
                        "uas=",
                        StringComparison.OrdinalIgnoreCase))
                .ToList();

            sessionCookies.Should().NotBeEmpty();

            sessionCookies.Should().OnlyContain(
                x =>
                    x.Contains(
                        "expires=",
                        StringComparison.OrdinalIgnoreCase) ||
                    x.Contains(
                        "max-age=0",
                        StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public async Task Logout_ShouldDeleteSessionCookieWithRootPath()
    {
        _factory.Clock.Reset();

        var user = await _factory.CreateLoginUserAsync();
        var device = $"logout-cookie-path-{Guid.NewGuid():N}";

        using var client = CreateClient(device);

        var login = await LoginAsync(
            client,
            user.Identifier,
            user.Secret);

        login.StatusCode.Should().Be(HttpStatusCode.Found);

        var sessionCookie = GetSessionCookie(login);

        client.DefaultRequestHeaders.Add(
            "Cookie",
            sessionCookie);

        var logout = await client.PostAsync(
            "/auth/logout",
            null);

        logout.StatusCode.Should().Be(HttpStatusCode.Found);

        logout.Headers
            .TryGetValues("Set-Cookie", out var cookies)
            .Should()
            .BeTrue();

        var deletionCookie = cookies!
            .Single(x =>
                x.StartsWith(
                    "uas=",
                    StringComparison.OrdinalIgnoreCase));

        deletionCookie.Should().ContainEquivalentOf("path=/");

        deletionCookie.Should().MatchRegex(
            "(?i)(expires=|max-age=0)");
    }

    [Fact]
    public async Task Logout_ShouldNotReissueSessionCookieAfterServerSideInvalidation()
    {
        _factory.Clock.Reset();

        var user = await _factory.CreateLoginUserAsync();
        var device = $"logout-no-reissue-{Guid.NewGuid():N}";

        using var client = CreateClient(device);

        var login = await LoginAsync(
            client,
            user.Identifier,
            user.Secret);

        login.StatusCode.Should().Be(HttpStatusCode.Found);

        var oldCookie = GetSessionCookie(login);

        client.DefaultRequestHeaders.Add(
            "Cookie",
            oldCookie);

        var logout = await client.PostAsync(
            "/auth/logout",
            null);

        logout.StatusCode.Should().Be(HttpStatusCode.Found);

        logout.Headers
            .TryGetValues("Set-Cookie", out var cookies)
            .Should()
            .BeTrue();

        var sessionCookieResponses = cookies!
            .Where(x =>
                x.StartsWith(
                    "uas=",
                    StringComparison.OrdinalIgnoreCase))
            .ToList();

        sessionCookieResponses.Should().NotBeEmpty();

        //
        // Every uas emitted by logout must represent deletion.
        // There must not be another Set-Cookie that creates a fresh uas.
        //
        sessionCookieResponses.Should().OnlyContain(
            x =>
                x.Contains(
                    "expires=",
                    StringComparison.OrdinalIgnoreCase) ||
                x.Contains(
                    "max-age=0",
                    StringComparison.OrdinalIgnoreCase));

        //
        // And the original credential remains unusable server-side.
        //
        using var replayClient = CreateClient(device);

        replayClient.DefaultRequestHeaders.Add(
            "Cookie",
            oldCookie);

        var replay = await GetChainsAsync(replayClient);

        replay.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_CookieDeletion_ShouldNotContainOriginalSessionCredential()
    {
        _factory.Clock.Reset();

        var user = await _factory.CreateLoginUserAsync();
        var device = $"logout-cookie-value-{Guid.NewGuid():N}";

        using var client = CreateClient(device);

        var login = await LoginAsync(
            client,
            user.Identifier,
            user.Secret);

        login.StatusCode.Should().Be(HttpStatusCode.Found);

        var sessionCookie = GetSessionCookie(login);

        var originalValue = GetCookieValue(
            sessionCookie,
            "uas");

        originalValue.Should().NotBeNullOrWhiteSpace();

        client.DefaultRequestHeaders.Add(
            "Cookie",
            sessionCookie);

        var logout = await client.PostAsync(
            "/auth/logout",
            null);

        logout.StatusCode.Should().Be(HttpStatusCode.Found);

        logout.Headers
            .TryGetValues("Set-Cookie", out var cookies)
            .Should()
            .BeTrue();

        var deletionCookie = cookies!
            .Single(x =>
                x.StartsWith(
                    "uas=",
                    StringComparison.OrdinalIgnoreCase));

        //
        // Logout response must never echo the old credential.
        //
        deletionCookie.Should().NotContain(originalValue!);
    }

    [Fact]
    public async Task ConcurrentLogout_WithSameSession_ShouldLeaveSessionInvalid()
    {
        _factory.Clock.Reset();

        var user = await _factory.CreateLoginUserAsync();
        var device = $"logout-concurrent-same-{Guid.NewGuid():N}";

        using var loginClient = CreateClient(device);

        var login = await LoginAsync(
            loginClient,
            user.Identifier,
            user.Secret);

        login.StatusCode.Should().Be(HttpStatusCode.Found);

        var cookie = GetSessionCookie(login);

        //
        // Two independent requests carrying exactly the same authority.
        //
        using var client1 = CreateClient(device);
        using var client2 = CreateClient(device);

        client1.DefaultRequestHeaders.Add("Cookie", cookie);
        client2.DefaultRequestHeaders.Add("Cookie", cookie);

        var responses = await Task.WhenAll(
            client1.PostAsync("/auth/logout", null),
            client2.PostAsync("/auth/logout", null));

        //
        // Depending on where stale-session rejection occurs,
        // one request may observe the session after the other revoked it.
        //
        // What must never happen is an infrastructure failure.
        //
        responses.Should().OnlyContain(x =>
            x.StatusCode == HttpStatusCode.Found ||
            x.StatusCode == HttpStatusCode.Unauthorized);

        //
        // Final state is the actual security invariant.
        //
        using var replayClient = CreateClient(device);

        replayClient.DefaultRequestHeaders.Add(
            "Cookie",
            cookie);

        var replay = await GetChainsAsync(replayClient);

        replay.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ConcurrentLogout_FromDifferentDevices_ShouldInvalidateBothSessions()
    {
        _factory.Clock.Reset();

        var user = await _factory.CreateLoginUserAsync();

        var device1 =
            $"logout-concurrent-device-1-{Guid.NewGuid():N}";

        var device2 =
            $"logout-concurrent-device-2-{Guid.NewGuid():N}";

        using var loginClient1 = CreateClient(device1);
        using var loginClient2 = CreateClient(device2);

        var logins = await Task.WhenAll(
            LoginAsync(
                loginClient1,
                user.Identifier,
                user.Secret),
            LoginAsync(
                loginClient2,
                user.Identifier,
                user.Secret));

        logins.Should().OnlyContain(
            x => x.StatusCode == HttpStatusCode.Found);

        var cookie1 = GetSessionCookie(logins[0]);
        var cookie2 = GetSessionCookie(logins[1]);

        using var client1 = CreateClient(device1);
        using var client2 = CreateClient(device2);

        client1.DefaultRequestHeaders.Add(
            "Cookie",
            cookie1);

        client2.DefaultRequestHeaders.Add(
            "Cookie",
            cookie2);

        var logouts = await Task.WhenAll(
            client1.PostAsync("/auth/logout", null),
            client2.PostAsync("/auth/logout", null));

        logouts.Should().OnlyContain(
            x => x.StatusCode == HttpStatusCode.Found);

        //
        // Neither credential may survive.
        //
        using var replay1 = CreateClient(device1);
        using var replay2 = CreateClient(device2);

        replay1.DefaultRequestHeaders.Add(
            "Cookie",
            cookie1);

        replay2.DefaultRequestHeaders.Add(
            "Cookie",
            cookie2);

        var verification = await Task.WhenAll(
            GetChainsAsync(replay1),
            GetChainsAsync(replay2));

        verification.Should().OnlyContain(
            x => x.StatusCode == HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ConcurrentRequests_WithSessionBeingLoggedOut_ShouldNeverLeaveCredentialUsableAfterLogout()
    {
        _factory.Clock.Reset();

        var user = await _factory.CreateLoginUserAsync();
        var device =
            $"logout-concurrent-request-{Guid.NewGuid():N}";

        using var loginClient = CreateClient(device);

        var login = await LoginAsync(
            loginClient,
            user.Identifier,
            user.Secret);

        login.StatusCode.Should().Be(HttpStatusCode.Found);

        var cookie = GetSessionCookie(login);

        using var logoutClient = CreateClient(device);
        using var requestClient = CreateClient(device);

        logoutClient.DefaultRequestHeaders.Add(
            "Cookie",
            cookie);

        requestClient.DefaultRequestHeaders.Add(
            "Cookie",
            cookie);

        //
        // Intentionally race an authenticated operation against logout.
        //
        var logoutTask =
            logoutClient.PostAsync("/auth/logout", null);

        var authenticatedRequestTask =
            GetChainsAsync(requestClient);

        var logout = await logoutTask;
        var concurrentRequest = await authenticatedRequestTask;

        logout.StatusCode.Should().BeOneOf(
            HttpStatusCode.Found,
            HttpStatusCode.Unauthorized);

        //
        // The concurrent request itself is allowed to observe either state.
        //
        // If it authenticated before logout linearized -> 200.
        // If it authenticated afterwards              -> 401.
        //
        concurrentRequest.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.Unauthorized);

        //
        // But AFTER logout has completed, there is no ambiguity.
        //
        using var verificationClient = CreateClient(device);

        verificationClient.DefaultRequestHeaders.Add(
            "Cookie",
            cookie);

        var verification =
            await GetChainsAsync(verificationClient);

        verification.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ConcurrentLogoutAndRelogin_OnSameDevice_ShouldLeaveChainInConsistentState()
    {
        _factory.Clock.Reset();

        var user = await _factory.CreateLoginUserAsync();

        var device =
            $"logout-relogin-race-{Guid.NewGuid():N}";

        //
        // Establish Session A.
        //
        using var initialClient = CreateClient(device);

        var initialLogin = await LoginAsync(
            initialClient,
            user.Identifier,
            user.Secret);

        initialLogin.StatusCode.Should()
            .Be(HttpStatusCode.Found);

        var oldCookie = GetSessionCookie(initialLogin);

        //
        // One request logs Session A out while another request performs
        // a fresh login from the same device.
        //
        using var logoutClient = CreateClient(device);
        using var loginClient = CreateClient(device);

        logoutClient.DefaultRequestHeaders.Add(
            "Cookie",
            oldCookie);

        var logoutTask =
            logoutClient.PostAsync("/auth/logout", null);

        var loginTask =
            LoginAsync(
                loginClient,
                user.Identifier,
                user.Secret);

        await Task.WhenAll(
            logoutTask,
            loginTask);

        var logout = await logoutTask;
        var relogin = await loginTask;

        //
        // Neither operation may leak an infrastructure/concurrency failure.
        //
        logout.StatusCode.Should().BeOneOf(
            HttpStatusCode.Found,
            HttpStatusCode.Unauthorized);

        relogin.StatusCode.Should()
            .Be(HttpStatusCode.Found);

        var newCookie = GetSessionCookie(relogin);

        newCookie.Should().NotBeNullOrWhiteSpace();
        newCookie.Should().NotBe(oldCookie);

        //
        // Session A must NEVER become usable again regardless of
        // operation ordering.
        //
        using var oldSessionClient = CreateClient(device);

        oldSessionClient.DefaultRequestHeaders.Add(
            "Cookie",
            oldCookie);

        var oldSessionVerification =
            await GetChainsAsync(oldSessionClient);

        oldSessionVerification.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);

        //
        // Session B has two legitimate outcomes:
        //
        // 200:
        // logout linearized before the new login.
        //
        // 401:
        // login attached Session B first and the concurrent
        // chain logout subsequently invalidated it.
        //
        using var newSessionClient = CreateClient(device);

        newSessionClient.DefaultRequestHeaders.Add(
            "Cookie",
            newCookie);

        var newSessionVerification =
            await GetChainsAsync(newSessionClient);

        newSessionVerification.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.Unauthorized);

        //
        // Most important recovery invariant:
        // regardless of which operation won the race, another clean login
        // must restore a usable authenticated session.
        //
        using var recoveryClient = CreateClient(device);

        var recoveryLogin = await LoginAsync(
            recoveryClient,
            user.Identifier,
            user.Secret);

        recoveryLogin.StatusCode.Should()
            .Be(HttpStatusCode.Found);

        var recoveryCookie =
            GetSessionCookie(recoveryLogin);

        using var authenticatedRecoveryClient =
            CreateClient(device);

        authenticatedRecoveryClient.DefaultRequestHeaders.Add(
            "Cookie",
            recoveryCookie);

        var recoveryVerification =
            await GetChainsAsync(authenticatedRecoveryClient);

        recoveryVerification.StatusCode.Should()
            .Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task LogoutDeviceSelf_ShouldRevokeTargetDeviceSession()
    {
        _factory.Clock.Reset();

        var user = await _factory.CreateLoginUserAsync();

        var device1 = $"logout-device-self-1-{Guid.NewGuid():N}";
        var device2 = $"logout-device-self-2-{Guid.NewGuid():N}";

        using var client1 = CreateClient(device1);
        using var client2 = CreateClient(device2);

        var cookie1 = GetSessionCookie(
            await LoginAsync(client1, user.Identifier, user.Secret));

        var cookie2 = GetSessionCookie(
            await LoginAsync(client2, user.Identifier, user.Secret));

        client1.DefaultRequestHeaders.Add("Cookie", cookie1);
        client2.DefaultRequestHeaders.Add("Cookie", cookie2);

        var chainsResponse = await GetChainsAsync(client1);

        chainsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var page = await chainsResponse.Content
            .ReadFromJsonAsync<PagedResult<SessionChainSummary>>();

        page.Should().NotBeNull();

        var targetChain = page!.Items.Single(x =>
            !x.IsCurrentDevice &&
            x.ActiveSessionId is not null);

        var logoutResponse = await client1.PostAsJsonAsync(
            "/auth/me/logout-device",
            new LogoutDeviceRequest
            {
                ChainId = targetChain.ChainId
            });

        logoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var verificationClient = CreateClient(device2);
        verificationClient.DefaultRequestHeaders.Add("Cookie", cookie2);

        var verification = await GetChainsAsync(verificationClient);

        verification.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LogoutDeviceSelf_ShouldNotInvalidateCurrentDevice_WhenAnotherDeviceIsTargeted()
    {
        _factory.Clock.Reset();

        var user = await _factory.CreateLoginUserAsync();

        var device1 = $"logout-device-keep-{Guid.NewGuid():N}";
        var device2 = $"logout-device-target-{Guid.NewGuid():N}";

        using var client1 = CreateClient(device1);
        using var client2 = CreateClient(device2);

        var cookie1 = GetSessionCookie(
            await LoginAsync(client1, user.Identifier, user.Secret));

        var cookie2 = GetSessionCookie(
            await LoginAsync(client2, user.Identifier, user.Secret));

        client1.DefaultRequestHeaders.Add("Cookie", cookie1);
        client2.DefaultRequestHeaders.Add("Cookie", cookie2);

        var chainsResponse = await GetChainsAsync(client1);

        var page = await chainsResponse.Content
            .ReadFromJsonAsync<PagedResult<SessionChainSummary>>();

        page.Should().NotBeNull();

        var targetChain = page!.Items.Single(x =>
            !x.IsCurrentDevice &&
            x.ActiveSessionId is not null);

        var logout = await client1.PostAsJsonAsync(
            "/auth/me/logout-device",
            new LogoutDeviceRequest
            {
                ChainId = targetChain.ChainId
            });

        logout.StatusCode.Should().Be(HttpStatusCode.OK);

        //
        // Actor's own session must remain usable.
        //
        var actorVerification =
            await GetChainsAsync(client1);

        actorVerification.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        //
        // Target session must be dead.
        //
        using var targetVerificationClient =
            CreateClient(device2);

        targetVerificationClient.DefaultRequestHeaders.Add(
            "Cookie",
            cookie2);

        var targetVerification =
            await GetChainsAsync(targetVerificationClient);

        targetVerification.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LogoutDeviceSelf_ShouldDetachActiveSessionWithoutRevokingChain()
    {
        _factory.Clock.Reset();

        var user = await _factory.CreateLoginUserAsync();

        var device1 = $"logout-device-observer-{Guid.NewGuid():N}";
        var device2 = $"logout-device-revoked-{Guid.NewGuid():N}";

        using var client1 = CreateClient(device1);
        using var client2 = CreateClient(device2);

        var cookie1 = GetSessionCookie(await LoginAsync(client1, user.Identifier, user.Secret));

        var cookie2 = GetSessionCookie(await LoginAsync(client2, user.Identifier, user.Secret));

        client1.DefaultRequestHeaders.Add("Cookie", cookie1);
        client2.DefaultRequestHeaders.Add("Cookie", cookie2);

        var beforeResponse = await GetChainsAsync(client1);

        var before = await beforeResponse.Content.ReadFromJsonAsync<PagedResult<SessionChainSummary>>();

        before.Should().NotBeNull();

        var target = before!.Items.Single(x =>
            !x.IsCurrentDevice &&
            x.ActiveSessionId is not null);

        var logout = await client1.PostAsJsonAsync(
            "/auth/me/logout-device",
            new LogoutDeviceRequest
            {
                ChainId = target.ChainId
            });

        logout.StatusCode.Should().Be(HttpStatusCode.OK);

        var afterResponse = await GetChainsAsync(client1);

        afterResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var after = await afterResponse.Content.ReadFromJsonAsync<PagedResult<SessionChainSummary>>();

        after.Should().NotBeNull();

        var loggedOut = after!.Items.Single(x => x.ChainId == target.ChainId);

        loggedOut.IsRevoked.Should().BeFalse();
        loggedOut.RevokedAt.Should().BeNull();
        loggedOut.ActiveSessionId.Should().BeNull();
    }

    [Fact]
    public async Task LogoutDeviceSelf_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        _factory.Clock.Reset();

        using var client = CreateClient(
            $"logout-device-anonymous-{Guid.NewGuid():N}");

        var response = await client.PostAsJsonAsync(
            "/auth/me/logout-device",
            new LogoutDeviceRequest
            {
                ChainId = SessionChainId.New()
            });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LogoutDeviceSelf_ShouldNotAllowUserToLogoutAnotherUsersDevice()
    {
        _factory.Clock.Reset();

        var attacker = await _factory.CreateLoginUserAsync();
        var victim = await _factory.CreateLoginUserAsync();

        var attackerDevice =
            $"logout-device-attacker-{Guid.NewGuid():N}";

        var victimDevice =
            $"logout-device-victim-{Guid.NewGuid():N}";

        using var attackerClient = CreateClient(attackerDevice);
        using var victimClient = CreateClient(victimDevice);

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

        //
        // Obtain victim's own chain id.
        //
        var victimChainsResponse =
            await GetChainsAsync(victimClient);

        victimChainsResponse.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        var victimChains = await victimChainsResponse.Content
            .ReadFromJsonAsync<PagedResult<SessionChainSummary>>();

        victimChains.Should().NotBeNull();

        var victimChain = victimChains!.Items
            .Single(x => x.IsCurrentDevice);

        //
        // Attacker tries to target victim's ChainId.
        //
        var attack = await attackerClient.PostAsJsonAsync(
            "/auth/me/logout-device",
            new LogoutDeviceRequest
            {
                ChainId = victimChain.ChainId
            });

        attack.StatusCode.Should().BeOneOf(
            HttpStatusCode.Forbidden,
            HttpStatusCode.NotFound,
            HttpStatusCode.Unauthorized);

        var victimVerification = await GetChainsAsync(victimClient);

        victimVerification.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task LogoutDeviceSelf_ShouldAllowCurrentDeviceToLogoutItself()
    {
        _factory.Clock.Reset();

        var user = await _factory.CreateLoginUserAsync();
        var device = $"logout-device-current-{Guid.NewGuid():N}";

        using var client = CreateClient(device);

        var login = await LoginAsync(
            client,
            user.Identifier,
            user.Secret);

        login.StatusCode.Should().Be(HttpStatusCode.Found);

        var cookie = GetSessionCookie(login);

        client.DefaultRequestHeaders.Add(
            "Cookie",
            cookie);

        var chainsResponse = await GetChainsAsync(client);

        chainsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var chains = await chainsResponse.Content
            .ReadFromJsonAsync<PagedResult<SessionChainSummary>>();

        chains.Should().NotBeNull();

        var currentChain = chains!.Items
            .Single(x => x.IsCurrentDevice);

        currentChain.ActiveSessionId.Should().NotBeNull();

        var logout = await client.PostAsJsonAsync(
            "/auth/me/logout-device",
            new LogoutDeviceRequest
            {
                ChainId = currentChain.ChainId
            });

        logout.StatusCode.Should().Be(HttpStatusCode.OK);

        //
        // The authority that performed the operation must now be dead.
        //
        var verification = await GetChainsAsync(client);

        verification.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LogoutDeviceSelf_WithUnknownChain_ShouldNotAffectCurrentSession()
    {
        _factory.Clock.Reset();

        var user = await _factory.CreateLoginUserAsync();
        var device = $"logout-device-unknown-{Guid.NewGuid():N}";

        using var client = CreateClient(device);

        var login = await LoginAsync(
            client,
            user.Identifier,
            user.Secret);

        var cookie = GetSessionCookie(login);

        client.DefaultRequestHeaders.Add(
            "Cookie",
            cookie);

        var response = await client.PostAsJsonAsync(
            "/auth/me/logout-device",
            new LogoutDeviceRequest
            {
                ChainId = SessionChainId.New()
            });

        //
        // Exact mapping depends on the UltimateAuth exception pipeline.
        // Unknown resources must never produce success through mutation.
        //
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.NotFound,
            HttpStatusCode.Forbidden);

        //
        // Most important invariant:
        // malformed/unknown target must not damage actor's session.
        //
        var verification = await GetChainsAsync(client);

        verification.StatusCode.Should()
            .Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task LogoutDeviceSelf_AlreadyLoggedOutDevice_ShouldRemainSafe()
    {
        _factory.Clock.Reset();

        var user = await _factory.CreateLoginUserAsync();

        var actorDevice =
            $"logout-device-repeat-actor-{Guid.NewGuid():N}";

        var targetDevice =
            $"logout-device-repeat-target-{Guid.NewGuid():N}";

        using var actorClient = CreateClient(actorDevice);
        using var targetClient = CreateClient(targetDevice);

        var actorCookie = GetSessionCookie(
            await LoginAsync(
                actorClient,
                user.Identifier,
                user.Secret));

        var targetCookie = GetSessionCookie(
            await LoginAsync(
                targetClient,
                user.Identifier,
                user.Secret));

        actorClient.DefaultRequestHeaders.Add(
            "Cookie",
            actorCookie);

        targetClient.DefaultRequestHeaders.Add(
            "Cookie",
            targetCookie);

        var chainsResponse = await GetChainsAsync(actorClient);

        var chains = await chainsResponse.Content
            .ReadFromJsonAsync<PagedResult<SessionChainSummary>>();

        chains.Should().NotBeNull();

        var targetChain = chains!.Items.Single(x =>
            !x.IsCurrentDevice &&
            x.ActiveSessionId is not null);

        var first = await actorClient.PostAsJsonAsync(
            "/auth/me/logout-device",
            new LogoutDeviceRequest
            {
                ChainId = targetChain.ChainId
            });

        first.StatusCode.Should().Be(HttpStatusCode.OK);

        //
        // Same operation against an already detached chain.
        //
        var second = await actorClient.PostAsJsonAsync(
            "/auth/me/logout-device",
            new LogoutDeviceRequest
            {
                ChainId = targetChain.ChainId
            });

        second.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.NotFound);

        //
        // Actor must survive the repeated operation.
        //
        var actorVerification =
            await GetChainsAsync(actorClient);

        actorVerification.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        //
        // Target credential must remain dead.
        //
        using var replayClient = CreateClient(targetDevice);

        replayClient.DefaultRequestHeaders.Add(
            "Cookie",
            targetCookie);

        var targetVerification =
            await GetChainsAsync(replayClient);

        targetVerification.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LogoutDeviceSelf_LoggedOutDevice_ShouldBeAbleToLoginAgain()
    {
        _factory.Clock.Reset();

        var user = await _factory.CreateLoginUserAsync();

        var actorDevice =
            $"logout-device-relogin-actor-{Guid.NewGuid():N}";

        var targetDevice =
            $"logout-device-relogin-target-{Guid.NewGuid():N}";

        using var actorClient = CreateClient(actorDevice);
        using var targetClient = CreateClient(targetDevice);

        var actorCookie = GetSessionCookie(
            await LoginAsync(
                actorClient,
                user.Identifier,
                user.Secret));

        var oldTargetCookie = GetSessionCookie(
            await LoginAsync(
                targetClient,
                user.Identifier,
                user.Secret));

        actorClient.DefaultRequestHeaders.Add(
            "Cookie",
            actorCookie);

        targetClient.DefaultRequestHeaders.Add(
            "Cookie",
            oldTargetCookie);

        var chainsResponse =
            await GetChainsAsync(actorClient);

        var chains = await chainsResponse.Content
            .ReadFromJsonAsync<PagedResult<SessionChainSummary>>();

        chains.Should().NotBeNull();

        var targetChain = chains!.Items.Single(x =>
            !x.IsCurrentDevice &&
            x.ActiveSessionId is not null);

        var originalChainId = targetChain.ChainId;
        var originalSessionId = targetChain.ActiveSessionId;

        var logout = await actorClient.PostAsJsonAsync(
            "/auth/me/logout-device",
            new LogoutDeviceRequest
            {
                ChainId = originalChainId
            });

        logout.StatusCode.Should().Be(HttpStatusCode.OK);

        //
        // Old credential must be dead.
        //
        using var oldCredentialClient =
            CreateClient(targetDevice);

        oldCredentialClient.DefaultRequestHeaders.Add(
            "Cookie",
            oldTargetCookie);

        var oldVerification =
            await GetChainsAsync(oldCredentialClient);

        oldVerification.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);

        //
        // But logout-device is not device revocation.
        // Correct credentials may establish a new session.
        //
        using var reloginClient =
            CreateClient(targetDevice);

        var relogin = await LoginAsync(
            reloginClient,
            user.Identifier,
            user.Secret);

        relogin.StatusCode.Should()
            .Be(HttpStatusCode.Found);

        var newCookie = GetSessionCookie(relogin);

        newCookie.Should().NotBe(oldTargetCookie);

        reloginClient.DefaultRequestHeaders.Add(
            "Cookie",
            newCookie);

        var afterResponse =
            await GetChainsAsync(reloginClient);

        afterResponse.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        var after = await afterResponse.Content
            .ReadFromJsonAsync<PagedResult<SessionChainSummary>>();

        after.Should().NotBeNull();

        var current =
            after!.Items.Single(x => x.IsCurrentDevice);

        //
        // Same physical/device chain should be reused.
        //
        current.ChainId.Should().Be(originalChainId);

        current.IsRevoked.Should().BeFalse();
        current.RevokedAt.Should().BeNull();

        current.ActiveSessionId.Should().NotBeNull();
        current.ActiveSessionId.Should().NotBe(originalSessionId);
    }

    [Fact]
    public async Task LogoutOthersSelf_ShouldInvalidateAllOtherDeviceSessions()
    {
        _factory.Clock.Reset();

        var user = await _factory.CreateLoginUserAsync();

        var device1 = $"logout-others-current-{Guid.NewGuid():N}";
        var device2 = $"logout-others-other-1-{Guid.NewGuid():N}";
        var device3 = $"logout-others-other-2-{Guid.NewGuid():N}";

        using var client1 = CreateClient(device1);
        using var client2 = CreateClient(device2);
        using var client3 = CreateClient(device3);

        var cookie1 = GetSessionCookie(
            await LoginAsync(client1, user.Identifier, user.Secret));

        var cookie2 = GetSessionCookie(
            await LoginAsync(client2, user.Identifier, user.Secret));

        var cookie3 = GetSessionCookie(
            await LoginAsync(client3, user.Identifier, user.Secret));

        client1.DefaultRequestHeaders.Add("Cookie", cookie1);
        client2.DefaultRequestHeaders.Add("Cookie", cookie2);
        client3.DefaultRequestHeaders.Add("Cookie", cookie3);

        //
        // All three sessions must initially be usable.
        //
        (await GetChainsAsync(client1))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        (await GetChainsAsync(client2))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        (await GetChainsAsync(client3))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        //
        // Device 1 logs out every OTHER device.
        //
        var logout = await client1.PostAsync(
            "/auth/me/logout-others",
            null);

        logout.StatusCode.Should().Be(HttpStatusCode.OK);

        //
        // Actor survives.
        //
        (await GetChainsAsync(client1))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        //
        // Every other authority must be dead.
        //
        (await GetChainsAsync(client2))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        (await GetChainsAsync(client3))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LogoutOthersSelf_ShouldKeepCurrentSessionValid()
    {
        _factory.Clock.Reset();

        var user = await _factory.CreateLoginUserAsync();

        var currentDevice =
            $"logout-others-keep-current-{Guid.NewGuid():N}";

        var otherDevice =
            $"logout-others-target-{Guid.NewGuid():N}";

        using var currentClient = CreateClient(currentDevice);
        using var otherClient = CreateClient(otherDevice);

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

        currentClient.DefaultRequestHeaders.Add(
            "Cookie",
            currentCookie);

        otherClient.DefaultRequestHeaders.Add(
            "Cookie",
            otherCookie);

        //
        // Capture actor authority before mutation.
        //
        var beforeResponse =
            await GetChainsAsync(currentClient);

        beforeResponse.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        var before = await beforeResponse.Content
            .ReadFromJsonAsync<PagedResult<SessionChainSummary>>();

        before.Should().NotBeNull();

        var currentBefore =
            before!.Items.Single(x => x.IsCurrentDevice);

        var currentChainId = currentBefore.ChainId;
        var currentSessionId = currentBefore.ActiveSessionId;

        currentSessionId.Should().NotBeNull();

        var logout = await currentClient.PostAsync(
            "/auth/me/logout-others",
            null);

        logout.StatusCode.Should().Be(HttpStatusCode.OK);

        //
        // Actor credential remains usable.
        //
        var afterResponse =
            await GetChainsAsync(currentClient);

        afterResponse.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        var after = await afterResponse.Content
            .ReadFromJsonAsync<PagedResult<SessionChainSummary>>();

        after.Should().NotBeNull();

        var currentAfter =
            after!.Items.Single(x => x.IsCurrentDevice);

        //
        // logout-others must not rotate/detach actor authority.
        //
        currentAfter.ChainId.Should().Be(currentChainId);
        currentAfter.ActiveSessionId.Should().Be(currentSessionId);
        currentAfter.IsRevoked.Should().BeFalse();

        //
        // Other device must be invalid.
        //
        var otherVerification =
            await GetChainsAsync(otherClient);

        otherVerification.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LogoutOthersSelf_ShouldDetachOtherSessionsWithoutRevokingChains()
    {
        _factory.Clock.Reset();

        var user = await _factory.CreateLoginUserAsync();

        var actorDevice =
            $"logout-others-observer-{Guid.NewGuid():N}";

        var targetDevice1 =
            $"logout-others-target-1-{Guid.NewGuid():N}";

        var targetDevice2 =
            $"logout-others-target-2-{Guid.NewGuid():N}";

        using var actorClient = CreateClient(actorDevice);
        using var targetClient1 = CreateClient(targetDevice1);
        using var targetClient2 = CreateClient(targetDevice2);

        var actorCookie = GetSessionCookie(
            await LoginAsync(
                actorClient,
                user.Identifier,
                user.Secret));

        var targetCookie1 = GetSessionCookie(
            await LoginAsync(
                targetClient1,
                user.Identifier,
                user.Secret));

        var targetCookie2 = GetSessionCookie(
            await LoginAsync(
                targetClient2,
                user.Identifier,
                user.Secret));

        actorClient.DefaultRequestHeaders.Add(
            "Cookie",
            actorCookie);

        targetClient1.DefaultRequestHeaders.Add(
            "Cookie",
            targetCookie1);

        targetClient2.DefaultRequestHeaders.Add(
            "Cookie",
            targetCookie2);

        var beforeResponse =
            await GetChainsAsync(actorClient);

        beforeResponse.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        var before = await beforeResponse.Content
            .ReadFromJsonAsync<PagedResult<SessionChainSummary>>();

        before.Should().NotBeNull();

        var targetChains = before!.Items
            .Where(x =>
                !x.IsCurrentDevice &&
                x.ActiveSessionId is not null)
            .ToList();

        targetChains.Should().HaveCount(2);

        var targetChainIds = targetChains
            .Select(x => x.ChainId)
            .ToHashSet();

        var logout = await actorClient.PostAsync(
            "/auth/me/logout-others",
            null);

        logout.StatusCode.Should().Be(HttpStatusCode.OK);

        //
        // Actor can inspect post-operation chain state.
        //
        var afterResponse =
            await GetChainsAsync(actorClient);

        afterResponse.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        var after = await afterResponse.Content
            .ReadFromJsonAsync<PagedResult<SessionChainSummary>>();

        after.Should().NotBeNull();

        var loggedOutChains = after!.Items
            .Where(x => targetChainIds.Contains(x.ChainId))
            .ToList();

        loggedOutChains.Should().HaveCount(2);

        loggedOutChains.Should().OnlyContain(x =>
            x.ActiveSessionId == null);

        //
        // logout-others is session logout, not device revocation.
        //
        loggedOutChains.Should().OnlyContain(x =>
            !x.IsRevoked &&
            x.RevokedAt == null);

        //
        // Old credentials are dead.
        //
        (await GetChainsAsync(targetClient1))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        (await GetChainsAsync(targetClient2))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LogoutOthersSelf_ShouldNotAffectAnotherUsersSessions()
    {
        _factory.Clock.Reset();

        var actor = await _factory.CreateLoginUserAsync();
        var victim = await _factory.CreateLoginUserAsync();

        //
        // Actor owns two devices so logout-others actually has work to do.
        //
        using var actorClient1 = CreateClient(
            $"logout-others-actor-1-{Guid.NewGuid():N}");

        using var actorClient2 = CreateClient(
            $"logout-others-actor-2-{Guid.NewGuid():N}");

        using var victimClient = CreateClient(
            $"logout-others-victim-{Guid.NewGuid():N}");

        var actorCookie1 = GetSessionCookie(
            await LoginAsync(
                actorClient1,
                actor.Identifier,
                actor.Secret));

        var actorCookie2 = GetSessionCookie(
            await LoginAsync(
                actorClient2,
                actor.Identifier,
                actor.Secret));

        var victimCookie = GetSessionCookie(
            await LoginAsync(
                victimClient,
                victim.Identifier,
                victim.Secret));

        actorClient1.DefaultRequestHeaders.Add(
            "Cookie",
            actorCookie1);

        actorClient2.DefaultRequestHeaders.Add(
            "Cookie",
            actorCookie2);

        victimClient.DefaultRequestHeaders.Add(
            "Cookie",
            victimCookie);

        //
        // Sanity check before destructive operation.
        //
        (await GetChainsAsync(victimClient))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var logout = await actorClient1.PostAsync(
            "/auth/me/logout-others",
            null);

        logout.StatusCode.Should().Be(HttpStatusCode.OK);

        //
        // Actor's second device is expected to die.
        //
        (await GetChainsAsync(actorClient2))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        //
        // Critical ownership invariant:
        // another user's authority must be untouched.
        //
        var victimVerification =
            await GetChainsAsync(victimClient);

        victimVerification.StatusCode.Should()
            .Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task LogoutOthersSelf_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        _factory.Clock.Reset();

        using var client = CreateClient(
            $"logout-others-anonymous-{Guid.NewGuid():N}");

        var response = await client.PostAsync(
            "/auth/me/logout-others",
            null);

        response.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LogoutOthersSelf_WithNoOtherSessions_ShouldSucceed()
    {
        _factory.Clock.Reset();

        var user = await _factory.CreateLoginUserAsync();

        var device =
            $"logout-others-single-{Guid.NewGuid():N}";

        using var client = CreateClient(device);

        var cookie = GetSessionCookie(
            await LoginAsync(
                client,
                user.Identifier,
                user.Secret));

        client.DefaultRequestHeaders.Add(
            "Cookie",
            cookie);

        var beforeResponse =
            await GetChainsAsync(client);

        beforeResponse.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        var before = await beforeResponse.Content
            .ReadFromJsonAsync<PagedResult<SessionChainSummary>>();

        before.Should().NotBeNull();

        var currentBefore =
            before!.Items.Single(x => x.IsCurrentDevice);

        var sessionId =
            currentBefore.ActiveSessionId;

        sessionId.Should().NotBeNull();

        //
        // There is nothing to terminate.
        //
        var logout = await client.PostAsync(
            "/auth/me/logout-others",
            null);

        logout.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        //
        // No-op must not damage current authority.
        //
        var afterResponse =
            await GetChainsAsync(client);

        afterResponse.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        var after = await afterResponse.Content
            .ReadFromJsonAsync<PagedResult<SessionChainSummary>>();

        after.Should().NotBeNull();

        var currentAfter =
            after!.Items.Single(x => x.IsCurrentDevice);

        currentAfter.ActiveSessionId.Should()
            .Be(sessionId);

        currentAfter.IsRevoked.Should()
            .BeFalse();
    }

    [Fact]
    public async Task LogoutOthersSelf_RepeatedCall_ShouldRemainSafe()
    {
        _factory.Clock.Reset();

        var user = await _factory.CreateLoginUserAsync();

        using var actorClient = CreateClient(
            $"logout-others-repeat-actor-{Guid.NewGuid():N}");

        using var targetClient = CreateClient(
            $"logout-others-repeat-target-{Guid.NewGuid():N}");

        var actorCookie = GetSessionCookie(
            await LoginAsync(
                actorClient,
                user.Identifier,
                user.Secret));

        var targetCookie = GetSessionCookie(
            await LoginAsync(
                targetClient,
                user.Identifier,
                user.Secret));

        actorClient.DefaultRequestHeaders.Add(
            "Cookie",
            actorCookie);

        targetClient.DefaultRequestHeaders.Add(
            "Cookie",
            targetCookie);

        var first = await actorClient.PostAsync(
            "/auth/me/logout-others",
            null);

        first.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        //
        // Nothing remains to logout except actor itself.
        //
        var second = await actorClient.PostAsync(
            "/auth/me/logout-others",
            null);

        second.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        //
        // Actor survives repeated calls.
        //
        (await GetChainsAsync(actorClient))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        //
        // Previously invalidated authority stays invalid.
        //
        (await GetChainsAsync(targetClient))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LogoutOthersSelf_OldCredentialsFromOtherDevices_ShouldRemainInvalid()
    {
        _factory.Clock.Reset();

        var user = await _factory.CreateLoginUserAsync();

        var actorDevice =
            $"logout-others-replay-actor-{Guid.NewGuid():N}";

        var targetDevice =
            $"logout-others-replay-target-{Guid.NewGuid():N}";

        using var actorClient = CreateClient(actorDevice);
        using var targetClient = CreateClient(targetDevice);

        var actorCookie = GetSessionCookie(
            await LoginAsync(
                actorClient,
                user.Identifier,
                user.Secret));

        var oldTargetCookie = GetSessionCookie(
            await LoginAsync(
                targetClient,
                user.Identifier,
                user.Secret));

        actorClient.DefaultRequestHeaders.Add(
            "Cookie",
            actorCookie);

        targetClient.DefaultRequestHeaders.Add(
            "Cookie",
            oldTargetCookie);

        var logout = await actorClient.PostAsync(
            "/auth/me/logout-others",
            null);

        logout.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        //
        // Don't verify only through the original HttpClient.
        // Replay the exact authority from an independent request source.
        //
        using var replayClient =
            CreateClient(targetDevice);

        replayClient.DefaultRequestHeaders.Add(
            "Cookie",
            oldTargetCookie);

        var replay =
            await GetChainsAsync(replayClient);

        replay.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);

        //
        // Actor remains valid.
        //
        (await GetChainsAsync(actorClient))
            .StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task LogoutOthersSelf_LoggedOutDevices_ShouldBeAbleToLoginAgain()
    {
        _factory.Clock.Reset();

        var user = await _factory.CreateLoginUserAsync();

        var actorDevice =
            $"logout-others-relogin-actor-{Guid.NewGuid():N}";

        var targetDevice =
            $"logout-others-relogin-target-{Guid.NewGuid():N}";

        using var actorClient = CreateClient(actorDevice);
        using var targetClient = CreateClient(targetDevice);

        var actorCookie = GetSessionCookie(
            await LoginAsync(
                actorClient,
                user.Identifier,
                user.Secret));

        var targetCookie = GetSessionCookie(
            await LoginAsync(
                targetClient,
                user.Identifier,
                user.Secret));

        actorClient.DefaultRequestHeaders.Add(
            "Cookie",
            actorCookie);

        targetClient.DefaultRequestHeaders.Add(
            "Cookie",
            targetCookie);

        var beforeResponse =
            await GetChainsAsync(actorClient);

        beforeResponse.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        var before = await beforeResponse.Content
            .ReadFromJsonAsync<PagedResult<SessionChainSummary>>();

        before.Should().NotBeNull();

        var targetBefore = before!.Items.Single(x =>
            !x.IsCurrentDevice &&
            x.ActiveSessionId is not null);

        var originalChainId =
            targetBefore.ChainId;

        var originalSessionId =
            targetBefore.ActiveSessionId;

        var logout = await actorClient.PostAsync(
            "/auth/me/logout-others",
            null);

        logout.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        //
        // Previous authority is dead.
        //
        using var oldCredentialClient =
            CreateClient(targetDevice);

        oldCredentialClient.DefaultRequestHeaders.Add(
            "Cookie",
            targetCookie);

        (await GetChainsAsync(oldCredentialClient))
            .StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);

        //
        // But the device itself was not revoked.
        //
        using var reloginClient =
            CreateClient(targetDevice);

        var relogin = await LoginAsync(
            reloginClient,
            user.Identifier,
            user.Secret);

        relogin.StatusCode.Should()
            .Be(HttpStatusCode.Found);

        var newCookie =
            GetSessionCookie(relogin);

        newCookie.Should().NotBe(targetCookie);

        reloginClient.DefaultRequestHeaders.Add(
            "Cookie",
            newCookie);

        var afterResponse =
            await GetChainsAsync(reloginClient);

        afterResponse.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        var after = await afterResponse.Content
            .ReadFromJsonAsync<PagedResult<SessionChainSummary>>();

        after.Should().NotBeNull();

        var currentAfter =
            after!.Items.Single(x => x.IsCurrentDevice);

        //
        // Chain survives logout-others.
        //
        currentAfter.ChainId.Should()
            .Be(originalChainId);

        currentAfter.IsRevoked.Should()
            .BeFalse();

        currentAfter.RevokedAt.Should()
            .BeNull();

        //
        // Session authority is new.
        //
        currentAfter.ActiveSessionId.Should()
            .NotBeNull();

        currentAfter.ActiveSessionId.Should()
            .NotBe(originalSessionId);
    }

    [Fact]
    public async Task ConcurrentLogoutOthersSelf_ShouldLeaveOnlyActorSessionUsable()
    {
        _factory.Clock.Reset();

        var user = await _factory.CreateLoginUserAsync();

        var actorDevice = $"logout-others-concurrent-actor-{Guid.NewGuid():N}";

        var targetDevice1 = $"logout-others-concurrent-target-1-{Guid.NewGuid():N}";

        var targetDevice2 = $"logout-others-concurrent-target-2-{Guid.NewGuid():N}";

        using var actorClient = CreateClient(actorDevice);
        using var targetClient1 = CreateClient(targetDevice1);
        using var targetClient2 = CreateClient(targetDevice2);

        var actorCookie = GetSessionCookie(
            await LoginAsync(
                actorClient,
                user.Identifier,
                user.Secret));

        var targetCookie1 = GetSessionCookie(
            await LoginAsync(
                targetClient1,
                user.Identifier,
                user.Secret));

        var targetCookie2 = GetSessionCookie(
            await LoginAsync(
                targetClient2,
                user.Identifier,
                user.Secret));

        //
        // Two independent requests carry the same actor authority.
        //
        using var request1 = CreateClient(actorDevice);
        using var request2 = CreateClient(actorDevice);

        request1.DefaultRequestHeaders.Add(
            "Cookie",
            actorCookie);

        request2.DefaultRequestHeaders.Add(
            "Cookie",
            actorCookie);

        var responses = await Task.WhenAll(
            request1.PostAsync(
                "/auth/me/logout-others",
                null),

            request2.PostAsync(
                "/auth/me/logout-others",
                null));

        //
        // Operation is conceptually idempotent.
        //
        responses.Should().OnlyContain(x =>
            x.StatusCode == HttpStatusCode.OK);

        //
        // Actor authority survives.
        //
        using var actorVerification =
            CreateClient(actorDevice);

        actorVerification.DefaultRequestHeaders.Add(
            "Cookie",
            actorCookie);

        (await GetChainsAsync(actorVerification))
            .StatusCode.Should()
            .Be(HttpStatusCode.OK);

        //
        // Every other authority must converge to invalid.
        //
        using var targetVerification1 =
            CreateClient(targetDevice1);

        using var targetVerification2 =
            CreateClient(targetDevice2);

        targetVerification1.DefaultRequestHeaders.Add(
            "Cookie",
            targetCookie1);

        targetVerification2.DefaultRequestHeaders.Add(
            "Cookie",
            targetCookie2);

        var verification = await Task.WhenAll(
            GetChainsAsync(targetVerification1),
            GetChainsAsync(targetVerification2));

        verification.Should().OnlyContain(x =>
            x.StatusCode == HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LogoutAllSelf_ShouldInvalidateAllDeviceSessions()
    {
        _factory.Clock.Reset();

        var user = await _factory.CreateLoginUserAsync();

        var device1 = $"logout-all-device-1-{Guid.NewGuid():N}";
        var device2 = $"logout-all-device-2-{Guid.NewGuid():N}";
        var device3 = $"logout-all-device-3-{Guid.NewGuid():N}";

        using var client1 = CreateClient(device1);
        using var client2 = CreateClient(device2);
        using var client3 = CreateClient(device3);

        var cookie1 = GetSessionCookie(
            await LoginAsync(client1, user.Identifier, user.Secret));

        var cookie2 = GetSessionCookie(
            await LoginAsync(client2, user.Identifier, user.Secret));

        var cookie3 = GetSessionCookie(
            await LoginAsync(client3, user.Identifier, user.Secret));

        client1.DefaultRequestHeaders.Add("Cookie", cookie1);
        client2.DefaultRequestHeaders.Add("Cookie", cookie2);
        client3.DefaultRequestHeaders.Add("Cookie", cookie3);

        //
        // Sanity check: every authority is initially valid.
        //
        (await GetChainsAsync(client1))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        (await GetChainsAsync(client2))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        (await GetChainsAsync(client3))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        //
        // Device 1 logs out ALL sessions belonging to the user,
        // including its own session.
        //
        var logout = await client1.PostAsync(
            "/auth/me/logout-all",
            null);

        logout.StatusCode.Should().Be(HttpStatusCode.OK);

        //
        // Every previously issued authority must now be dead.
        //
        (await GetChainsAsync(client1))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        (await GetChainsAsync(client2))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        (await GetChainsAsync(client3))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LogoutAllSelf_ShouldInvalidateCallingSession()
    {
        _factory.Clock.Reset();

        var user = await _factory.CreateLoginUserAsync();

        var device =
            $"logout-all-current-{Guid.NewGuid():N}";

        using var client = CreateClient(device);

        var cookie = GetSessionCookie(
            await LoginAsync(
                client,
                user.Identifier,
                user.Secret));

        client.DefaultRequestHeaders.Add(
            "Cookie",
            cookie);

        //
        // Actor is authenticated before logout-all.
        //
        var before = await GetChainsAsync(client);

        before.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        //
        // Unlike logout-others, logout-all includes the actor.
        //
        var logout = await client.PostAsync(
            "/auth/me/logout-all",
            null);

        logout.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        //
        // The same authority cannot be used again.
        //
        var after = await GetChainsAsync(client);

        after.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);

        //
        // Verify replay independently from the original HttpClient.
        //
        using var replayClient =
            CreateClient(device);

        replayClient.DefaultRequestHeaders.Add(
            "Cookie",
            cookie);

        var replay = await GetChainsAsync(replayClient);

        replay.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LogoutAllSelf_ShouldDetachSessionsWithoutRevokingChains()
    {
        _factory.Clock.Reset();

        var user = await _factory.CreateLoginUserAsync();

        var device1 =
            $"logout-all-chain-1-{Guid.NewGuid():N}";

        var device2 =
            $"logout-all-chain-2-{Guid.NewGuid():N}";

        using var client1 = CreateClient(device1);
        using var client2 = CreateClient(device2);

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

        client1.DefaultRequestHeaders.Add(
            "Cookie",
            cookie1);

        client2.DefaultRequestHeaders.Add(
            "Cookie",
            cookie2);

        //
        // Capture both chains before logout-all because afterwards
        // neither old session may be used for inspection.
        //
        var beforeResponse =
            await GetChainsAsync(client1);

        beforeResponse.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        var before = await beforeResponse.Content
            .ReadFromJsonAsync<PagedResult<SessionChainSummary>>();

        before.Should().NotBeNull();

        var activeBefore = before!.Items
            .Where(x => x.ActiveSessionId is not null)
            .ToList();

        activeBefore.Should().HaveCount(2);

        var originalChains = activeBefore
            .ToDictionary(
                x => x.ChainId,
                x => x.ActiveSessionId);

        var logout = await client1.PostAsync(
            "/auth/me/logout-all",
            null);

        logout.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        //
        // Both old credentials are now dead.
        //
        (await GetChainsAsync(client1))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        (await GetChainsAsync(client2))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        //
        // Re-login from device1 so that we regain an authenticated
        // observer capable of inspecting the user's chains.
        //
        using var observer = CreateClient(device1);

        var relogin = await LoginAsync(
            observer,
            user.Identifier,
            user.Secret);

        relogin.StatusCode.Should()
            .Be(HttpStatusCode.Found);

        var observerCookie =
            GetSessionCookie(relogin);

        observer.DefaultRequestHeaders.Add(
            "Cookie",
            observerCookie);

        var afterResponse =
            await GetChainsAsync(observer);

        afterResponse.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        var after = await afterResponse.Content
            .ReadFromJsonAsync<PagedResult<SessionChainSummary>>();

        after.Should().NotBeNull();

        foreach (var original in originalChains)
        {
            var chain = after!.Items.Single(x =>
                x.ChainId == original.Key);

            //
            // logout-all is NOT chain/device revocation.
            //
            chain.IsRevoked.Should().BeFalse();
            chain.RevokedAt.Should().BeNull();

            if (chain.IsCurrentDevice)
            {
                //
                // device1 has logged in again, therefore its chain
                // now owns a NEW session.
                //
                chain.ActiveSessionId.Should().NotBeNull();
                chain.ActiveSessionId.Should().NotBe(original.Value);
            }
            else
            {
                //
                // device2 has not logged in again.
                //
                chain.ActiveSessionId.Should().BeNull();
            }
        }
    }

    [Fact]
    public async Task LogoutAllSelf_ShouldNotAffectAnotherUsersSessions()
    {
        _factory.Clock.Reset();

        var actor = await _factory.CreateLoginUserAsync();
        var victim = await _factory.CreateLoginUserAsync();

        using var actorClient1 = CreateClient(
            $"logout-all-actor-1-{Guid.NewGuid():N}");

        using var actorClient2 = CreateClient(
            $"logout-all-actor-2-{Guid.NewGuid():N}");

        using var victimClient = CreateClient(
            $"logout-all-victim-{Guid.NewGuid():N}");

        var actorCookie1 = GetSessionCookie(
            await LoginAsync(
                actorClient1,
                actor.Identifier,
                actor.Secret));

        var actorCookie2 = GetSessionCookie(
            await LoginAsync(
                actorClient2,
                actor.Identifier,
                actor.Secret));

        var victimCookie = GetSessionCookie(
            await LoginAsync(
                victimClient,
                victim.Identifier,
                victim.Secret));

        actorClient1.DefaultRequestHeaders.Add(
            "Cookie",
            actorCookie1);

        actorClient2.DefaultRequestHeaders.Add(
            "Cookie",
            actorCookie2);

        victimClient.DefaultRequestHeaders.Add(
            "Cookie",
            victimCookie);

        //
        // Victim authority is valid before actor operation.
        //
        (await GetChainsAsync(victimClient))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var logout = await actorClient1.PostAsync(
            "/auth/me/logout-all",
            null);

        logout.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        //
        // Both actor sessions must die.
        //
        (await GetChainsAsync(actorClient1))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        (await GetChainsAsync(actorClient2))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        //
        // Critical ownership invariant:
        // User B must remain completely unaffected.
        //
        var victimVerification =
            await GetChainsAsync(victimClient);

        victimVerification.StatusCode.Should()
            .Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task LogoutAllSelf_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        _factory.Clock.Reset();

        using var client = CreateClient(
            $"logout-all-anonymous-{Guid.NewGuid():N}");

        var response = await client.PostAsync(
            "/auth/me/logout-all",
            null);

        response.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LogoutAllSelf_LoggedOutDevices_ShouldBeAbleToLoginAgain()
    {
        _factory.Clock.Reset();

        var user = await _factory.CreateLoginUserAsync();

        var device1 =
            $"logout-all-relogin-1-{Guid.NewGuid():N}";

        var device2 =
            $"logout-all-relogin-2-{Guid.NewGuid():N}";

        using var client1 = CreateClient(device1);
        using var client2 = CreateClient(device2);

        var oldCookie1 = GetSessionCookie(
            await LoginAsync(
                client1,
                user.Identifier,
                user.Secret));

        var oldCookie2 = GetSessionCookie(
            await LoginAsync(
                client2,
                user.Identifier,
                user.Secret));

        client1.DefaultRequestHeaders.Add(
            "Cookie",
            oldCookie1);

        client2.DefaultRequestHeaders.Add(
            "Cookie",
            oldCookie2);

        var beforeResponse =
            await GetChainsAsync(client1);

        var before = await beforeResponse.Content
            .ReadFromJsonAsync<PagedResult<SessionChainSummary>>();

        before.Should().NotBeNull();

        var originalChains = before!.Items
            .Where(x => x.ActiveSessionId is not null)
            .ToDictionary(
                x => x.ChainId,
                x => x.ActiveSessionId);

        originalChains.Should().HaveCount(2);

        var logout = await client1.PostAsync(
            "/auth/me/logout-all",
            null);

        logout.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        //
        // Old authorities must be unusable.
        //
        using var replay1 = CreateClient(device1);
        using var replay2 = CreateClient(device2);

        replay1.DefaultRequestHeaders.Add(
            "Cookie",
            oldCookie1);

        replay2.DefaultRequestHeaders.Add(
            "Cookie",
            oldCookie2);

        (await GetChainsAsync(replay1))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        (await GetChainsAsync(replay2))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        //
        // Logout-all must not ban/revoke the devices themselves.
        //
        using var relogin1 = CreateClient(device1);
        using var relogin2 = CreateClient(device2);

        var login1 = await LoginAsync(
            relogin1,
            user.Identifier,
            user.Secret);

        var login2 = await LoginAsync(
            relogin2,
            user.Identifier,
            user.Secret);

        login1.StatusCode.Should().Be(HttpStatusCode.Found);
        login2.StatusCode.Should().Be(HttpStatusCode.Found);

        var newCookie1 = GetSessionCookie(login1);
        var newCookie2 = GetSessionCookie(login2);

        newCookie1.Should().NotBe(oldCookie1);
        newCookie2.Should().NotBe(oldCookie2);

        relogin1.DefaultRequestHeaders.Add(
            "Cookie",
            newCookie1);

        relogin2.DefaultRequestHeaders.Add(
            "Cookie",
            newCookie2);

        (await GetChainsAsync(relogin1))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        (await GetChainsAsync(relogin2))
            .StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ConcurrentLogoutAllSelf_ShouldConvergeToAllSessionsInvalid()
    {
        _factory.Clock.Reset();

        var user = await _factory.CreateLoginUserAsync();

        var actorDevice =
            $"logout-all-concurrent-actor-{Guid.NewGuid():N}";

        var otherDevice =
            $"logout-all-concurrent-other-{Guid.NewGuid():N}";

        using var actorClient = CreateClient(actorDevice);
        using var otherClient = CreateClient(otherDevice);

        var actorCookie = GetSessionCookie(
            await LoginAsync(
                actorClient,
                user.Identifier,
                user.Secret));

        var otherCookie = GetSessionCookie(
            await LoginAsync(
                otherClient,
                user.Identifier,
                user.Secret));

        //
        // Two independent HTTP requests start with the same
        // authenticated actor authority.
        //
        using var request1 = CreateClient(actorDevice);
        using var request2 = CreateClient(actorDevice);

        request1.DefaultRequestHeaders.Add(
            "Cookie",
            actorCookie);

        request2.DefaultRequestHeaders.Add(
            "Cookie",
            actorCookie);

        var responses = await Task.WhenAll(
            request1.PostAsync(
                "/auth/me/logout-all",
                null),

            request2.PostAsync(
                "/auth/me/logout-all",
                null));

        //
        // Depending on where authentication is linearized,
        // the second request may observe either the pre-logout
        // or post-logout authority.
        //
        responses.Should().OnlyContain(x =>
            x.StatusCode == HttpStatusCode.OK ||
            x.StatusCode == HttpStatusCode.Unauthorized);

        responses.Should().Contain(x =>
            x.StatusCode == HttpStatusCode.OK);

        //
        // Final state is the security invariant that matters:
        // no old authority may survive.
        //
        using var actorVerification =
            CreateClient(actorDevice);

        using var otherVerification =
            CreateClient(otherDevice);

        actorVerification.DefaultRequestHeaders.Add(
            "Cookie",
            actorCookie);

        otherVerification.DefaultRequestHeaders.Add(
            "Cookie",
            otherCookie);

        var verification = await Task.WhenAll(
            GetChainsAsync(actorVerification),
            GetChainsAsync(otherVerification));

        verification.Should().OnlyContain(x =>
            x.StatusCode == HttpStatusCode.Unauthorized);
    }


    // ------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------

    private HttpClient CreateClient(string deviceId)
    {
        var client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = false
            });

        client.DefaultRequestHeaders.Add("Origin", "https://localhost:6130");
        client.DefaultRequestHeaders.Add("X-UDID", deviceId);

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

    private static string GetSessionCookie(HttpResponseMessage response)
    {
        response.Headers
            .TryGetValues("Set-Cookie", out var cookies)
            .Should()
            .BeTrue();

        cookies.Should().NotBeNullOrEmpty();

        var cookie = cookies!.First();
        cookie.Should().NotBeNullOrWhiteSpace();

        return cookie;
    }

    private static string? GetCookieValue(string setCookie, string cookieName)
    {
        var firstSegment = setCookie
            .Split(';', 2)[0];

        var separator = firstSegment.IndexOf('=');

        if (separator < 0)
            return null;

        var name = firstSegment[..separator];

        if (!string.Equals(
                name,
                cookieName,
                StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return firstSegment[(separator + 1)..];
    }
}
