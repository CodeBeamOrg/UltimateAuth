using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using FluentAssertions;

namespace CodeBeam.UltimateAuth.Tests.Unit.Sessions.Contracts;

public abstract class SessionStoreSessionContractTests
{
    protected abstract Task<ISessionStoreTestDatabase> CreateDatabaseAsync();

    protected static async Task<(UAuthSessionRoot Root, UAuthSessionChain Chain, UAuthSession Session)> CreateSessionGraphAsync(
        ISessionStore store,
        TenantKey tenant,
        UserKey? userKey = null,
        AuthSessionId? sessionId = null)
    {
        var user = userKey ?? UserKey.New();

        // 1. Root
        var root = UAuthSessionRoot.Create(
            tenant,
            user,
            Now);

        await store.ExecuteAsync(
            ct => store.CreateRootAsync(root, ct));

        // 2. Chain
        var chain = UAuthSessionChain.Create(
            SessionChainId.New(),
            root.RootId,
            tenant,
            user,
            Now,
            expiresAt: Now.AddDays(30),
            device: TestDevice.Default(),
            claimsSnapshot: ClaimsSnapshot.Empty,
            securityVersion: root.SecurityVersion);

        await store.ExecuteAsync(
            ct => store.CreateChainAsync(chain, ct));

        // 3. Session
        var session = UAuthSession.Create(
            sessionId ?? NewSessionId(),
            tenant,
            user,
            chain.ChainId,
            Now,
            Now.AddHours(1),
            securityVersion: root.SecurityVersion,
            device: TestDevice.Default(),
            claims: ClaimsSnapshot.Empty,
            metadata: SessionMetadata.Empty);

        await store.ExecuteAsync(
            ct => store.CreateSessionAsync(session, ct));

        return (root, chain, session);
    }

    protected static readonly TenantKey Tenant =
        TenantKey.FromExternal("tenant-a");

    protected static readonly DateTimeOffset Now =
        new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

    private static UAuthSession CreateSession(
        TenantKey tenant,
        UserKey user,
        SessionChainId chainId,
        AuthSessionId? sessionId = null)
    {
        return UAuthSession.Create(
            sessionId ?? NewSessionId(),
            tenant,
            user,
            chainId,
            now: Now,
            expiresAt: Now.AddHours(1),
            securityVersion: 0,
            device: TestDevice.Default(),
            claims: ClaimsSnapshot.Empty,
            metadata: SessionMetadata.Empty);
    }

    private static AuthSessionId NewSessionId()
    {
        var raw = $"session-{Guid.NewGuid():N}";

        if (!AuthSessionId.TryCreate(raw, out var id))
            throw new InvalidOperationException(
                "Failed to create test session id.");

        return id;
    }

    private static Task WriteAsync(
        ISessionStore store,
        Func<CancellationToken, Task> action)
        => store.ExecuteAsync(action);

    // ------------------------------------------------------------
    // CREATE / GET
    // ------------------------------------------------------------

    [Fact]
    public async Task CreateSessionAsync_PersistsSession()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var (_, chain, session) =
            await CreateSessionGraphAsync(
                store,
                Tenant);

        var result = await store.GetSessionAsync(
            session.SessionId);

        result.Should().NotBeNull();

