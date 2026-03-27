using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.Authorization.EntityFrameworkCore;

public static class UAuthAuthorizationModelBuilder
{
    public static void Configure(ModelBuilder b)
    {
        ConfigureRoles(b);
        ConfigureRolePermissions(b);
        ConfigureUserRoles(b);
    }

    private static void ConfigureRoles(ModelBuilder b)
    {
        b.Entity<RoleProjection>(e =>
        {
            e.ToTable("UAuth_Roles");

            e.HasKey(x => x.Id);

            e.Property(x => x.Version)
                .IsConcurrencyToken();

            e.Property(x => x.Tenant)
                .HasConversion(v => v.Value, v => TenantKey.FromInternal(v))
                .HasMaxLength(128)
                .IsRequired();

            e.Property(x => x.Id)
                .HasConversion(v => v.Value, v => RoleId.From(v))
                .IsRequired();

            e.Property(x => x.Name)
                .HasMaxLength(128)
                .IsRequired();

            e.Property(x => x.NormalizedName)
                .HasMaxLength(128)
                .IsRequired();

            e.Property(x => x.CreatedAt).HasUtcDateTimeOffsetConverter().IsRequired();
            e.Property(x => x.UpdatedAt).HasNullableUtcDateTimeOffsetConverter();
            e.Property(x => x.DeletedAt).HasNullableUtcDateTimeOffsetConverter();

            e.HasIndex(x => new { x.Tenant, x.Id }).IsUnique();
            e.HasIndex(x => new { x.Tenant, x.NormalizedName }).IsUnique();
        });
    }

    private static void ConfigureRolePermissions(ModelBuilder b)
    {
        b.Entity<RolePermissionProjection>(e =>
        {
            e.ToTable("UAuth_RolePermissions");

            e.HasKey(x => new { x.Tenant, x.RoleId, x.Permission });

            e.Property(x => x.Tenant)
                .HasConversion(v => v.Value, v => TenantKey.FromInternal(v))
                .HasMaxLength(128)
                .IsRequired();

            e.Property(x => x.RoleId)
                .HasConversion(v => v.Value, v => RoleId.From(v))
                .IsRequired();

            e.Property(x => x.Permission)
                .HasMaxLength(256)
                .IsRequired();

            e.HasIndex(x => new { x.Tenant, x.RoleId });
            e.HasIndex(x => new { x.Tenant, x.Permission });
        });
    }

    private static void ConfigureUserRoles(ModelBuilder b)
    {
        b.Entity<UserRoleProjection>(e =>
        {
            e.ToTable("UAuth_UserRoles");

            e.HasKey(x => new { x.Tenant, x.UserKey, x.RoleId });

            e.Property(x => x.Tenant)
                .HasConversion(v => v.Value, v => TenantKey.FromInternal(v))
                .HasMaxLength(128)
                .IsRequired();

            e.Property(x => x.UserKey)
                .HasConversion(v => v.Value, v => UserKey.FromString(v))
                .HasMaxLength(128)
                .IsRequired();

            e.Property(x => x.RoleId)
                .HasConversion(v => v.Value, v => RoleId.From(v))
                .IsRequired();

            e.Property(x => x.AssignedAt).HasUtcDateTimeOffsetConverter().IsRequired();

            e.HasIndex(x => new { x.Tenant, x.UserKey });
            e.HasIndex(x => new { x.Tenant, x.RoleId });
        });
    }
}
