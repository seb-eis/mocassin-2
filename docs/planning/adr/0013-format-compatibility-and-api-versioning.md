# ADR 0013 — Format Compatibility and Public API Versioning

- **Status:** Accepted
- **Date:** 2026-11-02
- **Deciders:** Project owner

## Context

The project has several versioned artifacts:

- project JSON, which is migrated in-place;
- `.msl` packages, which are build artifacts but may be archived;
- `.msr` results stores, which may outlive the solver run;
- `.mcs` state files, which are resumable and analysis inputs;
- public NuGet libraries and the Python package.

Without an explicit policy, a new release can silently break old files or public APIs.

## Decision

### File formats

| Artifact | Read support | Write | Migration |
| --- | --- | --- | --- |
| `.mocprj` | all released schema versions | current | in-memory migrations |
| `.msl` | current and previous major | current | version-specific readers; no in-place rewrite |
| `.msr` | current and previous major | current | version-specific readers |
| `.mcs` | current major only | current | none; re-run or use the release that wrote it |
| `solver.json` | current and previous major | current | version-specific readers |
| MMCFE log DB | current and previous major | current | version-specific readers |
| CLI JSON/events | additive within a major version | current | unknown fields ignored |

- A major version is the integer before the first dot of `format_version`/`formatVersion`.
- Writers always write the current version.
- A reader rejects a version newer than its supported range with an actionable message.
- Previous-major fixtures remain in `tests/contracts/` and run in CI.
- `.msl` compatibility is read-only; users rebuild or export rather than upgrade packages in place.
- Results intended for long-term archive should be exported to CSV/JSON/Parquet. Archived `.msr` files get the
  same two-major window.

**Additive payload fields within a major version (addendum, 2026-11-21).** A versioned payload (`.msl` model,
job, and lattice payloads; `.msr` rows and state references; `.mcs` documents) may gain a **new optional member**
without a major version bump when all of the following hold:

- every existing field keeps its name, type, meaning, and required/optional status;
- the writer always emits a valid current-major document, and the new member's **absence** has a defined
  meaning that **fails closed with an actionable error and never yields a wrong value** for every document
  written before it existed. Full functional readability of an old document is **not** required: a rebuild may
  be necessary, as it is for a pre-map non-singleton cluster table;
- a reader that predates the member ignores it (the payload unknown-field policy is `ignore`) and still reads
  the document under the documented absence rule;
- the member is not part of a canonical signature projection, or the projection already binds the same data;
  adding a new signature input is a format-breaking change;
- the current-major fixtures are updated and a byte-fixed pre-change fixture, when the old shape must keep
  working, is retained for the life of the major.

A **new required field**, a changed type or meaning, a removed field, a changed ordering/encoding invariant, or
a new signature input is a **major** change: bump the version, add a version-specific reader, and keep the
previous-major fixtures. This addendum does not change the current/previous-major support window; it records how
an additive member enters a major. The first released `.msl` freezes v1; from that point, an optional member is
still additive within v1 only if the conditions above hold, and any change to the meaning of the absence rule or
to a signed projection is major. The cluster `code_expansions` member in
[`step-11-amend-2`](../amendments/step-11-amend-2-cluster-code-expansion.md) is the first application.

### .NET libraries

- NuGet packages use SemVer 2.0.
- No breaking public API changes within a major version.
- `Microsoft.CodeAnalysis.PublicApiAnalyzers` tracks public API files; `ApiCompat` checks against the last
  released package in CI.
- Preview packages may break, but must document the break.

### Python package

- `mocassin-analysis` declares its supported `.mcs`/`.msr` format versions and supported Python/NumPy ranges.
- The package version tracks the supported format generation.
- A new format major requires a new package major.

## Consequences

**Positive**

- Old packages and results remain usable for one major cycle.
- Users know which files are archive-grade and which are ephemeral.
- Public API drift is caught mechanically.

**Negative**

- Every major format change requires compatibility readers and fixtures.
- Two-major support increases test and maintenance cost.
- `.mcs` users must re-run or keep an old release for old states.

## Alternatives considered

- **Current-only for everything:** simplest, but poor for archives and reproducibility.
- **Archive-grade forever:** highest guarantees, but permanent compatibility code and fixtures.
