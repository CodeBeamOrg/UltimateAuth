using CodeBeam.UltimateAuth.Authorization;
using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Tests.Integration.Helpers;
using CodeBeam.UltimateAuth.Tests.Integration.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace CodeBeam.UltimateAuth.Tests.Integration;

public sealed class AuthorizationManagementTests
{
    private const string AdminBase = "/auth/admin/authorization";
    private const string SelfBase = "/auth/me/authorization";

    // --------------------------------------------------
    // Route / Request consistency
    // --------------------------------------------------

    [Fact]
    public async Task AssignRole_TargetMismatch_ShouldRejectWithoutMutation()
    {
        using var factory = NewFactory();

        var admin = await factory.CreateLoginUserAsync();
        var target = await factory.CreateLoginUserAsync();
        var other = await factory.CreateLoginUserAsync();

        await GrantAsync(factory, admin,
            UAuthActions.Authorization.Roles.AssignAdmin);

        var role = await SeedRoleAsync(factory);

        using var client = await AuthenticatedClientAsync(factory, admin);

        using var response = await client.PostAsJsonAsync(
            UserRolesUrl(target.UserKey, "assign"),
            new AssignRoleRequest
            {
                UserKey = other.UserKey,
                RoleName = role.Name
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        (await HasRoleAsync(factory, target.UserKey, role.Id))
            .Should().BeFalse();

        (await HasRoleAsync(factory, other.UserKey, role.Id))
            .Should().BeFalse();
    }

    [Fact]
    public async Task RemoveRole_TargetMismatch_ShouldRejectWithoutMutation()
    {
        using var factory = NewFactory();

        var admin = await factory.CreateLoginUserAsync();
        var target = await factory.CreateLoginUserAsync();
        var other = await factory.CreateLoginUserAsync();

        await GrantAsync(factory, admin,
            UAuthActions.Authorization.Roles.RemoveAdmin);

        var role = await SeedRoleAsync(factory);

        await SeedAssignmentAsync(factory, target.UserKey, role.Id);
        await SeedAssignmentAsync(factory, other.UserKey, role.Id);

        using var client = await AuthenticatedClientAsync(factory, admin);

        using var response = await client.PostAsJsonAsync(
            UserRolesUrl(target.UserKey, "remove"),
            new RemoveRoleRequest
            {
                UserKey = other.UserKey,
                RoleName = role.Name
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        (await HasRoleAsync(factory, target.UserKey, role.Id))
            .Should().BeTrue();

        (await HasRoleAsync(factory, other.UserKey, role.Id))
            .Should().BeTrue();
    }

    [Fact]
    public async Task RenameRole_TargetMismatch_ShouldRejectBothTargets()
    {
        using var factory = NewFactory();

        var admin = await factory.CreateLoginUserAsync();

        await GrantAsync(factory, admin,
            UAuthActions.Authorization.Roles.RenameAdmin);

        var target = await SeedRoleAsync(factory);
        var other = await SeedRoleAsync(factory);

        using var client = await AuthenticatedClientAsync(factory, admin);

        using var response = await client.PostAsJsonAsync(
            RoleUrl(target.Id, "rename"),
            new RenameRoleRequest
            {
                Id = other.Id,
                Name = "unexpected-name"
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        (await ReadRoleAsync(factory, target.Id))!
            .Name.Should().Be(target.Name);

        (await ReadRoleAsync(factory, other.Id))!
            .Name.Should().Be(other.Name);
    }

    [Fact]
    public async Task SetPermissions_TargetMismatch_ShouldNotModifyEitherRole()
    {
        using var factory = NewFactory();

        var admin = await factory.CreateLoginUserAsync();

        await GrantAsync(factory, admin,
            UAuthActions.Authorization.Roles.SetPermissionsAdmin);

        var target = await SeedRoleAsync(factory);
        var other = await SeedRoleAsync(factory);

        using var client = await AuthenticatedClientAsync(factory, admin);

        using var response = await client.PostAsJsonAsync(
            RoleUrl(target.Id, "permissions"),
            new SetRolePermissionsRequest
            {
                RoleId = other.Id,
                Permissions = [Permission.From("test.resource.write")]
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        (await ReadRoleAsync(factory, target.Id))!
            .Permissions.Should().BeEmpty();

        (await ReadRoleAsync(factory, other.Id))!
            .Permissions.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteRole_TargetMismatch_ShouldPreserveBothRoles()
    {
        using var factory = NewFactory();

        var admin = await factory.CreateLoginUserAsync();

        await GrantAsync(factory, admin,
            UAuthActions.Authorization.Roles.DeleteAdmin);

        var target = await SeedRoleAsync(factory);
        var other = await SeedRoleAsync(factory);

        using var client = await AuthenticatedClientAsync(factory, admin);

        using var response = await client.PostAsJsonAsync(
            RoleUrl(target.Id, "delete"),
            new DeleteRoleRequest
            {
                Id = other.Id
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        (await ReadRoleAsync(factory, target.Id))
            .Should().NotBeNull();

        (await ReadRoleAsync(factory, other.Id))
            .Should().NotBeNull();
    }

    // --------------------------------------------------
    // Role management - successful operations
    // --------------------------------------------------

    [Fact]
    public async Task CreateRole_ValidRequest_ShouldPersistRole()
    {
        using var factory = NewFactory();

        var admin = await factory.CreateLoginUserAsync();

        await GrantAsync(factory, admin,
            UAuthActions.Authorization.Roles.CreateAdmin);

        var name = UniqueRoleName();
        var permission = Permission.From("test.resource.read");

        using var client = await AuthenticatedClientAsync(factory, admin);

        using var response = await client.PostAsJsonAsync(
            $"{AdminBase}/roles/create",
            new CreateRoleRequest
            {
                Name = name,
                Permissions = [permission]
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var stored = await FindRoleByNameAsync(factory, name);

        stored.Should().NotBeNull();
        stored!.Name.Should().Be(name);
        stored.Permissions.Should().Contain(permission);
    }

    [Fact]
    public async Task RenameRole_ValidRequest_ShouldPersistNewName()
    {
        using var factory = NewFactory();

        var admin = await factory.CreateLoginUserAsync();

        await GrantAsync(factory, admin,
            UAuthActions.Authorization.Roles.RenameAdmin);

        var role = await SeedRoleAsync(factory);
        var newName = UniqueRoleName();

        using var client = await AuthenticatedClientAsync(factory, admin);

        using var response = await client.PostAsJsonAsync(
            RoleUrl(role.Id, "rename"),
            new RenameRoleRequest
            {
                Id = role.Id,
                Name = newName
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var stored = await ReadRoleAsync(factory, role.Id);

        stored.Should().NotBeNull();
        stored!.Name.Should().Be(newName);
        stored.NormalizedName.Should()
            .Be(newName.ToUpperInvariant());
    }

    [Fact]
    public async Task SetPermissions_ValidRequest_ShouldReplacePermissions()
    {
        using var factory = NewFactory();

        var admin = await factory.CreateLoginUserAsync();

        await GrantAsync(
            factory,
            admin,
            UAuthActions.Authorization.Roles.SetPermissionsAdmin);

        var oldPermission = Permission.From("test.old.read");

        var newPermission = Permission.From(
            UAuthActions.Authorization.Roles.AssignAdmin);

        var role = await SeedRoleAsync(factory, [oldPermission]);

        using var client = await AuthenticatedClientAsync(factory, admin);

        using var response = await client.PostAsJsonAsync(
            RoleUrl(role.Id, "permissions"),
            new SetRolePermissionsRequest
            {
                RoleId = role.Id,
                Permissions = [newPermission]
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var stored = await ReadRoleAsync(factory, role.Id);

        stored.Should().NotBeNull();

        stored!.Permissions.Should().NotBeEmpty();

        stored.Permissions.Should().NotContain(oldPermission);

        var normalized = PermissionNormalizer.Normalize(
            [newPermission],
            UAuthPermissionCatalog.GetAdminPermissions());

        stored.Permissions.Should().BeEquivalentTo(normalized);
    }

    [Fact]
    public async Task AssignRole_ValidRequest_ShouldPersistAssignment()
    {
        using var factory = NewFactory();

        var admin = await factory.CreateLoginUserAsync();
        var target = await factory.CreateLoginUserAsync();

        await GrantAsync(factory, admin,
            UAuthActions.Authorization.Roles.AssignAdmin);

        var role = await SeedRoleAsync(factory);

        using var client = await AuthenticatedClientAsync(factory, admin);

        using var response = await client.PostAsJsonAsync(
            UserRolesUrl(target.UserKey, "assign"),
            new AssignRoleRequest
            {
                UserKey = target.UserKey,
                RoleName = role.Name
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        (await HasRoleAsync(factory, target.UserKey, role.Id))
            .Should().BeTrue();
    }

    [Fact]
    public async Task RemoveRole_ValidRequest_ShouldRemoveAssignment()
    {
        using var factory = NewFactory();

        var admin = await factory.CreateLoginUserAsync();
        var target = await factory.CreateLoginUserAsync();

        await GrantAsync(factory, admin,
            UAuthActions.Authorization.Roles.RemoveAdmin);

        var role = await SeedRoleAsync(factory);

        await SeedAssignmentAsync(factory, target.UserKey, role.Id);

        using var client = await AuthenticatedClientAsync(factory, admin);

        using var response = await client.PostAsJsonAsync(
            UserRolesUrl(target.UserKey, "remove"),
            new RemoveRoleRequest
            {
                UserKey = target.UserKey,
                RoleName = role.Name
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        (await HasRoleAsync(factory, target.UserKey, role.Id))
            .Should().BeFalse();
    }

    [Fact]
    public async Task DeleteRole_ValidRequest_ShouldRemoveAssignments()
    {
        using var factory = NewFactory();

        var admin = await factory.CreateLoginUserAsync();
        var target = await factory.CreateLoginUserAsync();

        await GrantAsync(factory, admin,
            UAuthActions.Authorization.Roles.DeleteAdmin);

        var role = await SeedRoleAsync(factory);

        await SeedAssignmentAsync(factory, target.UserKey, role.Id);

        using var client = await AuthenticatedClientAsync(factory, admin);

        using var response = await client.PostAsJsonAsync(
            RoleUrl(role.Id, "delete"),
            new DeleteRoleRequest
            {
                Id = role.Id,
                Mode = default
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        (await HasRoleAsync(factory, target.UserKey, role.Id))
            .Should().BeFalse();

        var stored = await ReadRoleAsync(factory, role.Id);

        // Soft delete leaves the entity marked as deleted.
        // Hard delete may remove it entirely.
        (stored is null || stored.IsDeleted)
            .Should().BeTrue();
    }

    // --------------------------------------------------
    // Queries
    // --------------------------------------------------

    [Fact]
    public async Task GetUserRoles_ShouldReturnAssignedRole()
    {
        using var factory = NewFactory();

        var admin = await factory.CreateLoginUserAsync();
        var target = await factory.CreateLoginUserAsync();

        await GrantAsync(factory, admin,
            UAuthActions.Authorization.Roles.GetAdmin);

        var role = await SeedRoleAsync(factory);

        await SeedAssignmentAsync(factory, target.UserKey, role.Id);

        using var client = await AuthenticatedClientAsync(factory, admin);

        using var response = await client.PostAsJsonAsync(
            UserRolesUrl(target.UserKey, "get"),
            new RoleQuery());

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content
            .ReadFromJsonAsync<UserRolesResponse>();

        result.Should().NotBeNull();
        result!.UserKey.Should().Be(target.UserKey);
        result.Roles.Items.Should()
            .Contain(x => x.RoleId == role.Id);
    }

    [Fact]
    public async Task GetMyRoles_ShouldReturnOwnRole()
    {
        using var factory = NewFactory();

        var user = await factory.CreateLoginUserAsync();
        var role = await SeedRoleAsync(factory);

        await SeedAssignmentAsync(factory, user.UserKey, role.Id);

        using var client = await AuthenticatedClientAsync(factory, user);

        using var response = await client.PostAsJsonAsync(
            $"{SelfBase}/roles/get",
            new RoleQuery());

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content
            .ReadFromJsonAsync<UserRolesResponse>();

        result.Should().NotBeNull();
        result!.UserKey.Should().Be(user.UserKey);
        result.Roles.Items.Should()
            .Contain(x => x.RoleId == role.Id);
    }

    [Fact]
    public async Task QueryRoles_ShouldReturnCreatedRole()
    {
        using var factory = NewFactory();

        var admin = await factory.CreateLoginUserAsync();

        await GrantAsync(
            factory,
            admin,
            UAuthActions.Authorization.Roles.QueryAdmin);

        var role = await SeedRoleAsync(factory);

        using var client = await AuthenticatedClientAsync(factory, admin);

        using var response = await client.PostAsJsonAsync(
            $"{AdminBase}/roles/query",
            new RoleQuery());

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content
            .ReadFromJsonAsync<PagedResult<RoleInfo>>();

        result.Should().NotBeNull();

        result!.Items.Should().ContainSingle(
            x => x.Id == role.Id && x.Name == role.Name);
    }

    // --------------------------------------------------
    // Authentication and authorization
    // --------------------------------------------------

    [Fact]
    public async Task AssignRole_Anonymous_ShouldReject()
    {
        using var factory = NewFactory();

        var target = await factory.CreateLoginUserAsync();
        var role = await SeedRoleAsync(factory);

        using var client = CreateClient(factory);

        using var response = await client.PostAsJsonAsync(
            UserRolesUrl(target.UserKey, "assign"),
            new AssignRoleRequest
            {
                UserKey = target.UserKey,
                RoleName = role.Name
            });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        (await HasRoleAsync(factory, target.UserKey, role.Id))
            .Should().BeFalse();
    }

    [Fact]
    public async Task AssignRole_WithoutPermission_ShouldNotMutate()
    {
        using var factory = NewFactory();

        var actor = await factory.CreateLoginUserAsync();
        var target = await factory.CreateLoginUserAsync();
        var role = await SeedRoleAsync(factory);

        using var client = await AuthenticatedClientAsync(factory, actor);

        using var response = await client.PostAsJsonAsync(
            UserRolesUrl(target.UserKey, "assign"),
            new AssignRoleRequest
            {
                UserKey = target.UserKey,
                RoleName = role.Name
            });

        response.IsSuccessStatusCode.Should().BeFalse();

        (await HasRoleAsync(factory, target.UserKey, role.Id))
            .Should().BeFalse();
    }

    [Fact]
    public async Task CreateRole_WithoutPermission_ShouldNotCreateRole()
    {
        using var factory = NewFactory();

        var actor = await factory.CreateLoginUserAsync();
        var name = UniqueRoleName();

        using var client = await AuthenticatedClientAsync(factory, actor);

        using var response = await client.PostAsJsonAsync(
            $"{AdminBase}/roles/create",
            new CreateRoleRequest
            {
                Name = name,
                Permissions = []
            });

        response.IsSuccessStatusCode.Should().BeFalse();

        (await FindRoleByNameAsync(factory, name))
            .Should().BeNull();
    }

    [Fact]
    public async Task AuthorizationCheck_WithPermission_ShouldAllow()
    {
        using var factory = NewFactory();

        var user = await factory.CreateLoginUserAsync();

        await GrantAsync(
            factory,
            user,
            UAuthActions.Authorization.Roles.QueryAdmin);

        using var client = await AuthenticatedClientAsync(factory, user);

        using var response = await client.PostAsJsonAsync(
            $"{SelfBase}/check",
            new AuthorizationCheckRequest
            {
                Action = UAuthActions.Authorization.Roles.QueryAdmin,
                Resource = "authorization.roles"
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content
            .ReadFromJsonAsync<AuthorizationResult>();

        result.Should().NotBeNull();
        result!.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task AuthorizationCheck_WithoutPermission_ShouldDeny()
    {
        using var factory = NewFactory();

        var user = await factory.CreateLoginUserAsync();

        using var client = await AuthenticatedClientAsync(factory, user);

        using var response = await client.PostAsJsonAsync(
            $"{SelfBase}/check",
            new AuthorizationCheckRequest
            {
                Action = UAuthActions.Authorization.Roles.QueryAdmin,
                Resource = "authorization.roles"
            });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AssignRole_ServiceTargetMismatch_ShouldReject()
    {
        using var factory = NewFactory();

        var admin = await factory.CreateLoginUserAsync();
        var authorizedTarget = await factory.CreateLoginUserAsync();
        var other = await factory.CreateLoginUserAsync();

        await GrantAsync(
            factory,
            admin,
            UAuthActions.Authorization.Roles.AssignAdmin);

        var role = await SeedRoleAsync(factory);

        var context = TestAccessContext.ForTargetUser(
            admin.UserKey,
            authorizedTarget.UserKey,
            UAuthActions.Authorization.Roles.AssignAdmin,
            resource: "authorization.roles");

        using var scope = factory.Services.CreateScope();

        var service = scope.ServiceProvider
            .GetRequiredService<IUserRoleService>();

        Func<Task> act = () => service.AssignAsync(
            context,
            other.UserKey,
            role.Name);

        await act.Should().ThrowAsync<UAuthAuthorizationException>();

        (await HasRoleAsync(factory, authorizedTarget.UserKey, role.Id))
            .Should().BeFalse();

        (await HasRoleAsync(factory, other.UserKey, role.Id))
            .Should().BeFalse();
    }

    [Fact]
    public async Task RemoveRole_ServiceTargetMismatch_ShouldReject()
    {
        using var factory = NewFactory();

        var admin = await factory.CreateLoginUserAsync();
        var authorizedTarget = await factory.CreateLoginUserAsync();
        var other = await factory.CreateLoginUserAsync();

        await GrantAsync(
            factory,
            admin,
            UAuthActions.Authorization.Roles.RemoveAdmin);

        var role = await SeedRoleAsync(factory);

        await SeedAssignmentAsync(
            factory,
            authorizedTarget.UserKey,
            role.Id);

        await SeedAssignmentAsync(
            factory,
            other.UserKey,
            role.Id);

        var context = TestAccessContext.ForTargetUser(
            admin.UserKey,
            authorizedTarget.UserKey,
            UAuthActions.Authorization.Roles.RemoveAdmin,
            resource: "authorization.roles");

        using var scope = factory.Services.CreateScope();

        var service = scope.ServiceProvider
            .GetRequiredService<IUserRoleService>();

        Func<Task> act = () => service.RemoveAsync(
            context,
            other.UserKey,
            role.Name);

        await act.Should().ThrowAsync<UAuthAuthorizationException>();

        (await HasRoleAsync(factory, authorizedTarget.UserKey, role.Id))
            .Should().BeTrue();

        (await HasRoleAsync(factory, other.UserKey, role.Id))
            .Should().BeTrue();
    }

    [Fact]
    public async Task RoleStore_ShouldIsolateRolesBetweenTenants()
    {
        using var factory = NewFactory();

        var tenantA = TenantKeys.Single;
        var tenantB = TenantKey.FromExternal("integration-tenant-b");

        using var scope = factory.Services.CreateScope();

        var storeFactory = scope.ServiceProvider
            .GetRequiredService<IRoleStoreFactory>();

        var role = Role.Create(
            id: null,
            tenant: tenantA,
            name: UniqueRoleName(),
            permissions: [],
            now: factory.Clock.UtcNow);

        await storeFactory.Create(tenantA).AddAsync(role);

        var fromA = await storeFactory.Create(tenantA)
            .GetAsync(new RoleKey(tenantA, role.Id));

        var fromB = await storeFactory.Create(tenantB)
            .GetAsync(new RoleKey(tenantB, role.Id));

        fromA.Should().NotBeNull();
        fromB.Should().BeNull();
    }

    // --------------------------------------------------
    // Test infrastructure
    // --------------------------------------------------

    private static AuthServerFactory NewFactory()
    {
        var factory = new AuthServerFactory();
        factory.Clock.Reset();
        return factory;
    }

    private static string UniqueRoleName()
        => $"role-{Guid.NewGuid():N}";

    private static string RoleUrl(RoleId id, string operation)
        => $"{AdminBase}/roles/{id.Value}/{operation}";

    private static string UserRolesUrl(
        UserKey userKey,
        string operation)
        => $"{AdminBase}/users/{userKey.Value}/roles/{operation}";

    private static async Task GrantAsync(
        AuthServerFactory factory,
        IntegrationTestUser user,
        params string[] permissions)
    {
        await factory.GrantPermissionsAsync(
            user.UserKey,
            permissions);
    }

    private static HttpClient CreateClient(AuthServerFactory factory)
    {
        var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = false
            });

        client.DefaultRequestHeaders.Add(
            "Origin",
            "https://localhost:6130");

        client.DefaultRequestHeaders.Add(
            "X-UDID",
            $"authorization-test-{Guid.NewGuid():N}");

        return client;
    }

    private static async Task<HttpClient> AuthenticatedClientAsync(
        AuthServerFactory factory,
        IntegrationTestUser user)
    {
        var client = CreateClient(factory);

        using var response = await client.PostAsJsonAsync(
            "/auth/login",
            new
            {
                identifier = user.Identifier,
                secret = user.Secret
            });

        response.StatusCode.Should().Be(HttpStatusCode.Found);

        response.Headers.TryGetValues("Set-Cookie", out var values)
            .Should().BeTrue();

        var cookie = values!
            .First(x => x.StartsWith(
                "uas=",
                StringComparison.OrdinalIgnoreCase))
            .Split(';', 2)[0];

        client.DefaultRequestHeaders.Add("Cookie", cookie);

        return client;
    }

    private static async Task<Role> SeedRoleAsync(
        AuthServerFactory factory,
        IEnumerable<Permission>? permissions = null)
    {
        using var scope = factory.Services.CreateScope();

        var storeFactory = scope.ServiceProvider
            .GetRequiredService<IRoleStoreFactory>();

        var role = Role.Create(
            id: null,
            tenant: TenantKeys.Single,
            name: UniqueRoleName(),
            permissions: permissions,
            now: factory.Clock.UtcNow);

        await storeFactory
            .Create(TenantKeys.Single)
            .AddAsync(role);

        return role;
    }

    private static async Task<Role?> ReadRoleAsync(
        AuthServerFactory factory,
        RoleId roleId)
    {
        using var scope = factory.Services.CreateScope();

        var storeFactory = scope.ServiceProvider
            .GetRequiredService<IRoleStoreFactory>();

        return await storeFactory
            .Create(TenantKeys.Single)
            .GetAsync(new RoleKey(TenantKeys.Single, roleId));
    }

    private static async Task<Role?> FindRoleByNameAsync(
        AuthServerFactory factory,
        string name)
    {
        using var scope = factory.Services.CreateScope();

        var storeFactory = scope.ServiceProvider
            .GetRequiredService<IRoleStoreFactory>();

        return await storeFactory
            .Create(TenantKeys.Single)
            .GetByNameAsync(name.ToUpperInvariant());
    }

    private static async Task SeedAssignmentAsync(
        AuthServerFactory factory,
        UserKey userKey,
        RoleId roleId)
    {
        using var scope = factory.Services.CreateScope();

        var storeFactory = scope.ServiceProvider
            .GetRequiredService<IUserRoleStoreFactory>();

        await storeFactory
            .Create(TenantKeys.Single)
            .AssignAsync(userKey, roleId, factory.Clock.UtcNow);
    }

    private static async Task<bool> HasRoleAsync(
        AuthServerFactory factory,
        UserKey userKey,
        RoleId roleId)
    {
        using var scope = factory.Services.CreateScope();

        var storeFactory = scope.ServiceProvider
            .GetRequiredService<IUserRoleStoreFactory>();

        var assignments = await storeFactory
            .Create(TenantKeys.Single)
            .GetAssignmentsAsync(userKey);

        return assignments.Any(x => x.RoleId == roleId);
    }
}
