# Step 06 Audit Request

> Reviewer: write the evidence, findings, and verdict directly into
> `docs/planning/audits/step-06.md`. You may edit only your own metadata (`Auditor`, `Audit date`, `Audit
> status`), the checklist completion marks and inline verification notes (not item text), the `Evidence`,
> `Findings`, and `Verdict` sections, and commit only that file with a documentation-only message and
> `[skip ci]`. Do not edit the request scope, checklist item text, resolutions, code, tests, configuration,
> status dashboard, step page, or any other file.

## Request

| Field | Value |
| --- | --- |
| Step | 06 — Lattice and Simulation Models |
| Request owner | Step agent |
| Orchestrator (step agent) | Step 06 implementor |
| Audit initiated by | Human owner |
| Audit status | Ready for audit |
| Audited commit | `ea6293e` |
| Step branch | `step/06-lattice-and-simulation-models` |
| Step tag | — |
| Closeout CI run | [`43085605850`](https://github.com/seb-eis/mocassin-2/actions/runs/43085605850) — green at `ea6293e` |
| Platform CI | Not run (Ubuntu-only policy; no platform-specific code) |
| Audit date | 2026-11-11 |
| Auditor | — |

Step page: [`Step 06 — Lattice and Simulation Models`](../steps/step-06-lattice-and-simulation-models.md)

## Audit request summary

- Frozen code commit: `ea6293e` on `step/06-lattice-and-simulation-models`.
- Feature sequence: lattice model, persistence, and validation (`bc333f3`); simulation model and validation
  (`1c64c96`); persistence and contracts (`48e1f20`); queries and editing (`cfeb7b6`); samples and docs
  (`46ea087`); closeout (this commit).
- Local closeout gate: `./tools/verify.sh all` passed on the step branch; Zensical strict, DocFX, rustdoc,
  Rust, Python, contracts, API compatibility, and structure gates all passed.
- Closeout CI: one manual Ubuntu dispatch, run `43085605850`, green. Detect changes, .NET, Contracts and policy, and Public API compatibility passed; Rust and Python were path-filtered and skipped.
- Known deferrals: Step 10 owns fraction application, integer truncation, charge-compensation counts, the single
  supercell generator, the lattice/job-count preview service, and effective job counts. Step 11 owns the
  `.msl` writer and `SolverJobFlags` composition. Step 25 owns the GUI editors. Step 07 owns the consolidated
  diagnostic catalog and help pages.
- Accepted design-review residuals: R6-1 (Compilation/Packages carrier for the derived intent) is resolved by
  `SolverJobFlagsMapper` and its tests; R6-2 (data-contract deterministic wording) is corrected in this step;
  R6-3 (`ILatticePreviewService` placement) is Step 10; R6-4 is a process note only.

> Orchestration: the step agent (the orchestrator) freezes the step branch, requests the auditor, applies fixes
> autonomously on a new feature branch, reruns closeout CI, adds resolutions without altering reviewer text,
> and requests re-audit until the auditor no longer identifies issues. The human owner approves the final
> verdict, merge, and tag.

## Step summary

| Field | Value |
| --- | --- |
| Phase | B. Model definition |
| Depends on | 03, 04 |
| Delivers | Occupation exchanges, ordered dopings, and MMC/KMC simulation definitions with defaults and validation. |

## Step-specific acceptance checklist

- [ ] The ceria reduction doping validates at the model level: both deltas are non-zero and opposite in sign,
      and the derived building block is exposed.
- [ ] An isovalent doping without a dependent exchange validates.
- [ ] A dependent exchange with the wrong charge sign is rejected with `LAT009`.
- [ ] Doping priorities are unique, in `0..9999`, and serialize in ascending order; duplicates are `LAT006`.
- [ ] A KMC simulation cannot select an MMC transition, and vice versa (`SIM002`).
- [ ] All original simulation settings are represented: temperature, target MCSP, write-out count, time limit,
      min success rate, job count, RNG seed, execution flags, KMC pre-run/field/normalization/max frequency,
      and MMC break tolerance/sample length/interval/result sample.
- [ ] New simulations use the documented defaults, and every hard range and recommended band has a diagnostic
      and a boundary test.
- [ ] The lattice/simulation sections round-trip through the project JSON context, the canonical model, save,
      and load; dopings preserve priority order and simulations preserve kind-specific settings and
      authored-order transition arrays.
- [ ] Lattice/simulation edits change `ProjectHasher.Compute` and `ComputeDocument`; no section is dropped by
      canonicalization, empty arrays are omitted, and a pre-Step-06 project keeps its canonical hash.
- [ ] The generated project schema accepts the flat lattice/simulation sections and rejects unknown fields;
      fixtures cover empty and populated models.
- [ ] Public API files and the ApiCompat baseline are current.
- [ ] Documentation written.

## Standard audit checklist

- [ ] Required tests from the step page pass.
- [ ] Public/protected XML documentation policy is followed.
- [ ] Documentation, status dashboard, feature log, and parity matrix are current.
- [ ] Closeout CI is green at the audited commit.
- [ ] Windows/macOS runs are recorded, or their absence is accepted because the step has no platform-specific
      code.
- [ ] Format/version/fixture changes are documented and tested.
- [ ] Security, dependency, licence, and secret-scan gates pass.
- [ ] Code structure policy passes; no new root-level or catch-all folders.
- [ ] No unrelated changes or unapproved scope.
- [ ] Branch, tag, and status hygiene are correct.

## Evidence

Commands run:

```text

```

## Known notes for the auditor

- No lattice or job-count preview exists in Step 06 by design; Step 10 owns the single preview service and the
  shared supercell/doping generator.
- The simulation persisted shape is intentionally flat at the top level with a `settings` object. The generated
  schema cannot express a `oneOf` discriminator, so kind-specific fields are optional in the schema and the
  validator (`SIM023`) rejects a mismatched shape.
- Step 06 adds nullable support to `tools/contracts/generate.py` for the simulation settings that use `null`
  (`timeLimitSeconds`, `minSuccessRate`, `rngSeed`, `instruction`, `normalizationEnergyEv`,
  `maxAttemptFrequencyHz`). Existing generated artifacts are unchanged by the feature.
- Step 10 must cross-check the lattice preview against the generated lattice's doping report; Step 06 does not
  claim that cross-check.
- Windows and macOS were not run because Step 06 has no platform-specific code; this is the Ubuntu-only policy.
- `SolverJobFlags` composition is tested in `Mocassin.Packages.Tests`; the `.msl` writer itself is Step 11.

## Findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| — | — | — |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. The auditor writes the findings and verdict
directly. The step agent adds resolutions in the separate `Resolution status` section without altering the
auditor's findings, evidence, or verdict.

## Verdict

- [ ] Passed
- [ ] Passed with notes
- [ ] Fixes required
- [ ] Re-audit required
- [ ] Waived

Follow-up owner:
Follow-up issue/step:

## Resolution status

| Finding | Status | Owner | Commit |
| --- | --- | --- | --- |
| — | — | — | — |

The step agent fills this section after the verdict. Do not alter the reviewer-authored findings, evidence, or
verdict.

## Approval summary

The step agent completes this section before requesting human approval. It must cover the full history, not
only the latest round.

- [ ] Requested decision and frozen commit recorded
- [ ] Every review/audit round recorded (round, date, commit, reviewer, verdict)
- [ ] Every finding recorded (ID, severity, issue, evidence, resolution, fix commit, status)
- [ ] Accepted notes and waivers recorded with rationale
- [ ] Residual risks, deferred items, and follow-up owners recorded
- [ ] Explicit no-Blocker/no-Major statement, or the exact exceptions being waived
- [ ] Final CI and documentation-build results recorded

Round history:

| Round | Date | Commit | Reviewer | Verdict |
| --- | --- | --- | --- | --- |
| — | — | — | — | — |

Findings and resolutions:

| Finding | Severity | Issue | Resolution | Commit | Status |
| --- | --- | --- | --- | --- | --- |
| — | — | — | — | — | — |

## Human approval

- [ ] Accepted: Passed
- [ ] Accepted: Passed with notes
- [ ] Fixes required
- [ ] Waived

Approved by:
Date:
