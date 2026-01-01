using System;
using CodeBeam.UltimateAuth.Client.Contracts;
using CodeBeam.UltimateAuth.Client.Diagnostics;
using Xunit;

namespace CodeBeam.UltimateAuth.Tests.Unit.Client
{
    public class ClientDiagnosticsTests
    {
        [Fact]
        public void MarkStarted_ShouldSetStartedAt_AndIncrementCounter()
        {
            var diagnostics = new UAuthClientDiagnostics();
            diagnostics.MarkStarted();

            Assert.NotNull(diagnostics.StartedAt);
            Assert.Equal(1, diagnostics.StartCount);
            Assert.False(diagnostics.IsTerminated);
        }

        [Fact]
        public void MarkStopped_ShouldSetStoppedAt_AndIncrementCounter()
        {
            var diagnostics = new UAuthClientDiagnostics();
            diagnostics.MarkStopped();

            Assert.NotNull(diagnostics.StoppedAt);
            Assert.Equal(1, diagnostics.StopCount);
        }

        [Fact]
        public void MarkManualRefresh_ShouldIncrementManualAndTotalCounters()
        {
            var diagnostics = new UAuthClientDiagnostics();
            diagnostics.MarkManualRefresh();

            Assert.Equal(1, diagnostics.RefreshAttemptCount);
            Assert.Equal(1, diagnostics.ManualRefreshCount);
            Assert.Equal(0, diagnostics.AutomaticRefreshCount);
        }

        [Fact]
        public void MarkAutomaticRefresh_ShouldIncrementAutomaticAndTotalCounters()
        {
            var diagnostics = new UAuthClientDiagnostics();
            diagnostics.MarkAutomaticRefresh();

            Assert.Equal(1, diagnostics.RefreshAttemptCount);
            Assert.Equal(1, diagnostics.AutomaticRefreshCount);
            Assert.Equal(0, diagnostics.ManualRefreshCount);
        }

        [Fact]
        public void MarkRefreshOutcomes_ShouldIncrementCorrectCounters()
        {
            var diagnostics = new UAuthClientDiagnostics();
            diagnostics.MarkRefreshTouched();
            diagnostics.MarkRefreshNoOp();
            diagnostics.MarkRefreshReauthRequired();
            diagnostics.MarkRefreshUnknown();

            Assert.Equal(1, diagnostics.RefreshTouchedCount);
            Assert.Equal(1, diagnostics.RefreshNoOpCount);
            Assert.Equal(1, diagnostics.RefreshReauthRequiredCount);
            Assert.Equal(1, diagnostics.RefreshUnknownCount);
        }

        [Fact]
        public void MarkTerminated_ShouldSetTerminationState_AndIncrementCounter()
        {
            var diagnostics = new UAuthClientDiagnostics();
            diagnostics.MarkTerminated(CoordinatorTerminationReason.ReauthRequired);

            Assert.True(diagnostics.IsTerminated);
            Assert.Equal(CoordinatorTerminationReason.ReauthRequired, diagnostics.TerminationReason);
            Assert.Equal(1, diagnostics.TerminatedCount);
        }

        [Fact]
        public void ChangedEvent_ShouldFire_OnStateChanges()
        {
            var diagnostics = new UAuthClientDiagnostics();
            int changeCount = 0;

            diagnostics.Changed += () => changeCount++;

            diagnostics.MarkStarted();
            diagnostics.MarkManualRefresh();
            diagnostics.MarkRefreshTouched();
            diagnostics.MarkTerminated(CoordinatorTerminationReason.ReauthRequired);

            Assert.Equal(4, changeCount);
        }

        [Fact]
        public void MarkTerminated_ShouldBeIdempotent_ForStateButCountShouldIncrease()
        {
            var diagnostics = new UAuthClientDiagnostics();
            diagnostics.MarkTerminated(CoordinatorTerminationReason.ReauthRequired);
            diagnostics.MarkTerminated(CoordinatorTerminationReason.ReauthRequired);

            Assert.True(diagnostics.IsTerminated);
            Assert.Equal(2, diagnostics.TerminatedCount);
        }
    }
}
