# Architecture

## Layers

```text
ContextBridge.Api
  -> ContextBridge.Application
  -> ContextBridge.Infrastructure
       -> ContextBridge.Application
       -> ContextBridge.Domain
ContextBridge.Application -> ContextBridge.Domain
ContextBridge.Domain -> (no infrastructure dependencies)
```

- **Api**: ASP.NET Core host. Wires up the MCP Streamable HTTP endpoint
  (`/mcp`), health checks (`/health`), DI, and auth.
- **Application**: use cases and validation. No MediatR or generic
  repositories — plain service classes.
- **Domain**: entities, invariants, status transitions. No EF Core or other
  infrastructure types leak in here.
- **Infrastructure**: EF Core `DbContext`, PostgreSQL mappings/migrations,
  full-text search.

This is a Clean-Architecture-inspired modular monolith, not a microservice
split — the four-project layout is a portfolio-oriented choice and may be
more separation than a service this size strictly needs.

## Persistence

- PostgreSQL, EF Core. UUID primary keys.
- Markdown content stored in `text` columns; `jsonb` for revision snapshots
  and structured acceptance criteria.
- Full-text search via PostgreSQL `tsvector` + GIN index (English first;
  `simple` config for Ukrainian/mixed-language content). `pgvector` deferred.
- Indexes on `ProjectId`, `Status`, `UpdatedAt`.

## MCP integration

- Official `ModelContextProtocol` / `ModelContextProtocol.AspNetCore` C# SDK.
- Streamable HTTP transport, mapped at `/mcp` via `WithHttpTransport()` +
  `MapMcp()`.
- Tools registered via `WithToolsFromAssembly()` and `[McpServerToolType]` /
  `[McpServerTool]` attributes.
- Six tools (see [mvp-scope.md](mvp-scope.md)); writes accept an
  `idempotencyKey`, updates use `expectedVersion` for optimistic concurrency.
- Stable machine-readable error codes: `NOT_FOUND`, `VERSION_CONFLICT`,
  `VALIDATION_ERROR`, `FORBIDDEN`.

## Security

- Local prototype: API key from environment variable, never committed.
- Stored content is treated as untrusted data, not executable instructions —
  guards against prompt injection via retrieved context.
- Remote/cloud deployment (future): HTTPS, OAuth-compatible MCP client auth,
  project-scoped authorization, rate limiting, audit logging.

## Testing

- Unit tests: domain invariants, decision/handoff status transitions,
  validation.
- Integration tests: EF Core against a real PostgreSQL instance via
  Testcontainers (never EF Core InMemory for PostgreSQL-specific behavior).
- MCP tests: tool discovery, schemas, read/write behavior, structured errors.
