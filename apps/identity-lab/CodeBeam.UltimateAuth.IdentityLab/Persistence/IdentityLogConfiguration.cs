using CodeBeam.UltimateAuth.IdentityLab.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CodeBeam.UltimateAuth.IdentityLab.Persistence;

public sealed class ActivityLogConfiguration : IEntityTypeConfiguration<ActivityLog>
{
    public void Configure(EntityTypeBuilder<ActivityLog> builder)
    {
        builder.ToTable("LabActivityLogs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.SandboxId)
            .IsRequired();

        builder.Property(x => x.ActorUserKey)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.EventType)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.TargetUserKey)
            .HasMaxLength(128);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasOne(x => x.Sandbox)
            .WithMany()
            .HasForeignKey(x => x.SandboxId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new
        {
            x.SandboxId,
            x.CreatedAt
        });

        builder.HasIndex(x => new
        {
            x.SandboxId,
            x.ActorUserKey
        });
    }
}