        result!.SessionId.Should().Be(session.SessionId);
        result.Tenant.Should().Be(Tenant);
        result.UserKey.Should().Be(session.UserKey);
        result.ChainId.Should().Be(chain.ChainId);
        result.CreatedAt.Should().Be(Now);
        result.ExpiresAt.Should().Be(Now.AddHours(1));
        result.RevokedAt.Should().BeNull();
        result.SecurityVersionAtCreation.Should().Be(0);
        result.Version.Should().Be(0);
    }

    [Fact]
    public async Task GetSessionAsync_WhenSessionDoesNotExist_ReturnsNull()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var result = await store.GetSessionAsync(
            NewSessionId());

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateSessionAsync_WhenIdAlreadyExists_ThrowsConcurrency()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var (root, chain, first) =
            await CreateSessionGraphAsync(
                store,
                Tenant);

        var second = UAuthSession.Create(
            first.SessionId,
            Tenant,
            first.UserKey,
            chain.ChainId,
            Now,
            Now.AddHours(2),
            root.SecurityVersion,
            TestDevice.Default(),
            ClaimsSnapshot.Empty,
            SessionMetadata.Empty);

        var act = () => store.ExecuteAsync(
            ct => store.CreateSessionAsync(
                second,
                ct));

        await act.Should()
            .ThrowAsync<UAuthConcurrencyException>();
    }

    [Fact]
    public async Task CreateSessionAsync_WhenVersionIsNotZero_IsRejected()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var session = CreateSession(
            Tenant,
            UserKey.New(),
            SessionChainId.New());

        session.Version = 1;

        var act = () => WriteAsync(
            store,
            ct => store.CreateSessionAsync(session, ct));

        await act.Should()
            .ThrowAsync<InvalidOperationException>();
    }

    // ------------------------------------------------------------
    // SAVE
    // ------------------------------------------------------------

    [Fact]
    public async Task SaveSessionAsync_WhenExpectedVersionMatches_PersistsChanges()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var (_, _, session) =
            await CreateSessionGraphAsync(
                store,
                Tenant);

        var updated =
            session.Revoke(Now.AddMinutes(10));

        await store.ExecuteAsync(
            ct => store.SaveSessionAsync(
                updated,
                expectedVersion: session.Version,
                ct));

        var result =
            await store.GetSessionAsync(
                session.SessionId);

        result.Should().NotBeNull();
        result!.IsRevoked.Should().BeTrue();
        result.RevokedAt.Should().Be(
            Now.AddMinutes(10));
        result.Version.Should().Be(1);
    }

    [Fact]
    public async Task SaveSessionAsync_WhenSessionDoesNotExist_ThrowsNotFound()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var session = CreateSession(
            Tenant,
            UserKey.New(),
            SessionChainId.New());

        var updated = session.Revoke(
            Now.AddMinutes(10));

        var act = () => WriteAsync(
            store,
            ct => store.SaveSessionAsync(
                updated,
                expectedVersion: session.Version,
                ct));

        await act.Should()
            .ThrowAsync<UAuthNotFoundException>();
    }

    [Fact]
    public async Task SaveSessionAsync_WhenExpectedVersionIsStale_ThrowsConcurrency()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var (_, _, session) =
            await CreateSessionGraphAsync(
                store,
                Tenant);

        var updated =
            session.Revoke(Now.AddMinutes(10));

        var act = () => store.ExecuteAsync(
            ct => store.SaveSessionAsync(
                updated,
                expectedVersion: 999,
                ct));

        await act.Should()
            .ThrowAsync<UAuthConcurrencyException>();
    }

    // ------------------------------------------------------------
    // REVOKE
    // ------------------------------------------------------------

    [Fact]
    public async Task RevokeSessionAsync_WhenSessionExists_RevokesAndReturnsTrue()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var (_, _, session) =
            await CreateSessionGraphAsync(
                store,
                Tenant);

        bool revoked = false;

        await store.ExecuteAsync(async ct =>
        {
            revoked = await store.RevokeSessionAsync(
                session.SessionId,
                Now.AddMinutes(10),
                ct);
        });

        revoked.Should().BeTrue();

        var result = await store.GetSessionAsync(
            session.SessionId);

        result.Should().NotBeNull();
        result!.IsRevoked.Should().BeTrue();
        result.RevokedAt.Should().Be(Now.AddMinutes(10));
        result.Version.Should().Be(1);
    }

    [Fact]
    public async Task RevokeSessionAsync_WhenSessionDoesNotExist_ReturnsFalse()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        bool result = true;

        await store.ExecuteAsync(async ct =>
        {
            result = await store.RevokeSessionAsync(
                NewSessionId(),
                Now,
                ct);
        });

        result.Should().BeFalse();
    }

    [Fact]
    public async Task RevokeSessionAsync_WhenAlreadyRevoked_ReturnsFalse()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var (_, _, session) =
            await CreateSessionGraphAsync(
                store,
                Tenant);

        await store.ExecuteAsync(
            ct => store.RevokeSessionAsync(
                session.SessionId,
                Now.AddMinutes(5),
                ct));

        bool secondResult = true;

        await store.ExecuteAsync(async ct =>
        {
            secondResult =
                await store.RevokeSessionAsync(
                    session.SessionId,
                    Now.AddMinutes(10),
                    ct);
        });

        secondResult.Should().BeFalse();

        var result =
            await store.GetSessionAsync(
                session.SessionId);

        result.Should().NotBeNull();
        result!.RevokedAt.Should().Be(
            Now.AddMinutes(5));
        result.Version.Should().Be(1);
    }

    // ------------------------------------------------------------
    // REMOVE
    // ------------------------------------------------------------

    [Fact]
    public async Task RemoveSessionAsync_RemovesSession()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var (_, _, session) =
            await CreateSessionGraphAsync(
                store,
                Tenant);

        await store.ExecuteAsync(
            ct => store.RemoveSessionAsync(
                session.SessionId,
                ct));

        var result =
            await store.GetSessionAsync(
                session.SessionId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task RemoveSessionAsync_WhenSessionDoesNotExist_IsIdempotent()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var act = () => WriteAsync(
            store,
            ct => store.RemoveSessionAsync(
                NewSessionId(),
                ct));

        await act.Should().NotThrowAsync();
    }

    // ------------------------------------------------------------
    // LOOKUPS
    // ------------------------------------------------------------

    [Fact]
    public async Task GetChainIdBySessionAsync_ReturnsOwningChain()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var (_, chain, session) =
            await CreateSessionGraphAsync(
                store,
                Tenant);

        var result =
            await store.GetChainIdBySessionAsync(
                session.SessionId);

        result.Should().Be(chain.ChainId);
    }

    [Fact]
    public async Task GetChainIdBySessionAsync_WhenMissing_ReturnsNull()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var result = await store.GetChainIdBySessionAsync(
            NewSessionId());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSessionsByChainAsync_ReturnsOnlyRequestedChain()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var user = UserKey.New();

        var (_, chainA, a1) =
            await CreateSessionGraphAsync(
                store,
                Tenant,
                user);

        var a2 = CreateSession(
            Tenant,
            user,
            chainA.ChainId);

        await store.ExecuteAsync(
            ct => store.CreateSessionAsync(a2, ct));

        var root =
            await store.GetRootByUserAsync(user);

        root.Should().NotBeNull();

        var chainB = UAuthSessionChain.Create(
            SessionChainId.New(),
            root!.RootId,
            Tenant,
            user,
            Now,
            Now.AddDays(30),
            TestDevice.Default(),
            ClaimsSnapshot.Empty,
            root.SecurityVersion);

        await store.ExecuteAsync(
            ct => store.CreateChainAsync(chainB, ct));

        var b1 = CreateSession(
            Tenant,
            user,
            chainB.ChainId);

        await store.ExecuteAsync(
            ct => store.CreateSessionAsync(b1, ct));

        var result =
            await store.GetSessionsByChainAsync(
                chainA.ChainId);

        result.Should().HaveCount(2);

        result.Select(x => x.SessionId)
            .Should()
            .BeEquivalentTo([
                a1.SessionId,
            a2.SessionId
            ]);
    }

    // ------------------------------------------------------------
    // TENANT ISOLATION
    // ------------------------------------------------------------

    [Fact]
    public async Task GetSessionAsync_IsTenantIsolated()
    {
        await using var db = await CreateDatabaseAsync();

        var tenantA =
            TenantKey.FromExternal("tenant-a");

        var tenantB =
            TenantKey.FromExternal("tenant-b");

        var storeA = db.CreateStore(tenantA);
        var storeB = db.CreateStore(tenantB);

        var (_, _, session) =
            await CreateSessionGraphAsync(
                storeA,
                tenantA);

        var fromA =
            await storeA.GetSessionAsync(
                session.SessionId);

        var fromB =
            await storeB.GetSessionAsync(
                session.SessionId);

        fromA.Should().NotBeNull();
        fromB.Should().BeNull();
    }

    [Fact]
    public async Task CreateSessionAsync_WhenSessionBelongsToDifferentTenant_IsRejected()
    {
        await using var db = await CreateDatabaseAsync();

        var tenantA =
            TenantKey.FromExternal("tenant-a");

        var tenantB =
            TenantKey.FromExternal("tenant-b");

        var storeA = db.CreateStore(tenantA);

        var session = CreateSession(
            tenantB,
            UserKey.New(),
            SessionChainId.New());

        var act = () => storeA.ExecuteAsync(
            ct => storeA.CreateSessionAsync(
                session,
                ct));

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Tenant mismatch.");
    }

    // ------------------------------------------------------------
    // CANCELLATION
    // ------------------------------------------------------------

    [Fact]
    public async Task GetSessionAsync_WhenCancelled_Throws()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () =>
            store.GetSessionAsync(
                NewSessionId(),
                cts.Token);

        await act.Should()
            .ThrowAsync<OperationCanceledException>();
    }
}
