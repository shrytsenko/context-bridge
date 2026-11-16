using ContextBridge.Application.Abstractions;
using ContextBridge.Application.Revisions;
using ContextBridge.Domain.Entities;
using ContextBridge.Domain.Enums;
using ContextBridge.Domain.Exceptions;
using ContextBridge.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace ContextBridge.Application.ContextEntries;

public sealed class ContextEntryService
{
    private readonly IContextBridgeDbContext _db;

    public ContextEntryService(IContextBridgeDbContext db)
    {
        _db = db;
    }

    public async Task<ContextEntry> CreateAsync(
        Guid projectId,
        ContextEntryType type,
        string title,
        string content,
        Source source,
        CancellationToken ct = default)
    {
        await EnsureProjectExistsAsync(projectId, ct);

        var entry = ContextEntry.Create(projectId, type, title, content, source);
        await _db.ContextEntries.AddAsync(entry, ct);
        await RecordRevisionAsync(entry, ct);
        await _db.SaveChangesAsync(ct);
        return entry;
    }

    public async Task<ContextEntry> GetAsync(Guid id, CancellationToken ct = default)
    {
        var entry = await _db.ContextEntries.SingleOrDefaultAsync(e => e.Id == id, ct);
        return entry ?? throw new EntityNotFoundException(nameof(ContextEntry), id);
    }

    public async Task<IReadOnlyList<ContextEntry>> ListByProjectAsync(Guid projectId, CancellationToken ct = default)
    {
        return await _db.ContextEntries
            .Where(e => e.ProjectId == projectId)
            .OrderBy(e => e.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<ContextEntry> UpdateAsync(
        Guid id,
        string title,
        string content,
        Source source,
        int expectedVersion,
        CancellationToken ct = default)
    {
        var entry = await GetAsync(id, ct);
        entry.Update(title, content, source, expectedVersion);
        await RecordRevisionAsync(entry, ct);
        await _db.SaveChangesAsync(ct);
        return entry;
    }

    private async Task EnsureProjectExistsAsync(Guid projectId, CancellationToken ct)
    {
        var exists = await _db.Projects.AnyAsync(p => p.Id == projectId, ct);
        if (!exists)
        {
            throw new EntityNotFoundException(nameof(Project), projectId);
        }
    }

    private async Task RecordRevisionAsync(ContextEntry entry, CancellationToken ct)
    {
        var revision = ContextRevision.Capture(
            entry.ProjectId,
            nameof(ContextEntry),
            entry.Id,
            entry.Version,
            RevisionSnapshot.ToJson(entry));
        await _db.ContextRevisions.AddAsync(revision, ct);
    }
}
