using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Credentials.Reference;
using AwesomeAssertions;

namespace CodeBeam.UltimateAuth.Tests.Unit.Credentials.Contracts;

public abstract class PasswordCredentialStoreContractTests
{
    protected abstract Task<IPasswordCredentialStoreTestDatabase>
        CreateDatabaseAsync();

    protected static PasswordCredential CreateCredential(
        TenantKey tenant,
        UserKey userKey,
        string discriminator)
    {
        return PasswordCredential.Create(
            id: Guid.NewGuid(),
            tenant: tenant,
            userKey: userKey,
            secretHash: CreatePasswordHash(discriminator),
            security: CredentialSecurityState.Active(),
            metadata: new CredentialMetadata(),
            now: Now);
    }

    protected static PasswordHash CreatePasswordHash(string discriminator)
    {
        return PasswordHash.Create(algorithm: "test", hash: $"hashed-password-{discriminator}");
    }

    protected static readonly TenantKey TenantA =
        TenantKey.FromExternal("tenant-a");

    protected static readonly TenantKey TenantB =
        TenantKey.FromExternal("tenant-b");

    protected static readonly DateTimeOffset Now =
        new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

    // ============================================================
    // ADD / GET / EXISTS
    // ============================================================

    [Fact]
    public async Task AddAsync_PersistsCredential()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var user = UserKey.New();
        var credential = CreateCredential(TenantA, user, "credential-1");

        await store.AddAsync(credential, ct: TestContext.Current.CancellationToken);

        var key = new CredentialKey(
            credential.Tenant,
            credential.Id);

        var persisted = await store.GetAsync(key, ct: TestContext.Current.CancellationToken);

