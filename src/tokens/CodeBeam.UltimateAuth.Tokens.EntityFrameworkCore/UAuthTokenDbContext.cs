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
            e.HasKey(x => x.Id);

            e.Property(x => x.Version).IsConcurrencyToken();

            e.Property(x => x.TokenHash)
                .IsRequired();

            e.HasIndex(x => new { x.Tenant, x.TokenHash })
                .IsUnique();

            e.HasIndex(x => new { x.Tenant, x.UserKey });
            e.HasIndex(x => new { x.Tenant, x.SessionId });
            e.HasIndex(x => new { x.Tenant, x.ChainId });
            e.HasIndex(x => new { x.Tenant, x.ExpiresAt });
            e.HasIndex(x => new { x.Tenant, x.ReplacedByTokenHash });

            e.Property(x => x.ExpiresAt).IsRequired();
        });

        b.Entity<RevokedTokenIdProjection>(e =>
        {
            e.HasKey(x => x.Id);

            e.Property(x => x.Version).IsConcurrencyToken();

            e.Property(x => x.Jti)
                .IsRequired();

            e.HasIndex(x => x.Jti)
                .IsUnique();

            e.HasIndex(x => new { x.Tenant, x.Jti });

            e.Property(x => x.ExpiresAt)
                .IsRequired();

            e.Property(x => x.RevokedAt)
                .IsRequired();
        });
    }
}
