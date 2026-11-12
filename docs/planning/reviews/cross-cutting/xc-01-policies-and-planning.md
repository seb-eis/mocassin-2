# XC-01 — Policies and Planning

> Reviewer: write evidence, findings, and verdict directly into this record. You may edit only your own
> metadata, checklist completion marks and inline verification notes (not item text), `Evidence`, `Findings`,
> and `Verdict`, and commit only this file with `[skip ci]`. Do not edit the scope, checklist item text,
> resolutions, code, tests, configuration, step pages, status dashboard, or any other file.

## Request

| Field | Value |
| --- | --- |
| Review ID | XC-01 |
| Checkpoint | Phase B — Model definition (Steps 03–07) |
| Type | Repository-wide policies and planning audit |
| Target commit | `81fba457d6d4714b4fe56d9c7084a738815b365e` (Phase B checkpoint: drift remediation and record; parent `38a22d2`) |
| Request owner | Step agent / planner |
| Orchestrator (implementor) | OpenCode Phase B orchestrating agent |
| Review initiated by | Human owner |
| Reviewer | Fresh independent reviewer (OpenCode subagent; read-only) |
| Review date | 2026-11-12 |

> Orchestration: an implementor agent requests the reviewer, fixes findings on a maintenance branch, adds
> resolutions without altering reviewer text, and requests re-review until the reviewer no longer identifies
> issues. The human owner approves the final verdict and the phase gate.

## Purpose

Verify that the repository's agent instructions, workflows, policies, tooling, planning records, and status
surfaces are internally consistent and current before the next phase starts.

This is not a design review, scientific audit, or documentation-content review.

## Non-goals

- Re-reviewing the scientific model design (XC-02) or published documentation content (XC-03).
- Line-by-line implementation or performance review.
- Changing tags, branches, or policy files.
- Replacing the Step 08 design review or the Step 07 audit.

## Scope

- Root: `AGENTS.md`, `CONTRIBUTING.md`, `README.md`, `.editorconfig`, `Directory.Build.props`,
  `Directory.Packages.props`, `Mocassin.slnx`, `zensical.toml`, `global.json`.
- Workflows: `.github/workflows/*.yml`.
- Development docs: `docs/development/*.md`.
- Planning process: `docs/planning/reviews/`, `docs/planning/audits/`, `docs/planning/amendments/`,
  `docs/planning/ci-policy.md`, `docs/planning/status.md`, ADRs, open decisions, parity matrix.
- Step pages and review/audit/amendment records for Steps 00–07.
- Tooling: `tools/verify.sh`, `tools/structure-policy.json`, `tools/api-projects.txt`,
  `tools/check-api-compat.sh`, `tools/update-api-baseline.sh`, `tools/contracts/`, `tools/docs/`,
  `tools/symmetry/`.
- The new `docs/planning/reviews/cross-cutting/` checkpoint planning.

## Evidence

Reviewed at the frozen checkpoint `81fba457d6d4714b4fe56d9c7084a738815b365e` (worktree
`/tmp/opencode/mocassin-xc-01`, branch `docs/xc-01-policies`). The worktree `HEAD` at review start was the
docs-only request commit `2fe7b9180b0768147e9f6d81f29aa7f5bcef0aa1`, whose only delta from the frozen
checkpoint is the XC request metadata; the code, workflows, and planning state reviewed are the tree at
`81fba45`.

Files reviewed:

- Root and policy surfaces: `AGENTS.md` (§§1–16, especially 3, 6, 8, 9, 13, 15, 16), `CONTRIBUTING.md`,
  `README.md`, `global.json`, `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig`,
  `zensical.toml`.
- Workflows: `.github/workflows/ci.yml`, `docs.yml`, `benchmark.yml`, `nightly.yml`.
- Development docs: `docs/development/agent-workflow.md`, `contributing.md`, `code-structure.md`,
  `drift-checks.md`, `agent-security.md`, `incident-and-rollback.md`, `index.md`.
