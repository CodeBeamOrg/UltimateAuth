using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.Tokens.EntityFrameworkCore;

public static class UAuthTokensModelBuilder
{
    public static void Configure(ModelBuilder b)
    {
        ConfigureRefreshTokens(b);
    }

    private static void ConfigureRefreshTokens(ModelBuilder b)
    {
        b.Entity<RefreshTokenProjection>(e =>
        {
            e.ToTable("UAuth_RefreshTokens");

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

            e.Property(x => x.TokenId)
                .HasConversion(
                    v => v.Value,
                    v => TokenId.From(v))
                .IsRequired();

            e.Property(x => x.TokenHash)
                .HasMaxLength(128)
                .IsRequired();

            e.Property(x => x.SessionId)
                .HasConversion(new AuthSessionIdConverter());

            e.Property(x => x.ChainId)
                .HasConversion(new NullableSessionChainIdConverter());

            e.Property(x => x.CreatedAt).HasUtcDateTimeOffsetConverter().IsRequired();
            e.Property(x => x.ExpiresAt).HasUtcDateTimeOffsetConverter().IsRequired();
            e.Property(x => x.RevokedAt).HasNullableUtcDateTimeOffsetConverter();

            e.Property(x => x.ReplacedByTokenHash)
                .HasMaxLength(128);

            e.HasIndex(x => new { x.Tenant, x.TokenHash }).IsUnique();
            e.HasIndex(x => new { x.Tenant, x.TokenHash, x.RevokedAt });
            e.HasIndex(x => new { x.Tenant, x.TokenId });
            e.HasIndex(x => new { x.Tenant, x.UserKey });
            e.HasIndex(x => new { x.Tenant, x.SessionId });
            e.HasIndex(x => new { x.Tenant, x.ChainId });
            e.HasIndex(x => new { x.Tenant, x.ExpiresAt });
            e.HasIndex(x => new { x.Tenant, x.ExpiresAt, x.RevokedAt });
            e.HasIndex(x => new { x.Tenant, x.ReplacedByTokenHash });
        });
    }
}