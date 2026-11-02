# ADR 0001 — Clean-Break, Versioned File Formats

- **Status:** Accepted
- **Date:** 2026-11-02
- **Deciders:** Project owner

## Context

The original MOCASSIN uses three formats:

1. `.mocprj` — SQLite with one row per project and a Newtonsoft.Json object graph using `$id`/`$ref` and
   assembly-qualified `$type` names.
2. `.msl` — SQLite with EF Core entities whose payloads are raw native C structs and binary blobs.
3. `.mcs` — a raw dump of the C solver's memory buffer with no magic, version, length table, alignment, or
   checksum.

All three are tightly coupled to the original CLR type names, x86-64 ABI, and C struct layouts. Preserving
them would force the new architecture to retain mutable object graphs, reflection serialization, unsafe
pointer casts, and platform-specific layouts.

## Decision

Use **new, versioned formats** for projects, simulation packages, and result state. Do not implement readers
or writers for the legacy formats.

- Project: UTF-8 JSON with `schemaVersion` and stable string IDs.
- Package: SQLite with a small normalized schema and versioned MessagePack/Zstd payloads.
- State: self-describing binary container with magic, version, section table, 64-bit offsets, and CRCs.

Existing users must recreate projects in the new application. The original formats may be studied for
scientific semantics and used to generate reference data, but they are not a compatibility target.

## Consequences

**Positive**

- No reflection-based serializers, no `$type`/`$ref` graphs, no CLR type names in data.
- Stable, documented contracts that can be tested across .NET and Rust.
- Safe, bounds-checked parsing; no unaligned native struct access.
- Freedom to choose compact, compressible encodings and to evolve schemas.

**Negative**

- No automatic migration for existing projects or result files.
- Users must re-enter models and re-run simulations to use the new tooling.
- Documentation and samples must be provided to make recreation practical.

## Alternatives considered

- **Bidirectional compatibility:** rejected; would constrain the architecture and preserve unsafe layouts.
- **Legacy reader only:** rejected by the project owner; even a read-only parser adds long-term maintenance
  and test burden.
- **Hybrid new format with legacy export:** rejected; export would still require the legacy object model and
  ABI.
