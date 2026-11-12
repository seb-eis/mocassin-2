# Step 07 Audit Request

> Reviewer: write the evidence, findings, and verdict directly into
> `docs/planning/audits/step-07.md`. You may edit only your own metadata (`Auditor`, `Audit date`,
> `Record state`), the checklist completion marks and inline verification notes (not item text), the `Evidence`,
> `Findings`, and `Verdict` sections, and commit only that file with a documentation-only message and
> `[skip ci]`. Do not edit the request scope, checklist item text, resolutions, code, tests, configuration,
> status dashboard, step page, or any other file.

## Request

| Field | Value |
| --- | --- |
| Step | 07 — Validation, Diagnostics, and Model Statistics |
| Request owner | Step agent |
| Orchestrator (step agent) | Step 07 implementor |
| Audit initiated by | Human owner |
| Audited commit | `4d0fa537f6239ffd61fbcb464b9eb9d1ce876d54` |
| Step branch | `step/07-validation-and-diagnostics` |
| Step tag | — |
| Closeout CI run | [`43115061587`](https://github.com/seb-eis/mocassin-2/actions/runs/43115061587) — green at `4d0fa537f6239ffd61fbcb464b9eb9d1ce876d54` |
| Closeout CI head SHA | `4d0fa537f6239ffd61fbcb464b9eb9d1ce876d54` |
| Main SHA at freeze | `5b1eb7496f856660f90f4bb358bd3cbd1d95165d` |
| Merge-tree CI | Not required; `main` did not move after branch creation |
| Closeout evidence | `docs/planning/closeout/step-07.json` |
| Platform CI | Not run (Ubuntu-only policy; no platform-specific code) |
| Audit date | — |
| Auditor | — |

Step page: [`Step 07 — Validation, Diagnostics, and Model Statistics`](../steps/step-07-validation-and-diagnostics.md)

> Orchestration: the step agent (the orchestrator) freezes the step branch, requests the auditor, applies fixes
> autonomously on a new feature branch, reruns closeout CI, adds resolutions without altering reviewer text,
> and requests re-audit until the auditor no longer identifies issues. The human owner approves the final
> verdict, merge, and tag.

## Step summary

| Field | Value |
| --- | --- |
| Phase | B. Model definition |
| Depends on | 03, 04, 05, 06 |
| Delivers | A load/model/cross-domain/feasibility validation pipeline, stable diagnostic catalog, model statistics, and the headless `mocassin validate` command. |

## Step-specific acceptance checklist

Copy the acceptance criteria from the step page, preserving their wording:

- [ ] The ceria sample validates with zero errors and the warning set recorded in its fixture.
- [ ] Every code emitted by the pipeline and the stage-2 validators has a catalog entry. Step 07 adds a
      per-code test with expected severity, path, and hint for every code it emits and records existing Step
      03–06 tests for their severity coverage.
- [ ] The catalog has no duplicate or undocumented codes.
- [ ] Validation never mutates the project; model and document hashes are unchanged.
- [ ] Diagnostics are deterministically sorted and deduplicated.
- [ ] `mocassin validate --json` validates against the `validation-v1` schema, including load-failure reports.
- [ ] Exit codes match the contract, including warnings-as-errors, parse failures, and I/O versus validation
      errors.
- [ ] `--stats` is opt-in and deterministic. Statistics are `null` only when not requested, the document did
      not load, the model stage failed, or a preview failed; `--stats` never changes diagnostics or `valid`.
- [ ] Ceria validation completes in `< 200 ms`; ceria `--stats` completes in `< 2 s`.
- [ ] The GUI contract can navigate from every diagnostic `ModelPath` to an object/property. Step 07 covers the
      path contract and Step 07 diagnostics; the interactive verification is completed in Steps 24–25.
- [ ] Documentation written.

## Standard audit checklist

- [ ] Required tests from the step page pass.
- [ ] Public/protected XML documentation policy is followed.
- [ ] Documentation, status dashboard, feature log, and parity matrix are current.
- [ ] Closeout CI is green at the audited commit.
- [ ] Closeout evidence matches the audited commit and the merge candidate.
- [ ] If `main` moved after the freeze, a merge-tree CI run or a re-audit is recorded.
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
# On step/07-validation-and-diagnostics at 4d0fa537f6239ffd61fbcb464b9eb9d1ce876d54
./tools/verify.sh all --require
# Passed: .NET build/test/format, API compatibility, Rust, Python, docs (Zensical/DocFX/rustdoc),
# contracts, process consistency, SPDX, secret scan, cargo-deny.
python tools/check_closeout_evidence.py docs/planning/closeout/step-07.json
# Closeout evidence: OK (after the evidence commit)
python tools/check_process_consistency.py
# Process consistency: OK
python tools/check_docs_links.py
# Documentation links OK (146 files scanned).
zensical build --strict
# No issues found
```

Closeout CI run `43115061587` is the single manual Ubuntu dispatch for this step and is green at the audited
commit. No per-feature CI ran; no Windows/macOS dispatch was required because the step adds no
platform-specific code.

## Known notes for the auditor

- Feature squash sequence: `validation-core` (`24d9f58`), `diagnostic-catalog` (`47ae73f`),
  `model-statistics` (`ba4e2a3`), `cross-domain-validation` (`010562f`), `validate-cli` (`0ca9bd0`),
  `diagnostics-docs-and-fixtures` (`06568fe`), closeout (`4d0fa53`).
- Design record: `docs/planning/reviews/step-07.md` is `Ready for implementation` at `31b53e8` (round 4,
  record commit `2d01f3e`, human approval 2026-11-11).
- Recorded implementation deviations on the step page §12: `DiagnosticDescriptor` lives in
  `Mocassin.Core.Diagnostics` because the reviewed Core/Symmetry catalogs cannot reference a Packages type;
  `model-statistics` was implemented before `cross-domain-validation` so the budget checks consume the same
  estimates.
- `SIM040`/`SIM041` budget checks run regardless of `--stats` so the option cannot change diagnostics; statistics
  are materialized only when requested.
- The performance test measures the in-process pipeline on the ceria fixture (targets `< 200 ms` and `< 2 s`).
  Process startup is excluded and is visible in the CLI wall time.
- Accepted design Notes n1–n4 (advisory budgets, stage semantics, hint coverage, sizing) remain non-blocking
  as recorded in the design review.
- Follow-ups: Step 08 owns authoritative rule counts; Steps 09–12 own build-stage checks; Steps 24–25 own the
  GUI navigation and statistics panels; Step 30 completes the full CLI reference.

## Findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| — | — | — |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. The auditor writes the findings and verdict
directly. The step agent adds resolutions in the separate `Resolution status` section without altering the
auditor's findings, evidence, or verdict.

## Record state

**State:** Ready for audit

`Re-audit required` is a record state, not a verdict.

## Reviewer independence

> I am a fresh context. I did not author or implement the reviewed work and have no shared conversation with the
> planner or implementor.

## Verdict

- [ ] Passed
- [ ] Passed with notes
- [ ] Fixes required
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

- [x] Requested decision and frozen commit recorded
- [x] Every review/audit round recorded (round, date, commit, reviewer, verdict)
- [x] Every finding recorded (ID, severity, issue, evidence, resolution, fix commit, status)
- [x] Accepted notes and waivers recorded with rationale
- [x] Residual risks, deferred items, and follow-up owners recorded
- [x] Explicit no-Blocker/no-Major statement, or the exact exceptions being waived
- [x] Final CI and documentation-build results recorded

Round history:

| Round | Date | Commit | Reviewer | Verdict |
| --- | --- | --- | --- | --- |
| Design 1 | 2026-11-11 | `dc73ee4` | Fresh planning auditor | Changes required |
| Design 2 | 2026-11-11 | `4ddd571` | Fresh planning auditor | Changes required |
| Design 3 | 2026-11-11 | `60437ca` | Fresh planning auditor | Ready with notes |
| Design 4 | 2026-11-11 | `31b53e8` | Fresh planning auditor | Ready for implementation |

Findings and resolutions:

| Finding | Severity | Issue | Resolution | Commit | Status |
| --- | --- | --- | --- | --- | --- |
| M1–M6 | Major | CLI/report failure paths, load ownership, Build extension, feasibility overlap, statistics nullability, manifest/schema | Resolved in the reviewed design | `4ddd571`, `60437ca`, `31b53e8` | Closed |
| m1–m9, R2-M1, R2-m1–R2-m7, R3-m1 | Minor/Major | Catalog types, overflow, sources, cancellation, hashes, gates, cell counts, `ENM018`, load-kind propagation, schema path, mixed suppression | Resolved in the reviewed design | `4ddd571`, `60437ca`, `31b53e8` | Closed |
| n1–n4 | Note | Budget rationale, stage semantics, hint coverage, sizing | Accepted as non-blocking design notes | `4ddd571`, `31b53e8` | Accepted |

## Human approval

- [ ] Accepted: Passed
- [ ] Accepted: Passed with notes
- [ ] Fixes required
- [ ] Waived

Approved by:
Date:

## Audit rounds

Append each new round below. Never overwrite an earlier round.

### Audit round 1

#### Independence and frozen state

#### Commands and results

#### Findings

#### Verdict
