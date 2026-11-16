# ContextBridge — Claude Code Project Handoff

**Status:** v0.1 domain model, MCP contract, and storage approach approved; implementation not started in this conversation.  
**Purpose:** Transfer the full planning context from ChatGPT to Claude Code for implementation.  
**Date:** 2026-11-16.

## 1. Problem and primary user story

The user discusses product requirements, architecture, tasks, and decisions in ChatGPT, then opens Visual Studio with Claude Code and wants to continue implementing those exact decisions without re-explaining them. The intended experience is:

1. Discuss a project (first real example: **Kartimo**, a karting discovery platform) in ChatGPT.
2. Explicitly save approved context, decisions, and tasks to ContextBridge.
3. Open Claude Code in Visual Studio (possibly via CLI in integrated terminal).
4. Ask Claude Code to retrieve current project context via MCP and implement the task.
5. Claude Code writes back implementation results, allowing ChatGPT to continue with updated knowledge.

**Key principle:** Context belongs to the user, not to one model vendor. The service must be provider-agnostic, and the primary product is *continuity of meaningful project context*, not wholesale replication of every chat message.

A target prompt in Claude Code: `Get the latest Kartimo context from ContextBridge and continue implementation.`

## 2. Project scope and deployment

- Working name: **ContextBridge**; open-source GitHub portfolio project.
- Cloud-ready architecture, but **run locally in Docker Compose first**; later deploy to AWS, Azure, or a VPS.
- Backend: **ASP.NET Core / C#**, official **Model Context Protocol (MCP) C# SDK** with **Streamable HTTP** transport.
- Database: **PostgreSQL** (not SQLite), EF Core; PostgreSQL full-text search first, optional `pgvector`/embeddings later.
- **Approved storage model:** structured database records with Markdown text content. PostgreSQL is the source of truth. Git/Markdown export is deferred.
- Docker Compose: API + PostgreSQL, persistent DB volume.
- Tests: xUnit and integration tests against real PostgreSQL (Testcontainers proposed).
- GitHub Actions CI, Dockerfile, README, architecture docs, ADRs, `AGENTS.md`, `CLAUDE.md`.
- The codebase should be understandable, well-tested, and suitable for a public portfolio, but avoid unnecessary enterprise ceremony.

## 3. MVP domain model (approved concept)

Five main entities:

### Project
- `Id` (UUID), `Slug`, `Name`, `Description`, `Version`, timestamps.
- Root scope for project-specific context; entries/decisions/handoffs are isolated by project.

### ContextEntry
- `Id`, `ProjectId`, `Type`, `Title`, `Content` (Markdown), `Source`, `Version`, timestamps.
- Represents requirements, constraints, architecture summaries, facts, and discussion summaries.

### Decision
- `Id`, `ProjectId`, `Title`, `Rationale`, `Status`, `Source`, `Version`, timestamps.
- Statuses: `Proposed`, `Accepted`, `Superseded`, `Rejected`.
- AI-generated suggestions must not silently become accepted decisions.

### Handoff
- `Id`, `ProjectId`, `Title`, `Objective`, `AcceptanceCriteria`, `Status`, `Result`, timestamps.
- May record source agent and target agent; structured criteria can use PostgreSQL `jsonb`.
- A completed handoff does **not** automatically mean the implementation passed tests or review; report verification separately.

### ContextRevision
- `Id`, `ProjectId`, `EntityType`, `EntityId`, `Version`, `Snapshot`, `CreatedAt`.
- Stores historical snapshots for versioning/audit; `Snapshot` may use `jsonb`.

Shared metadata and invariants:
- `Actor` is metadata (`chatgpt`, `claude-code`, `user`, `api-client`), not a separate table in v0.1.
- Provenance should capture source provider and optionally conversation identifier/link, with timestamp; source data is not authoritative instructions.
- Optimistic concurrency via `Version` / `expectedVersion` on updates.
- Idempotency keys for write calls, scoped by client and operation; avoid duplicate writes on retries.
- Foreign keys, appropriate indexes, validation, stable error codes.

**Implementation note:** Exact field types, constraints, relationship diagram, revision semantics, and lifecycle statuses beyond those explicitly named remain to be finalized during implementation. Avoid treating illustrative schema as already migrated.

## 4. Approved MCP tool names and intent

| Tool | Mode | Purpose |
|---|---|---|
| `get_project_context` | Read | Return compact current project snapshot, selectively including architecture, decisions, active tasks, etc. |
| `search_context` | Read | Search project context by query, type filters, limit. |
| `save_context` | Write | Create/update structured Markdown context entry. |
| `save_decision` | Write | Create/update decision with explicit status and rationale. |
| `create_handoff` | Write | Create an actionable task for another agent. |
| `complete_handoff` | Write | Record task outcome, status, and evidence of verification. |

