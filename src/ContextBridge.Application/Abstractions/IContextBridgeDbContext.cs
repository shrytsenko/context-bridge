using ContextBridge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ContextBridge.Application.Abstractions;

public interface IContextBridgeDbContext
{
    DbSet<Project> Projects { get; }
    DbSet<ContextEntry> ContextEntries { get; }
    DbSet<Decision> Decisions { get; }
    DbSet<Handoff> Handoffs { get; }
    DbSet<ContextRevision> ContextRevisions { get; }
    DbSet<IdempotencyRecord> IdempotencyRecords { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
