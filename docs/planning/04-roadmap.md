# 04 — Roadmap, Dependencies, and Milestones

## 1. Shape of the plan

The work is organised into seven phases and 30 steps. Each step is a shippable increment with a concrete
outcome, acceptance criteria, and tests. Steps are ordered by dependency, not by org chart. The critical path
runs through the model compiler and the solver; the GUI has a parallel track once the project format and
compiled model exist.

```text
A Foundations        B Model              C Compilation          D Solver
00 ──► 01 ──► 02 ──► 03 ──► 04 ──► 05 ──► 06 ──► 07 ──► 08 ──► 09 ──► 10 ──► 11 ──► 12
                                                             │                       │
                                                             └──────────► 13 ──► 14 ──► 15 ──► 16 ──► 17 ──► 18
                                                                                                         │
E Results                                                                                                ▼
21 ◄── 20 ◄────────────────────────────────────────────────────────────────────────────────────────────── 19

F GUI
22 ──► 23 ──► 24 ──► 25 ──► 26 ──► 27
       │           │
       └───────────┴────────────► (can start after 01, consume 08/09/10/11/20 as they land)

G Release
28 ──► 29
```

## 2. Phase overview

| Phase | Steps | Goal | Exit criterion |
| --- | --- | --- | --- |
| A. Foundations | 00–02 | Repo, domain kernel, format, symmetry | A minimal project round-trips and a space-group orbit expands correctly |
| B. Model definition | 03–07 | All user model domains and validation | The ceria model can be authored and validated headlessly |
| C. Compilation and build | 08–12 | Compiled model, templates, supercells, `.msl` | A valid `.msl` with N jobs is produced from a project |
| D. Solver | 13–19 | Rust KMC/MMC, tracking, state, CLI | A job runs to completion and writes a valid `.mcs` |
| E. Results | 20–21 | Reader, evaluation, analysis | Results can be evaluated and exported |
| F. GUI | 22–27 | Full Avalonia application | A user can complete the whole workflow in the GUI |
| G. Release | 28–29 | Parity, performance, packaging | Release gates pass on all platforms |

## 3. Step estimates and dependencies

Estimates are in **person-weeks (pw)** for an experienced developer familiar with the domain. They include
tests, documentation, and review, not project management. Ranges reflect uncertainty.

| Step | Outcome | Depends on | Estimate |
| --- | --- | --- | --- |
| 00 | Repository, solution/workspace, CI, ADRs, fixtures | — | 1–2 pw |
| 01 | Domain kernel, IDs, diagnostics, project JSON v1 | 00 | 2–4 pw |
| 02 | Space groups, geometry, coordinate encoding, orbit reduction | 01 | 3–5 pw |
| 03 | Particle and structure models | 02 | 2–4 pw |
| 04 | Transition model | 03 | 3–5 pw |
| 05 | Energy model | 03, 04 | 2–4 pw |
| 06 | Lattice and simulation models | 03, 04 | 1–3 pw |
| 07 | Validation, diagnostics, model statistics | 03–06 | 2–3 pw |
| 08 | Compilation pipeline (P1, environments, interactions, rules) | 02–07 | 4–6 pw |
| 09 | Parameterization templates and signatures | 08 | 1–3 pw |
| 10 | Job templates, inheritance, supercell/doping generation | 06, 08, 09 | 3–4 pw |
| 11 | Compiled package encoding and `.msl` v1 writer | 08–10 | 3–4 pw |
| 12 | Build orchestration and `mocassin` CLI | 10, 11 | 1–3 pw |
| 13 | Rust workspace, `mocassin-sim`, `.msl` reader, job model | 11 | 2–4 pw |
| 14 | Simulation context initialization | 13 | 2–4 pw |
| 15 | Energy engine and field terms | 14 | 3–5 pw |
| 16 | KMC engine, pre-run, normalization, time | 15 | 4–6 pw |
| 17 | MMC engine and equilibrium breaking | 15 | 2–4 pw |
| 18 | Trackers, histograms, statistics, `.mcs` writer | 16, 17 | 3–5 pw |
| 19 | Production CLI, HPC helpers, extension ABI | 18 | 2–4 pw |
| 20 | `.mcs` reader and evaluation API | 18, 11 | 2–4 pw |
| 21 | Analysis queries, plots, exports | 20 | 2–3 pw |
| 22 | Avalonia shell, project lifecycle, undo/redo | 01 | 3–5 pw |
| 23 | Particle, structure, transition editors | 22, 03, 04 | 4–6 pw |
| 24 | Energy, lattice, simulation editors | 23, 05, 06 | 3–5 pw |
| 25 | Parameterization, job templates, build/deploy UI | 24, 09–12 | 4–6 pw |
| 26 | 3D viewer and visualization | 23, 03–05 | 4–6 pw |
| 27 | Run monitoring, charts, MMCFE UI | 25, 19–21 | 3–5 pw |
| 28 | Statistical parity, performance, fuzzing, hardening | 16–21 | 3–5 pw |
| 29 | Packaging, docs, samples, release | 28 | 2–4 pw |

