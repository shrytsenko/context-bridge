using ContextBridge.Application.Abstractions;
using ContextBridge.Domain.Entities;
using ContextBridge.Domain.Enums;
using ContextBridge.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace ContextBridge.Application.Handoffs;

public sealed class HandoffService
{
    private readonly IContextBridgeDbContext _db;

    public HandoffService(IContextBridgeDbContext db)
    {
        _db = db;
    }

    public async Task<Handoff> CreateAsync(
        Guid projectId,
        string title,
        string objective,
        string acceptanceCriteriaJson,
        Actor? sourceAgent,
        Actor? targetAgent,
        CancellationToken ct = default)
    {
        var exists = await _db.Projects.AnyAsync(p => p.Id == projectId, ct);
        if (!exists)
        {
            throw new EntityNotFoundException(nameof(Project), projectId);
        }

        var handoff = Handoff.Create(projectId, title, objective, acceptanceCriteriaJson, sourceAgent, targetAgent);
        await _db.Handoffs.AddAsync(handoff, ct);
        await _db.SaveChangesAsync(ct);
        return handoff;
    }

    public async Task<Handoff> GetAsync(Guid id, CancellationToken ct = default)
    {
        var handoff = await _db.Handoffs.SingleOrDefaultAsync(h => h.Id == id, ct);
        return handoff ?? throw new EntityNotFoundException(nameof(Handoff), id);
    }

    public async Task<IReadOnlyList<Handoff>> ListByProjectAsync(Guid projectId, CancellationToken ct = default)
    {
        return await _db.Handoffs
            .Where(h => h.ProjectId == projectId)
            .OrderBy(h => h.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<Handoff> CompleteAsync(Guid id, HandoffStatus status, string result, int expectedVersion, CancellationToken ct = default)
    {
        var handoff = await GetAsync(id, ct);
        handoff.Complete(status, result, expectedVersion);
        await _db.SaveChangesAsync(ct);
        return handoff;
    }
}
