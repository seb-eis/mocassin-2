# Appendix B — Parity Matrix

Status legend:

- **Planned** — part of the phased full-parity scope; not yet implemented.
- **Improvement** — new capability or deliberate fix beyond the original.
- **Replaced** — original feature is delivered through a different mechanism/UX.
- **Deferred** — out of the first parity release; target noted.
- **Out of scope** — explicitly excluded (ADR/decision).

`Req` = required for the first parity release.

## 1. Project, files, and lifecycle

| Feature | Req | Status | Step | Notes |
| --- | :---: | --- | --- | --- |
| Create new project | Y | Planned | 01/23 | Blank + ceria skeletons in 01; creation UI in 23 |
| Open/save/close project | Y | Planned | 01/23 | Atomic writes, lock, autosave store in 01; lifecycle UI in 23 |
| Recent files | N | Improvement | 23 | Workspace model |
| Multiple projects per file | N | Replaced | 23 | One project per file; workspace of open files |
| Duplicate/delete project in a library | N | Replaced | 23 | File-level duplicate/delete; no DB library |
| Autosave/recovery | N | Improvement | 01/23 | `.autosave` store in 01; timer/recovery prompt in 23 |
| Dirty tracking/unsaved prompt | Y | Planned | 01/23 | Document hash and dirty state in 01; prompt in 23 |
| Undo/redo | N | Improvement | 01/23 | Core stacks in 01; UI in 23 |
| Project schema versioning/migration | Y | Planned | 01 | v1 pipeline implemented; migration fixtures added per version |
| Stable project hash | Y | Improvement | 01/09 | Model and document SHA-256 in 01; compiled signature in 09 |
| Legacy `.mocprj` read/write | N | Out of scope | — | ADR 0001 |
| Project data viewer (JSON/XML) | N | Replaced | 23 | JSON view/debug export |

## 2. Particles and structure

| Feature | Req | Status | Step | Notes |
| --- | :---: | --- | --- | --- |
| Particle name/symbol/charge/vacancy | Y | Planned | 03 | Typed IDs |
| Implicit void particle | Y | Planned | 03 | Reserved index 0 |
| 63 custom particle limit | Y | Planned | 03 | Enforced |
| Particle sets with first-as-default | Y | Planned | 03 | |
| Drag-and-drop set membership | Y | Planned | 24 | + keyboard |
| Space group selection (274 settings) | Y | Planned | 02/24 | New catalog |
| Space-group operation display | Y | Planned | 24 | |
| Cell parameters + crystal-system locks | Y | Planned | 02/03/24 | Corrected transform |
| Stable/unstable sites | Y | Planned | 03 | |
| P1 expansion preview | Y | Planned | 03/24 | Canonical order |
| Site overlap validation | Y | Planned | 03 | Exact |
| Dummy position editor | Y | Planned | 24 | Visual only |
| Correct triclinic/rhombohedral cell vectors | Y | Improvement | 02 | Fixes original bug |
| Valid group 47 data | Y | Improvement | 02 | Fixes corrupt setting |
| Multiple site definitions per orbit | Y | Planned | 03 | With overlap diagnostics |

## 3. Transitions

| Feature | Req | Status | Step | Notes |
| --- | :---: | --- | --- | --- |
| State changes [D,A] | Y | Planned | 04 | |
| State-change groups | Y | Planned | 04 | Mixed physical/charge rejected |
| Abstract chains with D/S linkers | Y | Planned | 04 | Six patterns + custom |
| 2-site MMC | Y | Planned | 04/08/17 | |
| 3-site KMC migration | Y | Planned | 04/08/16 | |
| 5-site interstitialcy | Y | Planned | 04/08 | |
| 7-site interstitialcy | Y | Planned | 04/08 | |
| 5-site 2-species vehicle | Y | Planned | 04/08 | |
| 6-site 2-species vehicle | Y | Planned | 04/08 | |
| Charge transfer/polaron hop | Y | Planned | 04/08 | |
| Reverse rule generation (2/3-site) | Y | Planned | 08 | |
| No reverse for complex mechanisms | Y | Planned | 08 | Explicit warning |
| Dependent/hidden rules | Y | Planned | 08 | |
| Reference path editing/preview | Y | Planned | 24 | |
| Ambiguity detection | Y | Improvement | 08 | Compile-time error |
| Transition rule count preview | N | Improvement | 24 | |

## 4. Energy and lattice

