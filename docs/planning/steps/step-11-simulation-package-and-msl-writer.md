# Step 11 — Simulation Package and `.msl` Writer

**Phase:** C. Compilation  
**Depends on:** 08, 09, 10
**Delivers:** A versioned SQLite simulation package containing one parameterized model, content-addressed
initial lattices, and streamed job payloads, with a verified publication barrier, contract fixtures, and a
read-only inspection/verification library for the build CLI.

## Implementation status

- **Status:** In progress
- **Step branch:** `step/11-simulation-package-and-msl-writer`
- **Step CI:** —
- **Main merge:** —
- **Step tag:** —
- **Audit:** Not requested
- **Design review:** Ready with notes — accepted 2026-11-15 (record `32411c9`) — [record](../reviews/step-11.md)
- **Amendments:** [`step-11-amend-1`](../amendments/step-11-amend-1-dotnet-11-native-zstd.md) — .NET 11
  toolchain and native Zstandard — **Integrated** (`00753e7`); design review `Ready with notes` accepted
  2026-11-15 ([record](../reviews/step-11-amend-1.md))
- **Feature list:** Approved 2026-11-15 (seven features)
- **Last updated:** 2026-11-16

| Feature | Branch | Status | Local gates | Step CI | Squash commit |
| --- | --- | --- | --- | --- | --- |
| 1 Package contracts and lattice signature | `feature/step-11-simulation-package-and-msl-writer/package-contracts-and-lattice-signature` | Squashed | `dotnet`, `structure`, `api`, `contracts` exit 0 | Not run (no per-feature CI) | `eb8513d` |
| 2 Package manifest and schema | `feature/step-11-simulation-package-and-msl-writer/package-manifest-and-schema` | Squashed | `dotnet`, `structure`, `api`, `contracts`, `docs` exit 0 | Not run (no per-feature CI) | `9b754af` |
| 3 SQLite schema and publication barrier | `feature/step-11-simulation-package-and-msl-writer/sqlite-schema-and-publication` | Squashed | `dotnet`, `structure`, `api`, `contracts`, `docs` exit 0 | Not run (no per-feature CI) | `c0d8d64` |
| 4 Model payload encoding | `feature/step-11-simulation-package-and-msl-writer/model-payload-encoding` | Squashed | `all --require` exit 0 | Not run (no per-feature CI) | `bdafaa4` |
| 5 Job and lattice payload encoding | `feature/step-11-simulation-package-and-msl-writer/job-and-lattice-encoding` | Squashed | `all --require` exit 0 | Not run (no per-feature CI) | `4fe6225` |
| 6 Package verification and inspection | `feature/step-11-simulation-package-and-msl-writer/package-verification` | Squashed | `all --require` exit 0 | Not run (no per-feature CI) | `cbe345f` |
| A1 .NET 11 toolchain and native Zstandard | `feature/step-11-simulation-package-and-msl-writer/amend-1-dotnet-11-native-zstd` | Squashed | `all --require` exit 0; one-time cross-TFM ApiCompat 9/9 | Not run (no per-feature CI) | `00753e7` |
| A1-fix Lattice decode guard | `feature/step-11-simulation-package-and-msl-writer/fix-lattice-decode-guard` | Squashed | `all --require` exit 0 | Not run (no per-feature CI) | `25a3787` |
| 7 Fixtures, performance, documentation, and closeout | `feature/step-11-simulation-package-and-msl-writer/fixtures-performance-and-docs`; `feature/step-11-simulation-package-and-msl-writer/fix-windows-canonical-json` | Squashed | `all --require` exit 0 | Not run (no per-feature CI) | `e25811f` (fixtures/Part B/docs); `0ae05a4` (Windows canonical JSON) |

### Approved feature list

| # | Feature | Branch | Planned scope |
| --- | --- | --- | --- |
| 1 | Package contracts and lattice signature | `feature/step-11-simulation-package-and-msl-writer/package-contracts-and-lattice-signature` | `Mocassin.Contracts.Packages` DTOs for the model, job, and lattice payloads; payload format constants; `LatticeSignatureV1`; `PKG` diagnostic descriptors; `PublicAPI.Unshipped.txt`; golden lattice-projection vectors and the projection checker |
| 2 | Package manifest and schema | `feature/step-11-simulation-package-and-msl-writer/package-manifest-and-schema` | Finalize `signatures.lattice`; add `formats.package.payloads` and the separate `package-payloads-v1.schema.json`; remove the undefined job columns; extend `tools/contracts/generate.py`; regenerate constants/descriptors; record the ADR 0009 pre-release basis |
| 3 | SQLite schema and publication barrier | `feature/step-11-simulation-package-and-msl-writer/sqlite-schema-and-publication` | Writer options/factory, schema creation, pragmas, `application_id`/`user_version`, `schema_info`, batching, WAL→DELETE, fsync file and directory, atomic rename, cancellation/failure cleanup, result SHA-256, licence check for the new reference edges |
| 4 | Model payload encoding | `feature/step-11-simulation-package-and-msl-writer/model-payload-encoding` | Add `PairInteractionPreview.EquivalentImages` and emit one `CompiledPairLink` per periodic image (regenerate the model/parameterization signature fixtures and sample `modelSignature` values); `ParameterizedModel` → Contracts model payload (particles, structure with per-image pair links, energy, transitions with variable `tracker_order`, simulations); Zstd+MessagePack; content-addressed insert; write-time signature re-encode; unit/property tests |
| 5 | Job and lattice payload encoding | `feature/step-11-simulation-package-and-msl-writer/job-and-lattice-encoding` | `GeneratedLattice` → `InitialLattice`; `JobDescriptor` → job payload (flags composition, Cartesian field direction, optimizers, extension JSON, doping summary); lattice dedup; payload-derived columns; progress/cancellation |
| 6 | Package verification and inspection | `feature/step-11-simulation-package-and-msl-writer/package-verification` | Read-only `MslPackageInspector`/`PackageVerifier` in `Mocassin.Results.Packages`; bounded decode; `PKG001`–`PKG010`; `package-summary-v1`; reports; corruption/truncation/decompression-bomb tests |
| 7 | Fixtures, performance, documentation, and closeout | `feature/step-11-simulation-package-and-msl-writer/fixtures-performance-and-docs` | Checked-in `.msl` fixtures plus `package-summary-v1` goldens and extracted payloads; the 10⁴-job budget evidence; reference/developer pages; parity and status updates; closeout evidence and audit request |

The feature list was approved once by the human owner together with the `Ready with notes` design verdict on
2026-11-15. That approval created the step branch and marks the step `In progress`; individual features are
squash-merged without a further approval.

### Feature log

Filled during implementation. Each squash records the branch, local gates, and the squash commit; no CI runs
per feature.

#### Feature 1 — Package contracts and lattice signature

- **Branch:** `feature/step-11-simulation-package-and-msl-writer/package-contracts-and-lattice-signature`
- **Squash commit:** `eb8513d` (`step-11: add the .msl payload contracts and lattice signature`).
- **Local gates:** `./tools/verify.sh dotnet` exit 0 (build, all solution tests including 37 Contracts and 375
  Packages tests, format); `./tools/verify.sh structure` exit 0; `./tools/verify.sh api` exit 0;
  `./tools/verify.sh contracts` exit 0 (contract artifacts current, model 5 / parameterization 4 / lattice 2
  projection fixtures, process consistency, 37 tool tests, SPDX, secrets, cargo-deny).
- **Step CI:** not run — feature branches do not trigger CI; the closeout run is the only step-branch run.
- **Diff summary:** added `Mocassin.Contracts.Packages` payload DTOs for the model, structure, energy,
  transition, simulation, job, and lattice payloads with explicit snake_case MessagePack keys; added
  `PackageFormatConstants`, `LatticeSignatureV1`, and the `PKG001`–`PKG010` descriptors; added Contracts and
  Packages tests (DTO shape, MessagePack keys, projection and validation, real compiler/`SupercellGenerator`
  lattice vectors); added two checked-in lattice projection vectors and
  `tools/contracts/check_lattice_projection.py`, wired into `verify.sh contracts`; extended the manifest
  signature-vector list and regenerated the contract artifacts; refreshed `PublicAPI.Unshipped.txt` and the
  Contracts ApiCompat baseline.
- **Known limitations / follow-ups:** the Contracts DTOs use MessagePack annotations through the existing
  `Mocassin.Compilation` package reference; the payload-encoder feature adds the explicit MessagePack
  reference when it lands. The generated `signatures.lattice` descriptor is still the Step 00
  skeleton with `dopingInfo`; feature 2 finalizes it and the checker then cross-validates the final descriptor.
  The lattice vectors bind the current `modelSignature`, so feature 4's per-image pair-link change must
  regenerate them with the Step 08/09 vectors. The `PKG` descriptors are not yet composed into
  `DiagnosticCatalog.Default`; that belongs to the schema/verification features.
- **Parity:** no parity-matrix row is completed by this contract-only feature.

#### Feature 2 — Package manifest and schema

- **Branch:** `feature/step-11-simulation-package-and-msl-writer/package-manifest-and-schema`
- **Squash commit:** `9b754af` (`step-11: finalize the package payload manifest and schema`).
- **Local gates:** `./tools/verify.sh contracts` exit 0 (contract artifacts current at 19 files, model 5 /
  parameterization 4 / lattice 2 projection fixtures, process consistency, 45 tool tests, SPDX 545 files,
  secrets, cargo-deny); `./tools/verify.sh dotnet` exit 0 (all solution tests including 382 Packages tests);
  `./tools/verify.sh structure` exit 0; `./tools/verify.sh api` exit 0; `./tools/verify.sh docs` exit 0.
- **Step CI:** not run — feature branches do not trigger CI.
- **Diff summary:** finalized `signatures.lattice` to the five-field `LatticeSignatureV1` projection with
  `status: final` and no `dopingInfo`; added `formats.package.payloads` with the top-level model/job/lattice
  payload descriptors and their `format_version` values; extended `generate.py` to validate payload descriptors
  and emit the separate `contracts/generated/schema/package-payloads-v1.schema.json` (row schema unchanged);
  expressed the payload unknown-key policy as `formats.package.payloads.unknownFields = "ignore"` so the
  generator derives `additionalProperties` from the manifest (R2-8); removed `jobs.norm_factor` /
  `jobs.base_frequency_hz`; added the missing `jobs.write_out_count` column required by data contracts §3.2;
  kept `jump_directions.object_id` and did not add `jump_collections.object_id`; regenerated all 19 generated
  artifacts; added Python generator/descriptor tests and C# payload-schema-vs-DTO/descriptor tests; updated
  `docs/reference/file-layout.md`.
- **Known limitations / follow-ups:** the payload schema deliberately types nested maps and arrays only as
  `object`/`array` at the fidelity the manifest entry uses; the full payload field list remains in data contracts
  §3.3–§3.4 and the DTOs. No previous-major `.msl` fixture exists; the current-only fixture policy starts at v2.
  `write_out_count` was a pre-existing manifest drift against the already-frozen data contract, not a new shape
  decision.
- **Parity:** no parity-matrix row is completed by this contract-only feature.

#### Feature 3 — SQLite schema and publication barrier

- **Branch:** `feature/step-11-simulation-package-and-msl-writer/sqlite-schema-and-publication`
- **Squash commit:** `c0d8d64` (`step-11: add the SQLite schema and publication barrier`).
- **Local gates:** `./tools/verify.sh dotnet` exit 0 (all solution tests including **399** Packages tests,
  format clean); `./tools/verify.sh structure` exit 0; `./tools/verify.sh api` exit 0 (NuGet licence policy OK
  for **57** packages, ApiCompat clean); `./tools/verify.sh contracts` exit 0 (19 artifacts current, projection
  fixtures, 45 tool tests, SPDX 560 files, secrets, cargo-deny); `./tools/verify.sh docs` exit 0.
