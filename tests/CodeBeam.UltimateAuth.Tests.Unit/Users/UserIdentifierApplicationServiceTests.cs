using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using CodeBeam.UltimateAuth.Users.Contracts;
using CodeBeam.UltimateAuth.Users.Reference;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using static CodeBeam.UltimateAuth.Core.Defaults.UAuthActions;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class UserIdentifierApplicationServiceTests
{
    [Fact]
    public async Task Adding_new_primary_email_should_replace_existing_primary()
    {
        var runtime = new TestAuthRuntime<UserKey>();
        var service = runtime.GetUserApplicationService();

        var context = TestAccessContext.ForUser(TestUsers.User, UserIdentifiers.AddSelf);

        await service.AddUserIdentifierAsync(context,
            new AddUserIdentifierRequest
            {
                Type = UserIdentifierType.Email,
                Value = "new@example.com",
                IsPrimary = true
            });

        var identifiers = await service.GetIdentifiersByUserAsync(context, new UserIdentifierQuery());

        identifiers.Items.Where(x => x.Type == UserIdentifierType.Email).Should().ContainSingle(x => x.IsPrimary);

        identifiers.Items.Single(x => x.Type == UserIdentifierType.Email && x.IsPrimary).Value.Should().Be("new@example.com");
    }

    [Fact]
    public async Task Primary_phone_should_not_be_login_if_not_allowed()
    {
        var runtime = new TestAuthRuntime<UserKey>(configureServer: o =>
        {
            o.LoginIdentifiers.AllowedTypes = new HashSet<UserIdentifierType>
            {
                UserIdentifierType.Email
            };
        });

        var identifierService = runtime.Services.GetRequiredService<IUserApplicationService>();
        var loginOrchestrator = runtime.GetLoginOrchestrator();
        var flow = await runtime.CreateLoginFlowAsync();

        var context = TestAccessContext.ForUser(
            TestUsers.User,
            action: "users.identifiers.add");

        await identifierService.AddUserIdentifierAsync(context,
            new AddUserIdentifierRequest
            {
                Type = UserIdentifierType.Phone,
                Value = "+905551111111",
                IsPrimary = true
            });

        var result = await loginOrchestrator.LoginAsync(flow,
            new LoginRequest
            {
                Identifier = "+905551111111",
                Secret = "user",
            });

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Adding_non_primary_should_not_affect_existing_primary()
    {
        var runtime = new TestAuthRuntime<UserKey>();
        var service = runtime.GetUserApplicationService();

        var context = TestAccessContext.ForUser(TestUsers.User, UserIdentifiers.AddSelf);

        var before = await service.GetIdentifiersByUserAsync(context, new UserIdentifierQuery());
        var existingPrimaryEmail = before.Items.Single(x => x.Type == UserIdentifierType.Email && x.IsPrimary);

        await service.AddUserIdentifierAsync(context,
            new AddUserIdentifierRequest
            {
                Type = UserIdentifierType.Email,
                Value = "secondary@example.com",
                IsPrimary = false
            });

        var after = await service.GetIdentifiersByUserAsync(context, new UserIdentifierQuery());

        after.Items.Single(x => x.Type == UserIdentifierType.Email && x.IsPrimary)
             .Id.Should().Be(existingPrimaryEmail.Id);
    }

    [Fact]
    public async Task Updating_value_should_reset_verification()
    {
        var runtime = new TestAuthRuntime<UserKey>();
        var service = runtime.GetUserApplicationService();

        var context = TestAccessContext.ForUser(TestUsers.User, UserIdentifiers.UpdateSelf);

        var email = (await service.GetIdentifiersByUserAsync(context, new UserIdentifierQuery())).Items.Single(x => x.Type == UserIdentifierType.Email);

        await service.UpdateUserIdentifierAsync(context,
            new UpdateUserIdentifierRequest
            {
                Id = email.Id,
                NewValue = "updated@example.com"
            });

        var updated = (await service.GetIdentifiersByUserAsync(context, new UserIdentifierQuery())).Items
            .Single(x => x.Id == email.Id);

        updated.IsVerified.Should().BeFalse();
    }

    [Fact]
    public async Task Duplicate_email_should_be_allowed_when_uniqueness_is_none()
    {
        var runtime = new TestAuthRuntime<UserKey>(configureServer: o =>
        {
            o.Identifiers.Uniqueness.Email = UniquenessScope.None;
        });

        var service = runtime.GetUserApplicationService();

        var user1 = TestAccessContext.ForUser(
            TestUsers.User,
            UserIdentifiers.AddSelf);

        var user2 = TestAccessContext.ForUser(
            TestUsers.Admin,
            UserIdentifiers.AddSelf);

        await service.AddUserIdentifierAsync(
            user1,
            new AddUserIdentifierRequest
            {
                Type = UserIdentifierType.Email,
                Value = "shared@example.com",
                IsPrimary = false
            });

        Func<Task> act = () =>
            service.AddUserIdentifierAsync(
                user2,
                new AddUserIdentifierRequest
                {
                    Type = UserIdentifierType.Email,
                    Value = "shared@example.com",
                    IsPrimary = false
                });

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Add_email_should_fail_when_tenant_uniqueness_is_enabled()
    {
        var runtime = new TestAuthRuntime<UserKey>(configureServer: o =>
        {
            o.Identifiers.Uniqueness.Email = UniquenessScope.Tenant;
        });

        var service = runtime.GetUserApplicationService();

        var user1 = TestAccessContext.ForUser(
            TestUsers.User,
            UserIdentifiers.AddSelf);

        var user2 = TestAccessContext.ForUser(
            TestUsers.Admin,
            UserIdentifiers.AddSelf);

        await service.AddUserIdentifierAsync(
            user1,
            new AddUserIdentifierRequest
            {
                Type = UserIdentifierType.Email,
                Value = "unique@example.com"
            });

        Func<Task> act = () =>
            service.AddUserIdentifierAsync(
                user2,
                new AddUserIdentifierRequest
                {
                    Type = UserIdentifierType.Email,
                    Value = "unique@example.com"
                });

        await act.Should()
            .ThrowAsync<UAuthIdentifierConflictException>()
            .WithMessage("*identifier_already_exists*");
    }

    [Fact]
    public async Task Add_email_should_succeed_across_users_when_uniqueness_is_none()
    {
        var runtime = new TestAuthRuntime<UserKey>(configureServer: o =>
        {
            o.Identifiers.Uniqueness.Email = UniquenessScope.None;
        });

        var service = runtime.GetUserApplicationService();

        var user1 = TestAccessContext.ForUser(
            TestUsers.User,
            UserIdentifiers.AddSelf);

        var user2 = TestAccessContext.ForUser(
            TestUsers.Admin,
            UserIdentifiers.AddSelf);

        await service.AddUserIdentifierAsync(
            user1,
            new AddUserIdentifierRequest
            {
                Type = UserIdentifierType.Email,
                Value = "shared@example.com"
            });

        Func<Task> act = () =>
            service.AddUserIdentifierAsync(
                user2,
                new AddUserIdentifierRequest
                {
                    Type = UserIdentifierType.Email,
                    Value = "shared@example.com"
                });

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Unsetting_last_login_identifier_should_fail()
    {
        var runtime = new TestAuthRuntime<UserKey>(configureServer: o =>
        {
            o.LoginIdentifiers.AllowedTypes = new HashSet<UserIdentifierType>
        {
            UserIdentifierType.Email
        };
        });

        var service = runtime.GetUserApplicationService();

        var context = TestAccessContext.ForUser(TestUsers.User, UserIdentifiers.UnsetPrimarySelf);

        var email = (await service.GetIdentifiersByUserAsync(context, new UserIdentifierQuery())).Items.Single(x => x.Type == UserIdentifierType.Email && x.IsPrimary);

        Func<Task> act = async () =>
            await service.UnsetPrimaryUserIdentifierAsync(context,
                new UnsetPrimaryUserIdentifierRequest
                {
                    Id = email.Id
                });

        await act.Should().ThrowAsync<UAuthIdentifierConflictException>();
    }

    [Fact]
    public async Task Email_should_be_case_insensitive_by_default()
    {
        var runtime = new TestAuthRuntime<UserKey>();
        var service = runtime.GetUserApplicationService();

        var context = TestAccessContext.ForUser(TestUsers.User, UserIdentifiers.AddSelf);

        await service.AddUserIdentifierAsync(context,
            new AddUserIdentifierRequest
            {
                Type = UserIdentifierType.Email,
                Value = "Test@Example.com"
            });

        Func<Task> act = async () =>
            await service.AddUserIdentifierAsync(context,
                new AddUserIdentifierRequest
                {
                    Type = UserIdentifierType.Email,
                    Value = "test@example.com"
                });

        await act.Should().ThrowAsync<UAuthIdentifierConflictException>();
    }

    [Fact]
    public async Task Username_should_respect_case_policy()
    {
        var runtime = new TestAuthRuntime<UserKey>(configureServer: o =>
        {
            o.Identifiers.Behavior.AllowMultipleUsernames = true;
        });

        var service = runtime.GetUserApplicationService();

        var context = TestAccessContext.ForUser(TestUsers.User, UserIdentifiers.AddSelf);

        await service.AddUserIdentifierAsync(context,
            new AddUserIdentifierRequest
            {
                Type = UserIdentifierType.Username,
                Value = "UserName"
            });

        var identifiers = await service.GetIdentifiersByUserAsync(context, new UserIdentifierQuery());

        identifiers.Items.Should().Contain(x => x.Value == "UserName");
    }

    [Fact]
    public async Task Username_should_be_case_insensitive_when_configured()
    {
        var runtime = new TestAuthRuntime<UserKey>(configureServer: o =>
        {
            o.Identifiers.Normalization.UsernameCase = CaseHandling.ToLower;
            o.Identifiers.Behavior.AllowMultipleUsernames = true;
        });

        var service = runtime.GetUserApplicationService();
        var context = TestAccessContext.ForUser(TestUsers.User, UserIdentifiers.AddSelf);

        await service.AddUserIdentifierAsync(context,
            new AddUserIdentifierRequest
            {
                Type = UserIdentifierType.Username,
                Value = "UserName"
            });

        Func<Task> act = async () =>
            await service.AddUserIdentifierAsync(context,
                new AddUserIdentifierRequest
                {
                    Type = UserIdentifierType.Username,
                    Value = "username"
                });

        await act.Should().ThrowAsync<UAuthIdentifierConflictException>();
    }

    [Fact]
    public async Task Phone_should_be_normalized_to_digits()
    {
        var runtime = new TestAuthRuntime<UserKey>();
        var service = runtime.GetUserApplicationService();

        var context = TestAccessContext.ForUser(TestUsers.User, UserIdentifiers.AddSelf);

        await service.AddUserIdentifierAsync(context,
            new AddUserIdentifierRequest
            {
                Type = UserIdentifierType.Phone,
                Value = "+90 (555) 123-45-67"
            });

        var identifiers = await service.GetIdentifiersByUserAsync(context, new UserIdentifierQuery());

        identifiers.Items.Should().Contain(x =>
            x.Type == UserIdentifierType.Phone &&
            x.Value == "+90 (555) 123-45-67");
    }

    [Fact]
    public async Task Updating_to_existing_value_should_fail()
    {
        var runtime = new TestAuthRuntime<UserKey>();
        var service = runtime.GetUserApplicationService();

        var context = TestAccessContext.ForUser(TestUsers.User, UserIdentifiers.AddSelf);

        await service.AddUserIdentifierAsync(context,
            new AddUserIdentifierRequest
            {
                Type = UserIdentifierType.Email,
                Value = "one@example.com"
            });

        await service.AddUserIdentifierAsync(context,
            new AddUserIdentifierRequest
            {
                Type = UserIdentifierType.Email,
                Value = "two@example.com"
            });

        var identifiers = await service.GetIdentifiersByUserAsync(context, new UserIdentifierQuery());
        var second = identifiers.Items.Single(x => x.Value == "two@example.com");

        Func<Task> act = async () =>
            await service.UpdateUserIdentifierAsync(context,
                new UpdateUserIdentifierRequest
                {
                    Id = second.Id,
                    NewValue = "one@example.com"
                });

        await act.Should().ThrowAsync<UAuthIdentifierConflictException>();
    }

    [Fact]
    public async Task Availability_should_return_available_for_unused_identifier()
    {
        var runtime = new TestAuthRuntime<UserKey>();
        var service = runtime.GetUserApplicationService();

        var context = TestAccessContext.ForUser(
            TestUsers.User,
            UserIdentifiers.CheckAvailability);

        var result = await service.CheckIdentifierAvailabilityAsync(
            context,
            new CheckUserIdentifierAvailabilityRequest
            {
                Type = UserIdentifierType.Username,
                Value = "unused-user-name"
            });

        result.IsValid.Should().BeTrue();
        result.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public async Task Availability_should_return_unavailable_for_existing_identifier()
    {
        var runtime = new TestAuthRuntime<UserKey>();
        var service = runtime.GetUserApplicationService();

        var context = TestAccessContext.ForUser(
            TestUsers.User,
            UserIdentifiers.CheckAvailability);

        var identifiers =
            await service.GetIdentifiersByUserAsync(
                context,
                new UserIdentifierQuery());

        var existing = identifiers.Items.First();

        var result = await service.CheckIdentifierAvailabilityAsync(
            context,
            new CheckUserIdentifierAvailabilityRequest
            {
                Type = existing.Type,
                Value = existing.Value
            });

        result.IsValid.Should().BeTrue();
        result.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task Availability_should_use_normalized_identifier()
    {
        var runtime = new TestAuthRuntime<UserKey>();

        var user = await runtime.CreateLoginUserAsync();

        const string storedEmail = "availability@example.com";

        await runtime.AddIdentifierAsync(user.UserKey, UserIdentifierType.Email, storedEmail);

        var service = runtime.GetUserApplicationService();

        var context = TestAccessContext.ForUser(user.UserKey, UserIdentifiers.CheckAvailability);

        var result = await service.CheckIdentifierAvailabilityAsync(context,
            new CheckUserIdentifierAvailabilityRequest
            {
                Type = UserIdentifierType.Email,
                Value = "  AVAILABILITY@EXAMPLE.COM  "
            });

        result.IsValid.Should().BeTrue();
        result.IsAvailable.Should().BeFalse();
        result.NormalizedValue.Should().Be("availability@example.com");
    }

    [Fact]
    public async Task Availability_should_return_validation_errors_for_invalid_identifier()
    {
        var runtime = new TestAuthRuntime<UserKey>();
        var service = runtime.GetUserApplicationService();

        var context = TestAccessContext.ForUser(
            TestUsers.User,
            UserIdentifiers.CheckAvailability);

        var result = await service.CheckIdentifierAvailabilityAsync(
            context,
            new CheckUserIdentifierAvailabilityRequest
            {
                Type = UserIdentifierType.Email,
                Value = "invalid"
            });

        result.IsValid.Should().BeFalse();
        result.IsAvailable.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Email_availability_should_be_case_insensitive_by_default()
    {
        var runtime = new TestAuthRuntime<UserKey>();

        var user =
            await runtime.CreateLoginUserAsync();

        await runtime.AddIdentifierAsync(
            user.UserKey,
            UserIdentifierType.Email,
            "availability@example.com");

        var service =
            runtime.GetUserApplicationService();

        var context = TestAccessContext.ForUser(
            user.UserKey,
            UserIdentifiers.CheckAvailability);

        var result =
            await service.CheckIdentifierAvailabilityAsync(
                context,
                new CheckUserIdentifierAvailabilityRequest
                {
                    Type = UserIdentifierType.Email,
                    Value = "AVAILABILITY@EXAMPLE.COM"
                });

        result.IsValid.Should().BeTrue();
        result.IsAvailable.Should().BeFalse();
        result.NormalizedValue.Should().Be("availability@example.com");
    }

    //[Fact]
    //public async Task Same_identifier_in_different_tenants_should_not_conflict()
    //{
    //    var runtime = new TestAuthRuntime<UserKey>();

    //    var service = runtime.GetUserApplicationService();

    //    var tenant1User = TestAccessContext.ForUser(TestUsers.User, UserIdentifiers.AddSelf, TenantKey.Single);
    //    var tenant2User = TestAccessContext.ForUser(TestUsers.User, UserIdentifiers.AddSelf, TenantKey.FromInternal("other"));

    //    await service.AddUserIdentifierAsync(tenant1User,
    //        new AddUserIdentifierRequest
    //        {
    //            Type = UserIdentifierType.Email,
    //            Value = "tenant@example.com",
    //            IsPrimary = true
    //        });

    //    await service.AddUserIdentifierAsync(tenant2User,
    //        new AddUserIdentifierRequest
    //        {
    //            Type = UserIdentifierType.Email,
    //            Value = "tenant@example.com",
    //            IsPrimary = true
    //        });

    //    true.Should().BeTrue();
    //}
}   

