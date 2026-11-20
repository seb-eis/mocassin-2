# Appendix B — Parity Matrix

Status legend:

- **Planned** — part of the phased full-parity scope; not yet implemented.
- **Partial** — the model/API part is implemented; UI or later-step integration is still pending.
- **Improvement** — new capability or deliberate fix beyond the original.
- **Replaced** — original feature is delivered through a different mechanism/UX.
- **Deferred** — out of the first parity release; target noted.
- **Out of scope** — explicitly excluded (ADR/decision).

`Req` = required for the first parity release.

## 1. Project, files, and lifecycle

| Feature | Req | Status | Step | Notes |
| --- | :---: | --- | --- | --- |
| Create new project | Y | Partial | 01/12/23A | Blank, ceria, and barium-zirconate skeletons in 01/03; headless `mocassin new` with `--template blank` or `--template ceria`, fresh identity, and embedded assets in 12; creation UI in 23A using the same `ProjectTemplateFactory` and shared `ProjectPathRules` |
| Open/save/close project | Y | Planned | 01/23A | Atomic writes and lock store in 01; lifecycle UI, read-only lock mode, save-as lock migration, reload, and external-change detection in 23A |
| Recent files | N | Improvement | 23A | Settings-persisted list (max 10), missing-file handling, clear action |
| Multiple projects per file | N | Replaced | 23A | One project per file; workspace of open files with one tab strip and a project switcher |
| Duplicate/delete project in a library | N | Replaced | 23A | File-level duplicate/delete; no DB library |
| Autosave/recovery | N | Improvement | 01/23B | `.autosave` sidecar store in 01; scheduler, recovery prompt, untitled-project recovery in `recovery/`, and retention in 23B |
| Dirty tracking/unsaved prompt | Y | Planned | 01/23B | Document hash and dirty state in 01; content-based save point, title/tab markers, batch prompt in 23B |
| Undo/redo | N | Improvement | 01/23B | Core stacks in 01; shell commands, save-point handling, bounded depth, and per-project isolation in 23B |
| Project schema versioning/migration | Y | Planned | 01 | v1 pipeline implemented; migration fixtures added per version |
| Stable project hash | Y | Done | 01/03/09 | Model/document SHA-256 in 01; structure hash in 03; compiled and parameterization signatures in 08/09 |
| Legacy `.mocprj` read/write | N | Out of scope | — | ADR 0001 |
| Project data viewer (JSON/XML) | N | Replaced | 23A | Read-only Project JSON document with copy and debug export |
| Project metadata editing (name/description/author/notes) | Y | Planned | 23B | Project Overview editor; undoable through the shared edit pipeline |
| Cross-domain validation pipeline | Y | Implemented | 07 | Load/model/cross-domain/feasibility; no post-validation mutation |
| Stable diagnostic catalog | Y | Implemented | 07 | Codes, owners, severities, paths, hints; docs-drift checked |
| Model statistics and feasibility | Y | Implemented | 07 | Counts, rule/memory estimates, buildability warnings |
| Headless `mocassin validate` | Y | Done | 07/12 | `validation-v1`, diagnostics, statistics, and exit codes in 07; the complete CLI command surface, shared report family, and parser conventions in 12 |
| `validation-v1` report | Y | Implemented | 07 | Sorted diagnostics, summary, optional statistics; schema checked |
| GUI diagnostic navigation | Y | Planned | 07/23B/24/25 | `ModelPath` navigation and the target registry in 23A; status indicator, Problems channel, and error banner in 23B; domain providers and stats panels in 24/25 |

## 2. Particles and structure

