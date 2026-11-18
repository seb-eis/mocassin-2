# Supercell position limit: is 500,000 the right cap?

> This is a design scratch, not a decision. See [`README.md`](README.md).

| Field | Value |
| --- | --- |
| Status | Under discussion |
| Owner | Build/solver limits research (unassigned) |
| Created | 2026-11-18 |
| Last updated | 2026-11-18 |
| Target step | Step 10 (build), Step 13 (reader), Steps 14/15/18 (solver memory and state), Step 29 (parity and performance) |
| Related | Step 10 `JOB004`/`JOB014`; Step 13 `ReaderLimits`; [`02-data-contracts.md`](../02-data-contracts.md) solver read bounds; [ADR 0013](../adr/0013-format-compatibility-and-api-versioning.md); original `MocassinSimulationSettings.cs` |
| Summary | The 500,000-position hard cap is the original settings maximum, ported without a recorded derivation. This draft states what is verified about its provenance, proposes measurements that would tell us whether it is a useful guard, and lists options without choosing one. |

## 1. Status and scope

This draft is **non-normative**. It records research and an option set. It does not decide a cap, does not
change a cap, and does not authorize implementation.

- It owns no decision. If adopted, the decision belongs to a Step 10 page/contract update for the build limit,
  a Step 13 page/contract update for `ReaderLimits`, a Step 14/15/18 update for the solver memory budget, or an
  ADR if the package contract or compatibility promise changes.
- It is not a benchmark report. It contains no measured performance numbers for this implementation.
- It is not a parity deviation. Choosing an option that differs from the original would be a deliberate,
  recorded deviation under the normal step-page/ADR process, not a side effect of this draft.
- It does not touch the XC-04 checkpoint, the Step 13 review record, or any step/audit record.

The question is narrow: **is 500,000 still the right bound for this implementation, can it be raised
materially, and is the cap useful at all** — that is, does it protect against out-of-memory or pathological
builds, or is it an arbitrary legacy number that now rejects legitimate science?

## 2. Provenance and current behaviour

### 2.1 The original setting

The only occurrence of `500000` relevant to supercells in the read-only reference clone is the
simulation-settings constructor:

- `/workspace/mocassin-original/src/ModelBuilder/ICon.Model/ModelProject/Manager/Settings/Modules/MocassinSimulationSettings.cs:70`
  declares `ValueSetting<int> SupercellPositionCount`.
- Line 155 constructs it:
  `new ValueSetting<int>("Super-cell Position Count", 1, 100, 100000, 500000)`.
- `/workspace/mocassin-original/src/ModelBuilder/ICon.Model/ModelProject/Manager/Objects/ValueSetting.cs`
  defines the constructor order as `(displayName, minValue, lowerWarningLimit, upperWarningLimit, maxValue)`.

So the four bounds are:

| Bound | Value | Original semantics |
| --- | ---: | --- |
| Minimum | 1 | Values below are a restriction violation (`-2`). |
| Lower warning | 100 | Values below are a warning (`-1`). |
| Upper warning | 100,000 | Values above are a warning (`+1`). |
| Maximum | 500,000 | Values above are a restriction violation (`+2`). |

`ValueSetting.ParseValue` returns `0`, `±1`, or `±2`; the warning text itself names three possible reasons —
unphysical/unusual, performance, and memory. That is the closest the original comes to a rationale, and it is
generic boilerplate generated for every `ValueSetting`, not a statement about supercells.

The lower-warning value of 100 is odd for a supercell-position count and the port did not carry it over: this
repository has only `JOB004` (hard maximum) and `JOB014` (upper warning). A 1-position supercell is accepted
here without a warning. That is a small fidelity loss, not a correctness issue, but it belongs in the
provenance record.

### 2.2 What the original actually enforces

A repository-wide text search in the reference clone finds `SupercellPositionCount` only at the declaration,
the constructor, and the generated XML documentation. No code path consumes the value. `UnitCellsPerDirection`
(`1, 1, 100, 256`) has the same shape: declaration and constructor only.

Consequences:

- The 500,000 is a **declared settings maximum**, not an evidenced engine limit. In the public reference clone
  there is no code path that refuses a 600,000-position supercell.
