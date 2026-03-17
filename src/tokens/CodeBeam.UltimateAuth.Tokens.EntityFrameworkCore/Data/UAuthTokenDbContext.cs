using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.Tokens.EntityFrameworkCore;

internal sealed class UltimateAuthTokenDbContext : DbContext
{
    public DbSet<RefreshTokenProjection> RefreshTokens => Set<RefreshTokenProjection>();
    public DbSet<RevokedTokenIdProjection> RevokedTokenIds => Set<RevokedTokenIdProjection>();

    public UltimateAuthTokenDbContext(DbContextOptions<UltimateAuthTokenDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<RefreshTokenProjection>(e =>
        {
            e.ToTable("UAuth_RefreshTokens");
            e.HasKey(x => x.Id);

            e.Property(x => x.Version)
                .IsConcurrencyToken();

            e.Property(x => x.Tenant)
                .HasConversion(
                    v => v.Value,
                    v => TenantKey.FromInternal(v))
                .HasMaxLength(128)
                .IsRequired();

            e.Property(x => x.TokenId)
                .HasConversion(
                    v => v.Value,
                    v => TokenId.From(v))
                .IsRequired();

            e.Property(x => x.TokenHash)
                .HasMaxLength(128)
                .IsRequired();

            e.HasIndex(x => new { x.Tenant, x.TokenHash }).IsUnique();
            e.HasIndex(x => new { x.Tenant, x.TokenHash, x.RevokedAt });
            e.HasIndex(x => new { x.Tenant, x.TokenId });
            e.HasIndex(x => new { x.Tenant, x.UserKey });
            e.HasIndex(x => new { x.Tenant, x.SessionId });
            e.HasIndex(x => new { x.Tenant, x.ChainId });
            e.HasIndex(x => new { x.Tenant, x.ExpiresAt });
            e.HasIndex(x => new { x.Tenant, x.ExpiresAt, x.RevokedAt });
            e.HasIndex(x => new { x.Tenant, x.ReplacedByTokenHash });

            e.Property(x => x.SessionId)
                .HasConversion(new AuthSessionIdConverter());

            e.Property(x => x.ChainId)
                .HasConversion(new NullableSessionChainIdConverter());

            e.Property(x => x.ExpiresAt)
                .IsRequired();
        });
    }
}
