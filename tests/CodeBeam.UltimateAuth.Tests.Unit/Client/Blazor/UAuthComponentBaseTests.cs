using AwesomeAssertions;
using Bunit;
using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Client.Blazor;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace CodeBeam.UltimateAuth.Tests.Unit.Client.Blazor;

public sealed class UAuthComponentBaseTests : BunitContext
{
    [Fact]
    public void Render_WithoutUAuthState_ThrowsInvalidOperationException()
    {
        var act = () => Render<TestComponent>();

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*requires a cascading parameter*UAuthState*");
    }

    [Fact]
    public void Render_WithUAuthState_ReceivesCascadingState()
    {
        var state = AuthenticatedState();

        var cut = RenderWithState<TestComponent>(state);
        var component = GetComponent<TestComponent>(cut);

        component.CurrentAuthState.Should().BeSameAs(state);
    }

    [Fact]
    public void InitialRender_DoesNotInvokeStateChangedHandler()
    {
        var state = AuthenticatedState();

        var cut = RenderWithState<TestComponent>(state);
        var component = GetComponent<TestComponent>(cut);

        component.StateChangedCount.Should().Be(0);
        component.LastChangeReason.Should().BeNull();
        component.RenderCount.Should().Be(1);
    }

    [Fact]
    public void StateChange_ForwardsReasonToComponent()
    {
        var state = AuthenticatedState();

        var cut = RenderWithState<TestComponent>(state);
        var component = GetComponent<TestComponent>(cut);

        state.MarkStale();

        cut.WaitForAssertion(() =>
        {
            component.StateChangedCount.Should().Be(1);
            component.LastChangeReason
                .Should().Be(UAuthStateChangeReason.MarkedStale);
        });
    }

    [Fact]
    public void StateChange_TriggersAutomaticRender()
    {
        var state = AuthenticatedState();

        var cut = RenderWithState<TestComponent>(state);
        var component = GetComponent<TestComponent>(cut);

        var initialRenderCount = component.RenderCount;

        state.MarkStale();

        cut.WaitForAssertion(() =>
        {
            component.StateChangedCount.Should().Be(1);
            component.RenderCount.Should().BeGreaterThan(initialRenderCount);
        });
    }

    [Fact]
    public void StateChange_WhenAutoRefreshDisabled_DoesNotRender()
    {
        var state = AuthenticatedState();

        var cut = RenderWithState<NoAutoRefreshComponent>(state);
        var component = GetComponent<NoAutoRefreshComponent>(cut);

        var initialRenderCount = component.RenderCount;

        state.MarkStale();

        cut.WaitForAssertion(() =>
        {
            component.StateChangedCount.Should().Be(1);
        });

        component.RenderCount.Should().Be(initialRenderCount);
    }

    [Fact]
    public async Task StateChange_AwaitsAsyncHandlerBeforeRendering()
    {
        var state = AuthenticatedState();

        var cut = RenderWithState<AsyncComponent>(state);
        var component = GetComponent<AsyncComponent>(cut);

        var initialRenderCount = component.RenderCount;

        state.MarkStale();

        cut.WaitForAssertion(() =>
        {
            component.HandlerStarted.Should().BeTrue();
        });

        component.HandlerCompleted.Should().BeFalse();
        component.RenderCount.Should().Be(initialRenderCount);

        await cut.InvokeAsync(() =>
            component.CompleteHandler());

        cut.WaitForAssertion(() =>
        {
            component.HandlerCompleted.Should().BeTrue();
            component.RenderCount.Should().BeGreaterThan(initialRenderCount);
        });
    }

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        var state = AuthenticatedState();

        var cut = RenderWithState<TestComponent>(state);

        var act = () => cut.Dispose();

        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var state = AuthenticatedState();

        var cut = RenderWithState<TestComponent>(state);
        var component = GetComponent<TestComponent>(cut);

        var act = () =>
        {
            component.Dispose();
            component.Dispose();
            cut.Dispose();
        };

