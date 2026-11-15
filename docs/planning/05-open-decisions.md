# 05 — Open Decisions and Deferred Scope

This page tracks architecture decisions that are not yet final, plus items that are intentionally deferred.
It complements the ADRs: an ADR records a final decision; this page records what is still open and where the
decision will be made.

## 1. Status vocabulary

| Status | Meaning |
| --- | --- |
| **Resolved** | Decision recorded in an ADR or contract; implementation can proceed. |
| **Gated** | Decision is intentionally deferred to a named step with a spike or acceptance gate. |
| **Deferred** | Out of the first release; target release noted. |
| **Open** | A decision is needed before the named step can be completed. |

## 2. Resolved by ADR or contract

| Topic | Decision | Record |
| --- | --- | --- |
| Canonical signatures | Canonical MessagePack projection, fixed widths/order, domain-separated SHA-256 | [ADR 0010](adr/0010-canonical-messagepack-signatures.md) |
| `.msl` mutability | Immutable package; results in `.msr` sidecar; no WAL dependence for `.msl` | [ADR 0011](adr/0011-immutable-package-and-results-store.md) |
| Transport/MSD | Origin-based cumulative MSD; per-species and total transport; tracked-ensemble normalization | [ADR 0012](adr/0012-transport-and-msd-definitions.md) |
| Format lifetime | `.msl`/`.msr` current + previous major; `.mcs` current-only; SemVer for public APIs | [ADR 0013](adr/0013-format-compatibility-and-api-versioning.md) |
| Extension boundary | C ABI, panic containment, opt-in in-process loading, versioned host table | [ADR 0014](adr/0014-extension-abi-and-trust-boundary.md) |
| Contract manifest | `contracts/manifest.json` plus generated checks/fixtures | [ADR 0009](adr/0009-contract-manifest-and-contracts-library.md) |
| Project persistence | `ProjectStore` in `Mocassin.Packages`, not `Core` | [Step 01](steps/step-01-domain-kernel-and-project-format.md) |
| Primitive geometry | `Fractional3`/`Cartesian3`/`Vector4I` in `Core.Geometry`; public generic `Vector3<T>`/`Vector4<T>`/`Matrix3<T>`/`Matrix4<T>`; `Int3x3` for exact integer symmetry | [Step 02](steps/step-02-symmetry-and-geometry.md) |
| Space-group catalog | spglib as the sole source; 274-setting compatibility subset; committed canonical JSON, dev-only generator, CI drift check, checksum, and BSD-3-Clause attribution | [Step 02](steps/step-02-symmetry-and-geometry.md) |
| Space-group reference | Project stores structured `{number, axis, origin}`; HM/Hall/system/operations are catalog-derived | [Step 02](steps/step-02-symmetry-and-geometry.md) / [Step 03](steps/step-03-particle-and-structure-models.md) |
| Coordinate grid | Public `Fractional3` stays `double`; Symmetry uses an internal exact `24 × 10^12` grid for P1 expansion, deduplication, and ordering | [Step 02](steps/step-02-symmetry-and-geometry.md) |
| Cell geometry | Strict `0<α,β,γ<180°` plus positive Gram determinant; determinant-based cell vectors; named tolerances; reject inconsistent values | [Step 02](steps/step-02-symmetry-and-geometry.md) |
| P1 order and index | Canonical ascending grid order; `Vector4I(cellA,cellB,cellC,P)`; signed 32-bit linear index with 64-bit total-count check | [Step 02](steps/step-02-symmetry-and-geometry.md) |
| Symmetry reduction | Center, setwise, and pointwise stabilizers; induced site-permutation group; canonical pair shells; 6-bit cluster occupation codes | [Step 02](steps/step-02-symmetry-and-geometry.md) / [Step 08](steps/step-08-model-compilation-pipeline.md) |
| Periodic geometry | Base-P1 precomputation, metric-tensor distances, separate minimum-image/radial-search APIs, plane-distance bounding boxes, 1 fm buckets | [Step 02](steps/step-02-symmetry-and-geometry.md) |
| Symmetry contract and caches | `symmetry-1`; per-compilation caches; SHA-256 persisted IDs; brute-force/golden test tiers; base-P1 memory | [Step 02](steps/step-02-symmetry-and-geometry.md) |
| Project symmetry metadata | Optional project-level catalog/contract metadata; mismatch warns `PROJ034`; compiled payload is authoritative | [Step 02](steps/step-02-symmetry-and-geometry.md) / [Step 03](steps/step-03-particle-and-structure-models.md) |
| Pair ordering | Canonical shell order `(distanceBucket, targetP, cellA, cellB, cellC)`; stable IDs are content hashes, not ordering | [Step 02](steps/step-02-symmetry-and-geometry.md) / [Step 08](steps/step-08-model-compilation-pipeline.md) |
| Cell conditioning | `CellConditionLimit = 1e12` in addition to the Gram determinant cutoff | [Step 02](steps/step-02-symmetry-and-geometry.md) |
| Cross-process job contract | Job folder, `solver.json`, JSONL events, `mmcfe.db` | [`02-data-contracts.md`](02-data-contracts.md) §5 |
| Large state spill | Embed `.mcs` up to 1 GiB; larger states are external files referenced by `.msr` | ADR 0011 / data contracts §3.6 |
| MMCFE log | SQLite `mmcfe.db` with a versioned, resumable schema | [`02-data-contracts.md`](02-data-contracts.md) §5.4 |
| Security model | Untrusted parsers, extension trust boundary, path/zip/decompression guards | [`01-target-architecture.md`](01-target-architecture.md) §7.5, ADR 0014 |
| Settings schema | Versioned `mocassin.settings` with migrations and retention defaults | [Step 23](steps/step-23-avalonia-shell-and-project-lifecycle.md) |
| Accessibility | WCAG 2.2 AA target plus manual screen-reader checks | [`03-quality-strategy.md`](03-quality-strategy.md) |
| Test strategy | Testing trophy; mocks only at true boundaries | [`03-quality-strategy.md`](03-quality-strategy.md) |
| Validation pipeline | `Mocassin.Packages.Projects.Validation`; load/model/cross-domain/feasibility, build-stage extension | [Step 07](steps/step-07-validation-and-diagnostics.md) |
| Validation output | `validation-v1` JSON report; sorted diagnostics, raw summary, optional statistics | [`02-data-contracts.md`](02-data-contracts.md) §5.6 / [Step 07](steps/step-07-validation-and-diagnostics.md) |
| Compiler API | Synchronous, pure `IModelCompiler.Compile(Project, CompilationOptions, CancellationToken)`; Step 12 wraps it asynchronously | [Step 08](steps/step-08-model-compilation-pipeline.md) |
| Compilation validation boundary | Compiler assumes a validated `Project`, does not consume `ValidationResult`, and does not expose `IValidationStage`; Step 12 owns the Build stage | [Step 08](steps/step-08-model-compilation-pipeline.md) / [Step 12](steps/step-12-build-orchestration-and-cli.md) |
| Compiled vs parameterized signature | Step 08 geometry/rules/encoding `model_signature`; Step 09 parameterization signature over model + values; runtime settings excluded | [Step 08](steps/step-08-model-compilation-pipeline.md) / [Step 09](steps/step-09-parameterization-templates.md) |
| Compilation cache | Per-compilation by default; optional caller-owned bounded thread-safe `ICompilationCache`; no static process-wide cache | [Step 08](steps/step-08-model-compilation-pipeline.md) |
| Derived solver intent | Compiler emits `Kind`, `UsePrerun`, `UseDualDofCorrection`; Step 11 composes `SolverJobFlags` | [Step 06](steps/step-06-lattice-and-simulation-models.md) / [Step 08](steps/step-08-model-compilation-pipeline.md) / [Step 11](steps/step-11-simulation-package-and-msl-writer.md) |
| Tracker ownership | Step 08 emits definitions/mappings; Step 18 owns mutable trackers, mobile assignment, and `.mcs` output | [Step 08](steps/step-08-model-compilation-pipeline.md) / [Step 18](steps/step-18-tracking-statistics-and-state-writer.md) |
| Compile diagnostics | Step 08 owns the `CMP` prefix; `CompilationDiagnosticCatalog` composes into `DiagnosticCatalog`; docs-drift checked | [Step 08](steps/step-08-model-compilation-pipeline.md) / [Step 07](steps/step-07-validation-and-diagnostics.md) |
| `mocassin compile` | Step 08 is library + canonical JSON inspection only; Step 12 owns the command and full CLI | [Step 08](steps/step-08-model-compilation-pipeline.md) / [Step 12](steps/step-12-build-orchestration-and-cli.md) |
| Template persistence and versioning | Templates live in the project `parameterizations` array; `ParameterizationId` is UUIDv7; versioning is the project `schemaVersion`; no separate template document or `formatVersion` in v1 | [Step 09](steps/step-09-parameterization-templates.md) / [`02-data-contracts.md`](02-data-contracts.md) §2.3 |
| Template identity | Pair `EnergyId`; group `ClusterEnergyId` + canonical occupation tuple; rule `RuleGeometryId` inside `(transitionKind, transitionId)`; site override `(siteId, particleId)`; IDs are never dense indices | [Step 09](steps/step-09-parameterization-templates.md) / [Step 08](steps/step-08-model-compilation-pipeline.md) |
| Template sparse defaults | Absence means unset; pair/group `0 eV`, rule `1e13 Hz`, site background from authored model values; explicit defaults are real entries; creation writes set headers only | [Step 09](steps/step-09-parameterization-templates.md) |
| Parameterization signature | Values-only fixed six-field projection over `modelSignature` + applied pair/group/site/frequency tables; dense compiled order; 12-decimal quantization; no IDs, template metadata, or runtime settings. The pre-release `parameterization-v1` layout finalization is covered by ADR 0010's signature-layout exception. | [Step 09](steps/step-09-parameterization-templates.md) / [ADR 0010](adr/0010-canonical-messagepack-signatures.md) |
| Parameterization ownership | Template records in Core; applied `ParameterizationValues` + signature in Contracts; factory/apply/validation/diff/rebase in Packages; Compilation stays parameterization-free | [Step 09](steps/step-09-parameterization-templates.md) / [`01-target-architecture.md`](01-target-architecture.md) §3 |
| Site-energy background | Step 08 does not compile it; Step 09 resolves authored `siteEnergies` to the `[position][particle]` background, applies overrides, and Step 15 consumes the table | [Step 09](steps/step-09-parameterization-templates.md) / [`02-data-contracts.md`](02-data-contracts.md) §3.3 |
| Parameterization build-stage validation | No Step 09 `IValidationStage`; structural `PRM` checks run in Core, compiled-bound checks run in `ParameterizationService.Validate` called by Step 12 after compilation | [Step 09](steps/step-09-parameterization-templates.md) / [Step 12](steps/step-12-build-orchestration-and-cli.md) |
| `mocassin parameterize` | Not added; Step 12 owns `mocassin compile` and `mocassin build --parameterization <id>`; template creation/editing is the library API used by Steps 12/26 | [Step 09](steps/step-09-parameterization-templates.md) / [Step 12](steps/step-12-build-orchestration-and-cli.md) |
| Job/lattice seeds and build sampler | ADR 0005 tuple order; SHA-256 job/lattice derivation with fixed endianness; build-side SHA-256 counter stream and Fisher–Yates sampling; no new dependency | [Step 10](steps/step-10-job-templates-and-supercells.md) / [ADR 0005](adr/0005-statistical-parity-and-rng.md) |
| Package payload contract | `Mocassin.Contracts` owns the model/job/lattice payload DTOs; the manifest gains `formats.package.payloads` at the same top-level granularity as `.mcs`; each payload has its own `formatVersion = 1` | [Step 11](steps/step-11-simulation-package-and-msl-writer.md) / [ADR 0009](adr/0009-contract-manifest-and-contracts-library.md) |
| Lattice signature | Five-field projection `[formatVersion, modelSignature, latticeSize, positionCount, particles]`; excludes build metadata and the doping summary; finalized in Step 11 under the pre-release exception | [Step 11](steps/step-11-simulation-package-and-msl-writer.md) / [ADR 0010](adr/0010-canonical-messagepack-signatures.md) |
| Package inspection and verification | Read-only `.msl` inspection/verification lives in `Mocassin.Results` (one reader for CLI/evaluation/tools); `Mocassin.Packages` only self-checks the payloads it encodes; `PKG001`–`PKG010` descriptors are Contracts-owned, composed into `Mocassin.Packages`' `DiagnosticCatalog` but used directly by `Results` (no `Results -> Packages` edge) | [Step 11](steps/step-11-simulation-package-and-msl-writer.md) / [`01-target-architecture.md`](01-target-architecture.md) §3.1 |
| Package pair links | Environment pair entries are one row per periodic image with `{target_position, offset:[i32;3], energy_id}`; the compiler emits one `CompiledPairLink` per image from `PairShell.EquivalentImages` (exposed on `PairInteractionPreview`), so the existing four-field model-signature projection signs every image; Step 14 expands `cell + offset` into the target instance | [Step 11](steps/step-11-simulation-package-and-msl-writer.md) / [Step 14](steps/step-14-simulation-context-initialization.md) / [Step 08](steps/step-08-model-compilation-pipeline.md) |
| `tracker_order` shape | Variable-length `u8[]` with one global tracker index or `0xFF` per path node; length equals the rule's node count; not a fixed eight-entry array | [Step 11](steps/step-11-simulation-package-and-msl-writer.md) / [Step 13](steps/step-13-rust-solver-foundation-and-package-reader.md) / [Step 18](steps/step-18-tracking-statistics-and-state-writer.md) |
| Canonical package summary | `package-summary-v1` inspector projection (not a package format) for determinism, golden fixtures, and benchmark evidence; excludes build metadata, size, and payload byte hashes | [Step 11](steps/step-11-simulation-package-and-msl-writer.md) / [`02-data-contracts.md`](02-data-contracts.md) §3.5 |
| Package-shape pre-release basis | `.msl` table/payload shape finalized at `formatVersion = 1` before the first released package; the ADR 0009 version-bump rule resumes at first release | [ADR 0009](adr/0009-contract-manifest-and-contracts-library.md) / [Step 11](steps/step-11-simulation-package-and-msl-writer.md) |
| Extension config in `.msl` | Job payload stores `{id, config_json}` with the canonical UTF-8 JSON text, preserving authored numeric tokens and matching the Step 19 extension ABI byte payload | [Step 11](steps/step-11-simulation-package-and-msl-writer.md) / [`02-data-contracts.md`](02-data-contracts.md) §3.4 |
| Electric field direction in `.msl` | Job and model default settings store the normalized Cartesian unit vector derived from the authored fractional direction; the solver consumes it directly (Step 15) | [Step 11](steps/step-11-simulation-package-and-msl-writer.md) / [`02-data-contracts.md`](02-data-contracts.md) §3.4 |
| Package build-stage validation | Step 11 adds no `IValidationStage`; package size/dedup are build-report warnings that Step 12 maps to build output | [Step 11](steps/step-11-simulation-package-and-msl-writer.md) / [Step 12](steps/step-12-build-orchestration-and-cli.md) |
| `.msl` stable columns | `norm_factor`/`base_frequency_hz` are removed (no build-time value; Step 16 derives factors); `application_id = 0x4D4F4353`, `user_version` mirrors `format_version`; one model row per package in v1 | [Step 11](steps/step-11-simulation-package-and-msl-writer.md) / [`02-data-contracts.md`](02-data-contracts.md) §3.2 |
| .NET toolchain and payload codec | Repository targets `net11.0` on SDK `11.0.100` (`latestPatch`, no prerelease); the `.msl` payload codec is the in-box `System.IO.Compression` Zstandard API with one one-shot encoding path and quality = `PackageBuildOptions.CompressionLevel` (1..19); `ZstdSharp.Port` is removed; `Microsoft.NET.Test.Sdk` moves to `18.12.0` for net11 compatibility; no `.msl`, signature, summary, or solver-contract change | [Step 11 amendment 1](amendments/step-11-amend-1-dotnet-11-native-zstd.md) / [ADR 0010](adr/0010-canonical-messagepack-signatures.md) |
| Payload codec performance gate | Part A passed on 12 real payloads (240/240 required checks, two runs): size 0.000 %, encode −4.20 %/−5.88 % (L3/L9), decode 302/299 MB/s, round-trip and cross-decoder 100 %. Below 8 KiB, decode gates on an absolute penalty ≤0.15 µs per payload plus ≥100 MB/s; at or above 8 KiB the ≤+10 % relative rule stands. Byte identity is an observation, not a rollback threshold | [Step 11 amendment 1 §9.1](amendments/step-11-amend-1-dotnet-11-native-zstd.md) |

