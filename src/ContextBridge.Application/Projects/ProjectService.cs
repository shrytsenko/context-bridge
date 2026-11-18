using ContextBridge.Application.Abstractions;
using ContextBridge.Domain.Entities;
using ContextBridge.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace ContextBridge.Application.Projects;

public sealed class ProjectService
{
    private readonly IContextBridgeDbContext _db;

    public ProjectService(IContextBridgeDbContext db)
    {
        _db = db;
    }

    public async Task<Project> CreateAsync(string slug, string name, string? description, CancellationToken ct = default)
    {
        var project = Project.Create(slug, name, description);
        await _db.Projects.AddAsync(project, ct);
        await _db.SaveChangesAsync(ct);
        return project;
    }

    public async Task<Project> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        var project = await _db.Projects.SingleOrDefaultAsync(p => p.Slug == slug, ct);
        return project ?? throw new EntityNotFoundException(nameof(Project), slug);
    }

    /// <summary>
    /// For write tools only: no MCP tool exists to bootstrap a project, so the first write
    /// for an unknown slug creates it (named after the slug) rather than failing.
    /// </summary>
    public async Task<Project> GetOrCreateBySlugAsync(string slug, CancellationToken ct = default)
    {
        var project = await _db.Projects.SingleOrDefaultAsync(p => p.Slug == slug, ct);
        return project ?? await CreateAsync(slug, slug, description: null, ct);
    }
}
