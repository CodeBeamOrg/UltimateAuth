using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Services;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using FluentAssertions;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Unit.Server;

public sealed class UAuthSessionQueryServiceTests
{
    [Fact]
    public async Task GetSessionAsync_UsesCurrentTenant_AndDelegatesToStore()
    {
        var tenant = TenantKeys.Single;
        var sessionId = TestIds.Session("test-session");
        var ct = new CancellationTokenSource().Token;

        var store = new Mock<ISessionStore>(MockBehavior.Strict);
        var storeFactory = new Mock<ISessionStoreFactory>(MockBehavior.Strict);
        var authFlow = new Mock<IAuthFlowContextAccessor>(MockBehavior.Strict);

        var flow = AuthFlowTestFactory.New(tenant: tenant);

        authFlow
            .SetupGet(x => x.Current)
            .Returns(flow);

        storeFactory
            .Setup(x => x.Create(tenant))
            .Returns(store.Object);

        store
            .Setup(x => x.GetSessionAsync(sessionId, ct))
            .ReturnsAsync((UAuthSession?)null);

        var sut = new UAuthSessionQueryService(
            storeFactory.Object,
            authFlow.Object);

        var result = await sut.GetSessionAsync(sessionId, ct);

        result.Should().BeNull();

        storeFactory.Verify(
            x => x.Create(tenant),
            Times.Once);

        store.Verify(
            x => x.GetSessionAsync(sessionId, ct),
            Times.Once);
    }

    [Fact]
    public async Task GetSessionsByChainAsync_UsesCurrentTenant_AndDelegatesToStore()
    {
        var tenant = TenantKeys.Single;
        var chainId = SessionChainId.New();
        var ct = new CancellationTokenSource().Token;

        IReadOnlyList<UAuthSession> expected =
            Array.Empty<UAuthSession>();

        var store = new Mock<ISessionStore>(MockBehavior.Strict);
        var storeFactory = new Mock<ISessionStoreFactory>(MockBehavior.Strict);
        var authFlow = new Mock<IAuthFlowContextAccessor>(MockBehavior.Strict);

        authFlow
            .SetupGet(x => x.Current)
            .Returns(AuthFlowTestFactory.New(tenant: tenant));

        storeFactory
            .Setup(x => x.Create(tenant))
            .Returns(store.Object);

        store
            .Setup(x => x.GetSessionsByChainAsync(chainId, ct))
            .ReturnsAsync(expected);

        var sut = new UAuthSessionQueryService(
            storeFactory.Object,
            authFlow.Object);

        var result = await sut.GetSessionsByChainAsync(chainId, ct);

        result.Should().BeSameAs(expected);

        storeFactory.Verify(
            x => x.Create(tenant),
            Times.Once);

        store.Verify(
            x => x.GetSessionsByChainAsync(chainId, ct),
            Times.Once);
    }

    [Fact]
    public async Task GetChainsByUserAsync_UsesCurrentTenant_AndDelegatesToStore()
    {
        var tenant = TenantKeys.Single;
        var userKey = UserKey.New();
        var ct = new CancellationTokenSource().Token;

        IReadOnlyList<UAuthSessionChain> expected =
            Array.Empty<UAuthSessionChain>();

        var store = new Mock<ISessionStore>(MockBehavior.Strict);
        var storeFactory = new Mock<ISessionStoreFactory>(MockBehavior.Strict);
        var authFlow = new Mock<IAuthFlowContextAccessor>(MockBehavior.Strict);

        authFlow
            .SetupGet(x => x.Current)
            .Returns(AuthFlowTestFactory.New(tenant: tenant));

        storeFactory
            .Setup(x => x.Create(tenant))
            .Returns(store.Object);

        store
            .Setup(x => x.GetChainsByUserAsync(
                userKey,
                false,
                ct))
            .ReturnsAsync(expected);

        var sut = new UAuthSessionQueryService(
            storeFactory.Object,
            authFlow.Object);

        var result = await sut.GetChainsByUserAsync(userKey, ct);

        result.Should().BeSameAs(expected);

        storeFactory.Verify(
            x => x.Create(tenant),
            Times.Once);

        store.Verify(
            x => x.GetChainsByUserAsync(
                userKey,
                false,
                ct),
            Times.Once);
    }

    [Fact]
    public async Task ResolveChainIdAsync_UsesCurrentTenant_AndDelegatesToStore()
    {
        var tenant = TenantKeys.Single;
        var sessionId = TestIds.Session("test-session");
        var expectedChainId = SessionChainId.New();
        var ct = new CancellationTokenSource().Token;

        var store = new Mock<ISessionStore>(MockBehavior.Strict);
        var storeFactory = new Mock<ISessionStoreFactory>(MockBehavior.Strict);
        var authFlow = new Mock<IAuthFlowContextAccessor>(MockBehavior.Strict);

        authFlow
            .SetupGet(x => x.Current)
            .Returns(AuthFlowTestFactory.New(tenant: tenant));

        storeFactory
            .Setup(x => x.Create(tenant))
            .Returns(store.Object);

        store
            .Setup(x => x.GetChainIdBySessionAsync(sessionId, ct))
            .ReturnsAsync(expectedChainId);

        var sut = new UAuthSessionQueryService(
            storeFactory.Object,
            authFlow.Object);

        var result = await sut.ResolveChainIdAsync(sessionId, ct);

        result.Should().Be(expectedChainId);

        storeFactory.Verify(
            x => x.Create(tenant),
            Times.Once);

        store.Verify(
            x => x.GetChainIdBySessionAsync(sessionId, ct),
            Times.Once);
    }
}