using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;
using Xunit;

namespace ContextBridge.IntegrationTests;

public sealed class SmokeTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("contextbridge")
        .WithUsername("contextbridge")
        .WithPassword("contextbridge")
        .Build();

    private WebApplicationFactory<Program>? _factory;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:ContextBridge"] = _postgres.GetConnectionString()
                });
            });
        });
    }

    public async Task DisposeAsync()
    {
        _factory?.Dispose();
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task Health_endpoint_reports_healthy_against_real_postgres()
    {
        var client = _factory!.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Mcp_endpoint_is_mapped()
    {
        var client = _factory!.CreateClient();

        var response = await client.PostAsync("/mcp", new StringContent(""));

        Assert.NotEqual(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }
}
