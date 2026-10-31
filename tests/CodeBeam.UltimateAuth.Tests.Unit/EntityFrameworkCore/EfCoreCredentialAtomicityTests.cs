using CodeBeam.UltimateAuth.Authentication.EntityFrameworkCore;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Security;
using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Credentials.EntityFrameworkCore;
using CodeBeam.UltimateAuth.Credentials.Reference;
using CodeBeam.UltimateAuth.EntityFrameworkCore;
using CodeBeam.UltimateAuth.Sessions.EntityFrameworkCore;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public sealed class EfCoreCredentialAtomicityTests : EfCoreTestBase
{
    private static UAuthDbContext CreateDb(SqliteConnection connection)
    {
        return CreateDbContext<UAuthDbContext>(
            connection,
            options => new UAuthDbContext(options));
    }

    [Fact]
    public async Task ChangeSecret_WhenSessionRevocationFails_ShouldRollbackCredential()
    {
        using var connection = CreateOpenConnection();

        var tenant = TenantKeys.Single;
        var user = UserKey.FromGuid(Guid.NewGuid());
        var now = DateTimeOffset.UtcNow;
        var hasher = new TestPasswordHasher();

        var credential = PasswordCredential.Create(
            Guid.NewGuid(),
            tenant,
            user,
            hasher.Hash("old-password"),
            CredentialSecurityState.Active(),
            new CredentialMetadata(),
            now);

        // Arrange: Persist the original credential.
        await using (var db = CreateDb(connection))
        {
            var store = new EfCorePasswordCredentialStore<UAuthDbContext>(
                db,
                new TenantExecutionContext(tenant));

            await store.AddAsync(credential);
        }

        // Act: Change password, then simulate a session failure.
        await using (var db = CreateDb(connection))
        {
            var store = new EfCorePasswordCredentialStore<UAuthDbContext>(
                db,
                new TenantExecutionContext(tenant));

            var existing = await store.GetAsync(
                new CredentialKey(tenant, credential.Id));

            Assert.NotNull(existing);

            await Assert.ThrowsAsync<InvalidOperationException>(
                async () =>
                {
                    await using var transaction =
                        await db.Database.BeginTransactionAsync();

                    var updated = existing.ChangeSecret(
                        hasher.Hash("new-password"),
                        now.AddMinutes(1));

                    await store.SaveAsync(
                        updated,
                        existing.Version);

                    // Simulate a failure during session revocation.
                    throw new InvalidOperationException(
                        "Simulated session revocation failure");
                });
        }

        // Assert: Read from a fresh DbContext.
        await using (var db = CreateDb(connection))
        {
            var store = new EfCorePasswordCredentialStore<UAuthDbContext>(
                db,
                new TenantExecutionContext(tenant));

            var persisted = await store.GetAsync(
                new CredentialKey(tenant, credential.Id));

            Assert.NotNull(persisted);

            Assert.True(
                hasher.Verify(
                    persisted.SecretHash,
                    "old-password"));

            Assert.False(
                hasher.Verify(
                    persisted.SecretHash,
                    "new-password"));

            Assert.Equal(0, persisted.Version);
        }
    }

    [Fact]
    public async Task AtomicExecutor_WhenOperationSucceeds_ShouldCommitCredential()
    {
        using var connection = CreateOpenConnection();

        var tenant = TenantKeys.Single;
        var user = UserKey.FromGuid(Guid.NewGuid());
        var hasher = new TestPasswordHasher();
        var now = DateTimeOffset.UtcNow;

        var credential = PasswordCredential.Create(
            Guid.NewGuid(),
            tenant,
            user,
            hasher.Hash("old-password"),
            CredentialSecurityState.Active(),
            new CredentialMetadata(),
            now);

        await using (var db = CreateDb(connection))
        {
            var store = new EfCorePasswordCredentialStore<UAuthDbContext>(
                db,
                new TenantExecutionContext(tenant));

            await store.AddAsync(credential);
        }

        await using (var db = CreateDb(connection))
        {
            var services = new ServiceCollection();

            services.AddSingleton(db);
            CodeBeam.UltimateAuth.EntityFrameworkCore.Extensions.ServiceCollectionExtensions.AddUltimateAuthEntityFrameworkCore<UAuthDbContext>(services);

            await using var provider = services.BuildServiceProvider();
            await using var scope = provider.CreateAsyncScope();

            var executor = scope.ServiceProvider
                .GetRequiredService<IUAuthAtomicExecutor>();

            var store = new EfCorePasswordCredentialStore<UAuthDbContext>(
                db,
                new TenantExecutionContext(tenant));

            await executor.ExecuteAsync(async ct =>
            {
                var existing = await store.GetAsync(
                    new CredentialKey(tenant, credential.Id),
                    ct);

                Assert.NotNull(existing);

                var updated = existing.ChangeSecret(
                    hasher.Hash("new-password"),
                    now.AddMinutes(1));

                await store.SaveAsync(
                    updated,
                    existing.Version,
                    ct);

                Assert.NotNull(db.Database.CurrentTransaction);
            });
        }

        await using (var db = CreateDb(connection))
        {
            var store = new EfCorePasswordCredentialStore<UAuthDbContext>(
                db,
                new TenantExecutionContext(tenant));

            var persisted = await store.GetAsync(
                new CredentialKey(tenant, credential.Id));

            Assert.NotNull(persisted);
            Assert.True(
                hasher.Verify(persisted.SecretHash, "new-password"));
            Assert.Equal(1, persisted.Version);
        }
    }

    [Fact]
    public async Task AtomicExecutor_WhenOperationFails_ShouldRollbackCredential()
    {
        using var connection = CreateOpenConnection();

        var tenant = TenantKeys.Single;
        var user = UserKey.FromGuid(Guid.NewGuid());
        var hasher = new TestPasswordHasher();
        var now = DateTimeOffset.UtcNow;

        var credential = PasswordCredential.Create(
            Guid.NewGuid(),
            tenant,
            user,
            hasher.Hash("old-password"),
            CredentialSecurityState.Active(),
            new CredentialMetadata(),
            now);

        await using (var db = CreateDb(connection))
        {
            var store = new EfCorePasswordCredentialStore<UAuthDbContext>(
                db,
                new TenantExecutionContext(tenant));

            await store.AddAsync(credential);
        }

        await using (var db = CreateDb(connection))
        {
            var services = new ServiceCollection();

            services.AddSingleton(db);
            CodeBeam.UltimateAuth.EntityFrameworkCore.Extensions.ServiceCollectionExtensions.AddUltimateAuthEntityFrameworkCore<UAuthDbContext>(services);

            await using var provider = services.BuildServiceProvider();
            await using var scope = provider.CreateAsyncScope();

            var executor = scope.ServiceProvider.GetRequiredService<IUAuthAtomicExecutor>();

            var store = new EfCorePasswordCredentialStore<UAuthDbContext>(
                db,
                new TenantExecutionContext(tenant));

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => executor.ExecuteAsync(async ct =>
                {
                    var existing = await store.GetAsync(
                        new CredentialKey(tenant, credential.Id),
                        ct);

                    Assert.NotNull(existing);

                    var updated = existing.ChangeSecret(
                        hasher.Hash("new-password"),
                        now.AddMinutes(1));

                    await store.SaveAsync(
                        updated,
                        existing.Version,
                        ct);

                    Assert.NotNull(db.Database.CurrentTransaction);

                    throw new InvalidOperationException(
                        "Simulated session revocation failure");
                }));
        }

        await using (var db = CreateDb(connection))
        {
            var store = new EfCorePasswordCredentialStore<UAuthDbContext>(
                db,
                new TenantExecutionContext(tenant));

            var persisted = await store.GetAsync(
                new CredentialKey(tenant, credential.Id));

            Assert.NotNull(persisted);

            Assert.True(
                hasher.Verify(persisted.SecretHash, "old-password"));

            Assert.False(
                hasher.Verify(persisted.SecretHash, "new-password"));

            Assert.Equal(0, persisted.Version);
        }
    }

    [Fact]
    public async Task AtomicExecutor_WhenSessionOperationFails_ShouldRollbackBothStores()
    {
        using var connection = CreateOpenConnection();

        var tenant = TenantKeys.Single;
        var user = UserKey.FromGuid(Guid.NewGuid());
        var hasher = new TestPasswordHasher();
        var now = DateTimeOffset.UtcNow;

        var credential = PasswordCredential.Create(
            Guid.NewGuid(),
            tenant,
            user,
            hasher.Hash("old-password"),
            CredentialSecurityState.Active(),
            new CredentialMetadata(),
            now);

        // Arrange: Persist credential and session.
        AuthSessionId.TryCreate(
            "session-aaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            out var sessionId);

        var root = UAuthSessionRoot.Create(tenant, user, now);

        var chain = UAuthSessionChain.Create(
            SessionChainId.New(),
            root.RootId,
            tenant,
            user,
            now,
            null,
            TestDevice.Default(),
            ClaimsSnapshot.Empty,
            0);

        var session = UAuthSession.Create(
            sessionId,
            tenant,
            user,
            chain.ChainId,
            now,
            now.AddHours(1),
            0,
            TestDevice.Default(),
            ClaimsSnapshot.Empty,
            SessionMetadata.Empty);

        await using (var db = CreateDb(connection))
        {
            var credentialStore =
                new EfCorePasswordCredentialStore<UAuthDbContext>(
                    db,
                    new TenantExecutionContext(tenant));

            var sessionStore =
                new EfCoreSessionStore<UAuthDbContext>(
                    db,
                    new TenantExecutionContext(tenant));

            await credentialStore.AddAsync(credential);

            await sessionStore.ExecuteAsync(async ct =>
            {
                await sessionStore.CreateRootAsync(root, ct);
                await sessionStore.CreateChainAsync(chain, ct);
                await sessionStore.CreateSessionAsync(session, ct);
            });
        }

        // Act: Change password and revoke session within one atomic operation.
        await using (var db = CreateDb(connection))
        {
            var services = new ServiceCollection();

            services.AddSingleton(db);

            CodeBeam.UltimateAuth.EntityFrameworkCore.Extensions
                .ServiceCollectionExtensions
                .AddUltimateAuthEntityFrameworkCore<UAuthDbContext>(services);

            await using var provider = services.BuildServiceProvider();
            await using var scope = provider.CreateAsyncScope();

            var executor = scope.ServiceProvider
                .GetRequiredService<IUAuthAtomicExecutor>();

            var credentialStore =
                new EfCorePasswordCredentialStore<UAuthDbContext>(
                    db,
                    new TenantExecutionContext(tenant));

            var sessionStore =
                new EfCoreSessionStore<UAuthDbContext>(
                    db,
                    new TenantExecutionContext(tenant));

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => executor.ExecuteAsync(async ct =>
                {
                    var existing = await credentialStore.GetAsync(
                        new CredentialKey(tenant, credential.Id),
                        ct);

                    Assert.NotNull(existing);

                    var updated = existing.ChangeSecret(
                        hasher.Hash("new-password"),
                        now.AddMinutes(1));

                    await credentialStore.SaveAsync(
                        updated,
                        existing.Version,
                        ct);

                    await sessionStore.ExecuteAsync(async sessionCt =>
                    {
                        await sessionStore.RevokeAllChainsAsync(
                            user,
                            now.AddMinutes(1),
                            sessionCt);
                    }, ct);

                    // Session operation succeeded, but the outer operation fails.
                    throw new InvalidOperationException(
                        "Simulated failure before outer commit");
                }));
        }

        // Assert: Both credential and session changes must be rolled back.
        await using (var db = CreateDb(connection))
        {
            var credentialStore =
                new EfCorePasswordCredentialStore<UAuthDbContext>(
                    db,
                    new TenantExecutionContext(tenant));

            var sessionStore =
                new EfCoreSessionStore<UAuthDbContext>(
                    db,
                    new TenantExecutionContext(tenant));

            var persisted = await credentialStore.GetAsync(
                new CredentialKey(tenant, credential.Id));

            Assert.NotNull(persisted);

            Assert.True(
                hasher.Verify(
                    persisted.SecretHash,
                    "old-password"));

            Assert.Equal(0, persisted.Version);

            var persistedSession = await sessionStore.GetSessionAsync(sessionId);

            Assert.NotNull(persistedSession);
            Assert.False(persistedSession.IsRevoked);
        }
    }

    [Fact]
    public async Task AtomicReset_WhenSessionRevocationFails_ShouldRollbackSecurityStateAndCredential()
    {
        using var connection = CreateOpenConnection();

        var tenant = TenantKeys.Single;
        var user = UserKey.FromGuid(Guid.NewGuid());
        var now = DateTimeOffset.UtcNow;
        var hasher = new TestPasswordHasher();

        const string resetTokenHash = "test-reset-token-hash";

        var credential = PasswordCredential.Create(
            Guid.NewGuid(),
            tenant,
            user,
            hasher.Hash("old-password"),
            CredentialSecurityState.Active(),
            new CredentialMetadata(),
            now);

        var securityState = AuthenticationSecurityState
            .CreateAccount(tenant, user);

        // Arrange: Persist an active reset state and original password.
        await using (var db = CreateDb(connection))
        {
            var credentialStore =
                new EfCorePasswordCredentialStore<UAuthDbContext>(
                    db,
                    new TenantExecutionContext(tenant));

            var securityStore =
                new EfCoreAuthenticationSecurityStateStore<UAuthDbContext>(
                    db,
                    new TenantExecutionContext(tenant));

            await credentialStore.AddAsync(credential);

            await securityStore.AddAsync(securityState);

            var activeReset = securityState.BeginReset(
                resetTokenHash,
                now,
                TimeSpan.FromMinutes(10));

            await securityStore.UpdateAsync(
                activeReset,
                securityState.SecurityVersion);
        }

        // Act: Consume reset token and change password,
        // then fail before committing the transaction.
        await using (var db = CreateDb(connection))
        {
            var services = new ServiceCollection();

            services.AddSingleton(db);

            CodeBeam.UltimateAuth.EntityFrameworkCore.Extensions
                .ServiceCollectionExtensions
                .AddUltimateAuthEntityFrameworkCore<UAuthDbContext>(services);

            await using var provider = services.BuildServiceProvider();
            await using var scope = provider.CreateAsyncScope();

            var executor = scope.ServiceProvider
                .GetRequiredService<IUAuthAtomicExecutor>();

            var credentialStore =
                new EfCorePasswordCredentialStore<UAuthDbContext>(
                    db,
                    new TenantExecutionContext(tenant));

            var securityStore =
                new EfCoreAuthenticationSecurityStateStore<UAuthDbContext>(
                    db,
                    new TenantExecutionContext(tenant));

            var sessionStore =
                new EfCoreSessionStore<UAuthDbContext>(
                    db,
                    new TenantExecutionContext(tenant));

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => executor.ExecuteAsync(async ct =>
                {
                    var state = await securityStore.GetAsync(
                        user,
                        AuthenticationSecurityScope.Account,
                        null,
                        ct);

                    Assert.NotNull(state);
                    Assert.True(state.HasActiveReset(now));

                    var consumed = state.ConsumeReset(now);

                    await securityStore.UpdateAsync(
                        consumed,
                        state.SecurityVersion,
                        ct);

                    var existing = await credentialStore.GetAsync(
                        new CredentialKey(tenant, credential.Id),
                        ct);

                    Assert.NotNull(existing);

                    var updated = existing.ChangeSecret(
                        hasher.Hash("new-password"),
                        now);

                    await credentialStore.SaveAsync(
                        updated,
                        existing.Version,
                        ct);

                    await sessionStore.ExecuteAsync(async sessionCt =>
                    {
                        await sessionStore.RevokeAllChainsAsync(
                            user,
                            now,
                            sessionCt);
                    }, ct);

                    throw new InvalidOperationException(
                        "Simulated failure before reset commit");
                }));
        }

        // Assert: Both security state and credential must be unchanged.
        await using (var db = CreateDb(connection))
        {
            var credentialStore =
                new EfCorePasswordCredentialStore<UAuthDbContext>(
                    db,
                    new TenantExecutionContext(tenant));

            var securityStore =
                new EfCoreAuthenticationSecurityStateStore<UAuthDbContext>(
                    db,
                    new TenantExecutionContext(tenant));

            var persistedCredential = await credentialStore.GetAsync(
                new CredentialKey(tenant, credential.Id));

            Assert.NotNull(persistedCredential);

            Assert.True(
                hasher.Verify(
                    persistedCredential.SecretHash,
                    "old-password"));

            Assert.False(
                hasher.Verify(
                    persistedCredential.SecretHash,
                    "new-password"));

            Assert.Equal(0, persistedCredential.Version);

            var persistedState = await securityStore.GetAsync(
                user,
                AuthenticationSecurityScope.Account,
                null);

            Assert.NotNull(persistedState);

            Assert.True(persistedState.HasActiveReset(now));
            Assert.Null(persistedState.ResetConsumedAt);
            Assert.Equal(resetTokenHash, persistedState.ResetTokenHash);
        }
    }
}
