# ADR 0003: Projects are created implicitly by the first write, not a dedicated tool

## Status

Accepted

## Context

The approved MCP tool list (`get_project_context`, `search_context`,
`save_context`, `save_decision`, `create_handoff`, `complete_handoff`) has no
tool to create a new `Project`. Every write tool takes a project slug and
assumed the project already existed. The handoff doc itself flagged "project
bootstrap/admin workflow" as unresolved. Without some bootstrap path, a brand
new project (e.g. Kartimo) could never be used through MCP at all — contradicting
the stated goal of getting ChatGPT-discussed context into Claude Code with
minimal ceremony.

## Decision

`save_context`, `save_decision`, and `create_handoff` create the project
(named after the slug) if it doesn't exist yet, instead of returning
`NOT_FOUND`. Read tools (`get_project_context`, `search_context`) still
return `NOT_FOUND` for an unknown project — a read should never have a side
effect.

## Consequences

- No seventh MCP tool is needed; the approved six-tool contract is unchanged.
- A project created this way only has a slug-derived name; renaming or
  adding a description still requires direct use of `ProjectService` — there
  is no MCP tool for that either (same bootstrap gap, smaller scope).
- A typo'd project slug in a write call silently creates a new empty
  project rather than failing loudly. Accepted as a reasonable tradeoff for
  v0.1's single-user scope; worth revisiting if/when multi-project usage
  patterns show this causes real confusion.
