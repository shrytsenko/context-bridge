# ADR 0002: Four-project modular monolith, no MediatR or generic repositories

## Status

Accepted

## Context

ContextBridge is a small service, but it is also a public portfolio project
meant to demonstrate familiarity with Clean Architecture-style layering.

## Decision

Split the solution into `Api`, `Application`, `Domain`, and `Infrastructure`
projects with a one-directional dependency graph (see
[architecture.md](../architecture.md)). Use plain service classes for use
cases — no MediatR, no generic repository abstraction over EF Core.

## Consequences

- More ceremony than a single-project API would need for a service this
  small; accepted deliberately for portfolio clarity, not because the
  domain complexity demands it.
- Application services call `DbContext`/EF Core directly rather than through
  a repository interface, keeping the Infrastructure layer's real
  capabilities (e.g. full-text search) accessible without an abstraction
  that would have to be widened later anyway.
