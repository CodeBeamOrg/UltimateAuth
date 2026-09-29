using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Security;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using FluentAssertions;

namespace CodeBeam.UltimateAuth.Tests.Unit.Authentication.Contracts;

public abstract class AuthenticationSecurityStateStoreContractTests
{
    protected abstract Task<IAuthenticationSecurityStateStoreTestDatabase> CreateDatabaseAsync();

    protected virtual TenantKey Tenant => TenantKeys.Single;

    protected static readonly DateTimeOffset Now =
        new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    // ---------------------------------------------------------
    // Add / Get
    // ---------------------------------------------------------

    [Fact]
    public async Task AddAsync_WhenValid_PersistsState()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var user = UserKey.New();

        var state = AuthenticationSecurityState.CreateAccount(Tenant, user);

        await store.AddAsync(state);

        var result = await store.GetAsync(
            user,
            AuthenticationSecurityScope.Account,
            null);

        result.Should().NotBeNull();
        result!.Id.Should().Be(state.Id);
        result.Tenant.Should().Be(Tenant);
        result.UserKey.Should().Be(user);
        result.Scope.Should().Be(state.Scope);
        result.CredentialType.Should().Be(state.CredentialType);
        result.SecurityVersion.Should().Be(state.SecurityVersion);
    }

    [Fact]
    public async Task AddAsync_SameUserWithAccountAndFactorStates_IsAllowed()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var user = UserKey.New();

        var account = AuthenticationSecurityState.CreateAccount(
            Tenant,
            user);

        var factor = AuthenticationSecurityState.CreateFactor(
            Tenant,
            user,
            CredentialType.Password);

        await store.AddAsync(account);
        await store.AddAsync(factor);

        var accountResult = await store.GetAsync(
            user,
            AuthenticationSecurityScope.Account,
            null);

        var factorResult = await store.GetAsync(
            user,
            AuthenticationSecurityScope.Factor,
            CredentialType.Password);

        accountResult.Should().NotBeNull();
        factorResult.Should().NotBeNull();

        accountResult!.Id.Should().Be(account.Id);
        factorResult!.Id.Should().Be(factor.Id);
    }

    [Fact]
    public async Task AddAsync_WhenSameLogicalStateAlreadyExists_ThrowsConflict()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var user = UserKey.New();

        var first = AuthenticationSecurityState.CreateAccount(
            Tenant,
            user);

        var second = AuthenticationSecurityState.CreateAccount(
            Tenant,
            user);

        await store.AddAsync(first);

        var act = () => store.AddAsync(second);

        await act.Should()
            .ThrowAsync<UAuthConflictException>();
    }

    [Fact]
    public async Task AddAsync_WhenStateBelongsToDifferentTenant_IsRejected()
    {
        await using var db = await CreateDatabaseAsync();

        var tenantA = TestIds.Tenant("tenant-a");
        var tenantB = TestIds.Tenant("tenant-b");

        var store = db.CreateStore(tenantA);

        var state = AuthenticationSecurityState.CreateAccount(
            tenantB,
            UserKey.New());

        var act = () => store.AddAsync(state);

        await act.Should()
            .ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GetAsync_WhenStateDoesNotExist_ReturnsNull()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var result = await store.GetAsync(
            UserKey.New(),
            AuthenticationSecurityScope.Account,
            null);

        result.Should().BeNull();
    }

    // ---------------------------------------------------------
    // Logical key
    // ---------------------------------------------------------

    [Fact]
    public async Task AddAsync_SameUserWithDifferentScope_IsAllowed()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var user = UserKey.New();

        var first = AuthenticationSecurityState.CreateAccount(Tenant, user);

        var second = AuthenticationSecurityState.CreateFactor(Tenant, user, CredentialType.Password);

        await store.AddAsync(first);
        await store.AddAsync(second);

        var firstResult = await store.GetAsync(
            user,
            first.Scope,
            first.CredentialType);

        var secondResult = await store.GetAsync(
            user,
            second.Scope,
            second.CredentialType);

        firstResult.Should().NotBeNull();
        secondResult.Should().NotBeNull();

        firstResult!.Id.Should().Be(first.Id);
        secondResult!.Id.Should().Be(second.Id);
    }

    [Fact]
    public async Task AddAsync_SameUserAndScopeWithDifferentCredentialType_IsAllowed()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var user = UserKey.New();

        var first = AuthenticationSecurityState.CreateFactor(Tenant, user, CredentialType.Password);

        var second = AuthenticationSecurityState.CreateFactor(Tenant, user, CredentialType.Passkey);

        await store.AddAsync(first);
        await store.AddAsync(second);

        var password = await store.GetAsync(
            user,
            AuthenticationSecurityScope.Factor,
            CredentialType.Password);

        var passkey = await store.GetAsync(
            user,
            AuthenticationSecurityScope.Factor,
            CredentialType.Passkey);

        password.Should().NotBeNull();
        passkey.Should().NotBeNull();

        password!.Id.Should().Be(first.Id);
        passkey!.Id.Should().Be(second.Id);
    }

    // ---------------------------------------------------------
    // Update
    // ---------------------------------------------------------

    [Fact]
    public async Task UpdateAsync_WhenExpectedVersionMatches_PersistsChanges()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var user = UserKey.New();

        var original = AuthenticationSecurityState.CreateAccount(
            Tenant,
            user);

        await store.AddAsync(original);

        var updated = original.RegisterFailure(
            Now,
            threshold: 3,
            failureWindow: TimeSpan.FromMinutes(5),
            lockoutDuration: TimeSpan.FromMinutes(15));

        await store.UpdateAsync(
            updated,
            expectedVersion: original.SecurityVersion);

        var result = await store.GetAsync(
            user,
            AuthenticationSecurityScope.Account,
            null);

        result.Should().NotBeNull();

        result!.Id.Should().Be(original.Id);
        result.SecurityVersion.Should().Be(1);
        result.FailedAttempts.Should().Be(1);
        result.LastFailedAt.Should().Be(Now);
        result.LockedUntil.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_WhenExpectedVersionIsStale_ThrowsConflict()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var original = AuthenticationSecurityState.CreateAccount(
            Tenant,
            UserKey.New());

        await store.AddAsync(original);

        var updated = original.RegisterFailure(
            Now,
            threshold: 3,
            failureWindow: TimeSpan.FromMinutes(5),
            lockoutDuration: TimeSpan.FromMinutes(15));

        var act = () => store.UpdateAsync(
            updated,
            expectedVersion: updated.SecurityVersion);

        await act.Should()
            .ThrowAsync<UAuthConflictException>();
    }

    [Fact]
    public async Task UpdateAsync_WhenResetBegins_PersistsResetState()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var user = UserKey.New();

        var original = AuthenticationSecurityState.CreateFactor(
            Tenant,
            user,
            CredentialType.Password);

        await store.AddAsync(original);

        var updated = original.BeginReset(
            "hashed-reset-token",
            Now,
            TimeSpan.FromMinutes(30));

        await store.UpdateAsync(
            updated,
            original.SecurityVersion);

        var result = await store.GetAsync(
            user,
            AuthenticationSecurityScope.Factor,
            CredentialType.Password);

        result.Should().NotBeNull();

        result!.ResetRequestedAt.Should().Be(Now);
        result.ResetExpiresAt.Should().Be(Now.AddMinutes(30));
        result.ResetConsumedAt.Should().BeNull();
        result.ResetTokenHash.Should().Be("hashed-reset-token");
        result.ResetAttempts.Should().Be(0);
        result.SecurityVersion.Should().Be(1);
    }

    [Fact]
    public async Task UpdateAsync_WhenStateDoesNotExist_ThrowsNotFound()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var original = AuthenticationSecurityState.CreateAccount(
            Tenant,
            UserKey.New());

        var updated = original.RequireReauthentication();

        var act = () => store.UpdateAsync(
            updated,
            original.SecurityVersion);

        await act.Should()
            .ThrowAsync<UAuthNotFoundException>();
    }

    // ---------------------------------------------------------
    // Delete
    // ---------------------------------------------------------

    [Fact]
    public async Task DeleteAsync_WhenStateExists_RemovesState()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var user = UserKey.New();

        var state = AuthenticationSecurityState.CreateAccount(Tenant, user);

        await store.AddAsync(state);

        await store.DeleteAsync(
            user,
            state.Scope,
            state.CredentialType);

        var result = await store.GetAsync(
            user,
            state.Scope,
            state.CredentialType);

        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_AccountState_DoesNotDeleteFactorState()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var user = UserKey.New();

        var account = AuthenticationSecurityState.CreateAccount(
            Tenant,
            user);

        var factor = AuthenticationSecurityState.CreateFactor(
            Tenant,
            user,
            CredentialType.Password);

        await store.AddAsync(account);
        await store.AddAsync(factor);

        await store.DeleteAsync(
            user,
            AuthenticationSecurityScope.Account,
            null);

        var accountResult = await store.GetAsync(
            user,
            AuthenticationSecurityScope.Account,
            null);

        var factorResult = await store.GetAsync(
            user,
            AuthenticationSecurityScope.Factor,
            CredentialType.Password);

        accountResult.Should().BeNull();
        factorResult.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteAsync_WhenStateDoesNotExist_IsIdempotent()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var act = () => store.DeleteAsync(
            UserKey.New(),
            AuthenticationSecurityScope.Account,
            null);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DeleteAsync_RemovesOnlyExactLogicalState()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var user = UserKey.New();

        var password = AuthenticationSecurityState.CreateFactor(Tenant, user, CredentialType.Password);
        var passkey = AuthenticationSecurityState.CreateFactor(Tenant, user, CredentialType.Passkey);

        await store.AddAsync(password);
        await store.AddAsync(passkey);

        await store.DeleteAsync(
            user,
            AuthenticationSecurityScope.Factor,
            CredentialType.Password);

        var passwordResult = await store.GetAsync(
            user,
            AuthenticationSecurityScope.Factor,
            CredentialType.Password);

        var passkeyResult = await store.GetAsync(
            user,
            AuthenticationSecurityScope.Factor,
            CredentialType.Passkey);

        passwordResult.Should().BeNull();
        passkeyResult.Should().NotBeNull();
    }

    // ---------------------------------------------------------
    // Tenant isolation
    // ---------------------------------------------------------

    [Fact]
    public async Task GetAsync_IsTenantIsolated()
    {
        await using var db = await CreateDatabaseAsync();

        var tenantA = TestIds.Tenant("tenant-a");
        var tenantB = TestIds.Tenant("tenant-b");

        var storeA = db.CreateStore(tenantA);
        var storeB = db.CreateStore(tenantB);

        var user = UserKey.New();

        var state = AuthenticationSecurityState.CreateAccount(
            tenantA,
            user);

        await storeA.AddAsync(state);

        var fromA = await storeA.GetAsync(
            user,
            AuthenticationSecurityScope.Account,
            null);

        var fromB = await storeB.GetAsync(
            user,
            AuthenticationSecurityScope.Account,
            null);

        fromA.Should().NotBeNull();
        fromA!.Tenant.Should().Be(tenantA);

        fromB.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_IsTenantIsolated()
    {
        await using var db = await CreateDatabaseAsync();

        var tenantA = TestIds.Tenant("tenant-a");
        var tenantB = TestIds.Tenant("tenant-b");

        var storeA = db.CreateStore(tenantA);
        var storeB = db.CreateStore(tenantB);

        var user = UserKey.New();

        var stateA = AuthenticationSecurityState.CreateAccount(
            tenantA,
            user);

        var stateB = AuthenticationSecurityState.CreateAccount(
            tenantB,
            user);

        await storeA.AddAsync(stateA);
        await storeB.AddAsync(stateB);

        await storeA.DeleteAsync(
            user,
            AuthenticationSecurityScope.Account,
            null);

        var fromA = await storeA.GetAsync(
            user,
            AuthenticationSecurityScope.Account,
            null);

        var fromB = await storeB.GetAsync(
            user,
            AuthenticationSecurityScope.Account,
            null);

        fromA.Should().BeNull();

        fromB.Should().NotBeNull();
        fromB!.Id.Should().Be(stateB.Id);
        fromB.Tenant.Should().Be(tenantB);
    }

    // ---------------------------------------------------------
    // Cancellation
    // ---------------------------------------------------------

    [Fact]
    public async Task Operations_WhenAlreadyCancelled_ThrowOperationCanceledException()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var state = AuthenticationSecurityState.CreateAccount(Tenant, UserKey.New());

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await FluentActions
            .Invoking(() => store.GetAsync(
                state.UserKey,
                state.Scope,
                state.CredentialType,
                cts.Token))
            .Should()
            .ThrowAsync<OperationCanceledException>();

        await FluentActions
            .Invoking(() => store.AddAsync(state, cts.Token))
            .Should()
            .ThrowAsync<OperationCanceledException>();

        await FluentActions
            .Invoking(() => store.UpdateAsync(
                state,
                state.SecurityVersion,
                cts.Token))
            .Should()
            .ThrowAsync<OperationCanceledException>();

        await FluentActions
            .Invoking(() => store.DeleteAsync(
                state.UserKey,
                state.Scope,
                state.CredentialType,
                cts.Token))
            .Should()
            .ThrowAsync<OperationCanceledException>();
    }
}
