using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.IdentityLab.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CodeBeam.UltimateAuth.IdentityLab.Persistence.Configurations;

public sealed class SandboxMemberConfiguration : IEntityTypeConfiguration<SandboxMember>
{
    public void Configure(EntityTypeBuilder<SandboxMember> builder)
    {
        builder.ToTable("LabSandboxMembers");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.SandboxId)
            .IsRequired();

        builder.Property(x => x.UserKey)
            .HasConversion(
                v => v.Value,
                v => UserKey.FromString(v))
            .HasMaxLength(128)
            .IsRequired();

        builder.HasIndex(x => x.UserKey)
            .IsUnique();

        builder.Property(x => x.Role)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.JoinedAt)
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.SandboxId,
            x.Role
        });

        builder.HasIndex(x => x.SandboxId)
            .IsUnique()
            .HasFilter("\"Role\" = 'Owner'");
    }
}
