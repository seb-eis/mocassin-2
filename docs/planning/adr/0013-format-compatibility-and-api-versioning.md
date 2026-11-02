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
