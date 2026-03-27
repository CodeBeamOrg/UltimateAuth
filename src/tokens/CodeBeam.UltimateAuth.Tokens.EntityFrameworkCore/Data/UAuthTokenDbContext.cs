using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.Tokens.EntityFrameworkCore;

public sealed class UAuthTokenDbContext : DbContext
{
    public DbSet<RefreshTokenProjection> RefreshTokens => Set<RefreshTokenProjection>();
    //public DbSet<RevokedTokenIdProjection> RevokedTokenIds => Set<RevokedTokenIdProjection>(); // TODO: Add when JWT added.

    public UAuthTokenDbContext(DbContextOptions<UAuthTokenDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        UAuthTokensModelBuilder.Configure(modelBuilder);
    }
}