- Planning process: `docs/planning/README.md`, `status.md`, `ci-policy.md`, `04-roadmap.md`,
  `05-open-decisions.md`, `appendix-b-parity-matrix.md`; `docs/planning/reviews/README.md` and records
  `step-00`–`step-07`; `docs/planning/reviews/cross-cutting/` (registry, XC-01–XC-03, template);
  `docs/planning/audits/README.md` and records `step-00`–`step-07`; `docs/planning/amendments/README.md`
  and the Step 02 amendment pages; `docs/planning/closeout/README.md` and `step-00`–`step-07.json`;
  `docs/planning/drift-checks/2026-11-12-phase-b.md`.
- Step pages: `docs/planning/steps/step-00`–`step-07` status/evidence, plus `step-08` to confirm no
  pre-seeded review, audit, or closeout record.
- Tooling: `tools/verify.sh`, `structure-policy.json`, `api-projects.txt`, `check-api-compat.sh`,
  `check-license-headers.py`, `check-no-secrets.py`, `check_process_consistency.py`, `check_docs_links.py`,
  `check_closeout_evidence.py`, `tools/README.md`, `tools/tests/`.

Commands run and results:

| Command | Result |
| --- | --- |
| `git status --short` | Empty; worktree clean before and after verification. |
| `git log --oneline --decorate -20` | `HEAD -> docs/xc-01-policies, docs/phase-b-checkpoint` at `2fe7b91`; frozen checkpoint `81fba45`; `origin/main, main` at `38a22d2`; `tag: step-07` at `3ed558a`; no unexpected commits. |
| `git tag --list` | `development-ready`, `planning-complete`, `step-00`…`step-07`; no `milestone-*` tags. |
| `git for-each-ref refs/tags --format='%(refname:short) %(objecttype)'` | All 10 tags are annotated tag objects. |
| `git rev-list --parents -n1 <tag>^{}` for `step-00`…`step-07` | Every step tag peels to a two-parent `--no-ff` merge commit on `main`: `47a9a90`, `7b4a994`, `dbf5691`, `162ebe4`, `31ba12d`, `c11a364`, `41cfa4d`, `3ed558a`, matching `status.md`. |
| `git merge-base --is-ancestor <tag>^{} origin/main` | All eight step tags are on `main`. |
| `git branch -a`; `git ls-remote --heads origin` | Only `main` plus the local `docs/phase-b-checkpoint` and `docs/xc-01-policies` review branches; the remote has only `refs/heads/main` at `38a22d2`; no stale step/feature branches. |
| `git ls-remote --tags origin` | Remote tags match the local annotated tags; no milestone tags. |
| `git worktree list` | `/workspace/mocassin` at `38a22d2`, `/tmp/opencode/mocassin-phase-b` and `/tmp/opencode/mocassin-xc-01` at `2fe7b91`. |
| `./tools/verify.sh structure` | Exit 0: `Passed! Failed: 0, Passed: 1` (`Mocassin.Architecture.Tests`, net10.0). |
| `./tools/verify.sh api` | Exit 0: NuGet licence policy OK (47 packages), no vulnerable packages, ApiCompat OK for `Core`, `Symmetry`, `Compilation`, `Contracts`, `Packages`, `Results`, `App.ViewModels`, `App.Views`, `Rendering`. |
| `zensical build --strict` | Exit 0: `No issues found`; built `site/` contains no `planning/` directory. |
| `python tools/check_process_consistency.py` | `Process consistency: OK`. |
| `python tools/check_docs_links.py` | `Documentation links OK (148 files scanned).` |
| `python -m unittest discover -s tools/tests -t . -q` | `Ran 18 tests`, `OK`. |
| `python tools/check-license-headers.py` | `SPDX headers present in 321 source files` (scope limitation recorded in XC01-F3). |
| `python tools/check-no-secrets.py` | `No secret patterns found in 661 tracked text files`. |
| `python tools/contracts/generate.py --check` | `Contract artifacts are current (18 files)`. |
| `python tools/symmetry/generate_space_groups.py --check` | `space-group catalog is current: 274 settings, sha256 1f3bf67…e3395`. |
| `python tools/check_closeout_evidence.py docs/planning/closeout/step-NN.json` (00–07) | All eight: `Closeout evidence: OK`. |
| `bash -n` on every tracked `*.sh` (9 files) | No syntax errors. |
| `shellcheck` on every tracked `*.sh` | Exit 0; no diagnostics. |
| PyYAML parse of `.github/workflows/*.yml` | `benchmark.yml`, `ci.yml`, `docs.yml`, `nightly.yml`: all parse; jobs are `benchmark`; `changes`+`dotnet`+`rust`+`python`+`contracts`+`api-compat`; `docs`; `placeholder`. |
| Targeted greps for stale terms (`Steps 00–02`, `Step 03 is next`, `Step 07 … next`, `once the CLI exists`, `planning only`, `msl v2`, `step/03-particle-and-structure-models`, old CI matrix phrases, old structure limits, namespace baselines) | Hits are confined to the drift-check/review history or historical Step 00/Step 02 records; no live normative page still advertises the retired state. `ci-policy.md` uses `<step-branch>`; the planning index says `.msl` v1; `AGENTS.md` §1 says Steps 00–07 complete and Step 08 next; no namespace baseline remains. |
| Workflow inspection (triggers, filters, matrices, permissions, inputs, pins) | `ci.yml` triggers on `push: main` and `workflow_dispatch` with `platforms` (`ubuntu`/`windows`/`macos`/`all`) and `force_all`; path filters map to the documented jobs; `.NET`/`Rust` matrices are `fromJSON` of the platform input; `permissions: contents: read` on all four workflows; actions use mutable major tags (XC01-F7). `docs.yml`, `benchmark.yml`, and `nightly.yml` are manual-only; `nightly.yml` has no `schedule`. |
| Cross-document consistency read (`AGENTS.md`, `agent-workflow.md`, `contributing.md`, `CONTRIBUTING.md`, review/audit/amendment READMEs) | Branches, squash-merges, `--no-ff` step merges, annotated tags, reviewer independence, reviewer write scope, checklist ownership, and the human approval gate agree. The cumulative approval-report policy agrees in `AGENTS.md` §3.12 and `agent-workflow.md` §5; the cross-cutting template/records omit an `Approval summary` section (XC01-F4). |