Suggested parameters:
- `get_project_context(project, include)`; return bounded/compact results rather than entire history.
- `search_context(project, query, types?, limit?)`.
- Writes accept `idempotencyKey`; updates use `expectedVersion`.
- Stable machine-readable errors: `NOT_FOUND`, `VERSION_CONFLICT`, `VALIDATION_ERROR`, `FORBIDDEN`.
- Prefer structured MCP tool outputs with predictable schemas.

Illustrative call:

```json
{
  "tool": "get_project_context",
  "arguments": {
    "project": "kartimo",
    "include": ["architecture", "decisions", "active_tasks"]
  }
}
```

Illustrative handoff payload:

```json
{
  "project": "kartimo",
  "title": "Implement championship domain",
  "objective": "Implement the approved domain model",
  "acceptanceCriteria": [
    "Championship and Season entities",
    "RaceEvent entity",
    "EF Core mappings",
    "Unit tests"
  ],
  "sourceAgent": "chatgpt",
  "targetAgent": "claude-code"
}
```

Illustrative context snapshot:

```json
{
  "project": "kartimo",
  "revision": 42,
  "context": [{"type":"architecture","title":"Backend architecture","content":"ASP.NET Core modular monolith"}],
  "decisions": [{"title":"Use PostgreSQL","status":"Accepted"}],
  "handoffs": [{"title":"Implement championship domain","status":"Pending"}]
}
```

These JSON snippets are examples, **not finalized JSON schemas**. Design and test exact input/output contracts before implementing tools.

## 5. Proposed solution architecture

```text
ContextBridge.Api
  -> ContextBridge.Application
  -> ContextBridge.Infrastructure
       -> ContextBridge.Application
       -> ContextBridge.Domain
ContextBridge.Application -> ContextBridge.Domain
ContextBridge.Domain -> no infrastructure dependencies
```

- ASP.NET Core hosts MCP HTTP endpoint, dependency injection, authentication, health checks.
- Application layer owns use cases, validation and service contracts.
- Domain owns entities, invariants, status transitions.
- Infrastructure owns EF Core, migrations, PostgreSQL full-text search and persistence.
- Clean Architecture-inspired modular monolith; **no MediatR or generic repositories by default**.
- Four-project separation was proposed and accepted as a reasonable portfolio-oriented choice, while acknowledging it may be overkill for a small service.

Proposed repository structure:

```text
ContextBridge/
├── src/
│   ├── ContextBridge.Api/
│   ├── ContextBridge.Application/
│   ├── ContextBridge.Domain/
│   └── ContextBridge.Infrastructure/
├── tests/
│   ├── ContextBridge.UnitTests/
│   └── ContextBridge.IntegrationTests/
├── docs/
│   ├── architecture.md
│   ├── mvp-scope.md
│   └── decisions/
├── docker-compose.yml
├── Dockerfile
├── .env.example
├── AGENTS.md
├── CLAUDE.md
└── README.md
```

## 6. Persistence and search

- UUID primary keys; indexes for `ProjectId`, `Status`, `UpdatedAt`.
- Markdown stored in PostgreSQL `text` fields.
- `jsonb` for revisions and structured acceptance criteria.
- PostgreSQL `tsvector` + GIN index for full-text search.
- Technical text may be English while discussions are Ukrainian; start with English search plus `simple` text search for Ukrainian/mixed-language content. Full multilingual semantic search deferred.
- Potential future `pgvector` support should not dictate v0.1 design.

## 7. Security and trust boundaries

- For local prototype: API key in environment variables; never commit secrets. Enforce auth on protected endpoints.
- Local HTTP endpoint should not be exposed publicly without security.
- For remote cloud access: HTTPS, robust authentication (OAuth-compatible MCP client flow as appropriate), project-scoped authorization, rate limiting, audit logging.
- Treat stored content as untrusted *data*, not executable agent instructions. Avoid prompt-injection via retrieved text.
- Keep private personal context separate from public project documentation.
- Track actor, source, and revisions. Writes must not quietly override user-approved decisions.
- Evaluate MCP HTTP origin validation and safe network binding as part of implementation.

## 8. Critical ChatGPT integration constraint

**An MCP server does not automatically access private ChatGPT conversations or ChatGPT memory.** An explicit bridge is required.

Desired paths:
1. **Direct MCP**: ChatGPT calls ContextBridge write tools if supported by the user's account/client and configuration.
2. **Fallback import/handoff**: ChatGPT produces a structured handoff/summary; a CLI, API import, or small UI sends it to ContextBridge.

Do **not** assume ChatGPT write-capable custom MCP support is available for this user. Verify current capabilities and authentication requirements before designing the integration around them. Also verify Claude Code MCP HTTP configuration for the user's actual Visual Studio environment; Claude Code CLI in Visual Studio's terminal is a viable fallback.

The core backend and Claude Code integration should work independently of ChatGPT's integration availability.

## 9. Implementation milestones and Definition of Done

