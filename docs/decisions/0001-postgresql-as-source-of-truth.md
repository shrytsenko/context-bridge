# ADR 0001: PostgreSQL as source of truth, Markdown content in structured records

## Status

Accepted

## Context

Context entries and decisions need to store free-form Markdown content while
remaining queryable, versionable, and filterable by type/project.

## Decision

Store structured records (EF Core entities) in PostgreSQL with Markdown
content in `text` columns. PostgreSQL is the single source of truth. A
Git/Markdown export is a possible future feature but is explicitly deferred —
v0.1 does not write to or read from a Git repository.

## Consequences

- Full-text search can use PostgreSQL `tsvector`/GIN instead of a separate
  search index.
- No risk of drift between a database and an export format, because there is
  only one authoritative store.
- Revisiting Git export later requires a one-way sync design, not a
  bidirectional one.
