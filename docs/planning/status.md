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
| 00 | [Repository, Toolchain, and CI](steps/step-00-repository-toolchain-and-ci.md) | A. Foundations | Done | `step/00-foundation` | — | [green](https://github.com/seb-eis/mocassin-2/actions/runs/41733064540) (Ubuntu + Windows + macOS on main) | `47a9a90` | `step-00` | Passed | Waived — introduced after completion |
| 01 | [Domain Kernel and Project Format](steps/step-01-domain-kernel-and-project-format.md) | A. Foundations | Done | `step/01-domain-kernel` | — | [green](https://github.com/seb-eis/mocassin-2/actions/runs/41929788253) (main: Ubuntu + Windows + macOS) | `7b4a994` | `step-01` | Passed with notes at `05c41ab` | Waived — introduced after completion |
| 02 | [Symmetry and Geometry](steps/step-02-symmetry-and-geometry.md) | A. Foundations | Done | `step/02-symmetry-and-geometry` (merged and deleted) | — | [closeout green](https://github.com/seb-eis/mocassin-2/actions/runs/42478356751) | `dbf5691` | `step-02` | Passed with notes at `6341b56` | Ready with notes — accepted |
| 03 | [Particle and Structure Models](steps/step-03-particle-and-structure-models.md) | B. Model definition | Done | `step/03-particle-and-structure-models` (merged and deleted) | — | [main green](https://github.com/seb-eis/mocassin-2/actions/runs/42521062856) | `162ebe4` | `step-03` | Passed with notes at `b6a97ec` | Ready with notes — accepted at `3096aa8` |
| 04 | [Transition Model](steps/step-04-transition-model.md) | B. Model definition | Done | `step/04-transition-model` (merged and deleted) | — | [green](https://github.com/seb-eis/mocassin-2/actions/runs/42695864895) (closeout) | `31ba12d` | `step-04` | Passed with notes at `82c8cb4` | Ready for implementation — accepted at `a3d8641` |
| 05 | [Energy Model](steps/step-05-energy-model.md) | B. Model definition | Done | `step/05-energy-model` (merged and deleted) | — | [green](https://github.com/seb-eis/mocassin-2/actions/runs/42881426273) (replacement closeout) | `c11a364` | `step-05` | Passed with notes at `315aa19` | Ready for implementation — accepted at `0b2a571` |
| 06 | [Lattice and Simulation Models](steps/step-06-lattice-and-simulation-models.md) | B. Model definition | Done | `step/06-lattice-and-simulation-models` (merged and deleted) | — | [closeout green](https://github.com/seb-eis/mocassin-2/actions/runs/43085605850) | `41cfa4d` | `step-06` | Passed with notes at `c390dcc` (code `ea6293e`); accepted 2026-11-11 | Ready with notes — accepted at `6173e61` |
| 07 | [Validation, Diagnostics, and Model Statistics](steps/step-07-validation-and-diagnostics.md) | B. Model definition | Done | `step/07-validation-and-diagnostics` (merged and deleted) | — | [replacement closeout green](https://github.com/seb-eis/mocassin-2/actions/runs/43291748042) / [main green](https://github.com/seb-eis/mocassin-2/actions/runs/43293189634) | `3ed558a` | `step-07` | Passed with notes at `ccd0e55` (`d66f34f`) | Ready for implementation at `31b53e8` — review `2d01f3e`; approved 2026-11-11 |
| 08 | [Model Compilation Pipeline](steps/step-08-model-compilation-pipeline.md) | C. Compilation | Not started | — | — | — | — | — | Not requested | Not requested |
| 09 | [Parameterization Templates](steps/step-09-parameterization-templates.md) | C. Compilation | Not started | — | — | — | — | — | Not requested | Not requested |
| 10 | [Job Templates and Supercell Generation](steps/step-10-job-templates-and-supercells.md) | C. Compilation | Not started | — | — | — | — | — | Not requested | Not requested |
| 11 | [Simulation Package and `.msl` Writer](steps/step-11-simulation-package-and-msl-writer.md) | C. Compilation | Not started | — | — | — | — | — | Not requested | Not requested |
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
audit passed with notes at `ccd0e55`. The Phase B cross-cutting checkpoint is now active: `XC-01` policies and
planning starts first, then `XC-02` architecture and design and `XC-03` documentation. The Step 08 design review
waits for all three verdicts. A Phase B drift check ran on 2026-11-12 at `38a22d2`; its three Major
status/documentation findings were remediated before this checkpoint freeze, and the remaining Minor/Note
findings are tracked in the cross-cutting reviews. Steps 03–06 remain complete: merges `162ebe4`, `31ba12d`,
`c11a364`, and `41cfa4d`; tags `step-03`–`step-06`; all audits passed with notes.

## Cross-cutting phase checkpoints

Each phase closes with three repository-wide reviews: policies and planning, architecture and design status,
and documentation status. The policies and planning review starts first; architecture/design and documentation
may run in parallel. The architecture and design review also checks original-design fidelity: intended
corrections, ported original strengths, and regressions. The next phase's first step design review waits for
all three verdicts, unless the human owner waives one with a recorded reason.

These reviews do not create step branches or tags and do not replace step audits or Step 29 parity validation.

| Phase | Steps | Reviews | Status |
| --- | --- | --- | --- |
| A. Foundations | 00–02 | Waived — process introduced after completion | — |
| B. Model definition | 03–07 | `XC-01` [policies and planning](reviews/cross-cutting/xc-01-policies-and-planning.md), `XC-02` [architecture and design](reviews/cross-cutting/xc-02-architecture-and-design.md), `XC-03` [documentation](reviews/cross-cutting/xc-03-documentation-status.md) | Ready for review at `81fba45` — XC-01 starts first |
| C. Compilation and build | 08–12 | `XC-04`/`XC-05`/`XC-06` | Planned |
| D. Solver | 13–19 | `XC-07`/`XC-08`/`XC-09` | Planned |
| E. Results | 20–22 | `XC-10`/`XC-11`/`XC-12` | Planned |
| F. GUI | 23–28 | `XC-13`/`XC-14`/`XC-15` | Planned |
| G. Release and documentation | 29–31 | `XC-16`/`XC-17`/`XC-18` | Planned |

## Drift checks

Run a lightweight drift check monthly, at each milestone, and before every phase boundary. Use
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
- Tag names record completed checkpoints; they do not replace this dashboard.
