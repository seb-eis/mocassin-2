# Implementation Status

This is the at-a-glance status dashboard for the 32-step plan. The detailed task and acceptance checkboxes
remain in each step page; each step page also has a feature log.

## Status values

| Status | Meaning |
| --- | --- |
| **Not started** | No step branch or feature work yet. |
| **In progress** | Step branch exists and feature work is underway. |
| **Blocked** | Work cannot continue until a named blocker is resolved. |
| **Awaiting approval** | A feature is complete locally and waiting for squash-merge approval. |
| **Done** | All acceptance criteria pass and the step is merged into `main`. |
| **Deferred** | Intentionally postponed beyond the first parity release. |

## Update cadence

- **Step start:** after the feature-branch list is approved, set the step to `In progress` and record the
  step branch.
- **Each feature:** update the feature row and step page checkboxes in the feature branch. Mark it
  `Awaiting approval` before asking for approval.
- **After squash and step CI:** the next feature or the closeout feature records the squash commit and CI
  result.
- **Step completion:** the closeout feature records `Done`, the step CI result, and the main merge reference.
  It also updates `docs/planning/audits/step-NN.md` and sets the audit status to `Ready for audit`.
  After a passed audit and merge, create and push the annotated `step-NN` tag as part of the same approval.
- **Audit:** a fresh read-only audit agent reviews the frozen step branch. See
  [`audits/README.md`](audits/README.md).
- **Milestones:** when a roadmap milestone is reached, create the matching `milestone-mN` tag on the
  milestone commit.
- **Parity:** keep [`appendix-b-parity-matrix.md`](appendix-b-parity-matrix.md) as the feature-level
  completeness source.
- **Tags are immutable:** see [`AGENTS.md`](../../AGENTS.md) section 3.7.

## Dashboard

The `Step audit` column uses the audit statuses defined in [`audits/README.md`](audits/README.md).

