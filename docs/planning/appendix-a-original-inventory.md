# Appendix A — Original Solution Inventory and Mapping

This appendix records what exists in the archived `seb-eis/mocassin` repository and where each concern is
addressed in the new architecture. It is a navigation aid, not a line-by-line port list.

## 1. Original repository at a glance

| Area | Original location | Size / notes |
| --- | --- | --- |
| Foundation libraries | `src/ModelBuilder/ICon.Framework.Shared`, `ICon.Framework.Mathematics` | ~11k LOC, netstandard2.0 |
| Symmetry | `src/ModelBuilder/ICon.Framework.Symmetry` | ~5.3k LOC + 274-setting SQLite DB |
| Model | `src/ModelBuilder/ICon.Model` | ~24k LOC, six domains + manager/cache framework |
| Translator | `src/ModelBuilder/ICon.Model.Translator` | ~12k LOC, EF Core `.msl`, native struct blobs |
| UI data | `src/ModelBuilder/Mocassin.UI.Xml` | ~8k LOC, SQLite + JSON project graph |
| Desktop GUI | `src/ModelBuilder/Mocassin.UI.GUI` | ~30k LOC, WPF/HelixToolkit/SharpDX |
| Evaluation | `src/ModelBuilder/Mocassin.Tools.Evaluation` | ~5.2k LOC, result API |
| UAccess | `src/ModelBuilder/Mocassin.Tools.UAccess` | ~0.8k LOC, unsafe `.mcs` reader |
| Installer | `src/ModelBuilder/Mocassin.UI.Installer` | WiX MSI, Windows-only |
| Solver | `src/McSolver` | C11, ~11k LOC excluding 212k-line SQLite amalgamation |
| HPC scripts | `src/McSolver/Scripts` | Python wrappers, SLURM/MPI, collector |
| Docs | `docs/` | User guide, API guide, build VM setup, example project |

## 2. Project mapping

| Original | New | Step | Notes |
| --- | --- | --- | --- |
| `Mocassin.Framework` | `Mocassin.Core` (selected utilities) | 01 | Only what is needed; no reflection pipelines, no Rx messaging, no EF/Core wrappers |
| `Mocassin.Mathematics` | `Mocassin.Symmetry` + `Mocassin.Core` | 01–02 | Exact geometry, units, no tolerance-based sorted sets |
| `Mocassin.Symmetry` | `Mocassin.Symmetry` | 02 | New embedded catalog, exact integer operations, fixed group 47 |
| `Mocassin.Model` | `Mocassin.Core` (immutable model) | 01–06 | Definition only; no mutable manager/cache/event graph |
| `Mocassin.Model.Translator` | `Mocassin.Compilation` + `Mocassin.Packages` | 08–11 | Pure compiler, new package format |
| `Mocassin.UI.Data` | `Mocassin.Core.Project` | 01 | New JSON project format; no SQLite/JSON.NET graph |
| `Mocassin.UI.GUI` | `Mocassin.App*` + `Mocassin.Rendering` | 22–27 | Avalonia, new MVVM stack and information architecture |
| `Mocassin.Tools.Evaluation` | `Mocassin.Results` | 20–21 | New streaming API and corrected formulas |
| `Mocassin.Tools.UAccess` | `Mocassin.Results` | 20 | Safe MessagePack reader; no pinned whole-file buffers |
| `Mocassin.UI.Installer` | packaging/release | 30 | Cross-platform archives/packages instead of MSI |
| `docs/userguide-md`, `docs/old-guide` | `docs/` Zensical site | 00/29 | Skeleton in Step 00; rewritten user guide, physics, tutorials, developer guide, reference completed in Step 29; legacy docs not copied |
| `Mocassin.Simulator` (C) | `mocassin-sim` (Rust) | 13–19 | Process-per-job, new package reader, statistical parity |
| `Mocassin.Utility` (C) | `mocassin` CLI | 12/19 | Fold useful utilities into the new CLIs |
| Python HPC scripts | `tools/hpc` + `mocassin collect` | 19–20 | Thin SLURM/PBS wrappers; validated collection |
| MMCFE extension | built-in extension / `mocassin-extension` | 19/27 | New ABI and log format; legacy `mmcfelog.db` not read |

## 3. Model domain inventory

### Particles

