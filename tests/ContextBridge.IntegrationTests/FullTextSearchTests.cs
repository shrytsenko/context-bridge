using ContextBridge.Application.ContextEntries;
using ContextBridge.Application.Projects;
using ContextBridge.Domain.Enums;
using ContextBridge.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ContextBridge.IntegrationTests;

public sealed class FullTextSearchTests : IntegrationTestBase
{
    private static Source MakeSource() => new(Actor.ChatGpt, null, DateTimeOffset.UtcNow);

    [Fact]
    public async Task SearchAsync_finds_entries_by_word_regardless_of_order_and_ranks_best_match_first()
    {
        using var scope = Factory.Services.CreateScope();
        var projects = scope.ServiceProvider.GetRequiredService<ProjectService>();
        var entries = scope.ServiceProvider.GetRequiredService<ContextEntryService>();

        var project = await projects.CreateAsync($"kartimo-{Guid.NewGuid():N}", "Kartimo", null);

        var strongMatch = await entries.CreateAsync(
            project.Id, ContextEntryType.Architecture, "Backend architecture",
            "ASP.NET Core modular monolith backend, PostgreSQL persistence.", MakeSource());
        var weakMatch = await entries.CreateAsync(
            project.Id, ContextEntryType.Fact, "Unrelated fact",
            "The only mention of backend here is this single word.", MakeSource());
        await entries.CreateAsync(
            project.Id, ContextEntryType.Fact, "Totally unrelated",
            "Nothing to do with the search terms at all.", MakeSource());

        var results = await entries.SearchAsync(project.Id, "backend", types: null, limit: 20);

        Assert.Equal(2, results.Count);
        Assert.Equal(strongMatch.Id, results[0].Id);
        Assert.Equal(weakMatch.Id, results[1].Id);
    }

    [Fact]
    public async Task SearchAsync_filters_by_entry_type()
    {
        using var scope = Factory.Services.CreateScope();
        var projects = scope.ServiceProvider.GetRequiredService<ProjectService>();
        var entries = scope.ServiceProvider.GetRequiredService<ContextEntryService>();

        var project = await projects.CreateAsync($"kartimo-{Guid.NewGuid():N}", "Kartimo", null);

        var decision = await entries.CreateAsync(
            project.Id, ContextEntryType.Architecture, "Storage choice", "Use PostgreSQL for storage.", MakeSource());
        await entries.CreateAsync(
            project.Id, ContextEntryType.Fact, "Storage note", "Use PostgreSQL for storage too.", MakeSource());

        var results = await entries.SearchAsync(
            project.Id, "postgresql storage", types: [ContextEntryType.Architecture], limit: 20);

        Assert.Single(results);
        Assert.Equal(decision.Id, results[0].Id);
    }
}
