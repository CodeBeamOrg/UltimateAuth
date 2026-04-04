using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.Credentials.EntityFrameworkCore;

public static class UAuthCredentialsModelBuilder
{
    public static void Configure(ModelBuilder b)
    {
        ConfigurePasswordCredentials(b);
    }

    private static void ConfigurePasswordCredentials(ModelBuilder b)
    {
        b.Entity<PasswordCredentialProjection>(e =>
        {
            e.ToTable("UAuth_PasswordCredentials");

            e.HasKey(x => x.Id);

            e.Property(x => x.Version)
                .IsConcurrencyToken();

            e.Property(x => x.Tenant)
                .HasConversion(v => v.Value, v => TenantKey.FromInternal(v))
                .HasMaxLength(128)
                .IsRequired();

            e.Property(x => x.UserKey)
                .HasConversion(v => v.Value, v => UserKey.FromString(v))
                .HasMaxLength(128)
                .IsRequired();

            e.Property(x => x.SecretHash)
                .HasMaxLength(512)
                .IsRequired();

            e.Property(x => x.SecurityStamp)
                .IsRequired();

            e.Property(x => x.Source)
                .HasMaxLength(128);

            e.Property(x => x.CreatedAt).HasUtcDateTimeOffsetConverter().IsRequired();
            e.Property(x => x.UpdatedAt).HasNullableUtcDateTimeOffsetConverter();
            e.Property(x => x.DeletedAt).HasNullableUtcDateTimeOffsetConverter();
            e.Property(x => x.RevokedAt).HasNullableUtcDateTimeOffsetConverter();
            e.Property(x => x.ExpiresAt).HasNullableUtcDateTimeOffsetConverter();
            e.Property(x => x.LastUsedAt).HasNullableUtcDateTimeOffsetConverter();

            e.HasIndex(x => new { x.Tenant, x.Id }).IsUnique();
            e.HasIndex(x => new { x.Tenant, x.UserKey });
            e.HasIndex(x => new { x.Tenant, x.UserKey, x.DeletedAt });
            e.HasIndex(x => new { x.Tenant, x.RevokedAt });
            e.HasIndex(x => new { x.Tenant, x.ExpiresAt });
        });
    }
}
