using ContextBridge.Domain.Entities;
using ContextBridge.Domain.Enums;
using ContextBridge.Domain.Exceptions;
using ContextBridge.Domain.ValueObjects;
using Xunit;

namespace ContextBridge.UnitTests.Entities;

public class DecisionTests
{
    private static Source MakeSource() => new(Actor.User, null, DateTimeOffset.UtcNow);

    [Fact]
    public void Create_requires_explicit_status()
    {
        var decision = Decision.Create(Guid.NewGuid(), "Use PostgreSQL", "Need full-text search and jsonb", DecisionStatus.Proposed, MakeSource());

        Assert.Equal(DecisionStatus.Proposed, decision.Status);
    }

    [Fact]
    public void Create_does_not_silently_default_to_accepted()
    {
        var decision = Decision.Create(Guid.NewGuid(), "Use PostgreSQL", "Need full-text search and jsonb", DecisionStatus.Proposed, MakeSource());

        Assert.NotEqual(DecisionStatus.Accepted, decision.Status);
    }

    [Fact]
    public void Update_with_correct_expected_version_changes_status()
    {
        var decision = Decision.Create(Guid.NewGuid(), "Use PostgreSQL", "Rationale", DecisionStatus.Proposed, MakeSource());

        decision.Update("Use PostgreSQL", "Rationale", DecisionStatus.Accepted, MakeSource(), expectedVersion: 1);

        Assert.Equal(DecisionStatus.Accepted, decision.Status);
        Assert.Equal(2, decision.Version);
    }

    [Fact]
    public void Update_with_stale_expected_version_throws_concurrency_conflict()
    {
        var decision = Decision.Create(Guid.NewGuid(), "Use PostgreSQL", "Rationale", DecisionStatus.Proposed, MakeSource());

        Assert.Throws<ConcurrencyConflictException>(() =>
            decision.Update("Use PostgreSQL", "Rationale", DecisionStatus.Accepted, MakeSource(), expectedVersion: 5));
    }
}
