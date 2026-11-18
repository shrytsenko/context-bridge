# ContextBridge

**Share context across AI assistants without starting from scratch.**

ContextBridge is a small MCP (Model Context Protocol) server that stores project
context — requirements, decisions, and handoffs — so you can discuss a project
in one AI assistant (e.g. ChatGPT) and continue implementing it in another
(e.g. Claude Code) without re-explaining everything.

## Status

🚧 Early development. M1-M4 (solution bootstrap, domain & persistence, MCP
integration, full-text search & handoff hardening) are complete. See
[docs/mvp-scope.md](docs/mvp-scope.md) for the full milestone plan.

## Architecture

```text
ContextBridge.Api            ASP.NET Core host: MCP endpoint, health checks
  -> ContextBridge.Application  Use cases, validation, service contracts
  -> ContextBridge.Infrastructure  EF Core, PostgreSQL, full-text search
       -> ContextBridge.Application
       -> ContextBridge.Domain
ContextBridge.Application -> ContextBridge.Domain
ContextBridge.Domain          Entities, invariants — no infrastructure deps
```

See [docs/architecture.md](docs/architecture.md) for details and
[docs/decisions](docs/decisions) for ADRs.

## Quick start

```bash
docker compose up --build
```

This starts PostgreSQL and the API. Once healthy:

- Health check: `GET http://localhost:8081/health` → `Healthy`
- MCP endpoint (Streamable HTTP): `POST http://localhost:8081/mcp`

Copy `.env.example` to `.env` to override the default PostgreSQL credentials
and API key. The default dev key is `local-dev-key` (docker-compose) /
`dev-local-key` (`dotnet run` locally) — change `CONTEXTBRIDGE_API_KEY` /
`Authentication:ApiKey` for anything beyond local use, and never commit a
real key.

## MCP tools

| Tool | Mode | Purpose |
|---|---|---|
| `get_project_context` | Read | Compact snapshot: project metadata, recent context entries, decisions, handoffs. |
| `search_context` | Read | Full-text search (PostgreSQL `tsvector`/`ts_rank`) over a project's context entries, optionally filtered by type. |
| `save_context` | Write | Create (idempotent via `idempotencyKey`) or update (`expectedVersion`) a context entry. |
| `save_decision` | Write | Create (idempotent via `idempotencyKey`) or update (`expectedVersion`) a decision — status is always explicit, never silently accepted. |
| `create_handoff` | Write | Create an actionable task for another agent (idempotent via `idempotencyKey`). |
| `complete_handoff` | Write | Record a handoff's outcome (`Completed`/`Cancelled`). |

Errors use stable codes: `NOT_FOUND`, `VERSION_CONFLICT`, `VALIDATION_ERROR`.

## MCP client configuration

Point an MCP-capable client (e.g. Claude Code) at the Streamable HTTP endpoint,
with the API key as a header:

```json
{
  "mcpServers": {
    "contextbridge": {
      "url": "http://localhost:8081/mcp",
      "headers": {
        "X-Api-Key": "local-dev-key"
      }
    }
  }
}
```

## Development

```bash
dotnet build
dotnet test
```

Integration tests spin up a real PostgreSQL container via Testcontainers —
Docker must be running.

## Project layout

```text
src/            Api, Application, Domain, Infrastructure
tests/          UnitTests, IntegrationTests
docs/           Architecture notes and ADRs
```

## License

MIT (see [LICENSE](LICENSE)).
