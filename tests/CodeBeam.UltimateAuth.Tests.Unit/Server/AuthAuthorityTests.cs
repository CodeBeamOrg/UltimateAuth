using AwesomeAssertions;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Infrastructure;

namespace CodeBeam.UltimateAuth.Tests.Unit.Server;

public sealed class AuthAuthorityTests
{
    [Fact]
    public async Task DecideAsync_WithLegacyInvariant_ShouldWork()
    {
        var invariant = new LegacyInvariant();

        var authority = new AuthAuthority(
            new IAuthorityInvariant[] { invariant },
            Array.Empty<IAuthorityPolicy>());

        var result = await authority.DecideAsync(CreateContext());

        result.Decision.Should().Be(AuthorizationDecision.Deny);
        result.Reason.Should().Be("legacy_invariant_denied");
        invariant.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task DecideAsync_ShouldAwaitAsyncPolicy()
    {
        var policy = new DelayedPolicy();

        var authority = new AuthAuthority(
            Array.Empty<IAuthorityInvariant>(),
            new IAuthorityPolicy[] { policy });

        var evaluation = authority.DecideAsync(CreateContext());

        // Ensure the policy has started evaluating.
        await policy.Started;

        // The authority must not finish before the policy.
        evaluation.IsCompleted.Should().BeFalse();

        // Complete the asynchronous policy evaluation.
        policy.Complete(AccessDecisionResult.Deny("async_policy_denied"));

        var result = await evaluation;

        result.Decision.Should().Be(AuthorizationDecision.Deny);
        result.Reason.Should().Be("async_policy_denied");
    }

    [Fact]
    public async Task DecideAsync_ShouldForwardCancellationToken()
    {
        using var cts = new CancellationTokenSource();

        var invariant = new TokenCapturingInvariant();
        var policy = new TokenCapturingPolicy();

        var authority = new AuthAuthority(
            new IAuthorityInvariant[] { invariant },
            new IAuthorityPolicy[] { policy });

        var result = await authority.DecideAsync(CreateContext(), ct: cts.Token);

        result.Decision.Should().Be(AuthorizationDecision.Allow);

        invariant.ReceivedToken.Should().Be(cts.Token);
        policy.ReceivedToken.Should().Be(cts.Token);

        invariant.CallCount.Should().Be(1);
        policy.CallCount.Should().Be(1);
    }

    private static AuthContext CreateContext()
        => new()
        {
            // These tests exercise authority dispatch only.
            // No built-in invariant accesses Device.
            Device = null!
        };

    private sealed class LegacyInvariant : IAuthorityInvariant
    {
        public int CallCount { get; private set; }

#pragma warning disable CS0672
        public AccessDecisionResult Decide(AuthContext context)
#pragma warning restore CS0672
        {
            CallCount++;

            return AccessDecisionResult.Deny(
                "legacy_invariant_denied");
        }
    }

    private sealed class DelayedPolicy : IAuthorityPolicy
    {
        private readonly TaskCompletionSource<bool> _started =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        private readonly TaskCompletionSource<AccessDecisionResult> _completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Started => _started.Task;

        public bool AppliesTo(AuthContext context) => true;

        public async ValueTask<AccessDecisionResult> DecideAsync(
            AuthContext context,
            CancellationToken ct = default)
        {
            _started.TrySetResult(true);

            return await _completion.Task.WaitAsync(ct);
        }

        public void Complete(AccessDecisionResult decision)
            => _completion.TrySetResult(decision);
    }

    private sealed class TokenCapturingInvariant : IAuthorityInvariant
    {
        public CancellationToken ReceivedToken { get; private set; }

        public int CallCount { get; private set; }

        public ValueTask<AccessDecisionResult> DecideAsync(
            AuthContext context,
            CancellationToken ct = default)
        {
            ReceivedToken = ct;
            CallCount++;

            return ValueTask.FromResult(
                AccessDecisionResult.Allow());
        }
    }

    private sealed class TokenCapturingPolicy : IAuthorityPolicy
    {
        public CancellationToken ReceivedToken { get; private set; }

        public int CallCount { get; private set; }

        public bool AppliesTo(AuthContext context) => true;

        public ValueTask<AccessDecisionResult> DecideAsync(
            AuthContext context,
            CancellationToken ct = default)
        {
            ReceivedToken = ct;
            CallCount++;

            return ValueTask.FromResult(
                AccessDecisionResult.Allow());
        }
    }
}
