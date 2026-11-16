using ContextBridge.Application.Abstractions;
using ContextBridge.Application.Revisions;
using ContextBridge.Domain.Entities;
using ContextBridge.Domain.Enums;
using ContextBridge.Domain.Exceptions;
using ContextBridge.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace ContextBridge.Application.Decisions;

public sealed class DecisionService
{
    private readonly IContextBridgeDbContext _db;

    public DecisionService(IContextBridgeDbContext db)
    {
        _db = db;
    }

    public async Task<Decision> CreateAsync(
        Guid projectId,
        string title,
        string rationale,
        DecisionStatus status,
        Source source,
        CancellationToken ct = default)
    {
        await EnsureProjectExistsAsync(projectId, ct);

        var decision = Decision.Create(projectId, title, rationale, status, source);
        await _db.Decisions.AddAsync(decision, ct);
        await RecordRevisionAsync(decision, ct);
        await _db.SaveChangesAsync(ct);
        return decision;
    }

    public async Task<Decision> GetAsync(Guid id, CancellationToken ct = default)
    {
        var decision = await _db.Decisions.SingleOrDefaultAsync(d => d.Id == id, ct);
        return decision ?? throw new EntityNotFoundException(nameof(Decision), id);
    }

    public async Task<IReadOnlyList<Decision>> ListByProjectAsync(Guid projectId, CancellationToken ct = default)
    {
        return await _db.Decisions
            .Where(d => d.ProjectId == projectId)
            .OrderBy(d => d.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<Decision> UpdateAsync(
        Guid id,
        string title,
        string rationale,
        DecisionStatus status,
        Source source,
        int expectedVersion,
        CancellationToken ct = default)
    {
        var decision = await GetAsync(id, ct);
        decision.Update(title, rationale, status, source, expectedVersion);
        await RecordRevisionAsync(decision, ct);
        await _db.SaveChangesAsync(ct);
        return decision;
    }

    private async Task EnsureProjectExistsAsync(Guid projectId, CancellationToken ct)
    {
        var exists = await _db.Projects.AnyAsync(p => p.Id == projectId, ct);
        if (!exists)
        {
            throw new EntityNotFoundException(nameof(Project), projectId);
        }
    }

    private async Task RecordRevisionAsync(Decision decision, CancellationToken ct)
    {
        var revision = ContextRevision.Capture(
            decision.ProjectId,
            nameof(Decision),
            decision.Id,
            decision.Version,
            RevisionSnapshot.ToJson(decision));
        await _db.ContextRevisions.AddAsync(revision, ct);
    }
}