- The consumer, if any, was likely in a UI or a settings editor outside the code exercised by the solver. The
  clone does contain `Mocassin.UI.GUI`, but a search there also finds no consumer. The clone may omit a shipped
  consumer, but neither the code nor the user guide evidences one.
- The two limits are independent settings. Read together as a joint bound they are inconsistent:
  `256³ = 16,777,216` unit cells, and `500,000 / 16,777,216 ≈ 0.03`, so no real unit cell can satisfy both
  maxima. The position cap always binds first. This is evidence that the numbers were entered as independent
  "reasonable" guards, not derived from a shared model.

No comment, commit message, or user-guide section in the reference clone derives 500,000. The available
history in the clone records the value only in a source-tree move commit; it does not preserve a rationale. The original user-guide pages reviewed describe supercell choice qualitatively ("a supercell has a
finite number of atoms") and do not tabulate a maximum or a typical size.

### 2.3 How this repository ported it

Step 10 maps the setting to two diagnostics:

| Code | Severity | Rule | Owner |
| --- | --- | --- | --- |
| `JOB004` | Error | Cell count outside `1..256`, or total positions > 500,000 | Core validator for the cell range; Packages build stage for the total |
| `JOB014` | Warning | Total positions > 100,000 | Packages build stage |

Sources: [`step-10`](../steps/step-10-job-templates-and-supercells.md) §6 (table), §7 (ownership), and the
implementation constants `SupercellGenerator.MaximumPositions = 500_000` and
`PositionWarningThreshold = 100_000`. `JobTemplateValidationStage` computes
`(long)A * B * C * positionCount` and reports `JOB004` above the maximum; `SupercellGenerator` repeats the
guard when it starts generation.

Enforcement is **build-stage, not `validate`**:

- `mocassin validate` runs the Core structural validator without a compiled model, so it can check the
  `1..256` cell range but not `A*B*C*P`. A project whose product exceeds 500,000 validates clean and fails at
  build or `build --preview`.
- `mocassin build` and `build --preview` compile first, register `JobTemplateValidationStage`, and set
  `RunBuildStage = true`; the total-position error and the large-supercell warning appear there.
- There is no override, environment variable, or CLI flag. The only way to build above the cap is to change the
  constant or bypass the public build path.

### 2.4 Step 13 reader alignment

Step 13 §3.3 (`ReaderLimits`) sets `max_positions` to 500,000 to match `SupercellGenerator.MaximumPositions`,
and additionally requires `A*B*C*P <= i32::MAX` (2,147,483,647). The same defaults appear in
[`02-data-contracts.md`](../02-data-contracts.md) under the solver read bounds:

| Reader limit | Default | Relationship |
| --- | ---: | --- |
| `max_positions` | 500,000 | Matches the .NET writer's cap. |
| `max_position_count` (P1 positions) | 10,000 | Matches `UnitCellVectorEncoder.MaxP1Positions`. |
| `max_rules` | 1,000,000 | Matches `CompilationOptions.MaxRuleCount`. |
| `max_model_elements` | 16,000,000 | Per-model MessagePack scan budget, independent of supercell size. |
| `max_lattice_payload_bytes` | 512 MiB | Lattice guard is `4 × A × B × C × P`; implies about 134M positions before this cap binds. |

The `i32::MAX` clause is a separate arithmetic guard, not a scientific bound. 500,000 is about **1/4,295** of
`i32::MAX` (0.023 %). It cannot be explained by the index-width clause.

### 2.5 What neither codebase explains

- There is no derivation, benchmark, or memory calculation behind 500,000 in either implementation.
- The original warning threshold of 100,000 is equally underived; it is exactly one fifth of the maximum,
  which suggests a round-number judgement rather than a measurement.
- The contract's `≤ i32::MAX` clause is three orders of magnitude looser and therefore does not support the
  value.
- "Ported for input parity" is the only rationale recorded in this repository. It reproduces a declared
  settings bound, not a demonstrated engine bound. That makes the parity argument weaker than it first appears:
  we are matching a number, not a behaviour that was ever exercised in the reference code.

## 3. What to measure and why

The purpose of the probes is to replace "500,000 feels safe" with either a derived bound or a documented
decision to keep parity. Every probe must record the model (P1 positions, particles, pair/cluster link counts),
the supercell request, the hardware (CPU, RAM, OS), and the exact command.

### 3.1 Build-side scaling

Build one real sample model at 100k, 500k, 1M, and 5M total positions. The 5M point is above the current guard,
so it requires a **throwaway probe branch or harness** that calls the generator/writer below the public guard.
Do not merge a raised cap to obtain the measurement; record the harness revision and delete it afterwards.

Record, per point:

- wall time for lattice generation, doping application, charge compensation, and preview (if measured
  separately);
- peak RSS of the build process (managed and native if separable);
- output package size, split into model payload, lattice payload, job payload and rows, SQLite page overhead,
  and total;
- `mocassin verify` time and peak RSS on the result.

Questions answered:

- Is the curve linear in positions, or does something turn superlinear? Candidate superlinear paths are the
  downward ratio search for charge compensation (`JOB012`), repeated preview/lattice copies, per-position
  diagnostics, and SQLite page growth.
- Does a 5M-position build finish in a time a user would tolerate, or does the build itself become the
  bottleneck before the solver starts?
- Is peak build RSS dominated by the lattice byte array or by model/preview copies? That determines whether
  the build needs a separate cap from the solver.

### 3.2 Package composition and the Step 13 budgets

The reader's budgets do not all scale with supercell size, and comparing them by headline number is
misleading:

| Budget | Value | Scales with supercell positions? | Consequence |
| --- | ---: | --- | --- |
| `max_model_elements` | 16M elements | No — the model payload is per unit cell | Not a supercell cap. |
| `max_lattice_payload_bytes` | 512 MiB | Yes, guard `4 × A × B × C × P` | Binds near 134M positions. |
| `max_positions` | 500,000 | Yes | Binds first today. |
| `i32::MAX` linear index | 2.1B | Yes | Far above the current cap. |
| `.mcs` size cap (Step 18) | 3.5 GiB default | Yes, through lattice and trackers | May bind before the reader at high particle counts. |

Measure an actual package to confirm the composition. The expected shape is that the lattice is about one byte
per position before MessagePack framing, so 500,000 positions are well under 1 MiB and 5M positions are a few
MiB — small compared with the model tables and SQLite overhead. If that holds, the byte budgets are not the
reason for 500,000; solver runtime memory or the state file is.

The `.mcs` interaction needs its own probe. Step 18 stores static trackers per `(cell, position, particle)`, and
Step 13 pins `static_trackers_per_cell = position_count × (particle_count − 1)`. At 500,000 positions and 64
particles that is about 31.5M tracker vectors (roughly 0.75 GiB of raw `[f64; 3]` before encoding); at 5M
positions with the same 64 particles it is about 315M vectors, well past the 3.5 GiB default state cap. The state file may therefore be
the first hard ceiling for high-particle-count models, not the reader.

### 3.3 Solver-side memory model

Step 14 already fixes the data-oriented shape: a flat particle array, per-position flags, a dense
`[position][particle]` energy vector, cluster codes, a pair-link CSR, a cluster-link CSR, a selection pool,
per-`[cell][direction]` jump status, and trackers. Step 15 keeps flat tables in kT and may add optional
pair-delta tables.

A first-order model:

$$
M \approx M_{\text{fixed}} + N \left(c_{\text{env}} + L\,c_{\text{link}} + K\,c_{\text{cluster}}
+ S\,c_{\text{tracker}}\right),
$$

where $N = A B C P$ is the supercell position count, $L$ the pair links per position, $K$ the cluster links per
position, and $S$ the tracker vectors per position. $M_{\text{fixed}}$ covers the model tables, rules, code,
and allocator overhead. Every coefficient must be measured, not assumed.

Record at minimum:

| Component | What to measure | Why it matters |
| --- | --- | --- |
| Lattice and flags | Bytes per position | Sets the floor. |
| Energy vectors | Bytes per position × allowed particles | Dense site/pair energies can dominate. |
| Pair links (CSR) | Link struct size × links per position | Grows with interaction range, not just positions. |
| Cluster links | Entry size × clusters per position | 9-position clusters multiply entries. |
| Jump status | `[cell][direction]` links × entry size; the documented cap is 56 links | Grows with cells, not with P1 positions alone. |
| Trackers | Global, mobile, static; static is `N × (particles − 1)` vectors | Usually the largest term at high particle counts. |
| Allocator/arena overhead | Fraction of peak RSS not in arrays | Can be 10–30 % and is not in the formula. |

Then state a **target RSS per tier** and derive the implied cap. Illustrative arithmetic only — not a
measurement and not a proposal:

| Measured cost per position | 2 GiB tier | 8 GiB tier | 32 GiB tier |
| ---: | ---: | ---: | ---: |
| 64 B | ~33.5M | ~134M | ~536M |
| 512 B | ~4.2M | ~16.8M | ~67M |
| 4 KiB | ~524k | ~2.1M | ~8.4M |

The 4 KiB row shows the only way 500,000 looks like a 2 GiB design point: at about 4 KiB per position, 500k
fills 2 GiB. That is plausible for a dense 64-particle model with trackers, but it is not established by any
document in either repository. It must be measured before it can justify the cap.

The project currently has no solver-side per-position budget. Step 13's 64 MiB peak-RSS figure is explicitly a
ceria-fixture budget, not a large-package promise; Step 14 requires a memory report but names no number; the
quality strategy's `< 2 GiB` figures are build and 10k-P1-position budgets. This is an evidence gap that any
cap decision has to close.

### 3.4 Which cap binds first

For a realistic model, rank the caps by the position count at which each fails:

1. `max_positions` 500,000 — current intended limit.
2. `.mcs` size cap and tracker memory — likely the first physical failure at high particle counts.
3. Solver peak RSS on the target tier — the cap the project actually cares about.
4. `max_lattice_payload_bytes` 512 MiB — about 134M positions.
5. `i32::MAX` linear index — about 2.1B positions.
6. `max_model_elements` 16M — independent of supercell size.

The ranking must be measured, not reasoned from the table. If the state or RSS cap binds first, then relaxing
`JOB004` alone would move the failure from a clear build error to a late solver OOM, which is worse UX than the
current cap.

### 3.5 Index-width assumptions

Current assumptions are consistent across the stack but not obviously future-proof:

- .NET `Vector4I` is `(int A, int B, int C, int P)`; `UnitCellVectorEncoder.GetLinearIndex` returns `int` and
  throws when the checked total exceeds `int.MaxValue`.
- Rust `Position` is `[i32; 4]`, hot indices are `u32`, and Step 13 validates `A*B*C*P <= i32::MAX`.
- The original native structures use `int32` object and position ids, and the state header stores section
  starts as `int32` byte offsets (`SimulationState.h`), which caps a state section at 2 GiB by construction.
  That is a genuine 32-bit-era constraint, but it is a 2 GiB byte cap, not a 500k-position cap.

Raising the cap to 1M or 5M changes no index width and no array stride. Raising it toward 2.1B would require
64-bit indices, a new format version, and a compatibility decision. The probe should confirm there is no
`int` multiplication in the supercell index path that overflows below 2.1B; both implementations already
compute the total in 64-bit before comparing.

### 3.6 Practical scientific need

This is the probe the codebase cannot answer alone. It needs domain input:

- What supercell sizes do the reference materials and mechanisms actually need for converged transport
  statistics — ceria, Y-doped barium zirconate, and the complex/interstitialcy fixture?
- What are the P1 position counts and typical particle counts of those models, and therefore how many cells
  does 500,000 allow? As crystal-chemistry arithmetic only: a 12-atom fluorite cell allows about 41,600 cells
  (≈34³) at 500,000 positions; a 5-atom perovskite cell allows about 100,000 cells (≈46³). These illustrate
  the scale; they are not measurements of the checked-in samples.
- Does any reference study need more than ~10⁵–10⁶ atoms, and if so, is the need statistical (more dopants),
  geometric (larger correlation length), or both?
- What is the largest supercell in the original user guide or published MOCASSIN work? The user guide gives no
  size. The NaSICON draft in this directory mentions a 10×10×10 published study, which is a plan-level
  reference, not a verified original example.

If 500,000 is already several times larger than any reference simulation, the scientific-need argument for
raising it is weak and the parity argument dominates. If reference convergence studies want 1–5M, the cap is
actively rejecting legitimate work.

### 3.7 Why the original may have chosen 500,000

Hypotheses, none evidenced:

- A 32-bit era and native state layout: `int32` ids and byte offsets cap sections at 2 GiB, so a "few hundred
  thousand positions × tens of bytes per position" guard kept a run inside that envelope.
- Desktop memory at the time: a 500k-position run with dense energies and trackers plausibly needed 1–2 GB,
  which was a large workstation allocation.
- A UI guard against typos: entering 1000 cells per axis would otherwise be accepted and then fail obscurely.
- The value is simply a round number chosen alongside `100,000` and `256` without a calculation.

The first two would still matter if the solver's per-position cost is high; the third is a UX requirement, not
a scientific one; the fourth is the null hypothesis this draft is testing.

### 3.8 User experience

Current behaviour is a hard build error with a message that names the limit and the requested product. That is
good for typos and bad for a scientist who genuinely needs 600k positions: there is no "I know what I am
doing" path.

Questions to answer:

- Is the failure discovered early enough? `build --preview` reports it before writing a package, but
  `validate` does not. A user who only runs `validate` sees a clean project and then a build failure.
- Should the preview show a cost estimate (positions, links, rough memory) so the user can choose a smaller
  cell before building?
- If the limit is raised, does build time or package size become the new surprise? The probe in §3.1 answers
  this.
- Should the warning at 100,000 be a cost-aware warning (positions × links × particles) rather than a raw
  position count? A 100k-position model with 60 links per position is not the same risk as a 100k-position
  model with 6.

## 4. Candidate options, with trade-offs

No option is recommended here. The table summarises; the notes give the reasoning.

| Option | Parity impact | Contract/version impact | `ReaderLimits` lockstep | Test/fixture impact | Failure mode when exceeded |
| --- | --- | --- | --- | --- | --- |
| **A. Keep 500,000** | Exact | None | None | Existing `JOB004`/`JOB014` tests stay | Clear build error; possible rejection of legitimate science |
| **B. Raise to a derived value** | Deliberate deviation | None if only a constant changes; release note required | Must change in the same release | New boundary tests, large fixtures, performance evidence | Late solver OOM if the derived value is wrong |
| **C. Configurable, default 500,000** | Exact by default; deviates when changed | Where the setting lives must be decided; package metadata only if it becomes a format field | Reader must accept the configured maximum or packages become unreadable | Config precedence, portability, and cross-machine tests | A package built on a big machine fails on a small one |
| **D. Warning-only above a soft limit** | Near-parity; original error becomes warning | None if the hard ceiling stays | Hard ceiling still enforced | Warning/error boundary tests | User can OOM or produce a multi-GB state file |
| **E. Per-machine resource check** | No parity claim | Build report only; no schema change | Reader still needs a hard ceiling | Host-dependent tests are hard to make deterministic | Same package builds on one machine, fails on another |

**A. Keep 500,000.** The only option that is unambiguously parity-preserving as a number. It is cheap, it
already has tests, and it is defensible if the probes show the solver's real limit is at or below 500k. Its
weakness is that the justification remains "the original said so" even though the original did not enforce it.
If the project's stated goal is input parity rather than scientific throughput, this is the coherent choice;
that goal should then be written down where the cap is documented.

**B. Raise to a derived value.** Choose the cap from a memory model and a target tier, for example
`cap = floor((tier_RSS − fixed) / cost_per_position)`, rounded to a round number and validated by a benchmark
at the cap. Parity impact is a deliberate deviation: projects for which a caller of the original setting's
`ParseValue` would have reported a warning become buildable. The package format need not change, but the writer and reader must move
together, and the release notes must say that a newer writer can produce packages a same-major older reader
rejects. The risk is an incorrect coefficient: a cap derived from a sparse model can OOM on a dense one, so
the memory model needs a worst-case link/cluster/tracker factor, not an average.

**C. Configurable default.** Expose the maximum as a build setting with a documented default of 500,000. This
preserves parity by default and gives advanced users an escape hatch. The hard questions are where the setting
lives (project, user profile, CLI), whether it becomes part of the package contract, and how the reader knows
what to accept. A project-level setting that is not stored in the package makes the package's validity depend
on a file the solver never sees; a package-level field is a format change. A user-profile or CLI setting keeps
the format clean but makes the same project buildable on one machine and not another, which conflicts with
reproducibility.

**D. Warning-only above a soft limit.** Keep a hard ceiling (perhaps 500k, perhaps higher) but demote the
current error to a warning above a soft threshold and let the user proceed. This is a UX improvement for
legitimate large runs, but it does not answer the scientific question; it only moves the decision to the user.
If the hard ceiling stays at 500k, this is option A with a louder warning. If the hard ceiling moves, it is
option B with a warning band.

**E. Per-machine resource check.** Estimate the run's memory from the compiled model (positions × links ×
particles × trackers) and compare it with available RAM before building or running. This is the most
scientifically honest option, because the real constraint is memory, not a position count. It is also the
least reproducible: the same project can build on a 64 GiB workstation and fail on a 16 GiB laptop, and tests
that assert the check need host-dependent inputs. A hybrid — a hard format/reader ceiling plus a build-time
estimate that warns — may be the practical form, but that is two decisions, not one.

