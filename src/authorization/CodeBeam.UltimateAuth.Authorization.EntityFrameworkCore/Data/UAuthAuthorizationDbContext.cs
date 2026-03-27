using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.Authorization.EntityFrameworkCore;

public sealed class UAuthAuthorizationDbContext : DbContext
{
    public DbSet<RoleProjection> Roles => Set<RoleProjection>();
    public DbSet<RolePermissionProjection> RolePermissions => Set<RolePermissionProjection>();
    public DbSet<UserRoleProjection> UserRoles => Set<UserRoleProjection>();

    public UAuthAuthorizationDbContext(DbContextOptions<UAuthAuthorizationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        UAuthAuthorizationModelBuilder.Configure(modelBuilder);
    }
}
