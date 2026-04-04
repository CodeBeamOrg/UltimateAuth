using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.Authentication.EntityFrameworkCore;

public sealed class UAuthAuthenticationDbContext : DbContext
{
    public DbSet<AuthenticationSecurityStateProjection> AuthenticationSecurityStates => Set<AuthenticationSecurityStateProjection>();


    public UAuthAuthenticationDbContext(DbContextOptions<UAuthAuthenticationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        UAuthAuthenticationModelBuilder.Configure(modelBuilder);
    }
}