| Feature | Req | Status | Step | Notes |
| --- | :---: | --- | --- | --- |
| Particle name/symbol/charge/vacancy | Y | Implemented | 03 | Typed IDs; model, JSON, validation, and editing API |
| Implicit void particle | Y | Implemented | 03 | Reserved ID; compiled index 0 belongs to 08 |
| 63 custom particle limit | Y | Implemented | 03 | Enforced |
| Particle sets with first-as-default | Y | Implemented | 03 | Ordered `particleIds`; composed validation |
| Drag-and-drop set membership | Y | Planned | 24 | + keyboard |
| Space group selection (274 settings) | Y | Partial | 02/03/24 | Catalog, lookup, and validation in 02/03; selection UI in 24 |
| Space-group operation display | Y | Planned | 24 | Operations are available from the catalog |
| Cell parameters + crystal-system locks | Y | Partial | 02/03/24 | Corrected transform, validation, and tolerances in 02/03; UI locks in 24 |
| Stable/unstable sites | Y | Implemented | 03 | Unstable sites default to void |
| P1 expansion preview | Y | Partial | 02/03/24 | Encoder and P1 order in 02; preview API/cache in 03; UI in 24 |
| Site overlap validation | Y | Implemented | 03 | Exact grid; structured `CellSiteId` paths |
| Dummy position editor | Y | Planned | 24 | Model, JSON, and validation in 03; editor UI in 24 |
| Correct triclinic/rhombohedral cell vectors | Y | Improvement | 02 | Implemented in 02; fixes original bug |
| Valid group 47 data | Y | Improvement | 02 | Implemented in 02; fixes corrupt setting |
| Multiple site definitions per orbit | Y | Implemented | 03 | With overlap diagnostics |

## 3. Transitions

| Feature | Req | Status | Step | Notes |
| --- | :---: | --- | --- | --- |
| State changes [D,A] | Y | Partial | 04/24 | Model, validation, persistence, preview, and editor API in 04; editor UI in 24 |
| State-change groups | Y | Partial | 04/24 | Mixed physical/charge and stable/unstable roles rejected; editor UI in 24 |
| Abstract chains with D/S linkers | Y | Partial | 04/24 | Six patterns + custom grammar; `D S D` rejected; editor UI in 24 |
| 2-site MMC | Y | Partial | 04/08/17 | Model, validation, and preview in 04; exchange rules in 08/17 |
| 3-site KMC migration | Y | Partial | 04/08/16 | Model, path validation, and preview in 04; rules and KMC dual-DOF default in 08/16 |
| 5-site interstitialcy | Y | Partial | 04/08 | Model, path validation, preview, and sample in 04; rules in 08 |
| 7-site interstitialcy | Y | Partial | 04/08 | Model, path validation, and preview in 04; rules in 08 |
| 5-site 2-species vehicle | Y | Partial | 04/08 | Model, path validation, preview, and sample in 04; unidirectional rules in 08 |
| 6-site 2-species vehicle | Y | Partial | 04/08 | Model, path validation, and preview in 04; unidirectional rules in 08 |
| Charge transfer/polaron hop | Y | Partial | 04/08 | Homogeneous charge-state groups and sample in 04; conservation/rules in 08 |
| Reverse rule generation (2/3-site) | Y | Done | 08 | Auto-inverse for `Metropolis2`/`Migration3`; `CeriaKineticMigrationCompilesForwardAndInverseRules` and `MetropolisExchangeCompilesForwardAndInverseRulesWithoutState1`; explicit reverse stays separate |
| No reverse for complex mechanisms | Y | Done | 08 | `CorrectedVehicleCompilesUnidirectionalRules`, `CorrectedInterstitialcyCompilesWithASpectatorCentre`, and `ExplicitReverseCompilationTests`; `GeneratesReverse` validated in 04 |
| Dependent/hidden rules | Y | Partial | 08/29 | Explicit deduplication and dependent/hidden counts exist; equivalent-start orbit reduction and `DependentRules` emission are the accepted Step 08 audit m1 gap. Step 29's release gate requires every required parity item to be implemented or explicitly deferred (roadmap M12; Step 29 §9). |
| Reference path editing/preview | Y | Partial | 04/24 | Preview API in 04; editor UI in 24 |
| Ambiguity detection | Y | Improvement | 08 | Compile-time `CMP004` with both identities |
| Transition rule count preview | N | Improvement | 24 | Step 07 statistics are advisory; Step 08 is authoritative |

