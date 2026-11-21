# Step 14 multi-centre cluster `.msl` fixture

This directory holds a real-writer `.msl` v1 package with one **multi-centre** cluster table: the cluster is
centred on the four-position A site of the C-centered structure with two distinct member sites (B and C). It is
the Step 14 feature-6 acceptance fixture for the one-instance-per-slot-0-link cluster runtime.

## Files

| File | Size | Notes |
| --- | ---: | --- |
| `step-14-multi-centre-cluster.msl` | 98,304 bytes | immutable SQLite package; SHA-256 `ace6f38e2cd4ecfeddfb4ae85470705564ecc4fe3a29f4187b5d23a359ef9fe2` |
| `step-14-multi-centre-cluster.sha256` | — | `sha256sum` line for the package |
| `package-summary-v1.json` | 1,960 bytes | canonical inspector projection (data contracts §3.5) |
| `payloads/model-1.bin` | 10,083 bytes | raw model MessagePack |
| `payloads/lattice-1.bin` | 157 bytes | raw lattice MessagePack |
| `payloads/job-1.bin` | 450 bytes | raw MMC job MessagePack |
| `payloads/index.json` | 1,242 bytes | kind, id, compressed/raw size and SHA-256 for every payload |

The package has one model, one lattice, and one job (`1` MMC), `format_version = 1`, `application_id = MOCS`,
`user_version = 1`, no `results` table, and no `-wal`/`-shm` sidecar. The model has 4 particles (`∅`, `O`,
`Vc`, `H`), **12 P1 positions**, **3 reference sites** (A, B, C), 12 pair tables, **one cluster table**, one MMC
collection, 4 directions, and 8 rules. The lattice is 2×2×2 with 96 particles.

- **Model signature:** `5dd55dca332a5491a50c4cac6d9bc48d0af1d1e932eb8955f3323a7d73b2b0e6`
- **Model identity:** `34147482298bf158797539cfa662e175382f740a6f897b720ee3d1f0e3222130`
- **Lattice signature:** `608d61edf5d2fbf85e03c73926e694c7c49f4e56426fd8f6f846b7ad99bb146f`
- **Job:** 1 MMC, 1000 K, target 100 MCSP, seed
  `426cfebf844ead7ff1261cffd3ef1b73f02def76095bc6a170b1764499ecbe18`

## Cluster shape

The A centre site owns four P1 positions, so the writer emits **four slot-0 cluster links** (one per A position)
and one slot link for each member site (one B position and one C position). The context builds four instances,
each with its own centre position and resolved member images; the initialization test asserts the four slot
sets, the per-instance codes, and the per-instance energy mapping (with a synthetic distinct-energy table in the
Rust test, because the sample tables are zero). This exercises the multi-centre member-site fallback and the
one-instance-per-slot-0-link rule.

The project model's `ENM008` validator requires 2–8 surrounding offsets, so a one-member cluster cannot be
authored. The sample therefore uses two **distinct** member sites (B and C) rather than two images of one member
site; the latter remains the rejected OD-1 ambiguity and is not a passing fixture.

## Provenance

- **Source project:** `samples/step-14-multi-centre-cluster/step-14-multi-centre-cluster.mocprj`, derived from
  `samples/step-13-centered/step-13-centered.mocprj` (which stays byte-identical) with a third stable site C
  and the A-centred cluster.
- **Build path:** `ProjectJson` → `ModelCompiler` → `ParameterizationService` → `JobTemplateExpander` →
  `SupercellGenerator` → `SimulationPackageWriter`, with `PackageBuildOptions` defaults and
  `created_utc = 2026-11-21T12:00:00Z` (the fixture builder's fixed timestamp).
- **Toolchain:** .NET SDK 11.0.100 / runtime 11.0.0, in-box Zstandard, default compression level 3.

## Regeneration

Do not hand-edit the package or the payloads. Regenerate with the real writer:

```sh
MOCASSIN_REGENERATE_STEP14_MULTI_CENTRE_FIXTURE=1 \
  dotnet test src/dotnet/Mocassin.Results.Tests/Mocassin.Results.Tests.csproj -c Release \
  --filter "FullyQualifiedName~RegenerateStep14MultiCentreFixture"
```

`Step14MultiCentreFixtureTests` verifies the package, the canonical summary, the SHA-256 line, the payload
index, and rebuild reproducibility.
