# ContextBridge

**Share context across AI assistants without starting from scratch.**

ContextBridge is a small MCP (Model Context Protocol) server that stores project
context — requirements, decisions, and handoffs — so you can discuss a project
in one AI assistant (e.g. ChatGPT) and continue implementing it in another
(e.g. Claude Code) without re-explaining everything.

## Status

🚧 Early development. M1-M5 (solution bootstrap, domain & persistence, MCP
integration, full-text search & handoff hardening, ChatGPT fallback import)
are complete. See [docs/mvp-scope.md](docs/mvp-scope.md) for the full
milestone plan.

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

## Continuing a ChatGPT discussion in Claude Code

The point of ContextBridge: discuss a project in ChatGPT, save the approved
context, then in Claude Code say something like *"Get the latest Kartimo
context from ContextBridge and continue implementation."* Claude Code calls
`get_project_context` / `search_context` itself once it's configured as an
MCP client (see above) — no copy-pasting required on that side.

Getting the context *into* ContextBridge from a ChatGPT conversation has two
paths. **Check which one applies to your account before relying on it** —
MCP write-tool access from ChatGPT is plan-gated and still rolling out as of
late 2026 (OpenAI's own docs describe it as beta, limited to Business/
Enterprise/Edu workspaces at the time of writing, with Plus/Pro support
reported inconsistently across sources):

1. **Direct MCP** — if your ChatGPT plan/workspace supports custom
   connectors with write access (Settings → Connectors in ChatGPT), add
   ContextBridge's `/mcp` endpoint there the same way you would for Claude
   Code, and ChatGPT can call `save_context` / `save_decision` /
   `create_handoff` directly, with its own confirmation prompts for writes.

2. **Fallback import (always available)** — ask ChatGPT to summarize the
   discussion as a JSON bundle matching
   [docs/examples/kartimo-handoff.json](docs/examples/kartimo-handoff.json),
   save it to a file, then run:

   ```bash
   dotnet run --project tools/ContextBridge.Import -- path/to/bundle.json \
     --url http://localhost:8081/mcp --api-key local-dev-key
   ```

   The importer calls the same `save_context` / `save_decision` /
   `create_handoff` tools Claude Code would use, with a content-derived
   `idempotencyKey` — re-running the same bundle is safe and won't create
   duplicates. It prints the resulting `get_project_context` snapshot so you
   can see exactly what Claude Code will see.

Either path lands in the same PostgreSQL store, so the backend and the
Claude Code side work the same regardless of which one your ChatGPT account
supports.

### Worked example: Kartimo

[docs/examples/kartimo-handoff.json](docs/examples/kartimo-handoff.json) is a
real bundle for Kartimo (a karting discovery platform) — the sample project
this repo was seeded to demonstrate on. With the stack running:

```bash
dotnet run --project tools/ContextBridge.Import -- docs/examples/kartimo-handoff.json \
  --url http://localhost:8081/mcp --api-key local-dev-key
```

creates the `kartimo` project (projects are created automatically on first
write — there's no separate bootstrap tool) with 4 context entries, 2
accepted decisions, and 1 pending handoff ("Implement championship domain").
From there, telling Claude Code *"get the kartimo context from ContextBridge
and implement the championship domain handoff"* is the whole point: it reads
that handoff via `get_project_context`, implements it, then would call
`complete_handoff` to report back.

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
tools/          ContextBridge.Import — ChatGPT fallback import CLI
tests/          UnitTests, IntegrationTests
docs/           Architecture notes, ADRs, and example bundles
```

## License

MIT (see [LICENSE](LICENSE)).
