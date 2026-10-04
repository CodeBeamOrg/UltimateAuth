using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Tests.Integration.Helpers;
using CodeBeam.UltimateAuth.Tests.Integration.Infrastructure;
using CodeBeam.UltimateAuth.Users.Contracts;
using CodeBeam.UltimateAuth.Users.Reference;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Tests.Integration;

public sealed class UserCreationAtomicityTests
{
    [Fact]
    public async Task CreateUser_WhenPersistenceFails_ShouldRollbackAllUserState()
    {
        var fault = new UserIdentifierStoreFaultState();

        using var factory = AuthServerFactory.CreateWithServices(
            services =>
            {
                services.AddSingleton(fault);

                services.DecorateForTest<IUserIdentifierStoreFactory>(
                    (sp, inner) =>
                        new FailingUserIdentifierStoreFactory(inner, sp.GetRequiredService<UserIdentifierStoreFaultState>()));
            });

        // Force host initialization before accessing stores.
        _ = factory.Services;

        fault.Enable(failOnAddAttempt: 2);

        using var scope = factory.Services.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IUserApplicationService>();

        var lifecycleFactory =
            scope.ServiceProvider
                .GetRequiredService<IUserLifecycleStoreFactory>();

        var profileFactory =
            scope.ServiceProvider
                .GetRequiredService<IUserProfileStoreFactory>();

        var identifierFactory =
            scope.ServiceProvider
                .GetRequiredService<IUserIdentifierStoreFactory>();

        var username =
            $"atomic-{Guid.NewGuid():N}";

        var email =
            $"atomic-{Guid.NewGuid():N}@example.com";

        var context = TestAccessContext.ForUserCreation(TestUsers.Admin, UAuthActions.Users.CreateAdmin);

        var request = new CreateUserRequest
        {
            UserName = username,
            Email = email,

            FirstName = "Atomic",
            LastName = "Failure"
        };

        //
        // Current implementation is expected to throw when
        // the second identifier is persisted.
        //
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateUserAsync(
                context,
                request));

        exception.Message.Should()
            .Be("simulated_identifier_store_failure");

        //
        // Verify that the intended failure actually happened
        // at the expected persistence point.
        //
        fault.AddAttempts.Should().Be(2);

        fault.LastAttemptedType
            .Should().Be(UserIdentifierType.Email);

        fault.LastAttemptedUserKey
            .Should().NotBeNull();

        var userKey =
            fault.LastAttemptedUserKey!.Value;

        var tenant =
            context.ResourceTenant;

        //
        // ATOMICITY CONTRACT
        var lifecycleStore = lifecycleFactory.Create(tenant);

        var profileStore = profileFactory.Create(tenant);

        var identifierStore = identifierFactory.Create(tenant);

        var lifecycle = await lifecycleStore.GetAsync(new UserLifecycleKey(tenant, userKey));

        var profiles = await profileStore.GetAllProfilesByUserAsync(userKey);

        var identifiers = await identifierStore.GetByUserAsync(userKey);

        lifecycle.Should().BeNull("failed user creation must not leave a lifecycle aggregate");

        identifiers.Should().BeEmpty("failed user creation must not leave identifiers");

        profiles.Should().BeEmpty("failed user creation must not leave user profiles");
    }
}
