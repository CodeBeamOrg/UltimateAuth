using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.Sessions.EntityFrameworkCore
{
    internal sealed class UltimateAuthSessionDbContext<TUserId> : DbContext
    {
        public DbSet<SessionRootProjection<TUserId>> Roots => Set<SessionRootProjection<TUserId>>();
        public DbSet<SessionChainProjection<TUserId>> Chains => Set<SessionChainProjection<TUserId>>();
        public DbSet<SessionProjection<TUserId>> Sessions => Set<SessionProjection<TUserId>>();

        public UltimateAuthSessionDbContext(DbContextOptions options) : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder b)
        {
            b.Entity<SessionRootProjection<TUserId>>(e =>
            {
                e.HasKey(x => x.Id);

                e.Property(x => x.RowVersion)
                    .IsRowVersion();

                e.Property(x => x.UserId)
                    .IsRequired();

                e.HasIndex(x => new { x.TenantId, x.UserId })
                    .IsUnique();

                e.Property(x => x.SecurityVersion)
                    .IsRequired();

                e.Property(x => x.LastUpdatedAt)
                    .IsRequired();
            });

            b.Entity<SessionChainProjection<TUserId>>(e =>
            {
                e.HasKey(x => x.Id);

                e.Property(x => x.RowVersion)
                    .IsRowVersion();

                e.Property(x => x.UserId)
                    .IsRequired();

                e.HasIndex(x => x.ChainId)
                    .IsUnique();

                e.Property(x => x.ChainId)
                    .HasConversion(
                        v => v.Value,
                        v => ChainId.From(v))
                    .IsRequired();

                e.Property(x => x.ActiveSessionId)
                    .HasConversion(new NullableAuthSessionIdConverter());

                e.Property(x => x.ClaimsSnapshot)
                    .HasConversion(new JsonValueConverter<ClaimsSnapshot>())
                    .IsRequired();

                e.Property(x => x.SecurityVersionAtCreation)
                    .IsRequired();
            });

            b.Entity<SessionProjection<TUserId>>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.RowVersion).IsRowVersion();

                e.HasIndex(x => x.SessionId).IsUnique();
                e.HasIndex(x => new { x.ChainId, x.RevokedAt });

                e.Property(x => x.SessionId)
                    .HasConversion(
                        v => v.Value,
                        v => AuthSessionId.From(v))
                    .IsRequired();

                e.Property(x => x.ChainId)
                    .HasConversion(
                        v => v.Value,
                        v => ChainId.From(v))
                    .IsRequired();

                e.Property(x => x.Device)
                    .HasConversion(new JsonValueConverter<DeviceInfo>())
                    .IsRequired();

                e.Property(x => x.Claims)
                    .HasConversion(new JsonValueConverter<ClaimsSnapshot>())
                    .IsRequired();

                e.Property(x => x.Metadata)
                    .HasConversion(new JsonValueConverter<SessionMetadata>())
                    .IsRequired();
            });
        }

    }
}