| Original feature | New step | Notes |
| --- | --- | --- |
| Particle (name, symbol, charge, vacancy) | 03 | Immutable record, typed IDs |
| Implicit void particle at index 0 | 03 | Reserved, not stored |
| Particle set (ordered, first = default) | 03 | Immutable, bitmask in compiler |
| 63 custom particle limit | 03 | Enforced |
| Drag-and-drop set editing | 23 | Avalonia drag/drop + keyboard |
| Mass symbol semantics | 04/08 | Used in rule conservation |

### Structure

| Original feature | New step | Notes |
| --- | --- | --- |
| Space group selection (274 settings) | 02/23 | New catalog, exact operations, fixed group 47 |
| Space-group operation list | 02/23 | Read-only details panel |
| Cell parameters with crystal-system locks | 02/03/23 | Corrected cell transform |
| Stable/unstable cell sites | 03 | Typed, validated |
| P1/Wyckoff expansion preview | 03/23 | Canonical order contract |
| Dummy positions | 03/23 | Visual only |
| Site overlap validation | 03 | Exact orbit comparison |
| Multi-project library | 22 | Replaced by workspace + one project per file |

### Transitions

| Original feature | New step | Notes |
| --- | --- | --- |
| State changes [D,A] | 04 | Typed |
| State-change groups | 04 | Mixed physical/charge rejected |
| Abstract chains with D/S connectors | 04 | Six patterns + custom |
| 2-site MMC | 04/08/17 | Forward + inverse rules |
| 3-site migration | 04/08/16 | Forward + inverse incl. chiral |
| 5-/7-site interstitialcy | 04/08 | Unidirectional |
| 5-/6-site vehicle | 04/08 | Unidirectional, static linkers |
| Charge transfer/polaron hop | 04/08 | Charge conservation |
| Reference path symmetry extension | 08 | Canonical path expansion |
| Rule generation/dependent rules | 08 | Hash-based, conservation-first |
| Transition preview | 23 | State matrices + rule count |

### Energy

| Original feature | New step | Notes |
| --- | --- | --- |
| Stable environment cutoff | 05 | |
| Hollow-sphere filters | 05/24 | Symmetric, validated |
| Unstable environments per site | 05/24 | |
| Auto-sampled pair interactions | 08 | Stable geometry IDs, chiral linking |
| Group interactions (up to 9 positions) | 05/08 | Center + 8 offsets |
| Site/defect energies | 05/24 | Also overridable in parameterization (improvement) |
| Pair/cluster energy tables | 08/11 | Dense arrays in compiled package |
| Permutation/cost limits | 05/07 | Preview and warnings |
| Chiral pair linking | 05/08/09 | Single energy table |

### Lattice and simulations

| Original feature | New step | Notes |
| --- | --- | --- |
| Implicit default building block | 06/10 | Derived |
| Occupation exchanges | 06 | Reusable pool |
| Ordered dopings + priority | 06/10 | Unique priority |
| Dependent/counter doping | 06/10 | Exact charge ratio |
| Doping truncation/charge compensation | 10 | Deterministic, reported |
| Uniform random dopant placement | 10 | Deterministic per-job seed |
| MMC/KMC simulation definitions | 06 | All original settings |
| Transition assignment | 06/24 | Kind-filtered |
| Execution flags | 06/16/18 | NoSaving/NoJumpLogging/UseFastExp/Deterministic |
| RNG seed | 06/10 | Optional; derived per job |

## 4. Build and parameterization inventory

| Original feature | New step | Notes |
| --- | --- | --- |
| Parameterization template creation | 09 | From compiled model, stable IDs |
| Pair/group energy editing | 09/25 | Virtualized grids, bulk edit |
| Attempt frequency editing | 09/25 | Default 1e13 Hz |
| Chiral mirroring | 09/25 | Single storage |
| Model hash compatibility | 09 | BLAKE3, stable across processes |
| Template migration/rebase | 09/25 | Diff + report; no silent reuse |
| Site energy overrides in template | 09 | New capability |
| Job collections/configurations | 10 | Inheritance, overrides |
| Job multiplier/duplicates | 10 | Deterministic seeds |
| Selection optimizers | 10 | Apply once, correctly |
| Lattice size/doping values | 10 | Preview + charge report |
| Build template (model+param+job set) | 10/12 | Explicit IDs |
| Create/append package builds | 11/25 | Create/overwrite; append is not a v1 feature |
| 99,999-job cap | 11 | Removed; warning at 10k |
| Build progress/cancel/log | 12/25 | Shared event stream |
| Metadata table | 11/25 | `job_metadata` view |

