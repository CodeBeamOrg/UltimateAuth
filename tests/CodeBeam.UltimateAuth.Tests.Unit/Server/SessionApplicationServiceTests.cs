using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Services;
using CodeBeam.UltimateAuth.Sessions.InMemory;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using AwesomeAssertions;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Unit.Server;

public sealed class SessionApplicationServiceTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 9, 20, 12, 0, 0, TimeSpan.Zero);

    // ---------------------------------------------------------------------
    // GetUserChainsAsync
    // ---------------------------------------------------------------------

    [Fact]
    public async Task GetUserChainsAsync_ReturnsRequestedPage()
    {
        var (sut, store) = CreateSut();
        var user = UserKey.New();

        var root = await CreateRootAsync(store, user);

        await store.CreateChainAsync(
            CreateChain(root, Now.AddMinutes(-30)), ct: TestContext.Current.CancellationToken);

        await store.CreateChainAsync(
            CreateChain(root, Now.AddMinutes(-20)), ct: TestContext.Current.CancellationToken);

        await store.CreateChainAsync(
            CreateChain(root, Now.AddMinutes(-10)), ct: TestContext.Current.CancellationToken);

        var result = await sut.GetUserChainsAsync(
            Context(user),
            user,
            new PageRequest
            {
                PageNumber = 2,
                PageSize = 2
            }, ct: TestContext.Current.CancellationToken);

        result.TotalCount.Should().Be(3);
        result.PageNumber.Should().Be(2);
        result.PageSize.Should().Be(2);
        result.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetUserChainsAsync_MarksActorChainAsCurrentDevice()
    {
        var (sut, store) = CreateSut();
        var user = UserKey.New();

        var root = await CreateRootAsync(store, user);

        var actorChain = CreateChain(
            root,
            Now.AddMinutes(-10));

        var otherChain = CreateChain(
            root,
            Now.AddMinutes(-5));

        await store.CreateChainAsync(actorChain, ct: TestContext.Current.CancellationToken);
        await store.CreateChainAsync(otherChain, ct: TestContext.Current.CancellationToken);

        var result = await sut.GetUserChainsAsync(
            Context(user, actorChain.ChainId),
            user,
            new PageRequest
            {
                PageNumber = 1,
                PageSize = 10
            }, ct: TestContext.Current.CancellationToken);

        result.Items.Should().HaveCount(2);

        var actor = result.Items
            .Should()
            .ContainSingle(x => x.ChainId == actorChain.ChainId)
            .Subject;

        actor.IsCurrentDevice.Should().BeTrue();

        var other = result.Items
            .Should()
            .ContainSingle(x => x.ChainId == otherChain.ChainId)
            .Subject;

        other.IsCurrentDevice.Should().BeFalse();
    }

    [Fact]
    public async Task GetUserChainsAsync_SortByChainIdAscending_SortsByChainId()
    {
        var (sut, store) = CreateSut();
        var user = UserKey.New();

        var root = await CreateRootAsync(store, user);

        var first = CreateChain(root, Now.AddMinutes(-30));
        var second = CreateChain(root, Now.AddMinutes(-20));
        var third = CreateChain(root, Now.AddMinutes(-10));

        await store.CreateChainAsync(first, ct: TestContext.Current.CancellationToken);
        await store.CreateChainAsync(second, ct: TestContext.Current.CancellationToken);
        await store.CreateChainAsync(third, ct: TestContext.Current.CancellationToken);

        var result = await sut.GetUserChainsAsync(
            Context(user),
            user,
            new PageRequest
            {
                PageNumber = 1,
                PageSize = 10,
                SortBy = nameof(SessionChainSummary.ChainId),
                Descending = false
            }, ct: TestContext.Current.CancellationToken);

        result.Items
            .Select(x => x.ChainId.Value)
            .Should()
            .BeInAscendingOrder();
    }

    [Fact]
    public async Task GetUserChainsAsync_SortByChainIdDescending_SortsByChainId()
    {
        var (sut, store) = CreateSut();
        var user = UserKey.New();

        var root = await CreateRootAsync(store, user);

        await store.CreateChainAsync(
            CreateChain(root, Now.AddMinutes(-30)), ct: TestContext.Current.CancellationToken);

        await store.CreateChainAsync(
            CreateChain(root, Now.AddMinutes(-20)), ct: TestContext.Current.CancellationToken);

        await store.CreateChainAsync(
            CreateChain(root, Now.AddMinutes(-10)), ct: TestContext.Current.CancellationToken);

        var result = await sut.GetUserChainsAsync(
            Context(user),
            user,
            new PageRequest
            {
                PageNumber = 1,
                PageSize = 10,
                SortBy = nameof(SessionChainSummary.ChainId),
                Descending = true
            }, ct: TestContext.Current.CancellationToken);

        result.Items
            .Select(x => x.ChainId.Value)
            .Should()
            .BeInDescendingOrder();
    }

    [Fact]
    public async Task GetUserChainsAsync_SortByCreatedAtAscending_SortsByCreatedAt()
    {
        var (sut, store) = CreateSut();
        var user = UserKey.New();

        var root = await CreateRootAsync(store, user);

        // Deliberately insert in non-chronological order.
        await store.CreateChainAsync(
            CreateChain(root, Now.AddMinutes(-5)), ct: TestContext.Current.CancellationToken);

        await store.CreateChainAsync(
            CreateChain(root, Now.AddMinutes(-30)), ct: TestContext.Current.CancellationToken);

        await store.CreateChainAsync(
            CreateChain(root, Now.AddMinutes(-15)), ct: TestContext.Current.CancellationToken);

        var result = await sut.GetUserChainsAsync(
            Context(user),
            user,
            new PageRequest
            {
                PageNumber = 1,
                PageSize = 10,
                SortBy = nameof(SessionChainSummary.CreatedAt),
                Descending = false
            }, ct: TestContext.Current.CancellationToken);

        result.Items
            .Select(x => x.CreatedAt)
            .Should()
            .BeInAscendingOrder();
    }

    [Fact]
    public async Task GetUserChainsAsync_SortByCreatedAtDescending_SortsByCreatedAt()
    {
        var (sut, store) = CreateSut();
        var user = UserKey.New();

        var root = await CreateRootAsync(store, user);

        await store.CreateChainAsync(
            CreateChain(root, Now.AddMinutes(-5)), ct: TestContext.Current.CancellationToken);

        await store.CreateChainAsync(
            CreateChain(root, Now.AddMinutes(-30)), ct: TestContext.Current.CancellationToken);

        await store.CreateChainAsync(
            CreateChain(root, Now.AddMinutes(-15)), ct: TestContext.Current.CancellationToken);

        var result = await sut.GetUserChainsAsync(
            Context(user),
            user,
            new PageRequest
            {
                PageNumber = 1,
                PageSize = 10,
                SortBy = nameof(SessionChainSummary.CreatedAt),
                Descending = true
            }, ct: TestContext.Current.CancellationToken);

        result.Items
            .Select(x => x.CreatedAt)
            .Should()
            .BeInDescendingOrder();
    }

    // ---------------------------------------------------------------------
    // GetUserChainDetailAsync
    // ---------------------------------------------------------------------

    [Fact]
    public async Task GetUserChainDetailAsync_WhenChainDoesNotExist_ThrowsNotFound()
    {
        var (sut, _) = CreateSut();
        var user = UserKey.New();

        var act = () => sut.GetUserChainDetailAsync(
            Context(user),
            user,
            SessionChainId.New());

        await act.Should()
            .ThrowAsync<UAuthNotFoundException>();
    }

    [Fact]
    public async Task GetUserChainDetailAsync_WhenChainBelongsToDifferentUser_ThrowsValidation()
    {
        var (sut, store) = CreateSut();

        var requestedUser = UserKey.New();
        var owner = UserKey.New();

        var ownerRoot = await CreateRootAsync(store, owner);

        var chain = CreateChain(
            ownerRoot,
            Now.AddMinutes(-10));

        await store.CreateChainAsync(chain, ct: TestContext.Current.CancellationToken);

        var act = () => sut.GetUserChainDetailAsync(
            Context(requestedUser),
            requestedUser,
            chain.ChainId);

        await act.Should()
            .ThrowAsync<UAuthValidationException>();
    }

    [Fact]
    public async Task GetUserChainDetailAsync_ReturnsSessionsNewestFirst()
    {
        var (sut, store) = CreateSut();
        var user = UserKey.New();

        var root = await CreateRootAsync(store, user);

        var chain = CreateChain(
            root,
            Now.AddHours(-1));

        await store.CreateChainAsync(chain, ct: TestContext.Current.CancellationToken);

        var older = CreateSession(
            user,
            chain.ChainId,
            Now.AddMinutes(-40),
            "older-session");

        var newer = CreateSession(
            user,
            chain.ChainId,
            Now.AddMinutes(-10),
            "newer-session");

        await store.CreateSessionAsync(older, ct: TestContext.Current.CancellationToken);
        await store.CreateSessionAsync(newer, ct: TestContext.Current.CancellationToken);

        var result = await sut.GetUserChainDetailAsync(
            Context(user),
            user,
            chain.ChainId, ct: TestContext.Current.CancellationToken);

        result.ChainId.Should().Be(chain.ChainId);

        result.Sessions
            .Select(x => x.SessionId)
            .Should()
            .Equal(
                newer.SessionId,
                older.SessionId);
    }

    // ---------------------------------------------------------------------
    // RevokeUserSessionAsync
    // ---------------------------------------------------------------------

    [Fact]
    public async Task RevokeUserSessionAsync_WhenSessionBelongsToUser_RevokesSession()
    {
        var (sut, store) = CreateSut();
        var user = UserKey.New();

        var root = await CreateRootAsync(store, user);

        var chain = CreateChain(
            root,
            Now.AddHours(-1));

        var session = CreateSession(
            user,
            chain.ChainId,
            Now.AddMinutes(-30),
            "session-to-revoke");

        await store.CreateChainAsync(chain, ct: TestContext.Current.CancellationToken);
        await store.CreateSessionAsync(session, ct: TestContext.Current.CancellationToken);

        await sut.RevokeUserSessionAsync(
            Context(user),
            user,
            session.SessionId, ct: TestContext.Current.CancellationToken);

        var persisted =
            await store.GetSessionAsync(session.SessionId, ct: TestContext.Current.CancellationToken);

        persisted.Should().NotBeNull();
        persisted!.IsRevoked.Should().BeTrue();
        persisted.RevokedAt.Should().Be(Now);
    }

    [Fact]
    public async Task RevokeUserSessionAsync_WhenSessionBelongsToDifferentUser_DoesNotRevoke()
    {
        var (sut, store) = CreateSut();

        var caller = UserKey.New();
        var owner = UserKey.New();

        var ownerRoot = await CreateRootAsync(store, owner);

        var chain = CreateChain(
            ownerRoot,
            Now.AddHours(-1));

        var session = CreateSession(
            owner,
            chain.ChainId,
            Now.AddMinutes(-30),
            "foreign-session");

        await store.CreateChainAsync(chain, ct: TestContext.Current.CancellationToken);
        await store.CreateSessionAsync(session, ct: TestContext.Current.CancellationToken);

        var act = () => sut.RevokeUserSessionAsync(
            Context(caller),
            caller,
            session.SessionId);

        await act.Should().ThrowAsync<UAuthNotFoundException>();

        var persisted = await store.GetSessionAsync(session.SessionId, ct: TestContext.Current.CancellationToken);
        persisted.Should().NotBeNull();
        persisted!.IsRevoked.Should().BeFalse();
    }

    // ---------------------------------------------------------------------
    // RevokeUserChainAsync
    // ---------------------------------------------------------------------

    [Fact]
    public async Task RevokeUserChainAsync_WhenChainBelongsToUser_RevokesChain()
    {
        var (sut, store) = CreateSut();
        var user = UserKey.New();

        var root = await CreateRootAsync(store, user);

        var chain = CreateChain(
            root,
            Now.AddHours(-1));

        await store.CreateChainAsync(chain, ct: TestContext.Current.CancellationToken);

        var result = await sut.RevokeUserChainAsync(
            Context(user),
            user,
            chain.ChainId, ct: TestContext.Current.CancellationToken);

        result.CurrentChain.Should().BeFalse();
        result.RootRevoked.Should().BeFalse();

        var persisted =
            await store.GetChainAsync(chain.ChainId, ct: TestContext.Current.CancellationToken);

        persisted.Should().NotBeNull();
        persisted!.IsRevoked.Should().BeTrue();
        persisted.RevokedAt.Should().Be(Now);
    }

    [Fact]
    public async Task RevokeUserChainAsync_WhenRevokingActorChain_ReportsCurrentChain()
    {
        var (sut, store) = CreateSut();
        var user = UserKey.New();

        var root = await CreateRootAsync(store, user);

        var chain = CreateChain(
            root,
            Now.AddHours(-1));

        await store.CreateChainAsync(chain, ct: TestContext.Current.CancellationToken);

        var result = await sut.RevokeUserChainAsync(
            Context(user, chain.ChainId),
            user,
            chain.ChainId, ct: TestContext.Current.CancellationToken);

        result.CurrentChain.Should().BeTrue();
        result.RootRevoked.Should().BeFalse();

        var persisted =
            await store.GetChainAsync(chain.ChainId, ct: TestContext.Current.CancellationToken);

        persisted.Should().NotBeNull();
        persisted!.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task RevokeUserChainAsync_WhenChainDoesNotExist_ThrowsNotFound()
    {
        var (sut, _) = CreateSut();
        var user = UserKey.New();

        var act = () => sut.RevokeUserChainAsync(
            Context(user),
            user,
            SessionChainId.New());

        await act.Should().ThrowAsync<UAuthNotFoundException>();
    }

    [Fact]
    public async Task RevokeUserChainAsync_WhenChainBelongsToDifferentUser_MustNotRevokeChain()
    {
        var (sut, store) = CreateSut();

        var caller = UserKey.New();
        var owner = UserKey.New();

        var ownerRoot = await CreateRootAsync(store, owner);

        var chain = CreateChain(
            ownerRoot,
            Now.AddHours(-1));

        await store.CreateChainAsync(chain, ct: TestContext.Current.CancellationToken);

        var act = () => sut.RevokeUserChainAsync(
            Context(caller),
            caller,
            chain.ChainId);

        await act.Should()
            .ThrowAsync<UAuthValidationException>();

        var persisted =
            await store.GetChainAsync(chain.ChainId, ct: TestContext.Current.CancellationToken);

        persisted.Should().NotBeNull();
        persisted!.IsRevoked.Should().BeFalse();
    }

    // ---------------------------------------------------------------------
    // RevokeAllChainsAsync
    // ---------------------------------------------------------------------

    [Fact]
    public async Task RevokeAllChainsAsync_WithExceptChain_LeavesExceptedChainActive()
    {
        var (sut, store) = CreateSut();
        var user = UserKey.New();

        var root = await CreateRootAsync(store, user);

        var keep = CreateChain(
            root,
            Now.AddMinutes(-30));

        var revoke1 = CreateChain(
            root,
            Now.AddMinutes(-20));

        var revoke2 = CreateChain(
            root,
            Now.AddMinutes(-10));

        await store.CreateChainAsync(keep, ct: TestContext.Current.CancellationToken);
        await store.CreateChainAsync(revoke1, ct: TestContext.Current.CancellationToken);
        await store.CreateChainAsync(revoke2, ct: TestContext.Current.CancellationToken);

        await sut.RevokeAllChainsAsync(
            Context(user),
            user,
            keep.ChainId, ct: TestContext.Current.CancellationToken);

        var persistedKeep =
            await store.GetChainAsync(keep.ChainId, ct: TestContext.Current.CancellationToken);

        var persistedRevoke1 =
            await store.GetChainAsync(revoke1.ChainId, ct: TestContext.Current.CancellationToken);

        var persistedRevoke2 =
            await store.GetChainAsync(revoke2.ChainId, ct: TestContext.Current.CancellationToken);

        persistedKeep!.IsRevoked.Should().BeFalse();
        persistedRevoke1!.IsRevoked.Should().BeTrue();
        persistedRevoke2!.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task RevokeAllChainsAsync_WithoutExceptChain_RevokesAllChains()
    {
        var (sut, store) = CreateSut();
        var user = UserKey.New();

        var root = await CreateRootAsync(store, user);

        var first = CreateChain(
            root,
            Now.AddMinutes(-20));

        var second = CreateChain(
            root,
            Now.AddMinutes(-10));

        await store.CreateChainAsync(first, ct: TestContext.Current.CancellationToken);
        await store.CreateChainAsync(second, ct: TestContext.Current.CancellationToken);

        await sut.RevokeAllChainsAsync(
            Context(user),
            user,
            exceptChainId: null, ct: TestContext.Current.CancellationToken);

        (await store.GetChainAsync(first.ChainId, ct: TestContext.Current.CancellationToken))!
            .IsRevoked.Should().BeTrue();

        (await store.GetChainAsync(second.ChainId, ct: TestContext.Current.CancellationToken))!
            .IsRevoked.Should().BeTrue();
    }

    // ---------------------------------------------------------------------
    // Logout
    // ---------------------------------------------------------------------

    [Fact]
    public async Task LogoutDeviceAsync_WhenActorChainMatches_ReportsCurrentChain()
    {
        var (sut, store) = CreateSut();
        var user = UserKey.New();

        var root = await CreateRootAsync(store, user);

        var chain = CreateChain(
            root,
            Now.AddHours(-1));

        await store.CreateChainAsync(chain, ct: TestContext.Current.CancellationToken);

        var result = await sut.LogoutDeviceAsync(
            Context(user, chain.ChainId),
            chain.ChainId, ct: TestContext.Current.CancellationToken);

        result.CurrentChain.Should().BeTrue();
        result.RootRevoked.Should().BeFalse();
    }

    [Fact]
    public async Task LogoutOtherDevicesAsync_RevokesSessionsOnOtherChains()
    {
        var (sut, store) = CreateSut();
        var user = UserKey.New();

        var root = await CreateRootAsync(store, user);

        var currentChain = CreateChain(
            root,
            Now.AddHours(-2));

        var otherChain = CreateChain(
            root,
            Now.AddHours(-1));

        await store.CreateChainAsync(currentChain, ct: TestContext.Current.CancellationToken);
        await store.CreateChainAsync(otherChain, ct: TestContext.Current.CancellationToken);

        var currentSession = CreateSession(
            user,
            currentChain.ChainId,
            Now.AddMinutes(-30),
            "current-session");

        var otherSession = CreateSession(
            user,
            otherChain.ChainId,
            Now.AddMinutes(-20),
            "other-session");

        await store.CreateSessionAsync(currentSession, ct: TestContext.Current.CancellationToken);
        await store.CreateSessionAsync(otherSession, ct: TestContext.Current.CancellationToken);

        await sut.LogoutOtherDevicesAsync(
            Context(user, currentChain.ChainId),
            user,
            currentChain.ChainId, ct: TestContext.Current.CancellationToken);

        var persistedCurrent =
            await store.GetSessionAsync(currentSession.SessionId, ct: TestContext.Current.CancellationToken);

        var persistedOther =
            await store.GetSessionAsync(otherSession.SessionId, ct: TestContext.Current.CancellationToken);

        persistedCurrent!.IsRevoked.Should().BeFalse();
        persistedOther!.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task LogoutAllDevicesAsync_RevokesAllUserSessions()
    {
        var (sut, store) = CreateSut();
        var user = UserKey.New();

        var root = await CreateRootAsync(store, user);

        var firstChain = CreateChain(
            root,
            Now.AddHours(-2));

        var secondChain = CreateChain(
            root,
            Now.AddHours(-1));

        await store.CreateChainAsync(firstChain, ct: TestContext.Current.CancellationToken);
        await store.CreateChainAsync(secondChain, ct: TestContext.Current.CancellationToken);

        var firstSession = CreateSession(
            user,
            firstChain.ChainId,
            Now.AddMinutes(-30),
            "first-session");

        var secondSession = CreateSession(
            user,
            secondChain.ChainId,
            Now.AddMinutes(-20),
            "second-session");

        await store.CreateSessionAsync(firstSession, ct: TestContext.Current.CancellationToken);
        await store.CreateSessionAsync(secondSession, ct: TestContext.Current.CancellationToken);

        await sut.LogoutAllDevicesAsync(
            Context(user),
            user, ct: TestContext.Current.CancellationToken);

        (await store.GetSessionAsync(firstSession.SessionId, ct: TestContext.Current.CancellationToken))!
            .IsRevoked.Should().BeTrue();

        (await store.GetSessionAsync(secondSession.SessionId, ct: TestContext.Current.CancellationToken))!
            .IsRevoked.Should().BeTrue();
    }

    // ---------------------------------------------------------------------
    // Root
    // ---------------------------------------------------------------------

    [Fact]
    public async Task RevokeRootAsync_RevokesRootAndItsChains()
    {
        var (sut, store) = CreateSut();
        var user = UserKey.New();

        var root = await CreateRootAsync(store, user);

        var firstChain = CreateChain(root, Now.AddMinutes(-20));

        var secondChain = CreateChain(root, Now.AddMinutes(-10));

        await store.CreateChainAsync(firstChain, ct: TestContext.Current.CancellationToken);
        await store.CreateChainAsync(secondChain, ct: TestContext.Current.CancellationToken);

        await sut.RevokeRootAsync(Context(user), user, ct: TestContext.Current.CancellationToken);

        var activeRoot = await store.GetActiveRootByUserAsync(user, ct: TestContext.Current.CancellationToken);

        activeRoot.Should().BeNull();

        var persistedRoot = await store.GetRootByIdAsync(root.RootId, ct: TestContext.Current.CancellationToken);

        persistedRoot.Should().NotBeNull();
        persistedRoot!.IsRevoked.Should().BeTrue();
        persistedRoot.RevokedAt.Should().Be(Now);

        (await store.GetChainAsync(firstChain.ChainId, ct: TestContext.Current.CancellationToken))!.IsRevoked.Should().BeTrue();
        (await store.GetChainAsync(secondChain.ChainId, ct: TestContext.Current.CancellationToken))!.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task GetUserChainsAsync_ReturnsTotalSessionCountPerChain()
    {
        var (sut, store) = CreateSut();
        var user = UserKey.New();
        var root = await CreateRootAsync(store, user);

        var firstChain = CreateChain(root, Now.AddMinutes(-30));
        var secondChain = CreateChain(root, Now.AddMinutes(-20));

        await store.CreateChainAsync(firstChain, ct: TestContext.Current.CancellationToken);
        await store.CreateChainAsync(secondChain, ct: TestContext.Current.CancellationToken);

        var firstSession = CreateSession(
            user, firstChain.ChainId, Now.AddMinutes(-25), "first-1");

        var rotatedSession = CreateSession(
            user, firstChain.ChainId, Now.AddMinutes(-15), "first-2");

        var secondSession = CreateSession(
            user, secondChain.ChainId, Now.AddMinutes(-10), "second-1");

        await store.CreateSessionAsync(firstSession, ct: TestContext.Current.CancellationToken);
        await store.CreateSessionAsync(rotatedSession, ct: TestContext.Current.CancellationToken);
        await store.CreateSessionAsync(secondSession, ct: TestContext.Current.CancellationToken);

        var result = await sut.GetUserChainsAsync(
            Context(user),
            user,
            new PageRequest { PageNumber = 1, PageSize = 10 }, ct: TestContext.Current.CancellationToken);

        result.Items.Single(x => x.ChainId == firstChain.ChainId)
            .SessionCount.Should().Be(2);

        result.Items.Single(x => x.ChainId == secondChain.ChainId)
            .SessionCount.Should().Be(1);
    }

    [Fact]
    public async Task GetUserChainsAsync_RevokedSessionsRemainCounted()
    {
        var (sut, store) = CreateSut();
        var user = UserKey.New();
        var root = await CreateRootAsync(store, user);

        var chain = CreateChain(root, Now.AddMinutes(-30));
        await store.CreateChainAsync(chain, ct: TestContext.Current.CancellationToken);

        var first = CreateSession(user, chain.ChainId, Now.AddMinutes(-20), "revoked-1");

        var second = CreateSession(user, chain.ChainId, Now.AddMinutes(-10), "revoked-2");

        await store.CreateSessionAsync(first, ct: TestContext.Current.CancellationToken);
        await store.CreateSessionAsync(second, ct: TestContext.Current.CancellationToken);

        await store.RevokeChainCascadeAsync(chain.ChainId, Now, ct: TestContext.Current.CancellationToken);

        var result = await sut.GetUserChainsAsync(Context(user), user, new PageRequest { PageNumber = 1, PageSize = 10 }, ct: TestContext.Current.CancellationToken);

        result.Items.Single(x => x.ChainId == chain.ChainId).SessionCount.Should().Be(2);
    }

    // ---------------------------------------------------------------------
    // Infrastructure / Helpers
    // ---------------------------------------------------------------------

    private static (SessionApplicationService Sut, ISessionStore Store) CreateSut()
    {
        var factory = new InMemorySessionStoreFactory();

        var store = factory.Create(TenantKey.Single);

        var sut = new SessionApplicationService(
            new PassThroughAccessOrchestrator(),
            factory,
            new UAuthPaginationOptions(),
            new TestClock(Now));

        return (sut, store);
    }

    private static async Task<UAuthSessionRoot> CreateRootAsync(
        ISessionStore store,
        UserKey user)
    {
        var root = UAuthSessionRoot.Create(
            TenantKey.Single,
            user,
            Now.AddHours(-2));

        await store.CreateRootAsync(root);

        return root;
    }

    private static UAuthSessionChain CreateChain(
        UAuthSessionRoot root,
        DateTimeOffset createdAt)
    {
        return UAuthSessionChain.Create(
            SessionChainId.New(),
            root.RootId,
            root.Tenant,
            root.UserKey,
            createdAt,
            expiresAt: Now.AddDays(30),
            TestDevice.Default(),
            ClaimsSnapshot.Empty,
            securityVersion: root.SecurityVersion);
    }

    private static UAuthSession CreateSession(
        UserKey user,
        SessionChainId chainId,
        DateTimeOffset createdAt,
        string id)
    {
        return UAuthSession.Create(
            TestIds.Session(id),
            TenantKey.Single,
            user,
            chainId,
            createdAt,
            createdAt.AddHours(8),
            securityVersion: 0,
            TestDevice.Default(),
            ClaimsSnapshot.Empty,
            SessionMetadata.Empty);
    }

    private static AccessContext Context(
        UserKey user,
        SessionChainId? actorChainId = null)
    {
        return new AccessContext(
            actorUserKey: user,
            actorTenant: TenantKey.Single,
            isAuthenticated: true,
            isSystemActor: false,
            actorChainId: actorChainId,
            actorRoles: Array.Empty<AccessActorRole>(),
            resource: "session",
            targetUserKey: user,
            resourceTenant: TenantKey.Single,
            action: "session.manage",
            clientProfile: UAuthClientProfile.NotSpecified,
            attributes: EmptyAttributes.Instance);
    }

    private sealed class PassThroughAccessOrchestrator : IAccessOrchestrator
    {
        public Task ExecuteAsync(
            AccessContext context,
            IAccessCommand command,
            CancellationToken ct = default)
        {
            return command.ExecuteAsync(ct);
        }

        public Task<TResult> ExecuteAsync<TResult>(
            AccessContext context,
            IAccessCommand<TResult> command,
            CancellationToken ct = default)
        {
            return command.ExecuteAsync(ct);
        }
    }
}