using ContextBridge.Domain.Enums;
using ContextBridge.Domain.Exceptions;

namespace ContextBridge.Domain.Entities;

public sealed class Handoff
{
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Objective { get; private set; } = string.Empty;
    public string AcceptanceCriteria { get; private set; } = "[]";
    public HandoffStatus Status { get; private set; }
    public string? Result { get; private set; }
    public Actor? SourceAgent { get; private set; }
    public Actor? TargetAgent { get; private set; }
    public int Version { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Handoff()
    {
    }

    public static Handoff Create(
        Guid projectId,
        string title,
        string objective,
        string acceptanceCriteriaJson,
        Actor? sourceAgent,
        Actor? targetAgent)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainValidationException("Handoff title is required.");
        }

        if (string.IsNullOrWhiteSpace(objective))
        {
            throw new DomainValidationException("Handoff objective is required.");
        }

        var now = DateTimeOffset.UtcNow;
        return new Handoff
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Title = title.Trim(),
            Objective = objective,
            AcceptanceCriteria = acceptanceCriteriaJson,
            Status = HandoffStatus.Pending,
            SourceAgent = sourceAgent,
            TargetAgent = targetAgent,
            Version = 1,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
