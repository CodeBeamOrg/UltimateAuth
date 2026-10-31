using Moq;
using CodeBeam.UltimateAuth.Authorization;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.IdentityLab.Authorization;
using CodeBeam.UltimateAuth.IdentityLab.Contracts;
using CodeBeam.UltimateAuth.IdentityLab.Seed;
using CodeBeam.UltimateAuth.IdentityLab.Services;
using CodeBeam.UltimateAuth.Users.Contracts;
using CodeBeam.UltimateAuth.Users.Reference;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace CodeBeam.UltimateAuth.IdentityLab.Tests.Integration;

public sealed class RegistrationIntegrationTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task Registration_persists_account_owner_role_and_sandbox_without_ladmin_assignment()
    {
        await using var db = await postgres.CreateAsync();
        await using var runtime = await LabRuntime.CreateAsync(db);
        await using var scope = runtime.Services.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var registration = new LabRegistrationService(ActorProvider.For(null), sp.GetRequiredService<IUserApplicationService>(),
            sp.GetRequiredService<IRoleStoreFactory>(), sp.GetRequiredService<IUserRoleStoreFactory>(),
            new(db), new(db, NullLogger<ActivityLogService>.Instance));
        var result = await registration.RegisterAsync(new("alice", "alice@example.test", "Alice-test-password-8!"));
        Assert.True(result.IsSuccess, result.ErrorMessage);
        UserKey owner;
        await using (var verify = db.CreateDbContext())
        {
            var sandbox = await verify.Sandboxes.Include(x => x.Members).SingleAsync();
            owner = sandbox.OwnerUserKey;
            Assert.Equal(owner, Assert.Single(sandbox.Members).UserKey);
        }
        // Query actual framework stores, using a new scope to avoid tracked state.
        await using var verification = runtime.Services.CreateAsyncScope();
        var assignments = await verification.ServiceProvider.GetRequiredService<IUserRoleStoreFactory>()
            .Create(TenantKeys.Single).GetAssignmentsAsync(owner);
        Assert.Contains(assignments, x => x.RoleId == LabRoleIds.Owner);
        Assert.DoesNotContain(assignments, x => x.RoleId == LabRoleIds.Admin);
        var lifecycle = await verification.ServiceProvider.GetRequiredService<IUserLifecycleStoreFactory>()
            .Create(TenantKeys.Single).GetAsync(new UserLifecycleKey(TenantKeys.Single, owner));
        Assert.NotNull(lifecycle);
        var duplicate = await Record.ExceptionAsync(async () =>
        {
            var duplicateResult = await registration.RegisterAsync(new("alice", "alice@example.test", "Alice-test-password-8!"));
            Assert.False(duplicateResult.IsSuccess);
        });
        // Duplicate validation may throw or return failure depending on the framework contract.
        if (duplicate is not null) Assert.IsType<CodeBeam.UltimateAuth.Core.Errors.UAuthValidationException>(duplicate);
        await using var final = db.CreateDbContext();
        Assert.Equal(1, await final.Sandboxes.CountAsync());
    }

    [Fact]
    public async Task Trusted_seed_is_repeatable_and_assigns_ladmin_only_to_fixed_admin_identity()
    {
        await using var db = await postgres.CreateAsync();
        await using var runtime = await LabRuntime.CreateAsync(db);
        await using var scope = runtime.Services.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        await ActivatorUtilities.CreateInstance<LabAdminSeedContributor>(sp).SeedAsync(TenantKeys.Single);
        await ActivatorUtilities.CreateInstance<LabDemoRoleSeedContributor>(sp).SeedAsync(TenantKeys.Single);
        var store = sp.GetRequiredService<IUserRoleStoreFactory>().Create(TenantKeys.Single);
        var assignments = await store.GetAssignmentsAsync(LabAdminIdentity.UserKey);
        Assert.Single(assignments, x => x.RoleId == LabRoleIds.Admin);
        Assert.Equal(1, await store.CountAssignmentsAsync(LabRoleIds.Admin));
        var roleStore = sp.GetRequiredService<IRoleStoreFactory>().Create(TenantKeys.Single);
        var manager = await roleStore.GetByNameAsync("MANAGER");
        Assert.NotNull(manager);
        Assert.Equal(LabRoleIds.Manager, manager.Id);
        Assert.True(manager.Permissions.Select(x => x.Value).ToHashSet().SetEquals(LabDemoPermissions.Manager));
    }
    [Theory]
    [InlineData("Ladmin")]
    [InlineData("Owner")]
    public async Task Actual_role_service_denies_user_escalation_without_changing_persisted_assignments(string role)
    {
        await using var db = await postgres.CreateAsync();
        await using var runtime = await LabRuntime.CreateAsync(db);
        await db.SeedAsync();
        await using var scope = runtime.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IUserRoleService>();
        var store = scope.ServiceProvider.GetRequiredService<IUserRoleStoreFactory>().Create(TenantKeys.Single);
        var before = await store.GetAssignmentsAsync(LabDatabase.MemberA);
        var context = Contexts.Create(CodeBeam.UltimateAuth.Core.Defaults.UAuthActions.Authorization.Roles.AssignAdmin,
            LabDatabase.MemberA, LabDatabase.MemberA);
        await Assert.ThrowsAsync<CodeBeam.UltimateAuth.Core.Errors.UAuthAuthorizationException>(() =>
            service.AssignAsync(context, LabDatabase.MemberA, role));
        await using var verification = runtime.Services.CreateAsyncScope();
        var after = await verification.ServiceProvider.GetRequiredService<IUserRoleStoreFactory>()
            .Create(TenantKeys.Single).GetAssignmentsAsync(LabDatabase.MemberA);
        Assert.Equal(before.Select(x => x.RoleId).OrderBy(x => x.Value), after.Select(x => x.RoleId).OrderBy(x => x.Value));
    }

    [Fact]
    public async Task Owner_can_provision_member_and_assign_demo_role_but_cannot_delegate_ladmin()
    {
        await using var db = await postgres.CreateAsync();
        await using var runtime = await LabRuntime.CreateAsync(db);
        await using var scope = runtime.Services.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var users = sp.GetRequiredService<IUserApplicationService>();
        var roles = sp.GetRequiredService<IRoleStoreFactory>();
        var assignments = sp.GetRequiredService<IUserRoleStoreFactory>();
        var registration = new LabRegistrationService(ActorProvider.For(null), users, roles, assignments,
            new(db), new(db, NullLogger<ActivityLogService>.Instance));
        Assert.True((await registration.RegisterAsync(new("owner", "owner@example.test", "Owner-test-password-8!"))).IsSuccess);
        UserKey owner;
        await using (var verify = db.CreateDbContext()) owner = (await verify.Sandboxes.SingleAsync()).OwnerUserKey;
        var provider = ActorProvider.For(owner);
        var provisioning = new LabUserProvisioningService(provider, new(db), new(db), users, roles, assignments,
            sp.GetRequiredService<LabProvisioningRollbackGate>(), NullLogger<LabUserProvisioningService>.Instance);
        var result = await provisioning.CreateMemberAsync(owner, new CreateUserRequest
            { UserName = "member", Email = "member@example.test", Password = "Member-test-password-8!" });
        Assert.True(result.IsSuccess, result.FailureReason);
        var member = UserKey.FromString(result.UserKey!);
        var demos = new LabDemoRoleService(provider, db, roles, assignments);
        await demos.SetRoleAsync(member, "Manager", true);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => demos.SetRoleAsync(member, "Ladmin", true));
        var roleService = sp.GetRequiredService<IUserRoleService>();
        await Assert.ThrowsAsync<CodeBeam.UltimateAuth.Core.Errors.UAuthAuthorizationException>(() =>
            roleService.AssignAsync(Contexts.Create(CodeBeam.UltimateAuth.Core.Defaults.UAuthActions.Authorization.Roles.AssignAdmin,
                owner, member), member, "Ladmin"));
        await using var fresh = runtime.Services.CreateAsyncScope();
        var persisted = await fresh.ServiceProvider.GetRequiredService<IUserRoleStoreFactory>()
            .Create(TenantKeys.Single).GetAssignmentsAsync(member);
        Assert.Contains(persisted, x => x.RoleId == LabRoleIds.User);
        Assert.Contains(persisted, x => x.RoleId == LabRoleIds.Manager);
        Assert.DoesNotContain(persisted, x => x.RoleId == LabRoleIds.Admin || x.RoleId == LabRoleIds.Owner);
        await using var final = db.CreateDbContext();
        Assert.Equal(member, (await final.SandboxMembers.SingleAsync(x => x.UserKey == member)).UserKey);
    }

    [Fact]
    public async Task Owner_cannot_rename_delete_or_change_permissions_of_system_roles()
    {
        await using var db = await postgres.CreateAsync();
        await using var runtime = await LabRuntime.CreateAsync(db);
        await db.SeedAsync();
        await using var scope = runtime.Services.CreateAsyncScope();
        var roles = scope.ServiceProvider.GetRequiredService<IRoleService>();
        var store = scope.ServiceProvider.GetRequiredService<IRoleStoreFactory>().Create(TenantKeys.Single);
        var before = await store.GetByNameAsync("LADMIN");
        Assert.NotNull(before);
        CodeBeam.UltimateAuth.Core.Contracts.AccessContext Context(string action) =>
            Contexts.Create(action, LabDatabase.OwnerA);
        await Assert.ThrowsAsync<CodeBeam.UltimateAuth.Core.Errors.UAuthAuthorizationException>(() =>
            roles.RenameAsync(Context("authorization.roles.rename.admin"), LabRoleIds.Admin, "Manager"));
        await Assert.ThrowsAsync<CodeBeam.UltimateAuth.Core.Errors.UAuthAuthorizationException>(() =>
            roles.DeleteAsync(Context("authorization.roles.delete.admin"), LabRoleIds.Admin,
                CodeBeam.UltimateAuth.Core.Contracts.DeleteMode.Hard));
        await Assert.ThrowsAsync<CodeBeam.UltimateAuth.Core.Errors.UAuthAuthorizationException>(() =>
            roles.SetPermissionsAsync(Context("authorization.roles.permissions.admin"), LabRoleIds.Admin,
                Array.Empty<CodeBeam.UltimateAuth.Authorization.Contracts.Permission>()));
        await using var fresh = runtime.Services.CreateAsyncScope();
        var after = await fresh.ServiceProvider.GetRequiredService<IRoleStoreFactory>()
            .Create(TenantKeys.Single).GetByNameAsync("LADMIN");
        Assert.NotNull(after);
        Assert.Equal(before.Id, after.Id);
        Assert.Equal(before.Version, after.Version);
        Assert.Equal(before.Name, after.Name);
        Assert.Equal(before.IsDeleted, after.IsDeleted);
        Assert.Equal(before.Permissions.Select(x => x.Value).Order(), after.Permissions.Select(x => x.Value).Order());
    }

    [Fact]
    public async Task Provisioning_assignment_fault_hard_deletes_real_partial_account_without_changing_sandboxes()
    {
        await using var db = await postgres.CreateAsync();
        await using var runtime = await LabRuntime.CreateAsync(db);
        await using var scope = runtime.Services.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var users = sp.GetRequiredService<IUserApplicationService>();
        var roles = sp.GetRequiredService<IRoleStoreFactory>();
        var assignments = sp.GetRequiredService<IUserRoleStoreFactory>();
        var registration = new LabRegistrationService(ActorProvider.For(null), users, roles, assignments,
            new(db), new(db, NullLogger<ActivityLogService>.Instance));
        Assert.True((await registration.RegisterAsync(new("owner", "owner@example.test", "Owner-test-password-8!"))).IsSuccess);
        UserKey owner;
        await using (var verify = db.CreateDbContext()) owner = (await verify.Sandboxes.SingleAsync()).OwnerUserKey;
        var before = await db.SnapshotAsync();
        var failedStore = new Moq.Mock<IUserRoleStore>();
        failedStore.Setup(x => x.GetAssignmentsAsync(Moq.It.IsAny<UserKey>(), Moq.It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Injected role-store failure"));
        var failedFactory = new Moq.Mock<IUserRoleStoreFactory>();
        failedFactory.Setup(x => x.Create(Moq.It.IsAny<TenantKey>())).Returns(failedStore.Object);
        var provisioning = new LabUserProvisioningService(ActorProvider.For(owner), new(db), new(db), users,
            roles, failedFactory.Object, sp.GetRequiredService<LabProvisioningRollbackGate>(),
            NullLogger<LabUserProvisioningService>.Instance);
        var failure = await Assert.ThrowsAsync<LabPartialProvisioningException>(() => provisioning.CreateMemberAsync(owner,
            new CreateUserRequest { UserName = "failed-member", Email = "failed-member@example.test", Password = "Member-test-password-8!" }));
        var partialKey = UserKey.FromString(failure.CreatedUserKey);
        await using var fresh = runtime.Services.CreateAsyncScope();
        var lifecycle = await fresh.ServiceProvider.GetRequiredService<IUserLifecycleStoreFactory>()
            .Create(TenantKeys.Single).GetAsync(new UserLifecycleKey(TenantKeys.Single, partialKey));
        Assert.Null(lifecycle);
        var remainingRoles = await fresh.ServiceProvider.GetRequiredService<IUserRoleStoreFactory>()
            .Create(TenantKeys.Single).GetAssignmentsAsync(partialKey);
        Assert.Empty(remainingRoles);
        Assert.Equal(before, await db.SnapshotAsync());
    }

    [Fact]
    public async Task Actual_context_factory_replaces_forged_ladmin_attributes_with_stored_owner_identity()
    {
        await using var db = await postgres.CreateAsync();
        await using var runtime = await LabRuntime.CreateAsync(db);
        await db.SeedAsync();
        await using var scope = runtime.Services.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        await new LabRoleService(sp.GetRequiredService<IRoleStoreFactory>(), sp.GetRequiredService<IUserRoleStoreFactory>())
            .AssignAsync(TenantKeys.Single, LabDatabase.OwnerA, "Owner");
        var context = await sp.GetRequiredService<CodeBeam.UltimateAuth.Server.Auth.IAccessContextFactory>()
            .CreateAsync(Contexts.AuthenticatedFlow(LabDatabase.OwnerA), "users.delete.admin", "users",
                LabDatabase.MemberB.Value, new Dictionary<string, object> { ["roles"] = new[] { LabRoleIds.Admin } });
        Assert.False(new LabRootResolver().IsLadmin(context));
        Assert.Equal(LabDatabase.OwnerA, context.ActorUserKey);
        var resolved = Assert.IsAssignableFrom<IEnumerable<CodeBeam.UltimateAuth.Authorization.Contracts.RoleId>>(context.Attributes["roles"]);
        Assert.Contains(LabRoleIds.Owner, resolved);
        Assert.DoesNotContain(LabRoleIds.Admin, resolved);
        var decision = await new CodeBeam.UltimateAuth.IdentityLab.Policy.SandboxUAuthAccessPolicy(db,
            new LabRootResolver(), sp.GetRequiredService<LabProvisioningRollbackGate>()).DecideAsync(context);
        Assert.True(decision.IsDenied);
        Assert.Equal("sandbox_target_out_of_scope", decision.DenyReason);
    }

}
