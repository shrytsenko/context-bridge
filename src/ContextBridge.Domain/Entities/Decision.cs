using ContextBridge.Domain.Enums;
using ContextBridge.Domain.Exceptions;
using ContextBridge.Domain.ValueObjects;

namespace ContextBridge.Domain.Entities;

public sealed class Decision
{
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Rationale { get; private set; } = string.Empty;
    public DecisionStatus Status { get; private set; }
    public Source Source { get; private set; } = null!;
    public int Version { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Decision()
    {
    }

    public static Decision Create(Guid projectId, string title, string rationale, DecisionStatus status, Source source)
    {
        Validate(title, rationale);

        var now = DateTimeOffset.UtcNow;
        return new Decision
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Title = title.Trim(),
            Rationale = rationale,
            Status = status,
            Source = source,
            Version = 1,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Update(string title, string rationale, DecisionStatus status, Source source, int expectedVersion)
    {
        if (expectedVersion != Version)
        {
            throw new ConcurrencyConflictException(nameof(Decision), Id, expectedVersion, Version);
        }

        Validate(title, rationale);

        Title = title.Trim();
        Rationale = rationale;
        Status = status;
        Source = source;
        Version++;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static void Validate(string title, string rationale)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainValidationException("Decision title is required.");
        }

        if (string.IsNullOrWhiteSpace(rationale))
        {
            throw new DomainValidationException("Decision rationale is required.");
        }
    }
}