- **Step CI:** not run — feature branches do not trigger CI.
- **Licence result (n-5):** `Microsoft.Data.Sqlite/10.0.14` and `Microsoft.Data.Sqlite.Core/10.0.14` are **MIT**;
  the transitive `SQLitePCLRaw.*/2.1.13` packages are **Apache-2.0**; all are on the allow-list
  (`check_nuget_licenses.py`: `NuGet licence policy OK (57 packages)`). `ZstdSharp.Port` is not referenced yet;
  its licence result is recorded when feature 4 adds the encoder.
- **Diff summary:** added `Mocassin.Packages.SimulationPackage` with `PackageBuildRequest` (including the
  explicit `TargetPath` clarification), `PackageBuildOptions`, `PackageWriteResult`, `JobPackage`,
  `ISimulationPackageWriter`, and `SimulationPackageWriter`; created the exact v1 SQLite schema
  (`schema_info`/`models`/`lattices`/`jobs` plus the two indices, `application_id=0x4D4F4353`,
  `user_version=1`), build pragmas (`page_size=8192`, WAL, `synchronous=NORMAL`, `foreign_keys=ON`,
  `temp_store=MEMORY`), batch commits, checkpoint + `journal_mode=DELETE`, optional `VACUUM`, and
  `PRAGMA optimize`; implemented temp-file creation and cleanup, SHA-256 of the published bytes, file fsync,
  Unix directory fsync, atomic rename, `PKG009` advisory warning, and failure/cancellation cleanup; added the
  contract-typed internal `PackageModelRow`/`PackageLatticeRow`/`PackageJobRow` seams for features 4/5 and 17
  real-SQLite tests; added `Microsoft.Data.Sqlite` to `Mocassin.Packages`, refreshed `PublicAPI.Unshipped.txt`
  and the Packages ApiCompat baseline; documented the atomic publication in `docs/reference/file-layout.md`.
- **R2-7 resolution:** the file fsync uses `FileStream.Flush(true)`; the containing-directory fsync uses a
  minimal Unix libc `open`/`fsync`/`close` interop (`DllImport`, `UnmanagedType.LPUTF8Str`, `SetLastError`,
  `CharSet.Ansi`). .NET has no managed directory-fsync API. Windows has no equivalent directory-durability
  primitive in this path, so the Windows branch skips the directory call and relies on the file fsync plus atomic
  replace. This is the solution's first platform-specific native code and a §3.13 trigger: the closeout must
  dispatch the manual **Windows** platform job; the macOS Unix path is the same code exercised on Ubuntu, so a
  macOS run is optional/manual. If the Windows run is unavailable, it is an accepted risk in the audit.
- **Known limitations / follow-ups:** `WriteModelAsync`/`WriteJobAsync` are the public encode paths and throw
  `NotSupportedException` until features 4/5 fill them; feature 3 exercises the schema/publication barrier
  through the contract-typed internal row seam. `PRAGMA optimize` creates SQLite's internal `sqlite_stat1`
  table, which is not a contract table and is ignored by the schema test. The Windows directory-fsync skip and
  the required Windows closeout run are recorded above.
- **Parity:** no parity-matrix row is completed by this schema/publication feature; the package writer rows stay
  `Planned` until the encoders, fixtures, and CLI integration land.

#### Feature 4 — Model payload encoding

- **Branch:** `feature/step-11-simulation-package-and-msl-writer/model-payload-encoding`
- **Squash commit:** `bdafaa4` (`step-11: encode the parameterized model payload`).
- **Local gates:** `./tools/verify.sh all --require` exit 0 after the format fixes — .NET build 0 warnings and all
  solution tests (407 Packages tests, 96 Compilation, 223 Symmetry, 37 Contracts, 683 Core, 17 Cli, 1 Results,
  1 App, 1 Architecture); NuGet licence policy OK (**58** packages); ApiCompat clean after refreshing the
  `Mocassin.Symmetry` and `Mocassin.Contracts` baselines; Rust fmt/clippy/test; Python pytest and package build;
  docs strict build; contract artifacts current, model 5 / parameterization 4 / lattice 2 projection fixtures,
  process consistency, 45 tool tests, SPDX, secrets, cargo-deny.
- **Step CI:** not run — feature branches do not trigger CI.
- **Diff summary:** added the additive `PairInteractionPreview.EquivalentImages` field and made
  `PairCompilationStage` emit one `CompiledPairLink` per `PairShell.EquivalentImages` element (representative
  included), so the existing four-field model-signature link projection signs every runtime image; added the
  real `ParameterizedModel` → `ModelPayload` mappers (`ModelPayloadMapper`, `StructurePayloadMapper`,
  `EnergyPayloadMapper`, `TransitionPayloadMapper`, `SimulationPayloadMapper`, `FieldDirectionConverter`),
  the Zstd+MessagePack `PayloadEncoder` with a decompressed-size guard, and the `WriteModelAsync` write-time
  parameterization re-encode; added `MessagePack` and `ZstdSharp.Port` references to `Mocassin.Packages`;
  added 8 model-payload tests (round-trip, signature re-encode, one-to-one per-image links, multi-image
  expansion, tracker order, determinism, decode guard, field direction) and a writer integration test; refreshed
  `PublicAPI.Unshipped.txt` and the `Mocassin.Symmetry`/`Mocassin.Contracts` ApiCompat baselines; updated the
  Step 09 golden-signature note.
- **Regenerated vectors (old → new):**
  - `model-barium-zirconate`: `c0fd836e…` → `5e62f9dd…`.
  - `parameterization-barium-zirconate`: `a7f6ebd9…` → `828a5494…` (`modelSignature` → `5e62f9dd…`).
  - `parameterization-ceria`: `cac3b231…` → `8f86a7f9…` (`modelSignature` → `a9e0a45a…`).
  - `parameterization-chiral-p1`: `ac5b381e…` → `51263129…` (`modelSignature` `6d49e4d2…` → `ca44ca89…`).
  - `lattice-ceria-1x1x1`: `0d8327cd…` → `55f9e076…` (`modelSignature` → `a9e0a45a…`).
  - `lattice-ceria-2x2x2`: `2b132c6c…` → `d8bd1660…` (`modelSignature` → `a9e0a45a…`).
  - Sample/template `modelSignature` values: ceria parameters `3cee4758…` → `a9e0a45a…`; barium-zirconate
    parameters `c0fd836e…` → `5e62f9dd…`; `project-chiral-p1.mocprj` regenerated to `ca44ca89…`.
  - `model-ceria-kmc`, `model-ceria-mmc`, `model-vehicle5`, `model-interstitialcy5`, and their parameterization
    vectors are unchanged because those compiled models have no multi-image pair shells.
  - Commands: `MOCASSIN_UPDATE_GOLDENS=1 dotnet test` for `ProjectionFixtureTests`,
    `ParameterizationGoldenFixtureTests`, `ParameterizationChiralFixtureTests`, and
    `LatticeProjectionFixtureTests`; then the manifest vector metadata was refreshed from the generated fixture
    metadata and `python tools/contracts/generate.py` regenerated `signature-vectors.json` and
    `contracts/generated/fixtures/descriptors.json`.
- **Licence result (n-5):** `ZstdSharp.Port/0.8.8` is **MIT**; `MessagePack/3.1.12` is **MIT**; the transitive
  `SQLitePCLRaw.*`/`Microsoft.Data.Sqlite` edges remain MIT/Apache-2.0. `check_nuget_licenses.py`:
  `NuGet licence policy OK (58 packages)`.
- **Known limitations / follow-ups:** `WriteJobAsync` and the job/lattice payload encoders remain feature 5; the
  Step 14 runtime `cell + offset` expansion test is the cross-step acceptance test. The model signature is an
  identity string and is not re-encoded from the payload; the writer checks the decoded parameterization
  projection and the compiled-model identity instead.
- **Parity:** no parity-matrix row is completed by the model payload feature; the package writer rows stay
  `Planned` until job/lattice encoding, verification, fixtures, and CLI integration land.

#### Feature 5 — Job and lattice payload encoding

- **Branch:** `feature/step-11-simulation-package-and-msl-writer/job-and-lattice-encoding`
- **Squash commit:** `4fe6225` (`step-11: encode the generated lattices and expanded jobs`).
- **Local gates:** `./tools/verify.sh all --require` exit 0 — .NET build 0 warnings and all solution tests
  (414 Packages, 685 Core, 223 Symmetry, 96 Compilation, 37 Contracts, 17 Cli, 1 Results, 1 App,
  1 Architecture); NuGet licence policy OK (58 packages); ApiCompat clean after refreshing the
  `Mocassin.Core` and `Mocassin.Packages` baselines; Rust fmt/clippy/test; Python pytest and package build;
  docs strict build; contract artifacts current, projection fixtures, process consistency, 45 tool tests, SPDX,
  secrets, cargo-deny.
- **Step CI:** not run — feature branches do not trigger CI.
- **Diff summary:** added `LatticePayloadMapper` (flat row-major particles, dimensions, five-field signature)
  and `JobPayloadMapper` (resolved settings, `SolverJobFlagsMapper.Compose`, normalized Cartesian field,
  compiled optimizers, canonical extension JSON, doping summary); extended `PayloadEncoder` with job/lattice
  Zstd+MessagePack round-trips and decompressed-size guards; implemented `WriteJobAsync` with real generated
  lattices and descriptors, content-addressed lattice insert/dedup, payload-derived columns, strictly ascending
  job ids (gaps preserved), batching/cancellation, and `LatticeReuseFactor`; added the public
  `JobExtension.ToCanonicalJson()` helper reusing the project canonicalizer; added 8 real-sample
  encoding/dedup/columns/job-id/cancellation tests and 2 canonical-JSON tests; refreshed
  `PublicAPI.Unshipped.txt` and the Core/Packages ApiCompat baselines; updated the step page/status.
- **Dedup and ids:** the six-job `samples/jobs/ceria-temperature-sweep.mocprj` builds six distinct lattices
  (1.0 reuse factor); an identical-lattice pair plus one different lattice yields 2 rows and a 1.5 reuse
  factor; a 1/3 job-id pair preserves the gap.
- **Licence result:** no new reference edge in this feature; the feature-3/4 `Microsoft.Data.Sqlite`,
  `MessagePack`, and `ZstdSharp.Port` results stand (MIT/Apache-2.0; 58 packages OK).
- **Known limitations / follow-ups:** Step 12 owns the user-facing progress surface; the writer streams one
  job at a time and honors cancellation. Step 6 owns the verifier and fixtures; Step 7 owns the checked-in
  `.msl` fixture, benchmark, and canonical-summary golden.
- **Parity:** no parity-matrix row is completed by this encoding feature; the package writer rows stay
  `Planned` until verification, fixtures, and CLI integration land.

#### Feature 6 — Package verification and inspection

- **Branch:** `feature/step-11-simulation-package-and-msl-writer/package-verification`
- **Squash commit:** `cbe345f` (`step-11: add package verification and inspection`).
- **Local gates:** `./tools/verify.sh all --require` exit 0 — .NET build 0 warnings and all solution tests
  (415 Packages, 18 Results, 685 Core, 223 Symmetry, 96 Compilation, 37 Contracts, 17 Cli, 1 App,
  1 Architecture); NuGet licence policy OK (58 packages); ApiCompat clean after refreshing the
  `Mocassin.Contracts`/`Mocassin.Results` baselines; Rust fmt/clippy/test; Python pytest and package build;
  docs strict build; contract artifacts current, projection fixtures, process consistency, 45 tool tests, SPDX,
  secrets, cargo-deny.
- **Step CI:** not run — feature branches do not trigger CI.
- **Diff summary:** added `Mocassin.Results.Packages` with `IMslPackageInspector`/`MslPackageInspector`,
  `IPackageVerifier`/`PackageVerifier`, `PackageInspection`, `PackageVerificationReport`,
  `PackageVerificationOptions`, and `package-summary-v1`; added a read-only `PackageReader`, bounded
  Zstd+MessagePack decoder, payload invariant checks, and the ordered `PKG001`–`PKG010` engine; composed
  `PackageDiagnosticDescriptors.All` into `DiagnosticCatalog.Default` and documented every PKG code; added 18
  real-package tests covering inspection, summary determinism, read-only/no-WAL, and every PKG code; refreshed
  `PublicAPI.Unshipped.txt` and the Contracts/Results ApiCompat baselines.
