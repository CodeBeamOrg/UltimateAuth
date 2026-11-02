using AwesomeAssertions;
using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Credentials.Reference;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Unit.Credentials;

public sealed class UAuthPasswordHistoryValidatorTests
{
    private static readonly TenantKey Tenant = TenantKey.Single;

    private static readonly DateTimeOffset Now = new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

    private static PasswordHash Hash(string password) => PasswordHash.Create("test", password);

    private static PasswordCredential Credential(
        CredentialKey key,
        UserKey user,
        string password) =>
        PasswordCredential.Create(
            id: key.Id,
            tenant: key.Tenant,
            userKey: user,
            secretHash: Hash(password),
            security: CredentialSecurityState.Active(),
            metadata: new CredentialMetadata(),
            now: Now);

    private static PasswordHistoryEntry History(
        CredentialKey key,
        UserKey user,
        string password,
        int minutes) => new()
        {
            Id = Guid.NewGuid(),
            Tenant = key.Tenant,
            UserKey = user,
            CredentialKey = key,
            SecretHash = Hash(password),
            ReplacedAt = Now.AddMinutes(minutes)
        };

    private static TestFixture Create(
        string currentPassword = "CurrentPassword123!")
    {
        var key = new CredentialKey(Tenant, Guid.NewGuid());
        var user = UserKey.New();

        var credentialStore = new Mock<IPasswordCredentialStore>();
        var credentialFactory = new Mock<IPasswordCredentialStoreFactory>();
        var historyStore = new Mock<IPasswordHistoryStore>();
        var historyFactory = new Mock<IPasswordHistoryStoreFactory>();
        var hasher = new Mock<IUAuthPasswordHasher>();

        credentialFactory
            .Setup(x => x.Create(Tenant))
            .Returns(credentialStore.Object);

        credentialStore
            .Setup(x => x.GetAsync(
                key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Credential(
                key, user, currentPassword));

        historyFactory
            .Setup(x => x.Create(Tenant))
            .Returns(historyStore.Object);

        hasher
            .Setup(x => x.Verify(
                It.IsAny<PasswordHash>(),
                It.IsAny<string>()))
            .Returns((PasswordHash hash, string password) =>
                hash == Hash(password));

        var validator = new UAuthPasswordHistoryValidator(
            credentialFactory.Object,
            historyFactory.Object,
            hasher.Object);

        return new TestFixture(
            key, user, validator,
            credentialStore, credentialFactory,
            historyStore, historyFactory, hasher);
    }

    private sealed record TestFixture(
        CredentialKey Key,
        UserKey User,
        UAuthPasswordHistoryValidator Validator,
        Mock<IPasswordCredentialStore> CredentialStore,
        Mock<IPasswordCredentialStoreFactory> CredentialFactory,
        Mock<IPasswordHistoryStore> HistoryStore,
        Mock<IPasswordHistoryStoreFactory> HistoryFactory,
        Mock<IUAuthPasswordHasher> Hasher);

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task CurrentPassword_IsAlwaysRejected(
        int rememberLast)
    {
        var f = Create();

        var result = await f.Validator.ValidateAsync(
            f.Key, "CurrentPassword123!", rememberLast);

        result.Succeeded.Should().BeFalse();
        result.Violations.Should().ContainSingle(
            x => x.Code == PasswordPolicyErrorCodes.ReusedPassword);

        f.HistoryFactory.Verify(
            x => x.Create(It.IsAny<TenantKey>()),
            Times.Never);
    }

    [Fact]
    public async Task RememberLastZero_AllowsDifferentPassword_WithoutHistoryLookup()
    {
        var f = Create();

        var result = await f.Validator.ValidateAsync(
            f.Key, "DifferentPassword123!", 0);

        result.Succeeded.Should().BeTrue();

        f.HistoryFactory.Verify(
            x => x.Create(It.IsAny<TenantKey>()),
            Times.Never);
    }

    [Fact]
    public async Task HistoricalPassword_IsRejected()
    {
        var f = Create();

        f.HistoryStore
            .Setup(x => x.GetRecentAsync(
                f.Key, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                History(f.Key, f.User, "Previous1!", 3),
                History(f.Key, f.User, "Previous2!", 2),
                History(f.Key, f.User, "Previous3!", 1)
            });

        var result = await f.Validator.ValidateAsync(
            f.Key, "Previous2!", 3);

        result.Succeeded.Should().BeFalse();
        result.Violations.Should().ContainSingle(
            x => x.Code == PasswordPolicyErrorCodes.ReusedPassword);
    }

    [Fact]
    public async Task NewPassword_NotPresentInHistory_IsAllowed()
    {
        var f = Create();

        f.HistoryStore
            .Setup(x => x.GetRecentAsync(
                f.Key, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                History(f.Key, f.User, "Previous1!", 2),
                History(f.Key, f.User, "Previous2!", 1)
            });

        var result = await f.Validator.ValidateAsync(
            f.Key, "EntirelyNewPassword123!", 2);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyHistory_AllowsDifferentPassword()
    {
        var f = Create();

        f.HistoryStore
            .Setup(x => x.GetRecentAsync(
                f.Key, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<PasswordHistoryEntry>());

        var result = await f.Validator.ValidateAsync(
            f.Key, "NewPassword123!", 5);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task HistoryLookup_UsesRequestedLimit()
    {
        var f = Create();

        f.HistoryStore
            .Setup(x => x.GetRecentAsync(
                f.Key, 7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<PasswordHistoryEntry>());

        await f.Validator.ValidateAsync(
            f.Key, "NewPassword123!", 7);

        f.HistoryStore.Verify(
            x => x.GetRecentAsync(
                f.Key, 7, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(11)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public async Task InvalidRememberLast_ThrowsBeforeStoreAccess(
        int rememberLast)
    {
        var f = Create();

        var act = () => f.Validator.ValidateAsync(
            f.Key, "NewPassword123!", rememberLast);

        await act.Should()
            .ThrowAsync<ArgumentOutOfRangeException>();

        f.CredentialFactory.VerifyNoOtherCalls();
        f.HistoryFactory.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task MissingCredential_Throws()
    {
        var f = Create();

        f.CredentialStore
            .Setup(x => x.GetAsync(
                f.Key, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PasswordCredential?)null);

        var act = () => f.Validator.ValidateAsync(
            f.Key, "NewPassword123!", 3);

        await act.Should()
            .ThrowAsync<InvalidOperationException>();

        f.HistoryFactory.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HistoryVerification_StopsAtFirstMatch()
    {
        var f = Create();

        f.HistoryStore
            .Setup(x => x.GetRecentAsync(
                f.Key, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                History(f.Key, f.User, "Match123!", 3),
                History(f.Key, f.User, "Older123!", 2),
                History(f.Key, f.User, "Oldest123!", 1)
            });

        var result = await f.Validator.ValidateAsync(
            f.Key, "Match123!", 3);

        result.Succeeded.Should().BeFalse();

        // Current credential + first history entry only.
        f.Hasher.Verify(
            x => x.Verify(
                It.IsAny<PasswordHash>(),
                "Match123!"),
            Times.Exactly(2));
    }

    [Fact]
    public async Task CancelledToken_ThrowsBeforeStoreAccess()
    {
        var f = Create();

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => f.Validator.ValidateAsync(
            f.Key, "NewPassword123!", 3, cts.Token);

        await act.Should()
            .ThrowAsync<OperationCanceledException>();

        f.CredentialFactory.VerifyNoOtherCalls();
        f.HistoryFactory.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CancellationToken_IsForwardedToStores()
    {
        var f = Create();

        using var cts = new CancellationTokenSource();
        var ct = cts.Token;

        f.CredentialStore
            .Setup(x => x.GetAsync(f.Key, ct))
            .ReturnsAsync(Credential(
                f.Key, f.User, "CurrentPassword123!"));

        f.HistoryStore
            .Setup(x => x.GetRecentAsync(f.Key, 2, ct))
            .ReturnsAsync(Array.Empty<PasswordHistoryEntry>());

        var result = await f.Validator.ValidateAsync(
            f.Key, "NewPassword123!", 2, ct);

        result.Succeeded.Should().BeTrue();

        f.CredentialStore.Verify(
            x => x.GetAsync(f.Key, ct),
            Times.Once);

        f.HistoryStore.Verify(
            x => x.GetRecentAsync(f.Key, 2, ct),
            Times.Once);
    }
}
