using ContextBridge.Application.Abstractions;
using ContextBridge.Application.ContextEntries;
using ContextBridge.Application.Decisions;
using ContextBridge.Application.Projects;
using ContextBridge.Domain.Entities;
using ContextBridge.Domain.Enums;
using ContextBridge.Domain.Exceptions;
using ContextBridge.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ContextBridge.IntegrationTests;

public sealed class PersistenceTests : IntegrationTestBase
{
    private static Source MakeSource() => new(Actor.ChatGpt, "conversation-123", DateTimeOffset.UtcNow);

    [Fact]
    public async Task Context_entry_can_be_created_retrieved_and_updated_with_revisions_persisted()
    {
        using var scope = Factory.Services.CreateScope();
        var projects = scope.ServiceProvider.GetRequiredService<ProjectService>();
        var entries = scope.ServiceProvider.GetRequiredService<ContextEntryService>();
        var db = scope.ServiceProvider.GetRequiredService<IContextBridgeDbContext>();

        var project = await projects.CreateAsync("kartimo", "Kartimo", "Karting discovery platform");

        var created = await entries.CreateAsync(
            project.Id, ContextEntryType.Architecture, "Backend architecture", "ASP.NET Core modular monolith", MakeSource());

        var fetched = await entries.GetAsync(created.Id);
        Assert.Equal("Backend architecture", fetched.Title);
        Assert.Equal(1, fetched.Version);

        var updated = await entries.UpdateAsync(
            created.Id, "Backend architecture v2", "ASP.NET Core modular monolith + PostgreSQL", MakeSource(), expectedVersion: 1);
        Assert.Equal(2, updated.Version);

        var revisionCount = await db.ContextRevisions
            .Where(r => r.EntityType == nameof(ContextEntry) && r.EntityId == created.Id)
            .CountAsync();
        Assert.Equal(2, revisionCount);

        await Assert.ThrowsAsync<ConcurrencyConflictException>(() =>
            entries.UpdateAsync(created.Id, "Stale update", "Stale content", MakeSource(), expectedVersion: 1));
    }

    [Fact]
    public async Task Decision_can_be_created_retrieved_and_updated_without_silently_becoming_accepted()
    {
        using var scope = Factory.Services.CreateScope();
        var projects = scope.ServiceProvider.GetRequiredService<ProjectService>();
        var decisions = scope.ServiceProvider.GetRequiredService<DecisionService>();
        var db = scope.ServiceProvider.GetRequiredService<IContextBridgeDbContext>();

        var project = await projects.CreateAsync("kartimo-2", "Kartimo", null);

        var created = await decisions.CreateAsync(
            project.Id, "Use PostgreSQL", "Need full-text search and jsonb", DecisionStatus.Proposed, MakeSource());
        Assert.Equal(DecisionStatus.Proposed, created.Status);

        var accepted = await decisions.UpdateAsync(
            created.Id, created.Title, created.Rationale, DecisionStatus.Accepted, MakeSource(), expectedVersion: 1);
        Assert.Equal(DecisionStatus.Accepted, accepted.Status);

        var fetched = await decisions.GetAsync(created.Id);
        Assert.Equal(DecisionStatus.Accepted, fetched.Status);

        var revisionCount = await db.ContextRevisions
            .Where(r => r.EntityType == nameof(Decision) && r.EntityId == created.Id)
            .CountAsync();
        Assert.Equal(2, revisionCount);
    }

    [Fact]
    public async Task Creating_context_entry_for_unknown_project_throws_not_found()
    {
        using var scope = Factory.Services.CreateScope();
        var entries = scope.ServiceProvider.GetRequiredService<ContextEntryService>();

        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            entries.CreateAsync(Guid.NewGuid(), ContextEntryType.Fact, "Title", "Content", MakeSource()));
    }
}
