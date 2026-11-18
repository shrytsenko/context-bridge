using System.ComponentModel;
using System.Text.Json;
using ContextBridge.Application.Handoffs;
using ContextBridge.Application.Projects;
using ContextBridge.Domain.Enums;
using ContextBridge.Domain.Exceptions;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace ContextBridge.Api.Tools;

[McpServerToolType]
public sealed class HandoffTools
{
    private readonly ProjectService _projects;
    private readonly HandoffService _handoffs;

    public HandoffTools(ProjectService projects, HandoffService handoffs)
    {
        _projects = projects;
        _handoffs = handoffs;
    }

    [McpServerTool(Name = "create_handoff")]
    [Description("Creates an actionable handoff task for another agent to pick up.")]
    public async Task<CallToolResult> CreateHandoff(
        [Description("Project slug.")] string project,
        [Description("Short title.")] string title,
        [Description("What the receiving agent should accomplish.")] string objective,
        [Description("List of acceptance criteria strings.")] string[] acceptanceCriteria,
        [Description("Optional originating agent: ChatGpt, ClaudeCode, User, or ApiClient.")] string? sourceAgent = null,
        [Description("Optional target agent: ChatGpt, ClaudeCode, User, or ApiClient.")] string? targetAgent = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sourceAgentValue = sourceAgent is null ? (Actor?)null : EnumParsing.Parse<Actor>(sourceAgent, "sourceAgent");
            var targetAgentValue = targetAgent is null ? (Actor?)null : EnumParsing.Parse<Actor>(targetAgent, "targetAgent");
            var acceptanceCriteriaJson = JsonSerializer.Serialize(acceptanceCriteria ?? []);

            var projectEntity = await _projects.GetBySlugAsync(project, cancellationToken);
            var created = await _handoffs.CreateAsync(
                projectEntity.Id, title, objective, acceptanceCriteriaJson, sourceAgentValue, targetAgentValue, cancellationToken);
            return ToolResults.Success(created.ToDto());
        }
        catch (DomainException ex)
        {
            return ToolResults.FromDomainException(ex);
        }
    }

    [McpServerTool(Name = "complete_handoff")]
    [Description("Records the outcome of a handoff. Completing does not by itself mean the implementation passed tests or review - report verification separately.")]
    public async Task<CallToolResult> CompleteHandoff(
        [Description("Handoff id.")] Guid handoffId,
        [Description("Final status: Completed or Cancelled.")] string status,
        [Description("Outcome description, including whether it was verified.")] string result,
        [Description("The handoff's current version, for optimistic concurrency.")] int expectedVersion,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var statusValue = EnumParsing.Parse<HandoffStatus>(status, "handoff status");
            var completed = await _handoffs.CompleteAsync(handoffId, statusValue, result, expectedVersion, cancellationToken);
            return ToolResults.Success(completed.ToDto());
        }
        catch (DomainException ex)
        {
            return ToolResults.FromDomainException(ex);
        }
    }
}
