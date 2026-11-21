# Step 14 ceria 10×10×10 `.msl` fixture

This directory holds a real-writer `.msl` v1 package with one **10×10×10** ceria KMC job. It is the Step 14
context-initialization performance/acceptance fixture for the Rust solver.

## Files

| File | Size | Notes |
| --- | ---: | --- |
| `step-14-ceria-10x10x10.msl` | 98,304 bytes | immutable SQLite package; SHA-256 `0865dd75abf306029327605ed5912e88135f43f6c9f283ff641ba585c6a33df7` |
| `step-14-ceria-10x10x10.sha256` | — | `sha256sum` line for the package |
| `package-summary-v1.json` | 2,026 bytes | canonical inspector projection (data contracts §3.5) |
| `payloads/model-1.bin` | 3,475 bytes | raw model MessagePack |
| `payloads/lattice-1.bin` | 4,061 bytes | raw lattice MessagePack |
| `payloads/job-1.bin` | 530 bytes | raw KMC job MessagePack |
| `payloads/index.json` | 1,231 bytes | kind, id, compressed/raw size and SHA-256 for every payload |

The package has one model, one lattice, and one job (`1` KMC), `format_version = 1`, `application_id = MOCS`,
`user_version = 1`, no `results` table, and no `-wal`/`-shm` sidecar. The model is the msl-v1 ceria primary
model: 5 particles (`∅`, `Ce`, `O`, `Vc`, `H`), 4 P1 positions, 4 reference sites, **no pair or cluster tables**,
one KMC collection, 4 rules. The lattice is 10×10×10 with **4,000** particles (P1 position count 4).

- **Model signature:** `1845b70e82320d75f768b33d1a9a46a8ae6170c33ad96013317f89fe9189a74b`
- **Model identity:** `92cf375eb1cc9bf6efaac33430fa3d44660ad8a7b355d3976441ce7c1d683e60`
- **Lattice signature:** `867a313522a3e8741cef9622fb0f6a90ae9a222845fc27e30628f7364b3040e1`
- **Job:** 1 KMC, 773 K, target 150 MCSP, pre-run 250 MCSP, seed
  `979cab2a2e9a07b2cbbd3e7c0aae5b568118cd84cf6f5e7be22720bdb43e4170`

## Provenance

- **Source project:** `samples/step-14-ceria-10x10x10/step-14-ceria-10x10x10.mocprj`, derived from
  `samples/jobs/ceria-temperature-sweep.mocprj` by selecting the `T773`/`x001` collection and one job at a
  10×10×10 supercell. The model is unchanged, so its signature equals the msl-v1 ceria model signature.
- **Build path:** `ProjectJson` → `ModelCompiler` → `ParameterizationService` → `JobTemplateExpander` →
  `SupercellGenerator` → `SimulationPackageWriter`, with `PackageBuildOptions` defaults and
  `created_utc = 2026-11-21T12:00:00Z` (the fixture builder's fixed timestamp).
- **Toolchain:** .NET SDK 11.0.100 / runtime 11.0.0, in-box Zstandard, default compression level 3.
- **Writer commit:** the Step 14 feature-6 implementation commit (recorded in the step page).

The ceria fixture has **no pair or cluster tables**; it measures the table-free 10×10×10 context path. The
populated-model 10×10×10 clone (`step-13-populated` model on a hand-built lattice) is the pair/cluster-scale
measurement in the context benchmark and is not a separate package.

## Regeneration

Do not hand-edit the package or the payloads. Regenerate with the real writer:

```sh
MOCASSIN_REGENERATE_STEP14_CERIA_FIXTURE=1 \
  dotnet test src/dotnet/Mocassin.Results.Tests/Mocassin.Results.Tests.csproj -c Release \
  --filter "FullyQualifiedName~RegenerateStep14CeriaFixture"
```

`Step14CeriaFixtureTests` verifies the package, the canonical summary, the SHA-256 line, the payload index,
and rebuild reproducibility. Package bytes are a writer+runtime artifact; signatures and the canonical summary
are the logical contract.
