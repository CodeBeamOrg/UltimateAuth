using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.Credentials.EntityFrameworkCore;

public sealed class UAuthCredentialDbContext : DbContext
{
    public DbSet<PasswordCredentialProjection> PasswordCredentials => Set<PasswordCredentialProjection>();

    public UAuthCredentialDbContext(DbContextOptions<UAuthCredentialDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        UAuthCredentialsModelBuilder.Configure(modelBuilder);
    }
}
