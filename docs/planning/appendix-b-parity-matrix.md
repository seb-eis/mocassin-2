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
| Create new project | Y | Planned | 01/23 | Blank, ceria, and barium-zirconate skeletons in 01/03; creation UI in 23 |
| Open/save/close project | Y | Planned | 01/23 | Atomic writes, lock, autosave store in 01; lifecycle UI in 23 |
| Recent files | N | Improvement | 23 | Workspace model |
| Multiple projects per file | N | Replaced | 23 | One project per file; workspace of open files |
| Duplicate/delete project in a library | N | Replaced | 23 | File-level duplicate/delete; no DB library |
| Autosave/recovery | N | Improvement | 01/23 | `.autosave` store in 01; timer/recovery prompt in 23 |
| Dirty tracking/unsaved prompt | Y | Planned | 01/23 | Document hash and dirty state in 01; prompt in 23 |
| Undo/redo | N | Improvement | 01/23 | Core stacks in 01; UI in 23 |
| Project schema versioning/migration | Y | Planned | 01 | v1 pipeline implemented; migration fixtures added per version |
| Stable project hash | Y | Improvement | 01/03/09 | Model/document SHA-256 in 01; structure hash in 03; compiled signature in 09 |
| Legacy `.mocprj` read/write | N | Out of scope | — | ADR 0001 |
| Project data viewer (JSON/XML) | N | Replaced | 23 | JSON view/debug export |
| Cross-domain validation pipeline | Y | Implemented | 07 | Load/model/cross-domain/feasibility; no post-validation mutation |
| Stable diagnostic catalog | Y | Implemented | 07 | Codes, owners, severities, paths, hints; docs-drift checked |
| Model statistics and feasibility | Y | Implemented | 07 | Counts, rule/memory estimates, buildability warnings |
| Headless `mocassin validate` | Y | Partial | 07/12 | `validate` implemented; Step 12 extends the command surface |
| `validation-v1` report | Y | Implemented | 07 | Sorted diagnostics, summary, optional statistics; schema checked |
| GUI diagnostic navigation | Y | Planned | 07/24/25 | `ModelPath` navigation and stats panels |

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
| Reverse rule generation (2/3-site) | Y | Planned | 08 | Auto-inverse for `Metropolis2`/`Migration3`; explicit reverse stays separate |
| No reverse for complex mechanisms | Y | Planned | 08 | `GeneratesReverse` validated in 04; no auto-inverse in 08 |
| Dependent/hidden rules | Y | Planned | 08 | Symmetry-equivalent start instances; deduplicated and counted |
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
| Chiral pair linking | Y | Partial | 05/08/09 | Shell chiral linking in 02; shared `EnergyId` preview in 05; compiled tables in 08; template binding in 09 |
| Group/cluster interactions (up to 9 positions) | Y | Partial | 05/08 | Model, geometry validation, cost preview, and samples in 05; canonical tables in 08 |
| Site/defect energies | Y | Partial | 05/25 | Model, validation, editing, and samples in 05; editor UI in 25 |
| Cost/permutation limits | Y | Implemented | 05/07 | Limits, diagnostics, preview, consolidated statistics, and docs |
| Cluster geometry editor/preview | Y | Partial | 05/25 | Preview API in 05; editor UI in 25 |
| Site energy matrix + bulk edit | Y | Improvement | 25 | |
| Default building block viewer | Y | Planned | 25 | |
| Occupation exchanges | Y | Partial | 06/25 | Model, validation, persistence, query, and editor API in 06; editor UI in 25 |
| Ordered dopings/priorities | Y | Partial | 06/10/25 | Unique priorities, deterministic order, persistence, and editing in 06; fraction application in 10; editor UI in 25 |
| Counter-doping/charge compensation | Y | Partial | 06/10/25 | Shape/sign validation and persistence in 06; integer ratios and applied counts in 10; editor UI in 25 |
| Doping truncation semantics | Y | Planned | 10 | Reported |
| Deterministic lattice generation | Y | Improvement | 10 | Fixes shared crypto RNG |
| Doping preview (requested vs applied) | Y | Improvement | 25 | |

## 5. Compilation pipeline

| Feature | Req | Status | Step | Notes |
| --- | :---: | --- | --- | --- |
| Pure compiled model API | Y | Done | 08 | Synchronous `IModelCompiler` with diagnostics, counts, signatures, cancellation |
| P1-extended dense compiled model | Y | Done | 08 | Canonical P indices, dense particle/site/table indices, stable IDs |
| Auto-sampled pair tables | Y | Done | 08 | Shares Step 05 sampling/reduction kernel; canonical shell order |
| Chiral pair linking in compiled tables | Y | Done | 08/09 | Shared `EnergyId`; one table/value per chiral pair |
| Cluster tables and occupation codes | Y | Done | 08 | Point-group canonicalization; 6-bit codes; expansion maps |
| Forward/inverse/dependent rules | Y | Partial | 08 | Auto-inverse only for `Metropolis2`/`Migration3`; dependent instances for symmetry-equivalent starts |
| Mass/charge conservation filtering | Y | Done | 08 | Rejected counts reported; charge-transfer symbols balanced |
| Rule ambiguity detection | Y | Done | 08 | Compile-time `CMP004`; original failed at runtime |
| Unreferenced transition warning | N | Done | 08 | `CMP009` |
| Simulation encoding / tracker definitions | Y | Done | 08/18 | Collections, directions, mappings, intent; Step 18 owns state |
| Compiled-model signature | Y | Done | 08/09 | Geometry/rules signature distinct from parameterization signature |
| Compilation diagnostics/report | Y | Done | 08 | `CMP` catalog, counts, timings, input hashes, cache statistics |
| Compilation stage cache | N | Done | 08 | Content-hash keys; per-compilation default, optional caller cache |
| Compiled-model JSON inspection | N | Done | 08/12 | `compiled-inspection-v1`; `mocassin compile` in 12 |
| Headless `mocassin compile` | Y | Planned | 12 | Step 08 library only; Step 12 owns the command |

