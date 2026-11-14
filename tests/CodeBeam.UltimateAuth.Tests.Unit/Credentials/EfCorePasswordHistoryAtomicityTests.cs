using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Credentials.EntityFrameworkCore;
using CodeBeam.UltimateAuth.Credentials.Options;
using CodeBeam.UltimateAuth.Credentials.Policies;
using CodeBeam.UltimateAuth.Credentials.Reference;
using CodeBeam.UltimateAuth.Server.ConcurrencyLimiting;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Unit.Credentials;

public sealed class EfCorePasswordHistoryAtomicityTests
{
    private static readonly TenantKey Tenant = TenantKey.Single;
    private static readonly DateTimeOffset Now =
        new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

    private static PasswordHash Hash(string value) =>
        PasswordHash.Create("test", value);

    private static EffectivePasswordPolicyOptions Options(int rememberLast) =>
        PasswordPolicyOptionsResolver.Resolve(
            new PasswordPolicyOptions
            {
                History = new PasswordHistoryOptions
                {
                    RememberLast = rememberLast
                }
            });

    [Theory]
    [InlineData("add")]
    [InlineData("prune")]
    public async Task FailedHistoryOperation_RollsBackCredentialAndHistory(
        string failurePoint)
    {
        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync(cancellationToken: TestContext.Current.CancellationToken);

        var dbOptions = new DbContextOptionsBuilder<UAuthCredentialDbContext>()
            .UseSqlite(connection)
            .Options;

        Guid credentialId;
        Guid originalHistoryId;

        await using (var db = new UAuthCredentialDbContext(dbOptions))
        {
            await db.Database.EnsureCreatedAsync(cancellationToken: TestContext.Current.CancellationToken);

            var user = UserKey.New();

            var credential = PasswordCredential.Create(
                id: Guid.NewGuid(),
                tenant: Tenant,
                userKey: user,
                secretHash: Hash("current"),
                security: CredentialSecurityState.Active(),
                metadata: new CredentialMetadata(),
                now: Now);

            credentialId = credential.Id;

            var credentials = new EfCorePasswordCredentialStore<UAuthCredentialDbContext>(
                db, new TenantExecutionContext(Tenant));

            var history = new EfCorePasswordHistoryStore<UAuthCredentialDbContext>(
                db, Tenant);

            await credentials.AddAsync(credential, ct: TestContext.Current.CancellationToken);

            originalHistoryId = Guid.NewGuid();

            await history.AddAsync(new PasswordHistoryEntry
            {
                Id = originalHistoryId,
                Tenant = Tenant,
                UserKey = user,
                CredentialKey = new CredentialKey(Tenant, credentialId),
                SecretHash = Hash("older"),
                ReplacedAt = Now.AddDays(-1)
            }, ct: TestContext.Current.CancellationToken);

            var pipeline = new Mock<IPasswordValidationPipeline>();
            pipeline.Setup(x => x.ValidateAsync(
                    It.IsAny<PasswordValidationRequest>(),
                    It.IsAny<EffectivePasswordPolicyOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(PasswordPolicyResult.Success());

            var hasher = new Mock<IUAuthPasswordHasher>();
            hasher.Setup(x => x.Hash(It.IsAny<string>()))
                .Returns((string password) => Hash(password));

            var credentialFactory = new Mock<IPasswordCredentialStoreFactory>();
            credentialFactory.Setup(x => x.Create(Tenant))
                .Returns(credentials);

            var historyFactory = new Mock<IPasswordHistoryStoreFactory>();

            var failingHistory = new Mock<IPasswordHistoryStore>();

            failingHistory.Setup(x => x.AddAsync(
                    It.IsAny<PasswordHistoryEntry>(),
                    It.IsAny<CancellationToken>()))
                .Returns<PasswordHistoryEntry, CancellationToken>(
                    (entry, ct) =>
                        failurePoint == "add"
                            ? Task.FromException(
                                new InvalidOperationException("Injected add failure"))
                            : history.AddAsync(entry, ct));

            failingHistory.Setup(x => x.PruneAsync(
                    It.IsAny<CredentialKey>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .Returns<CredentialKey, int, CancellationToken>(
                    (key, count, ct) =>
                        failurePoint == "prune"
                            ? Task.FromException(
                                new InvalidOperationException("Injected prune failure"))
                            : history.PruneAsync(key, count, ct));

            historyFactory.Setup(x => x.Create(Tenant))
                .Returns(failingHistory.Object);

            using var limiter = new UAuthPasswordHashLimiter(Microsoft.Extensions.Options.Options.Create(new UAuthServerOptions()));

            var writer = new PasswordCredentialWriter(
                pipeline.Object,
                credentialFactory.Object,
                historyFactory.Object,
                hasher.Object,
                limiter);

            await using var transaction =
                await db.Database.BeginTransactionAsync(cancellationToken: TestContext.Current.CancellationToken);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                writer.ChangeAsync(
                    credential,
                    "new-password",
                    new PasswordPolicyContext
                    {
                        Password = "new-password"
                    },
                    Options(2),
                    Now.AddMinutes(1),
                    TestContext.Current.CancellationToken));

            // Cleanup must complete even when the test is cancelled.
            await transaction.RollbackAsync();
        }

        // Verify using a fresh DbContext, not tracked entities.
        await using (var verifyDb = new UAuthCredentialDbContext(dbOptions))
        {
            var savedCredential = await verifyDb.PasswordCredentials
                .AsNoTracking()
                .SingleAsync(x => x.Id == credentialId, cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal(Hash("current"), savedCredential.SecretHash);
            Assert.Equal(0, savedCredential.Version);

            var history = await verifyDb.PasswordHistory
                .AsNoTracking()
                .Where(x => x.CredentialId == credentialId)
                .ToListAsync(cancellationToken: TestContext.Current.CancellationToken);

            Assert.Single(history);
            Assert.Equal(originalHistoryId, history[0].Id);
            Assert.Equal(Hash("older"), history[0].SecretHash);
        }
    }
}
