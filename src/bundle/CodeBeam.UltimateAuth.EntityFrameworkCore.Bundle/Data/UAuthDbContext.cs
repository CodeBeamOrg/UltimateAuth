using CodeBeam.UltimateAuth.Authentication.EntityFrameworkCore;
using CodeBeam.UltimateAuth.Authorization.EntityFrameworkCore;
using CodeBeam.UltimateAuth.Credentials.EntityFrameworkCore;
using CodeBeam.UltimateAuth.Sessions.EntityFrameworkCore;
using CodeBeam.UltimateAuth.Tokens.EntityFrameworkCore;
using CodeBeam.UltimateAuth.Users.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.EntityFrameworkCore;

public sealed class UAuthDbContext : DbContext
{
    public UAuthDbContext(DbContextOptions<UAuthDbContext> options)
        : base(options)
    {
    }

    // Users
    public DbSet<UserLifecycleProjection> UserLifecycles => Set<UserLifecycleProjection>();
    public DbSet<UserProfileProjection> UserProfiles => Set<UserProfileProjection>();
    public DbSet<UserIdentifierProjection> UserIdentifiers => Set<UserIdentifierProjection>();

    // Credentials
    public DbSet<PasswordCredentialProjection> PasswordCredentials => Set<PasswordCredentialProjection>();

    // Authorization
    public DbSet<RoleProjection> Roles => Set<RoleProjection>();
    public DbSet<RolePermissionProjection> UserRoleAssignments => Set<RolePermissionProjection>();
    public DbSet<UserRoleProjection> UserPermissions => Set<UserRoleProjection>();

    // Sessions
    public DbSet<SessionRootProjection> Roots => Set<SessionRootProjection>();
    public DbSet<SessionChainProjection> Chains => Set<SessionChainProjection>();
    public DbSet<SessionProjection> Sessions => Set<SessionProjection>();

    // Tokens
    public DbSet<RefreshTokenProjection> RefreshTokens => Set<RefreshTokenProjection>();

    // Authentication
    public DbSet<AuthenticationSecurityStateProjection> AuthenticationSecurityStates => Set<AuthenticationSecurityStateProjection>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        UAuthSessionsModelBuilder.Configure(modelBuilder);
        UAuthTokensModelBuilder.Configure(modelBuilder);
        UAuthAuthenticationModelBuilder.Configure(modelBuilder);
        UAuthUsersModelBuilder.Configure(modelBuilder);
        UAuthCredentialsModelBuilder.Configure(modelBuilder);
        UAuthAuthorizationModelBuilder.Configure(modelBuilder);
    }
}
