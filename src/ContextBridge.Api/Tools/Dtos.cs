namespace ContextBridge.Api.Tools;

public sealed record ProjectDto(Guid Id, string Slug, string Name, string? Description, int Version);

public sealed record ContextEntryDto(
    Guid Id,
    Guid ProjectId,
    string Type,
    string Title,
    string Content,
    string SourceActor,
    string? ConversationReference,
    int Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record DecisionDto(
    Guid Id,
    Guid ProjectId,
    string Title,
    string Rationale,
    string Status,
    string SourceActor,
    string? ConversationReference,
    int Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record HandoffDto(
    Guid Id,
    Guid ProjectId,
    string Title,
    string Objective,
    IReadOnlyList<string> AcceptanceCriteria,
    string Status,
    string? Result,
    string? SourceAgent,
    string? TargetAgent,
    int Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record ProjectContextDto(
    ProjectDto Project,
    IReadOnlyList<ContextEntryDto>? Context,
    IReadOnlyList<DecisionDto>? Decisions,
    IReadOnlyList<HandoffDto>? Handoffs);
