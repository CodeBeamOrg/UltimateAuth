using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using FluentAssertions;

namespace CodeBeam.UltimateAuth.Tests.Unit.Sessions.Contracts;

public abstract class SessionStoreLifecycleContractTests
{
    protected abstract Task<ISessionStoreTestDatabase> CreateDatabaseAsync();

    protected static readonly TenantKey Tenant =
        TenantKey.FromExternal("tenant-a");

    protected static readonly DateTimeOffset Now =
        new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

    /*
        User A
        └── Root A
            ├── Chain A1
            │   ├── Session A11
            │   └── Session A12 [ACTIVE]
            │
            └── Chain A2
                └── Session A21 [ACTIVE]

        User B
        └── Root B
            └── Chain B1
                └── Session B11 [ACTIVE]
    */

    protected sealed record LifecycleGraph(
        UserKey UserA,
        UAuthSessionRoot RootA,
        UAuthSessionChain ChainA1,
        UAuthSession SessionA11,
        UAuthSession SessionA12,
        UAuthSessionChain ChainA2,
        UAuthSession SessionA21,

        UserKey UserB,
        UAuthSessionRoot RootB,
        UAuthSessionChain ChainB1,
        UAuthSession SessionB11);

    // ============================================================
    // GRAPH
    // ============================================================

    protected static async Task<LifecycleGraph> CreateGraphAsync(
        ISessionStore store)
    {
        var userA = UserKey.New();
        var userB = UserKey.New();

        var rootA = UAuthSessionRoot.Create(
            Tenant,
            userA,
            Now);

        var rootB = UAuthSessionRoot.Create(
            Tenant,
            userB,
            Now);

        await store.ExecuteAsync(async ct =>
        {
            await store.CreateRootAsync(rootA, ct);
            await store.CreateRootAsync(rootB, ct);
        });

        var chainA1 = CreateChain(
            rootA,
            userA,
            Now.AddMinutes(1));

        var chainA2 = CreateChain(
            rootA,
            userA,
            Now.AddMinutes(2),
            TestDevice.Alternative());

        var chainB1 = CreateChain(
            rootB,
            userB,
            Now.AddMinutes(3));

        await store.ExecuteAsync(async ct =>
        {
            await store.CreateChainAsync(chainA1, ct);
            await store.CreateChainAsync(chainA2, ct);
            await store.CreateChainAsync(chainB1, ct);
        });

        var sessionA11 = CreateSession(
            chainA1,
            userA,
            "lifecycle-session-a11",
            Now.AddMinutes(4));

        var sessionA12 = CreateSession(
            chainA1,
            userA,
            "lifecycle-session-a12",
            Now.AddMinutes(5));

        var sessionA21 = CreateSession(
            chainA2,
            userA,
            "lifecycle-session-a21",
            Now.AddMinutes(6));

        var sessionB11 = CreateSession(
            chainB1,
            userB,
            "lifecycle-session-b11",
            Now.AddMinutes(7));

        await store.ExecuteAsync(async ct =>
        {
            await store.CreateSessionAsync(sessionA11, ct);
            await store.CreateSessionAsync(sessionA12, ct);
            await store.CreateSessionAsync(sessionA21, ct);
            await store.CreateSessionAsync(sessionB11, ct);
        });

        /*
         * Establish active sessions through the Chain aggregate.
         *
         * A12 = active on A1
         * A21 = active on A2
         * B11 = active on B1
         */

        var chainA1Active = chainA1.AttachSession(
            sessionA12.SessionId,
            Now.AddMinutes(8));

        var chainA2Active = chainA2.AttachSession(
            sessionA21.SessionId,
            Now.AddMinutes(8));

        var chainB1Active = chainB1.AttachSession(
            sessionB11.SessionId,
            Now.AddMinutes(8));

        await store.ExecuteAsync(async ct =>
        {
            await store.SaveChainAsync(
                chainA1Active,
                chainA1.Version,
                ct);

            await store.SaveChainAsync(
                chainA2Active,
                chainA2.Version,
                ct);

            await store.SaveChainAsync(
                chainB1Active,
                chainB1.Version,
                ct);
        });

        return new LifecycleGraph(
            userA,
            rootA,
            chainA1Active,
            sessionA11,
            sessionA12,
            chainA2Active,
            sessionA21,

            userB,
            rootB,
            chainB1Active,
            sessionB11);
    }

