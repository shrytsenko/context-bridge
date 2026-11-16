using ContextBridge.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContextBridge.Infrastructure.Persistence.Configurations;

public sealed class HandoffConfiguration : IEntityTypeConfiguration<Handoff>
{
    public void Configure(EntityTypeBuilder<Handoff> builder)
    {
        builder.ToTable("handoffs");
        builder.HasKey(h => h.Id);

        builder.Property(h => h.Title).HasMaxLength(200).IsRequired();
        builder.Property(h => h.Objective).IsRequired();
        builder.Property(h => h.AcceptanceCriteria).HasColumnType("jsonb").IsRequired();
        builder.Property(h => h.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(h => h.SourceAgent).HasConversion<string>().HasMaxLength(50);
        builder.Property(h => h.TargetAgent).HasConversion<string>().HasMaxLength(50);
        builder.Property(h => h.Version).IsRequired();

        builder.HasIndex(h => h.ProjectId);
        builder.HasIndex(h => h.Status);

        builder.HasOne<Project>().WithMany().HasForeignKey(h => h.ProjectId).OnDelete(DeleteBehavior.Cascade);
    }
}