        act.Should().NotThrow();
        component.Disposed.Should().BeTrue();
    }

    [Fact]
    public void Dispose_UnsubscribesFromAuthState()
    {
        var state = AuthenticatedState();

        var cut = RenderWithState<TestComponent>(state);
        var component = GetComponent<TestComponent>(cut);

        state.MarkStale();

        cut.WaitForAssertion(() =>
        {
            component.StateChangedCount.Should().Be(1);
        });

        component.Dispose();

        state.MarkValidated(DateTimeOffset.UtcNow);

        component.StateChangedCount.Should().Be(1);
        component.LastChangeReason
            .Should().Be(UAuthStateChangeReason.MarkedStale);
    }

    [Fact]
    public void Dispose_DuringPendingHandler_DoesNotRenderAfterCompletion()
    {
        var state = AuthenticatedState();

        var cut = RenderWithState<AsyncComponent>(state);
        var component = GetComponent<AsyncComponent>(cut);

        state.MarkStale();

        cut.WaitForAssertion(() =>
        {
            component.HandlerStarted.Should().BeTrue();
        });

        component.Dispose();

        var renderCount = component.RenderCount;

        cut.InvokeAsync(() => component.CompleteHandler());

        cut.WaitForAssertion(() =>
        {
            component.HandlerCompleted.Should().BeTrue();
        });

        component.RenderCount.Should().Be(renderCount);
    }

    [Fact]
    public void StateChange_AfterDispose_DoesNotInvokeHandler()
    {
        var state = UAuthState.Anonymous();

        var cut = RenderWithState<TestComponent>(state);
        var component = GetComponent<TestComponent>(cut);

        component.Dispose();

        state.MarkStale();

        component.StateChangedCount.Should().Be(0);
        component.Disposed.Should().BeTrue();
    }


    [Fact]
    public void CascadingStateReplacement_UnsubscribesFromPreviousState()
    {
        var firstState = AuthenticatedState();
        var secondState = AuthenticatedState();

        var cut = Render<StateHost>(parameters => parameters
            .Add(x => x.State, firstState));

        var originalComponent = cut.FindComponent<TestComponent>().Instance;

        originalComponent.CurrentAuthState
            .Should().BeSameAs(firstState);

        cut.Render(parameters => parameters
            .Add(x => x.State, secondState));

        var component = cut.FindComponent<TestComponent>().Instance;

        // The same component instance should receive the new cascading state.
        component.Should().BeSameAs(originalComponent);

        component.CurrentAuthState
            .Should().BeSameAs(secondState);

        firstState.MarkStale();

        component.StateChangedCount.Should().Be(0);

        secondState.MarkStale();

        cut.WaitForAssertion(() =>
        {
            component.StateChangedCount.Should().Be(1);
            component.LastChangeReason
                .Should().Be(UAuthStateChangeReason.MarkedStale);
        });
    }

    [Fact]
    public void MarkStale_OnAuthenticatedState_RaisesChangedEvent()
    {
        var state = AuthenticatedState();

        var count = 0;
        UAuthStateChangeReason? reason = null;

        state.Changed += changeReason =>
        {
            count++;
            reason = changeReason;
        };

        state.MarkStale();

        count.Should().Be(1);
        reason.Should().Be(UAuthStateChangeReason.MarkedStale);
    }


    private IRenderedComponent<CascadingValue<UAuthState>> RenderWithState<TComponent>(UAuthState state) where TComponent : IComponent
    {
        return Render<CascadingValue<UAuthState>>(parameters =>
            parameters
                .Add(x => x.Value, state)
                .AddChildContent<TComponent>());
    }

    private static TComponent GetComponent<TComponent>(
        IRenderedComponent<CascadingValue<UAuthState>> cut)
        where TComponent : IComponent
    {
        return cut.FindComponent<TComponent>().Instance;
    }

    private sealed class StateHost : ComponentBase
    {
        [Parameter]
        public UAuthState State { get; set; } = default!;

        protected override void BuildRenderTree(
            RenderTreeBuilder builder)
        {
            builder.OpenComponent<CascadingValue<UAuthState>>(0);
            builder.AddAttribute(1, "Value", State);
            builder.AddAttribute(2, "ChildContent",
                (RenderFragment)(content =>
                {
                    content.OpenComponent<TestComponent>(0);
                    content.CloseComponent();
                }));
            builder.CloseComponent();
        }
    }

    private class TestComponent : UAuthComponentBase
    {
        public int StateChangedCount { get; private set; }

        public UAuthStateChangeReason? LastChangeReason
        { get; private set; }

        public int RenderCount { get; private set; }

        public UAuthState CurrentAuthState => AuthState;

        public bool Disposed => IsDisposed;

        protected override Task HandleAuthStateChangedAsync(
            UAuthStateChangeReason reason)
        {
            StateChangedCount++;
            LastChangeReason = reason;

            return Task.CompletedTask;
        }

        protected override void BuildRenderTree(
            RenderTreeBuilder builder)
        {
            RenderCount++;
            builder.AddContent(0, "uauth-test-component");
        }
    }

    private sealed class NoAutoRefreshComponent : TestComponent
    {
        protected override bool AutoRefreshOnAuthStateChanged => false;
    }

    private sealed class AsyncComponent : UAuthComponentBase
    {
        private readonly TaskCompletionSource<bool> _completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool HandlerStarted { get; private set; }

        public bool HandlerCompleted { get; private set; }

        public int RenderCount { get; private set; }

        protected override async Task HandleAuthStateChangedAsync(UAuthStateChangeReason reason)
        {
            HandlerStarted = true;

            await _completion.Task;

            HandlerCompleted = true;
        }

        public void CompleteHandler()
        {
            _completion.TrySetResult(true);
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            RenderCount++;
            builder.AddContent(0, "async-component");
        }
    }

    private static UAuthState AuthenticatedState()
    {
        var state = UAuthState.Anonymous();

        state.ApplySnapshot(
            new AuthStateSnapshot
            {
                Identity = new AuthIdentitySnapshot
                {
                    UserKey = UserKey.New(),
                    Tenant = TenantKey.Single,
                    PrimaryUserName = "alice"
                },
                Claims = ClaimsSnapshot.From([])
            },
            DateTimeOffset.UtcNow);

        return state;
    }
}
