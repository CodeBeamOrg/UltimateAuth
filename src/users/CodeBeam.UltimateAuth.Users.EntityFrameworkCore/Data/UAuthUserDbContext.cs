using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.Users.EntityFrameworkCore;

public sealed class UAuthUserDbContext : DbContext
{
    public DbSet<UserLifecycleProjection> Lifecycles => Set<UserLifecycleProjection>();
    public DbSet<UserIdentifierProjection> Identifiers => Set<UserIdentifierProjection>();
    public DbSet<UserProfileProjection> Profiles => Set<UserProfileProjection>();

    public UAuthUserDbContext(DbContextOptions<UAuthUserDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        UAuthUsersModelBuilder.Configure(modelBuilder);
    }
}
