using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Tests.Integration.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace CodeBeam.UltimateAuth.Tests.Integration;

public class RefreshTests : IClassFixture<AuthServerFactory>
{
    private readonly AuthServerFactory _factory;
    private readonly HttpClient _client;

    public RefreshTests(AuthServerFactory factory)
    {
        _factory = factory;

        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });

        _client.DefaultRequestHeaders.Add(
            "Origin",
            "https://localhost:6130");

        _client.DefaultRequestHeaders.Add(
            "X-UDID",
            "test-device-1234567890123456");
    }

    [Fact]
    public async Task Refresh_PureOpaque_BeforeTouchInterval_ShouldNotMutateChain()
    {
        _factory.Clock.Reset();

        var user = await _factory.CreateLoginUserAsync();

        await LoginAsync(user, "BlazorServer");

        var before =
            await GetCurrentChainAsync(_client);

        var response =
            await RefreshAsync();

        response.StatusCode.Should()
            .Be(HttpStatusCode.NoContent);

        var after =
            await GetCurrentChainAsync(_client);

        after.ChainId.Should()
            .Be(before.ChainId);

        after.TouchCount.Should()
            .Be(before.TouchCount);

        after.LastSeenAt.Should()
            .Be(before.LastSeenAt);

        after.IsRevoked.Should()
            .BeFalse();
    }

    [Fact]
    public async Task Refresh_PureOpaque_AfterTouchInterval_ShouldTouchChain()
    {
        _factory.Clock.Reset();

        var user = await _factory.CreateLoginUserAsync();

        await LoginAsync(user, "BlazorServer");

        var before =
            await GetCurrentChainAsync(_client);

        _factory.Clock.Advance(
            TimeSpan.FromHours(1));

        var response =
            await RefreshAsync();

        response.StatusCode.Should()
            .Be(HttpStatusCode.NoContent);

        var after =
            await GetCurrentChainAsync(_client);

        after.ChainId.Should()
            .Be(before.ChainId);

        after.TouchCount.Should()
            .Be(before.TouchCount + 1);

        after.LastSeenAt.Should()
            .NotBeNull();

        after.LastSeenAt.Should()
            .BeAfter(before.LastSeenAt!.Value);

        after.IsRevoked.Should()
            .BeFalse();
    }

    [Fact]
    public async Task Refresh_PureOpaque_AfterTouch_ShouldNotTouchAgainBeforeNextInterval()
    {
        _factory.Clock.Reset();

        var user = await _factory.CreateLoginUserAsync();

        await LoginAsync(user, "BlazorServer");

        var initial =
            await GetCurrentChainAsync(_client);

        _factory.Clock.Advance(
            TimeSpan.FromHours(1));

        var first =
            await RefreshAsync();

        first.StatusCode.Should()
            .Be(HttpStatusCode.NoContent);

        var afterFirst =
            await GetCurrentChainAsync(_client);

        afterFirst.TouchCount.Should()
            .Be(initial.TouchCount + 1);

        var second =
            await RefreshAsync();

        second.StatusCode.Should()
            .Be(HttpStatusCode.NoContent);

        var afterSecond =
            await GetCurrentChainAsync(_client);

        afterSecond.ChainId.Should()
            .Be(afterFirst.ChainId);

        afterSecond.TouchCount.Should()
            .Be(afterFirst.TouchCount);

        afterSecond.LastSeenAt.Should()
            .Be(afterFirst.LastSeenAt);
    }

    [Fact]
    public async Task Refresh_PureOpaque_ShouldPreserveSessionAndChainIdentity()
    {
        _factory.Clock.Reset();

        var user = await _factory.CreateLoginUserAsync();

        await LoginAsync(user, "BlazorServer");

        var before =
            await GetCurrentChainAsync(_client);

        before.ActiveSessionId.Should()
            .NotBeNull();

        var sessionIdBefore =
            before.ActiveSessionId;

        var response =
            await RefreshAsync();

        response.StatusCode.Should()
            .Be(HttpStatusCode.NoContent);

        var after =
            await GetCurrentChainAsync(_client);

        after.ChainId.Should()
            .Be(before.ChainId);

        after.ActiveSessionId.Should()
            .Be(sessionIdBefore);
    }

    [Fact]
    public async Task Refresh_PureOpaque_WithoutSession_ShouldReturnUnauthorized()
    {
        _factory.Clock.Reset();

        SetClientProfile("BlazorServer");

        var response =
            await RefreshAsync();

        response.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_PureOpaque_WithRevokedSession_ShouldReturnUnauthorized()
    {
        _factory.Clock.Reset();

        var user =
            await _factory.CreateLoginUserAsync();

        await LoginAsync(
            user,
            "BlazorServer");

        var current =
            await GetCurrentChainAsync(_client);

        current.ActiveSessionId.Should()
            .NotBeNull();

        var sessionId =
            current.ActiveSessionId!.Value;

        var store =
            GetSessionStore();

        await store.ExecuteAsync(
            ct => store.RevokeSessionAsync(
                sessionId,
                _factory.Clock.UtcNow,
                ct));

        var persisted =
            await store.GetSessionAsync(sessionId);

        persisted.Should().NotBeNull();
        persisted!.IsRevoked.Should().BeTrue();

        var response =
            await RefreshAsync();

        response.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_PureOpaque_WithRevokedChain_ShouldReturnUnauthorized()
    {
        _factory.Clock.Reset();

        var user =
            await _factory.CreateLoginUserAsync();

        await LoginAsync(
            user,
            "BlazorServer");

        var current =
            await GetCurrentChainAsync(_client);

        var store =
            GetSessionStore();

        await store.ExecuteAsync(
            ct => store.RevokeChainCascadeAsync(
                current.ChainId,
                _factory.Clock.UtcNow,
                ct));

        var persistedChain =
            await store.GetChainAsync(
                current.ChainId);

        persistedChain.Should().NotBeNull();
        persistedChain!.IsRevoked.Should().BeTrue();

        var response =
            await RefreshAsync();

        response.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_PureOpaque_WithRevokedRoot_ShouldReturnUnauthorized()
    {
        _factory.Clock.Reset();

        var user =
            await _factory.CreateLoginUserAsync();

        await LoginAsync(
            user,
            "BlazorServer");

        var current =
            await GetCurrentChainAsync(_client);

        var store =
            GetSessionStore();

        var rootBefore =
            await store.GetActiveRootByUserAsync(
                user.UserKey);

        rootBefore.Should().NotBeNull();
        rootBefore!.IsRevoked.Should().BeFalse();

        await store.ExecuteAsync(
            ct => store.RevokeRootCascadeAsync(
                user.UserKey,
                _factory.Clock.UtcNow,
                ct));

        //
        // Active lookup must no longer return the revoked root.
        //
        var activeRootAfter =
            await store.GetActiveRootByUserAsync(
                user.UserKey);

        activeRootAfter.Should().BeNull();

        //
        // Historical root must still exist for audit/history.
        //
        var historicalRoot =
            await store.GetRootByIdAsync(
                rootBefore.RootId);

        historicalRoot.Should().NotBeNull();
        historicalRoot!.IsRevoked.Should().BeTrue();

        //
        // Cascade must also invalidate the authentication graph.
        //
        var chainAfter =
            await store.GetChainAsync(
                current.ChainId);

        chainAfter.Should().NotBeNull();
        chainAfter!.IsRevoked.Should().BeTrue();

        var response =
            await RefreshAsync();

        response.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_PureOpaque_FromDifferentDevice_ShouldReturnUnauthorized()
    {
        _factory.Clock.Reset();

        var user =
            await _factory.CreateLoginUserAsync();

        var cookie =
            await LoginAsync(
                user,
                "BlazorServer");

        var originalChain =
            await GetCurrentChainAsync(_client);

        originalChain.IsRevoked.Should()
            .BeFalse();

        using var otherClient =
            _factory.CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    AllowAutoRedirect = false,
                    HandleCookies = false
                });

        otherClient.DefaultRequestHeaders.Add(
            "Origin",
            "https://localhost:6130");

        otherClient.DefaultRequestHeaders.Add(
            "X-UDID",
            $"different-device-{Guid.NewGuid():N}");

        otherClient.DefaultRequestHeaders.Add(
            "X-UAuth-ClientProfile",
            "BlazorServer");

        otherClient.DefaultRequestHeaders.Add(
            "Cookie",
            cookie);

        var response =
            await otherClient.PostAsync(
                "/auth/refresh",
                null);

        response.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);

        //
        // A failed device-binding attempt must not damage
        // the legitimate session.
        //
        var legitimateVerification =
            await RefreshAsync();

        legitimateVerification.StatusCode.Should()
            .Be(HttpStatusCode.NoContent);

        var chainAfter =
            await GetCurrentChainAsync(_client);

        chainAfter.ChainId.Should()
            .Be(originalChain.ChainId);

        chainAfter.IsRevoked.Should()
            .BeFalse();
    }

    [Fact]
    public async Task Refresh_Hybrid_ShouldRotateCredentials()
    {
        _factory.Clock.Reset();

        var user =
            await _factory.CreateLoginUserAsync();

        var beforeCookie =
            await LoginAsync(
                user,
                "BlazorWasm");

        var before =
            await GetCurrentChainAsync(_client);

        before.ActiveSessionId.Should()
            .NotBeNull();

        var response =
            await RefreshAsync();

        response.StatusCode.Should()
            .Be(HttpStatusCode.NoContent);

        response.Headers.TryGetValues(
                "Set-Cookie",
                out var setCookies)
            .Should()
            .BeTrue();

        setCookies.Should()
            .NotBeNull();

        setCookies!.Should()
            .NotBeEmpty();

        var rotatedCookie =
            BuildCookieHeader(response);

        rotatedCookie.Should()
            .NotBeNullOrWhiteSpace();

        rotatedCookie.Should()
            .NotBe(beforeCookie);
    }

    [Fact]
    public async Task Refresh_Hybrid_ShouldPreserveChainIdentity()
    {
        _factory.Clock.Reset();

        var user =
            await _factory.CreateLoginUserAsync();

        await LoginAsync(
            user,
            "BlazorWasm");

        var before =
            await GetCurrentChainAsync(_client);

        var response =
            await RefreshAsync();

        response.StatusCode.Should()
            .Be(HttpStatusCode.NoContent);

        ApplyResponseCookies(response);

        var after =
            await GetCurrentChainAsync(_client);

        after.ChainId.Should()
            .Be(before.ChainId);

        after.IsRevoked.Should()
            .BeFalse();
    }

    [Fact]
    public async Task Refresh_Hybrid_RotatedCredential_ShouldBeUsableForNextRefresh()
    {
        _factory.Clock.Reset();

        var user =
            await _factory.CreateLoginUserAsync();

        await LoginAsync(
            user,
            "BlazorWasm");

        var before =
            await GetCurrentChainAsync(_client);

        var first =
            await RefreshAsync();

        first.StatusCode.Should()
            .Be(HttpStatusCode.NoContent);

        ApplyResponseCookies(first);

        var second =
            await RefreshAsync();

        second.StatusCode.Should()
            .Be(HttpStatusCode.NoContent);

        ApplyResponseCookies(second);

        var after =
            await GetCurrentChainAsync(_client);

        after.ChainId.Should()
            .Be(before.ChainId);

        after.IsRevoked.Should()
            .BeFalse();
    }

    [Fact]
    public async Task Refresh_Hybrid_ReusingPreviousCredential_ShouldReturnUnauthorized()
    {
        _factory.Clock.Reset();

        var user =
            await _factory.CreateLoginUserAsync();

        var originalCookie =
            await LoginAsync(
                user,
                "BlazorWasm");

        var first =
            await RefreshAsync();

        first.StatusCode.Should()
            .Be(HttpStatusCode.NoContent);

        var rotatedCookie =
            BuildCookieHeader(first);

        rotatedCookie.Should()
            .NotBeNullOrWhiteSpace();

        rotatedCookie.Should()
            .NotBe(originalCookie);

        //
        // Replay the credentials that were valid before rotation.
        //
        _client.DefaultRequestHeaders.Remove(
            "Cookie");

        _client.DefaultRequestHeaders.Add(
            "Cookie",
            originalCookie);

        var replay =
            await RefreshAsync();

        replay.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_Hybrid_WithoutRefreshToken_ShouldReturnUnauthorized()
    {
        _factory.Clock.Reset();

        var user =
            await _factory.CreateLoginUserAsync();

        var cookie =
            await LoginAsync(
                user,
                "BlazorWasm");

        var sessionCookie = cookie
            .Split("; ", StringSplitOptions.RemoveEmptyEntries)
            .Single(x => x.StartsWith("uas=", StringComparison.Ordinal));

        _client.DefaultRequestHeaders.Remove("Cookie");
        _client.DefaultRequestHeaders.Add(
            "Cookie",
            sessionCookie);

        var response =
            await RefreshAsync();

        response.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_Hybrid_WithRefreshTokenFromDifferentSession_ShouldReturnUnauthorized()
    {
        _factory.Clock.Reset();

        var user =
            await _factory.CreateLoginUserAsync();

        using var clientA =
            CreateClient(
                $"hybrid-session-a-{Guid.NewGuid():N}");

        using var clientB =
            CreateClient(
                $"hybrid-session-b-{Guid.NewGuid():N}");

        var cookieA =
            await LoginAsync(
                clientA,
                user,
                "BlazorWasm");

        var cookieB =
            await LoginAsync(
                clientB,
                user,
                "BlazorWasm");

        var sessionB =
            GetCookie(cookieB, "uas");

        var refreshA =
            GetCookie(cookieA, "uar");

        sessionB.Should().NotBeNullOrWhiteSpace();
        refreshA.Should().NotBeNullOrWhiteSpace();

        clientB.DefaultRequestHeaders.Remove("Cookie");

        clientB.DefaultRequestHeaders.Add(
            "Cookie",
            $"{sessionB}; {refreshA}");

        var response =
            await clientB.PostAsync(
                "/auth/refresh",
                null);

        response.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_Hybrid_FromDifferentDevice_ShouldReturnUnauthorized()
    {
        _factory.Clock.Reset();

        var user =
            await _factory.CreateLoginUserAsync();

        using var originalClient =
            CreateClient(
                $"hybrid-original-{Guid.NewGuid():N}");

        using var foreignClient =
            CreateClient(
                $"hybrid-foreign-{Guid.NewGuid():N}");

        var cookie =
            await LoginAsync(
                originalClient,
                user,
                "BlazorWasm");

        foreignClient.DefaultRequestHeaders.Remove("Cookie");

        foreignClient.DefaultRequestHeaders.Add(
            "Cookie",
            cookie);

        var response =
            await foreignClient.PostAsync(
                "/auth/refresh",
                null);

        response.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);

        //
        // Failed attack must not invalidate
        // the legitimate credential set.
        //
        var legitimateRefresh =
            await originalClient.PostAsync(
                "/auth/refresh",
                null);

        legitimateRefresh.StatusCode.Should()
            .Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Refresh_Hybrid_WithRevokedSession_ShouldReturnUnauthorized()
    {
        _factory.Clock.Reset();

        var user =
            await _factory.CreateLoginUserAsync();

        await LoginAsync(
            user,
            "BlazorWasm");

        var chain =
            await GetCurrentChainAsync(_client);

        chain.ActiveSessionId.Should()
            .NotBeNull();

        await RevokeSessionDirectlyAsync(
            chain.ActiveSessionId!.Value);

        var response =
            await RefreshAsync();

        response.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_Hybrid_WithRevokedChain_ShouldReturnUnauthorized()
    {
        _factory.Clock.Reset();

        var user =
            await _factory.CreateLoginUserAsync();

        await LoginAsync(
            user,
            "BlazorWasm");

        var chain =
            await GetCurrentChainAsync(_client);

        await RevokeChainDirectlyAsync(
            chain.ChainId);

        var response =
            await RefreshAsync();

        response.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_Hybrid_WithRevokedRoot_ShouldReturnUnauthorized()
    {
        _factory.Clock.Reset();

        var user =
            await _factory.CreateLoginUserAsync();

        await LoginAsync(
            user,
            "BlazorWasm");

        await RevokeRootDirectlyAsync(
            user.UserKey);

        var response =
            await RefreshAsync();

        response.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_Hybrid_ConcurrentReuseOfSameRefreshToken_ShouldAllowExactlyOneRequest()
    {
        _factory.Clock.Reset();

        var user =
            await _factory.CreateLoginUserAsync();

        var deviceId =
            $"hybrid-race-{Guid.NewGuid():N}";

        using var loginClient =
            CreateClient(deviceId);

        var originalCookies =
            await LoginAsync(
                loginClient,
                user,
                "BlazorWasm");

        using var clientA =
            CreateClient(deviceId);

        using var clientB =
            CreateClient(deviceId);

        SetClientProfile(clientA, "BlazorWasm");
        SetClientProfile(clientB, "BlazorWasm");

        clientA.DefaultRequestHeaders.Add(
            "Cookie",
            originalCookies);

        clientB.DefaultRequestHeaders.Add(
            "Cookie",
            originalCookies);

        var responses =
            await Task.WhenAll(
                clientA.PostAsync("/auth/refresh", null),
                clientB.PostAsync("/auth/refresh", null));

        responses.Count(x =>
                x.StatusCode == HttpStatusCode.NoContent)
            .Should()
            .Be(1);

        responses.Count(x =>
                x.StatusCode == HttpStatusCode.Unauthorized)
            .Should()
            .Be(1);
    }

    [Fact]
    public async Task Refresh_Hybrid_ConcurrentDuplicate_ShouldLeaveWinningCredentialUsable()
    {
        await using var factory = AuthServerFactory.Create(options =>
        {
            options.Token.RefreshTokenConcurrentRequestWindow =
                TimeSpan.FromSeconds(5);
        });

        factory.Clock.Reset();

        var user = await factory.CreateLoginUserAsync();

        var deviceId =
            $"hybrid-concurrent-{Guid.NewGuid():N}";

        using var loginClient =
            CreateClient(factory, deviceId);

        var originalCookies =
            await LoginAsync(
                loginClient,
                user,
                "BlazorWasm");

        using var clientA =
            CreateClient(factory, deviceId);

        using var clientB =
            CreateClient(factory, deviceId);

        SetClientProfile(clientA, "BlazorWasm");
        SetClientProfile(clientB, "BlazorWasm");

        clientA.DefaultRequestHeaders.Add(
            "Cookie",
            originalCookies);

        clientB.DefaultRequestHeaders.Add(
            "Cookie",
            originalCookies);

        var responses =
            await Task.WhenAll(
                clientA.PostAsync("/auth/refresh", null),
                clientB.PostAsync("/auth/refresh", null));

        var winner =
            responses.Single(x =>
                x.StatusCode == HttpStatusCode.NoContent);

        responses.Single(x =>
            x.StatusCode == HttpStatusCode.Unauthorized);

        var winningCredentials =
            BuildHybridCredentialAfterRefresh(
                originalCookies,
                winner);

        //
        // Losing concurrent request must NOT have
        // revoked the chain.
        //
        using var continuation =
            CreateClient(factory, deviceId);

        SetClientProfile(
            continuation,
            "BlazorWasm");

        continuation.DefaultRequestHeaders.Add(
            "Cookie",
            winningCredentials);

        var result =
            await continuation.PostAsync(
                "/auth/refresh",
                null);

        result.StatusCode.Should()
            .Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Refresh_Hybrid_AfterConcurrentRotation_OriginalRefreshToken_ShouldRemainInvalid()
    {
        _factory.Clock.Reset();

        var user =
            await _factory.CreateLoginUserAsync();

        var deviceId =
            $"hybrid-race-replay-{Guid.NewGuid():N}";

        using var loginClient =
            CreateClient(deviceId);

        var originalCookies =
            await LoginAsync(
                loginClient,
                user,
                "BlazorWasm");

        using var clientA =
            CreateClient(deviceId);

        using var clientB =
            CreateClient(deviceId);

        SetClientProfile(clientA, "BlazorWasm");
        SetClientProfile(clientB, "BlazorWasm");

        clientA.DefaultRequestHeaders.Add(
            "Cookie",
            originalCookies);

        clientB.DefaultRequestHeaders.Add(
            "Cookie",
            originalCookies);

        var responses =
            await Task.WhenAll(
                clientA.PostAsync("/auth/refresh", null),
                clientB.PostAsync("/auth/refresh", null));

        responses.Count(x =>
                x.StatusCode == HttpStatusCode.NoContent)
            .Should()
            .Be(1);

        //
        // Try the original credential yet again.
        //
        using var replayClient =
            CreateClient(deviceId);

        SetClientProfile(
            replayClient,
            "BlazorWasm");

        replayClient.DefaultRequestHeaders.Add(
            "Cookie",
            originalCookies);

        var replay =
            await replayClient.PostAsync(
                "/auth/refresh",
                null);

        replay.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_Hybrid_ConcurrentRotation_ShouldNotCreateAnotherChain()
    {
        _factory.Clock.Reset();

        var user =
            await _factory.CreateLoginUserAsync();

        var deviceId =
            $"hybrid-race-chain-{Guid.NewGuid():N}";

        using var loginClient =
            CreateClient(deviceId);

        var originalCookies =
            await LoginAsync(
                loginClient,
                user,
                "BlazorWasm");

        var before =
            await GetCurrentChainAsync(loginClient);

        using var clientA =
            CreateClient(deviceId);

        using var clientB =
            CreateClient(deviceId);

        SetClientProfile(clientA, "BlazorWasm");
        SetClientProfile(clientB, "BlazorWasm");

        clientA.DefaultRequestHeaders.Add(
            "Cookie",
            originalCookies);

        clientB.DefaultRequestHeaders.Add(
            "Cookie",
            originalCookies);

        var responses =
            await Task.WhenAll(
                clientA.PostAsync("/auth/refresh", null),
                clientB.PostAsync("/auth/refresh", null));

        var success =
            responses.Single(x =>
                x.StatusCode == HttpStatusCode.NoContent);

        var winningCookies =
            BuildHybridCredentialAfterRefresh(
                originalCookies,
                success);

        using var verificationClient =
            CreateClient(deviceId);

        SetClientProfile(
            verificationClient,
            "BlazorWasm");

        verificationClient.DefaultRequestHeaders.Add(
            "Cookie",
            winningCookies);

        var after =
            await GetCurrentChainAsync(
                verificationClient);

        after.ChainId.Should()
            .Be(before.ChainId);

        after.IsRevoked.Should()
            .BeFalse();
    }

    // ============================================================
    // HYBRID - REVOCATION / REPLAY SECURITY
    // ============================================================

    [Fact]
    public async Task Refresh_Hybrid_AfterSessionRevoked_ShouldReturnUnauthorized()
    {
        _factory.Clock.Reset();

        var user = await _factory.CreateLoginUserAsync();
        var deviceId = $"refresh-revoke-session-{Guid.NewGuid():N}";

        using var client = CreateClient(deviceId);

        var cookies = await LoginAsync(
            client,
            user,
            "BlazorWasm");

        var before = await GetCurrentChainAsync(client);

        before.ActiveSessionId.Should().NotBeNull();

        //
        // Revoke the active session directly through the session store.
        // This test is about refresh behaviour after revocation,
        // not about testing the revoke endpoint again.
        //
        using (var scope = _factory.Services.CreateScope())
        {
            var storeFactory =
                scope.ServiceProvider.GetRequiredService<ISessionStoreFactory>();

            var store =
                storeFactory.Create(TenantKeys.Single);

            await store.ExecuteAsync(
                ct => store.RevokeSessionAsync(
                    before.ActiveSessionId!.Value,
                    _factory.Clock.UtcNow,
                    ct));
        }

        client.DefaultRequestHeaders.Remove("Cookie");
        client.DefaultRequestHeaders.Add("Cookie", cookies);

        var response =
            await client.PostAsync("/auth/refresh", null);

        response.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_Hybrid_AfterChainRevoked_ShouldReturnUnauthorized()
    {
        _factory.Clock.Reset();

        var user = await _factory.CreateLoginUserAsync();
        var deviceId = $"refresh-revoke-chain-{Guid.NewGuid():N}";

        using var client = CreateClient(deviceId);

        var cookies = await LoginAsync(
            client,
            user,
            "BlazorWasm");

        var before =
            await GetCurrentChainAsync(client);

        using (var scope = _factory.Services.CreateScope())
        {
            var storeFactory =
                scope.ServiceProvider.GetRequiredService<ISessionStoreFactory>();

            var store =
                storeFactory.Create(TenantKeys.Single);

            await store.ExecuteAsync(
                ct => store.RevokeChainCascadeAsync(
                    before.ChainId,
                    _factory.Clock.UtcNow,
                    ct));
        }

        client.DefaultRequestHeaders.Remove("Cookie");
        client.DefaultRequestHeaders.Add("Cookie", cookies);

        var response =
            await client.PostAsync("/auth/refresh", null);

        response.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_Hybrid_AfterRootRevoked_ShouldReturnUnauthorized()
    {
        _factory.Clock.Reset();

        var user = await _factory.CreateLoginUserAsync();
        var deviceId = $"refresh-revoke-root-{Guid.NewGuid():N}";

        using var client = CreateClient(deviceId);

        var cookies = await LoginAsync(
            client,
            user,
            "BlazorWasm");

        // Sanity check: credential is valid before root revocation.
        var before = await GetCurrentChainAsync(client);

        before.IsRevoked.Should().BeFalse();

        using (var scope = _factory.Services.CreateScope())
        {
            var storeFactory =
                scope.ServiceProvider.GetRequiredService<ISessionStoreFactory>();

            var store =
                storeFactory.Create(TenantKeys.Single);

            await store.ExecuteAsync(
                ct => store.RevokeRootCascadeAsync(
                    user.UserKey,
                    _factory.Clock.UtcNow,
                    ct));
        }

        client.DefaultRequestHeaders.Remove("Cookie");
        client.DefaultRequestHeaders.Add("Cookie", cookies);

        var response =
            await client.PostAsync("/auth/refresh", null);

        response.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_Hybrid_ReusingRotatedToken_ShouldReturnUnauthorized()
    {
        _factory.Clock.Reset();

        var user = await _factory.CreateLoginUserAsync();
        var deviceId = $"refresh-replay-{Guid.NewGuid():N}";

        using var loginClient = CreateClient(deviceId);

        var originalCookies =
            await LoginAsync(
                loginClient,
                user,
                "BlazorWasm");

        //
        // R0 -> R1
        //
        var first =
            await loginClient.PostAsync(
                "/auth/refresh",
                null);

        first.StatusCode.Should()
            .Be(HttpStatusCode.NoContent);

        //
        // Deliberately replay R0.
        //
        using var replayClient =
            CreateClient(deviceId);

        SetClientProfile(
            replayClient,
            "BlazorWasm");

        replayClient.DefaultRequestHeaders.Add(
            "Cookie",
            originalCookies);

        var replay =
            await replayClient.PostAsync(
                "/auth/refresh",
                null);

        replay.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_Hybrid_ReplayAfterConcurrencyWindow_ShouldInvalidateReplacementCredential()
    {
        await using var factory = AuthServerFactory.Create(options =>
        {
            options.Token.RefreshTokenConcurrentRequestWindow =
                TimeSpan.FromSeconds(2);
        });

        factory.Clock.Reset();

        var user =
            await factory.CreateLoginUserAsync();

        var deviceId =
            $"refresh-replay-family-{Guid.NewGuid():N}";

        using var client =
            CreateClient(factory, deviceId);

        var originalCookies =
            await LoginAsync(
                client,
                user,
                "BlazorWasm");

        //
        // R0 -> R1
        //
        var rotation =
            await client.PostAsync(
                "/auth/refresh",
                null);

        rotation.StatusCode.Should()
            .Be(HttpStatusCode.NoContent);

        var replacementCookies =
            BuildHybridCredentialAfterRefresh(
                originalCookies,
                rotation);

        //
        // Move OUTSIDE the concurrency tolerance window.
        //
        factory.Clock.Advance(
            TimeSpan.FromSeconds(3));

        //
        // Replay R0.
        //
        using var attacker =
            CreateClient(factory, deviceId);

        SetClientProfile(
            attacker,
            "BlazorWasm");

        attacker.DefaultRequestHeaders.Add(
            "Cookie",
            originalCookies);

        var replay =
            await attacker.PostAsync(
                "/auth/refresh",
                null);

        replay.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);

        //
        // Confirmed replay must invalidate the chain.
        // Therefore R1 must also be dead.
        //
        using var legitimateClient =
            CreateClient(factory, deviceId);

        SetClientProfile(
            legitimateClient,
            "BlazorWasm");

        legitimateClient.DefaultRequestHeaders.Add(
            "Cookie",
            replacementCookies);

        var legitimateRefresh =
            await legitimateClient.PostAsync(
                "/auth/refresh",
                null);

        legitimateRefresh.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_Hybrid_DuplicateWithinConcurrencyWindow_ShouldNotInvalidateReplacement()
    {
        await using var factory = AuthServerFactory.Create(options =>
        {
            options.Token.RefreshTokenConcurrentRequestWindow = TimeSpan.FromSeconds(2);
        });

        factory.Clock.Reset();

        var user = await factory.CreateLoginUserAsync();

        var deviceId = $"refresh-duplicate-window-{Guid.NewGuid():N}";

        using var client = CreateClient(factory, deviceId);

        var originalCookies = await LoginAsync(client, user, "BlazorWasm");

        // R0 -> R1
        var rotation = await client.PostAsync("/auth/refresh", null);

        rotation.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var replacementCookies = BuildHybridCredentialAfterRefresh(originalCookies, rotation);

        // Replay R0 immediately.
        // Still inside duplicate tolerance window.
        using var duplicate = CreateClient(factory, deviceId);

        SetClientProfile(duplicate, "BlazorWasm");

        duplicate.DefaultRequestHeaders.Add("Cookie", originalCookies);

        var duplicateResponse = await duplicate.PostAsync("/auth/refresh", null);

        duplicateResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Important:
        // duplicate was rejected but must NOT have destroyed R1.
        using var legitimate = CreateClient(factory, deviceId);

        SetClientProfile(legitimate, "BlazorWasm");

        legitimate.DefaultRequestHeaders.Add("Cookie", replacementCookies);

        var continuation = await legitimate.PostAsync("/auth/refresh", null);

        continuation.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Refresh_Hybrid_ReplayOnOneChain_ShouldNotAffectOtherDeviceChain()
    {
        _factory.Clock.Reset();

        var user =
            await _factory.CreateLoginUserAsync();

        var deviceA =
            $"refresh-compromised-{Guid.NewGuid():N}";

        var deviceB =
            $"refresh-safe-{Guid.NewGuid():N}";

        using var clientA =
            CreateClient(deviceA);

        using var clientB =
            CreateClient(deviceB);

        var originalCookiesA =
            await LoginAsync(
                clientA,
                user,
                "BlazorWasm");

        var originalCookiesB =
            await LoginAsync(
                clientB,
                user,
                "BlazorWasm");

        var chainA =
            await GetCurrentChainAsync(clientA);

        var chainB =
            await GetCurrentChainAsync(clientB);

        chainA.ChainId.Should()
            .NotBe(chainB.ChainId);

        //
        // Rotate device A:
        // A:R0 -> A:R1
        //
        var rotationA =
            await clientA.PostAsync(
                "/auth/refresh",
                null);

        rotationA.StatusCode.Should()
            .Be(HttpStatusCode.NoContent);

        //
        // Replay A:R0.
        //
        using var attacker =
            CreateClient(deviceA);

        SetClientProfile(
            attacker,
            "BlazorWasm");

        attacker.DefaultRequestHeaders.Add(
            "Cookie",
            originalCookiesA);

        var replay =
            await attacker.PostAsync(
                "/auth/refresh",
                null);

        replay.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);

        //
        // Device B belongs to another chain.
        // Compromise of A must not kill B.
        //
        using var safeClient =
            CreateClient(deviceB);

        SetClientProfile(
            safeClient,
            "BlazorWasm");

        safeClient.DefaultRequestHeaders.Add(
            "Cookie",
            originalCookiesB);

        var safeRefresh =
            await safeClient.PostAsync(
                "/auth/refresh",
                null);

        safeRefresh.StatusCode.Should()
            .Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Refresh_Hybrid_JustBeforeRefreshTokenExpiry_ShouldSucceed()
    {
        _factory.Clock.Reset();

        var user = await _factory.CreateLoginUserAsync();
        var deviceId = $"hybrid-expiry-before-{Guid.NewGuid():N}";

        using var client = CreateClient(deviceId);

        var cookies = await LoginAsync(
            client,
            user,
            "BlazorWasm");

        _factory.Clock.Advance(
            TimeSpan.FromDays(7) - TimeSpan.FromSeconds(1));

        client.DefaultRequestHeaders.Remove("Cookie");
        client.DefaultRequestHeaders.Add("Cookie", cookies);

        var response = await client.PostAsync(
            "/auth/refresh",
            null);

        response.StatusCode.Should()
            .Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Refresh_Hybrid_AtRefreshTokenExpiry_ShouldReturnUnauthorized()
    {
        _factory.Clock.Reset();

        var user = await _factory.CreateLoginUserAsync();
        var deviceId = $"hybrid-expiry-exact-{Guid.NewGuid():N}";

        using var client = CreateClient(deviceId);

        var cookies = await LoginAsync(
            client,
            user,
            "BlazorWasm");

        _factory.Clock.Advance(
            TimeSpan.FromDays(7));

        client.DefaultRequestHeaders.Remove("Cookie");
        client.DefaultRequestHeaders.Add("Cookie", cookies);

        var response = await client.PostAsync(
            "/auth/refresh",
            null);

        response.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_Hybrid_AfterRefreshTokenExpiry_ShouldReturnUnauthorized()
    {
        _factory.Clock.Reset();

        var user = await _factory.CreateLoginUserAsync();
        var deviceId = $"hybrid-expiry-after-{Guid.NewGuid():N}";

        using var client = CreateClient(deviceId);

        var cookies = await LoginAsync(
            client,
            user,
            "BlazorWasm");

        _factory.Clock.Advance(
            TimeSpan.FromDays(7) + TimeSpan.FromSeconds(1));

        client.DefaultRequestHeaders.Remove("Cookie");
        client.DefaultRequestHeaders.Add("Cookie", cookies);

        var response = await client.PostAsync(
            "/auth/refresh",
            null);

        response.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_Hybrid_WhenSessionExpired_ShouldReturnUnauthorized()
    {
        await using var factory = AuthServerFactory.Create(options =>
        {
            options.Session.Lifetime =
                TimeSpan.FromMinutes(30);

            options.Token.RefreshTokenLifetime =
                TimeSpan.FromDays(7);
        });

        factory.Clock.Reset();

        var user =
            await factory.CreateLoginUserAsync();

        var deviceId =
            $"hybrid-session-expiry-{Guid.NewGuid():N}";

        using var client =
            CreateClient(factory, deviceId);

        var cookies =
            await LoginAsync(
                client,
                user,
                "BlazorWasm");

        //
        // Session expired, refresh token is still valid.
        //
        factory.Clock.Advance(
            TimeSpan.FromMinutes(31));

        client.DefaultRequestHeaders.Remove("Cookie");
        client.DefaultRequestHeaders.Add(
            "Cookie",
            cookies);

        var response =
            await client.PostAsync(
                "/auth/refresh",
                null);

        response.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_Hybrid_WhenChainIdleTimeoutExceeded_ShouldReturnUnauthorized()
    {
        await using var factory = AuthServerFactory.Create(options =>
        {
            options.Session.Lifetime =
                TimeSpan.FromDays(7);

            options.Session.IdleTimeout =
                TimeSpan.FromMinutes(30);

            options.Token.RefreshTokenLifetime =
                TimeSpan.FromDays(7);
        });

        factory.Clock.Reset();

        var user =
            await factory.CreateLoginUserAsync();

        var deviceId =
            $"hybrid-idle-expiry-{Guid.NewGuid():N}";

        using var client =
            CreateClient(factory, deviceId);

        var cookies =
            await LoginAsync(
                client,
                user,
                "BlazorWasm");

        factory.Clock.Advance(
            TimeSpan.FromMinutes(31));

        client.DefaultRequestHeaders.Remove("Cookie");
        client.DefaultRequestHeaders.Add(
            "Cookie",
            cookies);

        var response =
            await client.PostAsync(
                "/auth/refresh",
                null);

        response.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_Hybrid_JustBeforeChainIdleTimeout_ShouldSucceed()
    {
        await using var factory = AuthServerFactory.Create(options =>
        {
            options.Session.Lifetime =
                TimeSpan.FromDays(7);

            options.Session.IdleTimeout =
                TimeSpan.FromMinutes(30);

            options.Token.RefreshTokenLifetime =
                TimeSpan.FromDays(7);
        });

        factory.Clock.Reset();

        var user =
            await factory.CreateLoginUserAsync();

        var deviceId =
            $"hybrid-idle-before-{Guid.NewGuid():N}";

        using var client =
            CreateClient(factory, deviceId);

        var cookies =
            await LoginAsync(
                client,
                user,
                "BlazorWasm");

        factory.Clock.Advance(
            TimeSpan.FromMinutes(30) -
            TimeSpan.FromSeconds(1));

        client.DefaultRequestHeaders.Remove("Cookie");
        client.DefaultRequestHeaders.Add(
            "Cookie",
            cookies);

        var response =
            await client.PostAsync(
                "/auth/refresh",
                null);

        response.StatusCode.Should()
            .Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Refresh_Hybrid_AtChainIdleTimeout_ShouldReturnUnauthorized()
    {
        await using var factory = AuthServerFactory.Create(options =>
        {
            options.Session.Lifetime =
                TimeSpan.FromDays(7);

            options.Session.IdleTimeout =
                TimeSpan.FromMinutes(30);

            options.Token.RefreshTokenLifetime =
                TimeSpan.FromDays(7);
        });

        factory.Clock.Reset();

        var user =
            await factory.CreateLoginUserAsync();

        var deviceId =
            $"hybrid-idle-exact-{Guid.NewGuid():N}";

        using var client =
            CreateClient(factory, deviceId);

        var cookies =
            await LoginAsync(
                client,
                user,
                "BlazorWasm");

        factory.Clock.Advance(
            TimeSpan.FromMinutes(30));

        client.DefaultRequestHeaders.Remove("Cookie");
        client.DefaultRequestHeaders.Add(
            "Cookie",
            cookies);

        var response =
            await client.PostAsync(
                "/auth/refresh",
                null);

        response.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);
    }

    // ============================================================
    // HYBRID - RESPONSE / COOKIE CONTRACT
    // ============================================================

    [Fact]
    public async Task Refresh_Hybrid_OnSuccess_ShouldRotateRefreshCookie()
    {
        _factory.Clock.Reset();

        var user =
            await _factory.CreateLoginUserAsync();

        var deviceId =
            $"hybrid-cookie-rotate-{Guid.NewGuid():N}";

        using var client =
            CreateClient(deviceId);

        var originalCookies =
            await LoginAsync(
                client,
                user,
                "BlazorWasm");

        var originalRefresh =
            GetCookie(originalCookies, "uar");

        var response =
            await client.PostAsync(
                "/auth/refresh",
                null);

        response.StatusCode.Should()
            .Be(HttpStatusCode.NoContent);

        var responseCookies =
            BuildCookieHeader(response);

        var rotatedRefresh =
            GetCookie(responseCookies, "uar");

        rotatedRefresh.Should()
            .NotBeNullOrWhiteSpace();

        rotatedRefresh.Should()
            .NotBe(originalRefresh);
    }

    [Fact]
    public async Task Refresh_Hybrid_OnSuccess_ShouldNotRotateSessionCookie()
    {
        _factory.Clock.Reset();

        var user =
            await _factory.CreateLoginUserAsync();

        var deviceId =
            $"hybrid-cookie-session-{Guid.NewGuid():N}";

        using var client =
            CreateClient(deviceId);

        var originalCookies =
            await LoginAsync(
                client,
                user,
                "BlazorWasm");

        var originalSession =
            GetCookie(originalCookies, "uas");

        var response =
            await client.PostAsync(
                "/auth/refresh",
                null);

        response.StatusCode.Should()
            .Be(HttpStatusCode.NoContent);

        //
        // Hybrid refresh rotates the refresh credential.
        // Session identity itself must remain stable.
        //
        var rotatedCredentials =
            BuildHybridCredentialAfterRefresh(
                originalCookies,
                response);

        var sessionAfter =
            GetCookie(rotatedCredentials, "uas");

        sessionAfter.Should()
            .Be(originalSession);
    }

    [Fact]
    public async Task Refresh_Hybrid_OnUnauthorized_ShouldNotIssueReplacementRefreshCookie()
    {
        _factory.Clock.Reset();

        var user =
            await _factory.CreateLoginUserAsync();

        var deviceId =
            $"hybrid-cookie-failure-{Guid.NewGuid():N}";

        using var client =
            CreateClient(deviceId);

        var originalCookies =
            await LoginAsync(
                client,
                user,
                "BlazorWasm");

        //
        // Remove refresh credential while preserving session.
        //
        var session =
            GetCookie(originalCookies, "uas");

        client.DefaultRequestHeaders.Remove("Cookie");

        client.DefaultRequestHeaders.Add(
            "Cookie",
            session);

        var response =
            await client.PostAsync(
                "/auth/refresh",
                null);

        response.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);

        var issuedRefreshCookie =
            response.Headers.TryGetValues(
                "Set-Cookie",
                out var setCookies)
            &&
            setCookies.Any(x =>
                x.StartsWith(
                    "uar=",
                    StringComparison.OrdinalIgnoreCase));

        issuedRefreshCookie.Should()
            .BeFalse();
    }

    [Fact]
    public async Task Refresh_Hybrid_OnReplayDetection_ShouldNotIssueReplacementRefreshCookie()
    {
        _factory.Clock.Reset();

        var user =
            await _factory.CreateLoginUserAsync();

        var deviceId =
            $"hybrid-cookie-replay-{Guid.NewGuid():N}";

        using var client =
            CreateClient(deviceId);

        var originalCookies =
            await LoginAsync(
                client,
                user,
                "BlazorWasm");

        //
        // R0 -> R1
        //
        var rotation =
            await client.PostAsync(
                "/auth/refresh",
                null);

        rotation.StatusCode.Should()
            .Be(HttpStatusCode.NoContent);

        //
        // Replay R0.
        //
        using var replayClient =
            CreateClient(deviceId);

        SetClientProfile(
            replayClient,
            "BlazorWasm");

        replayClient.DefaultRequestHeaders.Add(
            "Cookie",
            originalCookies);

        var replay =
            await replayClient.PostAsync(
                "/auth/refresh",
                null);

        replay.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);

        var issuedRefreshCookie =
            replay.Headers.TryGetValues(
                "Set-Cookie",
                out var setCookies)
            &&
            setCookies.Any(x =>
                x.StartsWith(
                    "uar=",
                    StringComparison.OrdinalIgnoreCase));

        issuedRefreshCookie.Should()
            .BeFalse();
    }

    [Fact]
    public async Task Refresh_Hybrid_ConcurrentLoser_ShouldNotReceiveReplacementRefreshCookie()
    {
        _factory.Clock.Reset();

        var user =
            await _factory.CreateLoginUserAsync();

        var deviceId =
            $"hybrid-cookie-race-{Guid.NewGuid():N}";

        using var loginClient =
            CreateClient(deviceId);

        var originalCookies =
            await LoginAsync(
                loginClient,
                user,
                "BlazorWasm");

        using var clientA =
            CreateClient(deviceId);

        using var clientB =
            CreateClient(deviceId);

        SetClientProfile(
            clientA,
            "BlazorWasm");

        SetClientProfile(
            clientB,
            "BlazorWasm");

        clientA.DefaultRequestHeaders.Add(
            "Cookie",
            originalCookies);

        clientB.DefaultRequestHeaders.Add(
            "Cookie",
            originalCookies);

        var responses =
            await Task.WhenAll(
                clientA.PostAsync(
                    "/auth/refresh",
                    null),
                clientB.PostAsync(
                    "/auth/refresh",
                    null));

        var winner =
            responses.Single(x =>
                x.StatusCode ==
                HttpStatusCode.NoContent);

        var loser =
            responses.Single(x =>
                x.StatusCode ==
                HttpStatusCode.Unauthorized);

        winner.Headers.TryGetValues(
                "Set-Cookie",
                out var winnerCookies)
            .Should()
            .BeTrue();

        winnerCookies.Should()
            .Contain(x =>
                x.StartsWith(
                    "uar=",
                    StringComparison.OrdinalIgnoreCase));

        var loserReceivedRefresh =
            loser.Headers.TryGetValues(
                "Set-Cookie",
                out var loserCookies)
            &&
            loserCookies.Any(x =>
                x.StartsWith(
                    "uar=",
                    StringComparison.OrdinalIgnoreCase));

        loserReceivedRefresh.Should()
            .BeFalse();
    }

    // ============================================================
    // PURE OPAQUE - RESPONSE / COOKIE CONTRACT
    // ============================================================

    [Fact]
    public async Task Refresh_PureOpaque_OnSuccess_ShouldNotIssueRefreshCookie()
    {
        _factory.Clock.Reset();

        var user =
            await _factory.CreateLoginUserAsync();

        await LoginAsync(
            user,
            "BlazorServer");

        var response =
            await RefreshAsync();

        response.StatusCode.Should()
            .Be(HttpStatusCode.NoContent);

        var issuedRefreshCookie =
            response.Headers.TryGetValues(
                "Set-Cookie",
                out var setCookies)
            &&
            setCookies.Any(x =>
                x.StartsWith(
                    "uar=",
                    StringComparison.OrdinalIgnoreCase));

        issuedRefreshCookie.Should()
            .BeFalse();
    }

    [Fact]
    public async Task Refresh_PureOpaque_OnSuccess_ShouldPreserveSessionCookie()
    {
        _factory.Clock.Reset();

        var user =
            await _factory.CreateLoginUserAsync();

        var originalCookies =
            await LoginAsync(
                user,
                "BlazorServer");

        var originalSession =
            GetCookie(
                originalCookies,
                "uas");

        var response =
            await RefreshAsync();

        response.StatusCode.Should()
            .Be(HttpStatusCode.NoContent);

        // PureOpaque refresh is a validation/touch operation.
        // It must not establish a different session identity.
        var currentCookie =
            _client.DefaultRequestHeaders
                .GetValues("Cookie")
                .Single();

        GetCookie(
                currentCookie,
                "uas")
            .Should()
            .Be(originalSession);
    }

    [Fact]
    public async Task Refresh_Hybrid_CredentialFromTenantA_ShouldNotBeUsableInTenantB()
    {
        using var factory =
            AuthServerFactory.Create(options =>
            {
                options.MultiTenant.Enabled = true;
                options.MultiTenant.EnableHeader = true;
                options.MultiTenant.HeaderName = "X-Tenant";
            });

        factory.Clock.Reset();

        var tenantA =
            TenantKey.FromExternal("tenant-a");

        var tenantB =
            TenantKey.FromExternal("tenant-b");

        var userA =
            await factory.CreateLoginUserAsync(
                tenant: tenantA);

        using var loginClient =
            CreateClient(
                factory,
                $"tenant-a-device-{Guid.NewGuid():N}");

        SetTenant(
            loginClient,
            "tenant-a");

        var cookiesA =
            await LoginAsync(
                loginClient,
                userA,
                "BlazorWasm");

        //
        // Same credential is now presented under Tenant B.
        //
        using var tenantBClient =
            CreateClient(
                factory,
                $"tenant-a-device-{Guid.NewGuid():N}");

        SetClientProfile(
            tenantBClient,
            "BlazorWasm");

        SetTenant(
            tenantBClient,
            "tenant-b");

        tenantBClient.DefaultRequestHeaders.Add(
            "Cookie",
            cookiesA);

        var response =
            await tenantBClient.PostAsync(
                "/auth/refresh",
                null);

        response.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_Hybrid_CrossTenantAttempt_ShouldNotInvalidateSourceTenantCredential()
    {
        using var factory =
            AuthServerFactory.Create(options =>
            {
                options.MultiTenant.Enabled = true;
                options.MultiTenant.EnableHeader = true;
                options.MultiTenant.HeaderName = "X-Tenant";
            });

        factory.Clock.Reset();

        var tenantA =
            TenantKey.FromExternal("tenant-a");

        var userA =
            await factory.CreateLoginUserAsync(
                tenant: tenantA);

        var deviceId =
            $"tenant-isolation-{Guid.NewGuid():N}";

        using var loginClient =
            CreateClient(
                factory,
                deviceId);

        SetTenant(
            loginClient,
            "tenant-a");

        var cookiesA =
            await LoginAsync(
                loginClient,
                userA,
                "BlazorWasm");

        //
        // Attack / accidental cross-tenant presentation.
        //
        using var tenantBClient =
            CreateClient(
                factory,
                deviceId);

        SetClientProfile(
            tenantBClient,
            "BlazorWasm");

        SetTenant(
            tenantBClient,
            "tenant-b");

        tenantBClient.DefaultRequestHeaders.Add(
            "Cookie",
            cookiesA);

        var crossTenant =
            await tenantBClient.PostAsync(
                "/auth/refresh",
                null);

        crossTenant.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);

        //
        // Original credential must still work in its real tenant.
        //
        using var tenantAClient =
            CreateClient(
                factory,
                deviceId);

        SetClientProfile(
            tenantAClient,
            "BlazorWasm");

        SetTenant(
            tenantAClient,
            "tenant-a");

        tenantAClient.DefaultRequestHeaders.Add(
            "Cookie",
            cookiesA);

        var legitimate =
            await tenantAClient.PostAsync(
                "/auth/refresh",
                null);

        legitimate.StatusCode.Should()
            .Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Refresh_Hybrid_DifferentTenants_ShouldRotateIndependently()
    {
        using var factory =
            AuthServerFactory.Create(options =>
            {
                options.MultiTenant.Enabled = true;
                options.MultiTenant.EnableHeader = true;
                options.MultiTenant.HeaderName = "X-Tenant";
            });

        factory.Clock.Reset();

        var tenantA =
            TenantKey.FromExternal("tenant-a");

        var tenantB =
            TenantKey.FromExternal("tenant-b");

        var userA =
            await factory.CreateLoginUserAsync(
                tenant: tenantA);

        var userB =
            await factory.CreateLoginUserAsync(
                tenant: tenantB);

        using var clientA =
            CreateClient(
                factory,
                $"tenant-a-{Guid.NewGuid():N}");

        using var clientB =
            CreateClient(
                factory,
                $"tenant-b-{Guid.NewGuid():N}");

        SetTenant(
            clientA,
            "tenant-a");

        SetTenant(
            clientB,
            "tenant-b");

        await LoginAsync(
            clientA,
            userA,
            "BlazorWasm");

        await LoginAsync(
            clientB,
            userB,
            "BlazorWasm");

        var responses =
            await Task.WhenAll(
                clientA.PostAsync(
                    "/auth/refresh",
                    null),
                clientB.PostAsync(
                    "/auth/refresh",
                    null));

        responses.Should()
            .OnlyContain(x =>
                x.StatusCode ==
                HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Refresh_Hybrid_SessionFromTenantAAndRefreshFromTenantB_ShouldBeRejected()
    {
        using var factory =
            AuthServerFactory.Create(options =>
            {
                options.MultiTenant.Enabled = true;
                options.MultiTenant.EnableHeader = true;
                options.MultiTenant.HeaderName = "X-Tenant";
            });

        factory.Clock.Reset();

        var tenantA =
            TenantKey.FromExternal("tenant-a");

        var tenantB =
            TenantKey.FromExternal("tenant-b");

        var deviceId =
            $"tenant-mixed-{Guid.NewGuid():N}";

        var userA =
            await factory.CreateLoginUserAsync(
                tenant: tenantA);

        var userB =
            await factory.CreateLoginUserAsync(
                tenant: tenantB);

        using var clientA =
            CreateClient(factory, deviceId);

        using var clientB =
            CreateClient(factory, deviceId);

        SetTenant(clientA, "tenant-a");
        SetTenant(clientB, "tenant-b");

        var cookiesA =
            await LoginAsync(
                clientA,
                userA,
                "BlazorWasm");

        var cookiesB =
            await LoginAsync(
                clientB,
                userB,
                "BlazorWasm");

        var sessionA =
            GetCookie(cookiesA, "uas");

        var refreshB =
            GetCookie(cookiesB, "uar");

        var mixedCredential =
            $"{sessionA}; {refreshB}";

        using var attacker =
            CreateClient(factory, deviceId);

        SetClientProfile(
            attacker,
            "BlazorWasm");

        SetTenant(
            attacker,
            "tenant-a");

        attacker.DefaultRequestHeaders.Add(
            "Cookie",
            mixedCredential);

        var response =
            await attacker.PostAsync(
                "/auth/refresh",
                null);

        response.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);
    }



    private async Task<string> LoginAsync(IntegrationTestUser user, string profile = "BlazorServer")
    {
        SetClientProfile(profile);

        var response = await _client.PostAsJsonAsync(
            "/auth/login",
            new
            {
                identifier = user.Identifier,
                secret = user.Secret
            });

        response.StatusCode.Should()
            .Be(HttpStatusCode.Found);

        var cookieHeader =
            BuildCookieHeader(response);

        cookieHeader.Should()
            .NotBeNullOrWhiteSpace();

        _client.DefaultRequestHeaders.Remove("Cookie");

        _client.DefaultRequestHeaders.Add(
            "Cookie",
            cookieHeader);

        return cookieHeader;
    }

    private static async Task<string> LoginAsync(HttpClient client, IntegrationTestUser user, string profile)
    {
        client.DefaultRequestHeaders.Remove(
            "X-UAuth-ClientProfile");

        client.DefaultRequestHeaders.Add(
            "X-UAuth-ClientProfile",
            profile);

        var response =
            await client.PostAsJsonAsync(
                "/auth/login",
                new
                {
                    identifier = user.Identifier,
                    secret = user.Secret
                });

        response.StatusCode.Should().Be(HttpStatusCode.Found);

        var cookieHeader = BuildCookieHeader(response);
        cookieHeader.Should().NotBeNullOrWhiteSpace();

        client.DefaultRequestHeaders.Remove("Cookie");
        client.DefaultRequestHeaders.Add("Cookie", cookieHeader);

        return cookieHeader;
    }

    private Task<HttpResponseMessage> RefreshAsync()
    {
        return _client.PostAsync("/auth/refresh", null);
    }

    private ISessionStore GetSessionStore()
    {
        var factory = _factory.Services.GetRequiredService<ISessionStoreFactory>();

        return factory.Create(TenantKeys.Single);
    }

    private void SetClientProfile(string profile)
    {
        _client.DefaultRequestHeaders.Remove("X-UAuth-ClientProfile");
        _client.DefaultRequestHeaders.Add("X-UAuth-ClientProfile", profile);
    }

    private static void SetClientProfile(HttpClient client, string profile)
    {
        client.DefaultRequestHeaders.Remove("X-UAuth-ClientProfile");
        client.DefaultRequestHeaders.Add("X-UAuth-ClientProfile", profile);
    }

    private static string BuildCookieHeader(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var values))
            return string.Empty;

        return string.Join(
            "; ",
            values.Select(x => x.Split(';')[0]));
    }

    private static async Task<SessionChainSummary> GetCurrentChainAsync(HttpClient client)
    {
        var response = await GetChainsAsync(client);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PagedResult<SessionChainSummary>>();
        result.Should().NotBeNull();

        return result!.Items.Single(x => x.IsCurrentDevice);
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

    private HttpClient CreateClient(string deviceId)
    {
        var client =
            _factory.CreateClient(
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

    private static HttpClient CreateClient(AuthServerFactory factory, string deviceId)
    {
        var client =
            factory.CreateClient(
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

    private static string GetCookie(string cookieHeader, string name)
    {
        return cookieHeader
            .Split(
                "; ",
                StringSplitOptions.RemoveEmptyEntries)
            .Single(x =>
                x.StartsWith(
                    $"{name}=",
                    StringComparison.Ordinal));
    }

    private void ApplyResponseCookies(HttpResponseMessage response)
    {
        var cookieHeader = BuildCookieHeader(response);
        cookieHeader.Should().NotBeNullOrWhiteSpace();

        _client.DefaultRequestHeaders.Remove("Cookie");
        _client.DefaultRequestHeaders.Add("Cookie", cookieHeader);
    }

    private async Task RevokeSessionDirectlyAsync(AuthSessionId sessionId)
    {
        using var scope = _factory.Services.CreateScope();

        var factory = scope.ServiceProvider.GetRequiredService<ISessionStoreFactory>();

        var store = factory.Create(TenantKeys.Single);

        await store.ExecuteAsync(ct => store.RevokeSessionAsync(sessionId, _factory.Clock.UtcNow, ct));
    }

    private async Task RevokeChainDirectlyAsync(SessionChainId chainId)
    {
        using var scope = _factory.Services.CreateScope();

        var factory = scope.ServiceProvider.GetRequiredService<ISessionStoreFactory>();

        var store = factory.Create(TenantKeys.Single);

        await store.ExecuteAsync(ct => store.RevokeChainCascadeAsync(chainId, _factory.Clock.UtcNow,ct));
    }

    private async Task RevokeRootDirectlyAsync(UserKey userKey)
    {
        using var scope = _factory.Services.CreateScope();

        var factory = scope.ServiceProvider.GetRequiredService<ISessionStoreFactory>();

        var store = factory.Create(TenantKeys.Single);

        await store.ExecuteAsync(ct => store.RevokeRootCascadeAsync(userKey, _factory.Clock.UtcNow, ct));
    }

    private static string BuildHybridCredentialAfterRefresh(string originalCookies, HttpResponseMessage refreshResponse)
    {
        var session =
            GetCookie(originalCookies, "uas");

        var refreshed =
            BuildCookieHeader(refreshResponse);

        var refresh =
            GetCookie(refreshed, "uar");

        return $"{session}; {refresh}";
    }

    private static void SetTenant(HttpClient client, string tenant)
    {
        client.DefaultRequestHeaders.Remove(
            "X-Tenant");

        client.DefaultRequestHeaders.Add(
            "X-Tenant",
            tenant);
    }
}