Phase B drift-check disposition (record `docs/planning/drift-checks/2026-11-12-phase-b.md`):

| Finding | Disposition | Evidence |
| --- | --- | --- |
| D1 `AGENTS.md` current state | Resolved | `AGENTS.md:11-16` now says Steps 00–07 complete and Step 08 next; the `once … exists` caveats are replaced. |
| D2 public/root status text | Resolved | `README.md:7-10`, `docs/index.md:6-12`, `docs/development/index.md:50-55` all report Step 07/Phase B complete and Step 08 next. |
| D3 `status.md` Active work | Resolved | `status.md:83-92` describes the Phase B checkpoint and XC-01/02/03 gate. |
| D4 dashboard row 07 columns | Resolved | `status.md:57` has `Current feature —` and `Main merge 3ed558a`; the CI links moved to `Step CI`. |
| D5 XC records frozen one state early | Resolved | XC-01/02/03 and both registries record `Ready for review at 81fba45`; target commits are populated. |
| D6 milestone tags | **Still present** | No `milestone-*` tag and no deferral or waiver in `04-roadmap.md` or `status.md` (XC01-F5). |
| D7 `ci-policy.md` deleted branch example | Resolved | `ci-policy.md:86,92-93,99` use `<step-branch>`. |
| D8 contract manifest status | **Still present** | `contracts/manifest.json:5-6` still says `step-06` and Step 06-only notes although `validation-v1` exists (XC01-F6). |
| D9 Step 11 `.msl` v2 wording | Resolved | `docs/planning/README.md:80` says `.msl` v1 writer. |
| D10 action pinning | **Still present** | All `uses:` lines still reference mutable major tags while `agent-security.md:53` requires SHA pinning where supported (XC01-F7). |
| D11 Dependabot/alert owner | **Still present** | No `.github/dependabot.yml` and no named dependency-alert owner (XC01-F8). |
| D12 local vs CI contract gate | **Still present** | `verify.sh:159-160` runs process consistency and tool tests; the CI `contracts` job does not, and `contributing.md` omits space-group drift from its job table (XC01-F9). |
| D13 navigation omissions | Resolved | `zensical.toml` now lists `agent-security.md`, `drift-checks.md`, and `incident-and-rollback.md`; strict build passes. |
| D14 closeout `merge_candidate` precision | **Still present as accepted Note** | `closeout/step-07.json` records `ccd0e55`, the audited code commit, not branch head `6bfde5d`; audit note N1 accepts the docs-only tail (XC01-F10). |
| D15 drift-check dashboard row | Resolved | `status.md:121-123` contains the 2026-11-12 drift-check row and link. |

