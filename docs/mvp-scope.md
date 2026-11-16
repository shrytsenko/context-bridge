# MVP Scope

## Problem

Continue a project discussed in one AI assistant (e.g. ChatGPT) inside another
(e.g. Claude Code) without re-explaining approved context. Context belongs to
the user, not to one model vendor.

## In scope (v0.1)

- Five entities: `Project`, `ContextEntry`, `Decision`, `Handoff`,
  `ContextRevision`.
- Six MCP tools: `get_project_context`, `search_context`, `save_context`,
  `save_decision`, `create_handoff`, `complete_handoff`.
- PostgreSQL as source of truth, structured records with Markdown content.
- Local Docker Compose deployment; cloud-ready but not deployed yet.
- Optimistic concurrency (`Version`/`expectedVersion`), idempotency keys on
  writes, stable error codes.
- Local prototype auth: API key via environment variable.

## Out of scope (v0.1)

- Full chat history sync or private chat scraping.
- Web dashboard.
- Embeddings / RAG / vector database (pgvector deferred).
- Multi-user SaaS / multi-tenancy.
- Autonomous multi-agent orchestration.
- Automatic GitHub sync or Markdown export.
- Production OAuth.

## Milestones

| Milestone | Scope | Definition of Done |
|---|---|---|
| M1 | Solution bootstrap: 4 projects, Dockerfile, Compose, Postgres, health checks, MCP host scaffold + smoke test, CI | `docker compose up --build` runs API + DB successfully |
| M2 | Domain & persistence: entities, EF Core mappings/migrations, validation, concurrency, revisions | Context and decisions can be created, retrieved, updated; revisions persist |
| M3 | MCP integration: 6 tool contracts, structured output, API key auth, integration tests | Claude Code can read/write context via MCP |
| M4 | Search & handoff: full-text search, handoff lifecycle, idempotency, response-size limits | One agent creates a handoff, another retrieves and completes it |
| M5 | ChatGPT integration & demo: verify integration, fallback import, Kartimo demo, docs | Meaningful ChatGPT-discussed context becomes available to Claude Code without manual rewriting |

Full detail: see the project handoff document this repo was seeded from.
