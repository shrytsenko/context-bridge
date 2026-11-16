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
    public async Task Mcp_endpoint_is_mapped()
    {
        var client = Factory.CreateClient();

        var response = await client.PostAsync("/mcp", new StringContent(""));

        Assert.NotEqual(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }
}
