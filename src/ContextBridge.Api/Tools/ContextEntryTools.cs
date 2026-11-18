using System.ComponentModel;
using ContextBridge.Application.ContextEntries;
using ContextBridge.Application.Projects;
using ContextBridge.Domain.Enums;
using ContextBridge.Domain.Exceptions;
using ContextBridge.Domain.ValueObjects;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace ContextBridge.Api.Tools;

[McpServerToolType]
public sealed class ContextEntryTools
{
    private readonly ProjectService _projects;
    private readonly ContextEntryService _contextEntries;

    public ContextEntryTools(ProjectService projects, ContextEntryService contextEntries)
    {
        _projects = projects;
        _contextEntries = contextEntries;
    }

    [McpServerTool(Name = "save_context")]
    [Description("Creates a new context entry, or updates an existing one when entryId is given (requires expectedVersion for optimistic concurrency).")]
    public async Task<CallToolResult> SaveContext(
        [Description("Project slug.")] string project,
        [Description("Entry type: Requirement, Constraint, Architecture, Fact, or DiscussionSummary.")] string type,
        [Description("Short title.")] string title,
        [Description("Markdown content.")] string content,
        [Description("Who is recording this: ChatGpt, ClaudeCode, User, or ApiClient.")] string actor,
        [Description("Optional source conversation identifier or link.")] string? conversationReference = null,
        [Description("Existing entry id to update; omit to create a new entry.")] Guid? entryId = null,
        [Description("Required when updating: the entry's current version, for optimistic concurrency.")] int? expectedVersion = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entryType = EnumParsing.Parse<ContextEntryType>(type, "context entry type");
            var actorValue = EnumParsing.Parse<Actor>(actor, "actor");
            var source = new Source(actorValue, conversationReference, DateTimeOffset.UtcNow);

            if (entryId is { } id)
            {
                if (expectedVersion is not { } version)
                {
                    throw new DomainValidationException("expectedVersion is required when updating an existing context entry.");
                }

                var updated = await _contextEntries.UpdateAsync(id, title, content, source, version, cancellationToken);
                return ToolResults.Success(updated.ToDto());
            }

            var projectEntity = await _projects.GetBySlugAsync(project, cancellationToken);
            var created = await _contextEntries.CreateAsync(projectEntity.Id, entryType, title, content, source, cancellationToken);
            return ToolResults.Success(created.ToDto());
        }
        catch (DomainException ex)
        {
            return ToolResults.FromDomainException(ex);
        }
    }
}
