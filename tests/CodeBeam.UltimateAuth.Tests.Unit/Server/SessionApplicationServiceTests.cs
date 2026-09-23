using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Services;
using CodeBeam.UltimateAuth.Sessions.InMemory;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using FluentAssertions;
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
            CreateChain(root, Now.AddMinutes(-30)));

        await store.CreateChainAsync(
            CreateChain(root, Now.AddMinutes(-20)));

        await store.CreateChainAsync(
            CreateChain(root, Now.AddMinutes(-10)));

        var result = await sut.GetUserChainsAsync(
            Context(user),
            user,
            new PageRequest
            {
                PageNumber = 2,
                PageSize = 2
            });

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

        await store.CreateChainAsync(actorChain);
        await store.CreateChainAsync(otherChain);

        var result = await sut.GetUserChainsAsync(
            Context(user, actorChain.ChainId),
            user,
            new PageRequest
            {
                PageNumber = 1,
                PageSize = 10
            });

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

        await store.CreateChainAsync(first);
        await store.CreateChainAsync(second);
        await store.CreateChainAsync(third);

        var result = await sut.GetUserChainsAsync(
            Context(user),
            user,
            new PageRequest
            {
                PageNumber = 1,
                PageSize = 10,
                SortBy = nameof(SessionChainSummary.ChainId),
                Descending = false
            });

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
            CreateChain(root, Now.AddMinutes(-30)));

        await store.CreateChainAsync(
            CreateChain(root, Now.AddMinutes(-20)));

        await store.CreateChainAsync(
            CreateChain(root, Now.AddMinutes(-10)));

        var result = await sut.GetUserChainsAsync(
            Context(user),
            user,
            new PageRequest
            {
                PageNumber = 1,
                PageSize = 10,
                SortBy = nameof(SessionChainSummary.ChainId),
                Descending = true
            });

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
            CreateChain(root, Now.AddMinutes(-5)));

        await store.CreateChainAsync(
            CreateChain(root, Now.AddMinutes(-30)));

        await store.CreateChainAsync(
            CreateChain(root, Now.AddMinutes(-15)));

        var result = await sut.GetUserChainsAsync(
            Context(user),
            user,
            new PageRequest
            {
                PageNumber = 1,
                PageSize = 10,
                SortBy = nameof(SessionChainSummary.CreatedAt),
                Descending = false
            });

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
            CreateChain(root, Now.AddMinutes(-5)));

        await store.CreateChainAsync(
            CreateChain(root, Now.AddMinutes(-30)));

        await store.CreateChainAsync(
            CreateChain(root, Now.AddMinutes(-15)));

        var result = await sut.GetUserChainsAsync(
            Context(user),
            user,
            new PageRequest
            {
                PageNumber = 1,
                PageSize = 10,
                SortBy = nameof(SessionChainSummary.CreatedAt),
                Descending = true
            });

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

        await store.CreateChainAsync(chain);

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

        await store.CreateChainAsync(chain);

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

        await store.CreateSessionAsync(older);
        await store.CreateSessionAsync(newer);

        var result = await sut.GetUserChainDetailAsync(
            Context(user),
            user,
            chain.ChainId);

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

        await store.CreateChainAsync(chain);
        await store.CreateSessionAsync(session);

        await sut.RevokeUserSessionAsync(
            Context(user),
            user,
            session.SessionId);

        var persisted =
            await store.GetSessionAsync(session.SessionId);

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

        await store.CreateChainAsync(chain);
        await store.CreateSessionAsync(session);

        var act = () => sut.RevokeUserSessionAsync(
            Context(caller),
            caller,
            session.SessionId);

        await act.Should()
            .ThrowAsync<UnauthorizedAccessException>();

        var persisted =
            await store.GetSessionAsync(session.SessionId);

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

        await store.CreateChainAsync(chain);

        var result = await sut.RevokeUserChainAsync(
            Context(user),
            user,
            chain.ChainId);

        result.CurrentChain.Should().BeFalse();
        result.RootRevoked.Should().BeFalse();

        var persisted =
            await store.GetChainAsync(chain.ChainId);

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

        await store.CreateChainAsync(chain);

        var result = await sut.RevokeUserChainAsync(
            Context(user, chain.ChainId),
            user,
            chain.ChainId);

        result.CurrentChain.Should().BeTrue();
        result.RootRevoked.Should().BeFalse();

        var persisted =
            await store.GetChainAsync(chain.ChainId);

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

        await store.CreateChainAsync(chain);

        var act = () => sut.RevokeUserChainAsync(
            Context(caller),
            caller,
            chain.ChainId);

        await act.Should()
            .ThrowAsync<UAuthValidationException>();

        var persisted =
            await store.GetChainAsync(chain.ChainId);

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

        await store.CreateChainAsync(keep);
        await store.CreateChainAsync(revoke1);
        await store.CreateChainAsync(revoke2);

        await sut.RevokeAllChainsAsync(
            Context(user),
            user,
            keep.ChainId);

        var persistedKeep =
            await store.GetChainAsync(keep.ChainId);

        var persistedRevoke1 =
            await store.GetChainAsync(revoke1.ChainId);

        var persistedRevoke2 =
            await store.GetChainAsync(revoke2.ChainId);

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

        await store.CreateChainAsync(first);
        await store.CreateChainAsync(second);

        await sut.RevokeAllChainsAsync(
            Context(user),
            user,
            exceptChainId: null);

        (await store.GetChainAsync(first.ChainId))!
            .IsRevoked.Should().BeTrue();

        (await store.GetChainAsync(second.ChainId))!
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

        await store.CreateChainAsync(chain);

        var result = await sut.LogoutDeviceAsync(
            Context(user, chain.ChainId),
            chain.ChainId);

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

        await store.CreateChainAsync(currentChain);
        await store.CreateChainAsync(otherChain);

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

        await store.CreateSessionAsync(currentSession);
        await store.CreateSessionAsync(otherSession);

        await sut.LogoutOtherDevicesAsync(
            Context(user, currentChain.ChainId),
            user,
            currentChain.ChainId);

        var persistedCurrent =
            await store.GetSessionAsync(currentSession.SessionId);

        var persistedOther =
            await store.GetSessionAsync(otherSession.SessionId);

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

        await store.CreateChainAsync(firstChain);
        await store.CreateChainAsync(secondChain);

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

        await store.CreateSessionAsync(firstSession);
        await store.CreateSessionAsync(secondSession);

        await sut.LogoutAllDevicesAsync(
            Context(user),
            user);

        (await store.GetSessionAsync(firstSession.SessionId))!
            .IsRevoked.Should().BeTrue();

        (await store.GetSessionAsync(secondSession.SessionId))!
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

        var firstChain = CreateChain(
            root,
            Now.AddMinutes(-20));

        var secondChain = CreateChain(
            root,
            Now.AddMinutes(-10));

        await store.CreateChainAsync(firstChain);
        await store.CreateChainAsync(secondChain);

        await sut.RevokeRootAsync(
            Context(user),
            user);

        var persistedRoot =
            await store.GetRootByUserAsync(user);

        persistedRoot.Should().NotBeNull();
        persistedRoot!.IsRevoked.Should().BeTrue();
        persistedRoot.RevokedAt.Should().Be(Now);

        (await store.GetChainAsync(firstChain.ChainId))!
            .IsRevoked.Should().BeTrue();

        (await store.GetChainAsync(secondChain.ChainId))!
            .IsRevoked.Should().BeTrue();
    }

    // ---------------------------------------------------------------------
    // Infrastructure / Helpers
    // ---------------------------------------------------------------------

    private static (
        SessionApplicationService Sut,
        ISessionStore Store)
        CreateSut()
    {
        var factory = new InMemorySessionStoreFactory();

        var store = factory.Create(TenantKey.Single);

        var sut = new SessionApplicationService(
            new PassThroughAccessOrchestrator(),
            factory,
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
            resource: "session",
            targetUserKey: user,
            resourceTenant: TenantKey.Single,
            action: "session.manage",
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