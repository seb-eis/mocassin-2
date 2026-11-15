# Step 10 Audit Request

> Reviewer: write the evidence, findings, and verdict directly into
> `docs/planning/audits/step-10.md`. You may edit only your own metadata (`Auditor`, `Audit date`, `Record state`), the checklist completion marks and inline verification notes (not item text), the `Evidence`,
> `Findings`, and `Verdict` sections, and commit only that file with a documentation-only message and
> `[skip ci]`. Do not edit the request scope, checklist item text, resolutions, code, tests, configuration,
> status dashboard, step page, or any other file.

## Request

| Field | Value |
| --- | --- |
| Step | 10 — Job Templates and Supercell Generation |
| Request owner | Step agent (implementor) |
| Orchestrator | — |
| Step agent (implementor) | Step agent (implementor) |
| Audit initiated by | Orchestrator, on request of the step agent |
| Audited commit | `f2a364dd5f96bca2f9a3df5588443f48cb10f61c` |
| Step branch | `step/10-job-templates` |
| Step tag | — |
| Closeout CI run | [`43699356371`](https://github.com/seb-eis/mocassin-2/actions/runs/43699356371) — green at `f2a364dd5f96bca2f9a3df5588443f48cb10f61c` |
| Closeout CI head SHA | `f2a364dd5f96bca2f9a3df5588443f48cb10f61c` |
| Main SHA at freeze | `ca9a98fe8c7756ccff7fb8bfa1e6fad7aef906a5` |
| Merge-tree CI | Not required; `main` `ca9a98f` is an ancestor of the audited commit |
| Closeout evidence | `docs/planning/closeout/step-10.json` |
| Platform CI | Not run (Ubuntu-only policy; no platform-specific code) |
| Audit date | — |
| Auditor | — |

Step page: [`Step 10 — Job Templates and Supercell Generation`](../steps/step-10-job-templates-and-supercells.md)

> Orchestration: the orchestrator spawns a fresh auditor when the step agent has frozen the step branch and
> completed the audit request, and relays the verdict and owner decisions. The step agent applies fixes on a new
> feature branch, reruns closeout CI, adds resolutions without altering reviewer text, and asks the orchestrator
> for re-audit until the auditor no longer identifies issues. The orchestrator does not fix findings or edit the
> audit record. The human owner approves the final verdict, merge, and tag.

## Step summary

| Field | Value |
| --- | --- |
| Phase | C. Compilation |
| Depends on | 06, 08, 09 |
| Delivers | Job template collections/configurations, deterministic job expansion, and doping-aware supercell generation with charge compensation. |

## Step-specific acceptance checklist

Copy the acceptance criteria from the step page, preserving their wording:

- [ ] The ceria KMC sample expands to the expected number of jobs with deterministic IDs.
- [ ] For a 10×10×10 ceria cell and 1 % Ce doping, the applied counts and vacancy compensation are correct.
- [ ] A requested fraction that cannot be realized is adjusted downward and reported.
- [ ] An impossible charge compensation fails with `JOB012`.
- [ ] The lattice preview echoes the request, reports requested/applied counts, per-site remaining populations,
  total charge before/after, and neutrality, and matches the generated lattice's report for the same request.
- [ ] The preview returns `null` plus diagnostics for an unknown doping, a non-finite or out-of-range
  fraction, an invalid supercell, or impossible compensation; it never returns a partial preview.
- [ ] The job-count preview reports the default, effective, and total job counts without generating lattices.
- [ ] Same job descriptor + seed produces an identical lattice across runs and OSes, and the documented
  job-seed/lattice-seed/sampler golden vectors match.
- [ ] Changing a simulation-only setting does not change the lattice for the same job.
- [ ] A same-site sequential doping applies to the particles remaining after earlier dopings.
- [ ] A non-compensated charged lattice (including a mixed compensated/non-compensated config) is permitted
  with `JOB016` when `IsChargeNeutral` is false; an unrealizable/unbalanced compensated pair is `JOB012`.
  `IsChargeNeutral` uses `1e-6 × max(1, total_positions)` and never fails a pre-existing base charge by itself.
- [ ] `validate` reports structural `JOB001`–`JOB008`, the `JOB009` reference check, `JOB015`, `JOB017`,
  `JOB018`, and `JOB020` without a compiled model; `JOB009`'s removes-all check and the other compiled-bound
  codes require the build stage.
- [ ] The 99,999-job cap and the structural count caps fail with `JOB019`/`JOB020`.
- [ ] Optimizers remove only start options at every P1 image of the target site, never lattice particles, and
  clear dual-DOF only under the documented all-masks rule.
- [ ] Documentation written

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
$ git status -sb
  ## step/10-job-templates...origin/step/10-job-templates
  (clean)

$ git log --oneline -4
  f2a364d docs: record step-10 feature 8 squash commit [skip ci]
  057a85c step-10: complete documentation, parity, and closeout
  cd1e830 step-10: add selection optimizers and build preview
  a95511e step-10: add shared lattice and job-count preview service

$ dotnet restore Mocassin.slnx
  exit 0

$ dotnet build Mocassin.slnx -c Release
  Build succeeded. 0 Warning(s), 0 Error(s); exit 0

$ ./tools/verify.sh all --require
  exit 0
  .NET: build 0 warnings; 1,394 tests passed across nine test projects
    (App 1, Results 1, Contracts 15, Cli 16, Symmetry 223, Core 683, Packages 358,
     Compilation 96, Architecture 1); dotnet format clean
  Public API and dependency gates: build succeeded; ApiCompat clean for nine projects;
    NuGet licence/vulnerability audit clean
  Rust: cargo fmt --check, clippy -D warnings, and workspace tests clean
  Python: 3 passed
  Documentation: math OK (157 files), links OK (160 files), Zensical strict "No issues found",
    DocFX 0 warnings/0 errors, rustdoc clean, site built in site/
  Contracts and policy: 31 tests OK; SPDX headers in 522 files; no secret patterns in
    874 tracked files; cargo-deny advisories/bans/licenses/sources ok
  Closeout gate completed with required checks enabled.

$ git push origin step/10-job-templates
  a146edb..f2a364d  step/10-job-templates -> step/10-job-templates

$ CI workflow_dispatch (platforms=ubuntu, force_all=true)
  run 43699356371, head_sha f2a364dd5f96bca2f9a3df5588443f48cb10f61c, completed/success

$ python tools/check_closeout_evidence.py docs/planning/closeout/step-10.json
  Closeout evidence: OK
```

## Known notes for the auditor

- Feature 7 was squash-merged as `cd1e830`; feature 8 was squash-merged as `057a85c`; `f2a364d` records the
  feature 8 squash commit in the feature log. The feature branch
  `feature/step-10-job-templates/documentation-parity-and-closeout` was deleted locally and remotely after the
  squash.
- `main` `ca9a98fe8c7756ccff7fb8bfa1e6fad7aef906a5` was already an ancestor of the frozen commit; no merge-tree
  run was needed. Only documentation and audit-record commits follow the audited commit.
- The closeout CI run is the single Ubuntu dispatch for this step. No per-feature CI ran and no replacement
  closeout run was dispatched.
- Platform CI (Windows/macOS) was not run. Step 10 adds managed C# compilation/lattice code with no P/Invoke,
  native library, RID-specific package, `System.Runtime.InteropServices`, GUI project, installer, or
  `cfg(windows)`/`cfg(target_os)` branch; path and case handling is not touched.
- Known limitations recorded on the step page:
  - The GUI documentation describes the intended Step 25/26 editor surfaces as planned; no editor UI ships in
    Step 10.
  - `build-preview-v1` is an interim report shape until Step 12 formalises the build report.
  - The job-template guide links to the CLI reference page without a fragment because the repository link
    checker and Zensical derive different heading slugs for `mocassin build --preview` (the `--` token).
  - `build --preview` performs load, validation, compilation, expansion, and generation but never opens or
    writes a `.msl`; the full build command belongs to Step 12.
  - The selection dual-DOF rule, the `IsChargeNeutral` tolerance, `JOB016` semantics, and the 99,999-job cap are
    documented design decisions with tests, not silent corrections.
- An earlier DocFX run reported three `InvalidAssemblyReference` warnings for MessagePack against stale build
  outputs. A clean `dotnet restore` plus Release build cleared them; the recorded gates are green.

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
> planner, implementor, or orchestrator.

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

The step agent completes this section before the orchestrator presents the approval request. It must cover the
full history, not only the latest round.

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

## Audit rounds

Append each new round below. Never overwrite an earlier round.

### Audit round 1

#### Independence and frozen state

#### Commands and results

#### Findings

#### Verdict