## 5. Evidence and review needs

### 5.1 What a reviewer must see before this can be promoted

1. **A benchmark table** with the exact commands, hardware, model (P1 positions, particles, pair/cluster link
   counts), supercell request, build wall time, peak RSS, package size, and `.mcs` size at each point.
2. **A validated memory model** for the solver: measured bytes per position for each component, with the
   worst-case link, cluster, and tracker factors stated, and at least three measured points that bracket the
   proposed cap.
3. **Reference sizes** for the actual science: the supercells used or needed by the reference materials and
   mechanisms, with sources, not estimates.
4. **A decision on parity**: is input parity a goal for this cap, or is the cap a performance guard that may
   deviate? The answer determines whether option A is required or option B is allowed.
5. **A compatibility statement**: writer/reader lockstep, current + previous major support, and what happens
   when a newer writer emits a package above an older reader's limit.
6. **A UX statement**: hard error vs warning, where the user sees it (`validate`, preview, build), and what
   the message tells them to do.
7. **A rollback plan**: if the raised cap causes OOM reports, how the cap is lowered without breaking already
   built packages.

### 5.2 Which steps own the change

| Area | Step | What would change |
| --- | --- | --- |
| Build validation and generation | Step 10 | `SupercellGenerator.MaximumPositions`/`PositionWarningThreshold`, `JOB004`/`JOB014` text and tests, user-guide and diagnostics reference |
| Package reader | Step 13 | `ReaderLimits.max_positions`, the `i32::MAX` interaction, reader tests and large-package fixtures |
| Solver memory and initialization | Step 14 | Environment/link/tracker layout, memory report, target RSS tier |
| Energy engine | Step 15 | Optional tables and caches; per-position cost |
| KMC/MMC loops | Steps 16/17 | Jump-status and selection-pool memory at scale |
| Tracking and state | Step 18 | Static/mobile/global tracker size, `.mcs` cap, state writer |
| Parity and release | Step 29 | Reference sizes, statistical parity evidence, performance baselines, release note |

