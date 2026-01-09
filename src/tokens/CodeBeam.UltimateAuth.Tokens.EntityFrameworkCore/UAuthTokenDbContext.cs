using CodeBeam.UltimateAuth.Core.Domain;
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
        // -------------------------------------------------
        // REFRESH TOKEN
        // -------------------------------------------------
        b.Entity<RefreshTokenProjection>(e =>
        {
            e.HasKey(x => x.Id);

            e.Property(x => x.RowVersion)
                .IsRowVersion();

            e.Property(x => x.TokenHash)
                .IsRequired();

            e.HasIndex(x => new { x.TenantId, x.TokenHash })
                .IsUnique();

            e.HasIndex(x => new { x.TenantId, x.UserId });
            e.HasIndex(x => new { x.TenantId, x.SessionId });
            e.HasIndex(x => new { x.TenantId, x.ChainId });
            e.HasIndex(x => new { x.TenantId, x.ExpiresAt });
            e.HasIndex(x => new { x.TenantId, x.ReplacedByTokenHash });

            e.Property(x => x.ExpiresAt).IsRequired();
        });

        // -------------------------------------------------
        // REVOKED JTI
        // -------------------------------------------------
        b.Entity<RevokedTokenIdProjection>(e =>
        {
            e.HasKey(x => x.Id);

            e.Property(x => x.RowVersion)
                .IsRowVersion();

            e.Property(x => x.Jti)
                .IsRequired();

            e.HasIndex(x => x.Jti)
                .IsUnique();

            e.HasIndex(x => new { x.TenantId, x.Jti });

            e.Property(x => x.ExpiresAt)
                .IsRequired();

            e.Property(x => x.RevokedAt)
                .IsRequired();
        });
    }
}
