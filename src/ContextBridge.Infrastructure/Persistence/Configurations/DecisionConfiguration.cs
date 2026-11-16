using ContextBridge.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContextBridge.Infrastructure.Persistence.Configurations;

public sealed class DecisionConfiguration : IEntityTypeConfiguration<Decision>
{
    public void Configure(EntityTypeBuilder<Decision> builder)
    {
        builder.ToTable("decisions");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Title).HasMaxLength(200).IsRequired();
        builder.Property(d => d.Rationale).IsRequired();
        builder.Property(d => d.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(d => d.Version).IsRequired();

        builder.OwnsOne(d => d.Source, source =>
        {
            source.Property(s => s.Actor)
                .HasConversion<string>()
                .HasColumnName("source_actor")
                .HasMaxLength(50)
                .IsRequired();
            source.Property(s => s.ConversationReference).HasColumnName("source_conversation_reference");
            source.Property(s => s.CapturedAt).HasColumnName("source_captured_at");
        });

        builder.HasIndex(d => d.ProjectId);
        builder.HasIndex(d => d.Status);

        builder.HasOne<Project>().WithMany().HasForeignKey(d => d.ProjectId).OnDelete(DeleteBehavior.Cascade);
    }
}