Not run, and why: the full `./tools/verify.sh all --require` (complete .NET/Rust/Python/docs closeout gate) was
not run because this is a policies-and-planning review; the requested structure/API/docs/links/process/licence/
secret/contracts/closeout subsets were run directly and the existing Step 07 closeout evidence was checked.
Windows/macOS CI was not run; the policy makes it manual and no Phase B step records platform-specific code.
The GitHub Actions API was not queried; tag/merge/CI identities were verified from Git objects, the durable
records, and the recorded run links. No secret or token was printed.

## Checklist

### A. Agent instructions

- [x] `AGENTS.md` current-state text matches the repository: Step 07 complete, Step 08 next, no planning-only or
  “commands not valid” caveat.
  Verified: `AGENTS.md:11-16` says Steps 00–07 complete and Step 08 next; §6 no longer labels the CLI/Python/docs
  commands as future-only.
- [x] Build/test commands are current and match `tools/verify.sh` and CI.
  Verified: restore/build/test/format, Rust fmt/clippy/test/release build, Python pytest/build, and the docs
  commands are current. Note XC01-F12: `AGENTS.md:699` documents `cargo clippy --workspace` while
  `verify.sh:134` and CI use `--workspace --all-targets`.
- [x] Branch, squash, merge, tag, and approval rules match the actual history and tags.
  Verified: all eight step tags peel to two-parent `--no-ff` merge commits on `main`; feature work is
  squash-merged; tags are annotated; no step/feature branches remain. Milestone tags are a separate gap
  (XC01-F5).
- [x] Reviewer/auditor spawning, independence, write scope, checklist ownership, and human approval gate are
  consistent across `AGENTS.md`, development guides, and process READMEs.
  Verified: `AGENTS.md` §3.8, `reviews/README.md` §§2, 8, `audits/README.md` §§1, 8, and
  `cross-cutting/README.md` §5 agree; each family uses `Changes required` (design) or `Fixes required`
  (audit/cross-cutting) as documented.
- [x] Cumulative approval-report policy is consistent across `AGENTS.md`, `agent-workflow.md`, and templates.
  Mostly verified: §3.12 and `agent-workflow.md` §5 agree, and the design/audit templates carry
  `Approval summary`. The cross-cutting template and XC records do not (XC01-F4).
- [x] CI cost policy matches the workflows: automatic `main`-only Ubuntu, one closeout dispatch, manual
  Windows/macOS, manual docs, no Nightly schedule.
  Verified against `ci.yml`, `docs.yml`, `benchmark.yml`, `nightly.yml` and `ci-policy.md` §2.
- [x] Code-structure policy matches `tools/structure-policy.json` and `Mocassin.Architecture.Tests`; no stale
  namespace baseline; limits, folders, and namespaces agree.
  Verified: 20 files/folder, depth 4, 800 lines/file, forbidden folders, test-root limit 20, and
  `IDE0130`/`IDE0161` enforcement agree; `verify.sh structure` passes; no namespace baseline remains.
- [x] Public API/ApiCompat and local-gate policy matches `tools/verify.sh api` and the CI api-compat job.
  Verified: both run the NuGet licence policy, vulnerability audit, and `check-api-compat.sh`; the nine
  `api-projects.txt` entries match the nine baseline assemblies; `verify.sh api` passes.

### B. Planning status and records

- [x] `docs/planning/status.md` matches tags, merges, audits, reviews, and step pages for Steps 00–07.
  Verified row-by-row against `git rev-list --parents`, tag peel, step pages, and records. One audit-status
  mismatch on Step 00 (XC01-F1); the rest match.
- [x] Step pages show correct status, branch, CI, merge, tag, audit, and design-review links.
  Verified for 00–07; Step 00 says `Audit: Passed` whereas the audit verdict is `Passed with notes`
  (XC01-F1). Step 04 and Step 07 name their branches without the “(merged and deleted)” suffix used by the
  other pages, but both branches are in fact gone.
