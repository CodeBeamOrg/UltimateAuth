using AwesomeAssertions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Security;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;

public sealed class AuthenticationUnlockTests
{
    [Fact]
    public void Unlock_ClearsFailuresAndLockout()
    {
        var now = DateTimeOffset.UtcNow;

        var state = AuthenticationSecurityState
            .CreateAccount(TenantKey.Single, TestUsers.User)
            .RegisterFailure(
                now,
                threshold: 1,
                lockoutDuration: TimeSpan.FromMinutes(15),
                failureWindow: TimeSpan.FromMinutes(15));

        var unlocked = state.Unlock();

        unlocked.FailedAttempts.Should().Be(0);
        unlocked.LastFailedAt.Should().BeNull();
        unlocked.LockedUntil.Should().BeNull();
        unlocked.IsLocked(now).Should().BeFalse();

        unlocked.SecurityVersion.Should().Be(state.SecurityVersion + 1);
    }

    [Fact]
    public void Unlock_WhenAlreadyUnlocked_IsIdempotent()
    {
        var state = AuthenticationSecurityState.CreateAccount(TenantKey.Single, TestUsers.User);

        var unlocked = state.Unlock();

        unlocked.Should().BeSameAs(state);
        unlocked.SecurityVersion.Should().Be(state.SecurityVersion);
    }

    [Fact]
    public void Unlock_PreservesReauthenticationRequirement()
    {
        var now = DateTimeOffset.UtcNow;

        var state = AuthenticationSecurityState
            .CreateAccount(TenantKey.Single, TestUsers.User)
            .RegisterFailure(
                now,
                1,
                TimeSpan.FromMinutes(15),
                TimeSpan.FromMinutes(15))
            .RequireReauthentication();

        var unlocked = state.Unlock();

        unlocked.RequiresReauthentication.Should().BeTrue();
        unlocked.IsLocked(now).Should().BeFalse();
    }

    [Fact]
    public void Recovery_ClearsLockAndConsumesResetProof()
    {
        var now = DateTimeOffset.UtcNow;

        var state = AuthenticationSecurityState
            .CreateFactor(
                TenantKey.Single,
                TestUsers.User,
                CredentialType.Password)
            .RegisterFailure(
                now,
                1,
                TimeSpan.FromMinutes(15),
                TimeSpan.FromMinutes(15))
            .BeginReset(
                "test-token-hash",
                now,
                TimeSpan.FromMinutes(10));

        var recovered = state.ConsumeReset(now).Unlock();

        recovered.IsLocked(now).Should().BeFalse();
        recovered.FailedAttempts.Should().Be(0);
        recovered.ResetConsumedAt.Should().Be(now);
        recovered.HasActiveReset(now).Should().BeFalse();
    }
}
