using AwesomeAssertions;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Security;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Security;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Unit.Server.Security;

public sealed class AuthenticationLockoutServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 14, 10, 0, 0, TimeSpan.Zero);

    private readonly UserKey _actor = UserKey.New();
    private readonly UserKey _target = UserKey.New();

    private readonly Mock<IAuthenticationSecurityManager> _security =
        new(MockBehavior.Strict);

    private readonly Mock<IUserRuntimeStateProvider> _users =
        new(MockBehavior.Strict);

    private readonly Mock<IAccessOrchestrator> _access =
        new(MockBehavior.Strict);

    private AuthenticationLockoutService CreateService()
    {
        _access.Setup(x => x.ExecuteAsync(
                It.IsAny<AccessContext>(),
                It.IsAny<AccessCommand<AuthenticationLockoutInfo>>(),
                It.IsAny<CancellationToken>()))
            .Returns<AccessContext,
                AccessCommand<AuthenticationLockoutInfo>,
                CancellationToken>(
                (_, command, ct) => command.ExecuteAsync(ct));

        _access.Setup(x => x.ExecuteAsync(
                It.IsAny<AccessContext>(),
                It.IsAny<AccessCommand<AuthenticationUnlockResult>>(),
                It.IsAny<CancellationToken>()))
            .Returns<AccessContext,
                AccessCommand<AuthenticationUnlockResult>,
                CancellationToken>(
                (_, command, ct) => command.ExecuteAsync(ct));

        _users.Setup(x => x.GetAsync(
                It.IsAny<TenantKey>(),
                _target,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserRuntimeRecord
            {
                UserKey = _target,
                Exists = true,
                IsActive = true,
                CanAuthenticate = true
            });

        return new AuthenticationLockoutService(
            _security.Object,
            _users.Object,
            _access.Object);
    }

    private AccessContext Context(string action) =>
        TestAccessContext.ForTargetUser(
            _actor, _target, action);

    private static AuthenticationSecurityState LockedAccount(
        TenantKey tenant, UserKey user) =>
        AuthenticationSecurityState.CreateAccount(tenant, user)
            .RegisterFailure(
                Now, 1,
                TimeSpan.FromMinutes(15),
                TimeSpan.FromMinutes(15));

    private static AuthenticationSecurityState LockedPassword(
        TenantKey tenant, UserKey user) =>
        AuthenticationSecurityState.CreateFactor(
                tenant, user, CredentialType.Password)
            .RegisterFailure(
                Now, 1,
                TimeSpan.FromMinutes(15),
                TimeSpan.FromMinutes(15));

    [Fact]
    public async Task UnlockAsync_Account_UsesOnlyAccountMutation()
    {
        var sut = CreateService();
        var context = Context(UAuthActions.Authentication.UnlockAdmin);
        var locked = LockedAccount(context.ResourceTenant, _target);

        _security.Setup(x => x.GetOrCreateAccountAsync(
                context.ResourceTenant, _target,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(locked);

        _security.Setup(x => x.MutateAccountAsync(
                context.ResourceTenant, _target,
                It.IsAny<Func<AuthenticationSecurityState,
                    AuthenticationSecurityState>>(),
                It.IsAny<CancellationToken>()))
            .Returns<TenantKey, UserKey,
                Func<AuthenticationSecurityState, AuthenticationSecurityState>,
                CancellationToken>(
                (_, _, mutate, _) => Task.FromResult(mutate(locked)));

        var result = await sut.UnlockAsync(
            context,
            new UnlockAuthenticationRequest(
                AuthenticationSecurityScope.Account),
            TestContext.Current.CancellationToken);

        result.State.IsLocked.Should().BeFalse();
        result.State.FailedAttempts.Should().Be(0);

        _security.Verify(x => x.MutateFactorAsync(
            It.IsAny<TenantKey>(),
            It.IsAny<UserKey>(),
            It.IsAny<CredentialType>(),
            It.IsAny<Func<AuthenticationSecurityState,
                AuthenticationSecurityState>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UnlockAsync_PasswordFactor_DoesNotMutateAccount()
    {
        var sut = CreateService();
        var context = Context(UAuthActions.Authentication.UnlockAdmin);
        var locked = LockedPassword(context.ResourceTenant, _target);

        _security.Setup(x => x.GetOrCreateFactorAsync(
                context.ResourceTenant,
                _target, CredentialType.Password,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(locked);

        _security.Setup(x => x.MutateFactorAsync(
                context.ResourceTenant,
                _target, CredentialType.Password,
                It.IsAny<Func<AuthenticationSecurityState,
                    AuthenticationSecurityState>>(),
                It.IsAny<CancellationToken>()))
            .Returns<TenantKey, UserKey, CredentialType,
                Func<AuthenticationSecurityState, AuthenticationSecurityState>,
                CancellationToken>(
                (_, _, _, mutate, _) =>
                    Task.FromResult(mutate(locked)));

        var result = await sut.UnlockAsync(
            context,
            new UnlockAuthenticationRequest(
                AuthenticationSecurityScope.Factor,
                CredentialType.Password),
            TestContext.Current.CancellationToken);

        result.State.IsLocked.Should().BeFalse();
        result.State.CredentialType.Should()
            .Be(CredentialType.Password);

        _security.Verify(x => x.MutateAccountAsync(
            It.IsAny<TenantKey>(),
            It.IsAny<UserKey>(),
            It.IsAny<Func<AuthenticationSecurityState,
                AuthenticationSecurityState>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(AuthenticationSecurityScope.Account,
        CredentialType.Password)]
    [InlineData(AuthenticationSecurityScope.Factor, null)]
    [InlineData((AuthenticationSecurityScope)99, null)]
    public async Task UnlockAsync_InvalidScope_RejectsBeforeMutation(
        AuthenticationSecurityScope scope,
        CredentialType? type)
    {
        var sut = CreateService();
        var context = Context(UAuthActions.Authentication.UnlockAdmin);

        var act = () => sut.UnlockAsync(
            context,
            new UnlockAuthenticationRequest(scope, type));

        await act.Should()
            .ThrowAsync<UAuthValidationException>();

        _security.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UnlockAsync_DeletedUser_IsRejected()
    {
        var sut = CreateService();
        var context = Context(UAuthActions.Authentication.UnlockAdmin);

        _users.Setup(x => x.GetAsync(
                context.ResourceTenant,
                _target,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserRuntimeRecord
            {
                UserKey = _target,
                Exists = true,
                IsDeleted = true
            });

        var act = () => sut.UnlockAsync(
            context,
            new UnlockAuthenticationRequest(
                AuthenticationSecurityScope.Account));

        await act.Should()
            .ThrowAsync<UAuthNotFoundException>();

        _security.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetAsync_ReturnsLockoutWithoutResetSecrets()
    {
        var sut = CreateService();
        var context = Context(
            UAuthActions.Authentication.GetLockoutAdmin);

        var state = LockedPassword(
                context.ResourceTenant, _target)
            .BeginReset(
                "secret-token-hash",
                Now, TimeSpan.FromMinutes(10));

        _security.Setup(x => x.GetOrCreateFactorAsync(
                context.ResourceTenant,
                _target, CredentialType.Password,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(state);

        var result = await sut.GetAsync(
            context,
            new LockoutQueryRequest(
                AuthenticationSecurityScope.Factor,
                CredentialType.Password),
            TestContext.Current.CancellationToken);

        result.FailedAttempts.Should().Be(1);
        result.CredentialType.Should()
            .Be(CredentialType.Password);

        typeof(AuthenticationLockoutInfo)
            .GetProperties()
            .Select(x => x.Name)
            .Should().NotContain("ResetTokenHash");
    }
}
