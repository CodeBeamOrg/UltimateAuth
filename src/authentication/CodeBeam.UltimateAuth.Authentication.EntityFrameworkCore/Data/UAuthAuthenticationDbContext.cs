using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Security;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.Authentication.EntityFrameworkCore;

internal sealed class UAuthAuthenticationDbContext : DbContext
{
    public DbSet<AuthenticationSecurityStateProjection> AuthenticationSecurityStates => Set<AuthenticationSecurityStateProjection>();
    private readonly TenantContext _tenant;

    public UAuthAuthenticationDbContext(DbContextOptions<UAuthAuthenticationDbContext> options, TenantContext tenant)
        : base(options)
    {
        _tenant = tenant;
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        ConfigureAuthenticationSecurityState(b);
    }

    private void ConfigureAuthenticationSecurityState(ModelBuilder b)
    {
        b.Entity<AuthenticationSecurityStateProjection>(e =>
        {
            e.HasKey(x => x.Id);

            e.Property(x => x.SecurityVersion).IsConcurrencyToken();

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

            e.Property(x => x.Scope)
                .IsRequired();

            e.Property(x => x.CredentialType);

            e.Property(x => x.FailedAttempts)
                .IsRequired();

            e.Property(x => x.LastFailedAt);

            e.Property(x => x.LockedUntil);

            e.Property(x => x.RequiresReauthentication)
                .IsRequired();

            e.Property(x => x.ResetRequestedAt);
            e.Property(x => x.ResetExpiresAt);
            e.Property(x => x.ResetConsumedAt);

            e.Property(x => x.ResetTokenHash)
                .HasMaxLength(512);

            e.Property(x => x.ResetAttempts)
                .IsRequired();

            e.HasIndex(x => new { x.Tenant, x.UserKey, x.Scope, x.CredentialType }).IsUnique();
            e.HasIndex(x => new { x.Tenant, x.UserKey });
            e.HasIndex(x => new { x.Tenant, x.LockedUntil });
            e.HasIndex(x => new { x.Tenant, x.ResetRequestedAt });
            e.HasIndex(x => new { x.Tenant, x.UserKey, x.Scope });

            e.HasQueryFilter(x => _tenant.IsGlobal || x.Tenant == _tenant.Tenant); });
    }
}
