using ContextBridge.Application;
using ContextBridge.Infrastructure;
using ContextBridge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure();

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

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<ContextBridgeDbContext>().Database.MigrateAsync();
}

app.MapHealthChecks("/health");

app.MapMcp("/mcp");

app.Run();

public partial class Program;
