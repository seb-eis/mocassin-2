# XC-01 — Repository Policy and Planning Status Audit

> Reviewer: write evidence, findings, and verdict directly into this record. You may edit only your own
> metadata, checklist completion marks and inline verification notes (not item text), `Evidence`, `Findings`,
> and `Verdict`, and commit only this file with `[skip ci]`. Do not edit the scope, checklist item text,
> resolutions, code, tests, configuration, step pages, status dashboard, or any other file.

## Request

| Field | Value |
| --- | --- |
| Review ID | XC-01 |
| Type | Repository-wide policy and planning audit |
| Status | Draft for owner review |
| Target commit | After the Step 07 merge and `step-07` tag, plus a docs-only checkpoint commit |
| Request owner | Step agent / planner |
| Review initiated by | Human owner |
| Reviewer | — |
| Review date | — |

## Purpose

Verify that the repository's agent instructions, workflows, policies, tooling, planning records, and status
surfaces are internally consistent and current before Step 08 starts.

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

Files reviewed:

Commands run:

The reviewer should run at least:

```sh
git status --short
git log --oneline --decorate -10
git tag --list
git branch -a
git worktree list
./tools/verify.sh structure
./tools/verify.sh api
zensical build --strict
```

Use `grep`/`rg` for stale terms, old CI matrices, old limits, old namespace baselines, old statuses, and
retired commands. Parse workflow YAML. Use `bash -n`/`shellcheck` on scripts.

## Checklist

### A. Agent instructions

- [ ] `AGENTS.md` current-state text matches the repository: Step 07 complete, Step 08 next, no planning-only or
  “commands not valid” caveat.
- [ ] Build/test commands are current and match `tools/verify.sh` and CI.
- [ ] Branch, squash, merge, tag, and approval rules match the actual history and tags.
- [ ] Reviewer/auditor spawning, independence, write scope, checklist ownership, and human approval gate are
  consistent across `AGENTS.md`, development guides, and process READMEs.
- [ ] Cumulative approval-report policy is consistent across `AGENTS.md`, `agent-workflow.md`, and templates.
- [ ] CI cost policy matches the workflows: automatic `main`-only Ubuntu, one closeout dispatch, manual
  Windows/macOS, manual docs, no Nightly schedule.
- [ ] Code-structure policy matches `tools/structure-policy.json` and `Mocassin.Architecture.Tests`; no stale
  namespace baseline; limits, folders, and namespaces agree.
- [ ] Public API/ApiCompat and local-gate policy matches `tools/verify.sh api` and the CI api-compat job.

### B. Planning status and records

- [ ] `docs/planning/status.md` matches tags, merges, audits, reviews, and step pages for Steps 00–07.
- [ ] Step pages show correct status, branch, CI, merge, tag, audit, and design-review links.
- [ ] No stale Step 02–07 feature, step, or planning branches remain locally or remotely.
- [ ] Review and audit records are append-only; verdicts, human approvals, and resolution sections are
  complete.
- [ ] Amendment records/index match the parent step audits and tags.
- [ ] ADRs, open decisions, and the parity matrix contain no outdated status or superseded decisions.
- [ ] The post-Step-07 cross-cutting checkpoint is recorded and consistent with this plan.

### C. Workflows and tooling

- [ ] All workflow YAML parses; triggers, path filters, matrices, and manual inputs match `ci-policy.md`.
- [ ] `tools/verify.sh` modes cover the CI gates: .NET, API, structure, Rust, Python, docs, contracts.
- [ ] `tools/api-projects.txt` matches the baseline directory; no orphan or missing baseline.
- [ ] Structure test/policy config matches the policy page and AGENTS summary.
- [ ] Docs build is strict, navigation is valid, and `docs/planning/` is excluded from publication.
- [ ] No secrets, credentials, private data, or unlicensed copied content in docs or records.
- [ ] SPDX headers and secret/licence gates still pass.

### D. Links, vocabulary, and simplification

- [ ] Internal links resolve; template placeholders are intentional and obvious.
- [ ] Status and verdict vocabularies are consistent across reviews, audits, amendments, and dashboard.
- [ ] No stale commands, paths, names, version pins, or removed tool references.
- [ ] Duplicated rules are either consistent or identified as simplification candidates.
- [ ] Findings can be remediated through maintenance branches without moving tags.

### E. Exit criteria

- [ ] No Blocker or Major finding remains, or every exception is explicitly waived by the human owner.
- [ ] Minor/Note findings have owners and follow-ups.
- [ ] The human owner approves the verdict and remediation plan.

## Findings

| ID | Severity | Area | Finding | Evidence | Recommendation |
| --- | --- | --- | --- | --- | --- |
| — | — | — | — | — | — |

## Verdict

- [ ] Passed
- [ ] Passed with notes
- [ ] Fixes required
- [ ] Re-review required
- [ ] Waived

Follow-up owner:
Follow-up step:

## Remediation status

| Finding | Status | Owner | Commit |
| --- | --- | --- | --- |
| — | — | — | — |

## Human approval

- [ ] Accepted: Passed
- [ ] Accepted: Passed with notes
- [ ] Fixes required
- [ ] Waived

Approved by:
Date:
