using ContextBridge.Application.ContextEntries;
using ContextBridge.Application.Decisions;
using ContextBridge.Application.Projects;
using Microsoft.Extensions.DependencyInjection;

namespace ContextBridge.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ProjectService>();
        services.AddScoped<ContextEntryService>();
        services.AddScoped<DecisionService>();
        return services;
    }
}
