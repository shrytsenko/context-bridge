using ContextBridge.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContextBridge.Infrastructure.Persistence.Configurations;

public sealed class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("idempotency_keys");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Operation).HasMaxLength(100).IsRequired();
        builder.Property(r => r.Key).HasMaxLength(200).IsRequired();
        builder.Property(r => r.EntityType).HasMaxLength(100).IsRequired();

        builder.HasIndex(r => new { r.Operation, r.Key }).IsUnique();
    }
}
