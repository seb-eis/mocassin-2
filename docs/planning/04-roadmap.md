# 04 — Roadmap, Dependencies, and Milestones

## 1. Shape of the plan

The work is organised into seven phases and 32 steps. Each step is a shippable increment with a concrete
outcome, acceptance criteria, and tests. Steps are ordered by dependency, not by org chart. The critical path
runs through the model compiler and the solver; the GUI has a parallel track once the project format and
compiled model exist.

```mermaid
gantt
    title MOCASSIN reimplementation - illustrative 3-developer timeline
    dateFormat YYYY-MM-DD
    axisFormat %b %d
    excludes weekends
    todayMarker off

    section A. Foundations
    Step 00 Repository CI docs       :s00, 2026-11-09, 2w
    Step 01 Domain kernel project    :s01, after s00, 3w
    Step 02 Symmetry geometry        :s02, after s01, 4w
    M0 skeleton                      :milestone, m0, after s00, 0d
    M2 symmetry proven               :milestone, m2, after s02, 0d

    section B. Model and compiler
    Step 03 Particles structure      :s03, after s02, 3w
    Step 04 Transitions              :s04, after s03, 4w
    Step 05 Energy model             :s05, after s04, 3w
    Step 06 Lattice simulations      :s06, after s04, 2w
    Step 07 Validation diagnostics   :s07, after s05, 2w
    Step 08 Compilation pipeline     :s08, after s07, 5w
    Step 09 Parameterization         :s09, after s08, 2w
    Step 10 Job templates supercells :s10, after s09, 3w
    Step 11 Package msl writer       :s11, after s10, 3w
    Step 12 Build CLI                :s12, after s11, 2w
    M3 model authored                :milestone, m3, after s07, 0d
    M4 model compiled                :milestone, m4, after s08, 0d
    M5 first package                 :milestone, m5, after s12, 0d

    section D. Solver
    Step 13 Rust foundation reader   :s13, after s11, 3w
    Step 14 Context initialization   :s14, after s13, 3w
    Step 15 Energy engine            :s15, after s14, 4w
    Step 16 KMC engine               :s16, after s15, 5w
    Step 17 MMC engine               :s17, after s15, 3w
    Step 18 Tracking state writer    :s18, after s16, 4w
    Step 19 Solver CLI HPC extensions:s19, after s18, 3w
    M6 first KMC run                 :milestone, m6, after s16, 0d
    M7 first MMC run                 :milestone, m7, after s17, 0d

    section E. Results
    Step 20 mcs reader eval API      :s20, after s18, 3w
    Step 21 Analysis plots reports   :s21, after s20, 3w
    Step 22 Python analysis API      :s22, after s20, 3w
    M8 results loop                  :milestone, m8, after s20, 0d
    M9 Python API                    :milestone, m9, after s22, 0d

    section F. GUI
    Step 23 Avalonia shell           :s23, after s01, 4w
    Step 24 Model editors            :s24, after s04, 5w
    Step 25 Energy lattice sim UI    :s25, after s24, 4w
    Step 26 Parameterization build UI:s26, after s12, 4w
    Step 27 3D viewer                :s27, after s24, 5w
    Step 28 Run monitoring MMCFE UI  :s28, after s26, 4w
    M10 MVP GUI                      :milestone, m10, after s26, 0d
    M11 visualization                :milestone, m11, after s27, 0d
    M12 feature parity               :milestone, m12, after s28, 0d

    section G. Validation and release
    Step 29 Validation parity perf   :s29, after s22, 4w
    Step 30 Documentation completion :s30, after s28, 4w
    Step 31 Packaging release        :s31, after s30, 3w
    M13 documentation                :milestone, m13, after s30, 0d
    M14 release                      :milestone, m14, after s31, 0d
```

This is an **illustrative three-developer schedule**, not a fixed commitment. Durations are rounded from the
person-week estimates and the `after` links encode the main dependencies. A single developer should expect a
substantially longer calendar schedule; see the estimate table below.


Milestones mark the first step that demonstrates the outcome (for example, M5 after the first `.msl` package).
The detailed dependency table below remains authoritative; the Gantt is a communication view.


## 2. Phase overview

| Phase | Steps | Goal | Exit criterion |
| --- | --- | --- | --- |
| A. Foundations | 00–02 | Repo, domain kernel, format, symmetry | A minimal project round-trips and a space-group orbit expands correctly |
| B. Model definition | 03–07 | All user model domains and validation | The ceria model can be authored and validated headlessly |
| C. Compilation and build | 08–12 | Compiled model, templates, supercells, `.msl` | A valid `.msl` with N jobs is produced from a project |
| D. Solver | 13–19 | Rust KMC/MMC, tracking, state, CLI | A job runs to completion and writes a valid `.mcs` |
| E. Results | 20–22 | Reader, evaluation, analysis, Python API | Results can be evaluated from .NET and Python |
| F. GUI | 23–28 | Full Avalonia application | A user can complete the whole workflow in the GUI |
| G. Release and documentation | 29–31 | Parity, documentation, packaging | Release gates pass, the docs site builds, and artifacts ship |

## 3. Step estimates and dependencies

Estimates are in **person-weeks (pw)** for an experienced developer familiar with the domain. They include
tests, documentation, and review, not project management. Ranges reflect uncertainty.