        persisted.Should().NotBeNull();
        persisted!.Id.Should().Be(credential.Id);
        persisted.Tenant.Should().Be(TenantA);
        persisted.UserKey.Should().Be(user);
        persisted.Version.Should().Be(0);
    }

    [Fact]
    public async Task ExistsAsync_WhenCredentialExists_ReturnsTrue()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var credential = CreateCredential(
            TenantA,
            UserKey.New(),
            "credential-1");

        await store.AddAsync(credential, ct: TestContext.Current.CancellationToken);

        var key = new CredentialKey(TenantA, credential.Id);

        (await store.ExistsAsync(key, ct: TestContext.Current.CancellationToken)).Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WhenCredentialDoesNotExist_ReturnsFalse()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var credential = CreateCredential(
            TenantA,
            UserKey.New(),
            "missing");

        var key = new CredentialKey(TenantA, credential.Id);

        (await store.ExistsAsync(key, ct: TestContext.Current.CancellationToken)).Should().BeFalse();
    }

    [Fact]
    public async Task GetAsync_WhenCredentialDoesNotExist_ReturnsNull()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var credential = CreateCredential(
            TenantA,
            UserKey.New(),
            "missing");

        var result = await store.GetAsync(
            new CredentialKey(TenantA, credential.Id), ct: TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    // ============================================================
    // UNIQUENESS
    // ============================================================

    [Fact]
    public async Task AddAsync_WhenUserAlreadyHasActivePasswordCredential_ThrowsConflict()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var user = UserKey.New();

        var first = CreateCredential(
            TenantA, user, "first");

        var second = CreateCredential(
            TenantA, user, "second");

        await store.AddAsync(first, ct: TestContext.Current.CancellationToken);

        var act = () => store.AddAsync(second);

        await act.Should()
            .ThrowAsync<UAuthConflictException>();
    }

    [Fact]
    public async Task AddAsync_AfterSoftDeletingExistingCredential_AllowsReplacement()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var user = UserKey.New();

        var first = CreateCredential(
            TenantA, user, "first");

        await store.AddAsync(first, ct: TestContext.Current.CancellationToken);

        await store.DeleteAsync(
            new CredentialKey(TenantA, first.Id),
            first.Version,
            DeleteMode.Soft,
            Now, ct: TestContext.Current.CancellationToken);

        var replacement = CreateCredential(
            TenantA, user, "replacement");

        var act = () => store.AddAsync(replacement);

        await act.Should().NotThrowAsync();

        var active = await store.GetByUserAsync(user, ct: TestContext.Current.CancellationToken);

        active.Should().ContainSingle();
        active.Single().Id.Should().Be(replacement.Id);
    }

    // ============================================================
    // SAVE / CONCURRENCY
    // ============================================================

    [Fact]
    public async Task SaveAsync_WhenExpectedVersionMatches_PersistsChanges()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var credential = CreateCredential(
            TenantA,
            UserKey.New(),
            "original");

        await store.AddAsync(credential, ct: TestContext.Current.CancellationToken);

        var changedAt = Now.AddMinutes(10);

        credential.ChangeSecret(
            CreatePasswordHash("changed"),
            changedAt);

        await store.SaveAsync(
            credential,
            expectedVersion: 0, ct: TestContext.Current.CancellationToken);

        var persisted = await store.GetAsync(
            new CredentialKey(TenantA, credential.Id), ct: TestContext.Current.CancellationToken);

        persisted.Should().NotBeNull();

        persisted!.SecretHash
            .Should().Be(credential.SecretHash);

        persisted.UpdatedAt
            .Should().Be(changedAt);

        persisted.Version.Should().Be(1);
    }

    [Fact]
    public async Task SaveAsync_WhenCredentialDoesNotExist_ThrowsNotFound()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var credential = CreateCredential(
            TenantA,
            UserKey.New(),
            "missing");

        var act = () => store.SaveAsync(
            credential,
            expectedVersion: 0);

        await act.Should()
            .ThrowAsync<UAuthNotFoundException>();
    }

    [Fact]
    public async Task SaveAsync_WhenExpectedVersionIsStale_ThrowsConcurrency()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var credential = CreateCredential(
            TenantA,
            UserKey.New(),
            "credential");

        await store.AddAsync(credential, ct: TestContext.Current.CancellationToken);

        var changed = credential.Revoke(
            Now.AddMinutes(5));

        await store.SaveAsync(
            changed,
            credential.Version, ct: TestContext.Current.CancellationToken);

        var act = () => store.SaveAsync(
            changed,
            expectedVersion: credential.Version);

        await act.Should()
            .ThrowAsync<UAuthConcurrencyException>();
    }

    // ============================================================
    // REVOKE
    // ============================================================

    [Fact]
    public async Task RevokeAsync_RevokesCredential()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var credential = CreateCredential(
            TenantA,
            UserKey.New(),
            "credential");

        await store.AddAsync(credential, ct: TestContext.Current.CancellationToken);

        var revokedAt = Now.AddMinutes(10);

        await store.RevokeAsync(
            new CredentialKey(TenantA, credential.Id),
            revokedAt,
            credential.Version, ct: TestContext.Current.CancellationToken);

        var persisted = await store.GetAsync(
            new CredentialKey(TenantA, credential.Id), ct: TestContext.Current.CancellationToken);

        persisted.Should().NotBeNull();
        persisted!.Security.RevokedAt.Should().Be(revokedAt);
        persisted.Version.Should().Be(1);
    }

    [Fact]
    public async Task RevokeAsync_WhenCredentialDoesNotExist_ThrowsNotFound()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var credential = CreateCredential(
            TenantA,
            UserKey.New(),
            "missing");

        var act = () => store.RevokeAsync(
            new CredentialKey(TenantA, credential.Id),
            Now,
            0);

        await act.Should()
            .ThrowAsync<UAuthNotFoundException>();
    }

    [Fact]
    public async Task RevokeAsync_WhenExpectedVersionIsStale_ThrowsConcurrency()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var credential = CreateCredential(
            TenantA,
            UserKey.New(),
            "credential");

        await store.AddAsync(credential, ct: TestContext.Current.CancellationToken);

        await store.RevokeAsync(
            new CredentialKey(TenantA, credential.Id),
            Now,
            0, ct: TestContext.Current.CancellationToken);

        var act = () => store.RevokeAsync(
            new CredentialKey(TenantA, credential.Id),
            Now.AddMinutes(1),
            0);

        await act.Should()
            .ThrowAsync<UAuthConcurrencyException>();
    }

    // ============================================================
    // DELETE
    // ============================================================

    [Fact]
    public async Task DeleteAsync_SoftDelete_HidesCredentialFromGetByUser()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var user = UserKey.New();
        var credential = CreateCredential(
            TenantA, user, "credential");

        await store.AddAsync(credential, ct: TestContext.Current.CancellationToken);

        await store.DeleteAsync(
            new CredentialKey(TenantA, credential.Id),
            credential.Version,
            DeleteMode.Soft,
            Now, ct: TestContext.Current.CancellationToken);

        var credentials = await store.GetByUserAsync(user, ct: TestContext.Current.CancellationToken);

        credentials.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteAsync_SoftDelete_PreservesCredentialRecord()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var credential = CreateCredential(
            TenantA,
            UserKey.New(),
            "credential");

        await store.AddAsync(credential, ct: TestContext.Current.CancellationToken);

        await store.DeleteAsync(
            new CredentialKey(TenantA, credential.Id),
            0,
            DeleteMode.Soft,
            Now, ct: TestContext.Current.CancellationToken);

        var persisted = await store.GetAsync(
            new CredentialKey(TenantA, credential.Id), ct: TestContext.Current.CancellationToken);

        persisted.Should().NotBeNull();
        persisted!.IsDeleted.Should().BeTrue();
        persisted.DeletedAt.Should().Be(Now);
        persisted.Version.Should().Be(1);
    }

    [Fact]
    public async Task DeleteAsync_HardDelete_RemovesCredential()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var credential = CreateCredential(
            TenantA,
            UserKey.New(),
            "credential");

        await store.AddAsync(credential, ct: TestContext.Current.CancellationToken);

        var key = new CredentialKey(
            TenantA,
            credential.Id);

        await store.DeleteAsync(
            key,
            credential.Version,
            DeleteMode.Hard,
            Now, ct: TestContext.Current.CancellationToken);

        (await store.GetAsync(key, ct: TestContext.Current.CancellationToken)).Should().BeNull();
        (await store.ExistsAsync(key, ct: TestContext.Current.CancellationToken)).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_WhenExpectedVersionIsStale_ThrowsConcurrency()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var credential = CreateCredential(
            TenantA,
            UserKey.New(),
            "credential");

        await store.AddAsync(credential, ct: TestContext.Current.CancellationToken);

        await store.RevokeAsync(
            new CredentialKey(TenantA, credential.Id),
            Now,
            0, ct: TestContext.Current.CancellationToken);

        var act = () => store.DeleteAsync(
            new CredentialKey(TenantA, credential.Id),
            expectedVersion: 0,
            DeleteMode.Soft,
            Now.AddMinutes(1));

        await act.Should()
            .ThrowAsync<UAuthConcurrencyException>();
    }

    // ============================================================
    // GET BY USER
    // ============================================================

    [Fact]
    public async Task GetByUserAsync_ReturnsOnlyActiveNonDeletedCredentialsForUser()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var userA = UserKey.New();
        var userB = UserKey.New();

        var credentialA = CreateCredential(
            TenantA, userA, "a");

        var credentialB = CreateCredential(
            TenantA, userB, "b");

        await store.AddAsync(credentialA, ct: TestContext.Current.CancellationToken);
        await store.AddAsync(credentialB, ct: TestContext.Current.CancellationToken);

        var result = await store.GetByUserAsync(userA, ct: TestContext.Current.CancellationToken);

        result.Should().ContainSingle();
        result.Single().Id.Should().Be(credentialA.Id);
    }

    // ============================================================
    // DELETE BY USER
    // ============================================================

    [Fact]
    public async Task DeleteByUserAsync_SoftDelete_DeletesUsersCredentials()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var user = UserKey.New();

        var credential = CreateCredential(
            TenantA, user, "credential");

        await store.AddAsync(credential, ct: TestContext.Current.CancellationToken);

        await store.DeleteByUserAsync(
            user,
            DeleteMode.Soft,
            Now, ct: TestContext.Current.CancellationToken);

        var active = await store.GetByUserAsync(user, ct: TestContext.Current.CancellationToken);
        active.Should().BeEmpty();

        var persisted = await store.GetAsync(
            new CredentialKey(TenantA, credential.Id), ct: TestContext.Current.CancellationToken);

        persisted.Should().NotBeNull();
        persisted!.IsDeleted.Should().BeTrue();
        persisted.DeletedAt.Should().Be(Now);
        persisted.Version.Should().Be(1);
    }

    [Fact]
    public async Task DeleteByUserAsync_HardDelete_RemovesUsersCredentials()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var user = UserKey.New();

        var credential = CreateCredential(
            TenantA, user, "credential");

        await store.AddAsync(credential, ct: TestContext.Current.CancellationToken);

        await store.DeleteByUserAsync(
            user,
            DeleteMode.Hard,
            Now, ct: TestContext.Current.CancellationToken);

        var persisted = await store.GetAsync(
            new CredentialKey(TenantA, credential.Id), ct: TestContext.Current.CancellationToken);

        persisted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteByUserAsync_DoesNotAffectOtherUsers()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var targetUser = UserKey.New();
        var otherUser = UserKey.New();

        var target = CreateCredential(
            TenantA, targetUser, "target");

        var other = CreateCredential(
            TenantA, otherUser, "other");

        await store.AddAsync(target, ct: TestContext.Current.CancellationToken);
        await store.AddAsync(other, ct: TestContext.Current.CancellationToken);

        await store.DeleteByUserAsync(
            targetUser,
            DeleteMode.Hard,
            Now, ct: TestContext.Current.CancellationToken);

        (await store.GetAsync(
            new CredentialKey(TenantA, target.Id), ct: TestContext.Current.CancellationToken))
            .Should().BeNull();

        (await store.GetAsync(
            new CredentialKey(TenantA, other.Id), ct: TestContext.Current.CancellationToken))
            .Should().NotBeNull();
    }

    // ============================================================
    // TENANT ISOLATION
    // ============================================================

    [Fact]
    public async Task GetByUserAsync_IsTenantIsolated()
    {
        await using var db = await CreateDatabaseAsync();

        var storeA = db.CreateStore(TenantA);
        var storeB = db.CreateStore(TenantB);

        var user = UserKey.New();

        var credentialA = CreateCredential(
            TenantA, user, "a");

        var credentialB = CreateCredential(
            TenantB, user, "b");

        await storeA.AddAsync(credentialA, ct: TestContext.Current.CancellationToken);
        await storeB.AddAsync(credentialB, ct: TestContext.Current.CancellationToken);

        var fromA = await storeA.GetByUserAsync(user, ct: TestContext.Current.CancellationToken);
        var fromB = await storeB.GetByUserAsync(user, ct: TestContext.Current.CancellationToken);

        fromA.Should().ContainSingle();
        fromA.Single().Id.Should().Be(credentialA.Id);

        fromB.Should().ContainSingle();
        fromB.Single().Id.Should().Be(credentialB.Id);
    }

    [Fact]
    public async Task AddAsync_WhenCredentialBelongsToDifferentTenant_IsRejected()
    {
        await using var db = await CreateDatabaseAsync();

        var store = db.CreateStore(TenantA);

        var credential = CreateCredential(
            TenantB,
            UserKey.New(),
            "cross-tenant");

        var act = () => store.AddAsync(credential);

        var exception = await act.Should()
            .ThrowAsync<UAuthConflictException>();

        exception.Which.Code.Should().Be("tenant_mismatch");
    }

    // ============================================================
    // CANCELLATION
    // ============================================================

    [Fact]
    public async Task GetByUserAsync_WhenCancelled_Throws()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => store.GetByUserAsync(
            UserKey.New(),
            cts.Token);

        await act.Should()
            .ThrowAsync<OperationCanceledException>();
    }
}