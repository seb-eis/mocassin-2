# Step 13 populated `.msl` fixture

This directory holds a real-writer `.msl` v1 package with populated pair and cluster tables and both simulation
kinds. It is the populated/MMC acceptance fixture for the Rust reader and mapping code.

## Files

| File | Size | Notes |
| --- | ---: | --- |
| `step-13-populated.msl` | 98,304 bytes | immutable SQLite package; SHA-256 `3e870f87303320c510ba6eba2d0eb70f0daa7412ae6db123a3fbbc392174ee50` |
| `step-13-populated.sha256` | — | `sha256sum` line for the package |
| `package-summary-v1.json` | 2,710 bytes | canonical inspector projection (data contracts §3.5) |
| `payloads/model-1.bin` | 6,518 bytes | raw model MessagePack |
| `payloads/lattice-1.bin` | 85 bytes | raw lattice MessagePack (both jobs share it) |
| `payloads/job-1.bin` | 446 bytes | raw KMC job MessagePack |
| `payloads/job-2.bin` | 447 bytes | raw MMC job MessagePack |
| `payloads/index.json` | 1,530 bytes | kind, id, compressed/raw size and SHA-256 for every payload |

The package has one model, one lattice, and two jobs (`1` KMC, `2` MMC), `format_version = 1`,
`application_id = MOCS`, `user_version = 1`, no `results` table, and no `-wal`/`-shm` sidecar. The model has
3 particles, 3 P1 positions, 6 pair tables, 1 cluster table, 2 collections, and 8 rules; the lattice is
2×2×2 with 24 particles.

- **Model signature:** `7a23241c57dc8fce177e1ea98c8db3dd054b30ac3cd7db46dc7ac051fbafa5a6`
- **Model identity:** `7036aac84de8044e34f2a7431b50bf97687d8e39aabbbb5327cc6b9e8dcd81a9`
- **Lattice signature:** `cc146e07bbf21c133cc99372e56210b648baf29fae9dbf1c0d22dd3a63065597`

## Provenance

- **Source project:** `samples/step-13-populated/step-13-populated.mocprj` (a small P1 ceria-derived mechanism
  with stable/unstable oxygen environments, a three-site migration-edge cluster, one KMC migration simulation,
  one MMC O/V exchange simulation, and a two-collection job template at 2×2×2).
- **Build path:** `ProjectJson` → `ModelCompiler` → `ParameterizationService` → `JobTemplateExpander` →
  `SupercellGenerator` → `SimulationPackageWriter`, with `PackageBuildOptions` defaults and
  `created_utc = 2026-11-20T00:00:00Z`.
- **Request:** `PackageBuildRequest(<path>, 018f4c9a-7f6b-7c3a-9d2e-0000000020a0, "all", 2026-11-20T00:00:00Z)`.
- **Toolchain:** .NET SDK 11.0.100 / runtime 11.0.0, in-box Zstandard, default compression level 3.

## Regeneration

Do not hand-edit the package or the payloads. Regenerate with the real writer:

```sh
MOCASSIN_REGENERATE_POPULATED_FIXTURE=1 \
  dotnet test src/dotnet/Mocassin.Results.Tests/Mocassin.Results.Tests.csproj -c Release \
  --filter "FullyQualifiedName~RegeneratePopulatedPackageFixture"
```

The command rebuilds the package, the `package-summary-v1` golden, the SHA-256 line, and the extracted payloads.
`PopulatedPackageFixtureTests` then verifies the package, compares the summary, checks the payload index, and
rebuilds once more to prove the logical content is reproducible. Package bytes are a writer+runtime artifact;
signatures and the canonical summary are the logical contract.

## Intentional limits

The model builder rejects two properties the Step 13 design originally asked this fixture to carry:

- **A non-trivial per-node mask difference** would need a particle set on one transition node that differs from
  the other direction's node. A mixed set on a migration endpoint is rejected with `CMP004` (the same start
  state then has two outcomes), and a mixed MMC exchange fails the same rule. The union check's positive and
  negative cases are covered by the synthetic payload mutations in
  `src/rust/crates/mocassin-msl/src/map/tests/`.
- **An equivalent-start mapping** needs a symmetry operation that maps a path onto itself with an identical
  relative encoding. The checked-in P1 and P-1 variants do not produce one (the P-1 build also fails the
  verifier's cluster-link check, `PKG010`). The reader never requires a mapped direction's
  `position_id` to equal the bucket position; that acceptance is covered by the synthetic mapping tests and
  remains a Step 16 interface check.

The fixture still covers the populated pair/cluster tables, both simulation kinds, the real-writer payload and
signature paths, and the MMC validation rules.
