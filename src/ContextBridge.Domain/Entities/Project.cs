using ContextBridge.Domain.Exceptions;

namespace ContextBridge.Domain.Entities;

public sealed class Project
{
    public Guid Id { get; private set; }
    public string Slug { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public int Version { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Project()
    {
    }

    public static Project Create(string slug, string name, string? description)
    {
        Validate(slug, name);

        var now = DateTimeOffset.UtcNow;
        return new Project
        {
            Id = Guid.NewGuid(),
            Slug = slug.Trim(),
            Name = name.Trim(),
            Description = description,
            Version = 1,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Update(string name, string? description, int expectedVersion)
    {
        if (expectedVersion != Version)
        {
            throw new ConcurrencyConflictException(nameof(Project), Id, expectedVersion, Version);
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainValidationException("Project name is required.");
        }

        Name = name.Trim();
        Description = description;
        Version++;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static void Validate(string slug, string name)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new DomainValidationException("Project slug is required.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainValidationException("Project name is required.");
        }
    }
}
