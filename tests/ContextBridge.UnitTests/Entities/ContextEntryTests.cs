using ContextBridge.Domain.Entities;
using ContextBridge.Domain.Enums;
using ContextBridge.Domain.Exceptions;
using ContextBridge.Domain.ValueObjects;
using Xunit;

namespace ContextBridge.UnitTests.Entities;

public class ContextEntryTests
{
    private static Source MakeSource() => new(Actor.ChatGpt, "conversation-123", DateTimeOffset.UtcNow);

    [Fact]
    public void Create_sets_initial_version()
    {
        var entry = ContextEntry.Create(Guid.NewGuid(), ContextEntryType.Architecture, "Backend architecture", "ASP.NET Core modular monolith", MakeSource());

        Assert.Equal(1, entry.Version);
        Assert.Equal(ContextEntryType.Architecture, entry.Type);
    }

    [Fact]
    public void Create_rejects_empty_content()
    {
        Assert.Throws<DomainValidationException>(() =>
            ContextEntry.Create(Guid.NewGuid(), ContextEntryType.Fact, "Title", "   ", MakeSource()));
    }

    [Fact]
    public void Update_with_correct_expected_version_increments_version()
    {
        var entry = ContextEntry.Create(Guid.NewGuid(), ContextEntryType.Fact, "Title", "Content", MakeSource());

        entry.Update("Updated title", "Updated content", MakeSource(), expectedVersion: 1);

        Assert.Equal("Updated title", entry.Title);
        Assert.Equal(2, entry.Version);
    }

    [Fact]
    public void Update_with_stale_expected_version_throws_concurrency_conflict()
    {
        var entry = ContextEntry.Create(Guid.NewGuid(), ContextEntryType.Fact, "Title", "Content", MakeSource());

        Assert.Throws<ConcurrencyConflictException>(() =>
            entry.Update("Updated title", "Updated content", MakeSource(), expectedVersion: 0));
    }
}
