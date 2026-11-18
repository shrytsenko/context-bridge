using System.ComponentModel;
using ContextBridge.Application.Decisions;
using ContextBridge.Application.Projects;
using ContextBridge.Domain.Enums;
using ContextBridge.Domain.Exceptions;
using ContextBridge.Domain.ValueObjects;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace ContextBridge.Api.Tools;

[McpServerToolType]
public sealed class DecisionTools
{
    private readonly ProjectService _projects;
    private readonly DecisionService _decisions;

    public DecisionTools(ProjectService projects, DecisionService decisions)
    {
        _projects = projects;
        _decisions = decisions;
    }

    [McpServerTool(Name = "save_decision")]
    [Description("Creates a new decision, or updates an existing one when decisionId is given (requires expectedVersion). Status is always explicit - it is never silently promoted to Accepted.")]
    public async Task<CallToolResult> SaveDecision(
        [Description("Project slug.")] string project,
        [Description("Short title.")] string title,
        [Description("Rationale for the decision.")] string rationale,
        [Description("Status: Proposed, Accepted, Superseded, or Rejected.")] string status,
        [Description("Who is recording this: ChatGpt, ClaudeCode, User, or ApiClient.")] string actor,
        [Description("Optional source conversation identifier or link.")] string? conversationReference = null,
        [Description("Existing decision id to update; omit to create a new decision.")] Guid? decisionId = null,
        [Description("Required when updating: the decision's current version, for optimistic concurrency.")] int? expectedVersion = null,
        [Description("Optional key to make a create request safe to retry without creating a duplicate decision. Ignored when updating.")] string? idempotencyKey = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var statusValue = EnumParsing.Parse<DecisionStatus>(status, "decision status");
            var actorValue = EnumParsing.Parse<Actor>(actor, "actor");
            var source = new Source(actorValue, conversationReference, DateTimeOffset.UtcNow);

            if (decisionId is { } id)
            {
                if (expectedVersion is not { } version)
                {
                    throw new DomainValidationException("expectedVersion is required when updating an existing decision.");
                }

                var updated = await _decisions.UpdateAsync(id, title, rationale, statusValue, source, version, cancellationToken);
                return ToolResults.Success(updated.ToDto());
            }

            var projectEntity = await _projects.GetOrCreateBySlugAsync(project, cancellationToken);
            var created = await _decisions.CreateAsync(projectEntity.Id, title, rationale, statusValue, source, idempotencyKey, cancellationToken);
            return ToolResults.Success(created.ToDto());
        }
        catch (DomainException ex)
        {
            return ToolResults.FromDomainException(ex);
        }
    }
}
