using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Security;
using CodeBeam.UltimateAuth.Server.Security;
using FluentAssertions;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Unit.Server.Security;

public sealed class AuthenticationSecurityManagerTests
{
    private readonly Mock<IAuthenticationSecurityStateStoreFactory> _storeFactory;
    private readonly Mock<IAuthenticationSecurityStateStore> _store;

    private readonly AuthenticationSecurityManager _sut;

    private readonly TenantKey _tenant =
        TenantKey.FromExternal("tenant-1");

    private readonly UserKey _userKey =
        UserKey.FromString("user-1");

    public AuthenticationSecurityManagerTests()
    {
        _storeFactory =
            new Mock<IAuthenticationSecurityStateStoreFactory>();

        _store =
            new Mock<IAuthenticationSecurityStateStore>();

        _storeFactory
            .Setup(x => x.Create(_tenant))
            .Returns(_store.Object);

        _sut = new AuthenticationSecurityManager(
            _storeFactory.Object);
    }

    // ---------------------------------------------------------------------
    // GetOrCreateAccountAsync
    // ---------------------------------------------------------------------

    [Fact]
    public async Task GetOrCreateAccountAsync_WhenStateExists_ShouldReturnExistingState()
    {
        var existing =
            AuthenticationSecurityState.CreateAccount(
                _tenant,
                _userKey);

        _store
            .Setup(x => x.GetAsync(
                _userKey,
                AuthenticationSecurityScope.Account,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await _sut.GetOrCreateAccountAsync(
            _tenant,
            _userKey);

        result.Should().BeSameAs(existing);

        _store.Verify(
            x => x.AddAsync(
                It.IsAny<AuthenticationSecurityState>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetOrCreateAccountAsync_WhenStateDoesNotExist_ShouldCreateState()
    {
        _store
            .Setup(x => x.GetAsync(
                _userKey,
                AuthenticationSecurityScope.Account,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((AuthenticationSecurityState?)null);

        AuthenticationSecurityState? added = null;

        _store
            .Setup(x => x.AddAsync(
                It.IsAny<AuthenticationSecurityState>(),
                It.IsAny<CancellationToken>()))
            .Callback<AuthenticationSecurityState, CancellationToken>(
                (state, _) => added = state)
            .Returns(Task.CompletedTask);

        var result = await _sut.GetOrCreateAccountAsync(
            _tenant,
            _userKey);

        result.Should().NotBeNull();
        result.Tenant.Should().Be(_tenant);
        result.UserKey.Should().Be(_userKey);
        result.Scope.Should()
            .Be(AuthenticationSecurityScope.Account);

        added.Should().BeSameAs(result);
    }

    [Fact]
    public async Task GetOrCreateAccountAsync_WhenConcurrentCreationOccurs_ShouldReturnExistingState()
    {
        var existing =
            AuthenticationSecurityState.CreateAccount(
                _tenant,
                _userKey);

        var call = 0;

        _store
            .Setup(x => x.GetAsync(
                _userKey,
                AuthenticationSecurityScope.Account,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                call++;

                return call == 1
                    ? null
                    : existing;
            });

        _store
            .Setup(x => x.AddAsync(
                It.IsAny<AuthenticationSecurityState>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(
                new UAuthConflictException("concurrent creation"));

        var result = await _sut.GetOrCreateAccountAsync(
            _tenant,
            _userKey);

        result.Should().BeSameAs(existing);

        _store.Verify(
            x => x.GetAsync(
                _userKey,
                AuthenticationSecurityScope.Account,
                null,
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task GetOrCreateAccountAsync_WhenConflictOccursAndStateStillDoesNotExist_ShouldRethrowConflict()
    {
        _store
            .Setup(x => x.GetAsync(
                _userKey,
                AuthenticationSecurityScope.Account,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((AuthenticationSecurityState?)null);

        _store
            .Setup(x => x.AddAsync(
                It.IsAny<AuthenticationSecurityState>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(
                new UAuthConflictException("conflict"));

        var act = () =>
            _sut.GetOrCreateAccountAsync(
                _tenant,
                _userKey);

        await act.Should()
            .ThrowAsync<UAuthConflictException>();
    }

    // ---------------------------------------------------------------------
    // GetOrCreateFactorAsync
    // ---------------------------------------------------------------------

    [Fact]
    public async Task GetOrCreateFactorAsync_WhenStateExists_ShouldReturnExistingState()
    {
        var existing =
            AuthenticationSecurityState.CreateFactor(
                _tenant,
                _userKey,
                CredentialType.Password);

        _store
            .Setup(x => x.GetAsync(
                _userKey,
                AuthenticationSecurityScope.Factor,
                CredentialType.Password,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await _sut.GetOrCreateFactorAsync(
            _tenant,
            _userKey,
            CredentialType.Password);

        result.Should().BeSameAs(existing);

        _store.Verify(
            x => x.AddAsync(
                It.IsAny<AuthenticationSecurityState>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetOrCreateFactorAsync_WhenStateDoesNotExist_ShouldCreateCorrectFactor()
    {
        _store
            .Setup(x => x.GetAsync(
                _userKey,
                AuthenticationSecurityScope.Factor,
                CredentialType.Password,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((AuthenticationSecurityState?)null);

        AuthenticationSecurityState? added = null;

        _store
            .Setup(x => x.AddAsync(
                It.IsAny<AuthenticationSecurityState>(),
                It.IsAny<CancellationToken>()))
            .Callback<AuthenticationSecurityState, CancellationToken>(
                (state, _) => added = state)
            .Returns(Task.CompletedTask);

        var result = await _sut.GetOrCreateFactorAsync(
            _tenant,
            _userKey,
            CredentialType.Password);

        result.Tenant.Should().Be(_tenant);
        result.UserKey.Should().Be(_userKey);
        result.Scope.Should()
            .Be(AuthenticationSecurityScope.Factor);

        result.CredentialType.Should()
            .Be(CredentialType.Password);

        added.Should().BeSameAs(result);
    }

    [Fact]
    public async Task GetOrCreateFactorAsync_WhenConcurrentCreationOccurs_ShouldReturnExistingState()
    {
        var existing =
            AuthenticationSecurityState.CreateFactor(
                _tenant,
                _userKey,
                CredentialType.Password);

        var call = 0;

        _store
            .Setup(x => x.GetAsync(
                _userKey,
                AuthenticationSecurityScope.Factor,
                CredentialType.Password,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                call++;

                return call == 1
                    ? null
                    : existing;
            });

        _store
            .Setup(x => x.AddAsync(
                It.IsAny<AuthenticationSecurityState>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(
                new UAuthConflictException("concurrent creation"));

        var result = await _sut.GetOrCreateFactorAsync(
            _tenant,
            _userKey,
            CredentialType.Password);

        result.Should().BeSameAs(existing);
    }

    // ---------------------------------------------------------------------
    // MutateAccountAsync
    // ---------------------------------------------------------------------

    [Fact]
    public async Task MutateAccountAsync_WhenMutationReturnsSameInstance_ShouldNotUpdateStore()
    {
        var existing =
            AuthenticationSecurityState.CreateAccount(
                _tenant,
                _userKey);

        _store
            .Setup(x => x.GetAsync(
                _userKey,
                AuthenticationSecurityScope.Account,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await _sut.MutateAccountAsync(
            _tenant,
            _userKey,
            state => state);

        result.Should().BeSameAs(existing);

        _store.Verify(
            x => x.UpdateAsync(
                It.IsAny<AuthenticationSecurityState>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task MutateAccountAsync_WhenUpdateSucceeds_ShouldUseCurrentSecurityVersion()
    {
        var current =
            AuthenticationSecurityState.CreateAccount(
                _tenant,
                _userKey);

        var updated = CreateMutatedState(current);

        _store
            .Setup(x => x.GetAsync(
                _userKey,
                AuthenticationSecurityScope.Account,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(current);

        _store
            .Setup(x => x.UpdateAsync(
                updated,
                current.SecurityVersion,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.MutateAccountAsync(
            _tenant,
            _userKey,
            _ => updated);

        result.Should().BeSameAs(updated);

        _store.Verify(
            x => x.UpdateAsync(
                updated,
                current.SecurityVersion,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task MutateAccountAsync_WhenUpdateConflicts_ShouldReloadAndReapplyMutation()
    {
        var first =
            AuthenticationSecurityState.CreateAccount(
                _tenant,
                _userKey);

        var second =
            AuthenticationSecurityState.CreateAccount(
                _tenant,
                _userKey);

        var getCall = 0;
        var updateCall = 0;
        var mutationCall = 0;

        _store
            .Setup(x => x.GetAsync(
                _userKey,
                AuthenticationSecurityScope.Account,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                getCall++;

                return getCall == 1
                    ? first
                    : second;
            });

        _store
            .Setup(x => x.UpdateAsync(
                It.IsAny<AuthenticationSecurityState>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()))
            .Returns<AuthenticationSecurityState, long, CancellationToken>(
                (_, _, _) =>
                {
                    updateCall++;

                    if (updateCall == 1)
                    {
                        throw new UAuthConflictException(
                            "concurrent update");
                    }

                    return Task.CompletedTask;
                });

        var result = await _sut.MutateAccountAsync(
            _tenant,
            _userKey,
            state =>
            {
                mutationCall++;
                return CreateMutatedState(state);
            });

        result.Should().NotBeNull();

        mutationCall.Should().Be(2);
        getCall.Should().Be(2);
        updateCall.Should().Be(2);
    }

    [Fact]
    public async Task MutateAccountAsync_WhenAllFiveUpdatesConflict_ShouldStopAfterFiveAttempts()
    {
        var mutationCalls = 0;

        _store
            .Setup(x => x.GetAsync(
                _userKey,
                AuthenticationSecurityScope.Account,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
                AuthenticationSecurityState.CreateAccount(
                    _tenant,
                    _userKey));

        _store
            .Setup(x => x.UpdateAsync(
                It.IsAny<AuthenticationSecurityState>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(
                new UAuthConflictException("conflict"));

        var act = () =>
            _sut.MutateAccountAsync(
                _tenant,
                _userKey,
                state =>
                {
                    mutationCalls++;
                    return CreateMutatedState(state);
                });

        await act.Should()
            .ThrowAsync<UAuthConflictException>();

        mutationCalls.Should().Be(5);

        _store.Verify(
            x => x.UpdateAsync(
                It.IsAny<AuthenticationSecurityState>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(5));
    }

    // ---------------------------------------------------------------------
    // MutateFactorAsync
    // ---------------------------------------------------------------------

    [Fact]
    public async Task MutateFactorAsync_WhenMutationReturnsSameInstance_ShouldNotUpdateStore()
    {
        var existing =
            AuthenticationSecurityState.CreateFactor(
                _tenant,
                _userKey,
                CredentialType.Password);

        _store
            .Setup(x => x.GetAsync(
                _userKey,
                AuthenticationSecurityScope.Factor,
                CredentialType.Password,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await _sut.MutateFactorAsync(
            _tenant,
            _userKey,
            CredentialType.Password,
            state => state);

        result.Should().BeSameAs(existing);

        _store.Verify(
            x => x.UpdateAsync(
                It.IsAny<AuthenticationSecurityState>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task MutateFactorAsync_WhenUpdateConflicts_ShouldReloadAndReapplyMutation()
    {
        var getCalls = 0;
        var updateCalls = 0;
        var mutationCalls = 0;

        _store
            .Setup(x => x.GetAsync(
                _userKey,
                AuthenticationSecurityScope.Factor,
                CredentialType.Password,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                getCalls++;

                return AuthenticationSecurityState.CreateFactor(
                    _tenant,
                    _userKey,
                    CredentialType.Password);
            });

        _store
            .Setup(x => x.UpdateAsync(
                It.IsAny<AuthenticationSecurityState>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()))
            .Returns<AuthenticationSecurityState, long, CancellationToken>(
                (_, _, _) =>
                {
                    updateCalls++;

                    if (updateCalls == 1)
                    {
                        throw new UAuthConflictException(
                            "concurrent update");
                    }

                    return Task.CompletedTask;
                });

        var result = await _sut.MutateFactorAsync(
            _tenant,
            _userKey,
            CredentialType.Password,
            state =>
            {
                mutationCalls++;
                return CreateMutatedState(state);
            });

        result.Should().NotBeNull();

        getCalls.Should().Be(2);
        mutationCalls.Should().Be(2);
        updateCalls.Should().Be(2);
    }

    // ---------------------------------------------------------------------
    // Direct delegation
    // ---------------------------------------------------------------------

    [Fact]
    public async Task UpdateAsync_ShouldResolveStoreUsingUpdatedStateTenant()
    {
        var otherTenant =
            TenantKey.FromExternal("tenant-2");

        var otherStore =
            new Mock<IAuthenticationSecurityStateStore>();

        _storeFactory
            .Setup(x => x.Create(otherTenant))
            .Returns(otherStore.Object);

        var state =
            AuthenticationSecurityState.CreateAccount(
                otherTenant,
                _userKey);

        await _sut.UpdateAsync(
            state,
            expectedVersion: 7);

        _storeFactory.Verify(
            x => x.Create(otherTenant),
            Times.Once);

        otherStore.Verify(
            x => x.UpdateAsync(
                state,
                7,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldResolveStoreUsingRequestedTenant()
    {
        var otherTenant =
            TenantKey.FromExternal("tenant-2");

        var otherStore =
            new Mock<IAuthenticationSecurityStateStore>();

        _storeFactory
            .Setup(x => x.Create(otherTenant))
            .Returns(otherStore.Object);

        await _sut.DeleteAsync(
            otherTenant,
            _userKey,
            AuthenticationSecurityScope.Factor,
            CredentialType.Password);

        _storeFactory.Verify(
            x => x.Create(otherTenant),
            Times.Once);

        otherStore.Verify(
            x => x.DeleteAsync(
                _userKey,
                AuthenticationSecurityScope.Factor,
                CredentialType.Password,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetOrCreateAccountAsync_WhenCancelled_ShouldNotAccessStore()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () =>
            _sut.GetOrCreateAccountAsync(
                _tenant,
                _userKey,
                cts.Token);

        await act.Should()
            .ThrowAsync<OperationCanceledException>();

        _storeFactory.Verify(
            x => x.Create(It.IsAny<TenantKey>()),
            Times.Never);
    }

    // ---------------------------------------------------------------------
    // Helper
    // ---------------------------------------------------------------------

    private static readonly DateTimeOffset MutationTime = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static AuthenticationSecurityState CreateMutatedState(AuthenticationSecurityState state)
    {
        return state.RegisterFailure(
            now: MutationTime,
            threshold: 5,
            lockoutDuration: TimeSpan.FromMinutes(5),
            failureWindow: TimeSpan.FromMinutes(15));
    }
}