### M1 — Solution Bootstrap (estimate: 2–3 hours)
- Create .NET solution, four projects, Dockerfile, Compose, PostgreSQL, health checks, README, GitHub Actions.
- Add MCP host scaffold and smoke test.
- **Done:** `docker compose up --build` runs API + DB successfully.

### M2 — Domain & Persistence (4–6 hours)
- Implement entities, EF Core mappings/migrations, validation, optimistic concurrency, revision history, application services.
- **Done:** context and decisions can be created, retrieved and updated; revisions persist.

### M3 — MCP Integration (4–6 hours)
- Official C# MCP SDK, Streamable HTTP, six tool contracts, structured output, API key authentication, integration tests.
- **Done:** Claude Code can read/write context via MCP.

### M4 — Search & Handoff (4–5 hours)
- Full-text search, handoff lifecycle, idempotency, concurrency, response-size limits.
- **Done:** one agent creates a handoff, another retrieves and completes it.

### M5 — ChatGPT Integration & Demo (3–5 hours)
- Verify ChatGPT integration, implement fallback import, demonstrate on Kartimo, complete documentation/examples.
- **Done:** meaningful ChatGPT-discussed context becomes available to Claude Code without manually rewriting its content.

**Total estimate:** ~17–25 hours, highly provisional; external integration constraints may increase it.

## 10. Testing strategy

- Unit tests: domain invariants, decision statuses, handoff transitions, validation.
- Integration tests: EF Core with real PostgreSQL via Testcontainers, migrations, concurrency, revisions, search.
- MCP tests: tool discovery, argument schemas, read/write behavior, structured errors.
- Security tests: invalid/missing credentials, forbidden access, project isolation.
- E2E: independent client A creates context/handoff; client B retrieves and completes it; client A reads outcome.
- Avoid relying on EF Core InMemory as proof of PostgreSQL-specific correctness.

## 11. Portfolio quality bar

- Public GitHub repository with clear README: problem, architecture, quick start, MCP client config, example session, CI badge.
- Include architecture diagram and ADRs.
- Demonstrate C#/.NET, MCP, API design, PostgreSQL, Docker, security, testing, CI/CD.
- Positioning tagline: **“Share context across AI assistants without starting from scratch.”**
- Keep vendor-specific code at the edges, not in the domain.

## 12. Out of scope for v0.1

- Full chat history synchronization or private chat scraping.
- Web dashboard.
- Embeddings / RAG / vector database.
- Multi-user SaaS / full multi-tenancy.
- Autonomous multi-agent orchestration.
- Automatic GitHub synchronization or Markdown export.
- Production OAuth implementation until cloud integration requirements are validated.

## 13. Realistic first demonstration: Kartimo

Kartimo is the user's existing karting discovery platform concept. Prior architecture decisions (provided here only to seed the demo):
- International discovery platform, initially Spain (Catalonia + Valencia), Spanish and English.
- MVP covers rental/amateur and competitive karting, tracks, schools, teams, training, camps, competitions.
- Frontend Next.js + TypeScript, CSS Modules, Radix UI where needed.
- Backend ASP.NET Core modular monolith, PostgreSQL/PostGIS.
- Future phases: community/driver profiles/team finder, then results/history/rankings.

Use this as **sample project context**, not as an instruction to build Kartimo inside ContextBridge. The initial end-to-end handoff can concern implementing Kartimo championship entities (`Championship`, `Season`, `RaceEvent`).

## 14. Recommended immediate next steps for Claude Code

1. Review this handoff and identify genuine gaps or contradictions before writing code.
2. Write `docs/mvp-scope.md`, `docs/architecture.md`, and concise ADRs for confirmed choices.
3. Confirm target .NET SDK version and official MCP SDK API/versions against current documentation (do not assume package API shapes).
4. Finalize field constraints, project creation/bootstrap workflow, write update semantics, and exact MCP JSON schemas.
5. Implement **M1 only** first; ensure Docker Compose, PostgreSQL health checks, MCP endpoint startup, and CI smoke tests pass.
6. Then proceed milestone by milestone, demonstrating acceptance criteria and asking before making major scope changes.

### Key decisions already approved by the user

- Build ContextBridge as a GitHub portfolio mini-project.
- Cloud-ready architecture, local Docker Compose initially.
- .NET/C# MCP backend, PostgreSQL, structured records + Markdown content.
- Five-entity domain concept, six named MCP tools.
- Main value: ChatGPT discussion -> Claude Code implementation continuity.
- Detailed architecture and milestone plan above are the current working baseline.

### Still to validate, not silently assume

- Whether this ChatGPT account can call remote custom MCP write tools.
- How the user's Visual Studio installation runs Claude Code.
- Exact auth model and remote MCP compatibility.
- Final tool schemas, DB migrations, concurrency/idempotency behavior.
- Project bootstrap/admin workflow and scope of project authorization in single-user v0.1.

**End of handoff.**