## 5. Solver inventory

| Original feature | New step | Notes |
| --- | --- | --- |
| MMC/KMC unified context | 14 | Rust data-oriented context |
| Rejection KMC | 16 | Preserved selection/normalization semantics |
| KMC pre-run + auto/fixed normalization | 16 | `K_auto`, `K_fix` |
| Electric field | 15/16 | Field factors and tests |
| MMC equilibrium break | 17 | Configurable window/tolerance |
| Pair/cluster/site energies | 15 | Dynamic S2 first, static later |
| Fast exp | 15 | Optional, bounded error |
| Global/mobile/static trackers | 18 | |
| Jump histograms | 18 | Attempt + accepted semantics |
| Statistics/stdout | 18 | Compatible spirit, documented units |
| `.mcs` state | 18 | New MessagePack format |
| Resume | 16/18 | Signature-checked |
| Extensions/MMCFE | 19 | New ABI, opt-in |
| HPC scripts | 19 | SLURM/PBS, batch runner |
| PCG32 bit-exact trajectories | — | Replaced by ChaCha12 statistical parity (ADR 0005) |

## 6. Evaluation inventory

| Original feature | New step | Notes |
| --- | --- | --- |
| `MslEvaluationContext` | 20 | `ResultPackage`, read-only SQLite |
| `JobContext` + `McsReader` | 20 | Safe MessagePack reader |
| Particle count/meta | 20 | |
| Global/mobile/static tracking | 20 | |
| Displacement/MSD/diffusion | 20 | Correct units |
| Ensemble/transition mobility, conductivity | 20 | Correct Nernst–Einstein |
| Correlation/Onsager | 20 | Symmetric matrix |
| Coordination analysis | 20 | Sorted shells |
| Jump statistics | 20 | Attempt vs accepted |
| MMCFE evaluation | 20/27 | New log format |
| `.dat` export | 21 | CSV/JSON/plots/reports |
| Scripting API | 20 | Public .NET API + CLI |

## 7. GUI inventory

| Original screen/feature | New step | Notes |
| --- | --- | --- |
| Main shell, tabs, consoles, status | 22 | Avalonia, dockable later |
| Project new/open/save/close | 22 | One project/file + workspace |
| Solution explorer | 22 | Tree + document tabs |
| Particle/set editor | 23 | Drag/drop + keyboard |
| Structure editor + P1 preview | 23 | |
| Transition editors | 23 | Visual chain builder |
| Energy editor | 24 | Radial filters, cluster preview |
| Lattice/doping editor | 24 | Doping sequence + preview |
| Simulation editor | 24 | KMC/MMC forms |
| Parameterization editor | 25 | Signature/rebase/completeness |
| Job template editor | 25 | Tree + preview |
| Build/deploy UI | 25 | Progress/cancel/summary/verify |
| 3D viewer | 26 | New cross-platform renderer |
| Model validator | 22/07 | Problems panel + navigation |
| MMCFE import tool | 27 | New log format |
| Lattice recycle tool | 27 | |
| Charts/analysis | 27/21 | New capability |
| Run monitoring | 27 | New capability |
| Undo/redo | 22 | New capability |
| Accessibility | 22 | New capability |

## 8. Intentional replacements and non-goals

| Original | New approach | Reason |
| --- | --- | --- |
| `.mocprj` SQLite + JSON.NET graph | Versioned JSON project | Clean break, stable IDs, no CLR types |
| `.msl` EF Core + native blobs | SQLite + MessagePack/Zstd | Cross-language, versioned, safe |
| `.mcs` raw buffer | MessagePack state document | Safe, versioned, bounded, documented; SHA-256 in `.msl` |
| PCG32 exact trajectories | ChaCha12 statistical parity | Portable, maintainable, validated by observables |
| WPF/SharpDX/Win64 | Avalonia/Silk.NET cross-platform | Required portability |
| MSI installer | archives/packages | Cross-platform release |
| Multiple projects per file | one project per file + workspace | Simpler, safer persistence |
| Local-only deploy | local + optional remote | Usability improvement |
| No undo/redo, no charts, no run monitor | added | Modern UX |
| Legacy file import | none | Owner decision (ADR 0001) |