- **`PKG` evidence:** `PKG001` text file; `PKG002` `application_id=0`; `PKG003` dropped
  `ix_jobs_temperature`; `PKG004` `X'0102'` model payload; `PKG005` wrong model signature and wrong lattice
  signature; `PKG006` job `model_id=999` with FK off; `PKG007` changed `temperature_k`; `PKG008` `results`
  table and a `-wal` sidecar; `PKG009` advisory size 1 (warning only, `IsValid` true); `PKG010` model
  position `site_index=999`. Every failing fixture is a real SQLite package built by the real writer.
- **R2-3 closure:** the verifier no longer asserts "the shell representative is present" or an independent
  tracker-order node count. `tracker_order` is checked as non-empty, length ≤ the documented ten six-bit
  node slots, with non-sentinel values below `global_tracker_count`; the writer retains the
  representative-path-length assertion. No new payload field was invented.
- **R2-9 closure:** `DiagnosticCatalogTests.DefaultIsTheUnionOfTheDomainCatalogsAndStep07Codes` now includes
  `PackageDiagnosticDescriptors.All`; `DiagnosticDocumentationTests` covers the ten new PKG codes and headings.
- **Licence result (n-5):** `Mocassin.Results` now references `Microsoft.Data.Sqlite` (MIT),
  `MessagePack` (MIT), and `ZstdSharp.Port` (MIT); transitive `SQLitePCLRaw` remains Apache-2.0.
  `check_nuget_licenses.py`: `NuGet licence policy OK (58 packages)`.
- **Known limitations / follow-ups:** `inspect`/`verify` CLI wiring and PKG→exit-code mapping are Step 12;
  the checked-in `.msl` fixture, canonical-summary golden, and benchmark are Step 7/feature 7.
- **Parity:** the package inspection/verification row remains `Improvement` because the Step 12 CLI half is
  still pending; the read-only library half is complete.

#### A1 — .NET 11 toolchain and native Zstandard

- **Branch:** `feature/step-11-simulation-package-and-msl-writer/amend-1-dotnet-11-native-zstd`
- **Squash commit:** `00753e7` (`step-11: retarget to .NET 11 and replace the payload codec`). Feature commits:
  `e2b00ea` pre-amendment fixture and codec goldens (net10 writer), `b0f189e` retarget, `771c28b` codec swap,
  `1f7f61a` amended-tree evidence. Approval commit on the step branch: `683b7c4`.
- **Local gates:** `./tools/verify.sh all --require` exit 0 — build and 1,506 tests (0 failed, 0 warnings),
  format, `api`, `structure`, `contracts` (19 artifacts current; 5/4/2 projections), `policy` (NuGet licences
  52 packages, 9 ApiCompat comparisons, SPDX 593 files, secrets 965 files, cargo-deny), Rust, Python, and the
  strict docs build (DocFX 0 warnings/0 errors). One-time cross-TFM ApiCompat: 9/9 assemblies report "no
  breaking changes"; `PublicAPI.*.txt` unchanged.
- **Step CI:** not run — feature branches do not trigger CI; the closeout run is the only step-branch run.
- **Diff summary:** `global.json` to SDK `11.0.100`/`latestPatch`; all 21 solution projects plus the reference
  harness to `net11.0`; `Microsoft.NET.Test.Sdk 18.12.0`; ApiCompat tool `11.0.100` and the baseline renamed and
  regenerated at `tests/contracts/api-baseline/net11.0/`; DocFX paths/metadata and the containerfile (11.0.100
  only, exact-count assertions, ReportGenerator 5.6.0, carried-over `COPY global.json` pin); the eight active
  documentation pages plus the parity row and the bug-report placeholder; `ZstdSharp.Port` removed and the
  payload codec replaced by the one-shot in-box `ZstandardEncoder`/`ZstandardDecoder` (writer, reader, and test
  support); three analyzer adaptations required by SDK 11 (`CA1517` read-only span, two `CA2026` `JsonElement`
  conversions); the pre-amendment `.msl` fixture, 12 raw payloads, and 24 golden frames checked in with tests.
- **Authorization:** the human owner authorized the `.NET 11` stack/convention update in `AGENTS.md` §1/§8 as
  part of the approved amendment (2026-11-15); the change is factual toolchain wording, not a process-rule change.
- **Cross-version evidence:** the `net10.0` fixture verifies on `net11.0` and its canonical summary matches the
  golden (PR-fast). The reverse check (net10 build at `e2b00ea` reading a net11-written package) reported
  `IsValid=true`, 10 checks, 0 diagnostics, and the same summary SHA-256 `60c95e1b…`; the net11 writer
  reproduced the net10 package SHA-256 `053e4c8f…` because the payload frames are byte-identical.
- **Part A re-run:** refined gate PASS in both the controlled and default-JIT runs; size 0.000 % (24/24
  byte-identical goldens), encode worst +4.12 %, worst small-payload decode +0.1055 µs (≤ 0.15 µs), ≥ 8 KiB
  decode +2.08 % (≤ +10 %), throughput ≥ 297 MB/s (≥ 100), 240/240 checks.
