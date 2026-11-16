namespace ContextBridge.Domain.Entities;

public sealed class ContextRevision
{
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public string EntityType { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public int Version { get; private set; }
    public string Snapshot { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }

    private ContextRevision()
    {
    }

    public static ContextRevision Capture(Guid projectId, string entityType, Guid entityId, int version, string snapshotJson)
    {
        return new ContextRevision
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            EntityType = entityType,
            EntityId = entityId,
            Version = version,
            Snapshot = snapshotJson,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