## 3. Gated decisions

| Topic | Gate | What must be decided | Fallback |
| --- | --- | --- | --- |
| 3D backend | Step 27 spike | Silk.NET + Avalonia native host vs. an OpenGL control or other backend; macOS/Metal path | ADR 0006 fallback: keep the abstraction and ship the working backend; defer advanced effects |
| AOT/single-file CLI | Step 31 | Whether SQLite/MessagePack dependencies permit `PublishAot`/single-file for CLI and tools | Normal self-contained publish; GUI is not AOT |
| Update mechanism | Step 31 | Whether to ship any updater at all | Offline-first: no auto-update; users install new releases via the package/installer |
| Remote deploy | Step 26/28 | Whether SSH/SFTP deploy ships in the first release | Local build + HPC scripts only; mark deferred in the parity matrix |
| `.msr` format | Step 20 | Journal mode, locking, and state spill are decided; embedded-size cap and retention defaults may be tuned after benchmarks | `journal_mode=DELETE`, one collector, embed up to 1 GiB then external state files, no auto-delete |
| Model editor layout | Step 24 spike | Choose the master-detail table/list + editor layout from owner-reviewed POCs: ListBox table, TreeView, TabControl, SplitView, custom TableView, or custom matrix. No DataGrid, TreeDataGrid Pro, or third-party grid. | ListBox table + detail editor baseline from ADR 0003. |
| Step 11 sizing | Step 11 start gate | Approve the 3.8–4.2 pw single-step exception or split 11A (schema/model/verifier) and 11B (jobs/fixtures/closeout) | Single-step exception, matching the Step 09 precedent |
| SDK feature band | After Step 11 is merged | Whether to move from the `11.0.1xx` band to a later feature band (`latestPatch` → `latestFeature` or a new pin); a feature band can change analyzers and generated code | Stay on `11.0.100`/`11.0.1xx`; any move is an explicit recorded change |
| Native Zstandard rollback | Step 11 amendment 1 Part A/Part B thresholds and correctness gates | Part A passed under the refined rule (2026-11-15). A Part A or Part B threshold breach or a correctness failure can still trigger rollback; a regression in the implemented codec is a Part A threshold breach even though the Part A measurement passed | Roll back to the unamended toolchain, drop the feature branch, and record the measurements here |
| Development image build | Step 11 closeout | Build `opencode:mocassin` from the amended containerfile (SDK 11.0.100 only) and record the image reference/digest, or record the accepted risk with the missing evidence | Owner-accepted risk: record "image not built" in `docs/planning/closeout/step-11.json` `notes` and the audit record; no page claims the image was built |

