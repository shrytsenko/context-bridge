# Agent notes

This repo is a .NET 10 / C# modular monolith exposing an MCP server. See
[README.md](README.md), [docs/architecture.md](docs/architecture.md), and
[docs/mvp-scope.md](docs/mvp-scope.md) before making changes.

## Conventions

- Dependency direction: `Api -> Application -> Infrastructure -> Domain`
  (see architecture doc). `Domain` has no infrastructure dependencies.
- No MediatR, no generic repository abstraction — plain service classes.
- Integration tests use Testcontainers against real PostgreSQL, never EF
  Core InMemory, for anything PostgreSQL-specific.
- MCP tools live in `src/ContextBridge.Api/Tools`, one `[McpServerToolType]`
  class per tool group, discovered via `WithToolsFromAssembly()`.
- Writes accept an `idempotencyKey`; updates use `expectedVersion` for
  optimistic concurrency. Stable error codes: `NOT_FOUND`,
  `VERSION_CONFLICT`, `VALIDATION_ERROR`, `FORBIDDEN`.

## Verifying changes

```bash
dotnet build
dotnet test
docker compose up --build
```

Implement one milestone at a time (see docs/mvp-scope.md); confirm scope
changes with the user before expanding beyond the current milestone.
