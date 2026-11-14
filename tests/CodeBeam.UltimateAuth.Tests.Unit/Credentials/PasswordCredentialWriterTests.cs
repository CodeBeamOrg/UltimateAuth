using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Credentials.Options;
using CodeBeam.UltimateAuth.Credentials.Policies;
using CodeBeam.UltimateAuth.Credentials.Reference;
using CodeBeam.UltimateAuth.Server.ConcurrencyLimiting;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.Extensions.Options;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Unit.Credentials;

public sealed class PasswordCredentialWriterTests
{
    private static readonly TenantKey Tenant = TenantKey.Single;
    private static readonly DateTimeOffset Now =
        new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

    private static PasswordHash Hash(string value) =>
        PasswordHash.Create("test", value);

    private static EffectivePasswordPolicyOptions Options(
        int rememberLast = 0) =>
        PasswordPolicyOptionsResolver.Resolve(
            new PasswordPolicyOptions
            {
                History = new PasswordHistoryOptions
                {
                    RememberLast = rememberLast
                }
            });

    private static PasswordPolicyContext Context() => new()
    {
        Password = ""
    };

    private static PasswordCredential Credential()
    {
        return PasswordCredential.Create(
            id: Guid.NewGuid(),
            tenant: Tenant,
            userKey: UserKey.New(),
            secretHash: Hash("old"),
            security: CredentialSecurityState.Active(),
            metadata: new CredentialMetadata(),
            now: Now);
    }

    private sealed class Fixture
    {
        public Mock<IPasswordValidationPipeline> Pipeline { get; } = new();
        public Mock<IPasswordCredentialStore> Credentials { get; } = new();
        public Mock<IPasswordCredentialStoreFactory> CredentialFactory { get; } = new();
        public Mock<IPasswordHistoryStore> History { get; } = new();
        public Mock<IPasswordHistoryStoreFactory> HistoryFactory { get; } = new();
        public Mock<IUAuthPasswordHasher> Hasher { get; } = new();
        public UAuthPasswordHashLimiter Limiter { get; } = new(Microsoft.Extensions.Options.Options.Create(new UAuthServerOptions()));

        public PasswordCredentialWriter Writer { get; }

