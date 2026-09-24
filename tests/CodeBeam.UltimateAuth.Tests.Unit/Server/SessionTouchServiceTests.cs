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
        var validation = SessionValidationResult.Invalid(SessionState.Revoked);

        var result = await sut.RefreshAsync(
            validation,
            new SessionTouchPolicy { TouchInterval = TimeSpan.FromMinutes(5) },
            SessionTouchMode.IfNeeded,
            Now);

        result.RequiresReauth.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();
        factory.Verify(x => x.Create(It.IsAny<TenantKey>()), Times.Never);
    }

    [Fact]
    public async Task RefreshAsync_WhenChainIdIsMissing_ReturnsReauthRequired()
    {
        var factory = new Mock<ISessionStoreFactory>(MockBehavior.Strict);
        var sut = new SessionTouchService(factory.Object);
        var validation = SessionValidationResult.Invalid(
            SessionState.Active,
            userId: TestUsers.User,
            sessionId: TestIds.Session("active-session"));

        var result = await sut.RefreshAsync(
            validation,
            new SessionTouchPolicy { TouchInterval = TimeSpan.FromMinutes(5) },
            SessionTouchMode.IfNeeded,
            Now);

        result.RequiresReauth.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();
        factory.Verify(x => x.Create(It.IsAny<TenantKey>()), Times.Never);
    }

    [Fact]
    public async Task RefreshAsync_WhenTouchIntervalIsDisabled_ReturnsSuccessWithoutTouchingStore()
    {
        var factory = new Mock<ISessionStoreFactory>(MockBehavior.Strict);
        var sut = new SessionTouchService(factory.Object);
        var validation = CreateActiveValidation();

        var result = await sut.RefreshAsync(
            validation,
            new SessionTouchPolicy { TouchInterval = null },
            SessionTouchMode.IfNeeded,
            Now);

        result.IsSuccess.Should().BeTrue();
        result.DidTouch.Should().BeFalse();
        result.SessionId.Should().Be(validation.SessionId);
        factory.Verify(x => x.Create(It.IsAny<TenantKey>()), Times.Never);
    }

    [Fact]
    public async Task RefreshAsync_WhenChainDoesNotExist_ReturnsSuccessWithoutTouch()
    {
        var factory = new InMemorySessionStoreFactory();
        var sut = new SessionTouchService(factory);
        var validation = CreateActiveValidation();

        var result = await sut.RefreshAsync(
            validation,
            new SessionTouchPolicy { TouchInterval = TimeSpan.FromMinutes(5) },
            SessionTouchMode.IfNeeded,
            Now);

        result.IsSuccess.Should().BeTrue();
        result.DidTouch.Should().BeFalse();
        result.SessionId.Should().Be(validation.SessionId);
    }

    [Fact]
    public async Task RefreshAsync_WhenChainIsRevoked_ReturnsSuccessWithoutTouch()
    {
        var factory = new InMemorySessionStoreFactory();
        var validation = CreateActiveValidation();
        var store = factory.Create(validation.Tenant);

        var chain = CreateChain(validation, Now.AddMinutes(-10));
        await store.CreateChainAsync(chain);

        var revoked = chain.Revoke(Now.AddMinutes(-1));
        await store.SaveChainAsync(revoked, chain.Version);

        var sut = new SessionTouchService(factory);

        var result = await sut.RefreshAsync(
            validation,
            new SessionTouchPolicy { TouchInterval = TimeSpan.FromMinutes(5) },
            SessionTouchMode.IfNeeded,
            Now);

        result.IsSuccess.Should().BeTrue();
        result.DidTouch.Should().BeFalse();

        var persisted = await store.GetChainAsync(validation.ChainId!.Value);

        persisted.Should().NotBeNull();
        persisted!.IsRevoked.Should().BeTrue();
        persisted.LastSeenAt.Should().Be(revoked.LastSeenAt);
        persisted.TouchCount.Should().Be(revoked.TouchCount);
        persisted.Version.Should().Be(revoked.Version);
    }

    [Fact]
    public async Task RefreshAsync_WhenTouchIntervalHasNotElapsed_ReturnsSuccessWithoutTouch()
    {
        var factory = new InMemorySessionStoreFactory();
        var validation = CreateActiveValidation();
        var store = factory.Create(validation.Tenant);
        var chain = CreateChain(validation, Now.AddMinutes(-4));
        await store.CreateChainAsync(chain);

        var sut = new SessionTouchService(factory);

        var result = await sut.RefreshAsync(
            validation,
            new SessionTouchPolicy { TouchInterval = TimeSpan.FromMinutes(5) },
            SessionTouchMode.IfNeeded,
            Now);

        result.IsSuccess.Should().BeTrue();
        result.DidTouch.Should().BeFalse();

        var persisted = await store.GetChainAsync(validation.ChainId!.Value);
        persisted!.LastSeenAt.Should().Be(chain.LastSeenAt);
        persisted.TouchCount.Should().Be(0);
    }

    [Fact]
    public async Task RefreshAsync_WhenTouchIntervalHasElapsed_TouchesAndSavesChain()
    {
        var factory = new InMemorySessionStoreFactory();
        var validation = CreateActiveValidation();
        var store = factory.Create(validation.Tenant);
        var chain = CreateChain(validation, Now.AddMinutes(-6));
        await store.CreateChainAsync(chain);

        var sut = new SessionTouchService(factory);

        var result = await sut.RefreshAsync(
            validation,
            new SessionTouchPolicy { TouchInterval = TimeSpan.FromMinutes(5) },
            SessionTouchMode.IfNeeded,
            Now);

        result.IsSuccess.Should().BeTrue();
        result.DidTouch.Should().BeTrue();
        result.SessionId.Should().Be(validation.SessionId);

        var persisted = await store.GetChainAsync(validation.ChainId!.Value);
        persisted!.LastSeenAt.Should().Be(Now);
        persisted.TouchCount.Should().Be(1);
        persisted.Version.Should().Be(chain.Version + 1);
    }

    [Fact]
    public async Task RefreshAsync_WhenTouchIntervalExactlyElapsed_TouchesChain()
    {
        var factory = new InMemorySessionStoreFactory();
        var validation = CreateActiveValidation();
        var store = factory.Create(validation.Tenant);
        var chain = CreateChain(validation, Now.AddMinutes(-5));
        await store.CreateChainAsync(chain);

        var sut = new SessionTouchService(factory);

        var result = await sut.RefreshAsync(
            validation,
            new SessionTouchPolicy { TouchInterval = TimeSpan.FromMinutes(5) },
            SessionTouchMode.IfNeeded,
            Now);

        result.IsSuccess.Should().BeTrue();
        result.DidTouch.Should().BeTrue();

        var persisted = await store.GetChainAsync(validation.ChainId!.Value);
        persisted!.LastSeenAt.Should().Be(Now);
        persisted.TouchCount.Should().Be(1);
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

    private static UAuthSessionChain CreateChain(SessionValidationResult validation, DateTimeOffset lastSeenAt)
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
}
