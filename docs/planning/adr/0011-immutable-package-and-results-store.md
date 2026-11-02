# ADR 0011 — Immutable Package and Separate Results Store

- **Status:** Accepted
- **Date:** 2026-11-02
- **Deciders:** Project owner

## Context

The original design used one `.msl` SQLite file for both the compiled package and collected results. That
creates three problems:

1. The solver must open the package read-only while `collect` writes results to the same file.
2. SQLite WAL mode is unsafe or unsupported on some shared HPC filesystems and requires `-wal`/`-shm` files.
3. A package is a build artifact that should be immutable and content-addressed; results change over time.

## Decision

1. **`.msl` is an immutable package.** After a successful build it contains only schema metadata, models,
   lattices, and jobs. It has no results table.
2. **`.msr` is a separate results store.** `mocassin collect` writes a SQLite results database, by default
   `<package>.msr` next to the package, or the path given by `--results`. States larger than the configurable
   embedded cap (default 1 GiB) are stored as external `.mcs` files referenced by relative path and SHA-256;
   `.msr` and those files travel together.
3. **Evaluation opens both.** `mocassin evaluate`, the .NET evaluation API, and the Python API open `.msl`
   for package/model metadata and `.msr` for results. They may attach both databases or query them
   separately.
4. **Publication barrier for `.msl`.** The writer builds in a temporary file, checkpoints and truncates WAL,
   switches the final database to `journal_mode=DELETE`, closes it, fsyncs, and atomically renames it. A
   published `.msl` must not require `-wal` or `-shm` files.
5. **Single writer for `.msr`.** `collect` takes an exclusive `.msr.lock`, writes in a transaction, fsyncs,
   and closes. Readers open read-only and use a snapshot. Concurrent collectors are rejected with a clear
   message.
6. **Local/shared storage policy.** Immutable `.msl` files are safe on read-only or shared filesystems.
   `.msr` files should stay on a filesystem with working POSIX locking; HPC users collect on a login node or
   local scratch and archive the result. This limitation is documented.
7. **CLI changes:**
   - `mocassin build` produces only `.msl`.
   - `mocassin collect <package.msl> [--results <file.msr>]`.
   - `mocassin evaluate <package.msl> [--results <file.msr>]`.
   - `mocassin export` can export from `.msr`.
8. **Compatibility:** `.msl` and `.msr` share the current + previous major read-support policy (ADR 0013).

## Consequences

**Positive**

- The solver never contends with result writers.
- Packages are reproducible, read-only artifacts.
- HPC use no longer depends on WAL over shared storage.
- Results can be copied, archived, or discarded independently.

**Negative**

- Users handle two files instead of one.
- `evaluate` must validate that `.msr` belongs to the same package/model signature.
- `.msr` needs its own lock and schema version.
- Packaging and tutorials must mention the sidecar.

## Alternatives considered

- **Single mutable `.msl` with offline collection:** simpler UX, but still has WAL/shared-filesystem risk and
  makes the package mutable.
- **Atomic copy-and-replace of `.msl`:** preserves one-file UX but copies multi-GB packages per collection.
- **Keep concurrent WAL writes:** acceptable on a local disk, not acceptable as the general HPC contract.
