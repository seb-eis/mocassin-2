# Step 11 `.msl` v1 fixture (amended toolchain)

This directory holds the checked-in simulation-package fixture for the `.msl` v1 contract, written by the
**amended .NET 11 writer** (in-box Zstandard codec). It is the package the Step 13 cross-language reader and the
verifier tests read. The pre-amendment `net10.0` compatibility fixture remains in
[`../amend-1-net10/`](../amend-1-net10/) and is never regenerated.

## Files

| File | SHA-256 | Notes |
| --- | --- | --- |
| `ceria-temperature-sweep.msl` | `e8f615e11022823113701375d8f5e1d597d8d868b93da341120fdff9aac58261` | immutable SQLite package, 98,304 bytes |
| `ceria-temperature-sweep.sha256` | — | `sha256sum` line for the package |
| `package-summary-v1.json` | `1bc3a85ead9287c00d4b953c34419cd6fc838581b5768daee3de01aba16a7234` | canonical inspector projection (data contracts §3.5) |
| `payloads/model-1.bin`, `payloads/lattice-*.bin`, `payloads/job-*.bin` | recorded in `payloads/index.json` | raw MessagePack payloads extracted from the package |
| `payloads/index.json` | — | kind, id, compressed/raw size and SHA-256 for every payload |

The package has one model row, six distinct lattice rows, six job rows, `format_version = 1`,
`application_id = MOCS`, `user_version = 1`, `created_utc = 2026-11-15T12:00:00Z`, no `results` table, and no
`-wal`/`-shm` sidecar. The model signature is
`92cf375eb1cc9bf6efaac33430fa3d44660ad8a7b355d3976441ce7c1d683e60`.

`schema_info.app_version` and `generator` record the **entry assembly** that produced the package. When the
fixture is regenerated from the test project they read `18.12.0` / `mocassin .NET 18.12.0` (the test host's
informational version). They are build metadata: `package-summary-v1` excludes them, together with `created_utc`,
size, and payload byte hashes, so the golden is stable across hosts and runtime patches.

## Provenance

- **Source project:** `samples/jobs/ceria-temperature-sweep.mocprj` (six jobs, six distinct lattices, two KMC
  collections, 6×6×6 and 8×8×8 ceria).
- **Build path:** `ProjectJson` → `ModelCompiler` → `ParameterizationService` → `JobTemplateExpander` →
  `SupercellGenerator` → `SimulationPackageWriter`, with `PackageBuildOptions` defaults (`CompressionLevel = 3`).
- **Request:** `PackageBuildRequest(<path>, 018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9c00, "all", 2026-11-15T12:00:00Z)`.
- **Toolchain:** .NET SDK `11.0.100`, runtime `11.0.0`, `Microsoft.NET.Test.Sdk 18.12.0`, in-box
  `System.IO.Compression` Zstandard; no third-party codec.
- **Logical equality:** the canonical summary is `DeepEquals` to the pre-amendment `amend-1-net10` summary. The
  `.msl` bytes differ from that fixture only in `app_version`/`generator`.

## Regeneration

Do **not** hand-edit the package or the extracted payloads. Regenerate with the real writer:

```sh
MOCASSIN_REGENERATE_PACKAGE_FIXTURES=1 \
  dotnet test src/dotnet/Mocassin.Results.Tests/Mocassin.Results.Tests.csproj -c Release \
  --filter "FullyQualifiedName~RegeneratePackageFixture"
```

The command rebuilds the package, the `package-summary-v1` golden, the SHA-256 line, and the extracted payloads
from the sample. The `PackageFixtureTests` suite then verifies the package, compares the summary, checks the
payload index, and rebuilds once more to prove that the logical content is reproducible. Package bytes are a
writer+runtime artifact: if a later runtime patch changes them, re-run the command and record the new SHA-256 in
the commit message; signatures and the canonical summary must stay equal.