        public Fixture()
        {
            Pipeline.Setup(x => x.ValidateAsync(
                    It.IsAny<PasswordValidationRequest>(),
                    It.IsAny<EffectivePasswordPolicyOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(PasswordPolicyResult.Success());

            CredentialFactory.Setup(x => x.Create(Tenant))
                .Returns(Credentials.Object);

            HistoryFactory.Setup(x => x.Create(Tenant))
                .Returns(History.Object);

            Hasher.Setup(x => x.Hash(It.IsAny<string>()))
                .Returns((string password) => Hash(password));

            Writer = new PasswordCredentialWriter(
                Pipeline.Object,
                CredentialFactory.Object,
                HistoryFactory.Object,
                Hasher.Object,
                Limiter);
        }
    }

    [Fact]
    public async Task Create_ValidatesAndPersistsHashedPassword()
    {
        var f = new Fixture();
        var user = UserKey.New();

        PasswordCredential? saved = null;

        f.Credentials
            .Setup(x => x.AddAsync(
                It.IsAny<PasswordCredential>(),
                It.IsAny<CancellationToken>()))
            .Callback<PasswordCredential, CancellationToken>(
                (credential, _) => saved = credential)
            .Returns(Task.CompletedTask);

        var result = await f.Writer.CreateAsync(
            Tenant, user, "new-password",
            Context(), Options(3), Now, TestContext.Current.CancellationToken);

        Assert.Same(result, saved);
        Assert.Equal(user, result.UserKey);
        Assert.Equal(Hash("new-password"), result.SecretHash);

        f.Pipeline.Verify(x => x.ValidateAsync(
            It.Is<PasswordValidationRequest>(r =>
                r.Context.Password == "new-password" &&
                r.ExistingCredentialKey == null),
            It.IsAny<EffectivePasswordPolicyOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);

        f.HistoryFactory.Verify(
            x => x.Create(It.IsAny<TenantKey>()),
            Times.Never);
    }

    [Fact]
    public async Task Change_WithHistory_StoresOldHashAndPrunes()
    {
        var f = new Fixture();
        var credential = Credential();
        var originalHash = credential.SecretHash;
        var key = new CredentialKey(Tenant, credential.Id);

        PasswordHistoryEntry? added = null;
        var calls = new List<string>();

        f.Credentials.Setup(x => x.SaveAsync(
                It.IsAny<PasswordCredential>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("save"))
            .Returns(Task.CompletedTask);

        f.History.Setup(x => x.AddAsync(
                It.IsAny<PasswordHistoryEntry>(),
                It.IsAny<CancellationToken>()))
            .Callback<PasswordHistoryEntry, CancellationToken>(
                (entry, _) =>
                {
                    calls.Add("history");
                    added = entry;
                })
            .Returns(Task.CompletedTask);

        f.History.Setup(x => x.PruneAsync(
                key, 3, It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("prune"))
            .Returns(Task.CompletedTask);

        var updated = await f.Writer.ChangeAsync(
            credential, "new-password",
            Context(), Options(3),
            Now.AddMinutes(1), TestContext.Current.CancellationToken);

        Assert.Equal(Hash("new-password"), updated.SecretHash);
        Assert.NotNull(added);
        Assert.NotEqual(Guid.Empty, added.Id);
        Assert.Equal(originalHash, added.SecretHash);
        Assert.Equal(key, added.CredentialKey);
        Assert.Equal(credential.UserKey, added.UserKey);
        Assert.Equal(Tenant, added.Tenant);
        Assert.Equal(Now.AddMinutes(1), added.ReplacedAt);

        Assert.Equal(new[] { "save", "history", "prune" }, calls);

        f.Credentials.Verify(x => x.SaveAsync(
            It.IsAny<PasswordCredential>(),
            0,
            It.IsAny<CancellationToken>()), Times.Once);

        f.Pipeline.Verify(x => x.ValidateAsync(
            It.Is<PasswordValidationRequest>(r =>
                r.Context.Password == "new-password" &&
                r.ExistingCredentialKey == key),
            It.IsAny<EffectivePasswordPolicyOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Change_WithoutHistory_PrunesAllButDoesNotAdd()
    {
        var f = new Fixture();
        var credential = Credential();
        var key = new CredentialKey(Tenant, credential.Id);

        await f.Writer.ChangeAsync(
            credential, "new-password",
            Context(), Options(0),
            Now.AddMinutes(1), TestContext.Current.CancellationToken);

        f.History.Verify(x => x.AddAsync(
            It.IsAny<PasswordHistoryEntry>(),
            It.IsAny<CancellationToken>()), Times.Never);

        f.History.Verify(x => x.PruneAsync(
            key, 0, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ValidationFailure_PreventsPersistence(
        bool change)
    {
        var f = new Fixture();

        f.Pipeline.Setup(x => x.ValidateAsync(
                It.IsAny<PasswordValidationRequest>(),
                It.IsAny<EffectivePasswordPolicyOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(PasswordPolicyResult.Failure(
                new PasswordPolicyViolation(
                    PasswordPolicyErrorCodes.ReusedPassword,
                    "Password was used previously.")));

        var exception = await Assert.ThrowsAsync<UAuthValidationException>(
            async () =>
            {
                if (change)
                {
                    await f.Writer.ChangeAsync(
                        Credential(), "reused",
                        Context(), Options(3), Now, TestContext.Current.CancellationToken);
                }
                else
                {
                    await f.Writer.CreateAsync(
                        Tenant, UserKey.New(), "reused",
                        Context(), Options(3), Now, TestContext.Current.CancellationToken);
                }
            });

        Assert.Equal(PasswordPolicyErrorCodes.ReusedPassword, exception.Code);

        f.Hasher.Verify(
            x => x.Hash(It.IsAny<string>()),
            Times.Never);

        f.CredentialFactory.Verify(
            x => x.Create(It.IsAny<TenantKey>()),
            Times.Never);

        f.HistoryFactory.Verify(
            x => x.Create(It.IsAny<TenantKey>()),
            Times.Never);
    }

    [Fact]
    public async Task Change_WhenCredentialSaveFails_DoesNotWriteHistory()
    {
        var f = new Fixture();

        f.Credentials.Setup(x => x.SaveAsync(
                It.IsAny<PasswordCredential>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("save failed"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            f.Writer.ChangeAsync(
                Credential(), "new-password",
                Context(), Options(3), Now, TestContext.Current.CancellationToken));

        f.HistoryFactory.Verify(
            x => x.Create(It.IsAny<TenantKey>()),
            Times.Never);
    }

    [Fact]
    public async Task Change_WhenHistoryAddFails_DoesNotPrune()
    {
        var f = new Fixture();

        f.History.Setup(x => x.AddAsync(
                It.IsAny<PasswordHistoryEntry>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("history failed"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            f.Writer.ChangeAsync(
                Credential(), "new-password",
                Context(), Options(3), Now, TestContext.Current.CancellationToken));

        f.History.Verify(x => x.PruneAsync(
            It.IsAny<CredentialKey>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Change_WhenPruneFails_PropagatesFailure()
    {
        var f = new Fixture();

        f.History.Setup(x => x.PruneAsync(
                It.IsAny<CredentialKey>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("prune failed"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            f.Writer.ChangeAsync(
                Credential(), "new-password",
                Context(), Options(3), Now, TestContext.Current.CancellationToken));

        f.History.Verify(x => x.AddAsync(
            It.IsAny<PasswordHistoryEntry>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