    private static UAuthSessionChain CreateChain(
        UAuthSessionRoot root,
        UserKey user,
        DateTimeOffset createdAt,
        DeviceContext? device = null)
    {
        return UAuthSessionChain.Create(
            SessionChainId.New(),
            root.RootId,
            root.Tenant,
            user,
            createdAt,
            createdAt.AddDays(30),
            device ?? TestDevice.Default(),
            ClaimsSnapshot.Empty,
            root.SecurityVersion);
    }

    private static UAuthSession CreateSession(
        UAuthSessionChain chain,
        UserKey user,
        string id,
        DateTimeOffset createdAt)
    {
        return UAuthSession.Create(
            TestIds.Session(id),
            chain.Tenant,
            user,
            chain.ChainId,
            createdAt,
            createdAt.AddHours(1),
            chain.SecurityVersionAtCreation,
            chain.Device,
            claims: null,
            metadata: new SessionMetadata());
    }

    // ============================================================
    // GRAPH SANITY
    // ============================================================

    [Fact]
    public async Task Graph_HasExpectedActiveSessions()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var graph = await CreateGraphAsync(store);

        var chainA1 = await store.GetChainAsync(
            graph.ChainA1.ChainId);

        var chainA2 = await store.GetChainAsync(
            graph.ChainA2.ChainId);

        var chainB1 = await store.GetChainAsync(
            graph.ChainB1.ChainId);

        chainA1!.ActiveSessionId
            .Should().Be(graph.SessionA12.SessionId);

        chainA2!.ActiveSessionId
            .Should().Be(graph.SessionA21.SessionId);

