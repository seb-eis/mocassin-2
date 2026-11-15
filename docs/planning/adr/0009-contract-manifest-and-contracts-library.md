# ADR 0009 — Shared Contract Manifest and `Mocassin.Contracts`

- **Status:** Accepted
- **Date:** 2026-11-02
- **Deciders:** Project owner

## Context

The package (`.msl`) and state (`.mcs`) formats are implemented in three languages:

- C#: package writer and result reader/evaluation;
- Rust: package reader and state writer;
- Python: state reader and basic evaluations.

The current plan describes the formats in prose and relies on golden fixtures. That is necessary but not
sufficient: field/type/version drift can pass until a fixture happens to exercise the changed field. The
planned architecture also had `Mocassin.Results` depending on `Mocassin.Packages`, which couples the read path
to the builder/writer.

## Decision

1. **Introduce `Mocassin.Contracts`** as a small .NET library that owns:
   - package/state DTOs such as `ParameterizedModel`, job/lattice payloads, and `.mcs` document types;
   - schema/format version constants;
   - manifest reader/validator helpers.
   `Mocassin.Results` depends on `Mocassin.Contracts`, not `Mocassin.Packages`.
2. **Add a machine-readable contract manifest** at `contracts/manifest.json`.
   It is the single source for field names, types, required/optional flags, format versions, and section
   identifiers.
3. **Generate checks, not entire DTOs.** `tools/contracts` generates:
   - JSON Schema fragments for docs/validation;
   - language-neutral fixture descriptors and canonical JSON expectations;
   - version/enum constants for C#, Rust, and Python.
   C#, Rust, and Python DTOs may remain hand-written; generated checks detect drift.
4. **CI enforcement.** CI regenerates artifacts and fails if they are stale or if a language DTO/validator
   disagrees with the manifest.

## Consequences

**Positive**

- `Mocassin.Results` no longer depends on the package writer/build pipeline.
- Schema drift between C#, Rust, and Python is caught mechanically.
- Format documentation and tests share one source of truth.
- Generated constants reduce copy-paste version mistakes.

**Negative**

- One more .NET project and one more tooling pipeline.
- The manifest must be updated with every format change.
- Generated files add review noise; CI must keep them current.

## Alternatives considered

- **Prose + golden fixtures only:** rejected; too easy to miss field/type drift.
- **Full IDL/code generation for all DTOs:** stronger, but more tooling and generated-code maintenance than
  needed for a small scientific project.
- **Switch to Protobuf/FlatBuffers/Arrow:** larger format change; not justified by the current < 4 GiB state
  workload.
- **Keep `Results -> Packages`:** rejected; the read path should not depend on the writer/build library.

## Rules

- `contracts/manifest.json` changes require a format version bump and updated fixtures.
- `Mocassin.Contracts` must not depend on `Mocassin.Packages` or `Mocassin.Results`.
- `Mocassin.Results` must not depend on `Mocassin.Packages`.
- CI regenerates contract artifacts and fails on drift.

### Pre-release package-shape finalization

The `.msl` package has never been released. Step 11 therefore finalizes the package-shaped manifest entries while
no released package exists: it adds `formats.package.payloads` (top-level model/job/lattice payload descriptors)
and a separate generated `package-payloads-v1.schema.json`, and it removes the undefined `jobs.norm_factor` and
`jobs.base_frequency_hz` columns from `formats.package.tables.jobs`. Both stay at `formatVersion = 1`.

This is the package-shape counterpart of ADR 0010's pre-release signature-layout finalization: it does not waive
the version-bump rule generally. The rule applies from the first released `.msl`; a later change to a table or
payload shape requires a version bump, regenerated artifacts, and updated fixtures. Steps 03–10 applied the same
pre-release basis to additive project-JSON sections, and Step 11 records the package basis here so the shape
change is not a silent exception.
