# Pre-amendment non-singleton cluster package fixture

This directory holds a **byte-fixed** `.msl` v1 package written by the **pre-amendment writer at `bbc2d08`**. It
is the real F-29 absence case: a non-singleton two-member cluster table with **no** `code_expansions` member.
It is captured once and **never regenerated**: after the amendment, the branch no longer contains the writer
that produced it.

## Files

| File | Size | Notes |
| --- | ---: | --- |
| `amend-2-pre-map.msl` | 98,304 bytes | immutable SQLite package; SHA-256 `97a97276d3f6670226742ef5a48a178345dc8f242c459a49473a54ed8df4788a` |
| `amend-2-pre-map.sha256` | — | `sha256sum` line for the package |
| `package-summary-v1.json` | 1,959 bytes | canonical inspector projection; logically equal to `step-11-amend-2` |
| `payloads/model-1.bin` | 6,006 bytes | raw model MessagePack **without** the optional member (24 bytes smaller) |
| `payloads/lattice-1.bin` | 101 bytes | raw lattice MessagePack |
| `payloads/job-1.bin` | 446 bytes | raw KMC job MessagePack |
| `payloads/index.json` | 1,235 bytes | kind, id, compressed/raw size and SHA-256 for every payload |

## Provenance

- **Source project:** `samples/step-11-cluster-expansion/step-11-cluster-expansion.mocprj`, copied into a
  temporary worktree at `bbc2d08`; the parent writer parses and compiles it unchanged.
- **Writer commit:** `bbc2d083ed96fa6e26526e335304107b20a7fdcb` (the parent of the amendment-2 implementation).
  `schema_info.app_version`/`generator` record `1.0.0+bbc2d08…`; `created_utc` is
  `2026-11-21T10:20:50.3970717Z`.
- **Toolchain:** .NET SDK 11.0.100, in-box Zstandard, default `PackageBuildOptions` (`CompressionLevel = 3`).
- **Model signature:** `90d4101a2ff594f353c4b3850079f1343fac71a5f3bac438589655c0204b0885`;
  **model identity:** `af2efa6298044cbafe39d62eac3140e241cdef5f5c4195fb61f4f14de841d610`.

## Test

`ClusterExpansionPackageFixtureTests.PreMapPackageLoadsWithAbsentExpansionsAndTheRecordedSummary` verifies the
package, checks the recorded summary, decodes the model, asserts `CodeExpansions == null`, and confirms the six
canonical codes. The Rust reader test in `mocassin-msl` asserts `ClusterCodeLookup::ExpansionMapAbsent` for
`8386`/`8387` and `Canonical` for the six canonical codes. Do not "fix" a test by editing this fixture.