## 4. Energy and lattice

| Feature | Req | Status | Step | Notes |
| --- | :---: | --- | --- | --- |
| Stable environment cutoff | Y | Partial | 05/25 | Model, validation, preview, and samples in 05; editor UI in 25 |
| Hollow-sphere filters | Y | Partial | 05/25 | Model, validation, and samples in 05; editor UI in 25 |
| Per-unstable-site environments | Y | Partial | 05/25 | Model, validation, and samples in 05; editor UI in 25 |
| Auto-sampled pair interactions | Y | Partial | 05/08 | Shared sampling kernel and preview in 05; compiled tables in 08 |
| Chiral pair linking | Y | Done | 05/08/09 | Shell chiral linking in 02; shared `EnergyId` preview in 05; compiled tables in 08; one shared value and no direction-specific field in 09 |
| Group/cluster interactions (up to 9 positions) | Y | Partial | 05/08 | Model, geometry validation, cost preview, and samples in 05; canonical tables in 08 |
| Site/defect energies | Y | Partial | 05/25 | Model, validation, editing, and samples in 05; editor UI in 25 |
| Cost/permutation limits | Y | Implemented | 05/07 | Limits, diagnostics, preview, consolidated statistics, and docs |
| Cluster geometry editor/preview | Y | Partial | 05/25 | Preview API in 05; editor UI in 25 |
| Site energy matrix + bulk edit | Y | Improvement | 25 | |
| Default building block viewer | Y | Planned | 25 | |
| Occupation exchanges | Y | Partial | 06/25 | Model, validation, persistence, query, and editor API in 06; editor UI in 25 |
| Ordered dopings/priorities | Y | Partial | 06/10/25 | Unique priorities, deterministic order, persistence, and editing in 06; fraction application in 10; editor UI in 25 |
| Counter-doping/charge compensation | Y | Partial | 06/10/25 | Shape/sign validation and persistence in 06; integer ratios and applied counts in 10; editor UI in 25 |
| Doping truncation semantics | Y | Done | 10 | `floor(fraction × available)`, requested vs applied counts, and `JOB010`/`JOB011` |
| Deterministic lattice generation | Y | Improvement | 10 | Fixes the shared crypto RNG; job/lattice seed and SHA-256 sampler vectors |
| Doping preview (requested vs applied) | Y | Improvement | 10/25 | Shared preview service and `build --preview` in 10; editor UI in 25 |

## 5. Compilation pipeline

| Feature | Req | Status | Step | Notes |
| --- | :---: | --- | --- | --- |
| Pure compiled model API | Y | Done | 08 | Synchronous `IModelCompiler` with diagnostics, counts, signatures, cancellation |
| P1-extended dense compiled model | Y | Done | 08 | Canonical P indices, dense particle/site/table indices, stable IDs |
| Auto-sampled pair tables | Y | Done | 08 | Shares Step 05 sampling/reduction kernel; canonical shell order |
| Chiral pair linking in compiled tables | Y | Done | 08/09 | Shared `EnergyId`; one table/value per chiral pair |
| Cluster tables and occupation codes | Y | Done | 08 | Point-group canonicalization; 6-bit codes; expansion maps |
| Forward/inverse/dependent rules | Y | Partial | 08/29 | Forward and inverse rules are Done and tested; dependent instances for symmetry-equivalent starts remain open as Step 08 audit m1, owned by the Step 29 release gate (see §3) |
| Mass/charge conservation filtering | Y | Done | 08 | Rejected counts reported; charge-transfer symbols balanced |
| Rule ambiguity detection | Y | Done | 08 | Compile-time `CMP004`; original failed at runtime |
| Unreferenced transition warning | N | Done | 08 | `CMP009` |
| Simulation encoding / tracker definitions | Y | Done | 08/18 | Collections, directions, mappings, intent; Step 18 owns state |
| Compiled-model signature | Y | Done | 08/09 | Geometry/rules signature distinct from parameterization signature |
| Compilation diagnostics/report | Y | Done | 08 | `CMP` catalog, counts, timings, input hashes, cache statistics |
| Compilation stage cache | N | Done | 08 | Content-hash keys; per-compilation default, optional caller cache |
| Compiled-model JSON inspection | N | Done | 08/12 | `compiled-inspection-v1`; `mocassin compile` in 12 |
| Headless `mocassin compile` | Y | Done | 12 | `compiled-inspection-v1` output with `-o`, `compile-v1` summary with the ten compiler stages, cache statistics, and exit codes |

