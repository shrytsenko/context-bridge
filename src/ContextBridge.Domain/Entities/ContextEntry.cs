using ContextBridge.Domain.Enums;
using ContextBridge.Domain.Exceptions;
using ContextBridge.Domain.ValueObjects;

namespace ContextBridge.Domain.Entities;

public sealed class ContextEntry
{
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public ContextEntryType Type { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public Source Source { get; private set; } = null!;
    public int Version { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private ContextEntry()
    {
    }

    public static ContextEntry Create(Guid projectId, ContextEntryType type, string title, string content, Source source)
    {
        Validate(title, content);

        var now = DateTimeOffset.UtcNow;
        return new ContextEntry
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Type = type,
            Title = title.Trim(),
            Content = content,
            Source = source,
            Version = 1,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Update(string title, string content, Source source, int expectedVersion)
    {
        if (expectedVersion != Version)
        {
            throw new ConcurrencyConflictException(nameof(ContextEntry), Id, expectedVersion, Version);
        }

        Validate(title, content);

        Title = title.Trim();
        Content = content;
        Source = source;
        Version++;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static void Validate(string title, string content)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainValidationException("Context entry title is required.");
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new DomainValidationException("Context entry content is required.");
        }
    }
}
