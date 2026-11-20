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
4 particles (`∅`, `O`, `Vc`, `H`), 3 P1 positions, 6 pair tables, 1 cluster table, 2 collections, and 8 rules;
the lattice is 2×2×2 with 24 particles. The extended O2 particle set `{O, Vc, H}` gives the package a
**non-trivial per-node mask difference**: at node 0 of the KMC collection the forward direction (O1,
`{O, Vc}`) does not admit `H`, while the reverse direction (O2, `{O, Vc, H}`) does; the MMC collection has
the same difference at nodes 0 and 1.

- **Model signature:** `abbd7062c53dd66a42009c52ffd7169e75645d78dac3c4cf7d5f94f7bddb2e79`
- **Model identity:** `f0dd41980d90e803a00f726e588f2895852d15f9fa7f658965b5c91495b94ba0`
- **Lattice signature:** `eee85b1aa92041b00cba7362acf930627a75becf077eb8dc9c4d90a7f3993475`

## Provenance

- **Source project:** `samples/step-13-populated/step-13-populated.mocprj` (a small P1 ceria-derived mechanism
  with stable/unstable oxygen environments, a three-site migration-edge cluster, one KMC migration simulation,
  one MMC O/V exchange simulation, an extended O2 particle set that creates the per-node mask difference, and a
  two-collection job template at 2×2×2).
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

## Fixture properties and limits

The fixture carries the **non-trivial per-node mask difference** the Step 13 design requires (see the shape
above). It does **not** carry an equivalent-start mapping (`jump_mapping` bucket position different from
`direction.position_id`). That property is blocked by the Step 08 transition compiler, not by this fixture's
construction:

- `TransitionCompilationStage.AddDraft` builds `DirectionKey` from the relative path, and the first relative
  component is `(0, 0, 0, start.P)`. Two drafts can therefore merge only when they have the same start `P`;
  `EquivalentStartPositions` can never contain a position different from the representative start, so
  `EncodingCompilationStage.BuildMappings` never places a direction in a different start bucket.
- The checked-in Fm-3m `msl-v1` ceria package (two directions) and a P-1 variant of this sample (six positions,
  plain `spaceGroup.number = 2` change, verified `ok`) both show no cross-bucket mapping. The P-1 `PKG010`
  claim recorded during feature 6 is **not reproducible**: that P-1 build verifies, and no checked-in P-1
  artifact ever produced `PKG010`.
- The verifier's `byte_position >= site_count` check is also inconsistent with the payload contract, which
  defines `byte_position` as the cluster **slot** (`0` is the centre), not a site index. No valid package in this
  repository trips it because duplicate-site clusters currently fail earlier with `CLI007`; it is recorded as a
  latent Step 11 verifier defect, not a Step 13 fix.

The reader acceptance for an equivalent-start bucket is covered by the synthetic
`a_direction_may_be_mapped_into_another_start_bucket` test, plus the collection-union tests in
`src/rust/crates/mocassin-msl/src/map/tests/`. The Step 13 amendment
[`step-13-amend-1`](../../../../docs/planning/amendments/step-13-amend-1-fixture-properties.md) records the
requested acceptance of that limitation and the compiler/verifier defect owners. Step 16 must re-plan its
exact per-direction fixture against the compiler fix.

The fixture still covers the populated pair/cluster tables, both simulation kinds, the real-writer payload and
signature paths, and the MMC validation rules.
