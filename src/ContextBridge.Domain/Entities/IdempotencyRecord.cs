namespace ContextBridge.Domain.Entities;

public sealed class IdempotencyRecord
{
    public Guid Id { get; private set; }
    public string Operation { get; private set; } = string.Empty;
    public string Key { get; private set; } = string.Empty;
    public string EntityType { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private IdempotencyRecord()
    {
    }

    public static IdempotencyRecord Create(string operation, string key, string entityType, Guid entityId)
    {
        return new IdempotencyRecord
        {
            Id = Guid.NewGuid(),
            Operation = operation,
            Key = key,
            EntityType = entityType,
            EntityId = entityId,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