Timing matters: Step 13's design is recorded and its implementation has not started, but the start gate is
still pending. A change to `ReaderLimits` is cheapest **before** Step 13 implementation and its audit. After
the step freezes, the normal route is a focused amendment (Step 13 or a parent-step amendment), not an edit to
a frozen branch. Step 10 is already merged and tagged; changing its cap is a maintenance/amendment change with
its own review and audit.

### 5.3 Recording a deliberate deviation

If the owner chooses an option that differs from 500,000, the decision must be recorded where the cap is
normative:

- a Step 10 page update (or amendment if the step is frozen) with the new value, the derivation, and the
  diagnostics;
- a Step 13 page/contract update for `ReaderLimits` and the compatibility rule;
- a parity-matrix note if the change affects a parity-relevant capability;
- an ADR if the change touches the package contract, the index width, or the current/previous-major
  compatibility promise;
- a release note telling users that a newer writer can produce packages an older reader rejects.

The `AGENTS.md` rule "no silent scientific corrections" applies by analogy: do not change the cap without
recording the reason and adding a test at the boundary.

## 6. Open questions

- Is 500,000 a UI/settings bound only, or did a shipped original build enforce it elsewhere? The public clone
  cannot prove the negative.
- Should the original lower warning of 100 positions be ported, or is dropping it correct?
- Is input parity a goal for this specific limit, or only for scientific observables?
- What target RSS tiers does the project actually promise — 2, 8, or 32 GiB — and on what hardware class?
- What are the P1 position counts and link densities of the ceria, barium zirconate, and complex fixtures?
- Which cap binds first in practice: solver RSS, `.mcs` size/trackers, or the reader's position limit?
- Is the 100,000 warning a useful signal, or should it be cost-aware (positions × links × particles)?
- Should the cap be per job (one job per process) or per build (many jobs in one package)?
- If the cap is raised, how are 5M-position benchmarks produced without shipping a build that bypasses the
  guard?
