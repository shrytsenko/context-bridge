using System.Text.Json;
using ContextBridge.Domain.Entities;

namespace ContextBridge.Api.Tools;

internal static class DtoMappers
{
    public static ProjectDto ToDto(this Project project) =>
        new(project.Id, project.Slug, project.Name, project.Description, project.Version);

    public static ContextEntryDto ToDto(this ContextEntry entry) => new(
        entry.Id,
        entry.ProjectId,
        entry.Type.ToString(),
        entry.Title,
        entry.Content,
        entry.Source.Actor.ToString(),
        entry.Source.ConversationReference,
        entry.Version,
        entry.CreatedAt,
        entry.UpdatedAt);

    public static DecisionDto ToDto(this Decision decision) => new(
        decision.Id,
        decision.ProjectId,
        decision.Title,
        decision.Rationale,
        decision.Status.ToString(),
        decision.Source.Actor.ToString(),
        decision.Source.ConversationReference,
        decision.Version,
        decision.CreatedAt,
        decision.UpdatedAt);

    public static ContextEntryDto ToTruncatedDto(this ContextEntry entry, int maxContentLength) => entry.ToDto() with
    {
        Content = entry.Content.Length > maxContentLength
            ? entry.Content[..maxContentLength] + "…"
            : entry.Content
    };

    public static HandoffDto ToDto(this Handoff handoff) => new(
        handoff.Id,
        handoff.ProjectId,
        handoff.Title,
        handoff.Objective,
        JsonSerializer.Deserialize<string[]>(handoff.AcceptanceCriteria) ?? [],
        handoff.Status.ToString(),
        handoff.Result,
        handoff.SourceAgent?.ToString(),
        handoff.TargetAgent?.ToString(),
        handoff.Version,
        handoff.CreatedAt,
        handoff.UpdatedAt);
}
