using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Credentials.Reference;
using CodeBeam.UltimateAuth.Credentials.Reference.Internal;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using CodeBeam.UltimateAuth.Users;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public sealed class CredentialManagementServiceTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 9, 21, 12, 0, 0, TimeSpan.Zero);

    // ---------------------------------------------------------
    // GetAll
    // ---------------------------------------------------------

    [Fact]
    public async Task GetAllAsync_ReturnsCredentialsForTargetUser()
    {
        var f = CreateFixture();
        var user = UserKey.New();

        var context = TestAccessContext.ForUser(
            user,
            UAuthActions.Credentials.ListSelf);

        var credential = CreateCredential(
            user,
            f.OldPasswordHash,
            version: 7);

        f.CredentialStore
            .Setup(x => x.GetByUserAsync(
                user,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { credential });

        var result = await f.Sut.GetAllAsync(context);

        result.Credentials.Should().ContainSingle();

        var dto = result.Credentials.Single();
        dto.Id.Should().Be(credential.Id);
        dto.Type.Should().Be(CredentialType.Password);
        dto.Status.Should().Be(CredentialSecurityStatus.Active);
        dto.Version.Should().Be(7);
    }

    // ---------------------------------------------------------
    // Add
    // ---------------------------------------------------------

    [Fact]
    public async Task AddAsync_HashesSecretAndPersistsCredentialForTargetUser()
    {
        var f = CreateFixture();
        var user = UserKey.New();

        var context = TestAccessContext.ForUser(
            user,
            UAuthActions.Credentials.AddSelf);

        f.Hasher
            .Setup(x => x.Hash("new-password"))
            .Returns(f.OldPasswordHash);

        PasswordCredential? persisted = null;

        f.CredentialStore
            .Setup(x => x.AddAsync(
                It.IsAny<PasswordCredential>(),
                It.IsAny<CancellationToken>()))
            .Callback<PasswordCredential, CancellationToken>(
                (credential, _) => persisted = credential)
            .Returns(Task.CompletedTask);

        var result = await f.Sut.AddAsync(
            context,
            new AddCredentialRequest
            {
                Type = CredentialType.Password,
                Secret = "new-password"
            });

        result.Succeeded.Should().BeTrue();

        persisted.Should().NotBeNull();
        persisted!.UserKey.Should().Be(user);
        persisted.Tenant.Should().Be(context.ResourceTenant);
        persisted.SecretHash.Should().Be(f.OldPasswordHash);

        result.Id.Should().Be(persisted.Id);
        result.Type.Should().Be(CredentialType.Password);
    }

    // ---------------------------------------------------------
    // ChangeSecret
    // ---------------------------------------------------------

    [Fact]
    public async Task ChangeSecretAsync_WhenCredentialDoesNotExist_ThrowsNotFound()
    {
        var f = CreateFixture();
        var user = UserKey.New();

        var context = TestAccessContext.ForUser(
            user,
            UAuthActions.Credentials.ChangeSelf);

        f.CredentialStore
            .Setup(x => x.GetByUserAsync(
                user,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<PasswordCredential>());

        var act = () => f.Sut.ChangeSecretAsync(
            context,
            new ChangeCredentialRequest
            {
                CurrentSecret = "old",
                NewSecret = "new"
            });

        await act.Should()
            .ThrowAsync<UAuthNotFoundException>()
            .WithMessage("*credential_not_found*");
    }

    [Fact]
    public async Task ChangeSecretAsync_Self_WhenCurrentSecretMissing_ThrowsNotFound()
    {
        var f = CreateFixture();
        var user = UserKey.New();

        var context = TestAccessContext.ForUser(
            user,
            UAuthActions.Credentials.ChangeSelf);

        var credential = CreateCredential(user, f.OldPasswordHash);

        f.CredentialStore
            .Setup(x => x.GetByUserAsync(
                user,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { credential });

        var act = () => f.Sut.ChangeSecretAsync(
            context,
            new ChangeCredentialRequest
            {
                CurrentSecret = null,
                NewSecret = "new-password"
            });

        await act.Should()
            .ThrowAsync<UAuthNotFoundException>()
            .WithMessage("*current_secret_required*");

        f.Hasher.Verify(
            x => x.Hash(It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task ChangeSecretAsync_Self_WhenCurrentSecretInvalid_ThrowsConflict()
    {
        var f = CreateFixture();
        var user = UserKey.New();

        var context = TestAccessContext.ForUser(
            user,
            UAuthActions.Credentials.ChangeSelf);

        var credential = CreateCredential(user, f.OldPasswordHash);

        f.CredentialStore
            .Setup(x => x.GetByUserAsync(
                user,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { credential });

        f.Hasher
            .Setup(x => x.Verify(
                f.OldPasswordHash,
                "wrong-password"))
            .Returns(false);

        var act = () => f.Sut.ChangeSecretAsync(
            context,
            new ChangeCredentialRequest
            {
                CurrentSecret = "wrong-password",
                NewSecret = "new-password"
            });

        var exception = await act.Should()
            .ThrowAsync<UAuthConflictException>();

        exception.Which.Code.Should().Be("invalid_credentials");
    }

    [Fact]
    public async Task ChangeSecretAsync_WhenNewSecretMatchesCurrent_ThrowsValidation()
    {
        var f = CreateFixture();
        var user = UserKey.New();

        var context = TestAccessContext.ForUser(
            user,
            UAuthActions.Credentials.ChangeSelf);

        var credential = CreateCredential(user, f.OldPasswordHash);

        f.CredentialStore
            .Setup(x => x.GetByUserAsync(
                user,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { credential });

        f.Hasher
            .Setup(x => x.Verify(
                f.OldPasswordHash,
                "current-password"))
            .Returns(true);

        f.Hasher
            .Setup(x => x.Verify(
                f.OldPasswordHash,
                "same-password"))
            .Returns(true);

        var act = () => f.Sut.ChangeSecretAsync(
            context,
            new ChangeCredentialRequest
            {
                CurrentSecret = "current-password",
                NewSecret = "same-password"
            });

        var exception = await act.Should()
            .ThrowAsync<UAuthValidationException>();

        exception.Which.Code.Should().Be("credential_secret_same");

        f.CredentialStore.Verify(
            x => x.SaveAsync(
                It.IsAny<PasswordCredential>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ChangeSecretAsync_Self_WhenValid_SavesAndRevokesOtherChains()
    {
        var f = CreateFixture();
        var user = UserKey.New();
        var chainId = SessionChainId.New();

        var context = TestAccessContext.ForUser(
            user,
            UAuthActions.Credentials.ChangeSelf,
            actorChainId: chainId);

        var credential = CreateCredential(
            user,
            f.OldPasswordHash,
            version: 12);

        f.CredentialStore
            .Setup(x => x.GetByUserAsync(
                user,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { credential });

        f.Hasher
            .Setup(x => x.Verify(
                f.OldPasswordHash,
                "current-password"))
            .Returns(true);

        f.Hasher
            .Setup(x => x.Verify(
                f.OldPasswordHash,
                "new-password"))
            .Returns(false);

        f.Hasher
            .Setup(x => x.Hash("new-password"))
            .Returns(f.NewPasswordHash);

        f.CredentialStore
            .Setup(x => x.SaveAsync(
                It.Is<PasswordCredential>(c =>
                    c.UserKey == user &&
                    c.SecretHash == f.NewPasswordHash),
                12,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        f.SessionStore
            .Setup(x => x.RevokeOtherChainsAsync(
                user,
                chainId,
                Now,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await f.Sut.ChangeSecretAsync(
            context,
            new ChangeCredentialRequest
            {
                CurrentSecret = "current-password",
                NewSecret = "new-password"
            });

        result.IsSuccess.Should().BeTrue();

        f.SessionStore.Verify(x => x.RevokeOtherChainsAsync(
            user,
            chainId,
            Now,
            It.IsAny<CancellationToken>()),
            Times.Once);

        f.SessionStore.Verify(x => x.RevokeAllChainsAsync(
            It.IsAny<UserKey>(),
            It.IsAny<DateTimeOffset>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ChangeSecretAsync_SelfWithoutActorChain_RevokesAllChains()
    {
        var f = CreateFixture();
        var user = UserKey.New();

        var context = TestAccessContext.ForUser(
            user,
            UAuthActions.Credentials.ChangeSelf,
            actorChainId: null);

        SetupSuccessfulChange(f, user);

        f.SessionStore
            .Setup(x => x.RevokeAllChainsAsync(
                user,
                Now,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await f.Sut.ChangeSecretAsync(
            context,
            new ChangeCredentialRequest
            {
                CurrentSecret = "current-password",
                NewSecret = "new-password"
            });

        result.IsSuccess.Should().BeTrue();

        f.SessionStore.Verify(x => x.RevokeAllChainsAsync(
            user,
            Now,
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ChangeSecretAsync_Admin_WhenValid_DoesNotRequireCurrentSecret_AndRevokesAllChains()
    {
        var f = CreateFixture();

        var actor = UserKey.New();
        var target = UserKey.New();

        var context = TestAccessContext.ForTargetUser(
            actor,
            target,
            UAuthActions.Credentials.ChangeAdmin);

        var credential = CreateCredential(
            target,
            f.OldPasswordHash,
            version: 4);

        f.CredentialStore
            .Setup(x => x.GetByUserAsync(
                target,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { credential });

        f.Hasher
            .Setup(x => x.Verify(
                f.OldPasswordHash,
                "new-password"))
            .Returns(false);

        f.Hasher
            .Setup(x => x.Hash("new-password"))
            .Returns(f.NewPasswordHash);

        f.CredentialStore
            .Setup(x => x.SaveAsync(
                It.Is<PasswordCredential>(c =>
                    c.SecretHash == f.NewPasswordHash),
                4,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        f.SessionStore
            .Setup(x => x.RevokeAllChainsAsync(
                target,
                Now,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await f.Sut.ChangeSecretAsync(
            context,
            new ChangeCredentialRequest
            {
                CurrentSecret = null,
                NewSecret = "new-password"
            });

        result.IsSuccess.Should().BeTrue();

        f.Hasher.Verify(x => x.Verify(
            f.OldPasswordHash,
            It.IsAny<string>()),
            Times.Once);

        f.SessionStore.Verify(x => x.RevokeAllChainsAsync(
            target,
            Now,
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ---------------------------------------------------------
    // Revoke
    // ---------------------------------------------------------

    [Fact]
    public async Task RevokeAsync_WhenCredentialBelongsToDifferentUser_ReturnsNotFound()
    {
        var f = CreateFixture();

        var target = UserKey.New();
        var other = UserKey.New();
        var id = Guid.NewGuid();

        var context = TestAccessContext.ForUser(
            target,
            UAuthActions.Credentials.RevokeSelf);

        var credential = CreateCredential(
            other,
            f.OldPasswordHash,
            id: id);

        f.CredentialStore
            .Setup(x => x.GetAsync(
                new CredentialKey(context.ResourceTenant, id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential);

        var result = await f.Sut.RevokeAsync(
            context,
            new RevokeCredentialRequest
            {
                Id = id
            });

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("credential_not_found");

        f.CredentialStore.Verify(x => x.SaveAsync(
            It.IsAny<PasswordCredential>(),
            It.IsAny<long>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RevokeAsync_WhenValid_RevokesAndSavesExpectedVersion()
    {
        var f = CreateFixture();
        var user = UserKey.New();
        var id = Guid.NewGuid();

        var context = TestAccessContext.ForUser(
            user,
            UAuthActions.Credentials.RevokeSelf);

        var credential = CreateCredential(
            user,
            f.OldPasswordHash,
            id: id,
            version: 9);

        f.CredentialStore
            .Setup(x => x.GetAsync(
                new CredentialKey(context.ResourceTenant, id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential);

        f.CredentialStore
            .Setup(x => x.SaveAsync(
                It.Is<PasswordCredential>(c =>
                    c.Id == id &&
                    c.IsRevoked &&
                    c.Security.RevokedAt == Now),
                9,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await f.Sut.RevokeAsync(
            context,
            new RevokeCredentialRequest
            {
                Id = id
            });

        result.Succeeded.Should().BeTrue();
    }

    // ---------------------------------------------------------
    // Delete
    // ---------------------------------------------------------

    [Theory]
    [InlineData(DeleteMode.Soft)]
    [InlineData(DeleteMode.Hard)]
    public async Task DeleteAsync_WhenValid_UsesRequestedDeleteMode(DeleteMode mode)
    {
        var f = CreateFixture();

        var actor = UserKey.New();
        var target = UserKey.New();
        var id = Guid.NewGuid();

        var context = TestAccessContext.ForTargetUser(
            actorUserKey: actor,
            targetUserKey: target,
            action: UAuthActions.Credentials.DeleteAdmin);

        var credential = CreateCredential(
            target,
            f.OldPasswordHash,
            id: id,
            version: 5);

        var key = new CredentialKey(
            context.ResourceTenant,
            id);

        f.CredentialStore
            .Setup(x => x.GetAsync(
                key,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential);

        f.CredentialStore
            .Setup(x => x.DeleteAsync(
                key,
                5,
                mode,
                Now,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await f.Sut.DeleteAsync(
            context,
            new DeleteCredentialRequest
            {
                Id = id,
                Mode = mode
            });

        result.Succeeded.Should().BeTrue(
            $"delete should succeed but returned '{result.Error}'");

        result.Error.Should().BeNull();

        f.CredentialStore.Verify(x => x.DeleteAsync(
            key,
            5,
            mode,
            Now,
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenCredentialBelongsToDifferentUser_ReturnsNotFound()
    {
        var f = CreateFixture();

        var target = UserKey.New();
        var other = UserKey.New();
        var id = Guid.NewGuid();

        var actor = UserKey.New();

        var context = TestAccessContext.ForTargetUser(
            actor,
            target,
            UAuthActions.Credentials.DeleteAdmin);

        f.CredentialStore
            .Setup(x => x.GetAsync(
                new CredentialKey(context.ResourceTenant, id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateCredential(
                other,
                f.OldPasswordHash,
                id: id));

        var result = await f.Sut.DeleteAsync(
            context,
            new DeleteCredentialRequest
            {
                Id = id,
                Mode = DeleteMode.Hard
            });

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("credential_not_found");

        f.CredentialStore.Verify(x => x.DeleteAsync(
            It.IsAny<CredentialKey>(),
            It.IsAny<long>(),
            It.IsAny<DeleteMode>(),
            It.IsAny<DateTimeOffset>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ---------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------

    private static PasswordCredential CreateCredential(
        UserKey user,
        PasswordHash hash,
        Guid? id = null,
        long version = 0)
    {
        return PasswordCredential.FromProjection(
            id: id ?? Guid.NewGuid(),
            tenant: TenantKey.Single,
            userKey: user,
            secretHash: hash,
            security: CredentialSecurityState.Active(),
            metadata: new CredentialMetadata(),
            createdAt: Now.AddDays(-10),
            updatedAt: null,
            deletedAt: null,
            version: version);
    }

    private static void SetupSuccessfulChange(
        Fixture f,
        UserKey user)
    {
        var credential = CreateCredential(
            user,
            f.OldPasswordHash,
            version: 3);

        f.CredentialStore
            .Setup(x => x.GetByUserAsync(
                user,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { credential });

        f.Hasher
            .Setup(x => x.Verify(
                f.OldPasswordHash,
                "current-password"))
            .Returns(true);

        f.Hasher
            .Setup(x => x.Verify(
                f.OldPasswordHash,
                "new-password"))
            .Returns(false);

        f.Hasher
            .Setup(x => x.Hash("new-password"))
            .Returns(f.NewPasswordHash);

        f.CredentialStore
            .Setup(x => x.SaveAsync(
                It.Is<PasswordCredential>(c =>
                    c.SecretHash == f.NewPasswordHash),
                3,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private static Fixture CreateFixture()
        => new();

    private sealed class Fixture
    {
        public Mock<IAccessOrchestrator> AccessOrchestrator { get; }
            = new(MockBehavior.Strict);

        public Mock<IPasswordCredentialStoreFactory> CredentialStoreFactory { get; }
            = new(MockBehavior.Strict);

        public Mock<IPasswordCredentialStore> CredentialStore { get; }
            = new(MockBehavior.Strict);

        public Mock<IAuthenticationSecurityManager> SecurityManager { get; }
            = new(MockBehavior.Strict);

        public Mock<IOpaqueTokenGenerator> TokenGenerator { get; }
            = new(MockBehavior.Strict);

        public Mock<INumericCodeGenerator> NumericCodeGenerator { get; }
            = new(MockBehavior.Strict);

        public Mock<IUAuthPasswordHasher> Hasher { get; }
            = new(MockBehavior.Strict);

        public Mock<ITokenHasher> TokenHasher { get; }
            = new(MockBehavior.Strict);

        public Mock<ILoginIdentifierResolver> IdentifierResolver { get; }
            = new(MockBehavior.Strict);

        public Mock<ISessionStoreFactory> SessionStoreFactory { get; }
            = new(MockBehavior.Strict);

        public Mock<ISessionStore> SessionStore { get; }
            = new(MockBehavior.Strict);

        public Mock<IClock> Clock { get; }
            = new(MockBehavior.Strict);

        public PasswordHash OldPasswordHash { get; }
            = PasswordHash.Create("test", "old-hash");

        public PasswordHash NewPasswordHash { get; }
            = PasswordHash.Create("test", "new-hash");

        public CredentialManagementService Sut { get; }

        public Fixture()
        {
            Clock
                .SetupGet(x => x.UtcNow)
                .Returns(Now);

            AccessOrchestrator
                .Setup(x => x.ExecuteAsync(
                    It.IsAny<AccessContext>(),
                    It.IsAny<AccessCommand<GetCredentialsResult>>(),
                    It.IsAny<CancellationToken>()))
                .Returns<AccessContext,
                    AccessCommand<GetCredentialsResult>,
                    CancellationToken>(
                    (_, command, ct) => command.ExecuteAsync(ct));

            AccessOrchestrator
                .Setup(x => x.ExecuteAsync(
                    It.IsAny<AccessContext>(),
                    It.IsAny<AccessCommand<AddCredentialResult>>(),
                    It.IsAny<CancellationToken>()))
                .Returns<AccessContext,
                    AccessCommand<AddCredentialResult>,
                    CancellationToken>(
                    (_, command, ct) => command.ExecuteAsync(ct));

            AccessOrchestrator
                .Setup(x => x.ExecuteAsync(
                    It.IsAny<AccessContext>(),
                    It.IsAny<AccessCommand<ChangeCredentialResult>>(),
                    It.IsAny<CancellationToken>()))
                .Returns<AccessContext,
                    AccessCommand<ChangeCredentialResult>,
                    CancellationToken>(
                    (_, command, ct) => command.ExecuteAsync(ct));

            AccessOrchestrator
                .Setup(x => x.ExecuteAsync(
                    It.IsAny<AccessContext>(),
                    It.IsAny<AccessCommand<CredentialActionResult>>(),
                    It.IsAny<CancellationToken>()))
                .Returns<AccessContext,
                    AccessCommand<CredentialActionResult>,
                    CancellationToken>(
                    (_, command, ct) => command.ExecuteAsync(ct));

            CredentialStoreFactory
                .Setup(x => x.Create(It.IsAny<TenantKey>()))
                .Returns(CredentialStore.Object);

            SessionStoreFactory
                .Setup(x => x.Create(It.IsAny<TenantKey>()))
                .Returns(SessionStore.Object);

            var options = Options.Create(
                TestServerOptions.Default());

            Sut = new CredentialManagementService(
                AccessOrchestrator.Object,
                CredentialStoreFactory.Object,
                SecurityManager.Object,
                TokenGenerator.Object,
                NumericCodeGenerator.Object,
                Hasher.Object,
                TokenHasher.Object,
                IdentifierResolver.Object,
                SessionStoreFactory.Object,
                options,
                Clock.Object);
        }
    }
}