# ADR 0004 — Package and State Encoding

- **Status:** Accepted
- **Date:** 2026-11-02
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

- SQLite database with `schema_info`, `models`, `jobs`, and `results` tables.
- Compiled model payloads are **Zstd-compressed MessagePack maps** with snake_case keys.
- Per-job payloads (lattice, energy background, settings, seed) are also Zstd+MessagePack.
- Common compiled models are stored once per `model_signature` and referenced by jobs.
- SQLite stores metadata in typed columns for querying; the solver reads one model row and one job row.

### Result state (`.mcs`)

- Custom binary container `MCS2`:
  - 64-byte header with magic, version, flags, counts, CRC;
  - 40-byte section table entries;
  - 64-bit offsets/lengths, 8/64-byte alignment;
  - little-endian fixed-width arrays;
  - CRC32C per section and header;
  - optional per-section Zstd compression.
- Section types: metadata JSON, lattice, counters, global/mobile/static trackers, mobile mapping, jump
  histograms, energy samples.
- No framework-specific serialization; the layout is specified in
  [`02-data-contracts.md`](../02-data-contracts.md) and implemented independently in Rust and .NET.

## Consequences

**Positive**

- MessagePack is compact and available in both ecosystems; Zstd makes large energy tables cheap to store.
- SQLite gives transactional writes, indexing, and easy result queries without EF Core.
- `MCS2` is safe, self-describing, checksummed, and can be memory-mapped section by section.
- 64-bit offsets remove the original 2 GiB limit.

**Negative**

- Two schema definitions to maintain; mitigated by contract fixtures and a shared schema document.
- MessagePack schema drift is possible; mitigated by explicit version fields and strict readers.
- Custom binary format requires documentation and fuzz testing.

## Alternatives considered

- **JSON for everything:** simple but slow and bulky for millions of energy/lattice values; kept only for
  the editable project and debug exports.
- **FlatBuffers/Cap'n Proto:** excellent zero-copy, but code generation and schema tooling add friction.
- **Arrow IPC for state:** strong for analytics and Python interop, but heavier and less convenient for
  solver-written incremental sections; could be added as an export format later.
- **MemoryPack:** C#-only and not a stable cross-language ABI; rejected.
- **Legacy raw structs:** rejected (see ADR 0001).

## Rules

- Any change to the package or state schema increments `format_version` and updates the contract fixtures.
- Readers reject unknown major versions and validate all lengths/indices before allocation.
- Compression is optional and signalled per section; readers must support uncompressed data.
- The package writer and solver reader share a single JSON description of field order and types in
  `tests/contracts/`.
