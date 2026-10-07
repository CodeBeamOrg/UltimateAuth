using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Users.Contracts;
using CodeBeam.UltimateAuth.Users.Reference;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Tests.Integration.EfCore.Users;

public sealed class UserCreationAtomicityTests
{
    [Fact]
    public async Task CreateUser_WhenIdentifierPersistenceFails_ShouldRollbackAllUserState()
    {
        var fault =
            new UserIdentifierStoreFaultState();

        await using var runtime =
            await EfCoreTestRuntime.CreateAsync(
                services =>
                {
                    services.AddSingleton(fault);

                    services.DecorateForTest<IUserIdentifierStoreFactory>(
                        (sp, inner) => new FailingUserIdentifierStoreFactory(inner, sp.GetRequiredService<UserIdentifierStoreFaultState>()));
                });

        // Fault injection is enabled only after the runtime and database
        // have been fully initialized.
        fault.Enable(failOnAddAttempt: 2);

        UserKey userKey;
        TenantKey tenant;

        //
        // Execute user creation in its own DI scope.
        //
        using (var scope = runtime.Services.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IUserApplicationService>();

            var context = TestAccessContext.ForUserCreation(TestUsers.Admin,UAuthActions.Users.CreateAnonymous);

            tenant = context.ResourceTenant;

            var request =
                new CreateUserRequest
                {
                    UserName = $"atomic-{Guid.NewGuid():N}",
                    Email = $"atomic-{Guid.NewGuid():N}@example.com",
                    FirstName = "Atomic",
                    LastName = "Failure"
                };

            var exception =
                await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateUserAsync(context, request));

            exception.Message.Should().Be("simulated_identifier_store_failure");

            fault.AddAttempts.Should().Be(2);

            fault.LastAttemptedType.Should().Be(UserIdentifierType.Email);

            fault.LastAttemptedUserKey.Should().NotBeNull();

            userKey = fault.LastAttemptedUserKey!.Value;
        }

        //
        // Verify using a fresh scope / DbContext.
        //
        // This ensures that the assertions observe persisted database
        // state rather than the DbContext change tracker used by the
        // failed operation.
        //
        using (var scope = runtime.Services.CreateScope())
        {
            var lifecycleFactory =
                scope.ServiceProvider
                    .GetRequiredService<IUserLifecycleStoreFactory>();

            var profileFactory =
                scope.ServiceProvider
                    .GetRequiredService<IUserProfileStoreFactory>();

            var identifierFactory =
                scope.ServiceProvider
                    .GetRequiredService<IUserIdentifierStoreFactory>();

            var lifecycle =
                await lifecycleFactory
                    .Create(tenant)
                    .GetAsync(
                        new UserLifecycleKey(
                            tenant,
                            userKey));

            var profiles =
                await profileFactory
                    .Create(tenant)
                    .GetAllProfilesByUserAsync(userKey);

            var identifiers =
                await identifierFactory
                    .Create(tenant)
                    .GetByUserAsync(userKey);

            //
            // Atomicity contract:
            //
            // A failed user creation must be observationally equivalent
            // to the operation never having occurred.
            //
            lifecycle.Should().BeNull();

            profiles.Should().BeEmpty();

            identifiers.Should().BeEmpty();
        }
    }
}
