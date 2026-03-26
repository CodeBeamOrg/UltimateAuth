using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.Sessions.EntityFrameworkCore;

public sealed class UAuthSessionDbContext : DbContext
{
    public DbSet<SessionRootProjection> Roots => Set<SessionRootProjection>();
    public DbSet<SessionChainProjection> Chains => Set<SessionChainProjection>();
    public DbSet<SessionProjection> Sessions => Set<SessionProjection>();


    public UAuthSessionDbContext(DbContextOptions<UAuthSessionDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<SessionRootProjection>(e =>
        {
            e.ToTable("UAuth_SessionRoots");
            e.HasKey(x => x.Id);

            e.Property(x => x.Version).IsConcurrencyToken();
            e.Property(x => x.CreatedAt).IsRequired();

            e.Property(x => x.UserKey)
                .HasConversion(
                    v => v.Value,
                    v => UserKey.FromString(v))
                .HasMaxLength(128)
                .IsRequired();
            e.Property(x => x.Tenant)
                .HasConversion(
                    v => v.Value,
                    v => TenantKey.FromInternal(v))
                .HasMaxLength(128)
                .IsRequired();

            e.HasIndex(x => new { x.Tenant, x.UserKey }).IsUnique();
            e.HasIndex(x => new { x.Tenant, x.RootId }).IsUnique();

            e.Property(x => x.SecurityVersion)
                .IsRequired();

            e.Property(x => x.RootId)
                .HasConversion(
                    v => v.Value,
                    v => SessionRootId.From(v))
                .HasMaxLength(128)
                .IsRequired();
        });

        b.Entity<SessionChainProjection>(e =>
        {
            e.ToTable("UAuth_SessionChains");
            e.HasKey(x => x.Id);

            e.Property(x => x.Version).IsConcurrencyToken();
            e.Property(x => x.CreatedAt).IsRequired();

            e.Property(x => x.UserKey)
                .HasConversion(
                    v => v.Value,
                    v => UserKey.FromString(v))
                .HasMaxLength(128)
                .IsRequired();
            e.Property(x => x.Tenant)
                .HasConversion(
                    v => v.Value,
                    v => TenantKey.FromInternal(v))
                .HasMaxLength(128)
                .IsRequired();

            e.HasIndex(x => new { x.Tenant, x.ChainId }).IsUnique();
            e.HasIndex(x => new { x.Tenant, x.UserKey });
            e.HasIndex(x => new { x.Tenant, x.UserKey, x.DeviceId });
            e.HasIndex(x => new { x.Tenant, x.RootId });

            e.HasOne<SessionRootProjection>()
                .WithMany()
                .HasForeignKey(x => new { x.Tenant, x.RootId })
                .HasPrincipalKey(x => new { x.Tenant, x.RootId })
                .OnDelete(DeleteBehavior.Restrict);

            e.Property(x => x.ChainId)
                .HasConversion(
                    v => v.Value,
                    v => SessionChainId.From(v))
                .IsRequired();

            e.Property(x => x.DeviceId)
                .HasConversion(
                    v => v.Value,
                    v => DeviceId.Create(v))
                .HasMaxLength(64)
                .IsRequired();

            e.Property(x => x.Device)
                .HasConversion(new JsonValueConverter<DeviceContext>())
                .IsRequired();

            e.Property(x => x.ActiveSessionId)
                .HasConversion(new NullableAuthSessionIdConverter());

            e.Property(x => x.ClaimsSnapshot)
                .HasConversion(new JsonValueConverter<ClaimsSnapshot>())
                .IsRequired();

            e.Property(x => x.SecurityVersionAtCreation)
                .IsRequired();
        });

        b.Entity<SessionProjection>(e =>
        {
            e.ToTable("UAuth_Sessions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Version).IsConcurrencyToken();
            e.Property(x => x.CreatedAt).IsRequired();

            e.Property(x => x.UserKey)
                .HasConversion(
                    v => v.Value,
                    v => UserKey.FromString(v))
                .HasMaxLength(128)
                .IsRequired();
            e.Property(x => x.Tenant)
                .HasConversion(
                    v => v.Value,
                    v => TenantKey.FromInternal(v))
                .HasMaxLength(128)
                .IsRequired();

            e.HasIndex(x => new { x.Tenant, x.SessionId }).IsUnique();
            e.HasIndex(x => new { x.Tenant, x.ChainId });
            e.HasIndex(x => new { x.Tenant, x.ChainId, x.RevokedAt });
            e.HasIndex(x => new { x.Tenant, x.UserKey, x.RevokedAt });
            e.HasIndex(x => new { x.Tenant, x.ExpiresAt });
            e.HasIndex(x => new { x.Tenant, x.RevokedAt });

            e.HasOne<SessionChainProjection>()
                .WithMany()
                .HasForeignKey(x => new { x.Tenant, x.ChainId })
                .HasPrincipalKey(x => new { x.Tenant, x.ChainId })
                .OnDelete(DeleteBehavior.Restrict);

            e.Property(x => x.SessionId)
                .HasConversion(new AuthSessionIdConverter())
                .IsRequired();

            e.Property(x => x.ChainId)
                .HasConversion(
                    v => v.Value,
                    v => SessionChainId.From(v))
                .IsRequired();

            e.Property(x => x.Device)
                .HasConversion(new JsonValueConverter<DeviceContext>())
                .IsRequired();

            e.Property(x => x.Claims)
                .HasConversion(new JsonValueConverter<ClaimsSnapshot>())
                .IsRequired();

            e.Property(x => x.Metadata)
                .HasConversion(new JsonValueConverter<SessionMetadata>())
                .IsRequired();
        });
    }

}