- **Known limitations / follow-ups:** the container image was not built (owner-accepted risk, deferred to
  closeout); the `VSTEST_ARTIFACTS_DIRECTORY` message persists because `/run/user/1000` is absent here and is
  recorded as benign; the ReferenceHarness builds on `net11.0` with the GPL clone copied outside the repository
  and `-p:NuGetAudit=false` (the clone's legacy packages carry audit warnings; one RID warning), and it remains
  outside the solution gate; `fix-lattice-decode-guard` remains a separate fix feature before the audit freeze;
  Part B is feature 7; the superseded `docs/dotnet-11-container` branch (`9ab4bb8`) is still present and is
  removed as closeout cleanup.
- **Parity:** the `.NET 11` row in `appendix-b-parity-matrix.md` §10 is now `Implemented`.

#### A1-fix — Lattice decode guard

- **Branch:** `feature/step-11-simulation-package-and-msl-writer/fix-lattice-decode-guard`
- **Squash commit:** `25a3787` (`step-11: bound the read-only lattice decode guard`); feature commit `bc9eaed`.
- **Local gates:** `./tools/verify.sh all --require` exit 0 — build and 1,514 tests (0 failed, 0 warnings),
  format, `api`, `structure`, `contracts`, `policy`, Rust, Python, and the strict docs build.
- **Diff summary:** the read-only reader no longer passes `int.MaxValue` to the lattice decoder.
  `PackageReader.ReadLatticesAsync` builds a per-lattice bound from the referencing `jobs` row
  (`lattice_id`, `model_id`, `lattice_a/b/c`) and the decoded model payload's `structure.position_count`, and
  decodes each lattice with it. `LatticePayloadBound.Compute` applies `4 × a × b × c × position_count` with
  `checked` arithmetic and falls back to the cap on invalid or overflowing dimensions.
  `PackageVerificationOptions.MaxLatticePayloadBytes` (init property, default 512 MiB) is the hard cap and the
  orphan-lattice fallback; `PublicAPI.Unshipped.txt` records the addition. Tests: four bound unit tests and four
  real-SQLite integration tests — a referenced lattice frame above the derived 3,456-byte bound yields `PKG004`
  with that guard in the message; an orphan frame above a 4,096-byte cap yields `PKG004`; an orphan within the
  cap still decodes; a legitimate 40×40×40 (256,000-position) lattice verifies. No `.msl` format, payload,
  signature, or diagnostic code changed; `Mocassin.Results` still references only `Core + Contracts`.
- **Known limitation:** a legitimate lattice whose decompressed payload exceeds the 512 MiB default (about
  134M positions at the writer's 4× factor) needs a raised `MaxLatticePayloadBytes`; the option is per call.
- **Resolves:** A1-10 / R2-01 (the amendment review's routed residual minor).

#### Feature 7 — Fixtures, performance, documentation, and closeout

- **Branch:** `feature/step-11-simulation-package-and-msl-writer/fixtures-performance-and-docs`; follow-up
  `feature/step-11-simulation-package-and-msl-writer/fix-windows-canonical-json`.
- **Squash commits:** `e25811f` (`step-11: add fixtures, the Part B budget, and closeout documentation`) and
  `0ae05a4` (`step-11: make canonical JSON newlines platform-independent`).
- **Local gates:** `DOTNET_ROOT=/tmp/opencode/dotnet11 PATH=/tmp/opencode/dotnet11:$PATH ./tools/verify.sh all
  --require` exit 0 on 2026-11-16 — .NET build 0 warnings / 0 errors and **1,519** tests passing
  (App 1, Contracts 37, Cli 17, Symmetry 223, Results 39, Core 685, Packages 420, Compilation 96,
  Architecture 1), format clean; NuGet licence policy OK (52 packages), no vulnerable packages, ApiCompat
  clean for 9 assemblies; Rust fmt/clippy/test; Python 3 tests and package build; strict Zensical build,
  DocFX 0 warnings / 0 errors, rustdoc; contract artifacts current (19 files; model 5 / parameterization 4 /
  lattice 2 projection fixtures), space-group catalog current (274 settings), process consistency, 45 tool
  tests, SPDX 600 source files, 979 secret-scanned files, cargo-deny clean. The running dev image predates
  amendment 1 and exposes only SDK 10.0.403 (`dotnet --list-sdks`); the containerfile already pins 11.0.100
  only. The pinned SDK was therefore installed under `/tmp/opencode/dotnet11`, and the gate ran on **SDK
  11.0.100 / runtime 11.0.0** from that root; no repository file changed. The follow-up bookkeeping commit
  re-ran `./tools/verify.sh docs --require` and `./tools/verify.sh policy --require` with the same SDK root:
  both exit 0, and the policy run re-confirmed the 19 contract files, 274 settings, 45 tool tests, 600 SPDX
  files, and 979 secret-scanned files above.
- **Step CI:** not run — no per-feature CI; the single Ubuntu closeout dispatch is still pending.
- **Diff summary:** added the checked-in `.msl` v1 fixture `tests/contracts/packages/msl-v1/` (98,304-byte
  package, SHA-256 line, `package-summary-v1.json`, 13 extracted payloads (six job, six lattice, one model)
  plus `payloads/index.json`, and a README with provenance and regeneration) and the env-gated
  `RegeneratePackageFixture` test; added
  `PackageFixtureBuilder` test support and five `PackageFixtureTests` covering verification, the canonical
  summary, the package hash, the payload index, and rebuild reproducibility; added the one-shot
  `SimulationPackageBudget` runner and the `SimulationPackageBuildBenchmarks` BenchmarkDotNet case with the
  `--simulation-package-budget` entry point; wrote `docs/reference/msl-format.md`, `docs/user-guide/building.md`,
  and the `file-layout.md`/`architecture.md`/`testing-and-parity.md`/reference-index/nav updates; updated the
  parity matrix. The follow-up commit pins `NewLine = "\n"` in the canonical-JSON writers, the package-summary
  builder, the benchmark output, and the affected fixture tests so the Windows and Ubuntu goldens agree.
- **Part B evidence:** `tests/reference/bench/step-11-package-budget.json` records both runs on the real ceria
  sample (10 collections × 1,000 jobs at 10×10×10, level 3). The net11 run (SDK 11.0.100, runtime 11.0.0, at
  `8745a64`) took **3.874 s** wall clock, inserted **5,853 jobs/s**, produced a **10,051,584-byte** package with
  a **1,415-byte** model payload at **118,263,808-byte** peak RSS; the mandatory net10 reference (SDK 10.0.403,
  runtime 10.0.14, at `e2b00ea`) took **4.028 s**, **5,230 jobs/s**, the same **10,051,584-byte** package, and
  **119,119,872-byte** peak RSS. The comparison is −3.82 % wall clock, 0 % package size, +11.92 % insertion
  rate, and −0.72 % peak RSS, with `verdict: pass` against the 600 s / +5 % / 2 GiB / +3 % / 100 jobs/s /
  50 MiB budgets. The package SHA-256 differs only because `schema_info.app_version` carries each build's
  informational version; the payload bytes are codec-identical and the size is equal.
- **Parity:** `.msl` v1 format and fixtures is now `Done`; create/overwrite package stays `Partial` (the Step 12
  CLI remains) and package inspection/verification stays `Improvement` (the Step 12 `inspect`/`verify` commands
  remain). No other row is completed by feature 7.
- **Known limitations / follow-ups:** the checked-in fixture's `app_version`/`generator` are test-host
  informational values and are excluded from the canonical summary; re-running the env-gated regeneration is
  the intended update path if a runtime patch changes the package bytes. The Part B measurement is one local
  run; the Windows focused platform gate is still required before the audit because feature 3 added native
  directory fsync. Step 13's cross-language reader acceptance is a Step 13 test and has not run yet. The two
  unchecked acceptance criteria (an explicit in-test parameterization package, and Step 13/18 consumption of
  the variable-length `tracker_order`) are not feature-7 work; the writer-side tracker assertion is
  implemented.
- **Closeout readiness:** `origin/main` (`286e10c`) is an ancestor of the feature-7 tip; the step branch was 35
  commits ahead at that tip (`0ae05a4`). Remaining before the audit request: merge the owner-approved
  `fix/ci-platform-gate` CI fix into `main` (the branch is active in a separate worktree, so its tip may move;
  the merge commit is the reference), merge the updated `main` into this step branch and
  re-run the full gate, fill `docs/planning/closeout/step-11.json`, dispatch the single Ubuntu closeout run,
  dispatch the focused Windows platform gate (`platforms=windows`), then create
  `docs/planning/audits/step-11.md` and request the audit. No closeout JSON or audit record has been created
  from placeholder values.

### Amendment 1 — .NET 11 toolchain and native Zstandard

Feature 7 waited until [`step-11-amend-1`](../amendments/step-11-amend-1-dotnet-11-native-zstd.md) was reviewed
and integrated. The amendment moves the repository from `net10.0` to `net11.0` and replaces the
`ZstdSharp.Port` payload codec with the in-box `System.IO.Compression` Zstandard API.

Design-review state: round 1 returned `Changes required` at `06774ee`; the re-review returned `Ready with notes`
at `17e723c`, accepted by the human owner on 2026-11-15. The amendment is **integrated** as `00753e7` after a
feature branch that captured the pre-amendment `.msl` compatibility fixture and the golden codec frames with the
still-net10 writer (`e2b00ea`), retargeted the repository (`b0f189e`), and replaced the codec (`771c28b`). Part A
of the benchmark gate is **complete** (2026-11-15, amendment §9.1), and re-confirmed on the amended tree after
the swap, under the owner-approved small-payload rule (absolute decode penalty ≤ 0.15 µs below 8 KiB,
≥ 100 MB/s throughput, ≤ +10 % relative at or above 8 KiB).

Design consequences recorded here so the parent design stays coherent:

- §3 and §6 item 14: the payload codec is the in-box `ZstandardEncoder`/`ZstandardDecoder` with a single
  one-shot encoding path. The third-party `ZstdSharp.Port` reference edge disappears; the
  `Microsoft.Data.Sqlite` edge and its MIT licence result remain. No `.msl` format, payload version, canonical
  signature, or solver contract changes.
- §2: `PackageBuildOptions.CompressionLevel` keeps its name, default, and 1..19 range and maps by identity to the
  native Zstd `Quality`.
- §7: performance targets are measured on .NET 11 Release builds.
- Feature 7 generates the checked-in `.msl` fixture, the `package-summary-v1` golden, the extracted payloads, and
  the 10⁴-job benchmark **once**, on the amended toolchain. The one deliberate exception is the amendment's
  pre-amendment compatibility fixture and golden frames, captured with the net10 writer **before** the retarget
  so the net11 reader and the quality-mapping test have a real pre-switch reference.
- The amendment's benchmark gate has two parts: Part A (real-payload codec comparison) is complete and recorded
  in the amendment page; Part B (10⁴ jobs) runs before the audit freeze against a mandatory net10 reference, with
  a documented absolute fallback and a recorded rollback rule that no longer depends on byte identity.
- Byte identity is an observation, not a correctness or rollback threshold: signatures and `package-summary-v1`
  are logical projections, and only byte-capturing artifacts are re-captured if a runtime patch changes frames.
- The lattice decode-bound gap found in the review (the read-only reader passes `int.MaxValue`) is **not** fixed
  by this amendment; it is a Step 11 fix feature (`fix-lattice-decode-guard`) before the audit freeze, with a
  lattice bomb test. Until it lands, no page claims the lattice path is bounded by `PackageVerificationOptions`.

## Goal

Write the complete, reproducible input for the Rust solver: one immutable file containing the shared
parameterized model, per-job initial lattices, and settings. Results are written to a separate `.msr` store by
Step 20. The writer must be streaming enough for thousands of jobs and safe against partial or corrupt output,
and a read-only verifier must be able to prove that a published package is internally consistent.

## Scope

**In scope**

- The `.msl` v1 SQLite schema, pragmas, publication barrier, and the `models`/`lattices`/`jobs` tables.
- The Contracts model, job, and lattice payload DTOs and their Zstd+MessagePack encoding.
- The mapping from the real Steps 08–10 types (`CompiledModel`, `ParameterizedModel`, `ParameterizationValues`,
  `JobDescriptor`, `GeneratedLattice`, `DopingReport`, `CompiledOptimizers`, `SolverJobFlagsMapper`).
- The additive `PairInteractionPreview.EquivalentImages` field and the `PairCompilationStage` change that emits
  one `CompiledPairLink` per periodic image, plus the regenerated model/parameterization signature fixtures and
  the ApiCompat baseline refresh.
- The finalization of the lattice signature projection under ADR 0010's pre-release exception, and the
  parameterization-signature re-encode that proves the stored model payload is self-consistent.
- Content addressing and deduplication of models and lattices, streaming job insertion, and bounded memory.
- The package contract manifest entries, generated schema, fixture descriptors, and checked-in `.msl` fixtures.
- A read-only inspection/verification library over a published package, including `PKG` diagnostics.
- Reference and developer documentation named below.

**Non-goals**

- No CLI command. `mocassin build`, `inspect`, `verify`, `--jobs`, `--parallel`, `--force`, and progress
  events belong to Step 12, which consumes this step's writer and verifier (`AGENTS.md` §5; Step 12 design).
- No results storage, no `.msr` schema, no `.mcs` decoding, and no evaluation queries (Steps 18–20).
- No Rust reader (Step 13). This step only produces the contract and a fixture the reader consumes.
- No project JSON, parameterization-template, job-template, or supercell changes. Steps 06/09/10 own those.
- No `IValidationStage`. Step 12 owns the Build-stage adapter; this step returns a build report and warnings.
- No append-to-existing-package path, no external-results mode, and no `.bak` backup file (all deferred, ADR
  0011 / `05-open-decisions.md`).
- No compression of the `.mcs` state format and no custom binary header anywhere: `.msl` payloads are standard
  MessagePack maps inside standard Zstd frames (ADR 0004/0010).

## Dependency and interface reconciliation

Steps 08, 09, and 10 are `Done` and merged (`945dddf` is the Step 10 merge). The following cross-step
interfaces were checked against the shipped code at the design commit and are the inputs this step may rely on:

| Step | Shipped interface | Step 11 use | Result |
| --- | --- | --- | --- |
| 08 | `CompiledModel` header (`ModelSignature`, `SymmetryContractVersion`, `SpaceGroupDataVersion`, `SpaceGroupDataSha256`) and the particle/structure/energy/transition/simulation tables | Model payload mapping | consistent |
| 08 | `CompiledStructureModel.CellVectors`, `InteractionRange`, `Positions[].AllowedParticleMask`, `Environments`, `CompiledTransitionModel.JumpCounts/JumpOffsets/JumpMappings/GlobalTrackers/StaticTrackers` | Structure/transition payload; row-major jump arrays; tracker definitions; Cartesian field direction | consistent |
| 02/08 | `PairShell.EquivalentImages` is the full runtime image list, but `CompiledPairLink` is emitted once per symmetry shell and `PairInteractionPreview` has no image list | One payload pair entry per periodic image | gap found and closed: the compiler emits one `CompiledPairLink` per image using the additive `PairInteractionPreview.EquivalentImages`; the existing four-field model-signature link projection then signs every image, so its values change and the Step 08/09 golden vectors and sample `modelSignature` values are regenerated |
| 08 | `CompiledJumpRule.FrequencyHz` is the compiled default (`1e13 Hz`); `ParameterizationValues` is the only applied-value source | Absolute `jump_rules[].frequency_hz` comes from Step 09, never from the compiled default or a factor × maximum | consistent |
| 08 | `CompiledIndexMaps` and stable ID strings are inspection-only | Not persisted except the `jump_directions[].object_id` that has a compiled source | consistent |
| 09 | `ParameterizedModel(CompiledModel, ParameterizationValues, Signature)` with `ModelSignature` | `models.signature` = `Signature`; payload `signature`/`model_signature`; applied tables | consistent |
| 09 | `ParameterizationSignatureV1.Compute(modelSignature, values)` and its six-field projection | Writer self-check and verifier re-encode | consistent |
| 09 | Implicit default parameterization for `parameterizations: []`; `Apply` returns dense values | Packages built from samples with no template are valid | consistent |
| 09 | `PRM` validation is called by Step 12 after compile; no Step 09 validation stage | Step 11 does not validate templates; it encodes the applied result | consistent |
| 10 | `JobDescriptor` (1-based `JobId`, collection/config/job indices and names, `SimulationIndex`, `Kind`, `UseDualDofCorrection`, `Settings`, `Seed`, `CompiledOptimizers`) | Job row, payload, flags | consistent |
| 10 | `ResolvedJobSettings` with nullable limits and authored `ExecutionFlags` | Job payload settings | consistent |
| 10 | `GeneratedLattice(SizeA/B/C, PositionCount, Particles, Report, DopingInfo)` | `InitialLattice` payload, `doping_info` in the job payload and column | consistent |
| 10 | `SolverJobFlagsMapper.Compose` in `Mocassin.Packages.Jobs` | Composed `flags` word | consistent |
| 10 | Row-major layout `((a*B + b)*C + c)*PositionCount + P` | Lattice payload and signature | consistent |
| 10 | `SupercellGenerator` is the only lattice generator | The caller (Step 12 or the acceptance harness) pairs descriptor+lattice; the writer never generates | clarified in this design |
| 14 | Step 14 §4 builds runtime links from the payload's per-image environment pairs: `target_position`, the relative cell `offset`, and `pair_id`; it defines the exact target-expansion rule | Per-image pair-link shape | corrected: one entry per image (M-1) |
| 14 | Step 14 §5 uses `jump_count`/`jump_mapping` and the selection masks to build the pool; §6 uses the rule state codes and `tracker_order` | Jump tables and variable-length `tracker_order` | corrected (M-2) |
| 15 | Step 15 §4 consumes `(target, pair_id)` links and `charge_transport_vector·ê` | No extra payload field | consistent |
| 18 | Step 18 consumes `tracker_order` as per-node global tracker indices with the `0xFF` sentinel | Variable-length, length = node count | corrected (M-2) |
| 02 | `SymmetryVersionInfo` (`symmetry-1`, catalog version, catalog SHA-256) | `schema_info` and model payload provenance | consistent |

Cross-step statements corrected by this design (no behaviour change, only stale text):

- Step 06 said the solver normalizes the fractional field direction; the data contract already stores the
  normalized Cartesian vector in the job payload, which is what Step 15 consumes. Step 11 performs the
  conversion (see §3) and Step 06 now points at the package contract.
- Step 06 said a positive `MaxAttemptFrequencyHz` "rescales all frequencies"; since Step 09 the model stores
  absolute rule frequencies and Step 16 derives a per-job factor. Step 06 now matches Steps 09/16.
- Step 13's Rust `ParameterizedModel` lacked the symmetry provenance fields and used `particles` for the payload
  key `particle`. Step 13 is corrected to the contract.
- Step 12 and this page both claimed the `inspect`/`verify` commands. The commands stay in Step 12; this step
  owns the library they call.

## Original reference

- `.msl` is an EF Core SQLite database with separate tables for job packages, structure/energy/transition/
  lattice models, environments, pair/cluster tables, jump collections/directions, job metadata, and results.
- Large arrays are raw native structs (`Array_t`) in blobs; jobs reference shared model rows by integer IDs.
- The builder creates the DB, adds rows, saves changes, and embeds the project XML and build template XML.
- Hard limit: 99,999 jobs per package.
- Results are stored as MessagePack `.mcs` blobs in `JobResultData`.
- Journal mode is WAL during build, then switched to DELETE; `vacuum` is run on save.

The new format keeps SQLite (a single-file, transactional artifact) but uses a small, explicit, versioned
schema, standard MessagePack payloads, and Zstd frames, with no native structs and no results table
(ADR 0004, 0010, 0011).

## Design

### 1. Schema, pragmas, and version metadata

The schema is the one in [`02-data-contracts.md`](../02-data-contracts.md) §3.2, with these Step 11
finalizations (recorded as contract deltas in §6):

- `application_id = 0x4D4F4353` (`"MOCS"`), `user_version = 1`, `schema_info.format_version = '1'`.
- `schema_info` rows: `format_version`, `app_version`, `created_utc`, `project_id`, `generator`,
  `job_selection`, `symmetry_contract_version`, `space_group_data_version`, `space_group_data_sha256`.
  `app_version` is the entry assembly's informational version and `generator` is `mocassin .NET <app_version>`;
  `created_utc` is `PackageBuildRequest.CreatedUtc`, never read from the clock.
- The `jobs` table keeps only build-time or payload-derived values. `norm_factor` and `base_frequency_hz` are
  removed because no build-time value exists for them and Step 16 owns every derived factor.
- No `results` table, no `job_metadata` view, no legacy table.

Build-time pragmas:

- `journal_mode=WAL`, `synchronous=NORMAL`, `foreign_keys=ON`, `page_size=8192` (before the first table),
  `temp_store=MEMORY`, `application_id`, `user_version`.
- One transaction per `BatchSize` jobs (default 500), not one per job.
- On completion: `PRAGMA wal_checkpoint(TRUNCATE)`, `PRAGMA journal_mode=DELETE`, `PRAGMA optimize`, close.
- No `VACUUM` by default; `PackageBuildOptions.Vacuum` (Step 12's `--compact`) runs it before publication.

One model row per package in v1. The schema keeps `jobs.model_id` as a foreign key so a later version can
store several parameterizations of one compiled model, but `WriteModelAsync` is called exactly once; a second
call with a different signature is a contract error. This matches the build (one selected parameterization per
package) and removes the ambiguity of which model a job references.

### 2. Writer API and publication pipeline

```csharp
public sealed record JobPackage(JobDescriptor Descriptor, GeneratedLattice Lattice);

public sealed record PackageBuildRequest(
    string TargetPath,            // final .msl path; temp file is written beside it
    Guid ProjectId,
    string JobSelection,          // "all" or "1-100,250"; validated non-empty
    DateTimeOffset CreatedUtc);   // supplied, never read from the clock

public sealed record PackageBuildOptions
{
    public bool Overwrite { get; init; }
    public int CompressionLevel { get; init; } = 3;   // Zstd quality, 1..19 (amendment 1)
    public bool Vacuum { get; init; }
    public int BatchSize { get; init; } = 500;
    public long AdvisorySizeBytes { get; init; } = 2L * 1024 * 1024 * 1024;
}

public sealed record PackageWriteResult(
    string Path,
    string Sha256,                // SHA-256 of the exact published bytes
    int JobCount,
    int ModelCount,
    int LatticeCount,
    long SizeBytes,
    ImmutableArray<Diagnostic> Warnings);   // PKG009 and future advisory build warnings

public interface ISimulationPackageWriter : IAsyncDisposable
{
    Task WriteModelAsync(ParameterizedModel model, CancellationToken cancellationToken = default);
    Task WriteJobAsync(JobPackage job, CancellationToken cancellationToken = default);
    Task<PackageWriteResult> CompleteAsync(CancellationToken cancellationToken = default);
}
```

`SimulationPackageWriter.Create(PackageBuildRequest request, PackageBuildOptions options)` is the factory
(`Mocassin.Packages` has no DI container yet; the module marker stays a marker). Every call performs bounded
synchronous SQLite/encode work on the caller's task; there is no fake `Task.Run`.

Implementation clarification: `PackageBuildRequest` carries the final `TargetPath`; the design's pipeline and
`PackageWriteResult.Path` require it, and the frozen snippet omitted it. The temporary build file is
`<target>.tmp-<pid>-<random>`, which is `<name>.msl.tmp-*` for a `.msl` target and matches the `.gitignore`
pattern.

Pipeline:

1. Validate the target path; if it exists and `Overwrite` is `false`, fail before creating anything.
2. Create `<target>.msl.tmp-<pid>-<random>` in the target directory (matching the `.gitignore` pattern) and open
   SQLite on it.
3. Set pragmas, create the schema, and write `schema_info` once the model is known (the symmetry provenance
   comes from the compiled model).
4. Compile/parameterize the model once (Steps 08/09 by the caller); `WriteModelAsync` maps it, encodes the
   payload, re-encodes the parameterization projection from the payload it just built, compares it with
   `ParameterizedModel.Signature`, and inserts the row. A mismatch is a contract error, not a diagnostic.
5. Stream `JobPackage` items from the caller:
   - map the lattice, encode it, and insert/reuse the row by signature;
   - map the job, derive the `SolverJobFlags` word, encode it, and insert the row with the denormalized
     columns (all job rows carry the request's `created_utc`);
   - commit every `BatchSize` jobs and honor cancellation between batches.
6. `CompleteAsync`: checkpoint, switch to DELETE, optionally `VACUUM`, close, hash the temp file, `fsync` it and
   the containing directory, then atomically rename over the target.
7. `DisposeAsync` without `CompleteAsync` deletes the temp file. Any exception deletes the temp file and leaves
   an existing package untouched. Orphaned `.msl.tmp-*` files from a crashed process are inert; `mocassin
   clean` (Step 28) may remove them.

The writer never holds all jobs or lattices in memory. `WriteJobAsync` requires a model first and
`CompleteAsync` requires exactly one model and at least one job; both are contract guards
(`InvalidOperationException`), because Step 12 has already reported user-data errors.

### 3. Payload encoding

All payloads are MessagePack maps with snake_case keys, encoded with `MessagePack-CSharp` using explicit
`[Key("...")]` names (no contractless resolver) and `MessagePackSecurity.UntrustedData`, then compressed with a
standard Zstd frame (in-box `System.IO.Compression.ZstandardEncoder`/`ZstandardDecoder` under .NET 11, one
one-shot encoding path, quality = `PackageBuildOptions.CompressionLevel`; see
[amendment 1](../amendments/step-11-amend-1-dotnet-11-native-zstd.md); no dictionaries, no custom framing).
Readers ignore unknown keys within the current major and reject an unsupported `format_version`.

- **Model payload** — the §3.3 map. `format_version = 1` is the payload-encoding version, independent of
  `CompiledModel.FormatVersion`. The mapping is mechanical and one-way:
  - `signature` = `ParameterizedModel.Signature`; `model_signature` = `CompiledModel.ModelSignature`; the three
    symmetry fields come from the `CompiledModel` header.
  - `particle.charges/symbols/is_vacancy/count` come from `CompiledModel.Particles` in dense index order, void
    at index 0.
  - `structure.cell_vectors_ang` is `[a, b, c]` in ångström, so `fractional → Cartesian` is
    `fa·a + fb·b + fc·c`. `positions` are ascending `P` with `vector4 = [cellA, cellB, cellC, P]`,
    `site_index`, `stable`, and `particle_ids` = the ascending compiled indices set in
    `AllowedParticleMask`. `environments` carry `selection_particle_mask`, `pairs`, `clusters`, and
    `update_particle_ids`:
    - a pair entry is `{target_position, offset, energy_id}` and there is one entry per periodic image, mapped
      one-to-one from a `CompiledPairLink`: `target_position = TargetP`,
      `offset = [Offset.A, Offset.B, Offset.C]`, `energy_id = PairTableIndex`. `PairCompilationStage` emits one
      compiled link per `PairShell.EquivalentImages` element (representative included), so the existing
      four-field link projection signs every runtime image and the writer needs no extra expansion. Offsets are
      unique within an environment; `distance_bucket_fm` is not persisted.
    - a cluster entry is `{cluster_id, byte_position}` from `CompiledClusterLink`
      (`byte_position = SiteSlot`).
    - `static_trackers_per_cell = position_count × (particle_count − 1)`;
      `global_tracker_count = CompiledTransitionModel.GlobalTrackers.Length`.
  - `energy.pair_tables[i].energies` = `ParameterizationValues.PairTables[i]`;
    `energy.cluster_tables[i].energies` = `ParameterizationValues.GroupSets[i]`, in compiled occupation order;
    `energy.defect_background` = `ParameterizationValues.SiteEnergies` (dense `[position][particle]`, void at
    column 0). The compiled defaults are never encoded.
  - `transitions.jump_collections` = `{kind, transition_id, mobile_particle_mask, rule_ids, direction_ids}`;
    `jump_directions` = `{object_id, position_id, collection_id, path, movement, path_vector, length,
    is_inverse}`; `jump_rules` = `{rule_id, collection_id, state0, state1, state2,
    frequency_hz = ParameterizationValues.TransitionRuleFrequencies[i], charge_transport_vector,
    static_correction = nil, tracker_order}`. `tracker_order` is a variable-length `u8[]` copied from
    `CompiledJumpRule.TrackerOrder`: one entry per path node, each a global tracker index or `0xFF`. The writer
    asserts that its length equals the representative direction's `path` length (the state-code node count) and
    that every non-sentinel value is below `global_tracker_count`.
  - `jump_count` is `CompiledTransitionModel.JumpCounts` in `[position][particle]` order; `jump_mapping` is the
    flat concatenation of `JumpMappings` in `[position][particle][relative]` order, so the reader derives the
    offsets from the prefix sums of `jump_count`. A test pins `JumpOffsets` against those prefix sums.
  - `static_tracker_mapping` is a dense `[position][particle] → tracker offset` array with `-1` for "none";
    `global_tracker_mapping` is a dense `[collection][particle] → tracker index` array with `-1` for "none". The
    tracker models in `simulations` reference the same indices.
  - `simulations[].default_settings` carries the compiled runtime defaults; `flags` contains only the authored
    bits 0–3 (derived intent is per job), and `field_direction` is the normalized Cartesian vector so the model
    and job payloads use one representation. `tracker_models` are definitions only; Step 18 owns values.
- **Lattice payload** — `{format_version, lattice_size, position_count, lattice}`. `lattice` is the
  `GeneratedLattice.Particles` byte array (row-major layout of §3.4). No doping summary is stored in the
  lattice: the identical particle array must dedupe regardless of which job's doping request produced it, and
  the job payload already carries the summary.
- **Job payload** — the §3.4 map. `simulation_index`, `kind`, all resolved settings, `flags` from
  `SolverJobFlagsMapper.Compose(settings.Flags, descriptor.Kind, settings.PreRunMcsp,
  descriptor.UseDualDofCorrection)`, `selection_optimizers` from `descriptor.CompiledOptimizers`,
  `doping_info` from `GeneratedLattice.DopingInfo`, and `extensions` as `{id, config_json}`.
- `config_json` is the UTF-8 canonical JSON text of `JobExtension.Config`, produced by the same canonicalizer
  the project hash uses (exposed as a small public Core helper in this step). It is stored as a MessagePack
  `str`, not a map: extension config numbers are opaque and must keep the authored token, which a
  `int64`/`float64` map cannot represent for integers above 2^63. Extensions are sorted by `id`.
- The writer verifies round-trip decoding of every payload it encodes before insert; on mismatch it fails and
  deletes the temp file.

### 4. Content addressing, dedup, and determinism

- `models.signature` is the Step 09 parameterization signature and the uniqueness key. Insert uses
  `INSERT … ON CONFLICT(signature) DO NOTHING` plus a lookup of the existing `model_id`.
- `lattices.signature` is the finalized `LatticeSignatureV1` (ADR 0010), a domain-separated SHA-256 over the
  canonical MessagePack projection `[format_version(uint64), model_signature(string), lattice_size(uint64×3),
  position_count(uint64), particles(bin)]`. It binds the compiled-model identity because lattice entries are
  compiled particle indices, and it excludes build metadata and the doping summary.
- Dedup uses an in-memory `signature → id` map plus the unique index; exact matches insert once. Two jobs with
  the same particles share one row; a doping request that yields different particles produces a different row.
- Determinism: the same project, selected parameterization, and job set produce identical model and lattice
  signatures and an identical **canonical package summary** (`package-summary-v1`, data contracts §3.5). Stored
  bytes may differ only in build metadata (`schema_info.created_utc`, `app_version`, `generator`), file size,
  payload byte hashes, and SQLite/Zstd implementation details; no golden test compares raw file bytes.
- The canonical summary is the inspector projection defined in the data contract: it includes the normalized
  `schema` block, one entry per model (signatures, payload format version, table/rule counts), the distinct
  lattices sorted by signature, and one entry per job (IDs, names, indices, settings, lattice size,
  `doping_info`, `flags`, `seedHex`). It excludes `created_utc`, `app_version`, `generator`, size, and payload
  byte hashes. Checked-in fixtures store it beside the `.msl`, and regeneration uses the same
  `MOCASSIN_UPDATE_GOLDENS=1` test path as the other contract fixtures.

### 5. Verification and inspection

`Mocassin.Results.Packages` gains the read-only `.msl` structural reader used by Step 12's `mocassin
inspect`/`mocassin verify` and later by Step 20. `Mocassin.Packages` must not own general `.msl` reading
(`01-target-architecture.md` §3.1).

```csharp
public sealed record PackageModelInfo(
    string Signature, string ModelSignature, int PayloadFormatVersion, long PayloadBytes);

public sealed record PackageCollectionInfo(
    string Kind, string CollectionName, string ConfigName, int JobCount);

public sealed record PackageInspection(
    int FormatVersion, string AppVersion, DateTimeOffset CreatedUtc, Guid ProjectId,
    string Generator, string JobSelection,
    string SymmetryContractVersion, string SpaceGroupDataVersion, string SpaceGroupDataSha256,
    ImmutableArray<PackageModelInfo> Models, int LatticeCount, int JobCount,
    ImmutableArray<PackageCollectionInfo> Collections,
    double MinTemperatureK, double MaxTemperatureK,
    double MinElectricFieldVm, double MaxElectricFieldVm,
    long SizeBytes, double LatticeReuseFactor);   // jobCount / latticeCount

public sealed record PackageVerificationCheck(string Code, bool Passed, string Message);

public sealed record PackageVerificationReport(
    bool IsValid,
    ImmutableArray<PackageVerificationCheck> Checks,
    ImmutableArray<Diagnostic> Diagnostics,      // PKG codes, severity from the shared descriptors
    PackageInspection Inspection);

public sealed record PackageVerificationOptions(
    long MaxModelPayloadBytes = 512L * 1024 * 1024,
    long MaxJobPayloadBytes = 8L * 1024 * 1024,
    long AdvisorySizeBytes = 2L * 1024 * 1024 * 1024);

public interface IMslPackageInspector
{
    Task<PackageInspection> InspectAsync(string path, CancellationToken cancellationToken = default);
    Task<JsonDocument> BuildCanonicalSummaryAsync(string path, CancellationToken cancellationToken = default);
}

public interface IPackageVerifier
{
    Task<PackageVerificationReport> VerifyAsync(
        string path, PackageVerificationOptions options, CancellationToken cancellationToken = default);
}
```

`BuildCanonicalSummaryAsync` returns the `package-summary-v1` JSON of data contracts §3.5 (the normalized
inspector projection), so the fixture and determinism tests never compare raw file bytes. The inspector and
verifier are separate interfaces because `inspect` must work on a package that fails some verification checks
(it reports what it can read and records errors), while `verify` runs the complete ordered check list.

Checks, in order (each produces one `PackageVerificationCheck` and zero or one `Diagnostic`):

1. SQLite opens read-only (`Mode=ReadOnly`, `Cache=Private`, `query_only=ON`) and `PRAGMA integrity_check`
   returns `ok`; required tables, indices, and columns exist; no `results` table and no `-wal`/`-shm` sidecar.
2. `application_id`, `user_version`, and `schema_info` agree with each other, with `format_version = 1`, and
   with the manifest constants; `project_id` is a GUID, `created_utc` parses, and the symmetry provenance rows
   are present.
3. Every job references an existing model and lattice; `job_id` is unique, positive, and ascending; the
   `(collection_index, config_index, job_index)` triple is unique; names are non-empty.
4. Every payload decodes within the explicit decompressed-size bound: model and job from
   `PackageVerificationOptions`. A lattice is bounded by `4 × a × b × c × position_count` from its referencing
   `jobs` row (`lattice_a/b/c`) and the referenced model payload's `position_count`, and is always capped by
   `PackageVerificationOptions.MaxLatticePayloadBytes`; an orphan lattice row (no referencing job), or one whose
   model payload did not decode, uses that cap as its fallback. Every payload's `format_version` matches the
   column.
5. Model payload invariants: pair-table lengths are `particle_count²`; cluster occupation/energy/mapping lengths
   agree; `defect_background.Length == position_count × particle_count`; environment pair entries have
   `target_position < position_count`; cluster `byte_position < site_count`; `tracker_order` is a non-empty
   array whose non-sentinel values are below `global_tracker_count` and whose length is within the documented
   path-node bound (the payload carries no independent node count or representative marker, so the writer's
   `length == representative path length` assertion is not re-provable by the verifier); `jump_count`/
   `jump_mapping`/tracker-mapping lengths match the declared dimensions; pair entries within one environment
   are unique by `(target_position, offset)` (the shell representative is not identifiable from the payload,
   because `distance_bucket_fm` and any symmetry reference are not persisted); every particle index is below
   `particle_count`; no NaN/Inf in required numeric fields.
6. The model's parameterization projection re-encodes to `models.signature` and the payload's `signature` from
   the stored absolute `frequency_hz` values — never from a factor × maximum. `model_signature` is a 64-hex
   identity string and is not re-encoded from the payload (the compiled projection signs inspection-only stable
   IDs).
7. The lattice projection re-encodes to `lattices.signature` using `model_signature` from the referenced model
   row; `lattice.Length == a·b·c·position_count`; every particle index is below the model particle count.
8. Payload-derived `jobs` columns equal their payload values: `model_id`/`lattice_id` by signature, `kind`,
   `temperature_k`, `electric_field_vm`, `target_mcsp`, `write_out_count`, `pre_run_mcsp`, `time_limit_s`,
   `min_success_rate`, `lattice_a/b/c`, `doping_info`, and `flags`. Authored/build metadata
   (`collection_name`, `config_name`, the three indices, `seed`, `created_utc`) has no payload counterpart and
   is checked for presence/format/uniqueness instead.
9. `flags` has exactly one kind bit; `usePrerun` is set if and only if the job is KMC and `pre_run_mcsp > 0`;
   `useDualDofCorrection` is clear for MMC; an MMC job has `pre_run_mcsp = 0`.
10. The package size is compared with `AdvisorySizeBytes` (warning only).

`PKG` codes (Contracts-owned descriptors; anchors in `docs/reference/diagnostics.md`):

| Code | Severity | Check |
| --- | --- | --- |
| `PKG001` | error | Not a readable SQLite database or `PRAGMA integrity_check` failed |
| `PKG002` | error | `application_id`, `user_version`, or `schema_info.format_version` is missing or unsupported |
| `PKG003` | error | Required table, index, column, or `schema_info` key is missing or malformed |
| `PKG004` | error | Payload is not a valid Zstd frame/MessagePack map, or exceeds the decompressed-size bound |
| `PKG005` | error | Parameterization or lattice signature does not match its re-encoded projection |
| `PKG006` | error | Job/model/lattice reference, job ID, or unique triple is invalid |
| `PKG007` | error | A payload-derived job column disagrees with its payload value |
| `PKG008` | error | The package is not immutable: a `results` table or a `-wal`/`-shm` sidecar is present |
| `PKG009` | warning | Package size exceeds the advisory limit |
| `PKG010` | error | A payload value is out of bounds (table lengths, particle/position indices, tracker order, mappings) |

Bounds are enforced before allocation; a malformed or hostile package produces a report with `PKG` errors, never
an unhandled exception or an unbounded allocation. Verification never mutates the package. `IsValid` is false
only when an error-severity diagnostic is present; the `PKG009` advisory size warning records its check but does
not fail verification.

Diagnostic ownership: the `PKG` descriptors live in `Mocassin.Contracts.Packages`
(`PackageDiagnosticDescriptors.All`). `Mocassin.Packages` concatenates them into `DiagnosticCatalog.Default` so
the existing `docs/reference/diagnostics.md` drift test and build-side lookup see them;
`Mocassin.Results` builds its diagnostics directly from the same Contracts descriptors and does **not**
reference `Mocassin.Packages` or its catalog (`Mocassin.Results -> Core + Contracts` stays intact). Step 12 maps
`verify` failures to exit code `1`/`3` and `inspect` to human/JSON output.

### 6. Contract deltas and pre-release finalization

`.msl` has never been released, so these are pre-release finalizations, not migrations. ADR 0010's pre-release
exception covers the signature layout, and ADR 0009 gains a matching "pre-release package-shape finalization"
section: while no released `.msl` exists, the package table/payload shape may be finalized at
`formatVersion = 1`, with the version-bump rule resuming at the first released package. Each delta is applied in
the same feature as the code that needs it, with regenerated artifacts and fixtures:

1. `signatures.lattice` is finalized from the Step 00 skeleton to the five fields of §4: `dopingInfo` is
   removed (doping provenance lives on the job) and `modelSignature` is kept (particle indices are
   model-specific). `status` becomes `final`; the generated descriptor and the golden vectors are added.
2. `formats.package.payloads` is added to the manifest with the **top-level** model, job, and lattice payload
   keys, types, required flags, and payload `formatVersion` values. This closes the ADR 0009 gap at the same
   granularity the `.mcs` entry uses (nested maps stay typed as `object`/`array`). `generate.py` emits a
   **separate** `contracts/generated/schema/package-payloads-v1.schema.json`; the existing
   `package-v1.schema.json` stays the SQLite row schema and is not mixed with payload shapes. Payload schemas
   use `additionalProperties: true` (readers ignore unknown keys within a major), unlike the row schema.
   Fixture descriptors and the generated constants are regenerated.
3. The undefined `jobs.norm_factor` and `jobs.base_frequency_hz` columns are removed from the manifest and the
   SQL contract.
4. `jump_collections` has no `object_id` field, and §3.3 does not add one: the dense index plus
   `transition_id` already identify a collection. `jump_directions.object_id` stays (its compiled source is
   `CompiledJumpDirection.ObjectId`).
5. The lattice payload drops `doping_info`; the job payload and `jobs.doping_info` keep the canonical summary.
6. `extensions` entries become `{id, config_json}` with the canonical JSON text (see §3.3).
7. `electric_field_direction` is the normalized Cartesian unit vector derived from the authored fractional
   direction; a zero modulus stores the normalized authored direction when non-zero and `(1,0,0)` when the
   authored vector is also zero.
8. `schema_info` gains the three symmetry provenance rows; the writer requires every model it writes to share
   them.
9. The job payload's `jump_mapping`/tracker-mapping shapes and the `particle_ids` ordering are stated
   explicitly (they were implicit).
10. Environment pair entries become one entry per periodic image `{target_position, offset:[i32;3],
    energy_id}`. The compiler currently emits one link per symmetry shell, so this step adds
    `PairInteractionPreview.EquivalentImages` (from `PairShell.EquivalentImages`) and emits one
    `CompiledPairLink` per image. The existing four-field link projection in the model signature then signs
    every runtime image; its **values** change for multi-image shells, so the Step 08 model projection
    vectors, the Step 09 parameterization vectors/`signature-vectors.json`, and the checked-in sample
    `modelSignature` values are regenerated in the same feature. The projection **schema** is unchanged, and
    `distance_bucket_fm` is not persisted (the table index is the energy identity). Step 14's link expansion
    and Step 15's `(target, pair_id)` deltas are the consumers and are tested against the payload. This is a
    cross-step public-API/behaviour change to Steps 02/08; the ApiCompat baseline for
    `Mocassin.Symmetry`/`Mocassin.Compilation` is refreshed.
11. `tracker_order` becomes a variable-length `u8[]` with one entry per path node (global tracker index or
    `0xFF`), matching `CompiledJumpRule.TrackerOrder` and the model signature; the fixed `[u8;8]` shape and the
    Step 18 "path node index" wording are corrected.
12. The `package-summary-v1` inspector projection is defined in the data contract and used for determinism,
    fixtures, and the benchmark evidence.
13. `PKG001`–`PKG010` are defined with severities and check mappings, and the `PKG` descriptors are Contracts-
    owned with `DiagnosticCatalog.Default` composing them in `Mocassin.Packages`.
14. Adding `ZstdSharp.Port` and `Microsoft.Data.Sqlite` references to `Mocassin.Packages`/`Mocassin.Results` is a
    new package-reference edge; the feature that adds it records the MIT licence result from
    `check_nuget_licenses.py` and the `policy` gate. **Superseded by amendment 1:** the payload codec is the
    in-box `System.IO.Compression` Zstandard API, so `ZstdSharp.Port` is removed and only the
    `Microsoft.Data.Sqlite` edge remains. The historical licence results in the feature log are retained.

Fixture consequences: regenerate `contracts/generated/**`, add `tests/contracts/projections/lattice/*.bin`
vectors and their metadata, extend `tests/contracts/signature-vectors.json`, add
`tools/contracts/check_lattice_projection.py` (or fold the check into the parameterization checker) and run it
in `verify.sh contracts`. No previous-major `.msl` fixture exists; the first released major starts the ADR 0013
current+previous policy, and this step records the current-only fixture layout so the policy can start at v2.

### 7. Bounds, performance, and failure behaviour

Targets (benchmark laptop class, .NET 11 Release):

- 10,000 small jobs (10×10×10 ceria, ~1,000 positions) in under 10 minutes and under 2 GB peak RSS.
- Model payload under 50 MB for a complex model.
- Job insertion throughput at least 100 jobs/s after compilation and lattice generation.
- Lattice insertion is dominated by generation; the writer reports the dedup ratio.

The writer warns (through `PackageWriteResult.Warnings`, surfaced by Step 12) when the package exceeds
`AdvisorySizeBytes`, and suggests fewer duplicate jobs, smaller supercells, external result storage later, or
`--compact` compression. There is no silent failure and no unbounded in-memory buffering: payloads are encoded
per model/job, latices one at a time, and `jump`/tracker arrays are written as flat arrays.

Failure behaviour: a cancelled or failed build deletes the temp file and leaves an existing package intact; a
disk-full/IO error surfaces as `IOException` and deletes the temp file; the published file is only ever
replaced by the atomic rename after the file and directory are synced.

**Directory fsync and platform consequence (R2-7).** The file is flushed with
`FileStream.Flush(flushToDisk: true)`. .NET has no managed directory-fsync API, so the containing-directory
flush uses a minimal Unix libc `open`/`fsync`/`close` interop (`DllImport` with
`UnmanagedType.LPUTF8Str`, `SetLastError`, and `CharSet.Ansi`). Windows has no equivalent directory-durability
primitive in this path; the Windows branch skips the directory call and relies on the file fsync plus the atomic
replace, which is the documented Windows guarantee. This is the solution's first platform-specific native code
and a `AGENTS.md` §3.13 trigger: the step closeout must dispatch the manual **Windows** platform job
(`platforms=windows` or `all`) to compile and exercise the Windows branch. macOS uses the same Unix libc path
exercised on Ubuntu, so a macOS run is optional/manual, not required. If the Windows run is unavailable at
closeout, it is recorded as an accepted risk in the audit.

### 8. Build integration and step boundaries

- Step 12 owns `mocassin build`: it loads, validates, compiles, parameterizes, expands jobs, generates lattices,
  pairs them into `JobPackage`, feeds the writer, then calls the verifier before reporting success.
- Step 11 does not add an `IValidationStage`. Package-size advice is build output, not user-model validation.
- Step 12 owns `mocassin inspect`/`verify` and maps `PKG` diagnostics to the CLI error model; Step 20 reuses
  the reader for evaluation.
- Step 13 consumes the checked-in `.msl` fixture and the §3.3/§3.4 payload contract; the cross-language read is
  demonstrated there, not here. Step 14's link expansion and Step 15's pair deltas are the acceptance test for
  the persisted pair `offset`; Step 18 is the acceptance test for the variable-length `tracker_order`.

### 9. Code layout and naming

The plan follows `docs/development/code-structure.md` and `AGENTS.md` §8: one domain folder per concept, the
namespace matches the folder, no forbidden folder names, at most 20 direct `.cs` files per folder, depth at most
4, and 800 lines per file.

| Project | Folder → namespace | Contents |
| --- | --- | --- |
| `Mocassin.Contracts` | `Packages/` → `Mocassin.Contracts.Packages` | `PackageFormatConstants.cs`, `ModelPayloadDtos.cs`, `StructurePayloadDtos.cs`, `EnergyPayloadDtos.cs`, `TransitionPayloadDtos.cs`, `SimulationPayloadDtos.cs`, `JobPayloadDtos.cs`, `LatticePayload.cs`, `LatticeSignatureV1.cs`, `PackageDiagnosticDescriptors.cs` |
| `Mocassin.Packages` | `SimulationPackage/` → `Mocassin.Packages.SimulationPackage` | `ISimulationPackageWriter.cs`, `SimulationPackageWriter.cs`, `SimulationPackageSchema.cs`, `PackageBuildOptions.cs`, `PackageBuildRequest.cs`, `PackageWriteResult.cs`, `JobPackage.cs` |
| `Mocassin.Packages` | `SimulationPackage/Payloads/` → `Mocassin.Packages.SimulationPackage.Payloads` | `PayloadEncoder.cs`, `ModelPayloadMapper.cs`, `StructurePayloadMapper.cs`, `EnergyPayloadMapper.cs`, `TransitionPayloadMapper.cs`, `SimulationPayloadMapper.cs`, `JobPayloadMapper.cs`, `LatticePayloadMapper.cs`, `FieldDirectionConverter.cs` |
| `Mocassin.Core` | existing `Model/Jobs/JobExtension.cs` | add `ToCanonicalJson()` using the existing internal canonicalizer (no new file) |
| `Mocassin.Results` | `Packages/` → `Mocassin.Results.Packages` | `IMslPackageInspector.cs`, `MslPackageInspector.cs`, `IPackageVerifier.cs`, `PackageVerifier.cs`, `PackageInspection.cs`, `PackageVerificationReport.cs`, `PackageVerificationCheck.cs`, `PackageVerificationOptions.cs`, `PackageSummaryBuilder.cs` |
| `Mocassin.Packages.Tests` | `SimulationPackage/` | writer, mapper, signature, atomicity, fixture tests |
| `Mocassin.Results.Tests` | `Packages/` | inspector, verifier, `PKG` mapping, corruption/bound tests |
| `Mocassin.Benchmarks` | `SimulationPackage/` | model-payload and 10⁴-job write benchmark |

The `Dtos` and `Options` file suffixes are the permitted multi-type suffixes; concept files stay inside their
domain folder. No `Utils/`, `Helpers/`, or `Common/` folder is introduced.

### 10. Sizing

Roadmap estimate: **3–4 person-weeks**. This design estimated **3.5–4.0 pw** before the review, which already
exceeded the roadmap row while staying at the four-week trigger. Correcting M-1 adds the additive
compiler/preview image-list field, its tests, and the ApiCompat baseline refresh (about 0.2–0.3 pw), so the
honest estimate is **3.8–4.2 pw**. The upper bound now exceeds the roadmap §3.1 four-week trigger. Seven features
remain within the "roughly 6–8" range; the compiler field is folded into feature 4 rather than added as an
eighth feature.

Two options, for the human owner's start-gate decision:

1. **Single-step exception (recommended).** Approve Step 11 as one step at 3.8–4.2 pw, as Step 09 did at
   3.4–4.4 pw, because the writer, its payload contract, its fixtures, and its verifier form one coherent
   contract and a split would add a roadmap step and change Step 12's dependency.
2. **Split.** 11A = schema, publication barrier, model payload with the per-image pair links, lattice signature,
   and the verifier; 11B = job/lattice encoding, fixtures, benchmark, and closeout. This adds a roadmap step and
   makes Step 11B the dependency of Step 12; it is not proposed because the two halves share the same schema and
   fixtures.

## Implementation tasks

- [x] Add the Contracts package DTOs, payload constants, `LatticeSignatureV1`, and `PKG` descriptors.
- [x] Finalize `signatures.lattice` and add `formats.package.payloads`; extend the generator and regenerate.
- [x] Implement schema creation, pragmas, `application_id`/`user_version`, and `schema_info`.
- [x] Implement the model payload mapper and the write-time parameterization re-encode.
- [x] Persist the environment pair-link `offset` and the variable-length `tracker_order`, with the writer
      contract assertions; the Step 14 consumer expansion is its own cross-step acceptance test.
- [x] Implement the lattice payload/signature and content-addressed dedup.
- [x] Implement the job payload mapper, `SolverJobFlags` composition, Cartesian field conversion, optimizer
      mapping, extension JSON, and payload-derived columns.
- [x] Implement batching, streaming, progress/cancellation, and bounded memory. (The writer streams and
      honors cancellation; Step 12 owns the user-facing progress surface.)
- [x] Implement the temp file, fsync of file and directory, atomic rename, and failure cleanup.
- [x] Implement the read-only inspector/verifier with the `package-summary-v1` projection, bounded decoding,
      and `PKG001`–`PKG010`.
- [x] Add the `PKG` descriptors to Contracts and compose them into `DiagnosticCatalog.Default`; confirm
      `Mocassin.Results` does not reference `Mocassin.Packages`.
- [x] Add checked-in `.msl` fixtures, canonical summaries, extracted payloads, and regeneration command: `tests/contracts/packages/msl-v1/` with `package-summary-v1.json`, `payloads/index.json`, and the env-gated `RegeneratePackageFixture` test; the net10 compatibility fixture and codec goldens stay.
- [x] Add the lattice projection golden vectors and `verify.sh contracts` checker.
- [x] Add the `Mocassin.Benchmarks/SimulationPackage` benchmark and record the 10⁴-job budget: the one-shot budget runner and a BenchmarkDotNet case, with the net10 reference run recorded in `tests/reference/bench/step-11-package-budget.json`.
- [x] Update `PublicAPI.Unshipped.txt` and the ApiCompat baseline for `Mocassin.Contracts`, `Mocassin.Core`
      (the new `JobExtension.ToCanonicalJson`), `Mocassin.Symmetry` (the new
      `PairInteractionPreview.EquivalentImages`), `Mocassin.Compilation`, `Mocassin.Packages`, and
      `Mocassin.Results`. Refreshed by features 1–6, amendment 1, and `fix-lattice-decode-guard`; the only
      later addition is `PackageVerificationOptions.MaxLatticePayloadBytes`.
- [x] Record the MIT licence result for the new `ZstdSharp.Port`/`Microsoft.Data.Sqlite` reference edges.
- [x] Write the documentation named below and update parity/status.
- [x] Review and integrate [`step-11-amend-1`](../amendments/step-11-amend-1-dotnet-11-native-zstd.md) (.NET 11
      toolchain and native Zstandard) before feature 7 generates the fixtures and before the audit freeze.
      Integrated as `00753e7` after the `Ready with notes` acceptance.
- [x] Capture the pre-amendment `.msl` compatibility fixture and the golden codec frames with the net10 writer
      before the retarget (amendment §9.4–§9.5), and commit them with the amendment feature (`e2b00ea`).
- [x] Close the lattice decode-bound gap found by review A1-10 in a Step 11 fix feature
      (`feature/step-11-simulation-package-and-msl-writer/fix-lattice-decode-guard`): derive the bound from the
      referencing `jobs` row (`lattice_a/b/c`) and the model payload's `position_count`, apply it in
      `PackageReader.ReadLatticesAsync`, bound an orphan lattice row (no referencing job) with an explicit
      `MaxLatticePayloadBytes` option, and add the lattice bomb test. Squashed as `25a3787`.

## Acceptance criteria

- [x] A package built from `samples/jobs/ceria-temperature-sweep.mocprj` (six jobs, `parameterizations: []`)
      uses the implicit default parameterization, opens read-only, and contains one model row, the expected
      job rows, and the expected lattice rows.
- [ ] A package built with an explicit in-test parameterization stores the applied pair/group/site/frequency
      values, not compiled defaults, and `models.signature` equals `ParameterizedModel.Signature`.
- [x] The published package has no `results` table, needs no `-wal`/`-shm` files, and reports
      `application_id = 0x4D4F4353` and `user_version = 1`.
- [x] Every job references a valid model and lattice; `job_id` is unique and ascending; a partial build leaves
      gaps exactly as recorded in `job_selection`.
- [x] `verify` re-encodes the parameterization signature from the payload's absolute `frequency_hz` values and
      re-encodes the lattice signature from the particle array and the referenced `model_signature`.
- [x] Every environment pair entry is one row per periodic image with its relative `offset`; the compiler emits
      one `CompiledPairLink` per image and the writer maps it one-to-one, Step 14's target expansion
      (`cell + offset` wrapped, `target_position`) reproduces the compiled minimum-image link for every pair
      table, and a multi-image shell (for example a six-image nearest-neighbour shell) is fully present.
- [ ] The writer asserts `tracker_order.Length` equals the representative direction's path-node count; the
      verifier proves the payload-checkable part (non-empty, non-sentinel values below `global_tracker_count`,
      length within the documented bound), and Step 13's reader loads it as a variable `u8[]` with `0xFF` for
      non-mobile nodes; Step 18's mobile-tracker permutation follows the same indices.
- [x] `PKG001`–`PKG010` are documented with severities and anchors, `PKG` descriptors are composed into
      `DiagnosticCatalog.Default`, and `Mocassin.Results` still references only `Core` and `Contracts`.
- [x] The checked-in `.msl` fixture's `package-summary-v1` golden matches a rebuild after normalizing
      `created_utc`, `app_version`, `generator`, size, and payload byte hashes (`PackageFixtureTests`).
- [x] Two jobs whose lattices are byte-identical share one `lattices` row; a different particle array does not.
- [x] Rebuilding the same project produces identical model/lattice signatures and an identical canonical
      summary; stored bytes may differ only in build metadata (`PackageFixtureTests.RebuildReproducesTheFixtureSummary`).
- [x] The job `flags` word equals `SolverJobFlagsMapper.Compose(...)` and contains exactly one kind bit.
- [x] The job `electric_field_direction` is a unit vector equal to the compiled cell transform of the authored
      fractional direction (tested on a non-orthogonal cell).
- [x] `extensions[].config_json` is byte-equal to the project canonical JSON for the same config, including a
      number token above 2^53.
- [x] A cancelled build leaves no temp file and does not modify an existing package.
- [x] A simulated disk-full/write error leaves the previous package intact.
- [x] `verify` detects a truncated payload, a corrupt Zstd frame, a wrong signature, a missing FK, a wrong
      payload-derived column, a stray `-wal`, and an injected `results` table, each with the expected `PKG` code.
- [x] 10⁴ small jobs build within the time/size budget (amended: 3.87 s, 10.05 MB, 118 MB peak RSS, 5,853 jobs/s; net10 reference: 4.03 s, equal size, 119 MB, 5,230 jobs/s).
- [x] No EF Core, no native structs, no custom binary header, and no results table.
- [x] Documentation written

## Documentation

At this point, document the simulation package:

- [x] `docs/reference/msl-format.md` — SQLite schema, `application_id`/`user_version`, content-addressed
  models/lattices, model/job/lattice payloads (including the pair-link `offset` and variable-length
  `tracker_order`), `package-summary-v1`, versioning, and verification checks (new page plus a nav entry in
  `zensical.toml` and `docs/reference/index.md`).
- [x] `docs/reference/file-layout.md` — package location, immutable package rules, the separate `.msr` store,
  atomic-write and temp-file behaviour, the checked-in fixture layout, and the generated
  `package-payloads-v1.schema.json`.
- [x] `docs/reference/diagnostics.md` — `PKG001`–`PKG010` with severity, owner, and help text (feature 6).
- [x] `docs/development/architecture.md` — `Mocassin.Packages` writer versus `Mocassin.Results` reader boundary,
  the Contracts-owned `PKG` descriptors, the payload mapping, and the .NET 11/native-zstd toolchain.
- [x] `docs/development/testing-and-parity.md` — package fixture regeneration and the `.msl` test tiers.
- [x] `docs/user-guide/building.md` — the build pipeline, options, reproducibility, atomic publication, and the
  Step 12 CLI status. The owner directed that this page be written in feature 7 even though the original design
  deferred the CLI-facing page to Step 12; it describes the implemented pipeline and marks the command as
  Step 12 work.

## Tests

| Test | Tier | Assertion |
| --- | --- | --- |
| Schema | PR-fast | Tables, columns, indices, pragmas, `application_id`/`user_version`, and `schema_info` rows match the contract |
| Model payload mapping | PR-fast | Every §3.3 field maps from the documented compiled/parameterized source; values equal `ParameterizationValues` exactly |
| Pair-image expansion | PR-fast | One payload entry per compiled per-image link; offsets unique; a multi-image shell expands to the same target set as `PairReduction`; the model signature still covers every link; Step 14's expansion reproduces the compiled minimum-image target for every image |
| Tracker order | PR-fast | Variable length equals the node count; sentinel and tracker indices are correct for KMC; MMC rules are all `0xFF`; Step 13/18 consume it |
| Lattice payload and layout | PR-fast | `lattice.Length == a·b·c·position_count`; row-major index order; particle indices valid |
| Job payload mapping | PR-fast | Settings, optimizers, `doping_info`, extension JSON, and `flags` match the descriptor |
| Field direction | PR-fast | Unit vector; non-orthogonal cell; zero-modulus and zero-direction cases |
| Signatures | PR-fast | Parameterization and lattice projections re-encode exactly; golden `.bin` vectors match |
| Dedup | PR-fast | Identical model/lattice inserted once; different particles insert a second row |
| Determinism | PR-fast | Rebuild yields identical signatures and an identical `package-summary-v1` golden after metadata normalization |
| Atomicity | PR-fast | Cancel and injected write failure delete the temp file and preserve an existing package |
| Immutability | PR-fast | No `results` table; no `-wal`/`-shm` after publication; read-only open succeeds |
| Job IDs | PR-fast | Unique, ascending, template-ordered; partial-build gaps preserved |
| Verification | PR-fast | Each `PKG001`–`PKG010` fires on its corrupt/truncated/inconsistent fixture with the documented severity |
| Diagnostic ownership | PR-fast | `PKG` descriptors appear in `DiagnosticCatalog.Default` and the docs page; `Mocassin.Results` has no `Mocassin.Packages` reference |
| Decode bounds | PR-fast | A Zstd bomb or oversized count produces `PKG004`, not OOM |
| Property: payload round-trip | PR-fast | Arbitrary valid compiled/parameterized models encode and decode to the same logical values |
| Property: signature stability | PR-fast | Normalized `-0.0`, shuffled map order, and a different Zstd level do not change the signature |
| Cross-language fixture | PR-medium | The checked-in `.msl` and extracted model payload are accepted by the Step 13 reader and drive Step 14's link expansion and Step 15's pair deltas unchanged |
| Corruption/fuzz | Nightly | Mutated `.msl` files never crash or over-allocate the verifier |
| Performance | Nightly/release | 10⁴ jobs under 10 min / 2 GB; model payload under 50 MB; ≥ 100 jobs/s insertion |

Every test declares its tier. Tests use the real compiler, parameterizer, expander, generator, and writer; no
domain doubles. Temporary directories and injected IO failures are the only boundary doubles.

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| SQLite bloat for large campaigns | Content addressing, Zstd, `--compact`, advisory size report, external results later |
| MessagePack/Zstd drift between .NET and Rust | Standard frames and maps; manifest-backed payload descriptors; checked-in payload fixtures read by Step 13 |
| Pre-release contract delta breaks a golden vector | Apply each delta with its regenerated manifest/schema/vectors in the same feature; `verify.sh contracts` is a gate |
| Signature covers a value the payload cannot reproduce | Only parameterization and lattice projections are re-encoded; `model_signature` is cross-checked at write time and treated as an identity string |
| Long transactions lock the file | Batch commits; build only in a temp file |
| Cancellation or crash leaves partial data | Temp file, delete on failure, fsync + atomic rename; orphaned temp files are inert |
| Determinism across SQLite/Zstd versions | Hash logical projections, not pages or compressed bytes; golden tests compare canonical summaries |
| Extension config loses authored number tokens | Store canonical JSON text, not a numeric map |
| Pair image identity is lost | The compiler emits one `CompiledPairLink` per periodic image and the payload maps it one-to-one; the existing model-signature link projection signs every image, and Step 14's expansion is tested against the compiled links |
| Variable-length arrays drift across languages | `tracker_order` length is specified and validated as the node count; the golden payload fixture is read by Step 13 |
| Pre-release package-shape basis is disputed | ADR 0009 records the pre-release finalization; the first released `.msl` starts the version-bump discipline |
| Cross-step API extension is rejected | The design recommends the per-image compiled link (additive preview field only); the alternative (solver-side reconstruction from a stored bucket/geometry identity) is ambiguous for chiral or coincident shells, so it needs a documented uniqueness proof before use |
| Model-signature value change ripples into Step 09 fixtures | Regenerate the Step 08 model vectors, Step 09 parameterization vectors/`signature-vectors.json`, and the checked-in sample `modelSignature` values in the same feature; the projection schema is unchanged |
| Lattice dedup merges distinct provenance | Doping provenance is kept on the job, not on the lattice |
| Verifier becomes an unbounded parser | Explicit decompressed-size caps, `UntrustedData`, count/limit checks before allocation |
| Placement of `.msl` reading in the wrong project | Read-only inspection lives in `Mocassin.Results`, matching the architecture table; the writer only self-checks its own payloads |

## Done when

A valid, versioned, immutable `.msl` package can be built atomically from a project's compiled and parameterized
model plus streamed jobs, its model/lattice signatures re-encode from the stored payloads, every environment pair
image and variable-length tracker order is preserved for Step 14/15 and Step 18, a read-only verifier and
inspector report its content and `PKG` integrity with a `package-summary-v1` golden, the contract manifest and a
separate generated payload schema describe the payloads, and a checked-in package fixture is ready for the Step 13
Rust reader — with deterministic content signatures and bounded memory.
