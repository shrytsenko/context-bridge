using ContextBridge.Domain.Entities;
using ContextBridge.Domain.Exceptions;
using Xunit;

namespace ContextBridge.UnitTests.Entities;

public class ProjectTests
{
    [Fact]
    public void Create_sets_initial_version_and_timestamps()
    {
        var project = Project.Create("kartimo", "Kartimo", "Karting discovery platform");

        Assert.Equal("kartimo", project.Slug);
        Assert.Equal("Kartimo", project.Name);
        Assert.Equal(1, project.Version);
        Assert.Equal(project.CreatedAt, project.UpdatedAt);
    }

    [Theory]
    [InlineData("", "Kartimo")]
    [InlineData("kartimo", "")]
    [InlineData(null, "Kartimo")]
    public void Create_rejects_missing_required_fields(string? slug, string name)
    {
        Assert.Throws<DomainValidationException>(() => Project.Create(slug!, name, null));
    }

    [Fact]
    public void Update_with_correct_expected_version_increments_version()
    {
        var project = Project.Create("kartimo", "Kartimo", null);

        project.Update("Kartimo Updated", "New description", expectedVersion: 1);

        Assert.Equal("Kartimo Updated", project.Name);
        Assert.Equal(2, project.Version);
    }

    [Fact]
    public void Update_with_stale_expected_version_throws_concurrency_conflict()
    {
        var project = Project.Create("kartimo", "Kartimo", null);

        Assert.Throws<ConcurrencyConflictException>(() => project.Update("Kartimo Updated", null, expectedVersion: 99));
    }
}
