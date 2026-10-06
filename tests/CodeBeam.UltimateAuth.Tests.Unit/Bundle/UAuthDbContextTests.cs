using CodeBeam.UltimateAuth.Authentication.EntityFrameworkCore;
using CodeBeam.UltimateAuth.Authorization.EntityFrameworkCore;
using CodeBeam.UltimateAuth.Credentials.EntityFrameworkCore;
using CodeBeam.UltimateAuth.EntityFrameworkCore;
using CodeBeam.UltimateAuth.Sessions.EntityFrameworkCore;
using CodeBeam.UltimateAuth.Tokens.EntityFrameworkCore;
using CodeBeam.UltimateAuth.Users.EntityFrameworkCore;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.Tests.Unit.EntityFrameworkCore;

public sealed class UAuthDbContextTests
{
    [Fact]
    public void Model_ShouldContainAllUltimateAuthProjectionTypes()
    {
        var options =
            new DbContextOptionsBuilder<UAuthDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

        using var context = new UAuthDbContext(options);

        var model = context.Model;

        model.FindEntityType(typeof(UserLifecycleProjection))
            .Should().NotBeNull();

        model.FindEntityType(typeof(UserProfileProjection))
            .Should().NotBeNull();

        model.FindEntityType(typeof(UserIdentifierProjection))
            .Should().NotBeNull();

        model.FindEntityType(typeof(PasswordCredentialProjection))
            .Should().NotBeNull();

        model.FindEntityType(typeof(RoleProjection))
            .Should().NotBeNull();

        model.FindEntityType(typeof(RolePermissionProjection))
            .Should().NotBeNull();

        model.FindEntityType(typeof(UserRoleProjection))
            .Should().NotBeNull();

        model.FindEntityType(typeof(SessionRootProjection))
            .Should().NotBeNull();

        model.FindEntityType(typeof(SessionChainProjection))
            .Should().NotBeNull();

        model.FindEntityType(typeof(SessionProjection))
            .Should().NotBeNull();

        model.FindEntityType(typeof(RefreshTokenProjection))
            .Should().NotBeNull();

        model.FindEntityType(typeof(AuthenticationSecurityStateProjection))
            .Should().NotBeNull();
    }

    [Fact]
    public void DbSets_ShouldBeAvailable()
    {
        var options =
            new DbContextOptionsBuilder<UAuthDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

        using var context = new UAuthDbContext(options);

        context.UserLifecycles.Should().NotBeNull();
        context.UserProfiles.Should().NotBeNull();
        context.UserIdentifiers.Should().NotBeNull();
        context.PasswordCredentials.Should().NotBeNull();

        context.Roles.Should().NotBeNull();
        context.UserRoleAssignments.Should().NotBeNull();
        context.UserPermissions.Should().NotBeNull();

        context.Roots.Should().NotBeNull();
        context.Chains.Should().NotBeNull();
        context.Sessions.Should().NotBeNull();

        context.RefreshTokens.Should().NotBeNull();
        context.AuthenticationSecurityStates.Should().NotBeNull();
    }
}
