using ContextBridge.Application.Abstractions;
using ContextBridge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ContextBridge.Application.Idempotency;

internal static class IdempotencyGuard
{
    public static async Task<Guid?> TryGetExistingEntityIdAsync(
        IContextBridgeDbContext db, string operation, string? key, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        var record = await db.IdempotencyRecords
            .SingleOrDefaultAsync(r => r.Operation == operation && r.Key == key, ct);
        return record?.EntityId;
    }

    public static async Task RecordAsync(
        IContextBridgeDbContext db, string operation, string? key, string entityType, Guid entityId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        await db.IdempotencyRecords.AddAsync(IdempotencyRecord.Create(operation, key, entityType, entityId), ct);
    }
}