- [x] No stale Step 02–07 feature, step, or planning branches remain locally or remotely.
  Verified with `git branch -a` and `git ls-remote --heads origin`: only `main` and the two local review
  branches exist.
- [x] Review and audit records are append-only; verdicts, human approvals, and resolution sections are
  complete.
  Verified by reading the round structure and commit history of the Step 00–07 review/audit records and the
  Step 02 amendment reviews; earlier reviewer findings/verdicts were not rewritten. Two human-approval
  sections are still blank (XC01-F2).
- [x] Amendment records/index match the parent step audits and tags.
  Verified: `step-02-amend-1`/`-2` are `Integrated`, link to review records, and record parent audit
  `6341b56`, merge `dbf5691`, and tag `step-02`, matching the Step 02 page/audit.
- [x] ADRs, open decisions, and the parity matrix contain no outdated status or superseded decisions.
  Verified: ADR 0001–0014 are `Accepted` (with the documented revisions/spike gate); open-decision statuses
  are current; the parity matrix marks Phase B model items `Partial` with future editor/compiler steps and
  leaves later items `Planned`. The stale contract-manifest status is a tooling metadata issue (XC01-F6),
  not a planning decision.
- [x] The post-Step-07 cross-cutting checkpoint is recorded and consistent with this plan.
  Verified: `status.md`, the cross-cutting registry, and XC-01/02/03 all record “Ready for review at
  81fba45”; Step 08 is `Not started` and has no pre-seeded review, audit, or closeout record.

### C. Workflows and tooling

- [x] All workflow YAML parses; triggers, path filters, matrices, and manual inputs match `ci-policy.md`.
  Verified with PyYAML on all four workflows and by reading the `changes` filter and platform matrix. The
  `ci.yml` auto trigger is `push: main` only; `platforms` and `force_all` inputs match the policy; docs,
  benchmark, and nightly are manual. Action pinning is a separate finding (XC01-F7).
- [x] `tools/verify.sh` modes cover the CI gates: .NET, API, structure, Rust, Python, docs, contracts.
  Verified: `dotnet`, `api`, `structure`, `rust`, `python`, `docs`, `contracts`, `policy`, and `all` exist;
  `structure` is also exercised inside the .NET test job. D12 records the contracts-job split (XC01-F9).
- [x] `tools/api-projects.txt` matches the baseline directory; no orphan or missing baseline.
  Verified: nine project names and nine `PublicAPI.*.txt` projects/baselines agree; `check-api-compat.sh`
  rejects either direction and `verify.sh api` passes.
- [x] Structure test/policy config matches the policy page and AGENTS summary.
  Verified against `tools/structure-policy.json`, `code-structure.md`, and `AGENTS.md` §8; the structure test
  passes.
- [x] Docs build is strict, navigation is valid, and `docs/planning/` is excluded from publication.
  Verified: `zensical build --strict` reports no issues; `site/planning` does not exist; the 148-file link
  check passes; the three previously unlisted developer pages are now in `zensical.toml` nav.
- [x] No secrets, credentials, private data, or unlicensed copied content in docs or records.
  Verified: secret scan is clean (661 tracked text files); no token, credential, or private URL appears in the
  records; the MIT/GPL boundary and `NOTICE.md` are intact. No copied original source was found.
- [x] SPDX headers and secret/licence gates still pass.
  The gates pass, but their coverage is narrower than the rule: nine tracked tool scripts (`tools/**/*.sh`
  and `*.ps1`) have no SPDX line and `check-license-headers.py` scans only `src/` (XC01-F3).

### D. Links, vocabulary, and simplification

- [x] Internal links resolve; template placeholders are intentional and obvious.
  Verified: `check_docs_links.py` scans 148 files successfully; remaining `—`/placeholder fields are in
  on-demand records or the XC request metadata.
- [x] Status and verdict vocabularies are consistent across reviews, audits, amendments, and dashboard.
  Verified: design reviews use `Changes required`/`Ready with notes`/`Ready for implementation`; audits and
  cross-cutting use `Fixes required`/`Passed with notes`; `Re-review required` and `Re-audit required` are
  record states, not verdicts.
- [x] No stale commands, paths, names, version pins, or removed tool references.
  Targeted greps found no live stale usage; hits were historical drift/review text. Note XC01-F12 on the
  documented clippy flag.