| Feature | Req | Status | Step | Notes |
| --- | :---: | --- | --- | --- |
| Stable environment cutoff | Y | Planned | 05 | |
| Hollow-sphere filters | Y | Planned | 05/25 | |
| Per-unstable-site environments | Y | Planned | 05/25 | |
| Auto-sampled pair interactions | Y | Planned | 08 | |
| Chiral pair linking | Y | Planned | 05/08/09 | |
| Group/cluster interactions (up to 9 positions) | Y | Planned | 05/08 | |
| Site/defect energies | Y | Planned | 05 | |
| Cost/permutation limits | Y | Planned | 05/07 | |
| Cluster geometry editor/preview | Y | Planned | 25 | |
| Site energy matrix + bulk edit | Y | Improvement | 25 | |
| Default building block viewer | Y | Planned | 25 | |
| Occupation exchanges | Y | Planned | 06 | |
| Ordered dopings/priorities | Y | Planned | 06/10 | |
| Counter-doping/charge compensation | Y | Planned | 06/10 | |
| Doping truncation semantics | Y | Planned | 10 | Reported |
| Deterministic lattice generation | Y | Improvement | 10 | Fixes shared crypto RNG |
| Doping preview (requested vs applied) | Y | Improvement | 25 | |

## 5. Simulations, templates, and builds

| Feature | Req | Status | Step | Notes |
| --- | :---: | --- | --- | --- |
| MMC simulation definition | Y | Planned | 06 | |
| KMC simulation definition | Y | Planned | 06 | |
| All original simulation settings | Y | Planned | 06 | |
| Transition assignment | Y | Planned | 06/25 | Kind-filtered |
| Parameterization template creation | Y | Planned | 09 | |
| Pair/group energy editing | Y | Planned | 09/26 | |
| Attempt frequency editing | Y | Planned | 09/26 | |
| Chiral mirroring | Y | Planned | 09/26 | |
| Stable model signature | Y | Improvement | 09 | SHA-256 |
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

## 6. Solver

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

## 7. Results and analysis

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

## 8. GUI

| Feature | Req | Status | Step | Notes |
| --- | :---: | --- | --- | --- |
| Cross-platform app | Y | Planned | 23 | Windows/Linux/macOS |
| Shell/tree/tabs/console/status | Y | Planned | 23 | |
| Project lifecycle | Y | Planned | 23 | |
| Undo/redo | N | Improvement | 23 | |
| Model validator/problems | Y | Planned | 23/07 | Navigable |
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

## 9. Platform, packaging, and quality

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
| CI on three OSes | Y | Planned | 00 | |
| Unit/property/contract tests | Y | Planned | 03 | |
| Statistical parity suite | Y | Planned | 29 | |
| Performance benchmarks | Y | Planned | 29 | |
| Fuzzing | Y | Planned | 13/29 | |
| Docs skeleton + build/CI | Y | Planned | 00 | Zensical, nav, build scripts, planning excluded |
| Standalone docs completion (Zensical) | Y | Planned | 30 | Cross-cutting content, audit, publish |
| User guide + physics | Y | Planned | 30 | Required content |
| Developer architecture guide | Y | Planned | 30 | Code structure/design choices |
| Tutorials | Y | Planned | 30 | Ceria, BaZrO3, vehicle, HPC, analysis |
| CLI + format reference | Y | Planned | 30 | CLI, project/.msl/.msr/.mcs, job folder, diagnostics |
| Troubleshooting + FAQ | Y | Planned | 30 | |
| Performance + HPC guide | Y | Planned | 30 | |
| Contributing + dev workflow | Y | Planned | 30 | |
| Migration + clean-break guide | Y | Planned | 30 | |
| Glossary, notation, units | Y | Planned | 30 | |
| Examples + publications | Y | Planned | 30 | |
| .NET API reference (DocFX) | Y | Planned | 30 | `/api/dotnet/` |
| Rust API reference (rustdoc) | Y | Planned | 30 | `/api/rust/` |
| Python API reference (mkdocstrings) | Y | Planned | 30 | `/api/python/` |
| MIT + original GPL attribution/notices | Y | Planned | 00 | |
| Legacy file migration | N | Out of scope | — | Owner decision |

## 10. Known original defects fixed by the plan

| Defect | New handling | Step |
| --- | --- | --- |
| Corrupt space group 47 | Authoritative data + validation | 02 |
| Triclinic cell-vector formula missing `c` | Correct formula + regression test | 02 |
| Tolerance-based sorted sets/hash inconsistency | Exact rational geometry; no tolerant hashing | 02 |
| `Vector4I.P` order dependent on DB/tolerance | Canonical position order contract | 02 |
| `PcgRandom32` seed swap/NextBytes/NextDouble bugs | New ChaCha12 RNG + tests | 13 |
| Randomized `string.GetHashCode` model hash | SHA-256 canonical model hash | 09 |
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
