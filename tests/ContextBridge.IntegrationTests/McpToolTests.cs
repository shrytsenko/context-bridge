using System.Text.Json;
using ContextBridge.Application.Projects;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Xunit;

namespace ContextBridge.IntegrationTests;

public sealed class McpToolTests : IntegrationTestBase
{
    [Fact]
    public async Task Save_context_then_get_project_context_and_search_context_round_trip()
    {
        await using var mcp = await CreateMcpClientAsync();

        var projectSlug = $"kartimo-{Guid.NewGuid():N}";
        await SeedProjectAsync(projectSlug);

        var created = await CallSuccessAsync(mcp, "save_context", new Dictionary<string, object?>
        {
            ["project"] = projectSlug,
            ["type"] = "Architecture",
            ["title"] = "Backend architecture",
            ["content"] = "ASP.NET Core modular monolith",
            ["actor"] = "ChatGpt"
        });
        var entryId = created.GetProperty("id").GetGuid();
        Assert.Equal(1, created.GetProperty("version").GetInt32());

        var snapshot = await CallSuccessAsync(mcp, "get_project_context", new Dictionary<string, object?>
        {
            ["project"] = projectSlug
        });
        Assert.Equal(projectSlug, snapshot.GetProperty("project").GetProperty("slug").GetString());
        Assert.Contains(
            snapshot.GetProperty("context").EnumerateArray(),
            e => e.GetProperty("id").GetGuid() == entryId);

        var searchResult = await CallSuccessAsync(mcp, "search_context", new Dictionary<string, object?>
        {
            ["project"] = projectSlug,
            ["query"] = "modular monolith"
        });
        Assert.Contains(searchResult.EnumerateArray(), e => e.GetProperty("id").GetGuid() == entryId);

        var updated = await CallSuccessAsync(mcp, "save_context", new Dictionary<string, object?>
        {
            ["project"] = projectSlug,
            ["type"] = "Architecture",
            ["title"] = "Backend architecture v2",
            ["content"] = "ASP.NET Core modular monolith + PostgreSQL",
            ["actor"] = "ChatGpt",
            ["entryId"] = entryId,
            ["expectedVersion"] = 1
        });
        Assert.Equal(2, updated.GetProperty("version").GetInt32());
    }