## 6. Simulations, templates, and builds

| Feature | Req | Status | Step | Notes |
| --- | :---: | --- | --- | --- |
| MMC simulation definition | Y | Partial | 06/17/25 | Model, defaults, limits, persistence, query, and editor API in 06; solver in 17; editor UI in 25 |
| KMC simulation definition | Y | Partial | 06/16/25 | Model, defaults, limits, persistence, query, and editor API in 06; solver in 16; editor UI in 25 |
| All original simulation settings | Y | Partial | 06/25 | Settings, defaults, hard limits, and recommended bands in 06; editor UI in 25 |
| Transition assignment | Y | Partial | 06/25 | Authored-order, kind-filtered assignment and duplicate rejection in 06; multi-select UI in 25 |
| Parameterization template persistence | Y | Done | 09 | Project-resident `parameterizations`; UUIDv7 identity; canonical JSON/ordering/limits; project `schemaVersion`, no separate document |
| Parameterization template creation | Y | Done | 09 | Sparse values, set headers, required-slot summary; library API only |
| Pair/group energy editing | Y | Partial | 09/26 | Compiled-bound apply/validation and pure editor operations in 09; editor UI in 26 |
| Attempt frequency editing | Y | Partial | 09/26 | Absolute `frequency_hz` values and pure editor operation in 09; editor UI in 26; Step 16 derives the per-job factor |
| Chiral linked editing | Y | Partial | 09/26 | Improvement: shared `EnergyId`, one value per chiral pair, independent values unrepresentable; editor UI in 26 |
| Stable model signature | Y | Done | 08/09 | Compiled geometry/rules/encoding signature in 08; values-only parameterization signature in 09 |
| Parameterization signature | Y | Done | 09 | Finalized `signatures.parameterization`; golden applied values and projection vectors |
| Template defaults/completeness | N | Done | 09 | Sparse defaults, `PRM003`/`PRM006` completeness warnings |
| Template rebase/migration | Y | Partial | 09/26 | Diff + explicit report; no silent reuse; editor UI in 26 |
| Site energy overrides in template | N | Done | 09 | Original limitation removed; reset-to-model |
| `PRM` diagnostics | N | Done | 09 | Structural Core checks + compiled-bound Packages checks; docs-drift checked |
| Checked-in golden templates | N | Done | 09 | Ceria and barium-zirconate templates; applied values and signatures regenerated through the real compiler/apply path |
| `mocassin parameterize` CLI | N | Replaced | 12 | `compile` and `build --parameterization` select, validate, apply, and rebase templates; no separate command |
| Job collections/configs | Y | Done | 10 | Typed Core records, project JSON/manifest, authored-order expansion, and structural `JOB` validation |
| Base + config overrides | Y | Partial | 10/26 | Correct simulation → collection → config inheritance, kind checks, and extension replacement in 10; editor UI in 26 |
| Job duplicates/multiplier | Y | Done | 10 | `jobCountPerConfig`/simulation default, 1-based IDs, per-duplicate seeds |
| Selection optimizers | Y | Partial | 10/11/14/26 | P1 image expansion, dual-DOF rule, and `JOB009` reference check in 10; `.msl` encoding in 11; runtime removal in 14; editor UI in 26. Partial until Step 14. |
| Lattice size + doping values | Y | Partial | 10/26 | Deterministic supercell, dopings, truncation, compensation, and preview in 10; editor UI in 26 |
| Build template selection | Y | Partial | 12/26 | `--template` selects a build template; `--job-template` and `--parameterization` override it; ambiguity lists IDs and names; the implicit parameterization default is applied when none exists; selection UI in 26 |
| Build preview (no package write) | Y | Done | 10/12 | Shared `ILatticePreviewService` and `BuildPreviewBuilder`; the CLI projects `BuildResult.Preview` into `build-v1` preview mode, never opens or writes `.msl`, and rejects `-o`/`--force`/`--compact` |
| Create/overwrite package | Y | Partial | 11/12/26 | Immutable `.msl` v1 writer, verifier, fixtures, and atomic publication in 11; full `build`, `--force`, partial jobs with stable full-expansion IDs, and post-publication verification in 12; build UI in 26 |
| Append to existing package | N | Deferred | post-1.0 | Not required; rebuild is safer |
| 99,999-job cap | Y | Done | 10 | Hard cap `JOB019`; warning `JOB013` above 10k |
| Build progress/cancel/log | Y | Partial | 12/26 | Typed stage/progress event stream, throttled JSON-Lines events, Ctrl+C/SIGTERM cancellation with temp cleanup and package preservation in 12; build UI in 26 |
| Package inspection/verification | Y | Improvement | 11/12 | Read-only inspector/verifier, `PKG` diagnostics, `package-summary-v1`, fixtures in 11; CLI `inspect`/`verify` with `inspect-v1`/`verify-v1`, tolerant inspection, and exit/`CLI009` mapping in 12 |
| CLI `doctor` | N | Improvement | 12 | Seven ordered checks (runtime, SQLite, write permission, disk, symmetry catalog, project templates, optional solver), `doctor-v1`, warning/failure semantics, and a healthy CI run; the original had no environment diagnosis |
| Clean job folders (dry run) | Y | Partial | 12/28 | `clean --dry-run` lists `Job%05d` folders with age filtering and sizes, reports `clean-v1`, and never deletes; a no-dry-run request is refused with `CLI008`; deletion and `.msr` cleanup in 28 |
| `.msl` v1 format and fixtures | Y | Done | 11 | Schema, content-addressed payloads, canonical summary, verification, checked-in `msl-v1` fixture, and the pre-switch compatibility fixture. The Rust reader that consumes the fixture is Step 13. |
| Build history | N | Improvement | 26 | |