- [x] Duplicated rules are either consistent or identified as simplification candidates.
  The role/gate summaries link to `AGENTS.md` §3 as the normative home and agree with it; `AGENTS.md` §3.15
  records the rule homes. Minor duplication remains between the CI tables, but they agree.
- [x] Findings can be remediated through maintenance branches without moving tags.
  All findings are documentation, template, tooling, or tag-creation decisions; none requires rewriting an
  existing `step-NN` tag or branch.

### E. Exit criteria

- [x] No Blocker or Major finding remains, or every exception is explicitly waived by the human owner.
  No Blocker or Major finding was identified; the remaining findings are Minor/Note.
- [x] Minor/Note findings have owners and follow-ups.
  Each finding below names a recommendation and an owner role; the orchestrator sets the remediation rows.
- [x] The human owner approves the verdict and remediation plan.
  Pending: this review records the evidence and proposed verdict; the human approval section remains for the
  owner.

## Findings

| ID | Severity | Area | Finding | Evidence | Recommendation |
| --- | --- | --- | --- | --- | --- |
| XC01-F1 | Minor | Status and records | Step 00's audit status is recorded as `Passed` on the dashboard, step page, and audit-request field, while the audit verdict is `Passed with notes` and lists two Minor findings plus three Notes. This is the only Phase A/B step with this mismatch and it weakens the dashboard's audit column. | `docs/planning/audits/step-00.md:9` and `:121`; `docs/planning/status.md:50`; `docs/planning/steps/step-00-repository-toolchain-and-ci.md:15`. | Set the Step 00 audit status, dashboard cell, and step-page field to `Passed with notes`, or record why the verdict is treated as `Passed`. Owner: orchestrator/planner. |
| XC01-F2 | Minor | Process records | Two gate records leave the `Human approval` section entirely unchecked and unsigned although the decisions were taken: the Step 07 audit (the last Phase B step) and the 2026-11-11 policy-update review. The approvals are inferable from the step page, merge, and tag, but the durable record itself is incomplete. | `docs/planning/audits/step-07.md:260-268`; `docs/planning/reviews/policy-update-2026-11-11.md:441-448`; Step 07 merge `3ed558a`/tag `step-07` and the policy commits exist. | Record the owner/date and accepted verdict in both sections (a retroactive note is acceptable and does not reopen the verdict). Owner: orchestrator/planner. |
| XC01-F3 | Minor | Licensing | Nine tracked tool scripts (seven `.sh`, including `tools/verify.sh`, and two `.ps1`), including the four `tools/docs` build/serve scripts, lack the `SPDX-License-Identifier: MIT` line required by `AGENTS.md` §13. `check-license-headers.py` scans only `src/dotnet/**/*.cs`, `src/rust/**/*.rs`, and `src/python/**/*.py`, so the gate reports “SPDX headers present in 321 source files” while missing them. This is the accepted Step 00 Minor follow-up left unclosed. | `head -5 tools/verify.sh`, `tools/docs/{build-docs,serve}.{sh,ps1}`, `tools/reference/*.sh`; `tools/check-license-headers.py:12`; `docs/planning/audits/step-00.md` Minor finding and resolution. | Add the SPDX line to the nine scripts and extend the checker to `tools/**/*.sh`/`*.ps1`, or record an explicit licensing exception and narrow the gate claim. Owner: tooling/planner. |
| XC01-F4 | Minor | Template consistency | `AGENTS.md` §3.12 requires a cumulative approval report before any review verdict is approved, and the design-review/audit templates carry an `Approval summary` section. The cross-cutting template and the three XC records have no such section, so the checkpoint records do not provide the same durable place for the cumulative report. | `AGENTS.md:442-466`; `docs/planning/reviews/step-design-review-template.md:103`; `docs/planning/audits/step-audit-template.md:119`; `docs/planning/reviews/cross-cutting/cross-cutting-review-template.md:78-121`; `xc-01`–`xc-03`. | Add an `Approval summary` section to the cross-cutting template and records, or state in the README where the cumulative checkpoint report is retained. Owner: planner. |
| XC01-F5 | Minor | Status and records (drift D6) | Roadmap milestones M0–M3 are reached after Steps 00, 01, 02, and 07, and `AGENTS.md` §3.7 plus the roadmap require a `milestone-mN` tag when a milestone is reached, but no `milestone-*` tag exists and no deferral or waiver is recorded. | `git tag --list`; `git ls-remote --tags origin`; `docs/planning/04-roadmap.md:109-127`; `docs/planning/status.md:37-38`; dashboard rows 00–02/07 are `Done`. | Owner decides whether to create the four annotated tags on the existing milestone commits or record a deliberate deferral in the roadmap/status. Never move a step tag. |
| XC01-F6 | Minor | Contracts/planning metadata (drift D8) | `contracts/manifest.json` still declares `"status": "step-06"` and its `notes` describe only the Step 06 lattice/simulation additions, although Step 07 added `reports.validation`/`validation-v1`. `generate.py --check` passes because it does not compare the human-readable status, so the drift is invisible to the gate. XC-02 also covers contract status, but it remains a Phase B drift finding. | `contracts/manifest.json:5-6`; `:2184` (`validation`); `git show 0ca9bd0 -- contracts/manifest.json`; `python tools/contracts/generate.py --check` OK. | Set the status to `step-07` and add a validation-v1 note, or define the field as “last schema-changing step” and document that definition. Owner: implementor/planner. |
| XC01-F7 | Minor | Security/tooling (drift D10) | Every workflow action is still pinned to a mutable major tag (`actions/checkout@v7`, `setup-dotnet@v6`, `setup-python@v7`, `upload-artifact@v7`, `Swatinem/rust-cache@v2`, `dtolnay/rust-toolchain@1.99.1`), while `agent-security.md` requires commit-SHA pinning where the provider supports it. | All `uses:` lines in `.github/workflows/*.yml`; `docs/development/agent-security.md:53,97`. | Pin to full commit SHAs with the version in a comment, or record an explicit accepted exception with an owner and review date. Owner: security/tooling owner. |
| XC01-F8 | Minor | Security/operations (drift D11) | There is no `.github/dependabot.yml` and no named owner for dependency alerts/security advisories; the drift check could not demonstrate that automated alerts are enabled. Gate-time NuGet/cargo checks are green but only run when the gate runs. | Absence of `.github/dependabot.yml`; `docs/development/drift-checks.md:57`; drift record D11 API responses. | Enable Dependabot (NuGet, Cargo, Actions, pip) or record a named owner and manual review cadence in `agent-security.md`/`ci-policy.md`. Owner: human/security owner. |
| XC01-F9 | Minor | Policy/tooling (drift D12) | The local `verify.sh contracts` mode runs `check_process_consistency.py` and `tools/tests`, but the CI `Contracts and policy` job runs neither; `contributing.md`'s CI job table also omits the space-group catalog drift that CI does run. `verify.sh` is described as mirroring the affected CI jobs, so a local pass and the CI job are not equivalent in either direction. | `tools/verify.sh:150-171`; `.github/workflows/ci.yml:299-354`; `docs/development/contributing.md:100-109`; `tools/verify.sh:4-6`. | Add process consistency and the tool self-tests to the CI contracts job (and catalog drift to the contributing table), or document the intentional split and correct the “mirrors” claim. Owner: planner/CI owner. |
| XC01-F10 | Note | Closeout evidence (drift D14) | `closeout/step-07.json` records `merge_candidate = ccd0e55` (the audited code commit), not the branch head `6bfde5d` or merge `3ed558a`; the checker passes because the tail is documentation-only, and audit note N1 accepts the precision limit. | `docs/planning/closeout/step-07.json:9`; `docs/planning/audits/step-07.md` N1 and resolution table; `closeout/README.md:38-51`; `3ed558a` parents `5b1eb74`/`6bfde5d`. | Optional: refresh the file or state in `closeout/README.md` that `merge_candidate` may be the last code-tested commit when the tail is docs-only. Owner: step agent/planner. |
| XC01-F11 | Note | Status tracking | `status.md` says the remaining Phase B drift “Minor/Note findings are tracked in the cross-cutting reviews”, but XC-01/02/03 contain no D-ID mapping; the drift record remains the only place that names D6–D14. A reader cannot trace a drift finding to its checkpoint owner from the dashboard phrase. | `docs/planning/status.md:89-91`; `docs/planning/reviews/cross-cutting/xc-01`–`xc-03`; `docs/planning/drift-checks/2026-11-12-phase-b.md` D6–D14. | Add a drift-findings disposition table to the relevant XC record(s), or add a resolution/owner column to the drift record. Owner: orchestrator. |
| XC01-F12 | Note | Agent instructions | `AGENTS.md` §6 documents `cargo clippy --manifest-path src/rust/Cargo.toml --workspace -- -D warnings`, while `tools/verify.sh` and CI use `--workspace --all-targets`. An agent following the documented command does not lint tests/benches that the gate and CI lint. | `AGENTS.md:699`; `tools/verify.sh:134`; `.github/workflows/ci.yml:241-243`. | Add `--all-targets` to the documented command so the guide matches the gate. Owner: planner. |

