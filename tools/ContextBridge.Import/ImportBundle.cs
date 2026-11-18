namespace ContextBridge.Import;

public sealed record ImportBundle(
    ImportProject Project,
    List<ImportContextEntry>? ContextEntries,
    List<ImportDecision>? Decisions,
    List<ImportHandoff>? Handoffs);

public sealed record ImportProject(string Slug, string Name, string? Description);

public sealed record ImportContextEntry(
    string Type,
    string Title,
    string Content,
    string Actor,
    string? ConversationReference);

public sealed record ImportDecision(
    string Title,
    string Rationale,
    string Status,
    string Actor,
    string? ConversationReference);

public sealed record ImportHandoff(
    string Title,
    string Objective,
    List<string> AcceptanceCriteria,
    string? SourceAgent,
    string? TargetAgent);