## 7. Solver

| Feature | Req | Status | Step | Notes |
| --- | :---: | --- | --- | --- |
| Package reader and solver foundation | Y | Planned | 13 | Read-only `.msl` reader, bounded decode, typed job/model, canonical signature re-encode, `mocassin-sim --dry-run`; no physics until 14–19 |
| Deterministic RNG and seed derivation | Y | Planned | 13 | ChaCha12, SHA-256 job/lattice derivation, Lemire bounded draws, 64-byte `RngStateV1`, golden vectors; replaces the PCG32 defects |
| MMC engine | Y | Planned | 17 | |
| KMC engine | Y | Planned | 16 | |
| Rejection KMC selection | Y | Planned | 16 | Uniform over directions |
| Pre-run/normalization | Y | Planned | 16 | Auto + fixed |
| Time-step calculation | Y | Planned | 16 | |
| Electric field | Y | Planned | 15/16 | |
| Pair/cluster/site energy | Y | Planned | 15 | |
| Fast exponential | Y | Planned | 15 | Optional |
| MMC equilibrium break | Y | Planned | 17 | |
| Abort conditions | Y | Planned | 16/17 | MCSP/time/rate/cancel |
| Global/mobile/static trackers | Y | Planned | 18 | |
| Jump histograms | Y | Planned | 18 | Attempt + accepted |
| Statistics/stdout | Y | Planned | 18 | Documented units |
| State save/resume | Y | Planned | 18 | MessagePack |
| `.mcs` compatibility (legacy) | N | Out of scope | — | ADR 0001 |
| PCG32 bit-exact trajectories | N | Out of scope | — | ADR 0005 |
| Statistical parity | Y | Planned | 29 | Reference systems |
| Extension ABI | Y | Planned | 19 | Versioned, opt-in |
| MMCFE | Y | Planned | 19/28 | New format |
| HPC/SLURM/PBS | Y | Planned | 19 | |
| MPI inside solver | N | Out of scope | — | Job-level parallelism |
| GPU acceleration | N | Deferred | post-1.0 | Not in original |