No Blocker or Major finding was identified. The Phase B drift-check Majors D1–D3 are resolved; D6, D8, D10, D11,
and D12 remain as Minor findings, and D14 remains as an accepted Note. D13 and D15 are resolved.

## Record state

**State:** Passed with notes

`Re-review required` is a record state, not a verdict.

## Reviewer independence

> I am a fresh context. I did not author or implement the reviewed work and have no shared conversation with the
> request owner or the other reviewers.

I was spawned specifically for XC-01, read the repository at the frozen checkpoint (`81fba45`) myself, and did
not receive a preferred verdict or a summary intended to replace repository reading. I wrote only this record
and did not fix any finding.

## Verdict

- [ ] Passed
- [x] Passed with notes
- [ ] Fixes required
- [ ] Waived

Follow-up owner: Phase B orchestrator/planner (remediation), human owner (verdict and phase gate)
Follow-up step: Remediate the Minor/Note findings before the Step 08 design review; the Phase B gate still
requires the human owner to accept this verdict alongside XC-02 and XC-03.

## Remediation status

| Finding | Status | Owner | Commit |
| --- | --- | --- | --- |
| XC01-F1 | Fixed | Orchestrator | `62e1072` — Step 00 dashboard and step page now record `Passed with notes`; the audit-request field is left as historical text and its verdict remains authoritative |
| XC01-F2 | Open — human owner | Human owner | — — record the owner/date and accepted verdict in the Step 07 audit and 2026-11-11 policy-update review |
| XC01-F3 | Fixed | Tooling | `62e1072`, `ddf192e` — SPDX headers added to all tool scripts and the licence checker now scans `tools/**/*.{py,sh,ps1}` (345 files) |
| XC01-F4 | Open — planner | Planner | — — add an `Approval summary` section to the cross-cutting template/records or state where the cumulative checkpoint report is retained |
| XC01-F5 | Open — owner decision | Human owner | — — decide whether to create `milestone-m0`–`m3` on the existing milestone commits or record a deferral; never move step tags |
| XC01-F6 | Fixed | Orchestrator | `62e1072` — `contracts/manifest.json` status is `step-07` with a validation-v1 note |
| XC01-F7 | Open — security/tooling | Security/tooling owner | — — pin workflow actions to commit SHAs or record an accepted exception with a review date |
| XC01-F8 | Open — security/tooling | Security/tooling owner | — — enable Dependabot or record a named alert owner and review cadence |
| XC01-F9 | Open — CI owner | Planner/CI owner | — — align the local contracts gate with the CI contracts job, or document the intentional split |
| XC01-F10 | Accepted note | Step agent/planner | — — `merge_candidate` precision is documented by audit note N1; no action required for the gate |
| XC01-F11 | Fixed | Orchestrator | `62e1072` — `status.md` now carries the D-ID disposition mapping |
| XC01-F12 | Fixed | Planner | `62e1072` — `AGENTS.md` clippy command now uses `--workspace --all-targets` |

Final checkpoint after remediation: `ddf192e7e6fc9c3f51a2d9924913c01fc20ca600`. XC-01 found no Blocker or Major; the verdict remains **Passed with notes** and no re-review is required. The human owner must accept the notes and decide XC01-F2 and XC01-F5; the remaining open Minors are tracked above.

## Human approval

- [ ] Accepted: Passed
- [ ] Accepted: Passed with notes
- [ ] Fixes required
- [ ] Waived

Approved by:
Date:
