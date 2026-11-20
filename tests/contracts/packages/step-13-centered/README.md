# Step 13 C-centered `.msl` fixture

This directory holds a real-writer `.msl` v1 package with **translation-equivalent jump starts** and a
**non-trivial per-node mask difference**. It is the Step 13 F-1 acceptance fixture for the Rust reader and its
`jump_mapping` iteration.

## Files

| File | Size | Notes |
| --- | ---: | --- |
| `step-13-centered.msl` | 98,304 bytes | immutable SQLite package; SHA-256 `1074682afec617b9b48cea45352083b7550e137940ac2f62f194cf478d8ca316` |
| `step-13-centered.sha256` | — | `sha256sum` line for the package |
| `package-summary-v1.json` | 1,956 bytes | canonical inspector projection (data contracts §3.5) |
| `payloads/model-1.bin` | 6,395 bytes | raw model MessagePack |
| `payloads/lattice-1.bin` | 125 bytes | raw lattice MessagePack |
| `payloads/job-1.bin` | 450 bytes | raw MMC job MessagePack |
| `payloads/index.json` | 1,217 bytes | kind, id, compressed/raw size and SHA-256 for every payload |

The package has one model, one lattice, and one job (`1` MMC), `format_version = 1`,
`application_id = MOCS`, `user_version = 1`, no `results` table, and no `-wal`/`-shm` sidecar. The model has
4 particles (`∅`, `O`, `Vc`, `H`), 8 P1 positions, 2 reference sites, 4 pair tables, no cluster table, one
collection, 4 directions, and 8 rules; the lattice is 2×2×2 with 64 particles.

- **Model signature:** `0740daff9ba08ff94148ce9ec4e908e0ccfe724da654d5499d27a1d56cc39e60`
- **Model identity:** `d160d1d37d4071e0109bae71fc9a2ff4e0e03a4e48c8e39fdabf2bada5ac4e49`
- **Lattice signature:** `9be91a8e78e64ad257c54243a290ff846426ec86afe68f3c4500545f56146dfb`

## Provenance

- **Source project:** `samples/step-13-centered/step-13-centered.mocprj`, derived from the Step 08
  `samples/simulations/centered-exchange.mocprj` (space group 5, C-centered, two sites, one MMC exchange).
- **Derivation:** site B uses the extended `{O, Vc, H}` particle set while site A keeps `{O, Vc}`; the added
  particle creates the per-node mask difference without changing the C-centered geometry.
- **Build path:** `ProjectJson` → `ModelCompiler` → `ParameterizationService` → `JobTemplateExpander` →
  `SupercellGenerator` → `SimulationPackageWriter`, with `PackageBuildOptions` defaults and
  `created_utc = 2026-11-20T12:00:00Z`.
- **Toolchain:** .NET SDK 11.0.100 / runtime 11.0.0, in-box Zstandard, default compression level 3.

## Equivalent starts and masks

The C-centering operations produce four translations of each site. The real writer emits directions whose
`EquivalentStartPositions` include a different P1 start, so `jump_mapping` contains cross-bucket entries:
for example, bucket position 2 maps direction 3 whose `position_id` is 6, position 3 maps direction 1
(`position_id` 7), position 4 maps direction 0 (`position_id` 0), and position 5 maps direction 2
(`position_id` 1). The reader must not require `direction.position_id == bucket_position`.

The extended B set also gives the MMC collection a per-node mask difference at nodes 0 and 1: the A-side
position admits `{O, Vc}` and the B-side position admits `{O, Vc, H}`. One real package therefore carries both
F-1 properties.

## Regeneration

Do not hand-edit the package or the payloads. Regenerate with the real writer:

```sh
MOCASSIN_REGENERATE_CENTERED_FIXTURE=1 \
  dotnet test src/dotnet/Mocassin.Results.Tests/Mocassin.Results.Tests.csproj -c Release \
  --filter "FullyQualifiedName~RegenerateCenteredPackageFixture"
```

The command rebuilds the package, the `package-summary-v1` golden, the SHA-256 line, and the extracted
payloads. `CenteredPackageFixtureTests` verifies the package, the summary, the payload index, the cross-bucket
mapping, the mask difference, and rebuild reproducibility. Package bytes are a writer+runtime artifact;
signatures and the canonical summary are the logical contract.
