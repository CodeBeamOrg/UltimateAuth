using AwesomeAssertions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Security;

namespace CodeBeam.UltimateAuth.Tests.Unit.Authentication;

public class AuthenticationSecurityStateTests
{
    [Fact]
    public void ResetFailuresIfWindowExpired_ClearsFailuresButPreservesLock()
    {
        var now = new DateTimeOffset(2026, 11, 14, 12, 0, 0, TimeSpan.Zero);
        var tenant = TenantKey.Single;
        var user = UserKey.New();

        var state = AuthenticationSecurityState.CreateAccount(tenant, user)
            .RegisterFailure(
                now.AddMinutes(-30),
                threshold: 1,
                lockoutDuration: TimeSpan.FromHours(2),
                failureWindow: TimeSpan.FromMinutes(15));

        var result = state.ResetFailuresIfWindowExpired(
            now, TimeSpan.FromMinutes(15));

        result.FailedAttempts.Should().Be(0);
        result.LastFailedAt.Should().BeNull();
        result.LockedUntil.Should().Be(state.LockedUntil);
        result.SecurityVersion.Should().Be(state.SecurityVersion + 1);

        state.FailedAttempts.Should().Be(1);
    }

    [Fact]
    public void ResetFailuresIfWindowExpired_WithinWindow_ReturnsSameInstance()
    {
        var now = DateTimeOffset.UtcNow;
        var state = AuthenticationSecurityState.CreateAccount(
                TenantKey.Single, UserKey.New())
            .RegisterFailure(
                now,
                threshold: 3,
                lockoutDuration: TimeSpan.FromMinutes(15),
                failureWindow: TimeSpan.FromMinutes(15));

        state.ResetFailuresIfWindowExpired(
            now.AddMinutes(15), TimeSpan.FromMinutes(15))
            .Should().BeSameAs(state);

        state.ResetFailuresIfWindowExpired(
            now.AddHours(1), TimeSpan.Zero)
            .Should().BeSameAs(state);
    }

    [Fact]
    public void LockUntil_DoesNotShortenLockUnlessExplicitlyRequested()
    {
        var now = DateTimeOffset.UtcNow;
        var state = AuthenticationSecurityState.CreateAccount(
                TenantKey.Single, UserKey.New())
            .LockUntil(now.AddMinutes(30));

        var shorter = state.LockUntil(now.AddMinutes(10));

        shorter.Should().BeSameAs(state);

        var overwritten = state.LockUntil(
            now.AddMinutes(10),
            overwriteIfShorter: true);

        overwritten.LockedUntil.Should().Be(now.AddMinutes(10));
        overwritten.SecurityVersion.Should().Be(state.SecurityVersion + 1);
    }

    [Fact]
    public void LockUntil_ExtendsExistingLock()
    {
        var now = DateTimeOffset.UtcNow;
        var state = AuthenticationSecurityState.CreateAccount(
                TenantKey.Single, UserKey.New())
            .LockUntil(now.AddMinutes(10));

        var extended = state.LockUntil(now.AddMinutes(30));

        extended.LockedUntil.Should().Be(now.AddMinutes(30));
        extended.SecurityVersion.Should().Be(state.SecurityVersion + 1);
    }

    [Fact]
    public void ClearReauthentication_IsIdempotentAndPreservesLock()
    {
        var now = DateTimeOffset.UtcNow;
        var state = AuthenticationSecurityState.CreateAccount(
                TenantKey.Single, UserKey.New())
            .LockUntil(now.AddMinutes(30))
            .RequireReauthentication();

        var cleared = state.ClearReauthentication();

        cleared.RequiresReauthentication.Should().BeFalse();
        cleared.LockedUntil.Should().Be(state.LockedUntil);
        cleared.SecurityVersion.Should().Be(state.SecurityVersion + 1);

        cleared.ClearReauthentication().Should().BeSameAs(cleared);
    }
}