    [Fact]
    public async Task Save_decision_create_and_update_never_silently_accepts()
    {
        await using var mcp = await CreateMcpClientAsync();

        var projectSlug = $"kartimo-{Guid.NewGuid():N}";
        await SeedProjectAsync(projectSlug);

        var created = await CallSuccessAsync(mcp, "save_decision", new Dictionary<string, object?>
        {
            ["project"] = projectSlug,
            ["title"] = "Use PostgreSQL",
            ["rationale"] = "Need full-text search and jsonb",
            ["status"] = "Proposed",
            ["actor"] = "User"
        });
        Assert.Equal("Proposed", created.GetProperty("status").GetString());
        var decisionId = created.GetProperty("id").GetGuid();

        var accepted = await CallSuccessAsync(mcp, "save_decision", new Dictionary<string, object?>
        {
            ["project"] = projectSlug,
            ["title"] = "Use PostgreSQL",
            ["rationale"] = "Need full-text search and jsonb",
            ["status"] = "Accepted",
            ["actor"] = "User",
            ["decisionId"] = decisionId,
            ["expectedVersion"] = 1
        });
        Assert.Equal("Accepted", accepted.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Create_handoff_then_complete_handoff_round_trip()
    {
        await using var mcp = await CreateMcpClientAsync();

        var projectSlug = $"kartimo-{Guid.NewGuid():N}";
        await SeedProjectAsync(projectSlug);

        var created = await CallSuccessAsync(mcp, "create_handoff", new Dictionary<string, object?>
        {
            ["project"] = projectSlug,
            ["title"] = "Implement championship domain",
            ["objective"] = "Implement the approved domain model",
            ["acceptanceCriteria"] = new[] { "Championship and Season entities", "Unit tests" },
            ["sourceAgent"] = "ChatGpt",
            ["targetAgent"] = "ClaudeCode"
        });
        Assert.Equal("Pending", created.GetProperty("status").GetString());
        var handoffId = created.GetProperty("id").GetGuid();

        var completed = await CallSuccessAsync(mcp, "complete_handoff", new Dictionary<string, object?>
        {
            ["handoffId"] = handoffId,
            ["status"] = "Completed",
            ["result"] = "Implemented and verified with passing tests.",
            ["expectedVersion"] = 1
        });
        Assert.Equal("Completed", completed.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Get_project_context_for_unknown_project_returns_not_found_error()
    {
        await using var mcp = await CreateMcpClientAsync();

        await AssertErrorAsync(mcp, "get_project_context", new Dictionary<string, object?>
        {
            ["project"] = "does-not-exist"
        }, "NOT_FOUND");
    }

    [Fact]
    public async Task Save_context_with_stale_expected_version_returns_version_conflict_error()
    {
        await using var mcp = await CreateMcpClientAsync();

        var projectSlug = $"kartimo-{Guid.NewGuid():N}";
        await SeedProjectAsync(projectSlug);

        var created = await CallSuccessAsync(mcp, "save_context", new Dictionary<string, object?>
        {
            ["project"] = projectSlug,
            ["type"] = "Fact",
            ["title"] = "Title",
            ["content"] = "Content",
            ["actor"] = "User"
        });
        var entryId = created.GetProperty("id").GetGuid();

        await AssertErrorAsync(mcp, "save_context", new Dictionary<string, object?>
        {
            ["project"] = projectSlug,
            ["type"] = "Fact",
            ["title"] = "Stale update",
            ["content"] = "Stale content",
            ["actor"] = "User",
            ["entryId"] = entryId,
            ["expectedVersion"] = 99
        }, "VERSION_CONFLICT");
    }

    [Fact]
    public async Task Save_context_with_same_idempotency_key_does_not_create_a_duplicate()
    {
        await using var mcp = await CreateMcpClientAsync();

        var projectSlug = $"kartimo-{Guid.NewGuid():N}";
        await SeedProjectAsync(projectSlug);
        var idempotencyKey = Guid.NewGuid().ToString();

        var args = new Dictionary<string, object?>
        {
            ["project"] = projectSlug,
            ["type"] = "Fact",
            ["title"] = "Title",
            ["content"] = "Content",
            ["actor"] = "User",
            ["idempotencyKey"] = idempotencyKey
        };

        var first = await CallSuccessAsync(mcp, "save_context", args);
        var second = await CallSuccessAsync(mcp, "save_context", args);

        Assert.Equal(first.GetProperty("id").GetGuid(), second.GetProperty("id").GetGuid());

        var snapshot = await CallSuccessAsync(mcp, "get_project_context", new Dictionary<string, object?>
        {
            ["project"] = projectSlug
        });
        Assert.Equal(1, snapshot.GetProperty("context").GetArrayLength());
    }

    [Fact]
    public async Task Create_handoff_with_same_idempotency_key_does_not_create_a_duplicate()
    {
        await using var mcp = await CreateMcpClientAsync();

        var projectSlug = $"kartimo-{Guid.NewGuid():N}";
        await SeedProjectAsync(projectSlug);
        var idempotencyKey = Guid.NewGuid().ToString();

        var args = new Dictionary<string, object?>
        {
            ["project"] = projectSlug,
            ["title"] = "Implement championship domain",
            ["objective"] = "Implement the approved domain model",
            ["acceptanceCriteria"] = new[] { "Unit tests" },
            ["idempotencyKey"] = idempotencyKey
        };

        var first = await CallSuccessAsync(mcp, "create_handoff", args);
        var second = await CallSuccessAsync(mcp, "create_handoff", args);

        Assert.Equal(first.GetProperty("id").GetGuid(), second.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task Save_decision_with_invalid_status_returns_validation_error()
    {
        await using var mcp = await CreateMcpClientAsync();

        var projectSlug = $"kartimo-{Guid.NewGuid():N}";
        await SeedProjectAsync(projectSlug);

        await AssertErrorAsync(mcp, "save_decision", new Dictionary<string, object?>
        {
            ["project"] = projectSlug,
            ["title"] = "Use PostgreSQL",
            ["rationale"] = "Rationale",
            ["status"] = "NotARealStatus",
            ["actor"] = "User"
        }, "VALIDATION_ERROR");
    }

    private async Task SeedProjectAsync(string slug)
    {
        using var scope = Factory.Services.CreateScope();
        var projects = scope.ServiceProvider.GetRequiredService<ProjectService>();
        await projects.CreateAsync(slug, "Kartimo", "Karting discovery platform");
    }

    private static async Task<JsonElement> CallSuccessAsync(McpClient client, string name, Dictionary<string, object?> args)
    {
        var result = await client.CallToolAsync(name, args);
        Assert.False(result.IsError ?? false, Describe(result));
        return result.StructuredContent ?? Deserialize(result);
    }

    private static async Task AssertErrorAsync(McpClient client, string name, Dictionary<string, object?> args, string expectedCode)
    {
        var result = await client.CallToolAsync(name, args);
        Assert.True(result.IsError ?? false, Describe(result));
        var json = Deserialize(result);
        Assert.Equal(expectedCode, json.GetProperty("error").GetProperty("code").GetString());
    }

    private static JsonElement Deserialize(CallToolResult result)
    {
        var text = result.Content.OfType<TextContentBlock>().First().Text;
        return JsonSerializer.Deserialize<JsonElement>(text);
    }

    private static string Describe(CallToolResult result) =>
        string.Join(" | ", result.Content.OfType<TextContentBlock>().Select(c => c.Text));
}
