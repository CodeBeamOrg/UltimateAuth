using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using FluentAssertions;

namespace CodeBeam.UltimateAuth.Tests.Unit.Tokens.Contracts;

public abstract class RefreshTokenStoreContractTests
{
    protected abstract Task<IRefreshTokenStoreTestDatabase> CreateDatabaseAsync();

    protected static readonly TenantKey TenantA =
        TenantKey.FromExternal("tenant-a");

    protected static readonly TenantKey TenantB =
        TenantKey.FromExternal("tenant-b");

    protected static readonly DateTimeOffset Now =
        new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

    // ============================================================
    // STORE / FIND
    // ============================================================

    [Fact]
    public async Task StoreAsync_PersistsToken()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var token = CreateToken(
            TenantA,
            "hash-a11");

        await store.ExecuteAsync(
            ct => store.StoreAsync(token, ct));

        var persisted = await store.FindByHashAsync(
            token.TokenHash);

        persisted.Should().NotBeNull();

        persisted!.TokenHash.Should().Be(token.TokenHash);
        persisted.Tenant.Should().Be(token.Tenant);
        persisted.UserKey.Should().Be(token.UserKey);
        persisted.SessionId.Should().Be(token.SessionId);
        persisted.ChainId.Should().Be(token.ChainId);
        persisted.CreatedAt.Should().Be(token.CreatedAt);
        persisted.ExpiresAt.Should().Be(token.ExpiresAt);
        persisted.RevokedAt.Should().BeNull();
        persisted.ReplacedByTokenHash.Should().BeNull();
    }

    [Fact]
    public async Task FindByHashAsync_WhenMissing_ReturnsNull()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var result = await store.FindByHashAsync(
            "missing-token-hash");

        result.Should().BeNull();
    }

    [Fact]
    public async Task StoreAsync_WhenTokenBelongsToDifferentTenant_IsRejected()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var token = CreateToken(
            TenantB,
            "cross-tenant-token");

        var act = () => store.ExecuteAsync(
            ct => store.StoreAsync(token, ct));

        await act.Should()
            .ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task FindByHashAsync_IsTenantIsolated()
    {
        await using var db = await CreateDatabaseAsync();

        var storeA = db.CreateStore(TenantA);
        var storeB = db.CreateStore(TenantB);

        var token = CreateToken(
            TenantA,
            "tenant-isolation-token");

        await storeA.ExecuteAsync(
            ct => storeA.StoreAsync(token, ct));

        var fromA = await storeA.FindByHashAsync(
            token.TokenHash);

        var fromB = await storeB.FindByHashAsync(
            token.TokenHash);

        fromA.Should().NotBeNull();
        fromB.Should().BeNull();
    }

    // ============================================================
    // SINGLE REVOKE
    // ============================================================

    [Fact]
    public async Task RevokeAsync_WhenTokenExists_RevokesToken()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var token = CreateToken(
            TenantA,
            "revoke-token");

        await StoreAsync(store, token);

        var revokedAt = Now.AddMinutes(10);

        await store.ExecuteAsync(
            ct => store.RevokeAsync(
                token.TokenHash,
                revokedAt,
                ct: ct));

        var persisted = await store.FindByHashAsync(
            token.TokenHash);

        persisted.Should().NotBeNull();
        persisted!.IsRevoked.Should().BeTrue();
        persisted.RevokedAt.Should().Be(revokedAt);
        persisted.ReplacedByTokenHash.Should().BeNull();
    }

    [Fact]
    public async Task RevokeAsync_WithReplacement_PersistsReplacementHash()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var token = CreateToken(
            TenantA,
            "rotation-old");

        await StoreAsync(store, token);

        var revokedAt = Now.AddMinutes(10);

        await store.ExecuteAsync(
            ct => store.RevokeAsync(
                token.TokenHash,
                revokedAt,
                "rotation-new",
                ct));

        var persisted = await store.FindByHashAsync(
            token.TokenHash);

        persisted.Should().NotBeNull();
        persisted!.IsRevoked.Should().BeTrue();
        persisted.RevokedAt.Should().Be(revokedAt);

        persisted.ReplacedByTokenHash
            .Should().Be("rotation-new");
    }

    [Fact]
    public async Task RevokeAsync_WhenMissing_IsIdempotent()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var act = () => store.ExecuteAsync(
            ct => store.RevokeAsync(
                "missing-token",
                Now,
                ct: ct));

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RevokeAsync_WhenAlreadyRevoked_DoesNotMutateAgain()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var token = CreateToken(
            TenantA,
            "already-revoked");

        await StoreAsync(store, token);

        var firstRevocation = Now.AddMinutes(10);

        await store.ExecuteAsync(
            ct => store.RevokeAsync(
                token.TokenHash,
                firstRevocation,
                "replacement-1",
                ct));

        await store.ExecuteAsync(
            ct => store.RevokeAsync(
                token.TokenHash,
                Now.AddMinutes(20),
                "replacement-2",
                ct));

        var persisted = await store.FindByHashAsync(
            token.TokenHash);

        persisted.Should().NotBeNull();

        persisted!.RevokedAt
            .Should().Be(firstRevocation);

        persisted.ReplacedByTokenHash
            .Should().Be("replacement-1");
    }

    // ============================================================
    // REVOKE BY SESSION
    // ============================================================

    [Fact]
    public async Task RevokeBySessionAsync_RevokesTokensForSession()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var user = UserKey.New();
        var chain = SessionChainId.New();
        var session = TestIds.Session("session-revoke-target");

        var token1 = CreateToken(
            TenantA,
            "session-token-1",
            user,
            chain,
            session);

        var token2 = CreateToken(
            TenantA,
            "session-token-2",
            user,
            chain,
            session);

        await StoreAsync(store, token1, token2);

        await store.ExecuteAsync(
            ct => store.RevokeBySessionAsync(
                session,
                Now.AddMinutes(10),
                ct));

        (await store.FindByHashAsync(token1.TokenHash))!
            .IsRevoked.Should().BeTrue();

        (await store.FindByHashAsync(token2.TokenHash))!
            .IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task RevokeBySessionAsync_DoesNotAffectOtherSessions()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var user = UserKey.New();
        var chain = SessionChainId.New();

        var targetSession =
            TestIds.Session("target-session");

        var otherSession =
            TestIds.Session("other-session");

        var target = CreateToken(
            TenantA,
            "target-session-token",
            user,
            chain,
            targetSession);

        var other = CreateToken(
            TenantA,
            "other-session-token",
            user,
            chain,
            otherSession);

        await StoreAsync(store, target, other);

        await store.ExecuteAsync(
            ct => store.RevokeBySessionAsync(
                targetSession,
                Now.AddMinutes(10),
                ct));

        (await store.FindByHashAsync(target.TokenHash))!
            .IsRevoked.Should().BeTrue();

        (await store.FindByHashAsync(other.TokenHash))!
            .IsRevoked.Should().BeFalse();
    }

    // ============================================================
    // REVOKE BY CHAIN
    // ============================================================

    [Fact]
    public async Task RevokeByChainAsync_RevokesAllTokensForChain()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var user = UserKey.New();
        var chain = SessionChainId.New();

        var token1 = CreateToken(
            TenantA,
            "chain-token-1",
            user,
            chain,
            TestIds.Session("chain-session-1"));

        var token2 = CreateToken(
            TenantA,
            "chain-token-2",
            user,
            chain,
            TestIds.Session("chain-session-2"));

        await StoreAsync(store, token1, token2);

        await store.ExecuteAsync(
            ct => store.RevokeByChainAsync(
                chain,
                Now.AddMinutes(10),
                ct));

        (await store.FindByHashAsync(token1.TokenHash))!
            .IsRevoked.Should().BeTrue();

        (await store.FindByHashAsync(token2.TokenHash))!
            .IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task RevokeByChainAsync_DoesNotAffectOtherChains()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var user = UserKey.New();

        var targetChain = SessionChainId.New();
        var otherChain = SessionChainId.New();

        var target = CreateToken(
            TenantA,
            "target-chain-token",
            user,
            targetChain,
            TestIds.Session("target-chain-session"));

        var other = CreateToken(
            TenantA,
            "other-chain-token",
            user,
            otherChain,
            TestIds.Session("other-chain-session"));

        await StoreAsync(store, target, other);

        await store.ExecuteAsync(
            ct => store.RevokeByChainAsync(
                targetChain,
                Now.AddMinutes(10),
                ct));

        (await store.FindByHashAsync(target.TokenHash))!
            .IsRevoked.Should().BeTrue();

        (await store.FindByHashAsync(other.TokenHash))!
            .IsRevoked.Should().BeFalse();
    }

    // ============================================================
    // REVOKE USER
    // ============================================================

    [Fact]
    public async Task RevokeAllForUserAsync_RevokesTokensAcrossUsersChains()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var user = UserKey.New();

        var token1 = CreateToken(
            TenantA,
            "user-token-1",
            user,
            SessionChainId.New(),
            TestIds.Session("user-session-1"));

        var token2 = CreateToken(
            TenantA,
            "user-token-2",
            user,
            SessionChainId.New(),
            TestIds.Session("user-session-2"));

        await StoreAsync(store, token1, token2);

        await store.ExecuteAsync(
            ct => store.RevokeAllForUserAsync(
                user,
                Now.AddMinutes(10),
                ct));

        (await store.FindByHashAsync(token1.TokenHash))!
            .IsRevoked.Should().BeTrue();

        (await store.FindByHashAsync(token2.TokenHash))!
            .IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task RevokeAllForUserAsync_DoesNotAffectOtherUsers()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var targetUser = UserKey.New();
        var otherUser = UserKey.New();

        var target = CreateToken(
            TenantA,
            "target-user-token",
            targetUser);

        var other = CreateToken(
            TenantA,
            "other-user-token",
            otherUser);

        await StoreAsync(store, target, other);

        await store.ExecuteAsync(
            ct => store.RevokeAllForUserAsync(
                targetUser,
                Now.AddMinutes(10),
                ct));

        (await store.FindByHashAsync(target.TokenHash))!
            .IsRevoked.Should().BeTrue();

        (await store.FindByHashAsync(other.TokenHash))!
            .IsRevoked.Should().BeFalse();
    }

    // ============================================================
    // TENANT ISOLATION
    // ============================================================

    [Fact]
    public async Task RevokeAsync_IsTenantIsolated()
    {
        await using var db = await CreateDatabaseAsync();

        var storeA = db.CreateStore(TenantA);
        var storeB = db.CreateStore(TenantB);

        /*
         * Deliberately same hash.
         * Identity is (Tenant, TokenHash).
         */

        var tokenA = CreateToken(
            TenantA,
            "same-token-hash");

        var tokenB = CreateToken(
            TenantB,
            "same-token-hash");

        await StoreAsync(storeA, tokenA);
        await StoreAsync(storeB, tokenB);

        await storeA.ExecuteAsync(
            ct => storeA.RevokeAsync(
                tokenA.TokenHash,
                Now.AddMinutes(10),
                ct: ct));

        var persistedA =
            await storeA.FindByHashAsync(tokenA.TokenHash);

        var persistedB =
            await storeB.FindByHashAsync(tokenB.TokenHash);

        persistedA!.IsRevoked.Should().BeTrue();
        persistedB!.IsRevoked.Should().BeFalse();
    }

    [Fact]
    public async Task RevokeBySessionAsync_IsTenantIsolated()
    {
        await using var db = await CreateDatabaseAsync();

        var storeA = db.CreateStore(TenantA);
        var storeB = db.CreateStore(TenantB);

        var session =
            TestIds.Session("shared-session");

        var tokenA = CreateToken(
            TenantA,
            "tenant-a-session-token",
            sessionId: session);

        var tokenB = CreateToken(
            TenantB,
            "tenant-b-session-token",
            sessionId: session);

        await StoreAsync(storeA, tokenA);
        await StoreAsync(storeB, tokenB);

        await storeA.ExecuteAsync(
            ct => storeA.RevokeBySessionAsync(
                session,
                Now.AddMinutes(10),
                ct));

        (await storeA.FindByHashAsync(tokenA.TokenHash))!
            .IsRevoked.Should().BeTrue();

        (await storeB.FindByHashAsync(tokenB.TokenHash))!
            .IsRevoked.Should().BeFalse();
    }

    [Fact]
    public async Task RevokeByChainAsync_IsTenantIsolated()
    {
        await using var db = await CreateDatabaseAsync();

        var storeA = db.CreateStore(TenantA);
        var storeB = db.CreateStore(TenantB);

        var chain = SessionChainId.New();

        var tokenA = CreateToken(
            TenantA,
            "tenant-a-chain-token",
            chainId: chain);

        var tokenB = CreateToken(
            TenantB,
            "tenant-b-chain-token",
            chainId: chain);

        await StoreAsync(storeA, tokenA);
        await StoreAsync(storeB, tokenB);

        await storeA.ExecuteAsync(
            ct => storeA.RevokeByChainAsync(
                chain,
                Now.AddMinutes(10),
                ct));

        (await storeA.FindByHashAsync(tokenA.TokenHash))!
            .IsRevoked.Should().BeTrue();

        (await storeB.FindByHashAsync(tokenB.TokenHash))!
            .IsRevoked.Should().BeFalse();
    }

    [Fact]
    public async Task RevokeAllForUserAsync_IsTenantIsolated()
    {
        await using var db = await CreateDatabaseAsync();

        var storeA = db.CreateStore(TenantA);
        var storeB = db.CreateStore(TenantB);

        var user = UserKey.New();

        var tokenA = CreateToken(
            TenantA,
            "tenant-a-user-token",
            user);

        var tokenB = CreateToken(
            TenantB,
            "tenant-b-user-token",
            user);

        await StoreAsync(storeA, tokenA);
        await StoreAsync(storeB, tokenB);

        await storeA.ExecuteAsync(
            ct => storeA.RevokeAllForUserAsync(
                user,
                Now.AddMinutes(10),
                ct));

        (await storeA.FindByHashAsync(tokenA.TokenHash))!
            .IsRevoked.Should().BeTrue();

        (await storeB.FindByHashAsync(tokenB.TokenHash))!
            .IsRevoked.Should().BeFalse();
    }

    // ============================================================
    // IDEMPOTENCY / ALREADY REVOKED
    // ============================================================

    [Fact]
    public async Task BulkRevoke_DoesNotChangePreviouslyRevokedToken()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var user = UserKey.New();

        var token = CreateToken(
            TenantA,
            "previously-revoked",
            user);

        await StoreAsync(store, token);

        var firstRevocation = Now.AddMinutes(5);

        await store.ExecuteAsync(
            ct => store.RevokeAsync(
                token.TokenHash,
                firstRevocation,
                "replacement-token",
                ct));

        await store.ExecuteAsync(
            ct => store.RevokeAllForUserAsync(
                user,
                Now.AddMinutes(20),
                ct));

        var persisted =
            await store.FindByHashAsync(token.TokenHash);

        persisted!.RevokedAt.Should().Be(firstRevocation);

        persisted.ReplacedByTokenHash
            .Should().Be("replacement-token");
    }

    // ============================================================
    // CANCELLATION
    // ============================================================

    [Fact]
    public async Task FindByHashAsync_WhenCancelled_Throws()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => store.FindByHashAsync(
            "token",
            cts.Token);

        await act.Should()
            .ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task StoreAsync_WhenCancelled_Throws()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var token = CreateToken(
            TenantA,
            "cancelled-store");

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => store.ExecuteAsync(
            ct => store.StoreAsync(token, ct),
            cts.Token);

        await act.Should()
            .ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task StoreAsync_WhenChainIdIsNull_PersistsToken()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var token = RefreshToken.Create(
            tokenId: TokenId.New(),
            tokenHash: "token-without-chain",
            tenant: TenantA,
            userKey: UserKey.New(),
            sessionId: TestIds.Session("session-without-chain"),
            chainId: null,
            createdAt: Now,
            expiresAt: Now.AddDays(30));

        await StoreAsync(store, token);

        var persisted =
            await store.FindByHashAsync(token.TokenHash);

        persisted.Should().NotBeNull();
        persisted!.ChainId.Should().BeNull();
        persisted.TokenId.Should().Be(token.TokenId);
    }

    // ============================================================
    // HELPERS
    // ============================================================

    protected static Task StoreAsync(
        IRefreshTokenStore store,
        params RefreshToken[] tokens)
    {
        return store.ExecuteAsync(async ct =>
        {
            foreach (var token in tokens)
                await store.StoreAsync(token, ct);
        });
    }

    protected static RefreshToken CreateToken(
        TenantKey tenant,
        string hash,
        UserKey? userKey = null,
        SessionChainId? chainId = null,
        AuthSessionId? sessionId = null)
    {
        var user = userKey ?? UserKey.New();
        var chain = chainId ?? SessionChainId.New();
        var session =
            sessionId ?? TestIds.Session($"session-{hash}");

        return RefreshToken.Create(
            tokenId: TokenId.New(),
            tokenHash: hash,
            tenant: tenant,
            userKey: user,
            sessionId: session,
            chainId: chain,
            createdAt: Now,
            expiresAt: Now.AddDays(30));
    }
}