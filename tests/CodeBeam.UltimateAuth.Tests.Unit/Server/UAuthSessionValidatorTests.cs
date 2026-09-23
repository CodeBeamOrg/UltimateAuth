using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Server.Services;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Unit.Server;

public sealed class UAuthSessionValidatorTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 9, 20, 12, 0, 0, TimeSpan.Zero);

    // ---------------------------------------------------------------------
    // Session
    // ---------------------------------------------------------------------

    [Fact]
    public async Task ValidateSessionAsync_WhenSessionDoesNotExist_ReturnsNotFound()
    {
        var fixture = CreateFixture();

        fixture.Store
            .Setup(x => x.GetSessionAsync(
                fixture.Session.SessionId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((UAuthSession?)null);

        var result = await fixture.Sut.ValidateSessionAsync(fixture.Context);

        result.IsValid.Should().BeFalse();
        result.State.Should().Be(SessionState.NotFound);
        result.SessionId.Should().Be(fixture.Session.SessionId);

        fixture.Store.Verify(
            x => x.GetChainAsync(
                It.IsAny<SessionChainId>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        fixture.ClaimsProvider.Verify(
            x => x.GetClaimsAsync(
                It.IsAny<TenantKey>(),
                It.IsAny<UserKey>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ValidateSessionAsync_WhenSessionIsExpired_ReturnsExpired()
    {
        var fixture = CreateFixture(
            sessionExpiresAt: Now);

        SetupSession(fixture);

        var result = await fixture.Sut.ValidateSessionAsync(fixture.Context);

        result.IsValid.Should().BeFalse();
        result.State.Should().Be(SessionState.Expired);
        result.UserKey.Should().Be(fixture.User);
        result.SessionId.Should().Be(fixture.Session.SessionId);
        result.ChainId.Should().Be(fixture.Chain.ChainId);

        fixture.Store.Verify(
            x => x.GetChainAsync(
                It.IsAny<SessionChainId>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ValidateSessionAsync_WhenSessionIsRevoked_ReturnsRevoked()
    {
        var fixture = CreateFixture();

        var revoked = fixture.Session.Revoke(Now.AddMinutes(-1));

        fixture.Store
            .Setup(x => x.GetSessionAsync(
                fixture.Session.SessionId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(revoked);

        var result = await fixture.Sut.ValidateSessionAsync(fixture.Context);

        result.IsValid.Should().BeFalse();
        result.State.Should().Be(SessionState.Revoked);
        result.UserKey.Should().Be(fixture.User);
        result.SessionId.Should().Be(fixture.Session.SessionId);
        result.ChainId.Should().Be(fixture.Chain.ChainId);

        fixture.Store.Verify(
            x => x.GetChainAsync(
                It.IsAny<SessionChainId>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ---------------------------------------------------------------------
    // Chain
    // ---------------------------------------------------------------------

    [Fact]
    public async Task ValidateSessionAsync_WhenChainDoesNotExist_ReturnsRevoked()
    {
        var fixture = CreateFixture();

        SetupSession(fixture);

        fixture.Store
            .Setup(x => x.GetChainAsync(
                fixture.Chain.ChainId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((UAuthSessionChain?)null);

        var result = await fixture.Sut.ValidateSessionAsync(fixture.Context);

        result.IsValid.Should().BeFalse();
        result.State.Should().Be(SessionState.Revoked);
        result.UserKey.Should().Be(fixture.User);
        result.SessionId.Should().Be(fixture.Session.SessionId);
        result.ChainId.Should().Be(fixture.Chain.ChainId);

        fixture.Store.Verify(
            x => x.GetRootByUserAsync(
                It.IsAny<UserKey>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ValidateSessionAsync_WhenChainIsRevoked_ReturnsRevoked()
    {
        var fixture = CreateFixture();

        var revokedChain = fixture.Chain.Revoke(Now.AddMinutes(-1));

        SetupSession(fixture);

        fixture.Store
            .Setup(x => x.GetChainAsync(
                fixture.Chain.ChainId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(revokedChain);

        var result = await fixture.Sut.ValidateSessionAsync(fixture.Context);

        result.IsValid.Should().BeFalse();
        result.State.Should().Be(SessionState.Revoked);

        fixture.Store.Verify(
            x => x.GetRootByUserAsync(
                It.IsAny<UserKey>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ValidateSessionAsync_WhenChainIsAbsolutelyExpired_ReturnsExpired()
    {
        var fixture = CreateFixture(
            chainExpiresAt: Now);

        SetupSessionAndChain(fixture);

        var result = await fixture.Sut.ValidateSessionAsync(fixture.Context);

        result.IsValid.Should().BeFalse();
        result.State.Should().Be(SessionState.Expired);

        fixture.Store.Verify(
            x => x.GetRootByUserAsync(
                It.IsAny<UserKey>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ValidateSessionAsync_WhenChainIdleTimeoutIsExceeded_ReturnsExpired()
    {
        var fixture = CreateFixture(
            chainCreatedAt: Now.AddHours(-2),
            idleTimeout: TimeSpan.FromHours(1));

        SetupSessionAndChain(fixture);

        var result = await fixture.Sut.ValidateSessionAsync(fixture.Context);

        result.IsValid.Should().BeFalse();
        result.State.Should().Be(SessionState.Expired);

        fixture.Store.Verify(
            x => x.GetRootByUserAsync(
                It.IsAny<UserKey>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ---------------------------------------------------------------------
    // Root
    // ---------------------------------------------------------------------

    [Fact]
    public async Task ValidateSessionAsync_WhenRootDoesNotExist_ReturnsRevoked()
    {
        var fixture = CreateFixture();

        SetupSessionAndChain(fixture);

        fixture.Store
            .Setup(x => x.GetRootByUserAsync(
                fixture.User,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((UAuthSessionRoot?)null);

        var result = await fixture.Sut.ValidateSessionAsync(fixture.Context);

        result.IsValid.Should().BeFalse();
        result.State.Should().Be(SessionState.Revoked);
        result.RootId.Should().BeNull();

        fixture.ClaimsProvider.Verify(
            x => x.GetClaimsAsync(
                It.IsAny<TenantKey>(),
                It.IsAny<UserKey>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ValidateSessionAsync_WhenRootIsRevoked_ReturnsRevoked()
    {
        var fixture = CreateFixture();

        var revokedRoot = fixture.Root.Revoke(Now.AddMinutes(-1));

        SetupSessionAndChain(fixture);

        fixture.Store
            .Setup(x => x.GetRootByUserAsync(
                fixture.User,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(revokedRoot);

        var result = await fixture.Sut.ValidateSessionAsync(fixture.Context);

        result.IsValid.Should().BeFalse();
        result.State.Should().Be(SessionState.Revoked);
        result.RootId.Should().Be(revokedRoot.RootId);

        fixture.ClaimsProvider.Verify(
            x => x.GetClaimsAsync(
                It.IsAny<TenantKey>(),
                It.IsAny<UserKey>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ValidateSessionAsync_WhenChainBelongsToDifferentRoot_ReturnsSecurityMismatch()
    {
        var fixture = CreateFixture();

        var differentRoot = UAuthSessionRoot.Create(
            fixture.Tenant,
            fixture.User,
            Now.AddDays(-2));

        SetupSessionAndChain(fixture);

        fixture.Store
            .Setup(x => x.GetRootByUserAsync(
                fixture.User,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(differentRoot);

        var result = await fixture.Sut.ValidateSessionAsync(fixture.Context);

        result.IsValid.Should().BeFalse();
        result.State.Should().Be(SessionState.SecurityMismatch);
        result.RootId.Should().Be(differentRoot.RootId);

        fixture.ClaimsProvider.Verify(
            x => x.GetClaimsAsync(
                It.IsAny<TenantKey>(),
                It.IsAny<UserKey>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ValidateSessionAsync_WhenSecurityVersionDoesNotMatch_ReturnsSecurityMismatch()
    {
        var fixture = CreateFixture();

        var updatedRoot = fixture.Root.IncreaseSecurityVersion(
            Now.AddMinutes(-1));

        SetupSessionAndChain(fixture);

        fixture.Store
            .Setup(x => x.GetRootByUserAsync(
                fixture.User,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedRoot);

        var result = await fixture.Sut.ValidateSessionAsync(fixture.Context);

        result.IsValid.Should().BeFalse();
        result.State.Should().Be(SessionState.SecurityMismatch);
        result.RootId.Should().Be(updatedRoot.RootId);

        fixture.ClaimsProvider.Verify(
            x => x.GetClaimsAsync(
                It.IsAny<TenantKey>(),
                It.IsAny<UserKey>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ---------------------------------------------------------------------
    // Device
    // ---------------------------------------------------------------------

    [Fact]
    public async Task ValidateSessionAsync_WhenDeviceDoesNotMatchAndBehaviorIsReject_ReturnsDeviceMismatch()
    {
        var fixture = CreateFixture(
            requestDevice: TestDevice.Alternative(),
            deviceMismatchBehavior: DeviceMismatchBehavior.Reject);

        SetupValidAggregate(fixture);

        var result = await fixture.Sut.ValidateSessionAsync(fixture.Context);

        result.IsValid.Should().BeFalse();
        result.State.Should().Be(SessionState.DeviceMismatch);
        result.UserKey.Should().Be(fixture.User);
        result.SessionId.Should().Be(fixture.Session.SessionId);
        result.ChainId.Should().Be(fixture.Chain.ChainId);
        result.RootId.Should().Be(fixture.Root.RootId);

        fixture.ClaimsProvider.Verify(
            x => x.GetClaimsAsync(
                It.IsAny<TenantKey>(),
                It.IsAny<UserKey>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ValidateSessionAsync_WhenRequestHasNoDeviceId_AllowsValidation()
    {
        var fixture = CreateFixture(
            requestDevice: DeviceContext.Anonymous());

        var claims = ClaimsSnapshot.From(
            ("uauth:permission", "orders.read"));

        SetupValidAggregate(fixture);

        fixture.ClaimsProvider
            .Setup(x => x.GetClaimsAsync(
                fixture.Tenant,
                fixture.User,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(claims);

        var result = await fixture.Sut.ValidateSessionAsync(fixture.Context);

        result.IsValid.Should().BeTrue();
        result.State.Should().Be(SessionState.Active);
    }

    // ---------------------------------------------------------------------
    // Success
    // ---------------------------------------------------------------------

    [Fact]
    public async Task ValidateSessionAsync_WhenAggregateIsValid_ReturnsActiveResult()
    {
        var fixture = CreateFixture();

        var claims = ClaimsSnapshot.From(
            ("uauth:permission", "orders.read"),
            ("uauth:permission", "orders.write"));

        SetupValidAggregate(fixture);

        fixture.ClaimsProvider
            .Setup(x => x.GetClaimsAsync(
                fixture.Tenant,
                fixture.User,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(claims);

        var result = await fixture.Sut.ValidateSessionAsync(fixture.Context);

        result.IsValid.Should().BeTrue();
        result.State.Should().Be(SessionState.Active);

        result.Tenant.Should().Be(fixture.Tenant);
        result.UserKey.Should().Be(fixture.User);
        result.SessionId.Should().Be(fixture.Session.SessionId);
        result.ChainId.Should().Be(fixture.Chain.ChainId);
        result.RootId.Should().Be(fixture.Root.RootId);

        result.Claims.Should().Be(claims);
        result.AuthenticatedAt.Should().Be(fixture.Session.CreatedAt);
        result.BoundDeviceId.Should().Be(fixture.Chain.Device.DeviceId);
    }

    [Fact]
    public async Task ValidateSessionAsync_PropagatesCancellationToken()
    {
        var fixture = CreateFixture();

        using var cts = new CancellationTokenSource();
        var ct = cts.Token;

        fixture.Store
            .Setup(x => x.GetSessionAsync(
                fixture.Session.SessionId,
                ct))
            .ReturnsAsync(fixture.Session);

        fixture.Store
            .Setup(x => x.GetChainAsync(
                fixture.Chain.ChainId,
                ct))
            .ReturnsAsync(fixture.Chain);

        fixture.Store
            .Setup(x => x.GetRootByUserAsync(
                fixture.User,
                ct))
            .ReturnsAsync(fixture.Root);

        fixture.ClaimsProvider
            .Setup(x => x.GetClaimsAsync(
                fixture.Tenant,
                fixture.User,
                ct))
            .ReturnsAsync(ClaimsSnapshot.Empty);

        var result = await fixture.Sut.ValidateSessionAsync(
            fixture.Context,
            ct);

        result.IsValid.Should().BeTrue();

        fixture.Store.Verify(
            x => x.GetSessionAsync(fixture.Session.SessionId, ct),
            Times.Once);

        fixture.Store.Verify(
            x => x.GetChainAsync(fixture.Chain.ChainId, ct),
            Times.Once);

        fixture.Store.Verify(
            x => x.GetRootByUserAsync(fixture.User, ct),
            Times.Once);

        fixture.ClaimsProvider.Verify(
            x => x.GetClaimsAsync(fixture.Tenant, fixture.User, ct),
            Times.Once);
    }

    // ---------------------------------------------------------------------
    // Setup
    // ---------------------------------------------------------------------

    private static void SetupSession(Fixture fixture)
    {
        fixture.Store
            .Setup(x => x.GetSessionAsync(
                fixture.Session.SessionId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(fixture.Session);
    }

    private static void SetupSessionAndChain(Fixture fixture)
    {
        SetupSession(fixture);

        fixture.Store
            .Setup(x => x.GetChainAsync(
                fixture.Chain.ChainId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(fixture.Chain);
    }

    private static void SetupValidAggregate(Fixture fixture)
    {
        SetupSessionAndChain(fixture);

        fixture.Store
            .Setup(x => x.GetRootByUserAsync(
                fixture.User,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(fixture.Root);
    }

    private static Fixture CreateFixture(
        DateTimeOffset? sessionExpiresAt = null,
        DateTimeOffset? chainCreatedAt = null,
        DateTimeOffset? chainExpiresAt = null,
        TimeSpan? idleTimeout = null,
        DeviceContext? requestDevice = null,
        DeviceMismatchBehavior deviceMismatchBehavior =
            DeviceMismatchBehavior.Reject)
    {
        var tenant = TenantKey.Single;
        var user = UserKey.New();

        var root = UAuthSessionRoot.Create(
            tenant,
            user,
            Now.AddDays(-1));

        var chain = UAuthSessionChain.Create(
            SessionChainId.New(),
            root.RootId,
            tenant,
            user,
            chainCreatedAt ?? Now.AddHours(-1),
            chainExpiresAt ?? Now.AddDays(7),
            TestDevice.Default(),
            ClaimsSnapshot.Empty,
            root.SecurityVersion);

        var session = UAuthSession.Create(
            TestIds.Session("test-session"),
            tenant,
            user,
            chain.ChainId,
            Now.AddMinutes(-30),
            sessionExpiresAt ?? Now.AddHours(8),
            root.SecurityVersion,
            TestDevice.Default(),
            ClaimsSnapshot.Empty,
            SessionMetadata.Empty);

        var store = new Mock<ISessionStore>(MockBehavior.Strict);

        var storeFactory =
            new Mock<ISessionStoreFactory>(MockBehavior.Strict);

        storeFactory
            .Setup(x => x.Create(tenant))
            .Returns(store.Object);

        var claimsProvider =
            new Mock<IUserClaimsProvider>(MockBehavior.Strict);

        var options = new UAuthServerOptions();

        options.Session.IdleTimeout = idleTimeout;
        options.Session.DeviceMismatchBehavior = deviceMismatchBehavior;

        var sut = new UAuthSessionValidator(
            storeFactory.Object,
            claimsProvider.Object,
            Options.Create(options));

        var context = new SessionValidationContext
        {
            Tenant = tenant,
            SessionId = session.SessionId,
            Now = Now,
            Device = requestDevice ?? TestDevice.Default()
        };

        return new Fixture(
            sut,
            store,
            claimsProvider,
            tenant,
            user,
            root,
            chain,
            session,
            context);
    }

    private sealed record Fixture(
        UAuthSessionValidator Sut,
        Mock<ISessionStore> Store,
        Mock<IUserClaimsProvider> ClaimsProvider,
        TenantKey Tenant,
        UserKey User,
        UAuthSessionRoot Root,
        UAuthSessionChain Chain,
        UAuthSession Session,
        SessionValidationContext Context);
}