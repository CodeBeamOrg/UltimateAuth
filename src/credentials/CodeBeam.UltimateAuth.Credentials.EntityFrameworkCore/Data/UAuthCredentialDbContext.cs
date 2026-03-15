using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.Credentials.EntityFrameworkCore;

internal sealed class UAuthCredentialDbContext : DbContext
{
    public DbSet<PasswordCredentialProjection> PasswordCredentials => Set<PasswordCredentialProjection>();

    private readonly TenantContext _tenant;

    public UAuthCredentialDbContext(DbContextOptions<UAuthCredentialDbContext> options, TenantContext tenant)
        : base(options)
    {
        _tenant = tenant;
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        ConfigurePasswordCredential(b);
    }

    private void ConfigurePasswordCredential(ModelBuilder b)
    {
        b.Entity<PasswordCredentialProjection>(e =>
        {
            e.HasKey(x => x.Id);

            e.Property(x => x.Version).IsConcurrencyToken();

            e.Property(x => x.Tenant)
                .HasConversion(
                    v => v.Value,
                    v => TenantKey.FromInternal(v))
                .HasMaxLength(128)
                .IsRequired();

            e.Property(x => x.UserKey)
                .HasConversion(
                    v => v.Value,
                    v => UserKey.FromString(v))
                .HasMaxLength(128)
                .IsRequired();

            e.Property(x => x.SecretHash)
                .HasMaxLength(512)
                .IsRequired();

            e.Property(x => x.SecurityStamp).IsRequired();
            e.Property(x => x.RevokedAt);
            e.Property(x => x.ExpiresAt);
            e.Property(x => x.LastUsedAt);
            e.Property(x => x.Source).HasMaxLength(128);
            e.Property(x => x.CreatedAt).IsRequired();
            e.Property(x => x.UpdatedAt);
            e.Property(x => x.DeletedAt);

            e.HasIndex(x => new { x.Tenant, x.Id }).IsUnique();
            e.HasIndex(x => new { x.Tenant, x.UserKey });
            e.HasIndex(x => new { x.Tenant, x.UserKey, x.DeletedAt });
            e.HasIndex(x => new { x.Tenant, x.RevokedAt });
            e.HasIndex(x => new { x.Tenant, x.ExpiresAt });

            e.HasQueryFilter(x => _tenant.IsGlobal || x.Tenant == _tenant.Tenant);
        });
    }
}