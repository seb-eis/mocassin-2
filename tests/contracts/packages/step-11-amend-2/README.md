# Step 11 amendment-2 cluster expansion `.msl` fixture

This directory holds a real-writer `.msl` v1 package with one **non-singleton** two-member cluster table. It is
the acceptance fixture for the optional `code_expansions` member (`step-11-amend-2`).

## Files

| File | Size | Notes |
| --- | ---: | --- |
| `step-11-amend-2.msl` | 98,304 bytes | immutable SQLite package; SHA-256 `b8a9f47d9d93a2cf21d1a559471a838ca238dd0f8ebb3c46091d8b00010c6a51` |
| `step-11-amend-2.sha256` | — | `sha256sum` line for the package |
| `package-summary-v1.json` | 1,959 bytes | canonical inspector projection (data contracts §3.5) |
| `cluster-expansions-v1.json` | 771 bytes | expected table-0 map; 8 raw codes and 6 canonical codes |
| `payloads/model-1.bin` | 6,090 bytes | raw model MessagePack with `code_expansions` |
| `payloads/lattice-1.bin` | 101 bytes | raw lattice MessagePack |
| `payloads/job-1.bin` | 446 bytes | raw KMC job MessagePack |
| `payloads/index.json` | 1,215 bytes | kind, id, compressed/raw size and SHA-256 for every payload |

The package has one model, one lattice, and one KMC job, `format_version = 1`, `application_id = MOCS`,
`user_version = 1`, `created_utc = 2026-11-21T00:00:00Z`, no `results` table, and no `-wal`/`-shm` sidecar. The
compiled particle order is `0 = void, 1 = Ce, 2 = O, 3 = Vc`; the model has 5 P1 positions and one cluster table.

## Expected expansion map

Table 0 has six canonical occupation codes and eight enumerated raw codes. The two non-canonical codes are
numerically **smaller** than their canonical codes because the reduction is lexicographic in site order from the
centre while the 6-bit code places slot 0 in the least significant bits:

| Raw code | Canonical index | Canonical code |
| ---: | ---: | ---: |
| 8322 | 0 | 8322 |
| 8323 | 1 | 8323 |
| 8386 | 2 | 12418 |
| 8387 | 3 | 12419 |
| 12418 | 2 | 12418 |
| 12419 | 3 | 12419 |
| 12482 | 4 | 12482 |
| 12483 | 5 | 12483 |

The exact sequences are in `cluster-expansions-v1.json`; the test asserts them and the canonical self-entry for
every occupation code.

## Provenance

- **Source project:** `samples/step-11-cluster-expansion/step-11-cluster-expansion.mocprj`. It is the ceria
  primary two-member cluster (`centre 018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5f07`, offsets `[-0.25,0,0]` and
  `[+0.25,0,0]`), origin-shifted so the centre lies at the P-1 inversion origin; P-1 swaps the two member
  slots. It carries one KMC migration job at 2×2×2.
- **Writer commit:** `b3d967e05e84fd83062bfbc5d38f0cd7f8372cec` (the `step-11.1` implementation commit that
  adds the writer map). The package is written by that code state with the fixed timestamp below.
- **Build path:** `ProjectJson` → `ModelCompiler` → `ParameterizationService` → `JobTemplateExpander` →
  `SupercellGenerator` → `SimulationPackageWriter`, with `PackageBuildOptions` defaults and
  `created_utc = 2026-11-21T00:00:00Z`.
- **Request:** `PackageBuildRequest(<path>, 018f4c9a-7f6b-7c3a-9d2e-0000000011a0, "all", 2026-11-21T00:00:00Z)`.
- **Job count:** one KMC job; one lattice; one model row.
- **Toolchain:** .NET SDK 11.0.100 / runtime 11.0.0, in-box Zstandard, default compression level 3.
- **Logical equality:** `package-summary-v1.json` and both model/lattice signatures are byte-for-byte equal to
  the pre-map `amend-2-pre-map` fixture; only the model payload gains `code_expansions`.

## Regeneration

Do not hand-edit the package or the payloads. Regenerate with the real writer:

```sh
MOCASSIN_REGENERATE_CLUSTER_EXPANSION_FIXTURE=1 \
  dotnet test src/dotnet/Mocassin.Results.Tests/Mocassin.Results.Tests.csproj -c Release \
  --filter "FullyQualifiedName~RegenerateClusterExpansionPackageFixture"
```

`ClusterExpansionPackageFixtureTests` then verifies the package, compares the summary, checks the payload index,
rebuilds once more, and asserts byte determinism. The expectation JSON is a hand-written test fixture, not a
generated artifact; do not overwrite it from the writer output.