## 8. Results and analysis

| Feature | Req | Status | Step | Notes |
| --- | :---: | --- | --- | --- |
| `.mcs` reader | Y | Planned | 20 | MessagePack |
| `.msr` results store | Y | Planned | 20 | Separate from immutable `.msl` |
| Job query/metadata | Y | Planned | 20 | |
| Model context from package | Y | Planned | 20 | |
| Particle counts/meta | Y | Planned | 20 | |
| Displacement/MSD/diffusion | Y | Planned | 20 | Origin-based MSD; tracked-ensemble normalization |
| Mobility/conductivity | Y | Planned | 20 | Per-species + total; corrected Nernst–Einstein |
| Transition-resolved mobility | Y | Planned | 20 | |
| Correlation/Onsager | Y | Planned | 20 | Symmetric |
| Static tracking/flow | Y | Planned | 20 | |
| Coordination analysis | Y | Planned | 20 | Sorted |
| Jump statistics | Y | Planned | 20 | |
| MMCFE evaluation | Y | Planned | 20/28 | |
| Result collection | Y | Planned | 20 | Transactional `.msr` write; `.msl` untouched |
| CSV/JSON export | Y | Planned | 21 | |
| Parquet export | N | Deferred | post-1.0 | Optional |
| Plots/Arrhenius fits | N | Improvement | 21/28 | |
| Reports | N | Improvement | 21 | |
| Python `.mcs`/`.msr` reader | Y | Planned | 22 | Read-only MessagePack/SQLite readers |
| Python basic evaluations | Y | Planned | 22 | Counts, origin-based MSD, per-species mobility/conductivity, histograms |
| Python multi-file aggregation | Y | Planned | 22 | Filter/group/mean over `McsSet` |
| Python package (pytest) | Y | Planned | 22 | `mocassin-analysis`, Python 3.11–3.13 |
| Python API docs (mkdocstrings) | Y | Planned | 30 | `/api/python/` |
| Python package release | Y | Planned | 31 | Wheel/sdist or PyPI |
| Legacy `.mcs` read | N | Out of scope | — | ADR 0001 |

## 9. GUI

