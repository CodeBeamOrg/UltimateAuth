using AwesomeAssertions;
using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Client.Authentication;
using CodeBeam.UltimateAuth.Client.Events;
using CodeBeam.UltimateAuth.Client.Services;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Users.Contracts;
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
    public async Task EnsureAsync_Should_Deduplicate_InFlight_Calls_But_Allow_Later_Validation()
    {
        var (manager, flows, _, _) = CreateManager();

        using (manager)
        {
            var pendingValidation =
                new TaskCompletionSource<AuthValidationResult>(
                    TaskCreationOptions.RunContinuationsAsynchronously);

            flows.SetupSequence(x => x.ValidateAsync())
                .Returns(pendingValidation.Task)
                .ReturnsAsync(new AuthValidationResult
                {
                    State = SessionState.Active,
                    Snapshot = CreateSnapshot()
                });

            var first = manager.EnsureAsync(force: true);
            var second = manager.EnsureAsync(force: true);
            var third = manager.EnsureAsync(force: true);

            first.IsCompleted.Should().BeFalse();

            second.Should().BeSameAs(first);
            third.Should().BeSameAs(first);

            flows.Verify(
                x => x.ValidateAsync(),
                Times.Once);

            pendingValidation.SetResult(new AuthValidationResult
            {
                State = SessionState.Active,
                Snapshot = CreateSnapshot()
            });

            await Task.WhenAll(first, second, third);

            manager.State.IsAuthenticated.Should().BeTrue();

            await manager.EnsureAsync(force: true);

            flows.Verify(
                x => x.ValidateAsync(),
                Times.Exactly(2));
        }
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

    [Fact]
    public async Task OnLogin_Should_Mark_Authenticated_State_Stale()
    {
        var (manager, flows, _, _) = CreateManager();
        using (manager)
        {
            SetupValidSnapshot(flows);

            await manager.EnsureAsync();

            manager.State.IsAuthenticated.Should().BeTrue();
            manager.State.IsStale.Should().BeFalse();

            await manager.OnLoginAsync();

            manager.State.IsStale.Should().BeTrue();
            manager.NeedsValidation.Should().BeTrue();
        }
    }

    [Fact]
    public async Task OnLogin_When_Anonymous_Should_Still_Require_Validation()
    {
        var (manager, _, _, _) = CreateManager();
        using (manager)
        {
            await manager.OnLoginAsync();

            manager.State.IsAuthenticated.Should().BeFalse();
            manager.NeedsValidation.Should().BeTrue();
        }
    }

    [Fact]
    public async Task OnLogout_Should_Clear_Authenticated_State()
    {
        var (manager, flows, _, _) = CreateManager();
        using (manager)
        {
            SetupValidSnapshot(flows);

            await manager.EnsureAsync();
            await manager.OnLogoutAsync();

            manager.State.IsAuthenticated.Should().BeFalse();
            manager.State.Identity.Should().BeNull();
            manager.State.IsStale.Should().BeFalse();
            manager.NeedsValidation.Should().BeTrue();
        }
    }

    [Fact]
    public async Task MarkStale_Should_Cause_Next_Ensure_To_Validate()
    {
        var (manager, flows, _, _) = CreateManager();
        using (manager)
        {
            SetupValidSnapshot(flows);

            await manager.EnsureAsync();

            manager.MarkStale();

            await manager.EnsureAsync();

            flows.Verify(x => x.ValidateAsync(), Times.Exactly(2));
            manager.State.IsStale.Should().BeFalse();
        }
    }

    [Fact]
    public async Task Clear_Should_Require_Next_Validation()
    {
        var (manager, flows, _, _) = CreateManager();
        using (manager)
        {
            SetupValidSnapshot(flows);

            await manager.EnsureAsync();

            manager.Clear();

            manager.State.IsAuthenticated.Should().BeFalse();
            manager.NeedsValidation.Should().BeTrue();

            await manager.EnsureAsync();

            flows.Verify(x => x.ValidateAsync(), Times.Exactly(2));
        }
    }

    [Theory]
    [InlineData(UAuthStateEvent.SessionRevoked)]
    [InlineData(UAuthStateEvent.CredentialsChanged)]
    [InlineData(UAuthStateEvent.UserDeleted)]
    [InlineData(UAuthStateEvent.LogoutVariant)]
    public async Task State_Invalidating_Events_Should_Clear_State(UAuthStateEvent eventType)
    {
        var (manager, flows, events, _) = CreateManager();
        using (manager)
        {
            SetupValidSnapshot(flows);

            await manager.EnsureAsync();

            await events.PublishAsync(
                new UAuthStateEventArgsEmpty(
                    eventType,
                    UAuthStateEventHandlingMode.Patch));

            manager.State.IsAuthenticated.Should().BeFalse();
            manager.State.Identity.Should().BeNull();
        }
    }

    [Fact]
    public async Task ProfileChanged_Should_Patch_Current_Identity()
    {
        var (manager, flows, events, _) = CreateManager();
        using (manager)
        {
            SetupValidSnapshot(flows);

            await manager.EnsureAsync();

            await events.PublishAsync(
                new UAuthStateEventArgs<UpdateProfileRequest>(
                    UAuthStateEvent.ProfileChanged,
                    UAuthStateEventHandlingMode.Patch,
                    new UpdateProfileRequest
                    {
                        DisplayName = "Updated Name"
                    }));

            manager.State.Identity!.DisplayName
                .Should().Be("Updated Name");

            flows.Verify(x => x.ValidateAsync(), Times.Once);
        }
    }

    [Fact]
    public async Task UserStatusChanged_Should_Patch_Current_Identity()
    {
        var (manager, flows, events, _) = CreateManager();
        using (manager)
        {
            SetupValidSnapshot(flows);

            await manager.EnsureAsync();

            await events.PublishAsync(
                new UAuthStateEventArgs<ChangeUserStatusSelfRequest>(
                    UAuthStateEvent.UserStatusChanged,
                    UAuthStateEventHandlingMode.Patch,
                    new ChangeUserStatusSelfRequest
                    {
                        NewStatus = SelfAssignableUserStatus.SelfSuspended
                    }));

            manager.State.Identity!.UserStatus
                .Should().Be(UserStatus.SelfSuspended);

            flows.Verify(x => x.ValidateAsync(), Times.Once);
        }
    }

    [Fact]
    public async Task RefreshMode_None_Should_Ignore_Event()
    {
        var (manager, flows, events, _) = CreateManager();
        using (manager)
        {
            SetupValidSnapshot(flows);

            await manager.EnsureAsync();

            await events.PublishAsync(
                new UAuthStateEventArgsEmpty(
                    UAuthStateEvent.SessionRevoked,
                    UAuthStateEventHandlingMode.None));

            manager.State.IsAuthenticated.Should().BeTrue();
            manager.State.IsStale.Should().BeFalse();
        }
    }

    [Fact]
    public async Task RefreshMode_Validate_Should_Force_Validation()
    {
        var (manager, flows, events, _) = CreateManager();
        using (manager)
        {
            SetupValidSnapshot(flows);

            await manager.EnsureAsync();

            await events.PublishAsync(
                new UAuthStateEventArgsEmpty(
                    UAuthStateEvent.AuthorizationChanged,
                    UAuthStateEventHandlingMode.Validate));

            flows.Verify(x => x.ValidateAsync(), Times.Exactly(2));
        }
    }

    [Fact]
    public async Task ValidationCalled_With_Patch_Should_Mark_Validated()
    {
        var (manager, flows, events, clock) = CreateManager();
        using (manager)
        {
            SetupValidSnapshot(flows);

            await manager.EnsureAsync();

            manager.MarkStale();

            var newTime = new DateTimeOffset(
                2026, 10, 26, 12, 5, 0, TimeSpan.Zero);

            clock.SetupGet(x => x.UtcNow).Returns(newTime);

            await events.PublishAsync(
                new UAuthStateEventArgsEmpty(
                    UAuthStateEvent.ValidationCalled,
                    UAuthStateEventHandlingMode.Patch));

            manager.State.IsStale.Should().BeFalse();
            manager.State.LastValidatedAt.Should().Be(newTime);

            flows.Verify(x => x.ValidateAsync(), Times.Once);
        }
    }

    [Fact]
    public async Task AuthorizationChanged_With_Patch_Should_Mark_Stale()
    {
        var (manager, flows, events, _) = CreateManager();
        using (manager)
        {
            SetupValidSnapshot(flows);

            await manager.EnsureAsync();

            await events.PublishAsync(
                new UAuthStateEventArgsEmpty(
                    UAuthStateEvent.AuthorizationChanged,
                    UAuthStateEventHandlingMode.Patch));

            manager.State.IsStale.Should().BeTrue();
            manager.NeedsValidation.Should().BeTrue();
        }
    }

    [Fact]
    public async Task Dispose_Should_Unsubscribe_From_State_Events()
    {
        var (manager, flows, events, _) = CreateManager();

        SetupValidSnapshot(flows);

        await manager.EnsureAsync();

        manager.Dispose();

        await events.PublishAsync(
            new UAuthStateEventArgsEmpty(
                UAuthStateEvent.SessionRevoked,
                UAuthStateEventHandlingMode.Patch));

        manager.State.IsAuthenticated.Should().BeTrue();
    }

    [Fact]
    public async Task EnsureAsync_Should_Allow_Revalidation_After_Synchronous_Completion()
    {
        var (manager, flows, _, _) = CreateManager();

        using (manager)
        {
            SetupValidSnapshot(flows);
            await manager.EnsureAsync(force: true);
            await manager.EnsureAsync(force: true);
            await manager.EnsureAsync(force: true);
            flows.Verify(x => x.ValidateAsync(), Times.Exactly(3));
        }
    }

    [Theory]
    [InlineData(SessionState.Expired)]
    [InlineData(SessionState.Revoked)]
    [InlineData(SessionState.NotFound)]
    [InlineData(SessionState.Invalid)]
    [InlineData(SessionState.SecurityMismatch)]
    [InlineData(SessionState.DeviceMismatch)]
    [InlineData(SessionState.Unsupported)]
    public async Task EnsureAsync_When_Session_Is_Not_Active_Should_Clear_State(SessionState sessionState)
    {
        var (manager, flows, _, _) = CreateManager();

        using (manager)
        {
            SetupValidSnapshot(flows);

            await manager.EnsureAsync();

            manager.State.IsAuthenticated.Should().BeTrue();

            flows.Setup(x => x.ValidateAsync())
                .ReturnsAsync(new AuthValidationResult
                {
                    State = sessionState
                });

            await manager.EnsureAsync(force: true);

            manager.State.IsAuthenticated.Should().BeFalse();
            manager.State.Identity.Should().BeNull();
            manager.State.IsStale.Should().BeFalse();
            manager.NeedsValidation.Should().BeTrue();

            flows.Verify(
                x => x.ValidateAsync(),
                Times.Exactly(2));
        }
    }

    [Fact]
    public async Task EnsureAsync_Should_Allow_Retry_After_Validation_Exception()
    {
        var (manager, flows, _, _) = CreateManager();

        using (manager)
        {
            flows.SetupSequence(x => x.ValidateAsync())
                .ThrowsAsync(new HttpRequestException("Network failure"))
                .ReturnsAsync(new AuthValidationResult
                {
                    State = SessionState.Active,
                    Snapshot = CreateSnapshot()
                });

            var act = async () =>
                await manager.EnsureAsync(force: true);

            await act.Should()
                .ThrowAsync<HttpRequestException>()
                .WithMessage("Network failure");

            manager.State.IsAuthenticated.Should().BeFalse();

            await manager.EnsureAsync(force: true);

            manager.State.IsAuthenticated.Should().BeTrue();
            manager.State.IsStale.Should().BeFalse();

            flows.Verify(
                x => x.ValidateAsync(),
                Times.Exactly(2));
        }
    }

    [Fact]
    public async Task EnsureAsync_When_Network_Fails_Should_Preserve_Authenticated_State()
    {
        var (manager, flows, _, _) = CreateManager();

        using (manager)
        {
            SetupValidSnapshot(flows);

            await manager.EnsureAsync();

            var originalIdentity = manager.State.Identity;
            var originalValidatedAt = manager.State.LastValidatedAt;

            flows.Setup(x => x.ValidateAsync())
                .ThrowsAsync(
                    new HttpRequestException("Network unavailable"));

            var act = async () =>
                await manager.EnsureAsync(force: true);

            await act.Should().ThrowAsync<HttpRequestException>();

            manager.State.IsAuthenticated.Should().BeTrue();
            manager.State.Identity.Should().Be(originalIdentity);
            manager.State.LastValidatedAt.Should().Be(originalValidatedAt);
        }
    }

    [Fact]
    public async Task EnsureAsync_When_Active_Result_Has_No_Snapshot_Should_Mark_State_Stale()
    {
        var (manager, flows, _, _) = CreateManager();

        using (manager)
        {
            SetupValidSnapshot(flows);

            await manager.EnsureAsync();

            flows.Setup(x => x.ValidateAsync())
                .ReturnsAsync(new AuthValidationResult
                {
                    State = SessionState.Active,
                    Snapshot = null
                });

            await manager.EnsureAsync(force: true);

            manager.State.IsAuthenticated.Should().BeTrue();
            manager.State.IsStale.Should().BeTrue();
            manager.State.NeedsValidation.Should().BeTrue();
        }
    }


    private static AuthStateSnapshot CreateSnapshot(string userKey = "user-1", string displayName = "Original Name")
    {
        return new AuthStateSnapshot
        {
            Identity = new AuthIdentitySnapshot
            {
                UserKey = UserKey.FromString(userKey),
                Tenant = TenantKey.Single,
                DisplayName = displayName
            },
            Claims = ClaimsSnapshot.Empty
        };
    }

    private static (
        UAuthStateManager Manager,
        Mock<IFlowClient> Flows,
        UAuthClientEvents Events,
        Mock<IClock> Clock)
        CreateManager()
    {
        var flows = new Mock<IFlowClient>();

        var client = new Mock<IUAuthClient>();
        client.SetupGet(x => x.Flows).Returns(flows.Object);

        var events = new UAuthClientEvents();

        var clock = new Mock<IClock>();
        clock.SetupGet(x => x.UtcNow)
            .Returns(new DateTimeOffset(2026, 10, 26, 12, 0, 0, TimeSpan.Zero));

        return (
            new UAuthStateManager(client.Object, events, clock.Object),
            flows,
            events,
            clock);
    }

    private static void SetupValidSnapshot(
        Mock<IFlowClient> flows,
        AuthStateSnapshot? snapshot = null)
    {
        flows.Setup(x => x.ValidateAsync())
            .ReturnsAsync(new AuthValidationResult
            {
                State = SessionState.Active,
                Snapshot = snapshot ?? CreateSnapshot()
            });
    }
}
