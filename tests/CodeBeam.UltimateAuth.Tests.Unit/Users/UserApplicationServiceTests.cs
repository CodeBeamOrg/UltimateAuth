using CodeBeam.UltimateAuth.Authorization;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Users;
using CodeBeam.UltimateAuth.Users.Contracts;
using CodeBeam.UltimateAuth.Users.Reference;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Unit.Users;

public sealed class UserApplicationServiceTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 9, 21, 12, 0, 0, TimeSpan.Zero);

    // ============================================================
    // Create
    // ============================================================

    [Fact]
    public async Task CreateUserAsync_WhenValidationFails_ThrowsAndDoesNotPersist()
    {
        var f = CreateFixture();
        var context = CreateContext();
        var request = CreateUserRequest();

        f.UserCreateValidator
            .Setup(x => x.ValidateAsync(
                context,
                request,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                UserCreateValidatorResult.Failed(
                    new[]
                    {
                        new UAuthValidationError("identifier_required")
                    }));

        var act = () => f.Sut.CreateUserAsync(context, request);

        await act.Should()
            .ThrowAsync<UAuthValidationException>();

        f.LifecycleStore.Verify(
            x => x.AddAsync(
                It.IsAny<UserLifecycle>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        f.ProfileStore.Verify(
            x => x.AddAsync(
                It.IsAny<UserProfile>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        f.IdentifierStore.Verify(
            x => x.AddAsync(
                It.IsAny<UserIdentifier>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateUserAsync_WhenValid_CreatesLifecycleAndDefaultProfile()
    {
        var f = CreateFixture();
        var context = CreateContext();

        var request = CreateUserRequest();

        SetupValidCreate(f, context, request);

        UserLifecycle? capturedLifecycle = null;
        UserProfile? capturedProfile = null;

        f.LifecycleStore
            .Setup(x => x.AddAsync(
                It.IsAny<UserLifecycle>(),
                It.IsAny<CancellationToken>()))
            .Callback<UserLifecycle, CancellationToken>(
                (x, _) => capturedLifecycle = x)
            .Returns(Task.CompletedTask);

        f.ProfileStore
            .Setup(x => x.AddAsync(
                It.IsAny<UserProfile>(),
                It.IsAny<CancellationToken>()))
            .Callback<UserProfile, CancellationToken>(
                (x, _) => capturedProfile = x)
            .Returns(Task.CompletedTask);

        SetupIdentifierPersistence(f);

        var result = await f.Sut.CreateUserAsync(
            context,
            request);

        result.Succeeded.Should().BeTrue();

        capturedLifecycle.Should().NotBeNull();
        capturedLifecycle!.Tenant.Should().Be(context.ResourceTenant);
        capturedLifecycle.CreatedAt.Should().Be(Now);

        capturedProfile.Should().NotBeNull();
        capturedProfile!.Tenant.Should().Be(context.ResourceTenant);
        capturedProfile.ProfileKey.Should().Be(ProfileKey.Default);
        capturedProfile.UserKey.Should().Be(capturedLifecycle.UserKey);
    }

    [Fact]
    public async Task CreateUserAsync_CreatesConfiguredIdentifiersForSameUser()
    {
        var f = CreateFixture();
        var context = CreateContext();

        var request = new CreateUserRequest
        {
            UserName = "alice",
            Email = "alice@example.com",
            Phone = "+905551112233"
        };

        SetupValidCreate(f, context, request);

        f.LifecycleStore
            .Setup(x => x.AddAsync(
                It.IsAny<UserLifecycle>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        f.ProfileStore
            .Setup(x => x.AddAsync(
                It.IsAny<UserProfile>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var identifiers = new List<UserIdentifier>();

        f.IdentifierStore
            .Setup(x => x.AddAsync(
                It.IsAny<UserIdentifier>(),
                It.IsAny<CancellationToken>()))
            .Callback<UserIdentifier, CancellationToken>(
                (x, _) => identifiers.Add(x))
            .Returns(Task.CompletedTask);

        var result = await f.Sut.CreateUserAsync(
            context,
            request);

        result.Succeeded.Should().BeTrue();

        identifiers.Should().HaveCount(3);

        identifiers
            .Select(x => x.Type)
            .Should()
            .BeEquivalentTo(new[]
            {
                UserIdentifierType.Username,
                UserIdentifierType.Email,
                UserIdentifierType.Phone
            });

        identifiers
            .Select(x => x.UserKey)
            .Distinct()
            .Should()
            .ContainSingle();
    }

    [Fact]
    public async Task CreateUserAsync_InvokesLifecycleIntegrations()
    {
        var integration =
            new Mock<IUserLifecycleIntegration>(MockBehavior.Strict);

        var f = CreateFixture(integration.Object);
        var context = CreateContext();
        var request = CreateUserRequest();

        SetupValidCreate(f, context, request);
        SetupSuccessfulCreatePersistence(f);

        integration
            .Setup(x => x.OnUserCreatedAsync(
                context.ResourceTenant,
                It.IsAny<UserKey>(),
                request,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await f.Sut.CreateUserAsync(context, request);

        integration.Verify(x => x.OnUserCreatedAsync(
            context.ResourceTenant,
            It.IsAny<UserKey>(),
            request,
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ============================================================
    // Change status
    // ============================================================

    [Fact]
    public async Task ChangeUserStatusAsync_WhenUserDoesNotExist_ThrowsNotFound()
    {
        var f = CreateFixture();
        var user = UserKey.New();
        var context = CreateContext(targetUserKey: user);

        f.LifecycleStore
            .Setup(x => x.GetAsync(
                It.IsAny<UserLifecycleKey>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserLifecycle?)null);

        var request = new ChangeUserStatusAdminRequest
        {
            NewStatus = AdminAssignableUserStatus.Suspended
        };

        var act = () => f.Sut.ChangeUserStatusAsync(
            context,
            request);

        await act.Should()
            .ThrowAsync<UAuthNotFoundException>();

        f.LifecycleStore.Verify(
            x => x.SaveAsync(
                It.IsAny<UserLifecycle>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ChangeUserStatusAsync_Self_AllowsActiveToSelfSuspended()
    {
        var f = CreateFixture();

        var user = UserKey.New();

        var context = CreateContext(
            actorUserKey: user,
            targetUserKey: user,
            action: UAuthActions.Users.ChangeStatusSelf);

        var lifecycle = UserLifecycle.Create(
            context.ResourceTenant,
            user,
            Now.AddDays(-1));

        f.LifecycleStore
            .Setup(x => x.GetAsync(
                It.IsAny<UserLifecycleKey>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(lifecycle);

        f.LifecycleStore
            .Setup(x => x.SaveAsync(
                It.IsAny<UserLifecycle>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var request = new ChangeUserStatusSelfRequest
        {
            NewStatus = SelfAssignableUserStatus.SelfSuspended
        };

        await f.Sut.ChangeUserStatusAsync(
            context,
            request);

        lifecycle.Status.Should().Be(UserStatus.SelfSuspended);

        f.LifecycleStore.Verify(x => x.SaveAsync(
            lifecycle,
            It.IsAny<long>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ChangeUserStatusAsync_Self_WhenTransitionNotAllowed_ThrowsConflict()
    {
        var f = CreateFixture();

        var user = UserKey.New();

        var context = CreateContext(
            actorUserKey: user,
            targetUserKey: user,
            action: UAuthActions.Users.ChangeStatusSelf);

        var lifecycle = UserLifecycle.Create(
            context.ResourceTenant,
            user,
            Now.AddDays(-1));

        f.LifecycleStore
            .Setup(x => x.GetAsync(
                It.IsAny<UserLifecycleKey>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(lifecycle);

        // Active -> Active is explicitly not a valid self transition.
        var request = new ChangeUserStatusSelfRequest
        {
            NewStatus = SelfAssignableUserStatus.Active
        };

        var act = () => f.Sut.ChangeUserStatusAsync(
            context,
            request);

        await act.Should()
            .ThrowAsync<UAuthConflictException>();

        f.LifecycleStore.Verify(
            x => x.SaveAsync(
                It.IsAny<UserLifecycle>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ============================================================
    // Delete self
    // ============================================================

    [Fact]
    public async Task DeleteMeAsync_WhenUserDoesNotExist_ThrowsNotFound()
    {
        var f = CreateFixture();

        var user = UserKey.New();

        var context = CreateContext(
            actorUserKey: user,
            targetUserKey: user,
            action: UAuthActions.Users.DeleteSelf);

        f.LifecycleStore
            .Setup(x => x.GetAsync(
                It.IsAny<UserLifecycleKey>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserLifecycle?)null);

        var act = () => f.Sut.DeleteMeAsync(context);

        await act.Should()
            .ThrowAsync<UAuthNotFoundException>();

        f.SessionStore.Verify(
            x => x.RevokeAllChainsAsync(
                It.IsAny<UserKey>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task DeleteMeAsync_SoftDeletesUserData_AndRevokesAllChains()
    {
        var f = CreateFixture();

        var user = UserKey.New();

        var context = CreateContext(
            actorUserKey: user,
            targetUserKey: user,
            action: UAuthActions.Users.DeleteSelf);

        var lifecycle = UserLifecycle.Create(
            context.ResourceTenant,
            user,
            Now.AddDays(-10));

        f.LifecycleStore
            .Setup(x => x.GetAsync(
                It.IsAny<UserLifecycleKey>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(lifecycle);

        f.LifecycleStore
            .Setup(x => x.DeleteAsync(
                It.IsAny<UserLifecycleKey>(),
                lifecycle.Version,
                DeleteMode.Soft,
                Now,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        f.IdentifierStore
            .Setup(x => x.DeleteByUserAsync(
                user,
                DeleteMode.Soft,
                Now,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        f.ProfileStore
            .Setup(x => x.GetAllProfilesByUserAsync(
                user,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<UserProfile>());

        f.SessionStore
            .Setup(x => x.RevokeAllChainsAsync(
                user,
                Now,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await f.Sut.DeleteMeAsync(context);

        f.LifecycleStore.Verify(x => x.DeleteAsync(
            It.IsAny<UserLifecycleKey>(),
            lifecycle.Version,
            DeleteMode.Soft,
            Now,
            It.IsAny<CancellationToken>()),
            Times.Once);

        f.IdentifierStore.Verify(x => x.DeleteByUserAsync(
            user,
            DeleteMode.Soft,
            Now,
            It.IsAny<CancellationToken>()),
            Times.Once);

        f.SessionStore.Verify(x => x.RevokeAllChainsAsync(
            user,
            Now,
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteMeAsync_InvokesIntegrationWithSoftDelete()
    {
        var integration =
            new Mock<IUserLifecycleIntegration>(MockBehavior.Strict);

        var f = CreateFixture(integration.Object);

        var user = UserKey.New();

        var context = CreateContext(
            actorUserKey: user,
            targetUserKey: user,
            action: UAuthActions.Users.DeleteSelf);

        var lifecycle = UserLifecycle.Create(
            context.ResourceTenant,
            user,
            Now.AddDays(-1));

        SetupDeleteSelfPersistence(
            f,
            user,
            lifecycle);

        integration
            .Setup(x => x.OnUserDeletedAsync(
                context.ResourceTenant,
                user,
                DeleteMode.Soft,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await f.Sut.DeleteMeAsync(context);

        integration.Verify(x => x.OnUserDeletedAsync(
            context.ResourceTenant,
            user,
            DeleteMode.Soft,
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ============================================================
    // Delete admin
    // ============================================================

    [Theory]
    [InlineData(DeleteMode.Soft)]
    [InlineData(DeleteMode.Hard)]
    public async Task DeleteUserAsync_UsesRequestedDeleteMode(
        DeleteMode mode)
    {
        var f = CreateFixture();

        var actor = UserKey.New();
        var target = UserKey.New();

        var context = CreateContext(
            actorUserKey: actor,
            targetUserKey: target,
            action: UAuthActions.Users.DeleteAdmin);

        var lifecycle = UserLifecycle.Create(
            context.ResourceTenant,
            target,
            Now.AddDays(-1));

        f.LifecycleStore
            .Setup(x => x.GetAsync(
                It.IsAny<UserLifecycleKey>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(lifecycle);

        f.LifecycleStore
            .Setup(x => x.DeleteAsync(
                It.IsAny<UserLifecycleKey>(),
                lifecycle.Version,
                mode,
                Now,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        f.IdentifierStore
            .Setup(x => x.DeleteByUserAsync(
                target,
                mode,
                Now,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        f.ProfileStore
            .Setup(x => x.GetAllProfilesByUserAsync(
                target,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<UserProfile>());

        f.SessionStore
            .Setup(x => x.RevokeAllChainsAsync(
                target,
                Now,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var request = new DeleteUserRequest
        {
            Mode = mode
        };

        await f.Sut.DeleteUserAsync(
            context,
            request);

        f.LifecycleStore.Verify(x => x.DeleteAsync(
            It.IsAny<UserLifecycleKey>(),
            lifecycle.Version,
            mode,
            Now,
            It.IsAny<CancellationToken>()),
            Times.Once);

        f.IdentifierStore.Verify(x => x.DeleteByUserAsync(
            target,
            mode,
            Now,
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ============================================================
    // Identifiers - security / ownership
    // ============================================================

    [Fact]
    public async Task UpdateUserIdentifierAsync_WhenIdentifierDoesNotExist_ThrowsNotFound()
    {
        var f = CreateFixture();
        var context = CreateContext();
        var id = Guid.NewGuid();

        f.IdentifierStore
            .Setup(x => x.GetByIdAsync(
                id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserIdentifier?)null);

        var request = new UpdateUserIdentifierRequest
        {
            Id = id,
            NewValue = "new@example.com"
        };

        var act = () => f.Sut.UpdateUserIdentifierAsync(
            context,
            request);

        await act.Should()
            .ThrowAsync<UAuthIdentifierNotFoundException>();

        f.IdentifierStore.Verify(
            x => x.SaveAsync(
                It.IsAny<UserIdentifier>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SetPrimaryUserIdentifierAsync_WhenIdentifierDoesNotExist_ThrowsNotFound()
    {
        var f = CreateFixture();
        var context = CreateContext();
        var id = Guid.NewGuid();

        f.IdentifierStore
            .Setup(x => x.GetByIdAsync(
                id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserIdentifier?)null);

        var request = new SetPrimaryUserIdentifierRequest
        {
            Id = id
        };

        var act = () => f.Sut.SetPrimaryUserIdentifierAsync(
            context,
            request);

        await act.Should()
            .ThrowAsync<UAuthIdentifierNotFoundException>();

        f.IdentifierStore.Verify(
            x => x.SaveAsync(
                It.IsAny<UserIdentifier>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UnsetPrimaryUserIdentifierAsync_WhenIdentifierDoesNotExist_ThrowsNotFound()
    {
        var f = CreateFixture();
        var context = CreateContext();
        var id = Guid.NewGuid();

        f.IdentifierStore
            .Setup(x => x.GetByIdAsync(
                id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserIdentifier?)null);

        var request = new UnsetPrimaryUserIdentifierRequest
        {
            Id = id
        };

        var act = () => f.Sut.UnsetPrimaryUserIdentifierAsync(
            context,
            request);

        await act.Should()
            .ThrowAsync<UAuthIdentifierNotFoundException>();
    }

    [Fact]
    public async Task VerifyUserIdentifierAsync_WhenIdentifierDoesNotExist_ThrowsNotFound()
    {
        var f = CreateFixture();
        var context = CreateContext();
        var id = Guid.NewGuid();

        f.IdentifierStore
            .Setup(x => x.GetByIdAsync(
                id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserIdentifier?)null);

        var request = new VerifyUserIdentifierRequest
        {
            Id = id
        };

        var act = () => f.Sut.VerifyUserIdentifierAsync(
            context,
            request);

        await act.Should()
            .ThrowAsync<UAuthIdentifierNotFoundException>();

        f.IdentifierStore.Verify(
            x => x.SaveAsync(
                It.IsAny<UserIdentifier>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task DeleteUserIdentifierAsync_WhenIdentifierDoesNotExist_ThrowsNotFound()
    {
        var f = CreateFixture();
        var context = CreateContext();
        var id = Guid.NewGuid();

        f.IdentifierStore
            .Setup(x => x.GetByIdAsync(
                id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserIdentifier?)null);

        var request = new DeleteUserIdentifierRequest
        {
            Id = id,
            Mode = DeleteMode.Soft
        };

        var act = () => f.Sut.DeleteUserIdentifierAsync(
            context,
            request);

        await act.Should()
            .ThrowAsync<UAuthIdentifierNotFoundException>();
    }

    // ============================================================
    // Identifiers - UnsetPrimary invariants
    // ============================================================

    [Fact]
    public async Task UnsetPrimaryUserIdentifierAsync_WhenAlreadyNotPrimary_ThrowsValidation()
    {
        var f = CreateFixture();
        var context = CreateContext();

        var identifier = CreateIdentifier(
            context.GetTargetUserKey(),
            isPrimary: false,
            isVerified: true);

        f.IdentifierStore
            .Setup(x => x.GetByIdAsync(
                identifier.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(identifier);

        var request = new UnsetPrimaryUserIdentifierRequest
        {
            Id = identifier.Id
        };

        var act = () => f.Sut.UnsetPrimaryUserIdentifierAsync(
            context,
            request);

        await act.Should()
            .ThrowAsync<UAuthIdentifierValidationException>()
            .WithMessage("*identifier_already_not_primary*");

        f.IdentifierStore.Verify(
            x => x.SaveAsync(
                It.IsAny<UserIdentifier>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UnsetPrimaryUserIdentifierAsync_WhenItIsLastLoginPrimary_ThrowsConflict()
    {
        var f = CreateFixture();
        var context = CreateContext();

        var identifier = CreateIdentifier(
            context.GetTargetUserKey(),
            type: UserIdentifierType.Username,
            isPrimary: true,
            isVerified: true);

        f.IdentifierStore
            .Setup(x => x.GetByIdAsync(
                identifier.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(identifier);

        f.IdentifierStore
            .Setup(x => x.GetByUserAsync(
                identifier.UserKey,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { identifier });

        var request = new UnsetPrimaryUserIdentifierRequest
        {
            Id = identifier.Id
        };

        var act = () => f.Sut.UnsetPrimaryUserIdentifierAsync(
            context,
            request);

        await act.Should()
            .ThrowAsync<UAuthIdentifierConflictException>()
            .WithMessage("*cannot_unset_last_login_identifier*");

        f.IdentifierStore.Verify(
            x => x.SaveAsync(
                It.IsAny<UserIdentifier>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }


    // ============================================================
    // Identifiers - Verify
    // ============================================================

    [Fact]
    public async Task VerifyUserIdentifierAsync_WhenIdentifierExists_MarksVerifiedAndSaves()
    {
        var f = CreateFixture();
        var context = CreateContext();

        var identifier = CreateIdentifier(
            context.GetTargetUserKey(),
            isVerified: false,
            version: 7);

        var expectedVersion = identifier.Version;

        f.IdentifierStore
            .Setup(x => x.GetByIdAsync(
                identifier.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(identifier);

        f.IdentifierStore
            .Setup(x => x.SaveAsync(
                identifier,
                expectedVersion,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await f.Sut.VerifyUserIdentifierAsync(
            context,
            new VerifyUserIdentifierRequest
            {
                Id = identifier.Id
            });

        identifier.IsVerified.Should().BeTrue();
        identifier.VerifiedAt.Should().Be(Now);
        identifier.UpdatedAt.Should().Be(Now);

        f.IdentifierStore.Verify(x => x.SaveAsync(
            identifier,
            expectedVersion,
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ============================================================
    // Identifiers - Delete invariants
    // ============================================================

    [Fact]
    public async Task DeleteUserIdentifierAsync_WhenIdentifierIsPrimary_ThrowsValidation()
    {
        var f = CreateFixture();
        var context = CreateContext();

        var identifier = CreateIdentifier(
            context.GetTargetUserKey(),
            isPrimary: true,
            isVerified: true);

        f.IdentifierStore
            .Setup(x => x.GetByIdAsync(
                identifier.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(identifier);

        f.IdentifierStore
            .Setup(x => x.GetByUserAsync(
                identifier.UserKey,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { identifier });

        var act = () => f.Sut.DeleteUserIdentifierAsync(
            context,
            new DeleteUserIdentifierRequest
            {
                Id = identifier.Id,
                Mode = DeleteMode.Soft
            });

        await act.Should()
            .ThrowAsync<UAuthIdentifierValidationException>()
            .WithMessage("*cannot_delete_primary_identifier*");

        f.IdentifierStore.Verify(
            x => x.SaveAsync(
                It.IsAny<UserIdentifier>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        f.IdentifierStore.Verify(
            x => x.DeleteAsync(
                It.IsAny<Guid>(),
                It.IsAny<long>(),
                It.IsAny<DeleteMode>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task DeleteUserIdentifierAsync_WhenItIsLastLoginIdentifier_ThrowsConflict()
    {
        var f = CreateFixture();
        var context = CreateContext();

        var identifier = CreateIdentifier(
            context.GetTargetUserKey(),
            type: UserIdentifierType.Email,
            isPrimary: false,
            isVerified: true);

        f.IdentifierStore
            .Setup(x => x.GetByIdAsync(
                identifier.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(identifier);

        f.IdentifierStore
            .Setup(x => x.GetByUserAsync(
                identifier.UserKey,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { identifier });

        var act = () => f.Sut.DeleteUserIdentifierAsync(
            context,
            new DeleteUserIdentifierRequest
            {
                Id = identifier.Id,
                Mode = DeleteMode.Soft
            });

        await act.Should()
            .ThrowAsync<UAuthIdentifierConflictException>()
            .WithMessage("*cannot_delete_last_login_identifier*");

        f.IdentifierStore.Verify(
            x => x.SaveAsync(
                It.IsAny<UserIdentifier>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task DeleteUserIdentifierAsync_SoftDelete_MarksDeletedAndSaves()
    {
        var f = CreateFixture();
        var context = CreateContext();

        var target = CreateIdentifier(
            context.GetTargetUserKey(),
            type: UserIdentifierType.Email,
            isPrimary: false,
            isVerified: true);

        // Keeps the user with another active login identifier.
        var remaining = CreateIdentifier(
            context.GetTargetUserKey(),
            type: UserIdentifierType.Username,
            isPrimary: true,
            isVerified: true);

        var expectedVersion = target.Version;

        f.IdentifierStore
            .Setup(x => x.GetByIdAsync(
                target.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(target);

        f.IdentifierStore
            .Setup(x => x.GetByUserAsync(
                target.UserKey,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { target, remaining });

        f.IdentifierStore
            .Setup(x => x.SaveAsync(
                target,
                expectedVersion,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await f.Sut.DeleteUserIdentifierAsync(
            context,
            new DeleteUserIdentifierRequest
            {
                Id = target.Id,
                Mode = DeleteMode.Soft
            });

        target.IsDeleted.Should().BeTrue();
        target.DeletedAt.Should().Be(Now);

        f.IdentifierStore.Verify(x => x.SaveAsync(
            target,
            expectedVersion,
            It.IsAny<CancellationToken>()),
            Times.Once);

        f.IdentifierStore.Verify(
            x => x.DeleteAsync(
                It.IsAny<Guid>(),
                It.IsAny<long>(),
                It.IsAny<DeleteMode>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task DeleteUserIdentifierAsync_HardDelete_UsesStoreDelete()
    {
        var f = CreateFixture();
        var context = CreateContext();

        var target = CreateIdentifier(
            context.GetTargetUserKey(),
            type: UserIdentifierType.Email,
            isPrimary: false,
            isVerified: true);

        var remaining = CreateIdentifier(
            context.GetTargetUserKey(),
            type: UserIdentifierType.Username,
            isPrimary: true,
            isVerified: true);

        var expectedVersion = target.Version;

        f.IdentifierStore
            .Setup(x => x.GetByIdAsync(
                target.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(target);

        f.IdentifierStore
            .Setup(x => x.GetByUserAsync(
                target.UserKey,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { target, remaining });

        f.IdentifierStore
            .Setup(x => x.DeleteAsync(
                target.Id,
                expectedVersion,
                DeleteMode.Hard,
                Now,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await f.Sut.DeleteUserIdentifierAsync(
            context,
            new DeleteUserIdentifierRequest
            {
                Id = target.Id,
                Mode = DeleteMode.Hard
            });

        f.IdentifierStore.Verify(x => x.DeleteAsync(
            target.Id,
            expectedVersion,
            DeleteMode.Hard,
            Now,
            It.IsAny<CancellationToken>()),
            Times.Once);

        f.IdentifierStore.Verify(
            x => x.SaveAsync(
                It.IsAny<UserIdentifier>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateUserIdentifierAsync_ValidatesNewValue_NotExistingValue()
    {
        var f = CreateFixture();
        var context = CreateContext();

        var identifier = CreateIdentifier(
            context.GetTargetUserKey(),
            type: UserIdentifierType.Email,
            value: "old@example.com",
            normalizedValue: "old@example.com",
            version: 4);

        f.IdentifierStore
            .Setup(x => x.GetByIdAsync(
                identifier.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(identifier);

        f.IdentifierValidator
            .Setup(x => x.ValidateAsync(
                context,
                It.Is<UserIdentifierInfo>(x =>
                    x.Value == "new@example.com"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentifierValidationResult.Success());

        f.IdentifierNormalizer
            .Setup(x => x.Normalize(
                UserIdentifierType.Email,
                "new@example.com"))
            .Returns(new NormalizedIdentifier(
                "new@example.com",
                "new@example.com",
                true,
                null));

        f.IdentifierStore
            .Setup(x => x.GetAsync(
                UserIdentifierType.Email,
                "new@example.com",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserIdentifier?)null);

        f.IdentifierStore
            .Setup(x => x.ExistsAsync(
                It.Is<IdentifierExistenceQuery>(q =>
                    q.Type == UserIdentifierType.Email &&
                    q.NormalizedValue == "new@example.com" &&
                    q.Scope == IdentifierExistenceScope.WithinUser &&
                    q.UserKey == identifier.UserKey &&
                    q.ExcludeIdentifierId == identifier.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdentifierExistenceResult(
                Exists: false));

        f.IdentifierStore
            .Setup(x => x.SaveAsync(
                identifier,
                4,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await f.Sut.UpdateUserIdentifierAsync(
            context,
            new UpdateUserIdentifierRequest
            {
                Id = identifier.Id,
                NewValue = "new@example.com"
            });

        identifier.Value.Should().Be("new@example.com");
        identifier.NormalizedValue.Should().Be("new@example.com");

        f.IdentifierValidator.Verify(x => x.ValidateAsync(
            context,
            It.Is<UserIdentifierInfo>(i =>
                i.Value == "new@example.com"),
            It.IsAny<CancellationToken>()),
            Times.Once);

        f.IdentifierStore.Verify(x => x.SaveAsync(
            identifier,
            4,
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SetPrimaryUserIdentifierAsync_WhenValid_SetsPrimaryAndSavesExpectedVersion()
    {
        var f = CreateFixture();
        var context = CreateContext();

        var identifier = CreateIdentifier(
            context.GetTargetUserKey(),
            isPrimary: false,
            isVerified: true,
            version: 5);

        f.IdentifierStore
            .Setup(x => x.GetByIdAsync(
                identifier.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(identifier);

        f.IdentifierStore
            .Setup(x => x.ExistsAsync(
                It.Is<IdentifierExistenceQuery>(q =>
                    q.Type == UserIdentifierType.Email &&
                    q.NormalizedValue == "alice@example.com" &&
                    q.Scope == IdentifierExistenceScope.TenantPrimaryOnly &&
                    q.UserKey == null &&
                    q.ExcludeIdentifierId == identifier.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdentifierExistenceResult(
                Exists: false));

        f.IdentifierStore
            .Setup(x => x.SaveAsync(
                identifier,
                5,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await f.Sut.SetPrimaryUserIdentifierAsync(
            context,
            new SetPrimaryUserIdentifierRequest
            {
                Id = identifier.Id
            });

        identifier.IsPrimary.Should().BeTrue();
        identifier.UpdatedAt.Should().Be(Now);

        f.IdentifierStore.Verify(x => x.SaveAsync(
            identifier,
            5,
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SetPrimaryUserIdentifierAsync_WhenAlreadyPrimary_ThrowsValidation()
    {
        var f = CreateFixture();
        var context = CreateContext();

        var identifier = CreateIdentifier(
            context.GetTargetUserKey(),
            isPrimary: true,
            isVerified: true);

        f.IdentifierStore
            .Setup(x => x.GetByIdAsync(
                identifier.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(identifier);

        var request = new SetPrimaryUserIdentifierRequest
        {
            Id = identifier.Id
        };

        var act = () => f.Sut.SetPrimaryUserIdentifierAsync(
            context,
            request);

        await act.Should()
            .ThrowAsync<UAuthIdentifierValidationException>()
            .WithMessage("*identifier_already_primary*");

        f.IdentifierStore.Verify(
            x => x.SaveAsync(
                It.IsAny<UserIdentifier>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UnsetPrimaryUserIdentifierAsync_WhenValid_UnsetsPrimaryAndSavesExpectedVersion()
    {
        var f = CreateFixture();
        var context = CreateContext();

        var target = CreateIdentifier(
            context.GetTargetUserKey(),
            type: UserIdentifierType.Email,
            isPrimary: true,
            isVerified: true,
            version: 6);

        var remaining = CreateIdentifier(
            context.GetTargetUserKey(),
            type: UserIdentifierType.Username,
            isPrimary: true,
            isVerified: true);

        f.IdentifierStore
            .Setup(x => x.GetByIdAsync(
                target.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(target);

        f.IdentifierStore
            .Setup(x => x.GetByUserAsync(
                target.UserKey,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { target, remaining });

        f.IdentifierStore
            .Setup(x => x.SaveAsync(
                target,
                6,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await f.Sut.UnsetPrimaryUserIdentifierAsync(
            context,
            new UnsetPrimaryUserIdentifierRequest
            {
                Id = target.Id
            });

        target.IsPrimary.Should().BeFalse();
        target.UpdatedAt.Should().Be(Now);

        f.IdentifierStore.Verify(x => x.SaveAsync(
            target,
            6,
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(DeleteMode.Soft)]
    [InlineData(DeleteMode.Hard)]
    public async Task DeleteUserAsync_RevokesAllChains(DeleteMode mode)
    {
        var f = CreateFixture();

        var actor = UserKey.New();
        var target = UserKey.New();

        var context = CreateContext(
            actorUserKey: actor,
            targetUserKey: target,
            action: UAuthActions.Users.DeleteAdmin);

        var lifecycle = UserLifecycle.Create(
            context.ResourceTenant,
            target,
            Now.AddDays(-1));

        f.LifecycleStore
            .Setup(x => x.GetAsync(
                It.IsAny<UserLifecycleKey>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(lifecycle);

        f.LifecycleStore
            .Setup(x => x.DeleteAsync(
                It.IsAny<UserLifecycleKey>(),
                lifecycle.Version,
                mode,
                Now,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        f.IdentifierStore
            .Setup(x => x.DeleteByUserAsync(
                target,
                mode,
                Now,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        f.ProfileStore
            .Setup(x => x.GetAllProfilesByUserAsync(
                target,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<UserProfile>());

        f.SessionStore
            .Setup(x => x.RevokeAllChainsAsync(
                target,
                Now,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await f.Sut.DeleteUserAsync(
            context,
            new DeleteUserRequest
            {
                Mode = mode
            });

        f.SessionStore.Verify(x => x.RevokeAllChainsAsync(
            target,
            Now,
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(DeleteMode.Soft)]
    [InlineData(DeleteMode.Hard)]
    public async Task DeleteUserAsync_DeletesProfilesUsingRequestedDeleteMode(DeleteMode mode)
    {
        var f = CreateFixture();

        var actor = UserKey.New();
        var target = UserKey.New();

        var context = CreateContext(
            actorUserKey: actor,
            targetUserKey: target,
            action: UAuthActions.Users.DeleteAdmin);

        var lifecycle = UserLifecycle.Create(
            context.ResourceTenant,
            target,
            Now.AddDays(-1));

        var defaultProfile = UserProfile.Create(
            Guid.NewGuid(),
            context.ResourceTenant,
            target,
            ProfileKey.Default,
            Now.AddDays(-1));

        var secondaryProfileKey = ProfileKey.Parse("secondary", null);

        var secondaryProfile = UserProfile.Create(
            Guid.NewGuid(),
            context.ResourceTenant,
            target,
            secondaryProfileKey,
            Now.AddDays(-1));

        f.LifecycleStore
            .Setup(x => x.GetAsync(
                It.IsAny<UserLifecycleKey>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(lifecycle);

        f.LifecycleStore
            .Setup(x => x.DeleteAsync(
                It.IsAny<UserLifecycleKey>(),
                lifecycle.Version,
                mode,
                Now,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        f.IdentifierStore
            .Setup(x => x.DeleteByUserAsync(
                target,
                mode,
                Now,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        f.ProfileStore
            .Setup(x => x.GetAllProfilesByUserAsync(
                target,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
            defaultProfile,
            secondaryProfile
            });

        f.ProfileStore
            .Setup(x => x.DeleteAsync(
                It.IsAny<UserProfileKey>(),
                It.IsAny<long>(),
                mode,
                Now,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        f.SessionStore
            .Setup(x => x.RevokeAllChainsAsync(
                target,
                Now,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await f.Sut.DeleteUserAsync(
            context,
            new DeleteUserRequest
            {
                Mode = mode
            });

        f.ProfileStore.Verify(x => x.DeleteAsync(
            new UserProfileKey(
                context.ResourceTenant,
                target,
                ProfileKey.Default),
            defaultProfile.Version,
            mode,
            Now,
            It.IsAny<CancellationToken>()),
            Times.Once);

        f.ProfileStore.Verify(x => x.DeleteAsync(
            new UserProfileKey(
                context.ResourceTenant,
                target,
                secondaryProfileKey),
            secondaryProfile.Version,
            mode,
            Now,
            It.IsAny<CancellationToken>()),
            Times.Once);

        f.ProfileStore.Verify(
            x => x.DeleteAsync(
                It.IsAny<UserProfileKey>(),
                It.IsAny<long>(),
                It.IsAny<DeleteMode>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    // ============================================================
    // Helpers
    // ============================================================

    private static Fixture CreateFixture(
        params IUserLifecycleIntegration[] integrations)
    {
        var access =
            new Mock<IAccessOrchestrator>(MockBehavior.Strict);

        var lifecycleFactory =
            new Mock<IUserLifecycleStoreFactory>(MockBehavior.Strict);

        var identifierFactory =
            new Mock<IUserIdentifierStoreFactory>(MockBehavior.Strict);

        var profileFactory =
            new Mock<IUserProfileStoreFactory>(MockBehavior.Strict);

        var lifecycleStore =
            new Mock<IUserLifecycleStore>(MockBehavior.Strict);

        var identifierStore =
            new Mock<IUserIdentifierStore>(MockBehavior.Strict);

        var profileStore =
            new Mock<IUserProfileStore>(MockBehavior.Strict);

        var validator =
            new Mock<IUserCreateValidator>(MockBehavior.Strict);

        var identifierValidator =
            new Mock<IIdentifierValidator>(MockBehavior.Strict);

        var normalizer =
            new Mock<IIdentifierNormalizer>(MockBehavior.Strict);

        var sessionFactory =
            new Mock<ISessionStoreFactory>(MockBehavior.Strict);

        var sessionStore =
            new Mock<ISessionStore>(MockBehavior.Strict);

        var clock =
            new Mock<IClock>(MockBehavior.Strict);

        clock.SetupGet(x => x.UtcNow)
            .Returns(Now);

        lifecycleFactory
            .Setup(x => x.Create(It.IsAny<TenantKey>()))
            .Returns(lifecycleStore.Object);

        identifierFactory
            .Setup(x => x.Create(It.IsAny<TenantKey>()))
            .Returns(identifierStore.Object);

        profileFactory
            .Setup(x => x.Create(It.IsAny<TenantKey>()))
            .Returns(profileStore.Object);

        sessionFactory
            .Setup(x => x.Create(It.IsAny<TenantKey>()))
            .Returns(sessionStore.Object);

        /*
         * Unit-test the service command body without mocking its contents.
         * Authorization itself belongs to IAccessOrchestrator tests.
         */
        access
            .Setup(x => x.ExecuteAsync(
                It.IsAny<AccessContext>(),
                It.IsAny<AccessCommand>(),
                It.IsAny<CancellationToken>()))
            .Returns<AccessContext, AccessCommand, CancellationToken>(
                (_, command, ct) => command.ExecuteAsync(ct));

        access
            .Setup(x => x.ExecuteAsync(
                It.IsAny<AccessContext>(),
                It.IsAny<AccessCommand<UserCreateResult>>(),
                It.IsAny<CancellationToken>()))
            .Returns<AccessContext, AccessCommand<UserCreateResult>, CancellationToken>(
                (_, command, ct) => command.ExecuteAsync(ct));

        var options = Options.Create(
            new UAuthServerOptions());

        var sut = new UserApplicationService(
            access.Object,
            lifecycleFactory.Object,
            identifierFactory.Object,
            profileFactory.Object,
            validator.Object,
            identifierValidator.Object,
            integrations,
            normalizer.Object,
            sessionFactory.Object,
            options,
            clock.Object);

        return new Fixture(
            sut,
            access,
            lifecycleStore,
            identifierStore,
            profileStore,
            validator,
            identifierValidator,
            normalizer,
            sessionStore);
    }

    private static AccessContext CreateContext(
        UserKey? actorUserKey = null,
        UserKey? targetUserKey = null,
        string action = "test")
    {
        actorUserKey ??= UserKey.New();

        return new AccessContext(
            actorUserKey: actorUserKey,
            actorTenant: TenantKey.Single,
            isAuthenticated: true,
            isSystemActor: false,
            actorChainId: null,
            resource: "users",
            targetUserKey: targetUserKey ?? actorUserKey,
            resourceTenant: TenantKey.Single,
            action: action,
            attributes: EmptyAttributes.Instance);
    }

    private static CreateUserRequest CreateUserRequest()
        => new()
        {
            UserName = "alice"
        };

    private static void SetupValidCreate(
    Fixture f,
    AccessContext context,
    CreateUserRequest request)
    {
        f.UserCreateValidator
            .Setup(x => x.ValidateAsync(
                context,
                request,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(UserCreateValidatorResult.Success());

        SetupCreateNormalizers(f, request);
    }

    private static void SetupIdentifierPersistence(Fixture f)
    {
        f.IdentifierStore
            .Setup(x => x.AddAsync(
                It.IsAny<UserIdentifier>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private static void SetupSuccessfulCreatePersistence(Fixture f)
    {
        f.LifecycleStore
            .Setup(x => x.AddAsync(
                It.IsAny<UserLifecycle>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        f.ProfileStore
            .Setup(x => x.AddAsync(
                It.IsAny<UserProfile>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        SetupIdentifierPersistence(f);
    }

    private static void SetupDeleteSelfPersistence(
        Fixture f,
        UserKey user,
        UserLifecycle lifecycle)
    {
        f.LifecycleStore
            .Setup(x => x.GetAsync(
                It.IsAny<UserLifecycleKey>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(lifecycle);

        f.LifecycleStore
            .Setup(x => x.DeleteAsync(
                It.IsAny<UserLifecycleKey>(),
                lifecycle.Version,
                DeleteMode.Soft,
                Now,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        f.IdentifierStore
            .Setup(x => x.DeleteByUserAsync(
                user,
                DeleteMode.Soft,
                Now,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        f.ProfileStore
            .Setup(x => x.GetAllProfilesByUserAsync(
                user,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<UserProfile>());

        f.SessionStore
            .Setup(x => x.RevokeAllChainsAsync(
                user,
                Now,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private static void SetupCreateNormalizers(
    Fixture f,
    CreateUserRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.UserName))
        {
            f.IdentifierNormalizer
                .Setup(x => x.Normalize(
                    UserIdentifierType.Username,
                    request.UserName))
                .Returns(new NormalizedIdentifier(
                    request.UserName,
                    request.UserName,
                    true,
                    null));
        }

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            f.IdentifierNormalizer
                .Setup(x => x.Normalize(
                    UserIdentifierType.Email,
                    request.Email))
                .Returns(new NormalizedIdentifier(
                    request.Email,
                    request.Email,
                    true,
                    null));
        }

        if (!string.IsNullOrWhiteSpace(request.Phone))
        {
            f.IdentifierNormalizer
                .Setup(x => x.Normalize(
                    UserIdentifierType.Phone,
                    request.Phone))
                .Returns(new NormalizedIdentifier(
                    request.Phone,
                    request.Phone,
                    true,
                    null));
        }
    }

    private sealed record Fixture(
        UserApplicationService Sut,
        Mock<IAccessOrchestrator> Access,
        Mock<IUserLifecycleStore> LifecycleStore,
        Mock<IUserIdentifierStore> IdentifierStore,
        Mock<IUserProfileStore> ProfileStore,
        Mock<IUserCreateValidator> UserCreateValidator,
        Mock<IIdentifierValidator> IdentifierValidator,
        Mock<IIdentifierNormalizer> IdentifierNormalizer,
        Mock<ISessionStore> SessionStore);

    private static UserIdentifier CreateIdentifier(
    UserKey userKey,
    UserIdentifierType type = UserIdentifierType.Email,
    bool isPrimary = false,
    bool isVerified = false,
    string? value = null,
    string? normalizedValue = null,
    long version = 0)
    {
        value ??= type switch
        {
            UserIdentifierType.Username => "alice",
            UserIdentifierType.Email => "alice@example.com",
            UserIdentifierType.Phone => "+905551112233",
            _ => "custom-value"
        };

        normalizedValue ??= value;

        return UserIdentifier.FromProjection(
            id: Guid.NewGuid(),
            tenant: TenantKey.Single,
            userKey: userKey,
            type: type,
            value: value,
            normalizedValue: normalizedValue,
            isPrimary: isPrimary,
            createdAt: Now.AddDays(-1),
            verifiedAt: isVerified ? Now.AddHours(-1) : null,
            updatedAt: null,
            deletedAt: null,
            version: version);
    }
}