| Step | Outcome | Depends on | Estimate |
| --- | --- | --- | --- |
| 00 | Repository, solution/workspace, CI, ADRs, docs skeleton, fixtures | — | 2–3 pw |
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
| 22 | Python `.mcs` reader, basic evaluations, pytest, mkdocstrings | 18, 20 | 2–3 pw |
| 23 | Avalonia shell, project lifecycle, undo/redo | 01 | 3–5 pw |
| 24 | Particle, structure, transition editors | 23, 03, 04 | 4–6 pw |
| 25 | Energy, lattice, simulation editors | 24, 05, 06 | 3–5 pw |
| 26 | Parameterization, job templates, build/deploy UI | 25, 09–12 | 4–6 pw |
| 27 | 3D viewer and visualization | 24, 03–05 | 4–6 pw |
| 28 | Run monitoring, charts, MMCFE UI | 26, 19–21 | 3–5 pw |
| 29 | Statistical parity, performance, fuzzing, hardening | 16–22 | 3–5 pw |
| 30 | Documentation completion: coverage, cross-cutting pages, APIs, tutorials, publish | 19, 21, 22, 28 | 3–5 pw |
| 31 | Packaging, docs artifact, Python package, samples, release | 22, 29, 30 | 2–4 pw |

**Total:** roughly 81–124 person-weeks (about 1.6–2.4 person-years). With three experienced developers working
in parallel on model/compiler, solver, UI, and documentation, the calendar estimate is **10–13 months** including
integration, documentation, and release hardening. A single developer should expect **20–26 months**.

## 4. Milestones

| Milestone | After step | Demonstrable result |
| --- | --- | --- |
| **M0 — Skeleton builds** | 00 | `dotnet build`, `cargo test`, strict Zensical build, CI green on three OSes |
| **M1 — Project round-trip** | 01 | Create/open/save a project; stable canonical JSON |
| **M2 — Symmetry proven** | 02 | All 230 groups/settings validate; P1 expansion matches brute force |
| **M3 — Model authored** | 07 | Ceria model built via API/CLI and validated with zero errors |
| **M4 — Model compiled** | 08 | Compiled model report: positions, pairs, clusters, rules, indices |
| **M5 — First package** | 12 | `.msl` with one job and a JSON manifest |
| **M6 — First KMC run** | 16 | Job completes; energy/acceptance counters look sane |
| **M7 — First MMC run** | 17 | MMC equilibrates a small system; energy trace converges |
| **M8 — Results loop** | 20 | `.mcs` read back; particle counts, MSD, conductivity evaluated |
| **M9 — Python API** | 22 | Read `.mcs` files and run basic multi-file evaluations from Python |
| **M10 — MVP GUI** | 26 | Create → validate → parameterize → build → run → collect in Avalonia |
| **M11 — Visualization** | 27 | 3D cell/sites/transitions/clusters/overlays with picking |
| **M12 — Feature parity** | 28 | Every parity-matrix item implemented or explicitly deferred |
| **M13 — Documentation** | 30 | Completed Zensical site with guides, physics, tutorials, APIs including Python |
| **M14 — Release** | 31 | Statistical parity, performance, packaged binaries, documentation artifact, Python package |

## 5. Parallel work plan

Assuming three core developers plus documentation ownership (shared or dedicated):

### Track A — Model and compilation (.NET)

Steps 00→01→02→03→04→05→06→07→08→09→10→11→12, then support 23–25.

### Track B — Solver (Rust)

Starts after step 11 freezes the package contract. Can begin earlier with a mock package generated from a
fixture. Steps 13→14→15→16→17→18→19, then 29.

### Track C — UI (Avalonia)

Starts after step 01 with the shell and project lifecycle (23). Consumes step 03/04/05/06 APIs as they land
(24–25), then 09–12 for 26, then 20–21 for 28. Step 27 (3D) starts after 24 and runs in parallel.

### Track D — Documentation

Step 00 creates the Zensical skeleton, navigation, build scripts, and Docs CI job. Each feature step writes
its pages as it lands and keeps the strict build green; step 30 completes cross-cutting content, integrates
the final DocFX/rustdoc/mkdocstrings outputs, and publishes after steps 19/21/22/28. Step 31 packages the
site with the release.

### Integration points

- **Contract review after step 11.** .NET and Rust teams sign off on the `.msl` schema and fixture.
- **Contract review after step 18.** .NET and Rust teams sign off on the `.mcs` MessagePack schema and fixture.
- **Weekly cross-track build.** A script builds the ceria project, runs one KMC and one MMC job, collects,
  evaluates, and compares against the previous week's observables.

## 6. Critical path and schedule risks

Critical path: **01 → 02 → 03 → 04 → 05 → 08 → 09 → 10 → 11 → 13 → 14 → 15 → 16 → 18 → 20 → 21 → 22 → 30 → 31**.
Parity (29) runs in parallel and feeds the release gate.

The highest-schedule-risk items are:

1. **Step 08 (compilation)** — symmetry reduction and rule generation are the most intricate algorithms.
   Mitigation: start with the ceria model and brute-force oracles; add vehicle/interstitialcy later.
2. **Step 16 (KMC)** — correctness of normalization, time step, and selection semantics. Mitigation: small
   analytic systems and per-cycle trace comparisons.
3. **Step 27 (3D)** — renderer scope. Mitigation: spike in week 1 of the step; fall back to a simpler
   instanced renderer if the backend integration is unstable.
4. **Step 29 (parity)** — statistical validation can reveal deep issues late. Mitigation: run parity checks
   continuously from step 16, not only at the end.
5. **Step 22 (Python API)** — schema drift between the Rust writer, .NET reader, and Python reader. Mitigation:
   one shared fixture set and cross-language tests from the first implementation.

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
