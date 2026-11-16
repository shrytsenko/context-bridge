# ContextBridge

**Share context across AI assistants without starting from scratch.**

ContextBridge is a small MCP (Model Context Protocol) server that stores project
context — requirements, decisions, and handoffs — so you can discuss a project
in one AI assistant (e.g. ChatGPT) and continue implementing it in another
(e.g. Claude Code) without re-explaining everything.

## Status

🚧 Early development. Milestone M1 (solution bootstrap) is complete. See
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

Copy `.env.example` to `.env` to override the default PostgreSQL credentials.

## MCP client configuration

Point an MCP-capable client (e.g. Claude Code) at the Streamable HTTP endpoint:

```json
{
  "mcpServers": {
    "contextbridge": {
      "url": "http://localhost:8081/mcp"
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
