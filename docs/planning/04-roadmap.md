# 04 — Roadmap, Dependencies, and Milestones

## 1. Shape of the plan

The work is organised into seven phases and 32 steps. Each step is a shippable increment with a concrete
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
22 ◄── 21 ◄── 20 ◄────────────────────────────────────────────────────────────────────────────────────── 19

F GUI
23A ──► 23B ──► 24 ──► 25 ──► 26 ──► 27 ──► 28
                │           │
                └───────────┴────────────► (23A can start after 01/03–07/12; later steps consume
                                           08/09/10/11/20 as they land)

G Release and documentation
29 ──► 30 ──► 31
```

## 2. Phase overview

| Phase | Steps | Goal | Exit criterion |
| --- | --- | --- | --- |
| A. Foundations | 00–02 | Repo, domain kernel, format, symmetry | A minimal project round-trips and a space-group orbit expands correctly |
| B. Model definition | 03–07 | All user model domains and validation | The ceria model can be authored and validated headlessly |
| C. Compilation and build | 08–12 | Compiled model, templates, supercells, `.msl` | A valid `.msl` with N jobs is produced from a project |
| D. Solver | 13–19 | Rust KMC/MMC, tracking, state, CLI | A job runs to completion and writes a valid `.mcs` |
| E. Results | 20–22 | Reader, evaluation, analysis, Python API | Results can be evaluated from .NET and Python |
| F. GUI | 23A–28 | Full Avalonia application | A user can complete the whole workflow in the GUI |
| G. Release and documentation | 29–31 | Parity, documentation, packaging | Release gates pass, the docs site builds, and artifacts ship |

Each phase is also gated by the cross-cutting reviews in
[`reviews/cross-cutting/`](reviews/cross-cutting/README.md). Phase A was waived. Phase B used three separate
reviews: policies and planning, architecture and design status, and documentation status (`XC-01`–`XC-03`).
From Phase C, each phase gets one combined review: `XC-04` for Phase C (compilation and build), `XC-05` for
Phase D (solver), and so on. The next phase's first step design review may not start until the combined verdict
is accepted by the human owner, or the review is explicitly waived with a recorded reason. The architecture
and design section also explicitly checks original-design fidelity: the intended corrections, the ported
original strengths, and any regression.

## 3. Step estimates and dependencies

Estimates are in **person-weeks (pw)** for an experienced developer familiar with the domain. They include
tests, documentation, and review, not project management. Ranges reflect uncertainty.

The pw figures are **forecasts, not gates**. They exist to schedule work, compare alternatives, and size the
parallel tracks. A step is not split, deferred, or refused because a forecast exceeds a threshold, and no
approval depends on a pw number. Whether a step must be split is decided by the complexity class and split
triggers in §3.1.

The forecasts were calibrated for human capacity. Implementation is now largely agent-driven, so wall-clock
duration is not the binding constraint on a step. What makes a step hard to review and integrate is its
contract, dependency, and evidence surface, not its calendar length. The pw figures are retained for
comparability and planning, not as a sizing gate.

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
| 09 | Parameterization templates and signatures | 08 | 1–3 pw (owner-approved 3.4–4.4 pw single-step exception, 2026-11-14) |
| 10 | Job templates, inheritance, supercell/doping generation | 06, 08, 09 | 3–4 pw |
| 11 | Compiled package encoding and `.msl` v1 writer | 08–10 | 3–4 pw |
| 12 | Build orchestration and `mocassin` CLI | 10, 11 | 1–3 pw (owner-approved 3.0–4.0 pw single-step exception, 2026-11-16; owner-approved 2.75–3.75 pw on 2026-11-17 after the `System.CommandLine` decision) |
| 13 | Rust workspace, `mocassin-sim`, `.msl` reader, job model | 11 | 2–4 pw (owner-approved 4.5 pw single-step exception, 2026-11-18) |
| 14 | Simulation context initialization | 13 + `step-11.1` | 2–4 pw |
| 15 | Energy engine and field terms | 14 | 3–5 pw |
| 16 | KMC engine, pre-run, normalization, time | 15 | 4–6 pw |
| 17 | MMC engine and equilibrium breaking | 15 | 2–4 pw |
| 18 | Trackers, histograms, statistics, `.mcs` writer | 16, 17 | 3–5 pw |
| 19 | Production CLI, HPC helpers, extension ABI | 18 | 2–4 pw |
| 20 | `.mcs` reader, `.msr` collection, evaluation API | 18, 11 | 2–4 pw |
| 21 | Analysis queries, plots, exports | 20 | 2–3 pw |
| 22 | Python `.mcs` reader, basic evaluations, pytest, mkdocstrings | 18, 20 | 2–3 pw |
| 23A | Avalonia workspace and project lifecycle | 01, 03–07, 12 | 2–3 pw forecast (non-gating). Complexity class **High**; owner acceptance at the 23A start gate — see the [23A page](steps/step-23a-workspace-and-project-lifecycle.md) §18 |
| 23B | Editing, diagnostics, and recovery | 23A | 1.5–2.5 pw forecast (non-gating). Complexity class **High**; owner acceptance at the 23B start gate — see the [23B page](steps/step-23b-editing-diagnostics-and-recovery.md) §16 |
| 24 | Particle, structure, transition editors | 23B, 03, 04 | 4–6 pw |
| 25 | Energy, lattice, simulation editors | 24, 05, 06 | 3–5 pw |
| 26 | Parameterization, job templates, build/deploy UI | 25, 09–12 | 4–6 pw |
| 27 | 3D viewer and visualization | 24, 03–05 | 4–6 pw |
| 28 | Run monitoring, charts, MMCFE UI | 26, 19–21 | 3–5 pw |
| 29 | Statistical parity, performance, fuzzing, hardening | 16–22 | 3–5 pw |
| 30 | Documentation completion: coverage, cross-cutting pages, APIs, tutorials, publish | 19, 21, 22, 28 | 3–5 pw |
| 31 | Packaging, docs artifact, Python package, samples, release | 22, 29, 30 | 2–4 pw |

**Total:** roughly 81–124 person-weeks of forecast effort (about 1.6–2.4 person-years). This is a planning
baseline for scheduling, staffing, and comparing alternatives, not a delivery commitment or a gate. With three
developers working in parallel on model/compiler, solver, UI, and documentation, the calendar forecast is
**10–13 months** including integration, documentation, and release hardening; as a single-developer forecast,
**20–26 months**. Agent-driven implementation can compress the calendar without changing the effort basis in
this table.

Complexity classes are recorded per step on the step page (§3.1), not in this table; the table stays a forecast
of effort.

### 3.1 Step sizing and split triggers

A step must be small enough that a fresh auditor can read its diff, tests, and evidence in one sitting, and
large enough to deliver one coherent outcome. Size is judged by a **complexity class**, not by elapsed time.
The pw figures in §3 are forecasts and never trigger a split.

#### Complexity dimensions

Classify the step at design time against seven dimensions:

| Dimension | Question |
| --- | --- |
| Contracts, formats, and public surfaces | Does the step change a versioned contract, format, schema, public API, or CLI surface? How many, and how widely are they consumed? |
| New dependencies | Does it add a library, toolchain, native component, or cross-language dependency? How hard would it be to remove later? |
| Cross-step interfaces | Does it define an interface that later steps consume? How many later steps depend on it, and how expensive is a late change? |
| Irreversibility | Does it make a decision that is hard to reverse after merge: a format, signature, published behaviour, or scientific convention? |
| Test-matrix breadth and failure modes | How many test layers are required (unit, property, golden, contract, cross-language, statistical, performance)? How many distinct failure modes and platform paths? |
| Independent acceptance stories | How many outcomes could be demonstrated, approved, or rejected separately by the owner? |
| Evidence size for a fresh auditor | How much diff, fixture, benchmark, and test output must one auditor hold at once to verify the step? |

#### Classes

| Class | Meaning |
| --- | --- |
| **Low** | One narrow surface; no versioned contract, format, or public-API change; no new dependency, or a contained dev-only one; no new cross-step interface; easily reversed; one or two test layers and few failure modes; one acceptance story; a small audit surface. |
| **Medium** | At most one versioned contract, format, or public-surface change, or one contained new dependency, or one cross-step interface with few consumers; reversible with a recorded decision; two or three test layers; still one acceptance story; an audit a fresh reader can complete in one sitting. |
| **High** | Any of: a format or signature change; a new dependency with lock-in or platform impact; a cross-step interface that several later steps consume; an irreversible decision; a broad test matrix including cross-language, statistical, or performance layers; more than one independent acceptance story; an evidence set too large for one audit sitting. |

The class is the **highest** dimension, not an average. One irreversible format change makes the step High even
if every other dimension is small.

#### Split triggers

Split a step before implementation when any of these holds:

- the step is **High** complexity, unless the owner accepts it as one unit (see below);
- it has **more than one independent acceptance story** — distinct outcomes that could be delivered, approved,
  or rejected separately (several acceptance criteria inside one story do not count as separate stories);
- it mixes risks, for example low-risk documentation or tooling with high-risk format, security, or solver work;
- it has more than roughly **6–8 features**, or features with unrelated outcomes;
- a fresh auditor could not read the diff, tests, and evidence in one sitting.

If a step is split, update the step index, the dependency graph, the §3 forecasts, and any affected milestone
before the design review freezes. Do not split during the audit.

#### Recording the class

Record the class and a one-paragraph justification on the step page at design time, before the design review.
The paragraph names the dimensions that drive the class, states the split decision, and for a High step gives
either the owner acceptance or the split plan. If the class changes during design — a new format, dependency, or
cross-step interface appears — re-record it before the design freezes. The step page is the source of record;
§3's table stays a forecast and does not carry the class.

#### Owner acceptance of a High step

A High-complexity step may proceed as one unit only with an explicit owner decision, recorded on the step page
and in this section. The request states the class, the dimensions that make it High, why a split would cost more
than it saves (for example, it would duplicate fixtures, separate an API from its acceptance test, or reopen a
frozen phase boundary), and the guard that stops the step if the risk materialises. This is an acceptance of a
complexity class, not a waiver of a time estimate.

#### Recorded split — Step 23 (2026-11-20)

The owner split **Step 23** into **23A** (workspace and project lifecycle) and **23B** (editing, diagnostics, and
recovery) under the complexity rule. The unsplit step was class **High** on four dimensions (cross-step
interfaces, test-matrix breadth, three independent acceptance stories, and an evidence set too large for one
audit sitting); the boundary after the workspace/lifecycle features is dependency-clean, so the split reduces the
audit surface without stranding either half.

- **23A** is class **High** on cross-step interfaces and test-matrix breadth; its contracts, dependencies,
  irreversibility, acceptance-story count, and evidence size are Medium. It defines the interface surface that
  23B and Steps 24–28 consume.
- **23B** is class **High** on cross-step interfaces and test-matrix breadth; its contracts, dependencies,
  irreversibility, acceptance-story count, and evidence size are Medium. It defines the edit/validation
  interfaces that Steps 24–27 consume.
- **Neither half is Low or Medium**: the split reduced the evidence size and the acceptance-story count, not the
  interface or test-matrix breadth. Under this section, each half therefore needs **explicit owner acceptance as
  a High unit at its own start gate**. The acceptance is recorded on each step page (§18 for 23A, §16 for 23B)
  when it is given; no time waiver is involved.
- A further split is not proposed. Host/workspace versus settings/lifecycle would leave either a shell that
  cannot open a project or settings/lifecycle code without a host to run in; editing versus recovery would leave
  autosave without a dirty state or validation without an editing surface.
- The same change updated the step index, §3 forecasts, the dependency graph (§2/§3), and **Step 24's dependency
  to 23B**; Steps 25–28 keep their existing chains, and milestone M10 (after 26) is unaffected.

#### Recorded class — Step 14 (design freeze 2026-11-20)

Step 14 (**Simulation context initialization**) is class **High** on three dimensions: cross-step interfaces
(Steps 15, 16, 17, 18, and 19 consume the context API, the neighbour-link orientation and inverse index, the
energy-vector layout, the cluster-code representation, the supercell pool space, and the jump-status link
identity); irreversibility (the owner/receiver delta rule, the pool key space and ordering, the cluster-geometry
rule, and the site/pair/cluster accounting are behavioural conventions that are expensive to change after
merge); and test-matrix breadth (unit, property, snapshot, real-package acceptance, allocation, benchmark,
memory). Its contracts/format surface is Medium (no persisted-format change, but a new public Rust API), it adds
no dependency, it has **one** independent acceptance story ("a validated job becomes a fully initialized
context"), and its evidence set is bounded (six features, one design page, one new fixture pair, one benchmark).
Per this section a High step needs either a split or explicit owner acceptance as one unit.

**No split is proposed.** The dependency-clean boundaries would separate the context skeleton from the
acceptance that proves it (leaving Step 15 a half-context or duplicating the fixtures and memory evidence) or
split "energy runtime" from "selection and jump status" (leaving a context with no selectable jumps, or a pool
with no energies); both halves would remain High on cross-step interfaces. The owner-acceptance request, the
case, and the API-freeze guard are recorded on the Step 14 page §4; acceptance is given at the Step 14 start
gate and recorded there and in [`05-open-decisions.md`](05-open-decisions.md). This note records the class
request; it does not itself approve it.

**Dependency note (2026-11-21).** Step 14 also depends on `step-11.1`, the implementation of
[`step-11-amend-2`](amendments/step-11-amend-2-cluster-code-expansion.md) (the per-table cluster-code expansion
map and the `ClusterTable::lookup` reader/runtime contract). The design contract is approved and merged
(`2f3a6fe`, design freeze `bf273ff`); review round 3's F-29 closure is a Step 14 gate, not a `step-11.1`
acceptance criterion. The Step 14 start gate is expected after `step-11.1` is merged and tagged; feature 1 may
start earlier, but feature 2's cluster work must not.

#### Historical exceptions (previous time-based rule)

The three exceptions below were approved under the previous **four-week** time trigger. They are historical
records: their pw figures, split rationales, and guard conditions are unchanged, and their step pages,
design-review records, and audit records are not rewritten or re-reviewed. This section does not re-open them,
and Step 13 continues in progress under its 2026-11-18 decision. The complexity rule applies to step designs
frozen on or after 2026-11-20; a step approved under the time rule keeps its recorded decision, including any
guard and its already-approved status.

**Recorded exception — Step 09 (2026-11-14).** The human owner approved Step 09 as a single step at an honest
3.4–4.4 person-week estimate. The upper bound exceeds the four-week trigger. The 09A/09B split was considered
and rejected because the seven features form one coherent template/apply/rebase contract and a split would add
a roadmap step and change the Step 09–12 dependency chain. This exception applies only to Step 09; a later step
that exceeds the trigger must split or record a new owner-approved exception. The Step 09 row above and the
Step 09 page record the same estimate and exception.

**Recorded exception — Step 12 (2026-11-16; revised and re-approved 2026-11-17).** The human owner approved
Step 12 as a single step at a 3.0–4.0 person-week estimate with seven features and a 2.25 pw contingency. The
upper bound is at, not beyond, the four-week trigger; the step grew from the roadmap's 1–3 pw because Step 12
now owns the no-write preview report from Step 10, the read-only inspector/verifier CLI from Step 11, seven
report contracts, and environment diagnosis. On 2026-11-17 the parser decision removed the hand-written
parser, help writer, and completion writer and replaced them with `System.CommandLine` 3.0.0 adoption; the
owner approved the revised 2.75–3.75 pw estimate with the same single-step exception, seven features, no
split, and contingency. The 12A (CLI shell + build) / 12B (auxiliary commands + docs) split was considered and
rejected because it would leave a CLI without `inspect`, `verify`, `doctor`, or `clean --dry-run`, add a
roadmap step, and change the Phase C closeout. If features 1–3 exceed 2.25 pw, the step stops and asks the
owner to re-open the split before feature 4. This exception applies only to Step 12; the Step 12 row above and
the Step 12 page record the same estimate and exception.

**Recorded exception — Step 13 (2026-11-18).** The human owner approved Step 13 as a single step at a
**4.5 person-week** estimate: 3.5 pw base implementation plus 1.0 pw risk contingency. The upper bound exceeds
the four-week trigger. The 13A/13B split was considered and rejected: it would separate the bounded reader
from its CLI acceptance and the populated/MMC fixture work, duplicate fixture and closeout effort, add a
roadmap step, and change the Phase D closeout; the seven features form one coherent reader/RNG/CLI contract.
The exception is single-step only. Guard: if features 1–3 exceed 2.0 pw, the step agent stops and reports to
the orchestrator before feature 4. The Step 13 row above and the Step 13 page record the same estimate and
exception; `05-open-decisions.md` records the resolved sizing and the rejected split.

## 4. Milestones

| Milestone | Tag | After step | Demonstrable result |
| --- | --- | --- | --- |
| **M0 — Skeleton builds** | `milestone-m0` | 00 | `dotnet build`, `cargo test`, strict Zensical build, CI green on three OSes |
| **M1 — Project round-trip** | `milestone-m1` | 01 | Create/open/save a project; stable canonical JSON |
| **M2 — Symmetry proven** | `milestone-m2` | 02 | All 230 groups/settings validate; P1 expansion matches brute force |
| **M3 — Model authored** | `milestone-m3` | 07 | Ceria model built via API/CLI and validated with zero errors |
| **M4 — Model compiled** | `milestone-m4` | 08 | Compiled model report: positions, pairs, clusters, rules, indices |
| **M5 — First package** | `milestone-m5` | 12 | `.msl` with one job and a JSON manifest |
| **M6 — First KMC run** | `milestone-m6` | 16 | Job completes; energy/acceptance counters look sane |
| **M7 — First MMC run** | `milestone-m7` | 17 | MMC equilibrates a small system; energy trace converges |
| **M8 — Results loop** | `milestone-m8` | 20 | `.mcs` collected into `.msr`; particle counts, origin-based MSD, per-species conductivity evaluated |
| **M9 — Python API** | `milestone-m9` | 22 | Read `.mcs`/`.msr` files and run basic multi-file evaluations from Python |
| **M10 — MVP GUI** | `milestone-m10` | 26 | Create → validate → parameterize → build → run → collect in Avalonia |
| **M11 — Visualization** | `milestone-m11` | 27 | 3D cell/sites/transitions/clusters/overlays with picking |
| **M12 — Feature parity** | `milestone-m12` | 28 | Every parity-matrix item implemented or explicitly deferred |
| **M13 — Documentation** | `milestone-m13` | 30 | Completed Zensical site with guides, physics, tutorials, APIs including Python |
| **M14 — Release** | `milestone-m14` | 31 | Statistical parity, performance, packaged binaries, documentation artifact, Python package |

Tags in the table are created on the commit that satisfies the milestone, after the milestone is accepted.
See [`AGENTS.md`](../../AGENTS.md) section 3.7 for tag rules.

M0–M3 were backfilled on 2026-11-12 on the existing step merge commits (`milestone-m0` → `47a9a90`,
`milestone-m1` → `7b4a994`, `milestone-m2` → `dbf5691`, `milestone-m3` → `3ed558a`) after the Phase B
checkpoint identified the missing tags. M4 and M5 were backfilled on 2026-11-18 on the Step 08 and Step 12
merge commits (`milestone-m4` → `f91fc63`, `milestone-m5` → `ec2e04a`). Later milestones are tagged when they
are reached.

## 5. Parallel work plan

Assuming three core developers plus documentation ownership (shared or dedicated):

### Track A — Model and compilation (.NET)

Steps 00→01→02→03→04→05→06→07→08→09→10→11→12, then support 23–25.

### Track B — Solver (Rust)

Starts after step 11 freezes the package contract. Can begin earlier with a checked-in fixture package
produced by the real writer. Steps 13→14→15→16→17→18→19, then 29.

### Track C — UI (Avalonia)

Starts after steps 01, 03–07, and 12 with the workspace and project lifecycle (**23A**), then the editing,
diagnostics, and recovery half (**23B**); the owner started this track in parallel with the solver track
(Step 13) on 2026-11-20 and split Step 23 into 23A/23B on the same date (see §3.1). 23A consumes only the merged
library surfaces of 01/03–07/12 and defines the registration seams; 23B fills them and adds the edit/validation
surface; neither assumes Steps 13–22 exist. Steps 24–25 consume the model APIs as they land, then 09–12 for 26,
then 20–21 for 28. Step 27 (3D) starts after 24 and runs in parallel.

### Track D — Documentation

Step 00 creates the Zensical skeleton, navigation, build scripts, and Docs CI job. Each feature step writes
its pages as it lands and keeps the strict build green; step 30 completes cross-cutting content, integrates
the final DocFX/rustdoc/mkdocstrings outputs, and publishes after steps 19/21/22/28. Step 31 packages the
site with the release.

### Integration points

- **Contract review after step 11.** .NET and Rust owners sign off on the immutable `.msl` schema and fixture.
- **Contract review after step 18.** .NET and Rust owners sign off on the `.mcs` MessagePack schema and fixture;
  Python joins the review when the analysis API lands (step 22).
- **Contract review after step 20.** .NET, Rust, and Python owners sign off on the `.msr` schema, package
  binding, and collection lifecycle.
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
