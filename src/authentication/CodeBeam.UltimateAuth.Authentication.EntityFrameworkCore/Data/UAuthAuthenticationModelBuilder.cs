using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.Authentication.EntityFrameworkCore;

public static class UAuthAuthenticationModelBuilder
{
    public static void Configure(ModelBuilder b)
    {
        ConfigureAuthenticationSecurityState(b);
    }

    private static void ConfigureAuthenticationSecurityState(ModelBuilder b)
    {
        b.Entity<AuthenticationSecurityStateProjection>(e =>
        {
            e.ToTable("UAuth_Authentication");

            e.HasKey(x => x.Id);

            e.Property(x => x.SecurityVersion)
                .IsConcurrencyToken();

            e.Property(x => x.Tenant)
                .HasConversion(v => v.Value, v => TenantKey.FromInternal(v))
                .HasMaxLength(128)
                .IsRequired();

            e.Property(x => x.UserKey)
                .HasConversion(v => v.Value, v => UserKey.FromString(v))
                .HasMaxLength(128)
                .IsRequired();

            e.Property(x => x.Scope)
                .IsRequired();

            e.Property(x => x.CredentialType);

            e.Property(x => x.FailedAttempts)
                .IsRequired();

            e.Property(x => x.LastFailedAt)
                .HasNullableUtcDateTimeOffsetConverter();

            e.Property(x => x.LockedUntil)
                .HasNullableUtcDateTimeOffsetConverter();

            e.Property(x => x.RequiresReauthentication)
                .IsRequired();

            e.Property(x => x.ResetRequestedAt)
                .HasNullableUtcDateTimeOffsetConverter();

            e.Property(x => x.ResetExpiresAt)
                .HasNullableUtcDateTimeOffsetConverter();

            e.Property(x => x.ResetConsumedAt)
                .HasNullableUtcDateTimeOffsetConverter();

            e.Property(x => x.ResetTokenHash)
                .HasMaxLength(512);

            e.Property(x => x.ResetAttempts)
                .IsRequired();

            e.HasIndex(x => new { x.Tenant, x.UserKey, x.Scope, x.CredentialType }).IsUnique();
            e.HasIndex(x => new { x.Tenant, x.UserKey });
            e.HasIndex(x => new { x.Tenant, x.LockedUntil });
            e.HasIndex(x => new { x.Tenant, x.ResetRequestedAt });
            e.HasIndex(x => new { x.Tenant, x.UserKey, x.Scope });
        });
    }
}