| Feature | Req | Status | Step | Notes |
| --- | :---: | --- | --- | --- |
| Cross-platform app | Y | Planned | 23A | Windows/Linux/macOS; Ubuntu automated, Windows/macOS platform gate and manual smoke checklist |
| Shell/tree/tabs/console/status | Y | Planned | 23A | Regions, document tabs, project switcher/explorer, Application console, populated status bar; Problems channel in 23B |
| Project lifecycle | Y | Planned | 23A | New/open/save/save-as/close/reload, recents, locks, read-only, external-change detection |
| Undo/redo | N | Improvement | 23B | Single per-project stack over `ProjectDocument`, bounded, save-point correct |
| Model validator/problems | Y | Partial | 23B/07 | Status indicator, Problems channel (including the load/migration diagnostics on open, conditional `Load` stage), filters, help anchors, revalidate, and navigable `ModelPath` targets in 23B; domain providers and full stats panels in 24/25 |
| Command palette and shortcut cheat sheet | N | Improvement | 23A | Searchable command catalog with platform gestures |
| Application settings and logging | N | Improvement | 23A/23B | Versioned `mocassin.settings` v1 with migration and retention in 23A; `autosave`/`session` additive fields in 23B; structured rolling logs replace the original XML settings file |
| Crash recovery dialog | N | Improvement | 23A/23B | Global exception boundary, log entry, and "open log folder" in 23A; recovery snapshot and the recovery action in 23B |
| Application diagnostics | N | Improvement | 23A | App-side environment report (distinct from CLI `doctor`) |
| Particle/set editor | Y | Planned | 24 | |
| Structure editor | Y | Planned | 24 | |
| Transition editor | Y | Planned | 24 | |
| Energy editor | Y | Planned | 25 | |
| Lattice/doping editor | Y | Planned | 25 | |
| Simulation editor | Y | Planned | 25 | |
| Parameterization editor | Y | Planned | 26 | |
| Job template editor | Y | Planned | 26 | |
| Build/deploy UI | Y | Planned | 26 | |
| 3D viewer | Y | Planned | 27 | |
| 3D picking/selection | N | Improvement | 27 | Original had none |
| 3D style persistence | Y | Planned | 27 | |
| 3D advanced effects (SSAO/shadows) | N | Deferred | post-1.0 | Quality subset first |
| Run monitoring | N | Improvement | 28 | |
| Analysis dashboard/charts | N | Improvement | 28 | |
| MMCFE UI | Y | Planned | 28 | |
| Lattice recycle tool | Y | Planned | 28 | |
| Remote deploy | N | Deferred | post-1.0 | Local + HPC scripts first |
| Accessibility/high contrast | N | Improvement | 23A | |
| Numeric expression input | Y | Planned | 24/25 | |
| Localization | N | Deferred | post-1.0 | Invariant first; resources ready |

## 10. Platform, packaging, and quality

| Feature | Req | Status | Step | Notes |
| --- | :---: | --- | --- | --- |
| .NET 11 | Y | Implemented | 00/11 | Retargeted by amendment 1 |
| Avalonia cross-platform | Y | Planned | 23A | |
| Rust solver | Y | Planned | 13 | Workspace, read-only package reader, ChaCha12 RNG, and `mocassin-sim --dry-run` land in 13; engines and state in 14–19 |
| Windows support | Y | Planned | 31 | |
| Linux support | Y | Planned | 31 | |
| macOS support | Y | Planned | 31 | |
| 64-bit x86_64 | Y | Planned | 00 | |
| arm64 | N | Improvement | 31 | Target for Apple Silicon/Linux |
| Windows MSI installer | N | Replaced | 31 | Archives/packages |
| NuGet evaluation package | Y | Planned | 20/31 | Public API |
| CI on three OSes | Y | Partial | 00/07 | Ubuntu automatic on `main`; Windows and macOS manual until platform-specific code appears |
| Unit/property/contract tests | Y | Implemented | 03–07 | Suite green; Step 29 adds statistical parity and fuzzing |
| Statistical parity suite | Y | Planned | 29 | |
| Performance benchmarks | Y | Planned | 29 | |
| Fuzzing | Y | Planned | 13/29 | Step 13 adds `mocassin-msl` scan/decode/package targets and a time-boxed run; Step 29 extends to `.mcs`/extension configs and the nightly job |
| Docs skeleton + build/CI | Y | Implemented | 00/30 | Zensical, navigation, build scripts, and planning exclusion; Step 30 completes content |
| Standalone docs completion (Zensical) | Y | Partial | 30 | Site builds strictly; completion, audit, and publication in 30 |
| User guide + physics | Y | Partial | 03–07/30 | Model-definition pages exist; Step 30 completes cross-cutting content |
| Developer architecture guide | Y | Partial | 00–07/30 | Architecture, compilation pipeline, testing, and workflow pages exist; Step 30 completes |
| Tutorials | Y | Planned | 30 | Ceria, BaZrO3, vehicle, HPC, analysis |
| CLI + format reference | Y | Partial | 07/12/30 | `validate`, diagnostics, and project references in 07; the full CLI command/option/report/event/exit-code reference and troubleshooting entries in 12; formats and documentation completion in 30 |
| Troubleshooting + FAQ | Y | Planned | 30 | |
| Performance + HPC guide | Y | Planned | 30 | |
| Contributing + dev workflow | Y | Implemented | 00 | Branches, review gates, local gates, and evidence playbook |
| Migration + clean-break guide | Y | Planned | 30 | |
| Glossary, notation, units | Y | Planned | 30 | |
| Examples + publications | Y | Planned | 30 | |
| .NET API reference (DocFX) | Y | Partial | 00/30 | Generated `/api/dotnet/`; Step 30 finalizes coverage and publication |
| Rust API reference (rustdoc) | Y | Partial | 00/13/30 | Skeleton crates only; the public API grows from Step 13; Step 30 finalizes coverage and publication |
| Python API reference (mkdocstrings) | Y | Planned | 22/30 | `/api/python/` |
| MIT + original GPL attribution/notices | Y | Implemented | 00 | `LICENSE`, `NOTICE.md`, and SPDX headers |
| Legacy file migration | N | Out of scope | — | Owner decision |

