# Implementation Status

This is the at-a-glance status dashboard for the 32-step plan. The detailed task and acceptance checkboxes
remain in each step page; each step page also has a feature log.

## Status values

| Status | Meaning |
| --- | --- |
| **Not started** | No step branch or feature work yet. |
| **In progress** | Step branch exists and feature work is underway. |
| **Blocked** | Work cannot continue until a named blocker is resolved. |
| **Awaiting approval** | The step or audit round is frozen and waiting for human approval of the verdict, merge, or tag. |
| **In audit** | The step branch is frozen at the audit request; a fresh completion audit is pending or running. |
| **Done** | All acceptance criteria pass and the step is merged into `main`. |
| **Deferred** | Intentionally postponed beyond the first parity release. |

## Update cadence

- **Step start:** after the design review is `Ready for implementation` and the feature-branch list is
  approved, set the step to `In progress` and record the step branch.
- **Each feature:** update the feature row and step page checkboxes in the feature branch. Mark it
  `Squashed` or `Done` after the squash; no per-feature approval is required.
- **After squash and step CI:** the next feature or the closeout feature records the squash commit and CI
  result.
- **Step completion:** the closeout feature records `In audit`, the closeout CI result, the closeout evidence
  path, and the audit request. Merge current `main`, run `./tools/verify.sh all --require`, fill and check
  `docs/planning/closeout/step-NN.json`, and set the audit status to `Ready for audit`. After a passed audit and
  merge, set the step to `Done`, record the main merge reference, and create and push the annotated `step-NN`
  tag as part of the same approval.
- **Audit:** a fresh out-of-context audit agent reviews the frozen step branch and writes its evidence,
  findings, and verdict directly into `docs/planning/audits/step-NN.md`. See
  [`audits/README.md`](audits/README.md).
- **Design review:** before implementation, a fresh planning auditor reviews the frozen design docs and writes
  its evidence, findings, and verdict directly into `docs/planning/reviews/step-NN.md`. See
  [`reviews/README.md`](reviews/README.md).
- **Milestones:** when a roadmap milestone is reached, create the matching `milestone-mN` tag on the
  milestone commit.
- **Parity:** keep [`appendix-b-parity-matrix.md`](appendix-b-parity-matrix.md) as the feature-level
  completeness source.
- **Tags are immutable:** see [`AGENTS.md`](../../AGENTS.md) section 3.7.

## Dashboard

The `Step audit` column uses the audit statuses defined in [`audits/README.md`](audits/README.md).
The `Design review` column uses the statuses defined in [`reviews/README.md`](reviews/README.md).

