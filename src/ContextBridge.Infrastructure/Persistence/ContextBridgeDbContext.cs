using ContextBridge.Application.Abstractions;
using ContextBridge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ContextBridge.Infrastructure.Persistence;

public sealed class ContextBridgeDbContext : DbContext, IContextBridgeDbContext
{
    public ContextBridgeDbContext(DbContextOptions<ContextBridgeDbContext> options) : base(options)
    {
    }

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ContextEntry> ContextEntries => Set<ContextEntry>();
    public DbSet<Decision> Decisions => Set<Decision>();
    public DbSet<Handoff> Handoffs => Set<Handoff>();
    public DbSet<ContextRevision> ContextRevisions => Set<ContextRevision>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ContextBridgeDbContext).Assembly);
    }
}