## 11. Known original defects fixed by the plan

| Defect | New handling | Step |
| --- | --- | --- |
| Corrupt space group 47 | Authoritative data + validation | 02 |
| Triclinic cell-vector formula missing `c` | Correct formula + regression test | 02 |
| Tolerance-based sorted sets/hash inconsistency | Exact rational geometry; no tolerant hashing | 02 |
| `Vector4I.P` order dependent on DB/tolerance | Canonical position order contract | 02 |
| `PcgRandom32` seed swap/NextBytes/NextDouble bugs | New ChaCha12 RNG + tests | 13 |
| Randomized `string.GetHashCode` model hash | SHA-256 canonical model hash | 08/09 |
| Whole-cache invalidation after one edit | Content-hash stage caches + invalidation tests | 08 |
| O(R²) inverse/dependent rule pairing | Hash-map rule lookup + deduplication | 08 |
| Ambiguous end-state/rule selection at runtime | Compile-time `CMP004` ambiguity error | 08 |
| Uncounted hidden/dependent rules | Explicit forward/inverse/dependent counts | 08 |
| Non-deterministic/shared lattice RNG | Per-job domain-separated seeds | 10 |
| Unstable lattice positions left uninitialized | Explicit void initialization | 10 |
| Optimizer mask inverted/double-applied | Correct single application + tests | 10 |
| Base lattice config discarded in configs | Explicit inheritance model | 10 |
| MMC frequency NaN | MMC rules carry no frequency; schema separates kinds | 11 |
| Raw `.mcs` with no header/checksum | MessagePack with `format`/`formatVersion`; SHA-256 in `.msr` | 18 |
| Unsafe unaligned/pinned `.mcs` reads | Managed MessagePack reader; no pinned buffers | 20 |
| Eager result blob loading | Metadata-first streaming queries | 20 |
| `A+B+C` unit-cell bug | `A*B*C` with tests | 20 |
| `D_eff` charge-factor ambiguity | Per-species and total Nernst–Einstein definitions | 20 |
| Asymmetric correlation matrix | Full symmetric matrix | 20 |
| MMC zero-energy break check aborts immediately | Guard below 1e-12 eV; normal behavior unchanged | 17 |
| Reflection/service-locator model layer | Immutable domain + explicit compiler | 01/08 |
| No undo/redo, no run monitor, no charts | Added | 23B/28 |
| Settings in `Documents/Mocassin` XML, hard-coded symmetry path | Versioned app-data settings, no hard-coded paths | 23A |
| `Console.WriteLine` logging, `MessageBox` dialogs | Structured rolling logs, in-shell dialogs, console channels | 23A |
| No dirty prompt, autosave, or crash recovery | Content-based dirty state, autosave, recovery flow | 01/23B |
| Detached library copy missed duplicates/deletes | One project per file; file-level duplicate/delete | 23A |
| Status bar present but empty | Populated status bar | 23A |