- Does the package format need to record the writer's cap so the reader can give a better error than "invalid
  package"?

## 7. Non-goals

- Not a decision: no option is selected, and no cap is changed.
- No code, tests, contracts, fixtures, configuration, or generated artifacts.
- No benchmark run, no measured numbers presented as results; the arithmetic in §3.3 is illustrative only.
- No step-page, status, parity-matrix, ADR, review, audit, or XC-04 change.
- No claim that 500,000 is wrong. The draft's verified finding is narrower: it has no recorded derivation and
  no evidenced consumer in the reference clone.
- No reopening of Step 10 or Step 13. Promotion goes through the normal step-page/amendment/ADR route.

## 8. References

### Repository

- [`AGENTS.md`](../../../AGENTS.md) §3 (branch/merge rules) and §10 (scientific invariants).
- [`step-10-job-templates-and-supercells.md`](../steps/step-10-job-templates-and-supercells.md) §6–§7,
  `JOB004`/`JOB014`.
- [`step-13-rust-solver-foundation-and-package-reader.md`](../steps/step-13-rust-solver-foundation-and-package-reader.md)
  §3.3 (`ReaderLimits`), §6 (data-oriented structures).
- [`step-14-simulation-context-initialization.md`](../steps/step-14-simulation-context-initialization.md) §2–§6.
- [`step-15-energy-engine.md`](../steps/step-15-energy-engine.md), [`step-18-tracking-statistics-and-state-writer.md`](../steps/step-18-tracking-statistics-and-state-writer.md).
- [`02-data-contracts.md`](../02-data-contracts.md) solver read bounds and `.mcs` size assumption.
- [`appendix-b-parity-matrix.md`](../appendix-b-parity-matrix.md) supercell and lattice rows.
- [`docs/user-guide/job-templates.md`](../../user-guide/job-templates.md) limits table.
- `src/dotnet/Mocassin.Packages/Jobs/SupercellGenerator.cs`;
  `src/dotnet/Mocassin.Packages/Projects/Validation/JobTemplateValidationStage.cs`;
  `src/dotnet/Mocassin.Core/Geometry/Vector4I.cs`;
  `src/dotnet/Mocassin.Symmetry/Geometry/UnitCellVectorEncoder.cs`.

### Read-only original (paths only; no copied text)

- `src/ModelBuilder/ICon.Model/ModelProject/Manager/Settings/Modules/MocassinSimulationSettings.cs`
  (declaration line 70, constructor line 155).
- `src/ModelBuilder/ICon.Model/ModelProject/Manager/Objects/ValueSetting.cs` (constructor, `ParseValue`).
- `src/ModelBuilder/ICon.Framework.Mathematics/ValueTypes/Vectors/Vector4I.cs` (`int` coordinates).
- `src/McSolver/Libraries/Simulator/Data/State/SimulationState.h` (`int32` section byte offsets).
- `src/McSolver/Libraries/Simulator/Data/Jobs/JobDbModel.h` (`int32` ids and position fields).
- `docs/userguide-md/job-templates.md` and `docs/userguide-md/the-simulator.md` (no maximum or typical size
  stated).
