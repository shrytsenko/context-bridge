using System.Net;
using Xunit;

namespace ContextBridge.IntegrationTests;

public sealed class SmokeTests : IntegrationTestBase
{
    [Fact]
    public async Task Health_endpoint_reports_healthy_against_real_postgres()
    {
        var client = Factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Mcp_endpoint_rejects_requests_without_a_valid_api_key()
    {
        var client = Factory.CreateClient();

        var response = await client.PostAsync("/mcp", new StringContent(""));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Mcp_endpoint_lists_all_six_approved_tools()
    {
        await using var mcpClient = await CreateMcpClientAsync();

        var tools = await mcpClient.ListToolsAsync();

        var toolNames = tools.Select(t => t.Name).ToHashSet();
        Assert.Equal(
            new HashSet<string>
            {
                "get_project_context",
                "search_context",
                "save_context",
                "save_decision",
                "create_handoff",
                "complete_handoff"
            },
            toolNames);
    }
}