        chainB1!.ActiveSessionId
            .Should().Be(graph.SessionB11.SessionId);
    }

    // ============================================================
    // SINGLE SESSION REVOKE
    // ============================================================

    [Fact]
    public async Task RevokeSessionAsync_WhenSessionIsNotActive_RevokesSession()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var graph = await CreateGraphAsync(store);

        var revokedAt = Now.AddMinutes(20);

        await store.ExecuteAsync(
            ct => store.RevokeSessionAsync(
                graph.SessionA11.SessionId,
                revokedAt,
                ct));

        var session = await store.GetSessionAsync(
            graph.SessionA11.SessionId);

        session.Should().NotBeNull();
        session!.IsRevoked.Should().BeTrue();
        session.RevokedAt.Should().Be(revokedAt);
    }

    [Fact]
    public async Task RevokeSessionAsync_WhenSessionIsNotActive_PreservesActiveSession()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var graph = await CreateGraphAsync(store);

        await store.ExecuteAsync(
            ct => store.RevokeSessionAsync(
                graph.SessionA11.SessionId,
                Now.AddMinutes(20),
                ct));

        var chain = await store.GetChainAsync(
            graph.ChainA1.ChainId);

        chain!.ActiveSessionId
            .Should().Be(graph.SessionA12.SessionId);
    }

    [Fact]
    public async Task RevokeSessionAsync_WhenSessionIsActive_DoesNotLeaveRevokedSessionActive()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var graph = await CreateGraphAsync(store);

        await store.ExecuteAsync(
            ct => store.RevokeSessionAsync(
                graph.SessionA12.SessionId,
                Now.AddMinutes(20),
                ct));

        var session = await store.GetSessionAsync(
            graph.SessionA12.SessionId);

        var chain = await store.GetChainAsync(
            graph.ChainA1.ChainId);

        session.Should().NotBeNull();
        session!.IsRevoked.Should().BeTrue();

        chain.Should().NotBeNull();
        chain!.ActiveSessionId.Should().BeNull(
            "a revoked session must not remain the active session of its chain");
    }

    [Fact]
    public async Task RevokeSessionAsync_DoesNotAffectOtherChainsOrUsers()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var graph = await CreateGraphAsync(store);

        await store.ExecuteAsync(
            ct => store.RevokeSessionAsync(
                graph.SessionA12.SessionId,
                Now.AddMinutes(20),
                ct));

        var a21 = await store.GetSessionAsync(
            graph.SessionA21.SessionId);

        var b11 = await store.GetSessionAsync(
            graph.SessionB11.SessionId);

        a21!.IsRevoked.Should().BeFalse();
        b11!.IsRevoked.Should().BeFalse();
    }

    // ============================================================
    // REMOVE SESSION
    // ============================================================

    [Fact]
    public async Task RemoveSessionAsync_WhenSessionIsNotActive_RemovesSession()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var graph = await CreateGraphAsync(store);

        await store.ExecuteAsync(
            ct => store.RemoveSessionAsync(
                graph.SessionA11.SessionId,
                ct));

        var session = await store.GetSessionAsync(
            graph.SessionA11.SessionId);

        session.Should().BeNull();
    }

    [Fact]
    public async Task RemoveSessionAsync_WhenSessionIsNotActive_PreservesActiveSession()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var graph = await CreateGraphAsync(store);

        await store.ExecuteAsync(
            ct => store.RemoveSessionAsync(
                graph.SessionA11.SessionId,
                ct));

        var chain = await store.GetChainAsync(
            graph.ChainA1.ChainId);

        chain!.ActiveSessionId
            .Should().Be(graph.SessionA12.SessionId);
    }

    [Fact]
    public async Task RemoveSessionAsync_WhenSessionIsActive_DoesNotLeaveDanglingReference()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var graph = await CreateGraphAsync(store);

        await store.ExecuteAsync(
            ct => store.RemoveSessionAsync(
                graph.SessionA12.SessionId,
                ct));

        var session = await store.GetSessionAsync(
            graph.SessionA12.SessionId);

        var chain = await store.GetChainAsync(
            graph.ChainA1.ChainId);

        session.Should().BeNull();

        chain.Should().NotBeNull();
        chain!.ActiveSessionId.Should().BeNull(
            "a removed session must not remain referenced by its chain");
    }

    // ============================================================
    // LOGOUT CHAIN
    // ============================================================

    [Fact]
    public async Task LogoutChainAsync_RevokesAllSessionsInChain()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var graph = await CreateGraphAsync(store);

        var at = Now.AddMinutes(20);

        await store.ExecuteAsync(
            ct => store.LogoutChainAsync(
                graph.ChainA1.ChainId,
                at,
                ct));

        var a11 = await store.GetSessionAsync(
            graph.SessionA11.SessionId);

        var a12 = await store.GetSessionAsync(
            graph.SessionA12.SessionId);

        a11!.IsRevoked.Should().BeTrue();
        a12!.IsRevoked.Should().BeTrue();

        a11.RevokedAt.Should().Be(at);
        a12.RevokedAt.Should().Be(at);
    }

    [Fact]
    public async Task LogoutChainAsync_DetachesActiveSession()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var graph = await CreateGraphAsync(store);

        await store.ExecuteAsync(
            ct => store.LogoutChainAsync(
                graph.ChainA1.ChainId,
                Now.AddMinutes(20),
                ct));

        var chain = await store.GetChainAsync(
            graph.ChainA1.ChainId);

        chain.Should().NotBeNull();
        chain!.ActiveSessionId.Should().BeNull();
    }

    [Fact]
    public async Task LogoutChainAsync_DoesNotRevokeChain()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var graph = await CreateGraphAsync(store);

        await store.ExecuteAsync(
            ct => store.LogoutChainAsync(
                graph.ChainA1.ChainId,
                Now.AddMinutes(20),
                ct));

        var chain = await store.GetChainAsync(
            graph.ChainA1.ChainId);

        chain.Should().NotBeNull();
        chain!.IsRevoked.Should().BeFalse();
    }

    [Fact]
    public async Task LogoutChainAsync_DoesNotAffectOtherChainsOrUsers()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var graph = await CreateGraphAsync(store);

        await store.ExecuteAsync(
            ct => store.LogoutChainAsync(
                graph.ChainA1.ChainId,
                Now.AddMinutes(20),
                ct));

        var chainA2 = await store.GetChainAsync(
            graph.ChainA2.ChainId);

        var sessionA21 = await store.GetSessionAsync(
            graph.SessionA21.SessionId);

        var chainB1 = await store.GetChainAsync(
            graph.ChainB1.ChainId);

        var sessionB11 = await store.GetSessionAsync(
            graph.SessionB11.SessionId);

        chainA2!.IsRevoked.Should().BeFalse();
        chainA2.ActiveSessionId.Should().Be(
            graph.SessionA21.SessionId);

        sessionA21!.IsRevoked.Should().BeFalse();

        chainB1!.IsRevoked.Should().BeFalse();
        chainB1.ActiveSessionId.Should().Be(
            graph.SessionB11.SessionId);

        sessionB11!.IsRevoked.Should().BeFalse();
    }

    // ============================================================
    // CHAIN CASCADE
    // ============================================================

    [Fact]
    public async Task RevokeChainCascadeAsync_RevokesChain()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var graph = await CreateGraphAsync(store);

        var at = Now.AddMinutes(20);

        await store.ExecuteAsync(
            ct => store.RevokeChainCascadeAsync(
                graph.ChainA1.ChainId,
                at,
                ct));

        var chain = await store.GetChainAsync(
            graph.ChainA1.ChainId);

        chain.Should().NotBeNull();
        chain!.IsRevoked.Should().BeTrue();
        chain.RevokedAt.Should().Be(at);
    }

    [Fact]
    public async Task RevokeChainCascadeAsync_RevokesAllSessionsInChain()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var graph = await CreateGraphAsync(store);

        await store.ExecuteAsync(
            ct => store.RevokeChainCascadeAsync(
                graph.ChainA1.ChainId,
                Now.AddMinutes(20),
                ct));

        var a11 = await store.GetSessionAsync(
            graph.SessionA11.SessionId);

        var a12 = await store.GetSessionAsync(
            graph.SessionA12.SessionId);

        a11!.IsRevoked.Should().BeTrue();
        a12!.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task RevokeChainCascadeAsync_DoesNotAffectOtherChainsOrUsers()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var graph = await CreateGraphAsync(store);

        await store.ExecuteAsync(
            ct => store.RevokeChainCascadeAsync(
                graph.ChainA1.ChainId,
                Now.AddMinutes(20),
                ct));

        var chainA2 = await store.GetChainAsync(
            graph.ChainA2.ChainId);

        var sessionA21 = await store.GetSessionAsync(
            graph.SessionA21.SessionId);

        var chainB1 = await store.GetChainAsync(
            graph.ChainB1.ChainId);

        var sessionB11 = await store.GetSessionAsync(
            graph.SessionB11.SessionId);

        chainA2!.IsRevoked.Should().BeFalse();
        sessionA21!.IsRevoked.Should().BeFalse();

        chainB1!.IsRevoked.Should().BeFalse();
        sessionB11!.IsRevoked.Should().BeFalse();
    }

    // ============================================================
    // REVOKE ALL SESSIONS
    // ============================================================

    [Fact]
    public async Task RevokeAllSessionsAsync_RevokesAllSessionsForUser()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var graph = await CreateGraphAsync(store);

        await store.ExecuteAsync(
            ct => store.RevokeAllSessionsAsync(
                graph.UserA,
                Now.AddMinutes(20),
                ct));

        var a11 = await store.GetSessionAsync(
            graph.SessionA11.SessionId);

        var a12 = await store.GetSessionAsync(
            graph.SessionA12.SessionId);

        var a21 = await store.GetSessionAsync(
            graph.SessionA21.SessionId);

        a11!.IsRevoked.Should().BeTrue();
        a12!.IsRevoked.Should().BeTrue();
        a21!.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task RevokeAllSessionsAsync_DetachesActiveSessions()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var graph = await CreateGraphAsync(store);

        await store.ExecuteAsync(
            ct => store.RevokeAllSessionsAsync(
                graph.UserA,
                Now.AddMinutes(20),
                ct));

        var chainA1 = await store.GetChainAsync(
            graph.ChainA1.ChainId);

        var chainA2 = await store.GetChainAsync(
            graph.ChainA2.ChainId);

        chainA1!.ActiveSessionId.Should().BeNull();
        chainA2!.ActiveSessionId.Should().BeNull();
    }

    [Fact]
    public async Task RevokeAllSessionsAsync_DoesNotAffectOtherUsers()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var graph = await CreateGraphAsync(store);

        await store.ExecuteAsync(
            ct => store.RevokeAllSessionsAsync(
                graph.UserA,
                Now.AddMinutes(20),
                ct));

        var chainB1 = await store.GetChainAsync(
            graph.ChainB1.ChainId);

        var sessionB11 = await store.GetSessionAsync(
            graph.SessionB11.SessionId);

        chainB1!.IsRevoked.Should().BeFalse();

        chainB1.ActiveSessionId
            .Should().Be(graph.SessionB11.SessionId);

        sessionB11!.IsRevoked.Should().BeFalse();
    }

    // ============================================================
    // REVOKE OTHER SESSIONS
    //
    // IMPORTANT:
    // RevokeOtherSessionsAsync keeps an entire CHAIN, not one
    // individual session.
    // ============================================================

    [Fact]
    public async Task RevokeOtherSessionsAsync_PreservesSessionsInKeptChain()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var graph = await CreateGraphAsync(store);

        await store.ExecuteAsync(
            ct => store.RevokeOtherSessionsAsync(
                graph.UserA,
                graph.ChainA1.ChainId,
                Now.AddMinutes(20),
                ct));

        var a11 = await store.GetSessionAsync(
            graph.SessionA11.SessionId);

        var a12 = await store.GetSessionAsync(
            graph.SessionA12.SessionId);

        a11!.IsRevoked.Should().BeFalse();
        a12!.IsRevoked.Should().BeFalse();

        var chainA1 = await store.GetChainAsync(
            graph.ChainA1.ChainId);

        chainA1!.ActiveSessionId
            .Should().Be(graph.SessionA12.SessionId);
    }

    [Fact]
    public async Task RevokeOtherSessionsAsync_RevokesSessionsOutsideKeptChain()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var graph = await CreateGraphAsync(store);

        await store.ExecuteAsync(
            ct => store.RevokeOtherSessionsAsync(
                graph.UserA,
                graph.ChainA1.ChainId,
                Now.AddMinutes(20),
                ct));

        var a21 = await store.GetSessionAsync(
            graph.SessionA21.SessionId);

        a21.Should().NotBeNull();
        a21!.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task RevokeOtherSessionsAsync_DetachesActiveSessionFromAffectedChains()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var graph = await CreateGraphAsync(store);

        await store.ExecuteAsync(
            ct => store.RevokeOtherSessionsAsync(
                graph.UserA,
                graph.ChainA1.ChainId,
                Now.AddMinutes(20),
                ct));

        var kept = await store.GetChainAsync(
            graph.ChainA1.ChainId);

        var affected = await store.GetChainAsync(
            graph.ChainA2.ChainId);

        kept!.ActiveSessionId
            .Should().Be(graph.SessionA12.SessionId);

        affected!.ActiveSessionId
            .Should().BeNull();
    }

    [Fact]
    public async Task RevokeOtherSessionsAsync_DoesNotAffectOtherUsers()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var graph = await CreateGraphAsync(store);

        await store.ExecuteAsync(
            ct => store.RevokeOtherSessionsAsync(
                graph.UserA,
                graph.ChainA1.ChainId,
                Now.AddMinutes(20),
                ct));

        var b11 = await store.GetSessionAsync(
            graph.SessionB11.SessionId);

        var chainB1 = await store.GetChainAsync(
            graph.ChainB1.ChainId);

        b11!.IsRevoked.Should().BeFalse();

        chainB1!.ActiveSessionId
            .Should().Be(graph.SessionB11.SessionId);
    }

    // ============================================================
    // REVOKE ALL CHAINS
    //
    // This intentionally does NOT assert that sessions are revoked.
    // RevokeChainCascadeAsync exists for cascade semantics.
    // ============================================================

    [Fact]
    public async Task RevokeAllChainsAsync_RevokesAllChainsForUser()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var graph = await CreateGraphAsync(store);

        await store.ExecuteAsync(
            ct => store.RevokeAllChainsAsync(
                graph.UserA,
                Now.AddMinutes(20),
                ct));

        var a1 = await store.GetChainAsync(
            graph.ChainA1.ChainId);

        var a2 = await store.GetChainAsync(
            graph.ChainA2.ChainId);

        a1!.IsRevoked.Should().BeTrue();
        a2!.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task RevokeAllChainsAsync_DoesNotAffectOtherUsers()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var graph = await CreateGraphAsync(store);

        await store.ExecuteAsync(
            ct => store.RevokeAllChainsAsync(
                graph.UserA,
                Now.AddMinutes(20),
                ct));

        var b1 = await store.GetChainAsync(
            graph.ChainB1.ChainId);

        b1!.IsRevoked.Should().BeFalse();
    }

    // ============================================================
    // REVOKE OTHER CHAINS
    // ============================================================

    [Fact]
    public async Task RevokeOtherChainsAsync_PreservesSpecifiedChain()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var graph = await CreateGraphAsync(store);

        await store.ExecuteAsync(
            ct => store.RevokeOtherChainsAsync(
                graph.UserA,
                graph.ChainA1.ChainId,
                Now.AddMinutes(20),
                ct));

        var a1 = await store.GetChainAsync(
            graph.ChainA1.ChainId);

        a1.Should().NotBeNull();
        a1!.IsRevoked.Should().BeFalse();
    }

    [Fact]
    public async Task RevokeOtherChainsAsync_RevokesOtherChains()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var graph = await CreateGraphAsync(store);

        await store.ExecuteAsync(
            ct => store.RevokeOtherChainsAsync(
                graph.UserA,
                graph.ChainA1.ChainId,
                Now.AddMinutes(20),
                ct));

        var a2 = await store.GetChainAsync(
            graph.ChainA2.ChainId);

        a2.Should().NotBeNull();
        a2!.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task RevokeOtherChainsAsync_DoesNotAffectOtherUsers()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var graph = await CreateGraphAsync(store);

        await store.ExecuteAsync(
            ct => store.RevokeOtherChainsAsync(
                graph.UserA,
                graph.ChainA1.ChainId,
                Now.AddMinutes(20),
                ct));

        var b1 = await store.GetChainAsync(
            graph.ChainB1.ChainId);

        b1!.IsRevoked.Should().BeFalse();
    }

    // ============================================================
    // ROOT CASCADE
    // ============================================================

    [Fact]
    public async Task RevokeRootCascadeAsync_RevokesRoot()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var graph = await CreateGraphAsync(store);

        var at = Now.AddMinutes(20);

        await store.ExecuteAsync(
            ct => store.RevokeRootCascadeAsync(
                graph.UserA,
                at,
                ct));

        var root = await store.GetRootByUserAsync(
            graph.UserA);

        root.Should().NotBeNull();
        root!.IsRevoked.Should().BeTrue();
        root.RevokedAt.Should().Be(at);
    }

    [Fact]
    public async Task RevokeRootCascadeAsync_IncreasesRootSecurityVersion()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var graph = await CreateGraphAsync(store);

        await store.ExecuteAsync(
            ct => store.RevokeRootCascadeAsync(
                graph.UserA,
                Now.AddMinutes(20),
                ct));

        var root = await store.GetRootByUserAsync(
            graph.UserA);

        root.Should().NotBeNull();

        root!.SecurityVersion.Should().Be(
            graph.RootA.SecurityVersion + 1);

        root.Version.Should().Be(
            graph.RootA.Version + 1);
    }

    [Fact]
    public async Task RevokeRootCascadeAsync_RevokesAllUserChains()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var graph = await CreateGraphAsync(store);

        await store.ExecuteAsync(
            ct => store.RevokeRootCascadeAsync(
                graph.UserA,
                Now.AddMinutes(20),
                ct));

        var a1 = await store.GetChainAsync(
            graph.ChainA1.ChainId);

        var a2 = await store.GetChainAsync(
            graph.ChainA2.ChainId);

        a1!.IsRevoked.Should().BeTrue();
        a2!.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task RevokeRootCascadeAsync_RevokesAllUserSessions()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var graph = await CreateGraphAsync(store);

        await store.ExecuteAsync(
            ct => store.RevokeRootCascadeAsync(
                graph.UserA,
                Now.AddMinutes(20),
                ct));

        var a11 = await store.GetSessionAsync(
            graph.SessionA11.SessionId);

        var a12 = await store.GetSessionAsync(
            graph.SessionA12.SessionId);

        var a21 = await store.GetSessionAsync(
            graph.SessionA21.SessionId);

        a11!.IsRevoked.Should().BeTrue();
        a12!.IsRevoked.Should().BeTrue();
        a21!.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task RevokeRootCascadeAsync_DoesNotAffectOtherUsers()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var graph = await CreateGraphAsync(store);

        await store.ExecuteAsync(
            ct => store.RevokeRootCascadeAsync(
                graph.UserA,
                Now.AddMinutes(20),
                ct));

        var rootB = await store.GetRootByUserAsync(
            graph.UserB);

        var chainB1 = await store.GetChainAsync(
            graph.ChainB1.ChainId);

        var sessionB11 = await store.GetSessionAsync(
            graph.SessionB11.SessionId);

        rootB.Should().NotBeNull();
        chainB1.Should().NotBeNull();
        sessionB11.Should().NotBeNull();

        rootB!.IsRevoked.Should().BeFalse();
        chainB1!.IsRevoked.Should().BeFalse();
        sessionB11!.IsRevoked.Should().BeFalse();

        chainB1.ActiveSessionId
            .Should().Be(graph.SessionB11.SessionId);
    }

    // ============================================================
    // QUERY / CASCADE CONSISTENCY
    // ============================================================

    [Fact]
    public async Task LogoutChainAsync_GetSessionsByChainReflectsRevocation()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var graph = await CreateGraphAsync(store);

        await store.ExecuteAsync(
            ct => store.LogoutChainAsync(
                graph.ChainA1.ChainId,
                Now.AddMinutes(20),
                ct));

        var sessions = await store.GetSessionsByChainAsync(
            graph.ChainA1.ChainId);

        sessions.Should().HaveCount(2);
        sessions.Should().OnlyContain(x => x.IsRevoked);
    }

    [Fact]
    public async Task RevokeRootCascadeAsync_GetChainsByUserIncludingHistoricalRootsReflectsRevocation()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var graph = await CreateGraphAsync(store);

        await store.ExecuteAsync(
            ct => store.RevokeRootCascadeAsync(
                graph.UserA,
                Now.AddMinutes(20),
                ct));

        var chains = await store.GetChainsByUserAsync(
            graph.UserA,
            includeHistoricalRoots: true);

        chains.Should().HaveCount(2);
        chains.Should().OnlyContain(x => x.IsRevoked);
    }
}
