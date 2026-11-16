using ContextBridge.Application.Abstractions;
using ContextBridge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ContextBridge.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddDbContext<ContextBridgeDbContext>((sp, options) =>
        {
            var connectionString = sp.GetRequiredService<IConfiguration>().GetConnectionString("ContextBridge")
                ?? throw new InvalidOperationException("Connection string 'ContextBridge' is not configured.");
            options.UseNpgsql(connectionString);
        });
        services.AddScoped<IContextBridgeDbContext>(sp => sp.GetRequiredService<ContextBridgeDbContext>());

        return services;
    }
}
