using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Client.Authentication;
using CodeBeam.UltimateAuth.Client.Events;
using CodeBeam.UltimateAuth.Client.Services;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using FluentAssertions;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class UAuthStateManagerTests
{
    [Fact]
    public async Task EnsureAsync_should_not_validate_when_authenticated_and_not_stale()
    {
        var snapshot = new AuthStateSnapshot
        {
            Identity = new AuthIdentitySnapshot
            {
                UserKey = UserKey.FromString("user"),
                Tenant = TenantKey.Single
            },
            Claims = ClaimsSnapshot.Empty
        };

        var flowClient = new Mock<IFlowClient>();
        flowClient
            .Setup(x => x.ValidateAsync())
            .ReturnsAsync(new AuthValidationResult
            {
                State = SessionState.Active,
                Snapshot = snapshot
            });

        var client = new Mock<IUAuthClient>();
        client.Setup(x => x.Flows).Returns(flowClient.Object);

        var events = new Mock<IUAuthClientEvents>();
        var clock = new Mock<IClock>();
        clock.Setup(x => x.UtcNow).Returns(DateTimeOffset.UtcNow);

        var manager = new UAuthStateManager(client.Object, events.Object, clock.Object);

        await manager.EnsureAsync();
        await manager.EnsureAsync();

        flowClient.Verify(x => x.ValidateAsync(), Times.Once);
    }

    [Fact]
    public async Task EnsureAsync_should_deduplicate_concurrent_calls()
    {
        var flows = new Mock<IFlowClient>();

        flows.Setup(x => x.ValidateAsync())
            .Returns(async () =>
            {
                await Task.Delay(50);
                return new AuthValidationResult { State = SessionState.Invalid };
            });

        var client = new Mock<IUAuthClient>();
        client.SetupGet(x => x.Flows).Returns(flows.Object);

        var events = new Mock<IUAuthClientEvents>();
        var clock = new Mock<IClock>();

        var manager = new UAuthStateManager(client.Object, events.Object, clock.Object);

        await Task.WhenAll(
            manager.EnsureAsync(true),
            manager.EnsureAsync(true),
            manager.EnsureAsync(true)
        );

        flows.Verify(x => x.ValidateAsync(), Times.Once);
    }

    [Fact]
    public async Task EnsureAsync_invalid_should_clear_state()
    {
        var client = new Mock<IUAuthClient>();
        var events = new Mock<IUAuthClientEvents>();
        var clock = new Mock<IClock>();

        client.Setup(x => x.Flows.ValidateAsync())
            .ReturnsAsync(new AuthValidationResult
            {
                State = SessionState.Invalid
            });

        var manager = new UAuthStateManager(client.Object, events.Object, clock.Object);
        await manager.EnsureAsync();
        manager.State.IsAuthenticated.Should().BeFalse();
    }
}
