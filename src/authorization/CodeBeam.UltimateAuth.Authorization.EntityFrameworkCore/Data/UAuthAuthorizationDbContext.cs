using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.Authorization.EntityFrameworkCore;

internal sealed class UAuthAuthorizationDbContext : DbContext
{
    public DbSet<RoleProjection> Roles => Set<RoleProjection>();
    public DbSet<RolePermissionProjection> RolePermissions => Set<RolePermissionProjection>();
    public DbSet<UserRoleProjection> UserRoles => Set<UserRoleProjection>();

    public UAuthAuthorizationDbContext(DbContextOptions<UAuthAuthorizationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        ConfigureRole(b);
        ConfigureRolePermission(b);
        ConfigureUserRole(b);
    }

    private void ConfigureRole(ModelBuilder b)
    {
        b.Entity<RoleProjection>(e =>
        {
            e.ToTable("UAuth_Roles");
            e.HasKey(x => x.Id);

            e.Property(x => x.Version)
                .IsConcurrencyToken();

            e.Property(x => x.Tenant)
                .HasConversion(
                    v => v.Value,
                    v => TenantKey.FromInternal(v))
                .HasMaxLength(128)
                .IsRequired();

            e.Property(x => x.Id)
                .HasConversion(
                    v => v.Value,
                    v => RoleId.From(v))
                .IsRequired();

            e.Property(x => x.Name)
                .HasMaxLength(128)
                .IsRequired();

            e.Property(x => x.NormalizedName)
                .HasMaxLength(128)
                .IsRequired();

            e.Property(x => x.CreatedAt)
                .HasConversion(
                    v => v.UtcDateTime,
                    v => new DateTimeOffset(v, TimeSpan.Zero));

            e.HasIndex(x => new { x.Tenant, x.Id }).IsUnique();
            e.HasIndex(x => new { x.Tenant, x.NormalizedName }).IsUnique();
        });
    }

    private void ConfigureRolePermission(ModelBuilder b)
    {
        b.Entity<RolePermissionProjection>(e =>
        {
            e.ToTable("UAuth_RolePermissions");
            e.HasKey(x => new { x.Tenant, x.RoleId, x.Permission });

            e.Property(x => x.Tenant)
                .HasConversion(
                    v => v.Value,
                    v => TenantKey.FromInternal(v))
                .HasMaxLength(128)
                .IsRequired();

            e.Property(x => x.RoleId)
                .HasConversion(
                    v => v.Value,
                    v => RoleId.From(v))
                .IsRequired();

            e.Property(x => x.Permission)
                .HasMaxLength(256)
                .IsRequired();

            e.HasIndex(x => new { x.Tenant, x.RoleId });
            e.HasIndex(x => new { x.Tenant, x.Permission });
        });
    }

    private void ConfigureUserRole(ModelBuilder b)
    {
        b.Entity<UserRoleProjection>(e =>
        {
            e.ToTable("UAuth_UserRoles");
            e.HasKey(x => new { x.Tenant, x.UserKey, x.RoleId });

            e.Property(x => x.Tenant)
                .HasConversion(
                    v => v.Value,
                    v => TenantKey.FromInternal(v))
                .HasMaxLength(128)
                .IsRequired();

            e.Property(x => x.UserKey)
                .HasConversion(
                    v => v.Value,
                    v => UserKey.FromString(v))
                .HasMaxLength(128)
                .IsRequired();

            e.Property(x => x.RoleId)
                .HasConversion(
                    v => v.Value,
                    v => RoleId.From(v))
                .IsRequired();

            e.Property(x => x.AssignedAt)
                .IsRequired();

            e.HasIndex(x => new { x.Tenant, x.UserKey });
            e.HasIndex(x => new { x.Tenant, x.RoleId });
        });
    }
}