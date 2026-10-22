using CodeBeam.UltimateAuth.IdentityLab.Domain;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.IdentityLab.Persistence;

public sealed class LabDbContext(DbContextOptions<LabDbContext> options) : DbContext(options)
{
    public DbSet<Sandbox> Sandboxes => Set<Sandbox>();

    public DbSet<SandboxMember> SandboxMembers => Set<SandboxMember>();

    public DbSet<SandboxSettings> SandboxSettings => Set<SandboxSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LabDbContext).Assembly);
    }
}
