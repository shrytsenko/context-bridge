var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddHealthChecks()
    .AddNpgSql(sp => sp.GetRequiredService<IConfiguration>().GetConnectionString("ContextBridge")
        ?? throw new InvalidOperationException("Connection string 'ContextBridge' is not configured."),
        name: "postgresql");

builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();

var app = builder.Build();

app.MapHealthChecks("/health");

app.MapMcp("/mcp");

app.Run();

public partial class Program;
