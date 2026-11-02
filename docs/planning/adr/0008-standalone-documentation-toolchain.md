# ADR 0008 — Standalone Documentation Toolchain

- **Status:** Accepted
- **Date:** 2026-11-02
- **Deciders:** Project owner

## Context

The reimplementation needs a standalone documentation site for users, scientists, and developers. It must
cover a user guide, the physics, an architecture/developer guide, tutorials, CLI/formats reference,
troubleshooting, performance/HPC, contributing, migration, glossary, and examples.

The content spans three languages/ecosystems:

- narrative Markdown (guides, tutorials, physics, architecture);
- C#/.NET API reference;
- Rust API reference.

Zensical is the chosen general documentation framework. It is Markdown-first, MkDocs-compatible, has good
navigation/search, and supports `exclude_docs` and strict link validation. Its `mkdocstrings` integration is
strong for Python but there is no mature official C# or Rust handler. Reimplementing .NET/Rust API rendering
inside Zensical would be fragile and duplicative.

## Decision

- **Zensical** owns the narrative documentation site.
  - `docs_dir = "docs"`, `site_dir = "site"`.
  - `docs/planning/` is excluded via `exclude_docs`.
  - Strict mode is used in CI (`zensical build --strict`).
- **DocFX** generates the .NET API reference from XML documentation and assemblies.
- **rustdoc** (`cargo doc`) generates the Rust API reference from doc comments.
- **One published site with API subpaths:**
  - narrative docs at `/`;
  - .NET API at `/api/dotnet/`;
  - Rust API at `/api/rust/`;
  - `docs/api/index.md` is the landing page.
- **Build order:** Zensical build first, then copy DocFX and rustdoc outputs into `site/api/dotnet` and
  `site/api/rust`. Generated API HTML is not committed.
- **Cross-links:** narrative pages link to stable API subpaths; API landing pages link back to the guides.
  API-internal links are validated by DocFX/rustdoc; narrative links by Zensical strict mode.
- **Search:** Zensical search covers narrative content; DocFX and rustdoc keep their own API search. A unified
  search index is deferred.
- **Versioning/PDF/offline:** current-version site only for the first release; versioned snapshots and PDF
  export are deferred.
- **Publishing:** attach the combined `site/` archive to releases; deploy to GitHub Pages or another static
  host if available.

## Consequences

**Positive**

- Each ecosystem uses its canonical, high-fidelity API documentation tool.
- Narrative content stays in one maintainable Markdown site with strong link validation.
- No hand-maintained duplicate API pages.
- The API subpath layout can later be versioned without changing the narrative site.

**Negative**

- Three build tools and a combine step.
- Navigation/search are not fully unified.
- Zensical is still on a 0.x release line; pin the version and keep content standard Markdown.
- DocFX and rustdoc styling differ from Zensical; the API landing page must set expectations.

## Alternatives considered

- **Zensical + mkdocstrings for everything:** rejected; no mature C#/Rust handlers, and generated API fidelity
  would be lower.
- **Three separate sites/subdomains:** rejected; navigation/search and release packaging are worse.
- **Hand-written API reference:** rejected; drifts from source and duplicates XML/doc comments.
- **DocFX for the whole site:** rejected; the narrative authoring/search experience is weaker than Zensical.
- **Rustdoc for .NET:** not applicable.

## Rules

- Public .NET APIs require XML docs; public Rust items require doc comments.
- CI builds the combined site on every PR; broken links or missing API docs block merge.
- `docs/planning/` is never published.
- Generated API HTML and `site/` are gitignored.
- Step 00 creates the site skeleton, navigation, build scripts, and Docs CI job. Every feature step updates its
  pages; Step 29 validates completeness and cross-cutting content; Step 30 packages the site artifact.
