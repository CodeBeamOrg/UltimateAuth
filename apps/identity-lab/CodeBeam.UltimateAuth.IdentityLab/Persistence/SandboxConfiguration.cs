using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.IdentityLab.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CodeBeam.UltimateAuth.IdentityLab.Persistence.Configurations;

public sealed class SandboxConfiguration : IEntityTypeConfiguration<Sandbox>
{
    public void Configure(EntityTypeBuilder<Sandbox> builder)
    {
        builder.ToTable("LabSandboxes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.OwnerUserKey)
            .HasConversion(
                v => v.Value,
                v => UserKey.FromString(v))
            .HasMaxLength(128)
            .IsRequired();

        builder.HasIndex(x => x.OwnerUserKey)
            .IsUnique();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .HasDefaultValue(SandboxStatus.Active)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasMany(x => x.Members)
            .WithOne(x => x.Sandbox)
            .HasForeignKey(x => x.SandboxId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Settings)
            .WithOne(x => x.Sandbox)
            .HasForeignKey<SandboxSettings>(x => x.SandboxId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
