using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Server.Flows;
using CodeBeam.UltimateAuth.Sessions.InMemory;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using FluentAssertions;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public sealed class SessionTouchServiceTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task RefreshAsync_WhenValidationIsInvalid_ReturnsReauthRequired()
    {
        var factory = new Mock<ISessionStoreFactory>(MockBehavior.Strict);
        var sut = new SessionTouchService(factory.Object);

        var result = await sut.RefreshAsync(
            SessionValidationResult.Invalid(SessionState.Revoked),
            Policy(),
            SessionTouchMode.IfNeeded,
            Now);

        result.IsSuccess.Should().BeFalse();
        result.RequiresReauth.Should().BeTrue();

        factory.Verify(
            x => x.Create(It.IsAny<TenantKey>()),
            Times.Never);
    }

    [Fact]
    public async Task RefreshAsync_WhenChainIdIsMissing_ReturnsReauthRequired()
    {
        var factory = new Mock<ISessionStoreFactory>(MockBehavior.Strict);
        var sut = new SessionTouchService(factory.Object);

        var validation = SessionValidationResult.Invalid(
            SessionState.Active,
            userKey: TestUsers.User,
            sessionId: TestIds.Session("active-session"));

        var result = await sut.RefreshAsync(
            validation,
            Policy(),
            SessionTouchMode.IfNeeded,
            Now);

        result.IsSuccess.Should().BeFalse();
        result.RequiresReauth.Should().BeTrue();

        factory.Verify(
            x => x.Create(It.IsAny<TenantKey>()),
            Times.Never);
    }

    [Fact]
    public async Task RefreshAsync_WhenChainDoesNotExist_ReturnsReauthRequired()
    {
        var factory = new InMemorySessionStoreFactory();
        var validation = CreateActiveValidation();
        var sut = new SessionTouchService(factory);

        var result = await sut.RefreshAsync(
            validation,
            Policy(),
            SessionTouchMode.IfNeeded,
            Now);

        result.IsSuccess.Should().BeFalse();
        result.RequiresReauth.Should().BeTrue();
        result.DidTouch.Should().BeFalse();
    }

    [Fact]
    public async Task RefreshAsync_WhenSessionDoesNotExist_ReturnsReauthRequired()
    {
        var factory = new InMemorySessionStoreFactory();
        var validation = CreateActiveValidation();
        var store = factory.Create(validation.Tenant);

        var chain = CreateChain(validation, Now.AddMinutes(-2));
        await store.CreateChainAsync(chain);

        var sut = new SessionTouchService(factory);

        var result = await sut.RefreshAsync(
            validation,
            Policy(),
            SessionTouchMode.IfNeeded,
            Now);

        result.IsSuccess.Should().BeFalse();
        result.RequiresReauth.Should().BeTrue();
        result.DidTouch.Should().BeFalse();
    }

    [Fact]
    public async Task RefreshAsync_WhenChainIsRevoked_ReturnsReauthRequired()
    {
        var (factory, validation, chain, _) =
            await CreateValidSessionAsync(Now.AddMinutes(-2));

        var store = factory.Create(validation.Tenant);
        var revoked = chain.Revoke(Now.AddMinutes(-1));
        await store.SaveChainAsync(revoked, chain.Version);

        var sut = new SessionTouchService(factory);

        var result = await sut.RefreshAsync(
            validation,
            Policy(),
            SessionTouchMode.IfNeeded,
            Now);

        result.IsSuccess.Should().BeFalse();
        result.RequiresReauth.Should().BeTrue();
        result.DidTouch.Should().BeFalse();

        var persisted = await store.GetChainAsync(chain.ChainId);

        persisted!.IsRevoked.Should().BeTrue();
        persisted.TouchCount.Should().Be(revoked.TouchCount);
        persisted.Version.Should().Be(revoked.Version);
    }

    [Fact]
    public async Task RefreshAsync_WhenSessionIsRevoked_ReturnsReauthRequired()
    {
        var (factory, validation, _, session) =
            await CreateValidSessionAsync(Now.AddMinutes(-2));

        var store = factory.Create(validation.Tenant);
        var revoked = session.Revoke(Now.AddMinutes(-1));

        await store.SaveSessionAsync(revoked, session.Version);

        var result = await new SessionTouchService(factory).RefreshAsync(
            validation, Policy(), SessionTouchMode.IfNeeded, Now);

        result.IsSuccess.Should().BeFalse();
        result.RequiresReauth.Should().BeTrue();
        result.DidTouch.Should().BeFalse();
    }

    [Fact]
    public async Task RefreshAsync_WhenSessionIsExpired_ReturnsReauthRequired()
    {
        var (factory, validation, _, _) =
            await CreateValidSessionAsync(
                Now.AddMinutes(-2),
                sessionExpiresAt: Now);

        var result = await new SessionTouchService(factory).RefreshAsync(
            validation, Policy(), SessionTouchMode.Force, Now);

        result.IsSuccess.Should().BeFalse();
        result.RequiresReauth.Should().BeTrue();
        result.DidTouch.Should().BeFalse();
    }

    [Fact]
    public async Task RefreshAsync_WhenChainIdleTimeoutElapsed_ReturnsReauthRequired()
    {
        var (factory, validation, _, _) =
            await CreateValidSessionAsync(Now.AddMinutes(-11));

        var result = await new SessionTouchService(factory).RefreshAsync(
            validation,
            Policy(idleTimeout: TimeSpan.FromMinutes(10)),
            SessionTouchMode.Force,
            Now);

        result.IsSuccess.Should().BeFalse();
        result.RequiresReauth.Should().BeTrue();
        result.DidTouch.Should().BeFalse();
    }

    [Fact]
    public async Task RefreshAsync_WhenTouchIntervalHasNotElapsed_ReturnsSuccessWithoutTouch()
    {
        var (factory, validation, chain, session) =
            await CreateValidSessionAsync(Now.AddMinutes(-4));

        var result = await new SessionTouchService(factory).RefreshAsync(
            validation, Policy(), SessionTouchMode.IfNeeded, Now);

        result.IsSuccess.Should().BeTrue();
        result.RequiresReauth.Should().BeFalse();
        result.DidTouch.Should().BeFalse();

        var store = factory.Create(validation.Tenant);
        var persistedChain = await store.GetChainAsync(chain.ChainId);
        var persistedSession = await store.GetSessionAsync(session.SessionId);

        persistedChain!.LastSeenAt.Should().Be(chain.LastSeenAt);
        persistedChain.TouchCount.Should().Be(0);
        persistedChain.Version.Should().Be(chain.Version);

        persistedSession!.ExpiresAt.Should().Be(session.ExpiresAt);
        persistedSession.Version.Should().Be(session.Version);
    }

    [Fact]
    public async Task RefreshAsync_WhenTouchIntervalHasElapsed_TouchesAndSavesChain()
    {
        var (factory, validation, chain, _) =
            await CreateValidSessionAsync(Now.AddMinutes(-6));

        var result = await new SessionTouchService(factory).RefreshAsync(
            validation, Policy(), SessionTouchMode.IfNeeded, Now);

        result.IsSuccess.Should().BeTrue();
        result.DidTouch.Should().BeTrue();
        result.SessionId.Should().Be(validation.SessionId);

        var persisted = await factory.Create(validation.Tenant)
            .GetChainAsync(chain.ChainId);

        persisted!.LastSeenAt.Should().Be(Now);
        persisted.TouchCount.Should().Be(1);
        persisted.Version.Should().Be(chain.Version + 1);
    }

    [Fact]
    public async Task RefreshAsync_WhenTouchIntervalExactlyElapsed_TouchesChain()
    {
        var (factory, validation, chain, _) =
            await CreateValidSessionAsync(Now.AddMinutes(-5));

        var result = await new SessionTouchService(factory).RefreshAsync(
            validation, Policy(), SessionTouchMode.IfNeeded, Now);

        result.IsSuccess.Should().BeTrue();
        result.DidTouch.Should().BeTrue();

        var persisted = await factory.Create(validation.Tenant)
            .GetChainAsync(chain.ChainId);

        persisted!.LastSeenAt.Should().Be(Now);
        persisted.TouchCount.Should().Be(1);
    }

    [Fact]
    public async Task RefreshAsync_WhenTouchIntervalIsNull_TouchesEveryRefresh()
    {
        var (factory, validation, chain, _) =
            await CreateValidSessionAsync(Now.AddMinutes(-1));

        var policy = PolicyWithoutTouchInterval();

        var result = await new SessionTouchService(factory).RefreshAsync(
            validation, policy, SessionTouchMode.IfNeeded, Now);

        result.IsSuccess.Should().BeTrue();
        result.DidTouch.Should().BeTrue();

        var persisted = await factory.Create(validation.Tenant)
            .GetChainAsync(chain.ChainId);

        persisted!.LastSeenAt.Should().Be(Now);
        persisted.TouchCount.Should().Be(1);
    }

    [Fact]
    public async Task RefreshAsync_WhenForceIsUsed_TouchesBeforeInterval()
    {
        var (factory, validation, chain, _) =
            await CreateValidSessionAsync(Now.AddMinutes(-1));

        var result = await new SessionTouchService(factory).RefreshAsync(
            validation, Policy(), SessionTouchMode.Force, Now);

        result.IsSuccess.Should().BeTrue();
        result.DidTouch.Should().BeTrue();

        var persisted = await factory.Create(validation.Tenant)
            .GetChainAsync(chain.ChainId);

        persisted!.LastSeenAt.Should().Be(Now);
    }

    [Fact]
    public async Task RefreshAsync_WhenSlidingEnabled_ExtendsSessionExpiration()
    {
        var originalExpiry = Now.AddMinutes(10);

        var (factory, validation, chain, session) =
            await CreateValidSessionAsync(
                Now.AddMinutes(-6),
                sessionExpiresAt: originalExpiry);

        var result = await new SessionTouchService(factory).RefreshAsync(
            validation,
            Policy(
                slidingExpiration: true,
                lifetime: TimeSpan.FromHours(1)),
            SessionTouchMode.IfNeeded,
            Now);

        result.IsSuccess.Should().BeTrue();
        result.DidTouch.Should().BeTrue();

        var store = factory.Create(validation.Tenant);
        var persistedSession = await store.GetSessionAsync(session.SessionId);
        var persistedChain = await store.GetChainAsync(chain.ChainId);

        persistedSession!.ExpiresAt.Should().Be(Now.AddHours(1));
        persistedSession.Version.Should().Be(session.Version + 1);
        persistedChain!.LastSeenAt.Should().Be(Now);
    }

    [Fact]
    public async Task RefreshAsync_WhenSlidingDisabled_DoesNotExtendSession()
    {
        var originalExpiry = Now.AddMinutes(10);

        var (factory, validation, _, session) =
            await CreateValidSessionAsync(
                Now.AddMinutes(-6),
                sessionExpiresAt: originalExpiry);

        var result = await new SessionTouchService(factory).RefreshAsync(
            validation,
            Policy(slidingExpiration: false),
            SessionTouchMode.IfNeeded,
            Now);

        result.IsSuccess.Should().BeTrue();
        result.DidTouch.Should().BeTrue();

        var persisted = await factory.Create(validation.Tenant)
            .GetSessionAsync(session.SessionId);

        persisted!.ExpiresAt.Should().Be(originalExpiry);
        persisted.Version.Should().Be(session.Version);
    }

    private static SessionTouchPolicy Policy(TimeSpan? idleTimeout = null, TimeSpan? lifetime = null, bool slidingExpiration = false)
    {
        return new SessionTouchPolicy
        {
            TouchInterval = TimeSpan.FromMinutes(5),
            IdleTimeout = idleTimeout ?? TimeSpan.FromMinutes(30),
            Lifetime = lifetime ?? TimeSpan.FromHours(1),
            SlidingExpiration = slidingExpiration
        };
    }

    private static SessionTouchPolicy PolicyWithoutTouchInterval()
    {
        return new SessionTouchPolicy
        {
            TouchInterval = null,
            IdleTimeout = TimeSpan.FromMinutes(30),
            Lifetime = TimeSpan.FromHours(1),
            SlidingExpiration = false
        };
    }

    private static SessionValidationResult CreateActiveValidation()
    {
        return SessionValidationResult.Active(
            tenant: TenantKey.Single,
            userKey: TestUsers.User,
            sessionId: TestIds.Session("session-touch-service"),
            chainId: SessionChainId.New(),
            rootId: SessionRootId.New(),
            claims: ClaimsSnapshot.Empty,
            authenticatedAt: Now.AddHours(-1),
            boundDeviceId: TestDevice.Default().DeviceId);
    }

    private static UAuthSessionChain CreateChain(
        SessionValidationResult validation,
        DateTimeOffset lastSeenAt)
    {
        return UAuthSessionChain.Create(
            chainId: validation.ChainId!.Value,
            rootId: validation.RootId!.Value,
            tenant: validation.Tenant,
            userKey: validation.UserKey!.Value,
            createdAt: lastSeenAt,
            expiresAt: Now.AddHours(1),
            device: TestDevice.Default(),
            claimsSnapshot: ClaimsSnapshot.Empty,
            securityVersion: 0);
    }

    private static async Task<(InMemorySessionStoreFactory Factory, SessionValidationResult Validation, UAuthSessionChain Chain,
        UAuthSession Session)> CreateValidSessionAsync(DateTimeOffset lastSeenAt, DateTimeOffset? sessionExpiresAt = null)
    {
        var factory = new InMemorySessionStoreFactory();
        var validation = CreateActiveValidation();
        var store = factory.Create(validation.Tenant);

        // 1. Create and persist the initial chain (Version = 0).
        var chain = CreateChain(validation, lastSeenAt);
        await store.CreateChainAsync(chain);

        // 2. Create and persist the session (Version = 0).
        var session = UAuthSession.Create(
            sessionId: validation.SessionId!.Value,
            tenant: validation.Tenant,
            userKey: validation.UserKey!.Value,
            chainId: validation.ChainId!.Value,
            now: lastSeenAt,
            expiresAt: sessionExpiresAt ?? Now.AddHours(1),
            securityVersion: 0,
            device: TestDevice.Default(),
            claims: ClaimsSnapshot.Empty,
            metadata: SessionMetadata.Empty);

        await store.CreateSessionAsync(session);

        // 3. Attach the session to the existing chain.
        var activeChain = chain.AttachSession(
            session.SessionId,
            lastSeenAt);

        // Optimistic concurrency: expected version is 0.
        await store.SaveChainAsync(
            activeChain,
            chain.Version);

        return (factory, validation, activeChain, session);
    }
}
