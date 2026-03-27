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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        UAuthSessionsModelBuilder.Configure(modelBuilder);
    }
}
