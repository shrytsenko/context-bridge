using System.ComponentModel;
using ContextBridge.Application.ContextEntries;
using ContextBridge.Application.Decisions;
using ContextBridge.Application.Handoffs;
using ContextBridge.Application.Projects;
using ContextBridge.Domain.Enums;
using ContextBridge.Domain.Exceptions;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace ContextBridge.Api.Tools;

[McpServerToolType]
public sealed class ProjectContextTools
{
    private const int MaxItemsPerCategory = 20;
    private const int MaxContentPreviewLength = 500;
    private const int DefaultSearchLimit = 20;
    private const int MaxSearchLimit = 100;

    private readonly ProjectService _projects;
    private readonly ContextEntryService _contextEntries;
    private readonly DecisionService _decisions;
    private readonly HandoffService _handoffs;

    public ProjectContextTools(
        ProjectService projects,
        ContextEntryService contextEntries,
        DecisionService decisions,
        HandoffService handoffs)
    {
        _projects = projects;
        _contextEntries = contextEntries;
        _decisions = decisions;
        _handoffs = handoffs;
    }

    [McpServerTool(Name = "get_project_context")]
    [Description("Returns a compact current snapshot of a project: its metadata plus recent context entries, decisions, and handoffs. Bounded to the most recent items, not full history.")]
    public async Task<CallToolResult> GetProjectContext(
        [Description("Project slug.")] string project,
        [Description("Which categories to include: any of 'context', 'decisions', 'handoffs'. Omit to include all.")] string[]? include = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var projectEntity = await _projects.GetBySlugAsync(project, cancellationToken);

            var includeSet = include is { Length: > 0 }
                ? include.Select(i => i.Trim().ToLowerInvariant()).ToHashSet()
                : ["context", "decisions", "handoffs"];

            var contextDtos = includeSet.Contains("context")
                ? (await _contextEntries.ListByProjectAsync(projectEntity.Id, cancellationToken))
                    .OrderByDescending(e => e.UpdatedAt).Take(MaxItemsPerCategory)
                    .Select(e => e.ToTruncatedDto(MaxContentPreviewLength)).ToList()
                : null;

            var decisionDtos = includeSet.Contains("decisions")
                ? (await _decisions.ListByProjectAsync(projectEntity.Id, cancellationToken))
                    .OrderByDescending(d => d.UpdatedAt).Take(MaxItemsPerCategory).Select(d => d.ToDto()).ToList()
                : null;

            var handoffDtos = includeSet.Contains("handoffs")
                ? (await _handoffs.ListByProjectAsync(projectEntity.Id, cancellationToken))
                    .OrderByDescending(h => h.UpdatedAt).Take(MaxItemsPerCategory).Select(h => h.ToDto()).ToList()
                : null;

            var snapshot = new ProjectContextDto(projectEntity.ToDto(), contextDtos, decisionDtos, handoffDtos);
            return ToolResults.Success(snapshot);
        }
        catch (DomainException ex)
        {
            return ToolResults.FromDomainException(ex);
        }
    }

    [McpServerTool(Name = "search_context")]
    [Description("Searches a project's context entries by a text query, optionally filtered by entry type.")]
    public async Task<CallToolResult> SearchContext(
        [Description("Project slug.")] string project,
        [Description("Text to search for in context entry titles and content.")] string query,
        [Description("Optional context entry types to filter by: Requirement, Constraint, Architecture, Fact, DiscussionSummary.")] string[]? types = null,
        [Description("Maximum number of results (default 20, max 100).")] int? limit = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                throw new DomainValidationException("Search query is required.");
            }

            var projectEntity = await _projects.GetBySlugAsync(project, cancellationToken);

            var parsedTypes = types?.Select(t => EnumParsing.Parse<ContextEntryType>(t, "context entry type")).ToList();
            var effectiveLimit = Math.Clamp(limit ?? DefaultSearchLimit, 1, MaxSearchLimit);

            var results = await _contextEntries.SearchAsync(projectEntity.Id, query, parsedTypes, effectiveLimit, cancellationToken);
            return ToolResults.Success(results.Select(e => e.ToDto()).ToList());
        }
        catch (DomainException ex)
        {
            return ToolResults.FromDomainException(ex);
        }
    }
}
