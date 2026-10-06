using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using System.Security;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public sealed class UAuthSessionIssuerTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 10, 6, 12, 0, 0, TimeSpan.Zero);

    private static readonly TenantKey Tenant =
        TenantKey.FromExternal("tenant-a");

    private static readonly TimeSpan Lifetime =
        TimeSpan.FromHours(8);

    private const string OpaqueToken =
        "opaque-session-token-000000000000000000000001";

    // =====================================================================
    // IssueSessionAsync - mode / token guards
    // =====================================================================

    [Fact]
    public async Task IssueSessionAsync_WhenModeIsPureJwt_ShouldRejectSessionIssuance()
    {
        var fixture = CreateFixture();
        var context = CreateIssuanceContext(
            mode: UAuthMode.PureJwt);

        var act = () =>
            fixture.Sut.IssueSessionAsync(context);

        await act.Should()
            .ThrowAsync<InvalidOperationException>();

        fixture.TokenGenerator.Verify(
            x => x.Generate(),
            Times.Never);

        fixture.StoreFactory.Verify(
            x => x.Create(It.IsAny<TenantKey>()),
            Times.Never);
    }

    [Fact]
    public async Task IssueSessionAsync_WhenOpaqueGeneratorReturnsInvalidId_ShouldFailBeforeStoreAccess()
    {
        var fixture = CreateFixture(
            opaqueToken: "invalid");

        var context = CreateIssuanceContext();

        var act = () =>
            fixture.Sut.IssueSessionAsync(context);

        await act.Should()
            .ThrowAsync<InvalidCastException>();

        fixture.StoreFactory.Verify(
            x => x.Create(It.IsAny<TenantKey>()),
            Times.Never);
    }

    // =====================================================================
    // IssueSessionAsync - root
    // =====================================================================

    [Fact]
    public async Task IssueSessionAsync_WhenActiveRootDoesNotExist_ShouldCreateRoot()
    {
        var fixture = CreateFixture();
        var context = CreateIssuanceContext();

        fixture.Store
            .Setup(x => x.GetActiveRootByUserAsync(
                context.UserKey,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((UAuthSessionRoot?)null);

        UAuthSessionRoot? createdRoot = null;

        fixture.Store
            .Setup(x => x.CreateRootAsync(
                It.IsAny<UAuthSessionRoot>(),
                It.IsAny<CancellationToken>()))
            .Callback<UAuthSessionRoot, CancellationToken>(
                (root, _) => createdRoot = root)
            .Returns(Task.CompletedTask);

        SetupEmptySessions(fixture);
        SetupSessionAndChainPersistence(fixture);

        await fixture.Sut.IssueSessionAsync(context);

        createdRoot.Should().NotBeNull();
        createdRoot!.Tenant.Should().Be(context.Tenant);
        createdRoot.UserKey.Should().Be(context.UserKey);

        fixture.Store.Verify(
            x => x.CreateRootAsync(
                It.IsAny<UAuthSessionRoot>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task IssueSessionAsync_WhenActiveRootExists_ShouldReuseRoot()
    {
        var fixture = CreateFixture();
        var context = CreateIssuanceContext();

        var root = CreateRoot(
            context.Tenant,
            context.UserKey);

        fixture.Store
            .Setup(x => x.GetActiveRootByUserAsync(
                context.UserKey,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(root);

        SetupEmptySessions(fixture);
        SetupSessionAndChainPersistence(fixture);

        var result =
            await fixture.Sut.IssueSessionAsync(context);

        result.Session.Should().NotBeNull();

        fixture.Store.Verify(
            x => x.CreateRootAsync(
                It.IsAny<UAuthSessionRoot>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // =====================================================================
    // IssueSessionAsync - chain creation
    // =====================================================================

    [Fact]
    public async Task IssueSessionAsync_WhenChainIdIsNotProvided_ShouldCreateNewChain()
    {
        var fixture = CreateFixture();
        var context = CreateIssuanceContext();

        var root = CreateRoot(
            context.Tenant,
            context.UserKey);

        SetupRoot(fixture, context.UserKey, root);

        UAuthSessionChain? createdChain = null;

        fixture.Store
            .Setup(x => x.CreateChainAsync(
                It.IsAny<UAuthSessionChain>(),
                It.IsAny<CancellationToken>()))
            .Callback<UAuthSessionChain, CancellationToken>(
                (chain, _) => createdChain = chain)
            .Returns(Task.CompletedTask);

        SetupEmptySessions(fixture);
        SetupSessionPersistence(fixture);
        SetupChainSave(fixture);

        var result =
            await fixture.Sut.IssueSessionAsync(context);

        createdChain.Should().NotBeNull();

        createdChain!.Tenant.Should()
            .Be(context.Tenant);

        createdChain.UserKey.Should()
            .Be(context.UserKey);

        createdChain.RootId.Should()
            .Be(root.RootId);

        result.Session.ChainId.Should()
            .Be(createdChain.ChainId);
    }

    [Fact]
    public async Task IssueSessionAsync_WhenExistingChainIsProvided_ShouldReuseChain()
    {
        var fixture = CreateFixture();
        var context = CreateIssuanceContext();

        var root = CreateRoot(
            context.Tenant,
            context.UserKey);

        var chain = CreateChain(
            root,
            context.UserKey,
            context.Tenant);

        context = CreateIssuanceContext(
            userKey: context.UserKey,
            chainId: chain.ChainId);

        SetupRoot(fixture, context.UserKey, root);

        fixture.Store
            .Setup(x => x.GetChainAsync(
                chain.ChainId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(chain);

        SetupEmptySessions(fixture);
        SetupSessionPersistence(fixture);
        SetupChainSave(fixture);

        var result =
            await fixture.Sut.IssueSessionAsync(context);

        result.Session.ChainId.Should()
            .Be(chain.ChainId);

        fixture.Store.Verify(
            x => x.CreateChainAsync(
                It.IsAny<UAuthSessionChain>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task IssueSessionAsync_WhenExplicitChainDoesNotExist_CurrentlyCreatesDifferentChain()
    {
        var fixture = CreateFixture();
        var userKey = UserKey.New();

        var requestedChainId =
            SessionChainId.New();

        var context = CreateIssuanceContext(
            userKey: userKey,
            chainId: requestedChainId);

        var root = CreateRoot(
            context.Tenant,
            context.UserKey);

        SetupRoot(fixture, context.UserKey, root);

        fixture.Store
            .Setup(x => x.GetChainAsync(
                requestedChainId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((UAuthSessionChain?)null);

        UAuthSessionChain? createdChain = null;

        fixture.Store
            .Setup(x => x.CreateChainAsync(
                It.IsAny<UAuthSessionChain>(),
                It.IsAny<CancellationToken>()))
            .Callback<UAuthSessionChain, CancellationToken>(
                (chain, _) => createdChain = chain)
            .Returns(Task.CompletedTask);

        SetupEmptySessions(fixture);
        SetupSessionPersistence(fixture);
        SetupChainSave(fixture);

        var result =
            await fixture.Sut.IssueSessionAsync(context);

        createdChain.Should().NotBeNull();

        // Regression documentation:
        // Current implementation silently creates a NEW chain when the
        // explicitly requested chain cannot be found.
        createdChain!.ChainId.Should()
            .NotBe(requestedChainId);

        result.Session.ChainId.Should()
            .Be(createdChain.ChainId);
    }

    // =====================================================================
    // IssueSessionAsync - chain security
    // =====================================================================

    [Fact]
    public async Task IssueSessionAsync_WhenExistingChainBelongsToDifferentUser_ShouldReject()
    {
        var fixture = CreateFixture();

        var user = UserKey.New();
        var otherUser = UserKey.New();

        var root = CreateRoot(Tenant, user);
        var chain = CreateChain(
            root,
            otherUser,
            Tenant);

        var context = CreateIssuanceContext(
            userKey: user,
            chainId: chain.ChainId);

        SetupRoot(fixture, user, root);
        SetupChain(fixture, chain);

        var act = () =>
            fixture.Sut.IssueSessionAsync(context);

        await act.Should()
            .ThrowAsync<UAuthValidationException>();
    }

    [Fact]
    public async Task IssueSessionAsync_WhenExistingChainBelongsToDifferentTenant_ShouldReject()
    {
        var fixture = CreateFixture();

        var user = UserKey.New();
        var otherTenant =
            TenantKey.FromExternal("tenant-b");

        var root = CreateRoot(Tenant, user);

        var chain = CreateChain(
            root,
            user,
            otherTenant);

        var context = CreateIssuanceContext(
            userKey: user,
            chainId: chain.ChainId);

        SetupRoot(fixture, user, root);
        SetupChain(fixture, chain);

        var act = () =>
            fixture.Sut.IssueSessionAsync(context);

        await act.Should()
            .ThrowAsync<UAuthValidationException>();
    }

    [Fact]
    public async Task IssueSessionAsync_WhenChainBelongsToDifferentRoot_ShouldReject()
    {
        var fixture = CreateFixture();

        var user = UserKey.New();

        var activeRoot =
            CreateRoot(Tenant, user);

        var otherRoot =
            CreateRoot(Tenant, user);

        var chain =
            CreateChain(otherRoot, user, Tenant);

        var context = CreateIssuanceContext(
            userKey: user,
            chainId: chain.ChainId);

        SetupRoot(fixture, user, activeRoot);
        SetupChain(fixture, chain);

        var act = () =>
            fixture.Sut.IssueSessionAsync(context);

        await act.Should()
            .ThrowAsync<UAuthValidationException>();
    }

    [Fact]
    public async Task IssueSessionAsync_WhenChainIsRevoked_ShouldReject()
    {
        var fixture = CreateFixture();

        var user = UserKey.New();
        var root = CreateRoot(Tenant, user);

        var chain = CreateChain(
            root,
            user,
            Tenant);

        chain = chain.Revoke(Now);

        var context = CreateIssuanceContext(
            userKey: user,
            chainId: chain.ChainId);

        SetupRoot(fixture, user, root);
        SetupChain(fixture, chain);

        var act = () =>
            fixture.Sut.IssueSessionAsync(context);

        await act.Should()
            .ThrowAsync<UAuthValidationException>();
    }

    // =====================================================================
    // IssueSessionAsync - expiration
    // =====================================================================

    [Fact]
    public async Task IssueSessionAsync_ShouldUseConfiguredSessionLifetime()
    {
        var fixture = CreateFixture(
            lifetime: TimeSpan.FromHours(4));

        var context = CreateIssuanceContext();

        SetupNewIssuance(fixture, context);

        var result =
            await fixture.Sut.IssueSessionAsync(context);

        result.Session.ExpiresAt.Should()
            .Be(Now.AddHours(4));
    }

    [Fact]
    public async Task IssueSessionAsync_WhenMaxLifetimeIsShorter_ShouldCapExpiration()
    {
        var fixture = CreateFixture(
            lifetime: TimeSpan.FromHours(8),
            maxLifetime: TimeSpan.FromHours(2));

        var context = CreateIssuanceContext();

        SetupNewIssuance(fixture, context);

        var result =
            await fixture.Sut.IssueSessionAsync(context);

        result.Session.ExpiresAt.Should()
            .Be(Now.AddHours(2));
    }

    [Fact]
    public async Task IssueSessionAsync_WhenMaxLifetimeIsLonger_ShouldUseNormalLifetime()
    {
        var fixture = CreateFixture(
            lifetime: TimeSpan.FromHours(4),
            maxLifetime: TimeSpan.FromHours(24));

        var context = CreateIssuanceContext();

        SetupNewIssuance(fixture, context);

        var result =
            await fixture.Sut.IssueSessionAsync(context);

        result.Session.ExpiresAt.Should()
            .Be(Now.AddHours(4));
    }

    // =====================================================================
    // IssueSessionAsync - result / persistence
    // =====================================================================

    [Fact]
    public async Task IssueSessionAsync_ShouldPersistSessionAndAttachItToChain()
    {
        var fixture = CreateFixture();
        var context = CreateIssuanceContext();

        var root = CreateRoot(
            context.Tenant,
            context.UserKey);

        SetupRoot(fixture, context.UserKey, root);

        UAuthSession? createdSession = null;
        UAuthSessionChain? savedChain = null;

        fixture.Store
            .Setup(x => x.CreateChainAsync(
                It.IsAny<UAuthSessionChain>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        fixture.Store
            .Setup(x => x.GetSessionsByChainAsync(
                It.IsAny<SessionChainId>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<UAuthSession>());

        fixture.Store
            .Setup(x => x.CreateSessionAsync(
                It.IsAny<UAuthSession>(),
                It.IsAny<CancellationToken>()))
            .Callback<UAuthSession, CancellationToken>(
                (session, _) => createdSession = session)
            .Returns(Task.CompletedTask);

        fixture.Store
            .Setup(x => x.SaveChainAsync(
                It.IsAny<UAuthSessionChain>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()))
            .Callback<UAuthSessionChain, long, CancellationToken>(
                (chain, _, _) => savedChain = chain)
            .Returns(Task.CompletedTask);

        var result =
            await fixture.Sut.IssueSessionAsync(context);

        createdSession.Should().NotBeNull();
        savedChain.Should().NotBeNull();

        result.Session.SessionId.Should()
            .Be(createdSession!.SessionId);

        savedChain!.ActiveSessionId.Should()
            .Be(createdSession.SessionId);

        result.OpaqueSessionId.Should()
            .Be(OpaqueToken);
    }

    [Fact]
    public async Task IssueSessionAsync_WhenModeIsSemiHybrid_ShouldMarkResultAsMetadataOnly()
    {
        var fixture = CreateFixture();

        var context = CreateIssuanceContext(
            mode: UAuthMode.SemiHybrid);

        SetupNewIssuance(fixture, context);

        var result =
            await fixture.Sut.IssueSessionAsync(context);

        result.IsMetadataOnly.Should().BeTrue();
    }

    [Fact]
    public async Task IssueSessionAsync_WhenModeIsHybrid_ShouldNotMarkResultAsMetadataOnly()
    {
        var fixture = CreateFixture();

        var context = CreateIssuanceContext(
            mode: UAuthMode.Hybrid);

        SetupNewIssuance(fixture, context);

        var result =
            await fixture.Sut.IssueSessionAsync(context);

        result.IsMetadataOnly.Should().BeFalse();
    }

    // =====================================================================
    // IssueSessionAsync - session limit
    // =====================================================================

    [Fact]
    public async Task IssueSessionAsync_WhenSessionLimitReached_ShouldRemoveOldNonActiveSessions()
    {
        var fixture = CreateFixture(
            maxSessionsPerChain: 2);

        var context = CreateIssuanceContext();

        var root = CreateRoot(
            context.Tenant,
            context.UserKey);

        var chain = CreateChain(
            root,
            context.UserKey,
            context.Tenant);

        var activeSession = CreateSession(
            context.UserKey,
            chain.ChainId,
            Now.AddHours(-1));

        chain = chain.AttachSession(
            activeSession.SessionId,
            Now.AddHours(-1));

        var oldSession1 = CreateSession(
            context.UserKey,
            chain.ChainId,
            Now.AddHours(-5));

        var oldSession2 = CreateSession(
            context.UserKey,
            chain.ChainId,
            Now.AddHours(-4));

        context = CreateIssuanceContext(
            userKey: context.UserKey,
            chainId: chain.ChainId);

        SetupRoot(fixture, context.UserKey, root);
        SetupChain(fixture, chain);

        fixture.Store
            .Setup(x => x.GetSessionsByChainAsync(
                chain.ChainId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                activeSession,
                oldSession2,
                oldSession1
            });

        SetupSessionPersistence(fixture);
        SetupChainSave(fixture);

        var removed = new List<AuthSessionId>();

        fixture.Store
            .Setup(x => x.RemoveSessionAsync(
                It.IsAny<AuthSessionId>(),
                It.IsAny<CancellationToken>()))
            .Callback<AuthSessionId, CancellationToken>(
                (id, _) => removed.Add(id))
            .Returns(Task.CompletedTask);

        await fixture.Sut.IssueSessionAsync(context);

        removed.Should().HaveCount(2);

        removed.Should()
            .Contain(oldSession1.SessionId);

        removed.Should()
            .Contain(oldSession2.SessionId);

        removed.Should()
            .NotContain(activeSession.SessionId);
    }

    [Fact]
    public async Task IssueSessionAsync_WhenBelowSessionLimit_ShouldNotRemoveSessions()
    {
        var fixture = CreateFixture(
            maxSessionsPerChain: 3);

        var context = CreateIssuanceContext();

        SetupNewIssuance(fixture, context);

        await fixture.Sut.IssueSessionAsync(context);

        fixture.Store.Verify(
            x => x.RemoveSessionAsync(
                It.IsAny<AuthSessionId>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // =====================================================================
    // RotateSessionAsync - token guard
    // =====================================================================

    [Fact]
    public async Task RotateSessionAsync_WhenOpaqueGeneratorReturnsInvalidId_ShouldFailBeforeTransaction()
    {
        var fixture = CreateFixture(
            opaqueToken: "invalid");

        var context = CreateRotationContext();

        var act = () =>
            fixture.Sut.RotateSessionAsync(context);

        await act.Should()
            .ThrowAsync<InvalidCastException>();

        fixture.StoreFactory.Verify(
            x => x.Create(context.Tenant),
            Times.Once);

        fixture.Store.Verify(
            x => x.ExecuteAsync(
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // =====================================================================
    // RotateSessionAsync - security failures
    // =====================================================================

    [Fact]
    public async Task RotateSessionAsync_WhenRootDoesNotExist_ShouldThrowSecurityException()
    {
        var fixture = CreateFixture();
        var context = CreateRotationContext();

        fixture.Store
            .Setup(x => x.GetActiveRootByUserAsync(
                context.UserKey,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((UAuthSessionRoot?)null);

        var act = () =>
            fixture.Sut.RotateSessionAsync(context);

        await act.Should()
            .ThrowAsync<SecurityException>();
    }

    [Fact]
    public async Task RotateSessionAsync_WhenSessionDoesNotExist_ShouldThrowSecurityException()
    {
        var fixture = CreateFixture();
        var context = CreateRotationContext();

        var root = CreateRoot(
            context.Tenant,
            context.UserKey);

        SetupRoot(fixture, context.UserKey, root);

        fixture.Store
            .Setup(x => x.GetSessionAsync(
                context.CurrentSessionId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((UAuthSession?)null);

        var act = () =>
            fixture.Sut.RotateSessionAsync(context);

        await act.Should()
            .ThrowAsync<SecurityException>();
    }

    [Fact]
    public async Task RotateSessionAsync_WhenSessionIsRevoked_ShouldThrowSecurityException()
    {
        var fixture = CreateFixture();
        var context = CreateRotationContext();

        var root = CreateRoot(
            context.Tenant,
            context.UserKey);

        var chain = CreateChain(
            root,
            context.UserKey,
            context.Tenant);

        var session = CreateSession(
            context.UserKey,
            chain.ChainId,
            Now.AddHours(-1),
            context.CurrentSessionId,
            root.SecurityVersion);

        session = session.Revoke(
            Now.AddMinutes(-1));

        SetupRoot(fixture, context.UserKey, root);
        SetupSession(fixture, session);

        var act = () =>
            fixture.Sut.RotateSessionAsync(context);

        await act.Should()
            .ThrowAsync<SecurityException>();
    }

    [Fact]
    public async Task RotateSessionAsync_WhenSessionIsExpired_ShouldThrowSecurityException()
    {
        var fixture = CreateFixture();
        var context = CreateRotationContext();

        var root = CreateRoot(
            context.Tenant,
            context.UserKey);

        var chain = CreateChain(
            root,
            context.UserKey,
            context.Tenant);

        var session = UAuthSession.Create(
            context.CurrentSessionId,
            context.Tenant,
            context.UserKey,
            chain.ChainId,
            Now.AddHours(-10),
            Now.AddMinutes(-1),
            root.SecurityVersion,
            context.Device,
            ClaimsSnapshot.Empty,
            SessionMetadata.Empty);

        SetupRoot(fixture, context.UserKey, root);
        SetupSession(fixture, session);

        var act = () =>
            fixture.Sut.RotateSessionAsync(context);

        await act.Should()
            .ThrowAsync<SecurityException>();
    }

    [Fact]
    public async Task RotateSessionAsync_WhenSessionBelongsToDifferentUser_ShouldThrowSecurityException()
    {
        var fixture = CreateFixture();

        var context = CreateRotationContext();
        var otherUser = UserKey.New();

        var root = CreateRoot(
            context.Tenant,
            context.UserKey);

        var chain = CreateChain(
            root,
            otherUser,
            context.Tenant);

        var session = CreateSession(
            otherUser,
            chain.ChainId,
            Now.AddHours(-1),
            context.CurrentSessionId,
            root.SecurityVersion);

        SetupRoot(fixture, context.UserKey, root);
        SetupSession(fixture, session);

        var act = () =>
            fixture.Sut.RotateSessionAsync(context);

        await act.Should()
            .ThrowAsync<SecurityException>();
    }

    [Fact]
    public async Task RotateSessionAsync_WhenSecurityVersionDoesNotMatch_ShouldThrowSecurityException()
    {
        var fixture = CreateFixture();
        var context = CreateRotationContext();

        var root = CreateRoot(
            context.Tenant,
            context.UserKey);

        var chain = CreateChain(
            root,
            context.UserKey,
            context.Tenant);

        var session = CreateSession(
            context.UserKey,
            chain.ChainId,
            Now.AddHours(-1),
            context.CurrentSessionId,
            root.SecurityVersion + 1);

        SetupRoot(fixture, context.UserKey, root);
        SetupSession(fixture, session);

        var act = () =>
            fixture.Sut.RotateSessionAsync(context);

        await act.Should()
            .ThrowAsync<SecurityException>();
    }

    [Fact]
    public async Task RotateSessionAsync_WhenChainDoesNotExist_ShouldThrowSecurityException()
    {
        var fixture = CreateFixture();
        var context = CreateRotationContext();

        var root = CreateRoot(
            context.Tenant,
            context.UserKey);

        var chainId =
            SessionChainId.New();

        var session = CreateSession(
            context.UserKey,
            chainId,
            Now.AddHours(-1),
            context.CurrentSessionId,
            root.SecurityVersion);

        SetupRoot(fixture, context.UserKey, root);
        SetupSession(fixture, session);

        fixture.Store
            .Setup(x => x.GetChainAsync(
                chainId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((UAuthSessionChain?)null);

        var act = () =>
            fixture.Sut.RotateSessionAsync(context);

        await act.Should()
            .ThrowAsync<SecurityException>();
    }

    [Fact]
    public async Task RotateSessionAsync_WhenChainIsRevoked_ShouldThrowSecurityException()
    {
        var fixture = CreateFixture();
        var context = CreateRotationContext();

        var root = CreateRoot(
            context.Tenant,
            context.UserKey);

        var chain = CreateChain(
            root,
            context.UserKey,
            context.Tenant);

        var session = CreateSession(
            context.UserKey,
            chain.ChainId,
            Now.AddHours(-1),
            context.CurrentSessionId,
            root.SecurityVersion);

        chain = chain.Revoke(Now);

        SetupRoot(fixture, context.UserKey, root);
        SetupSession(fixture, session);
        SetupChain(fixture, chain);

        var act = () =>
            fixture.Sut.RotateSessionAsync(context);

        await act.Should()
            .ThrowAsync<SecurityException>();
    }

    [Fact]
    public async Task RotateSessionAsync_WhenChainBelongsToDifferentUser_ShouldThrowSecurityException()
    {
        var fixture = CreateFixture();
        var context = CreateRotationContext();

        var root = CreateRoot(
            context.Tenant,
            context.UserKey);

        var otherUser = UserKey.New();

        var chain = CreateChain(
            root,
            otherUser,
            context.Tenant);

        var session = CreateSession(
            context.UserKey,
            chain.ChainId,
            Now.AddHours(-1),
            context.CurrentSessionId,
            root.SecurityVersion);

        SetupRoot(fixture, context.UserKey, root);
        SetupSession(fixture, session);
        SetupChain(fixture, chain);

        var act = () =>
            fixture.Sut.RotateSessionAsync(context);

        await act.Should()
            .ThrowAsync<SecurityException>();
    }

    [Fact]
    public async Task RotateSessionAsync_WhenChainBelongsToDifferentRoot_ShouldThrowSecurityException()
    {
        var fixture = CreateFixture();
        var context = CreateRotationContext();

        var activeRoot = CreateRoot(
            context.Tenant,
            context.UserKey);

        var otherRoot = CreateRoot(
            context.Tenant,
            context.UserKey);

        var chain = CreateChain(
            otherRoot,
            context.UserKey,
            context.Tenant);

        var session = CreateSession(
            context.UserKey,
            chain.ChainId,
            Now.AddHours(-1),
            context.CurrentSessionId,
            activeRoot.SecurityVersion);

        SetupRoot(
            fixture,
            context.UserKey,
            activeRoot);

        SetupSession(fixture, session);
        SetupChain(fixture, chain);

        var act = () =>
            fixture.Sut.RotateSessionAsync(context);

        await act.Should()
            .ThrowAsync<SecurityException>();
    }

    // =====================================================================
    // RotateSessionAsync - success
    // =====================================================================

    [Fact]
    public async Task RotateSessionAsync_ShouldCreateNewSessionOnSameChain()
    {
        var fixture = CreateFixture();
        var context = CreateRotationContext();

        var setup =
            SetupSuccessfulRotation(
                fixture,
                context);

        UAuthSession? persistedSession = null;

        fixture.Store
            .Setup(x => x.CreateSessionAsync(
                It.IsAny<UAuthSession>(),
                It.IsAny<CancellationToken>()))
            .Callback<UAuthSession, CancellationToken>(
                (session, _) =>
                    persistedSession = session)
            .Returns(Task.CompletedTask);

        var result =
            await fixture.Sut.RotateSessionAsync(context);

        persistedSession.Should().NotBeNull();

        persistedSession!.SessionId.Should()
            .NotBe(context.CurrentSessionId);

        persistedSession.ChainId.Should()
            .Be(setup.Chain.ChainId);

        result.OpaqueSessionId.Should()
            .Be(OpaqueToken);
    }

    [Fact]
    public async Task RotateSessionAsync_ShouldRotateChainActiveSession()
    {
        var fixture = CreateFixture();
        var context = CreateRotationContext();

        var setup =
            SetupSuccessfulRotation(
                fixture,
                context);

        UAuthSessionChain? savedChain = null;

        fixture.Store
            .Setup(x => x.SaveChainAsync(
                It.IsAny<UAuthSessionChain>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()))
            .Callback<UAuthSessionChain, long, CancellationToken>(
                (chain, _, _) => savedChain = chain)
            .Returns(Task.CompletedTask);

        await fixture.Sut.RotateSessionAsync(context);

        savedChain.Should().NotBeNull();

        savedChain!.ActiveSessionId.Should()
            .NotBe(context.CurrentSessionId);

        savedChain.ActiveSessionId.Should()
            .NotBeNull();
    }

    [Fact]
    public async Task RotateSessionAsync_ShouldRevokeOldSession()
    {
        var fixture = CreateFixture();
        var context = CreateRotationContext();

        var setup =
            SetupSuccessfulRotation(
                fixture,
                context);

        UAuthSession? savedOldSession = null;
        long? expectedVersion = null;

        fixture.Store
            .Setup(x => x.SaveSessionAsync(
                It.IsAny<UAuthSession>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()))
            .Callback<UAuthSession, long, CancellationToken>(
                (session, version, _) =>
                {
                    savedOldSession = session;
                    expectedVersion = version;
                })
            .Returns(Task.CompletedTask);

        await fixture.Sut.RotateSessionAsync(context);

        savedOldSession.Should().NotBeNull();
        savedOldSession!.IsRevoked.Should().BeTrue();

        expectedVersion.Should()
            .Be(setup.OldSession.Version);
    }

    [Fact]
    public async Task RotateSessionAsync_ShouldReturnSameBoundSessionThatWasPersisted()
    {
        var fixture = CreateFixture();
        var context = CreateRotationContext();

        var setup =
            SetupSuccessfulRotation(
                fixture,
                context);

        UAuthSession? persistedSession = null;

        fixture.Store
            .Setup(x => x.CreateSessionAsync(
                It.IsAny<UAuthSession>(),
                It.IsAny<CancellationToken>()))
            .Callback<UAuthSession, CancellationToken>(
                (session, _) =>
                    persistedSession = session)
            .Returns(Task.CompletedTask);

        var result = await fixture.Sut.RotateSessionAsync(context);

        persistedSession.Should().NotBeNull();
        persistedSession!.ChainId.Should().Be(setup.Chain.ChainId);
        result.Session.ChainId.Should().Be(setup.Chain.ChainId);
        result.Session.SessionId.Should().Be(persistedSession.SessionId);
        result.Session.ChainId.Should().Be(persistedSession.ChainId);
    }

    [Fact]
    public async Task RotateSessionAsync_WhenModeIsSemiHybrid_ShouldMarkResultAsMetadataOnly()
    {
        var fixture = CreateFixture();

        var context = CreateRotationContext(
            mode: UAuthMode.SemiHybrid);

        SetupSuccessfulRotation(
            fixture,
            context);

        var result =
            await fixture.Sut.RotateSessionAsync(context);

        result.IsMetadataOnly.Should().BeTrue();
    }

    [Fact]
    public async Task RotateSessionAsync_WhenMaxLifetimeIsShorter_ShouldCapNewSessionExpiration()
    {
        var fixture = CreateFixture(
            lifetime: TimeSpan.FromHours(8),
            maxLifetime: TimeSpan.FromHours(2));

        var context = CreateRotationContext();

        SetupSuccessfulRotation(
            fixture,
            context);

        UAuthSession? persisted = null;

        fixture.Store
            .Setup(x => x.CreateSessionAsync(
                It.IsAny<UAuthSession>(),
                It.IsAny<CancellationToken>()))
            .Callback<UAuthSession, CancellationToken>(
                (session, _) => persisted = session)
            .Returns(Task.CompletedTask);

        await fixture.Sut.RotateSessionAsync(context);

        persisted.Should().NotBeNull();

        persisted!.ExpiresAt.Should()
            .Be(Now.AddHours(2));
    }

    // =====================================================================
    // RevokeSessionAsync
    // =====================================================================

    [Fact]
    public async Task RevokeSessionAsync_ShouldDelegateToTenantStoreAndReturnResult()
    {
        var fixture = CreateFixture();

        var sessionId =
            AuthSessionId.Parse(OpaqueToken, null);

        fixture.Store
            .Setup(x => x.RevokeSessionAsync(
                sessionId,
                Now,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result =
            await fixture.Sut.RevokeSessionAsync(
                Tenant,
                sessionId,
                Now);

        result.Should().BeTrue();

        fixture.StoreFactory.Verify(
            x => x.Create(Tenant),
            Times.Once);
    }

    // =====================================================================
    // RevokeChainAsync
    // =====================================================================

    [Fact]
    public async Task RevokeChainAsync_WhenChainDoesNotExist_ShouldDoNothing()
    {
        var fixture = CreateFixture();
        var chainId = SessionChainId.New();

        fixture.Store
            .Setup(x => x.GetChainAsync(
                chainId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((UAuthSessionChain?)null);

        await fixture.Sut.RevokeChainAsync(
            Tenant,
            chainId,
            Now);

        fixture.Store.Verify(
            x => x.RevokeChainCascadeAsync(
                It.IsAny<SessionChainId>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RevokeChainAsync_WhenChainExists_ShouldCascadeRevoke()
    {
        var fixture = CreateFixture();

        var user = UserKey.New();
        var root = CreateRoot(Tenant, user);
        var chain = CreateChain(root, user, Tenant);

        SetupChain(fixture, chain);

        fixture.Store
            .Setup(x => x.RevokeChainCascadeAsync(
                chain.ChainId,
                Now,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await fixture.Sut.RevokeChainAsync(
            Tenant,
            chain.ChainId,
            Now);

        fixture.Store.Verify(
            x => x.RevokeChainCascadeAsync(
                chain.ChainId,
                Now,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // =====================================================================
    // RevokeAllChainsAsync
    // =====================================================================

    [Fact]
    public async Task RevokeAllChainsAsync_ShouldRevokeEveryUserChain()
    {
        var fixture = CreateFixture();

        var user = UserKey.New();
        var root = CreateRoot(Tenant, user);

        var chain1 = CreateChain(root, user, Tenant);
        var chain2 = CreateChain(root, user, Tenant);

        fixture.Store
            .Setup(x => x.GetChainsByUserAsync(
                user,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { chain1, chain2 });

        fixture.Store
            .Setup(x => x.RevokeChainCascadeAsync(
                It.IsAny<SessionChainId>(),
                Now,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await fixture.Sut.RevokeAllChainsAsync(
            Tenant,
            user,
            exceptChainId: null,
            Now);

        fixture.Store.Verify(
            x => x.RevokeChainCascadeAsync(
                chain1.ChainId,
                Now,
                It.IsAny<CancellationToken>()),
            Times.Once);

        fixture.Store.Verify(
            x => x.RevokeChainCascadeAsync(
                chain2.ChainId,
                Now,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RevokeAllChainsAsync_WhenExceptChainProvided_ShouldPreserveThatChain()
    {
        var fixture = CreateFixture();

        var user = UserKey.New();
        var root = CreateRoot(Tenant, user);

        var keep = CreateChain(root, user, Tenant);
        var revoke = CreateChain(root, user, Tenant);

        fixture.Store
            .Setup(x => x.GetChainsByUserAsync(
                user,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { keep, revoke });

        fixture.Store
            .Setup(x => x.RevokeChainCascadeAsync(
                revoke.ChainId,
                Now,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await fixture.Sut.RevokeAllChainsAsync(
            Tenant,
            user,
            keep.ChainId,
            Now);

        fixture.Store.Verify(
            x => x.RevokeChainCascadeAsync(
                keep.ChainId,
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        fixture.Store.Verify(
            x => x.RevokeChainCascadeAsync(
                revoke.ChainId,
                Now,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // =====================================================================
    // RevokeRootAsync
    // =====================================================================

    [Fact]
    public async Task RevokeRootAsync_ShouldCascadeRevokeRoot()
    {
        var fixture = CreateFixture();
        var user = UserKey.New();

        fixture.Store
            .Setup(x => x.RevokeRootCascadeAsync(
                user,
                Now,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await fixture.Sut.RevokeRootAsync(
            Tenant,
            user,
            Now);

        fixture.Store.Verify(
            x => x.RevokeRootCascadeAsync(
                user,
                Now,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // =====================================================================
    // GetChainIdBySessionAsync
    // =====================================================================

    [Fact]
    public async Task GetChainIdBySessionAsync_ShouldReturnStoreResult()
    {
        var fixture = CreateFixture();

        var sessionId =
            AuthSessionId.Parse(OpaqueToken, null);

        var chainId =
            SessionChainId.New();

        fixture.Store
            .Setup(x => x.GetChainIdBySessionAsync(
                sessionId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(chainId);

        var result =
            await fixture.Sut.GetChainIdBySessionAsync(
                Tenant,
                sessionId);

        result.Should().Be(chainId);
    }

    // =====================================================================
    // LogoutChainAsync
    // =====================================================================

    [Fact]
    public async Task LogoutChainAsync_WhenChainDoesNotExist_ShouldReturnFalse()
    {
        var fixture = CreateFixture();
        var chainId = SessionChainId.New();

        fixture.Store
            .Setup(x => x.GetChainAsync(
                chainId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((UAuthSessionChain?)null);

        var result =
            await fixture.Sut.LogoutChainAsync(
                Tenant,
                chainId,
                Now);

        result.Should().BeFalse();

        fixture.Store.Verify(
            x => x.LogoutChainAsync(
                It.IsAny<SessionChainId>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task LogoutChainAsync_WhenChainIsRevoked_ShouldReturnFalse()
    {
        var fixture = CreateFixture();

        var user = UserKey.New();
        var root = CreateRoot(Tenant, user);

        var chain = CreateChain(
            root,
            user,
            Tenant)
            .Revoke(Now);

        SetupChain(fixture, chain);

        var result =
            await fixture.Sut.LogoutChainAsync(
                Tenant,
                chain.ChainId,
                Now);

        result.Should().BeFalse();

        fixture.Store.Verify(
            x => x.LogoutChainAsync(
                It.IsAny<SessionChainId>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task LogoutChainAsync_WhenChainIsActive_ShouldLogoutAndReturnTrue()
    {
        var fixture = CreateFixture();

        var user = UserKey.New();
        var root = CreateRoot(Tenant, user);
        var chain = CreateChain(root, user, Tenant);

        SetupChain(fixture, chain);

        fixture.Store
            .Setup(x => x.LogoutChainAsync(
                chain.ChainId,
                Now,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result =
            await fixture.Sut.LogoutChainAsync(
                Tenant,
                chain.ChainId,
                Now);

        result.Should().BeTrue();

        fixture.Store.Verify(
            x => x.LogoutChainAsync(
                chain.ChainId,
                Now,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // =====================================================================
    // Cancellation / transaction
    // =====================================================================

    [Fact]
    public async Task IssueSessionAsync_ShouldPassCancellationTokenToTransaction()
    {
        var fixture = CreateFixture();
        var context = CreateIssuanceContext();

        SetupNewIssuance(fixture, context);

        using var cts =
            new CancellationTokenSource();

        await fixture.Sut.IssueSessionAsync(
            context,
            cts.Token);

        fixture.Store.Verify(
            x => x.ExecuteAsync(
                It.IsAny<Func<CancellationToken, Task>>(),
                cts.Token),
            Times.Once);
    }

    [Fact]
    public async Task RotateSessionAsync_ShouldPassCancellationTokenToTransaction()
    {
        var fixture = CreateFixture();
        var context = CreateRotationContext();

        SetupSuccessfulRotation(
            fixture,
            context);

        using var cts =
            new CancellationTokenSource();

        await fixture.Sut.RotateSessionAsync(
            context,
            cts.Token);

        fixture.Store.Verify(
            x => x.ExecuteAsync(
                It.IsAny<Func<CancellationToken, Task>>(),
                cts.Token),
            Times.Once);
    }

    // =====================================================================
    // Fixture
    // =====================================================================

    private static Fixture CreateFixture(
        string opaqueToken = OpaqueToken,
        TimeSpan? lifetime = null,
        TimeSpan? maxLifetime = null,
        int maxSessionsPerChain = 5)
    {
        var storeFactory =
            new Mock<ISessionStoreFactory>(
                MockBehavior.Strict);

        var store =
            new Mock<ISessionStore>(
                MockBehavior.Loose);

        var tokenGenerator =
            new Mock<IOpaqueTokenGenerator>(
                MockBehavior.Strict);

        tokenGenerator
            .Setup(x => x.Generate())
            .Returns(opaqueToken);

        storeFactory
            .Setup(x => x.Create(
                It.IsAny<TenantKey>()))
            .Returns(store.Object);

        store
            .Setup(x => x.ExecuteAsync(
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>(
                (action, ct) => action(ct));

        store
            .Setup(x => x.ExecuteAsync(
                It.IsAny<Func<CancellationToken, Task<bool>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<bool>>, CancellationToken>(
                (action, ct) => action(ct));

        store
            .Setup(x => x.ExecuteAsync(
                It.IsAny<Func<CancellationToken, Task<SessionChainId?>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<SessionChainId?>>, CancellationToken>(
                (action, ct) => action(ct));

        var options =
            new UAuthServerOptions();

        options.Session.Lifetime =
            lifetime ?? Lifetime;

        options.Session.MaxLifetime =
            maxLifetime;

        options.Session.MaxSessionsPerChain =
            maxSessionsPerChain;

        var sut =
            new UAuthSessionIssuer(
                storeFactory.Object,
                tokenGenerator.Object,
                Options.Create(options));

        return new Fixture(
            sut,
            storeFactory,
            store,
            tokenGenerator);
    }

    // =====================================================================
    // Setup helpers
    // =====================================================================

    private static void SetupNewIssuance(
        Fixture fixture,
        SessionIssuanceContext context)
    {
        var root =
            CreateRoot(
                context.Tenant,
                context.UserKey);

        SetupRoot(
            fixture,
            context.UserKey,
            root);

        SetupEmptySessions(fixture);
        SetupSessionAndChainPersistence(fixture);
    }

    private static RotationSetup SetupSuccessfulRotation(
        Fixture fixture,
        SessionRotationContext context)
    {
        var root =
            CreateRoot(
                context.Tenant,
                context.UserKey);

        var chain =
            CreateChain(
                root,
                context.UserKey,
                context.Tenant);

        var oldSession =
            CreateSession(
                context.UserKey,
                chain.ChainId,
                Now.AddHours(-1),
                context.CurrentSessionId,
                root.SecurityVersion);

        SetupRoot(
            fixture,
            context.UserKey,
            root);

        SetupSession(
            fixture,
            oldSession);

        SetupChain(
            fixture,
            chain);

        fixture.Store
            .Setup(x => x.CreateSessionAsync(
                It.IsAny<UAuthSession>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        fixture.Store
            .Setup(x => x.SaveChainAsync(
                It.IsAny<UAuthSessionChain>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        fixture.Store
            .Setup(x => x.SaveSessionAsync(
                It.IsAny<UAuthSession>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return new RotationSetup(
            root,
            chain,
            oldSession);
    }

    private static void SetupRoot(
        Fixture fixture,
        UserKey userKey,
        UAuthSessionRoot root)
    {
        fixture.Store
            .Setup(x => x.GetActiveRootByUserAsync(
                userKey,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(root);
    }

    private static void SetupSession(
        Fixture fixture,
        UAuthSession session)
    {
        fixture.Store
            .Setup(x => x.GetSessionAsync(
                session.SessionId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
    }

    private static void SetupChain(
        Fixture fixture,
        UAuthSessionChain chain)
    {
        fixture.Store
            .Setup(x => x.GetChainAsync(
                chain.ChainId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(chain);
    }

    private static void SetupEmptySessions(
        Fixture fixture)
    {
        fixture.Store
            .Setup(x => x.GetSessionsByChainAsync(
                It.IsAny<SessionChainId>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                Array.Empty<UAuthSession>());
    }

    private static void SetupSessionPersistence(
        Fixture fixture)
    {
        fixture.Store
            .Setup(x => x.CreateSessionAsync(
                It.IsAny<UAuthSession>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private static void SetupChainSave(
        Fixture fixture)
    {
        fixture.Store
            .Setup(x => x.SaveChainAsync(
                It.IsAny<UAuthSessionChain>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private static void SetupSessionAndChainPersistence(
        Fixture fixture)
    {
        fixture.Store
            .Setup(x => x.CreateChainAsync(
                It.IsAny<UAuthSessionChain>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        SetupSessionPersistence(fixture);
        SetupChainSave(fixture);
    }

    // =====================================================================
    // Domain helpers
    // =====================================================================

    private static SessionIssuanceContext CreateIssuanceContext(
        UserKey? userKey = null,
        SessionChainId? chainId = null,
        UAuthMode mode = UAuthMode.Hybrid)
    {
        return new SessionIssuanceContext
        {
            Tenant = Tenant,
            UserKey = userKey ?? UserKey.New(),
            Device = TestDevice.Default(),
            Now = Now,
            Claims = ClaimsSnapshot.Empty,
            Metadata = SessionMetadata.Empty,
            Mode = mode,
            ChainId = chainId
        };
    }

    private static SessionRotationContext CreateRotationContext(
        UAuthMode mode = UAuthMode.Hybrid)
    {
        return new SessionRotationContext
        {
            Tenant = Tenant,
            CurrentSessionId =
                AuthSessionId.Parse(
                    "current-session-000000000000000000000000001",
                    null),
            UserKey = UserKey.New(),
            Now = Now,
            Device = TestDevice.Default(),
            Claims = ClaimsSnapshot.Empty,
            Metadata = SessionMetadata.Empty,
            Mode = mode
        };
    }

    private static UAuthSessionRoot CreateRoot(
        TenantKey tenant,
        UserKey userKey)
    {
        return UAuthSessionRoot.Create(
            tenant,
            userKey,
            Now.AddDays(-1));
    }

    private static UAuthSessionChain CreateChain(
        UAuthSessionRoot root,
        UserKey userKey,
        TenantKey tenant)
    {
        return UAuthSessionChain.Create(
            SessionChainId.New(),
            root.RootId,
            tenant,
            userKey,
            Now.AddHours(-2),
            Now.AddDays(7),
            TestDevice.Default(),
            ClaimsSnapshot.Empty,
            root.SecurityVersion);
    }

    private static UAuthSession CreateSession(
        UserKey userKey,
        SessionChainId chainId,
        DateTimeOffset createdAt,
        AuthSessionId? sessionId = null,
        long securityVersion = 0)
    {
        return UAuthSession.Create(
            sessionId ?? AuthSessionId.Parse(Guid.NewGuid().ToString(), null),
            Tenant,
            userKey,
            chainId,
            createdAt,
            Now.AddHours(4),
            securityVersion,
            TestDevice.Default(),
            ClaimsSnapshot.Empty,
            SessionMetadata.Empty);
    }

    private sealed record RotationSetup(
        UAuthSessionRoot Root,
        UAuthSessionChain Chain,
        UAuthSession OldSession);

    private sealed record Fixture(
        UAuthSessionIssuer Sut,
        Mock<ISessionStoreFactory> StoreFactory,
        Mock<ISessionStore> Store,
        Mock<IOpaqueTokenGenerator> TokenGenerator);
}
