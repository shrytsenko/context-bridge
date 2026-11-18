using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Client;
using Testcontainers.PostgreSql;
using Xunit;

namespace ContextBridge.IntegrationTests;

public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected const string ApiKey = "test-api-key";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("contextbridge")
        .WithUsername("contextbridge")
        .WithPassword("contextbridge")
        .Build();

    protected WebApplicationFactory<Program> Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:ContextBridge"] = _postgres.GetConnectionString(),
                    ["Authentication:ApiKey"] = ApiKey
                });
            });
        });

        // Touch the factory's server once so Program's startup migration runs before tests resolve services.
        using var warmup = Factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        Factory.Dispose();
        await _postgres.DisposeAsync();
    }

    protected async Task<McpClient> CreateMcpClientAsync(CancellationToken cancellationToken = default)
    {
        var httpClient = Factory.CreateClient();
        var transport = new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Endpoint = new Uri(httpClient.BaseAddress!, "/mcp"),
                TransportMode = HttpTransportMode.StreamableHttp,
                AdditionalHeaders = new Dictionary<string, string> { ["X-Api-Key"] = ApiKey }
            },
            httpClient,
            null,
            true);

        return await McpClient.CreateAsync(transport, cancellationToken: cancellationToken);
    }
}
