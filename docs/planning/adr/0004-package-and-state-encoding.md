# ADR 0004 — Package and State Encoding

- **Status:** Accepted (revised)
- **Date:** 2026-11-02
- **Revision:** 2026-11-02 — replaced the custom `MCS2` state container with MessagePack.
- **Deciders:** Project owner

## Context

The compiled model and per-job data must cross a .NET writer → Rust reader boundary. Result state must be
read efficiently by .NET and optionally by Rust/Python. Requirements:

- compact for thousands of jobs,
- versioned and self-describing,
- fast to load,
- safe to parse,
- zero-copy access to large numeric arrays where possible,
- no dependence on native struct layout or CLR type names.

## Decision

### Simulation package (`.msl`)

- Immutable SQLite database with `schema_info`, `models`, `lattices`, and `jobs` tables. Results live in a
  separate `.msr` store (ADR 0011).
- Compiled model payloads are **Zstd-compressed MessagePack maps** with snake_case keys.
- Per-job payloads (settings, seed, optimizers) and content-addressed lattices are Zstd+MessagePack.
- Common compiled models are stored once per `model_signature` and referenced by jobs.
- SQLite stores metadata in typed columns for querying; the solver reads one model row and one job row.

### Result state (`.mcs`)

- A **single MessagePack document** encoded with `MessagePack-CSharp` (.NET) and `rmp-serde` (Rust).
- No custom header, section table, alignment scheme, or checksum trailer.
- Top-level map with `format = "mocassin.mcs"` and `formatVersion = 1`.
- `lattice` is a MessagePack `bin`; numeric payloads use native MessagePack arrays.
- KMC trackers, mobile mapping, and jump histograms are optional fields; MMC omits them.
- SHA-256 integrity is stored in `results.run_state_sha256` in the `.msr`; the state itself has no checksum.
- The schema is specified in [`02-data-contracts.md`](../02-data-contracts.md) and covered by cross-language
  fixtures.

## Consequences

**Positive**

- MessagePack is compact and available in both ecosystems; Zstd makes large energy tables cheap to store.
- SQLite gives transactional writes, indexing, and easy result queries without EF Core.
- The same MessagePack tooling is used for package payloads and state, reducing duplicate serializers.
- The state format has no hand-written binary layout to maintain; it is self-describing and versioned by fields.
- The expected state size (< 4 GiB) fits MessagePack 32-bit lengths.

**Negative**

- Two schema definitions to maintain; mitigated by contract fixtures and a shared schema document.
- MessagePack schema drift is possible; mitigated by explicit version fields and strict readers.
- Large arrays deserialize into memory; acceptable for the documented < 4 GiB workload.
- No built-in checksum; rely on the `.msr` SHA-256 and file-system integrity.

## Alternatives considered

- **JSON for everything:** simple but slow and bulky for millions of energy/lattice values; kept only for
  the editable project and debug exports.
- **FlatBuffers/Cap'n Proto:** excellent zero-copy, but code generation and schema tooling add friction.
- **Custom `MCS2` container:** rejected as unnecessary complexity for the documented < 4 GiB workload.
- **Arrow IPC for state:** strong for analytics and Python interop, but heavier and less convenient for
  solver-written state; could be added as an export format later.
- **MemoryPack:** C#-only and not a stable cross-language ABI; rejected.
- **Legacy raw structs:** rejected (see ADR 0001).

## Rules

- Any change to the package or state schema increments `format_version`/`formatVersion` and updates the
  contract fixtures.
- Readers reject unknown major versions and validate all lengths/indices before allocation.
- State compression is external and never changes the MessagePack document.
- The package writer and solver reader share a single schema description in `tests/contracts/`.