| Step | Title | Phase | Status | Step branch | Current feature | Step CI | Main merge | Step tag | Step audit |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 00 | [Repository, Toolchain, and CI](steps/step-00-repository-toolchain-and-ci.md) | A. Foundations | Done | `step/00-foundation` | — | [green](https://github.com/seb-eis/mocassin-2/actions/runs/41733064540) (Ubuntu + Windows + macOS on main) | `47a9a90` | `step-00` | Ready for audit |
| 01 | [Domain Kernel and Project Format](steps/step-01-domain-kernel-and-project-format.md) | A. Foundations | Not started | — | — | — | — | — | Pending — step in progress |
| 02 | [Symmetry and Geometry](steps/step-02-symmetry-and-geometry.md) | A. Foundations | Not started | — | — | — | — | — | — |
| 03 | [Particle and Structure Models](steps/step-03-particle-and-structure-models.md) | B. Model definition | Not started | — | — | — | — | — | — |
| 04 | [Transition Model](steps/step-04-transition-model.md) | B. Model definition | Not started | — | — | — | — | — | — |
| 05 | [Energy Model](steps/step-05-energy-model.md) | B. Model definition | Not started | — | — | — | — | — | — |
| 06 | [Lattice and Simulation Models](steps/step-06-lattice-and-simulation-models.md) | B. Model definition | Not started | — | — | — | — | — | — |
| 07 | [Validation, Diagnostics, and Model Statistics](steps/step-07-validation-and-diagnostics.md) | B. Model definition | Not started | — | — | — | — | — | — |
| 08 | [Model Compilation Pipeline](steps/step-08-model-compilation-pipeline.md) | C. Compilation | Not started | — | — | — | — | — | — |
| 09 | [Parameterization Templates](steps/step-09-parameterization-templates.md) | C. Compilation | Not started | — | — | — | — | — | — |
| 10 | [Job Templates and Supercell Generation](steps/step-10-job-templates-and-supercells.md) | C. Compilation | Not started | — | — | — | — | — | — |
| 11 | [Simulation Package and `.msl` Writer](steps/step-11-simulation-package-and-msl-writer.md) | C. Compilation | Not started | — | — | — | — | — | — |
| 12 | [Build Orchestration and `mocassin` CLI](steps/step-12-build-orchestration-and-cli.md) | C. Compilation | Not started | — | — | — | — | — | — |
| 13 | [Rust Solver Foundation and Package Reader](steps/step-13-rust-solver-foundation-and-package-reader.md) | D. Solver | Not started | — | — | — | — | — | — |
| 14 | [Simulation Context Initialization](steps/step-14-simulation-context-initialization.md) | D. Solver | Not started | — | — | — | — | — | — |
| 15 | [Energy Engine](steps/step-15-energy-engine.md) | D. Solver | Not started | — | — | — | — | — | — |
| 16 | [Kinetic Monte Carlo Engine](steps/step-16-kmc-engine.md) | D. Solver | Not started | — | — | — | — | — | — |
| 17 | [Metropolis Monte Carlo Engine](steps/step-17-mmc-engine.md) | D. Solver | Not started | — | — | — | — | — | — |
| 18 | [Tracking, Statistics, and State Writer](steps/step-18-tracking-statistics-and-state-writer.md) | D. Solver | Not started | — | — | — | — | — | — |
| 19 | [Solver CLI, HPC Workflows, and Extensions](steps/step-19-solver-cli-hpc-and-extensions.md) | D. Solver | Not started | — | — | — | — | — | — |
| 20 | [State Reader, Result Collection, and Evaluation API](steps/step-20-state-reader-and-evaluation-api.md) | E. Results | Not started | — | — | — | — | — | — |
| 21 | [Analysis, Plots, and Reports](steps/step-21-analysis-plots-and-reports.md) | E. Results | Not started | — | — | — | — | — | — |
| 22 | [Python Analysis API](steps/step-22-python-analysis-api.md) | E. Results | Not started | — | — | — | — | — | — |
| 23 | [Avalonia Shell and Project Lifecycle](steps/step-23-avalonia-shell-and-project-lifecycle.md) | F. GUI | Not started | — | — | — | — | — | — |
| 24 | [Particle, Structure, and Transition Editors](steps/step-24-model-editors-particle-structure-transitions.md) | F. GUI | Not started | — | — | — | — | — | — |
| 25 | [Energy, Lattice, and Simulation Editors](steps/step-25-model-editors-energy-lattice-simulations.md) | F. GUI | Not started | — | — | — | — | — | — |
| 26 | [Parameterization, Job Templates, and Build UI](steps/step-26-parameterization-job-templates-and-build-ui.md) | F. GUI | Not started | — | — | — | — | — | — |
| 27 | [3D Viewer and Visualization](steps/step-27-3d-viewer-and-visualization.md) | F. GUI | Not started | — | — | — | — | — | — |
| 28 | [Run Monitoring, Analysis UI, and MMCFE](steps/step-28-run-monitoring-and-mmcfe-ui.md) | F. GUI | Not started | — | — | — | — | — | — |
| 29 | [Validation, Statistical Parity, Performance, and Hardening](steps/step-29-validation-parity-and-performance.md) | G. Release | Not started | — | — | — | — | — | — |
| 30 | [Standalone Documentation Completion and Publishing](steps/step-30-standalone-documentation.md) | G. Release and documentation | Not started | — | — | — | — | — | — |
| 31 | [Packaging and Release](steps/step-31-packaging-and-release.md) | G. Release and documentation | Not started | — | — | — | — | — | — |

## Active work

No step is active. Step 00 is complete; Step 01 is the next unblocked step.

## Notes

- Feature rows live in each step page under **Implementation status**.
- Step 00 is complete: merge `47a9a90`, tag `step-00`, full main CI `41733064540` green.
- A step is not `Done` until its acceptance criteria, tests, documentation, and parity entries pass.
- Status updates should ride in feature branches or the closeout feature; avoid status-only commits that
  trigger an extra step-branch CI run.
- Tag names record completed checkpoints; they do not replace this dashboard.
