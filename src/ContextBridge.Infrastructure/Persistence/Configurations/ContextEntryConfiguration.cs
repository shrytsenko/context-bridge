using ContextBridge.Domain.Entities;
using ContextBridge.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContextBridge.Infrastructure.Persistence.Configurations;

public sealed class ContextEntryConfiguration : IEntityTypeConfiguration<ContextEntry>
{
    public void Configure(EntityTypeBuilder<ContextEntry> builder)
    {
        builder.ToTable("context_entries");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Type).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(e => e.Title).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Content).IsRequired();
        builder.Property(e => e.Version).IsRequired();

        builder.OwnsOne(e => e.Source, source =>
        {
            source.Property(s => s.Actor)
                .HasConversion<string>()
                .HasColumnName("source_actor")
                .HasMaxLength(50)
                .IsRequired();
            source.Property(s => s.ConversationReference).HasColumnName("source_conversation_reference");
            source.Property(s => s.CapturedAt).HasColumnName("source_captured_at");
        });

        builder.HasIndex(e => e.ProjectId);
        builder.HasIndex(e => e.Type);

        builder.HasOne<Project>().WithMany().HasForeignKey(e => e.ProjectId).OnDelete(DeleteBehavior.Cascade);
    }
}
