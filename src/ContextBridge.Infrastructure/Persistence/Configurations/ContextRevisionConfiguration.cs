using ContextBridge.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContextBridge.Infrastructure.Persistence.Configurations;

public sealed class ContextRevisionConfiguration : IEntityTypeConfiguration<ContextRevision>
{
    public void Configure(EntityTypeBuilder<ContextRevision> builder)
    {
        builder.ToTable("context_revisions");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.EntityType).HasMaxLength(100).IsRequired();
        builder.Property(r => r.Snapshot).HasColumnType("jsonb").IsRequired();

        builder.HasIndex(r => new { r.EntityType, r.EntityId });
        builder.HasIndex(r => r.ProjectId);

        builder.HasOne<Project>().WithMany().HasForeignKey(r => r.ProjectId).OnDelete(DeleteBehavior.Cascade);
    }
}
