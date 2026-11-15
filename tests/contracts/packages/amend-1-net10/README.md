# Pre-amendment `.msl` compatibility fixture (amendment 1)

This directory holds a **real simulation package written by the unamended `net10.0` writer**, before the
`ZstdSharp.Port` payload codec was replaced by the in-box .NET 11 Zstandard API. It is the reference for
"a package written before the switch is still readable after it" (`step-11-amend-1` §9.5).

It is a **compatibility fixture, not the Step 11 feature-7 determinism fixture**. It is captured once and
**never regenerated**: after the amendment, the branch no longer contains the writer that produced it. The
canonical summary, not the file bytes, is the logical golden.

## Files

| File | SHA-256 | Notes |
| --- | --- | --- |
| `ceria-temperature-sweep.msl` | `053e4c8f5bd4ee115061dd5cb2b38266a50b155e504fe2108017216963413e59` | 98,304 bytes, immutable SQLite package |
| `ceria-temperature-sweep.sha256` | — | `sha256sum` line for the package |
| `package-summary-v1.json` | `60c95e1bcb2438781a6d9a2597840fd04c8e0e6c3caec254cf76589ec2739d2d` | canonical inspector projection (data contracts §3.5) |

The package has one model row, six distinct lattice rows, six job rows, `format_version = 1`,
`application_id = MOCS`, `user_version = 1`, `created_utc = 2026-11-15T12:00:00Z`, `app_version = 1.0.0`, and no
`results` table or `-wal`/`-shm` sidecar.

## Provenance

- **Source project:** `samples/jobs/ceria-temperature-sweep.mocprj` (six jobs, six distinct lattices, two KMC
  collections, 6×6×6 and 8×8×8 ceria).
- **Build path:** `ProjectJson` → `ModelCompiler` → `ParameterizationService` → `JobTemplateExpander` →
  `SupercellGenerator` → `SimulationPackageWriter`, exactly as the Step 11 test support does.
- **Writer commit:** `683b7c4` (the amendment branch point); its code is identical to `aa48114`, the commit
  recorded by the Part A measurement. The package was re-built at `683b7c4` and reproduced the Part A package
  hash byte-for-byte.
- **Toolchain:** .NET SDK `10.0.403`, runtime `10.0.14`, `ZstdSharp.Port 0.8.8`, `MessagePack 3.1.12`,
  `Microsoft.Data.Sqlite 10.0.14`; default `PackageBuildOptions` (`CompressionLevel = 3`).
- **Request:** `PackageBuildRequest(<path>, 018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9c00, "all", 2026-11-15T12:00:00Z)`.
- **Model signature:** `92cf375eb1cc9bf6efaac33430fa3d44660ad8a7b355d3976441ce7c1d683e60`.

## Golden summary

`package-summary-v1.json` was produced by `MslPackageInspector.BuildCanonicalSummaryAsync` on this exact file
and pretty-printed with two-space indentation. It is normalized: build metadata (`app_version`, `created_utc`,
`generator`) and file size are excluded, so it is stable across the codec swap and across rebuilds.

## Test

`Mocassin.Results.Tests/Packages/PreAmendmentPackageCompatibilityTests.cs` (PR-fast) verifies the package, checks
the inspection counts, compares the canonical summary with the golden, and checks the recorded SHA-256. Do not
"fix" the test by editing this fixture.