## 6. Simulations, templates, and builds

| Feature | Req | Status | Step | Notes |
| --- | :---: | --- | --- | --- |
| MMC simulation definition | Y | Partial | 06/17/25 | Model, defaults, limits, persistence, query, and editor API in 06; solver in 17; editor UI in 25 |
| KMC simulation definition | Y | Partial | 06/16/25 | Model, defaults, limits, persistence, query, and editor API in 06; solver in 16; editor UI in 25 |
| All original simulation settings | Y | Partial | 06/25 | Settings, defaults, hard limits, and recommended bands in 06; editor UI in 25 |
| Transition assignment | Y | Partial | 06/25 | Authored-order, kind-filtered assignment and duplicate rejection in 06; multi-select UI in 25 |
| Parameterization template creation | Y | Planned | 09 | |
| Pair/group energy editing | Y | Planned | 09/26 | |
| Attempt frequency editing | Y | Planned | 09/26 | |
| Chiral linked editing | Y | Planned | 09/26 | Shared `EnergyId`; one value per chiral pair |
| Stable model signature | Y | Improvement | 08/09 | Compiled geometry/rules/encoding signature in 08; parameterization signature in 09 |
| Template rebase/migration | Y | Planned | 09/26 | Diff + report |
| Site energy overrides in template | N | Improvement | 09 | Original limitation removed |
| Job collections/configs | Y | Planned | 10 | |
| Base + config overrides | Y | Planned | 10/26 | Correct inheritance |
| Job duplicates/multiplier | Y | Planned | 10 | |
| Selection optimizers | Y | Planned | 10 | Applied once, correct mask |
| Lattice size + doping values | Y | Planned | 10/26 | |
| Build template selection | Y | Planned | 10/12/26 | |
| Create/overwrite package | Y | Planned | 11/26 | |
| Append to existing package | N | Deferred | post-1.0 | Not required; rebuild is safer |
| 99,999-job cap | N | Replaced | 11 | Warning at 10k; no hard cap |
| Build progress/cancel/log | Y | Planned | 12/26 | |
| Package inspection/verification | Y | Improvement | 11/12 | |
| Build history | N | Improvement | 26 | |

## 7. Solver

| Feature | Req | Status | Step | Notes |
| --- | :---: | --- | --- | --- |
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
| Cross-platform app | Y | Planned | 23 | Windows/Linux/macOS |
| Shell/tree/tabs/console/status | Y | Planned | 23 | |
| Project lifecycle | Y | Planned | 23 | |
| Undo/redo | N | Improvement | 23 | |
| Model validator/problems | Y | Partial | 23/07 | Navigable validation result and paths; UI in 23 |
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
| Accessibility/high contrast | N | Improvement | 23 | |
| Numeric expression input | Y | Planned | 24/25 | |
| Localization | N | Deferred | post-1.0 | Invariant first; resources ready |

## 10. Platform, packaging, and quality

| Feature | Req | Status | Step | Notes |
| --- | :---: | --- | --- | --- |
| .NET 10 | Y | Planned | 00 | |
| Avalonia cross-platform | Y | Planned | 23 | |
| Rust solver | Y | Planned | 13 | |
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
| Fuzzing | Y | Planned | 13/29 | |
| Docs skeleton + build/CI | Y | Implemented | 00/30 | Zensical, navigation, build scripts, and planning exclusion; Step 30 completes content |
| Standalone docs completion (Zensical) | Y | Partial | 30 | Site builds strictly; completion, audit, and publication in 30 |
| User guide + physics | Y | Partial | 03–07/30 | Model-definition pages exist; Step 30 completes cross-cutting content |
| Developer architecture guide | Y | Partial | 00–07/30 | Architecture, compilation pipeline, testing, and workflow pages exist; Step 30 completes |
| Tutorials | Y | Planned | 30 | Ceria, BaZrO3, vehicle, HPC, analysis |
| CLI + format reference | Y | Partial | 30/07 | `validate`, diagnostics, and project references in 07; full CLI/formats in 30 |
| Troubleshooting + FAQ | Y | Planned | 30 | |
| Performance + HPC guide | Y | Planned | 30 | |
| Contributing + dev workflow | Y | Implemented | 00 | Branches, review gates, local gates, and evidence playbook |
| Migration + clean-break guide | Y | Planned | 30 | |
| Glossary, notation, units | Y | Planned | 30 | |
| Examples + publications | Y | Planned | 30 | |
| .NET API reference (DocFX) | Y | Partial | 00/30 | Generated `/api/dotnet/`; Step 30 finalizes coverage and publication |
| Rust API reference (rustdoc) | Y | Partial | 00/13/30 | Skeleton crates only; the public API grows from Step 13 |
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
| No undo/redo, no run monitor, no charts | Added | 23/28 |
