using CodeBeam.UltimateAuth.IdentityLab.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CodeBeam.UltimateAuth.IdentityLab.Persistence.Configurations;

public sealed class SandboxSettingsConfiguration : IEntityTypeConfiguration<SandboxSettings>
{
    public void Configure(EntityTypeBuilder<SandboxSettings> builder)
    {
        builder.ToTable("LabSandboxSettings");

        builder.HasKey(x => x.SandboxId);

        builder.Property(x => x.MaxUsers)
            .HasDefaultValue(5)
            .IsRequired();

        builder.ToTable(t =>
            t.HasCheckConstraint("CK_LabSandboxSettings_MaxUsers", "\"MaxUsers\" >= 1"));
    }
}