| Step | Title | Phase | Status | Step branch | Current feature | Step CI | Main merge | Step tag | Step audit | Design review |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 00 | [Repository, Toolchain, and CI](steps/step-00-repository-toolchain-and-ci.md) | A. Foundations | Done | `step/00-foundation` | — | [green](https://github.com/seb-eis/mocassin-2/actions/runs/41733064540) (Ubuntu + Windows + macOS on main) | `47a9a90` | `step-00` | Passed with notes | Waived — introduced after completion |
| 01 | [Domain Kernel and Project Format](steps/step-01-domain-kernel-and-project-format.md) | A. Foundations | Done | `step/01-domain-kernel` | — | [green](https://github.com/seb-eis/mocassin-2/actions/runs/41929788253) (main: Ubuntu + Windows + macOS) | `7b4a994` | `step-01` | Passed with notes at `05c41ab` | Waived — introduced after completion |
| 02 | [Symmetry and Geometry](steps/step-02-symmetry-and-geometry.md) | A. Foundations | Done | `step/02-symmetry-and-geometry` (merged and deleted) | — | [closeout green](https://github.com/seb-eis/mocassin-2/actions/runs/42478356751) | `dbf5691` | `step-02` | Passed with notes at `6341b56` | Ready with notes — accepted |
| 03 | [Particle and Structure Models](steps/step-03-particle-and-structure-models.md) | B. Model definition | Done | `step/03-particle-and-structure-models` (merged and deleted) | — | [main green](https://github.com/seb-eis/mocassin-2/actions/runs/42521062856) | `162ebe4` | `step-03` | Passed with notes at `b6a97ec` | Ready with notes — accepted at `3096aa8` |
| 04 | [Transition Model](steps/step-04-transition-model.md) | B. Model definition | Done | `step/04-transition-model` (merged and deleted) | — | [green](https://github.com/seb-eis/mocassin-2/actions/runs/42695864895) (closeout) | `31ba12d` | `step-04` | Passed with notes at `82c8cb4` | Ready for implementation — accepted at `a3d8641` |
| 05 | [Energy Model](steps/step-05-energy-model.md) | B. Model definition | Done | `step/05-energy-model` (merged and deleted) | — | [green](https://github.com/seb-eis/mocassin-2/actions/runs/42881426273) (replacement closeout) | `c11a364` | `step-05` | Passed with notes at `315aa19` | Ready for implementation — accepted at `0b2a571` |
| 06 | [Lattice and Simulation Models](steps/step-06-lattice-and-simulation-models.md) | B. Model definition | Done | `step/06-lattice-and-simulation-models` (merged and deleted) | — | [closeout green](https://github.com/seb-eis/mocassin-2/actions/runs/43085605850) | `41cfa4d` | `step-06` | Passed with notes at `c390dcc` (code `ea6293e`); accepted 2026-11-11 | Ready with notes — accepted at `6173e61` |
| 07 | [Validation, Diagnostics, and Model Statistics](steps/step-07-validation-and-diagnostics.md) | B. Model definition | Done | `step/07-validation-and-diagnostics` (merged and deleted) | — | [replacement closeout green](https://github.com/seb-eis/mocassin-2/actions/runs/43291748042) / [main green](https://github.com/seb-eis/mocassin-2/actions/runs/43293189634) | `3ed558a` | `step-07` | Passed with notes at `ccd0e55` (`d66f34f`) | Ready for implementation at `31b53e8` — review `2d01f3e`; approved 2026-11-11 |
| 08 | [Model Compilation Pipeline](steps/step-08-model-compilation-pipeline.md) | C. Compilation | Done | `step/08-model-compilation-pipeline` (merged and deleted) | `step-08` | [closeout green](https://github.com/seb-eis/mocassin-2/actions/runs/43538703111) / [main green](https://github.com/seb-eis/mocassin-2/actions/runs/43578897490) | `f91fc63` | `step-08` | Passed with notes at `28e741f` (`f97b2c4`) | Ready for implementation at `b84ceadda1ba8b1715592a4f6884d76bc7c36fed` — accepted 2026-11-13 |
| 09 | [Parameterization Templates](steps/step-09-parameterization-templates.md) | C. Compilation | Done | `step/09-parameterization-templates` (merged and deleted) | — | [replacement closeout green](https://github.com/seb-eis/mocassin-2/actions/runs/43634322838) / [main green](https://github.com/seb-eis/mocassin-2/actions/runs/43639561778) | `ef2f335` | `step-09` | Passed with notes at `916bae1` (`3aeacb1`) — accepted 2026-11-14 | Ready with notes at `26d6a0e` (round 4) — accepted 2026-11-14 |
| 10 | [Job Templates and Supercell Generation](steps/step-10-job-templates-and-supercells.md) | C. Compilation | Done | `step/10-job-templates` (merged and deleted) | — | [main green](https://github.com/seb-eis/mocassin-2/actions/runs/43709029767) at `945dddf` | `945dddf` | `step-10` | Passed with notes at `c10e6d7` — accepted 2026-11-15 | Ready for implementation at `c5454af` — accepted 2026-11-14 |
| 11 | [Simulation Package and `.msl` Writer](steps/step-11-simulation-package-and-msl-writer.md) | C. Compilation | In progress | `step/11-simulation-package-and-msl-writer` | 3 — SQLite schema and publication barrier (squashed) | — | — | — | Not requested | Ready with notes — accepted 2026-11-15 ([record](reviews/step-11.md)) |
| 12 | [Build Orchestration and `mocassin` CLI](steps/step-12-build-orchestration-and-cli.md) | C. Compilation | Not started | — | — | — | — | — | Not requested | Not requested |
| 13 | [Rust Solver Foundation and Package Reader](steps/step-13-rust-solver-foundation-and-package-reader.md) | D. Solver | Not started | — | — | — | — | — | Not requested | Not requested |
| 14 | [Simulation Context Initialization](steps/step-14-simulation-context-initialization.md) | D. Solver | Not started | — | — | — | — | — | Not requested | Not requested |
| 15 | [Energy Engine](steps/step-15-energy-engine.md) | D. Solver | Not started | — | — | — | — | — | Not requested | Not requested |
| 16 | [Kinetic Monte Carlo Engine](steps/step-16-kmc-engine.md) | D. Solver | Not started | — | — | — | — | — | Not requested | Not requested |
| 17 | [Metropolis Monte Carlo Engine](steps/step-17-mmc-engine.md) | D. Solver | Not started | — | — | — | — | — | Not requested | Not requested |
| 18 | [Tracking, Statistics, and State Writer](steps/step-18-tracking-statistics-and-state-writer.md) | D. Solver | Not started | — | — | — | — | — | Not requested | Not requested |
| 19 | [Solver CLI, HPC Workflows, and Extensions](steps/step-19-solver-cli-hpc-and-extensions.md) | D. Solver | Not started | — | — | — | — | — | Not requested | Not requested |
| 20 | [State Reader, Result Collection, and Evaluation API](steps/step-20-state-reader-and-evaluation-api.md) | E. Results | Not started | — | — | — | — | — | Not requested | Not requested |
| 21 | [Analysis, Plots, and Reports](steps/step-21-analysis-plots-and-reports.md) | E. Results | Not started | — | — | — | — | — | Not requested | Not requested |
| 22 | [Python Analysis API](steps/step-22-python-analysis-api.md) | E. Results | Not started | — | — | — | — | — | Not requested | Not requested |
| 23 | [Avalonia Shell and Project Lifecycle](steps/step-23-avalonia-shell-and-project-lifecycle.md) | F. GUI | Not started | — | — | — | — | — | Not requested | Not requested |
| 24 | [Particle, Structure, and Transition Editors](steps/step-24-model-editors-particle-structure-transitions.md) | F. GUI | Not started | — | — | — | — | — | Not requested | Not requested |
| 25 | [Energy, Lattice, and Simulation Editors](steps/step-25-model-editors-energy-lattice-simulations.md) | F. GUI | Not started | — | — | — | — | — | Not requested | Not requested |
| 26 | [Parameterization, Job Templates, and Build UI](steps/step-26-parameterization-job-templates-and-build-ui.md) | F. GUI | Not started | — | — | — | — | — | Not requested | Not requested |
| 27 | [3D Viewer and Visualization](steps/step-27-3d-viewer-and-visualization.md) | F. GUI | Not started | — | — | — | — | — | Not requested | Not requested |
| 28 | [Run Monitoring, Analysis UI, and MMCFE](steps/step-28-run-monitoring-and-mmcfe-ui.md) | F. GUI | Not started | — | — | — | — | — | Not requested | Not requested |
| 29 | [Validation, Statistical Parity, Performance, and Hardening](steps/step-29-validation-parity-and-performance.md) | G. Release | Not started | — | — | — | — | — | Not requested | Not requested |
| 30 | [Standalone Documentation Completion and Publishing](steps/step-30-standalone-documentation.md) | G. Release and documentation | Not started | — | — | — | — | — | Not requested | Not requested |
| 31 | [Packaging and Release](steps/step-31-packaging-and-release.md) | G. Release and documentation | Not started | — | — | — | — | — | Not requested | Not requested |

## Active work

Phase B — Model definition (Steps 03–07) is complete. Step 07 is merged as `3ed558a`, tagged `step-07`, and
its main CI run [`43293189634`](https://github.com/seb-eis/mocassin-2/actions/runs/43293189634) is green; the
audit passed with notes at `ccd0e55`. The Phase B cross-cutting checkpoint is complete: `XC-01`, `XC-02`, and
`XC-03` all passed with notes at the substantive checkpoint `84781f8` and were accepted by the human owner on
2026-11-12. XC-02 closed three Majors across rounds (`F-01` at `8d61e71`, `R2-F1` at `47c5927`, and `R3-F1` at
`84781f8`). The checkpoint was merged into `main` as `e48d9ca`; main CI
[`43311424938`](https://github.com/seb-eis/mocassin-2/actions/runs/43311424938) is green. The Step 08 design
review may now start. A Phase B drift check ran on 2026-11-12 at `38a22d2`; its Major status/documentation
findings were remediated, D6 was closed by the `milestone-m0`–`m3` backfill, D10–D12 remain XC-01 follow-ups,
and D14 is an accepted audit note. Steps 03–06 remain complete: merges `162ebe4`, `31ba12d`, `c11a364`, and
`41cfa4d`; tags `step-03`–`step-06`; all audits passed with notes.

Step 08 — Model Compilation Pipeline is complete: merged as `f91fc63` (`--no-ff`), tagged `step-08`, main CI
[`43578897490`](https://github.com/seb-eis/mocassin-2/actions/runs/43578897490) green, audit passed with notes
at `28e741f`. The design was accepted on 2026-11-13 and merged to `main` as `51b99f0`; the human owner waived
the roadmap §3.1 split trigger for the 4–6 person-week step on 2026-11-12.

Step 09 — Parameterization Templates is complete. The design freeze is
`26d6a0e`; the review record `docs/planning/reviews/step-09.md` returned `Ready with notes` in round 4 and the
human owner accepted it on 2026-11-14 together with the seven-feature list and the single-step 3.4–4.4
person-week exception. Features 1–7 are squash-merged through `20c73bc`; the round-1 fixes are squashed as
`916bae1`. The single replacement closeout CI run
[`43634322838`](https://github.com/seb-eis/mocassin-2/actions/runs/43634322838) is green at that commit; the
earlier `43630049214` covered the pre-fix commit. No per-feature CI run is used.

Independent audit round 1 returned `Fixes required` at `20c73bc`: F-01 is a Blocker (finite values above the
canonical 12-decimal projection range could throw instead of returning `PRM` diagnostics), F-02/F-03 are Minor
completeness and structural-validation defects, F-04/F-05 are Minor stale documentation, and F-06/F-07 are
Notes. The fixes were squash-merged as `916bae1`, the full local gate passed, the replacement closeout CI run
is green, and `docs/planning/closeout/step-09.json` binds the new commit. Audit round 2 returned `Passed with
notes` at `916bae1` (record `3aeacb1`); the owner accepted the notes on 2026-11-14. The step merged into `main`
as `ef2f335` (`--no-ff`), main CI [`43639561778`](https://github.com/seb-eis/mocassin-2/actions/runs/43639561778)
is green, and the annotated `step-09` tag is pushed. The step branch was deleted.

Step 10 — Job Templates and Supercell Generation is complete. The owner accepted the `Passed with notes`
verdict at `c10e6d7` on 2026-11-15; the step merged into `main` as `945dddf` (`--no-ff`), main CI
[`43709029767`](https://github.com/seb-eis/mocassin-2/actions/runs/43709029767) is green at that merge, and the
annotated `step-10` tag is pushed. The step branch was deleted. The replacement closeout run
[`43704552037`](https://github.com/seb-eis/mocassin-2/actions/runs/43704552037) remains the closeout evidence at
`c10e6d7`; the earlier [`43699356371`](https://github.com/seb-eis/mocassin-2/actions/runs/43699356371) at
`f2a364d` is process history. R2-1 (performance test-plan deferral) and R2-2 (canonical-JSON contract
reconciliation) are recorded and accepted, with R2-3–R2-5 as Notes. Step 11 — Simulation Package and `.msl`
Writer is next. Its round-1 design review returned `Changes required` at `8ac8590` (M-1 pair-link offset, M-2
variable-length `tracker_order`, M-3 `PKG` descriptor composition, plus Minors and Notes); the design was revised
and is `Re-review required`. The step remains `Not started`.

## Cross-cutting phase checkpoints

Each phase closes with a repository-wide review. From Phase C that is one combined review with policies,
architecture/design, and documentation sections; the Phase B checkpoint used three separate reviews. The
architecture and design section also checks original-design fidelity: intended corrections, ported original
strengths, and regressions. The next phase's first step design review waits for the combined verdict, unless the
human owner waives the review with a recorded reason.

These reviews do not create step branches or tags and do not replace step audits or Step 29 parity validation.

| Phase | Steps | Reviews | Status |
| --- | --- | --- | --- |
| A. Foundations | 00–02 | Waived — process introduced after completion | — |
| B. Model definition | 03–07 | `XC-01` [policies and planning](reviews/cross-cutting/xc-01-policies-and-planning.md), `XC-02` [architecture and design](reviews/cross-cutting/xc-02-architecture-and-design.md), `XC-03` [documentation](reviews/cross-cutting/xc-03-documentation-status.md) | All three Passed with notes at `84781f8` — accepted by human owner 2026-11-12; merged `e48d9ca`, main CI `43311424938` green |
| C. Compilation and build | 08–12 | `XC-04` combined | Planned |
| D. Solver | 13–19 | `XC-05` combined | Planned |
| E. Results | 20–22 | `XC-06` combined | Planned |
| F. GUI | 23–28 | `XC-07` combined | Planned |
| G. Release and documentation | 29–31 | `XC-08` combined | Planned |

## Drift checks

Run a lightweight drift check at each milestone (or monthly when there is no milestone). Event-driven checks
after a process, CI, or architecture change still apply. Use
[`docs/planning/drift-checks/_template.md`](drift-checks/_template.md) and
[`docs/development/drift-checks.md`](../development/drift-checks.md). A drift check is not a gate; it is an
early-warning signal for the next cross-cutting review.

| Date | Commit | Checked by | Result | Findings / links |
| --- | --- | --- | --- | --- |
| 2026-11-12 | `38a22d2` | Fresh independent drift-check context (OpenCode subagent) | Drift found — 3 Major, 9 Minor, 3 Note; Major status/documentation drift remediated before checkpoint freeze | [2026-11-12-phase-b.md](drift-checks/2026-11-12-phase-b.md) |

## Notes

- Feature rows live in each step page under **Implementation status**.
- Step 00 is complete: merge `47a9a90`, tag `step-00`, full main CI `41733064540` green.
- Step 01 is complete: merge `7b4a994`, tag `step-01`, full main CI `41929788253` green.
- Step 02 is complete: merge `dbf5691`, tag `step-02`, main CI `42478570935` green.
- A step is not `Done` until its acceptance criteria, tests, documentation, and parity entries pass.
- Status updates should ride in feature branches or the closeout feature; avoid status-only commits. CI runs
  only at closeout and on `main`.
- Step 02 received a post-development maintenance update on 2026-11-08: `Mocassin.Symmetry` namespaces were
  aligned with their folders; the public API break is accepted; the `step-02` tag remains at `dbf5691`.
- Milestones M0–M3 were backfilled on 2026-11-12 with annotated tags on the existing step merge commits:
  `milestone-m0` → `47a9a90`, `milestone-m1` → `7b4a994`, `milestone-m2` → `dbf5691`, `milestone-m3` →
  `3ed558a`. Tags were pushed to `origin`; no step tag was moved.
- Tag names record completed checkpoints; they do not replace this dashboard.