## 4. Deferred scope

These are intentionally out of the first parity release. They must remain marked in
[`appendix-b-parity-matrix.md`](appendix-b-parity-matrix.md).

| Item | Target | Rationale |
| --- | --- | --- |
| Append to existing package | post-1.0 | Rebuild is safer; `.msl` is immutable |
| GPU acceleration | post-1.0 | Not in the original; CPU baseline first |
| Parquet export | post-1.0 | CSV/JSON first; optional dependency |
| External result storage in `.msr` | post-1.0 | Keep v1 self-contained; add for very large HPC campaigns |
| Advanced 3D effects (SSAO/shadows) | post-1.0 | Quality subset first |
| Localization | post-1.0 | English first; resources structured for later |
| Time-lagged MSD | post-1.0 | Origin-based MSD matches the original; separate API later |
| Remote deploy | post-1.0 | Local + HPC scripts first |
| Unified docs search across narrative + APIs | post-1.0 | Each generated API keeps its own search |
| PDF/offline documentation | post-1.0 | Current-version site first |

## 5. Remaining owner input

The following are the only topics still expected to need an explicit owner decision:

1. **3D backend** after the Step 27 spike (choose Silk.NET host, OpenGL control, or another backend).
2. **Remote deploy** scope for the first release (ship or defer).
3. **Update mechanism** policy (no updater vs. signed installer/package channel).
4. **AOT/single-file CLI** if dependency constraints make it feasible.
5. **Model editor layout** after the Step 24 POC spike (choose ListBox table, TreeView, TabControl,
   SplitView, custom TableView, or custom matrix).

Everything else is either resolved above or specified in the data contracts, architecture, or step pages.
When a decision is made, add an ADR (if expensive to reverse) and update this page.
