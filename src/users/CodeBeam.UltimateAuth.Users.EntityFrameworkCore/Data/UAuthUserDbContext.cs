using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.Users.EntityFrameworkCore;

internal sealed class UAuthUserDbContext : DbContext
{
    public DbSet<UserIdentifierProjection> Identifiers => Set<UserIdentifierProjection>();
    public DbSet<UserLifecycleProjection> Lifecycles => Set<UserLifecycleProjection>();
    public DbSet<UserProfileProjection> Profiles => Set<UserProfileProjection>();

    public UAuthUserDbContext(DbContextOptions<UAuthUserDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        ConfigureIdentifiers(b);
        ConfigureLifecycles(b);
        ConfigureProfiles(b);
    }

    private void ConfigureIdentifiers(ModelBuilder b)
    {
        b.Entity<UserIdentifierProjection>(e =>
        {
            e.ToTable("UAuth_UserIdentifiers");
            e.HasKey(x => x.Id);

            e.Property(x => x.Version)
                .IsConcurrencyToken();

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

            e.Property(x => x.Value)
                .HasMaxLength(256)
                .IsRequired();

            e.Property(x => x.NormalizedValue)
                .HasMaxLength(256)
                .IsRequired();

            e.HasIndex(x => new { x.Tenant, x.Type, x.NormalizedValue }).IsUnique();
            e.HasIndex(x => new { x.Tenant, x.UserKey });
            e.HasIndex(x => new { x.Tenant, x.UserKey, x.Type, x.IsPrimary });
            e.HasIndex(x => new { x.Tenant, x.UserKey, x.IsPrimary });
            e.HasIndex(x => new { x.Tenant, x.NormalizedValue });

            e.Property(x => x.CreatedAt)
                .IsRequired();
        });
    }

    private void ConfigureLifecycles(ModelBuilder b)
    {
        b.Entity<UserLifecycleProjection>(e =>
        {
            e.ToTable("UAuth_UserLifecycles");
            e.HasKey(x => x.Id);

            e.Property(x => x.Version)
                .IsConcurrencyToken();

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

            e.HasIndex(x => new { x.Tenant, x.UserKey }).IsUnique();

            e.Property(x => x.SecurityVersion)
                .IsRequired();

            e.Property(x => x.CreatedAt)
                .IsRequired();
        });
    }

    private void ConfigureProfiles(ModelBuilder b)
    {
        b.Entity<UserProfileProjection>(e =>
        {
            e.ToTable("UAuth_UserProfiles");
            e.HasKey(x => x.Id);

            e.Property(x => x.Version)
                .IsConcurrencyToken();

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

            e.HasIndex(x => new { x.Tenant, x.UserKey });

            e.Property(x => x.Metadata)
                .HasConversion(new NullableJsonValueConverter<Dictionary<string, string>>())
                .Metadata.SetValueComparer(JsonValueComparers.Create<DeviceContext>());

            e.Property(x => x.CreatedAt)
                .IsRequired();
        });
    }
}