**Total:** roughly 75–115 person-weeks (about 1.5–2.2 person-years). With three experienced developers working
in parallel on model/compiler, solver, and UI, the calendar estimate is **9–12 months** including integration
and release hardening. A single developer should expect **18–24 months**.

## 4. Milestones

| Milestone | After step | Demonstrable result |
| --- | --- | --- |
| **M0 — Skeleton builds** | 00 | `dotnet build`, `cargo test`, CI green on three OSes |
| **M1 — Project round-trip** | 01 | Create/open/save a project; stable canonical JSON |
| **M2 — Symmetry proven** | 02 | All 230 groups/settings validate; P1 expansion matches brute force |
| **M3 — Model authored** | 07 | Ceria model built via API/CLI and validated with zero errors |
| **M4 — Model compiled** | 08 | Compiled model report: positions, pairs, clusters, rules, indices |
| **M5 — First package** | 12 | `.msl` with one job and a JSON manifest |
| **M6 — First KMC run** | 16 | Job completes; energy/acceptance counters look sane |
| **M7 — First MMC run** | 17 | MMC equilibrates a small system; energy trace converges |
| **M8 — Results loop** | 20 | `.mcs` read back; particle counts, MSD, conductivity evaluated |
| **M9 — MVP GUI** | 25 | Create → validate → parameterize → build → run → collect in Avalonia |
| **M10 — Visualization** | 26 | 3D cell/sites/transitions/clusters/overlays with picking |
| **M11 — Feature parity** | 27 | Every parity-matrix item implemented or explicitly deferred |
| **M12 — Release** | 29 | Statistical parity, performance, packaged binaries, docs |

## 5. Parallel work plan

Assuming three developers:

### Track A — Model and compilation (.NET)

Steps 00→01→02→03→04→05→06→07→08→09→10→11→12, then support 23–25.

### Track B — Solver (Rust)

Starts after step 11 freezes the package contract. Can begin earlier with a mock package generated from a
fixture. Steps 13→14→15→16→17→18→19, then 28.

### Track C — UI (Avalonia)

Starts after step 01 with the shell and project lifecycle (22). Consumes step 03/04/05/06 APIs as they land
(23–24), then 09–12 for 25, then 20–21 for 27. Step 26 (3D) starts after 23 and runs in parallel.

### Integration points

- **Contract review after step 11.** .NET and Rust teams sign off on the `.msl` schema and fixture.
- **Contract review after step 18.** .NET and Rust teams sign off on the `.mcs` MessagePack schema and fixture.
- **Weekly cross-track build.** A script builds the ceria project, runs one KMC and one MMC job, collects,
  evaluates, and compares against the previous week's observables.

## 6. Critical path and schedule risks

Critical path: **01 → 02 → 03 → 04 → 05 → 08 → 09 → 10 → 11 → 13 → 14 → 15 → 16 → 18 → 20 → 28 → 29**.

The highest-schedule-risk items are:

1. **Step 08 (compilation)** — symmetry reduction and rule generation are the most intricate algorithms.
   Mitigation: start with the ceria model and brute-force oracles; add vehicle/interstitialcy later.
2. **Step 16 (KMC)** — correctness of normalization, time step, and selection semantics. Mitigation: small
   analytic systems and per-cycle trace comparisons.
3. **Step 26 (3D)** — renderer scope. Mitigation: spike in week 1 of the step; fall back to a simpler
   instanced renderer if the backend integration is unstable.
4. **Step 28 (parity)** — statistical validation can reveal deep issues late. Mitigation: run parity checks
   continuously from step 16, not only at the end.

## 7. Definition of done for every step

A step is done only when:

- [ ] The public API/types are implemented and documented.
- [ ] Unit and property tests cover the new behaviour.
- [ ] Contract/golden fixtures are updated if the step touches a format.
- [ ] The step's acceptance criteria are demonstrated in a CLI command or test.
- [ ] No new warnings; analyzers and clippy are clean.
- [ ] The step page is updated with actual outcomes and any deviations.
- [ ] The parity matrix is updated.
- [ ] A reviewer has checked the scientific semantics, not only the code.

## 8. Change control

- Scope changes go through the parity matrix and an ADR if they affect architecture or formats.
- Estimates are revisited at each milestone; the roadmap is a living document.
- Deferring a feature requires an explicit "deferred" entry with a rationale and a target release.
- Adding a feature not present in the original requires an ADR and a note in the parity matrix under
  "improvements", so it is not mistaken for parity work.
