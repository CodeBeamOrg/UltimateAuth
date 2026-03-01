using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.Sessions.EntityFrameworkCore;

internal sealed class UltimateAuthSessionDbContext : DbContext
{
    public DbSet<SessionRootProjection> Roots => Set<SessionRootProjection>();
    public DbSet<SessionChainProjection> Chains => Set<SessionChainProjection>();
    public DbSet<SessionProjection> Sessions => Set<SessionProjection>();


    private readonly TenantContext _tenant;

    public UltimateAuthSessionDbContext(DbContextOptions options, TenantContext tenant) : base(options)
    {
        _tenant = tenant;
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<SessionProjection>()
            .HasQueryFilter(x => _tenant.IsGlobal || x.Tenant == _tenant.Tenant);

        b.Entity<SessionChainProjection>()
            .HasQueryFilter(x => _tenant.IsGlobal || x.Tenant == _tenant.Tenant);

        b.Entity<SessionRootProjection>()
            .HasQueryFilter(x => _tenant.IsGlobal || x.Tenant == _tenant.Tenant);

        b.Entity<SessionRootProjection>(e =>
        {
            e.HasKey(x => x.Id);

            e.Property(x => x.Version).IsConcurrencyToken();

            e.Property(x => x.UserKey)
                .IsRequired();

            e.HasIndex(x => new { x.Tenant, x.UserKey }).IsUnique();
            e.HasIndex(x => new { x.Tenant, x.RootId }).IsUnique();

            e.Property(x => x.SecurityVersion)
                .IsRequired();

            e.Property(x => x.RootId)
                .HasConversion(
                    v => v.Value,
                    v => SessionRootId.From(v))
                .IsRequired();
        });

        b.Entity<SessionChainProjection>(e =>
        {
            e.HasKey(x => x.Id);

            e.Property(x => x.Version).IsConcurrencyToken();

            e.Property(x => x.UserKey)
                .IsRequired();

            e.HasIndex(x => new { x.Tenant, x.ChainId }).IsUnique();

            e.Property(x => x.ChainId)
                .HasConversion(
                    v => v.Value,
                    v => SessionChainId.From(v))
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
            e.HasKey(x => x.Id);
            e.Property(x => x.Version).IsConcurrencyToken();

            e.HasIndex(x => new { x.Tenant, x.SessionId }).IsUnique();
            e.HasIndex(x => new { x.Tenant, x.ChainId, x.RevokedAt });

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
