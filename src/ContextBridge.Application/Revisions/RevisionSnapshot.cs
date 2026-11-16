using System.Text.Json;
using ContextBridge.Domain.Entities;

namespace ContextBridge.Application.Revisions;

public static class RevisionSnapshot
{
    public static string ToJson(ContextEntry entry) => JsonSerializer.Serialize(new
    {
        entry.Id,
        entry.ProjectId,
        entry.Type,
        entry.Title,
        entry.Content,
        entry.Source,
        entry.Version,
        entry.CreatedAt,
        entry.UpdatedAt
    });

    public static string ToJson(Decision decision) => JsonSerializer.Serialize(new
    {
        decision.Id,
        decision.ProjectId,
        decision.Title,
        decision.Rationale,
        decision.Status,
        decision.Source,
        decision.Version,
        decision.CreatedAt,
        decision.UpdatedAt
    });
}
