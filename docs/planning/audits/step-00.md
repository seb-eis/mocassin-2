# Step 00 Audit Request

## Request

| Field | Value |
| --- | --- |
| Step | 00 — Repository, Toolchain, and CI |
| Request owner | Step agent |
| Audit status | **Passed** |
| Audited commit | `47a9a90` (`step-00` tag) |
| Step branch | `step/00-foundation` — merged and deleted |
| Step tag | `step-00` |
| CI run | [41733064540](https://github.com/seb-eis/mocassin-2/actions/runs/41733064540) — green, Ubuntu + Windows + macOS |
| Audit date | 2026-11-04 |
| Auditor | Fresh read-only audit context |

Step page: [`Step 00 — Repository, Toolchain, and CI`](../steps/step-00-repository-toolchain-and-ci.md)

The audit covers the step merge commit and the `step-00` tag, not later documentation-only main commits.

## Step summary

| Field | Value |
| --- | --- |
| Phase | A. Foundations |
| Depends on | — |
| Delivers | A reproducible monorepo skeleton with .NET 10, Rust, CI on three operating systems, the decision records that constrain later steps, and the initial standalone documentation site skeleton. |

## Audit scope

- [x] Repository skeleton, licences, `NOTICE.md`, and absence of copied original source
- [x] .NET solution, projects, smoke tests, formatting, and warning gates
- [x] Rust workspace, crates, smoke tests, fmt/clippy, and CLI stub
- [x] Python placeholder package, pytest smoke test, wheel/sdist build
- [x] Contracts manifest, generator/validator, drift check, and public-API baseline
- [x] Zensical docs skeleton, required sections, strict build, and planning exclusion
- [x] DocFX/rustdoc API integration and API navigation
- [x] CI matrix, branch triggers, macOS-on-main policy, docs/contracts/security jobs
- [x] Step status, feature log, parity entries, branch deletion, and `step-00` tag
- [x] Public/protected XML documentation quality
- [x] No stale placeholders, unrelated changes, or unapproved scope

## Step-specific acceptance checklist

- [x] `dotnet build -c Release` succeeds from a clean clone on all three OSes.
- [x] `dotnet test` runs and passes the smoke tests.
- [x] `cargo build --workspace` and `cargo test --workspace` succeed on all three OSes.
- [x] `dotnet format --verify-no-changes`, `cargo fmt --check`, and `cargo clippy -- -D warnings` pass.
- [x] CI is green on `step/*` integration-branch pushes (Ubuntu + Windows); see the step-branch run history.
- [x] CI is green on the initial `main` push after the step merge (full Ubuntu + Windows + macOS matrix): run `41733064540`.
- [x] `README.md` describes prerequisites and the exact build/test/docs commands.
- [x] `zensical build --strict` succeeds and the skeleton contains every required top-level section.
- [x] The docs CI job is green and `docs/planning/` is absent from the built site.
- [x] `contracts/manifest.json` exists, generated checks are current, and the CI drift check passes.
- [x] The manifest defines the canonical signature-projection layouts and the generator emits the initial fixture/vector scaffolding; signed-type vectors are added with the types in Steps 08/09/11.
- [x] Public-API baseline files and the `ApiCompat` CI check are in place.
- [x] The repository contains no copied original source files.
- [x] Documentation written

## Standard audit checklist

- [x] Required tests from the step page pass.
- [x] Public/protected XML documentation policy is followed.
- [x] Documentation, status dashboard, feature log, and parity matrix are current.
- [x] CI is green at the audited commit.
- [x] Format/version/fixture changes are documented and tested.
- [x] Security, dependency, licence, and secret-scan gates pass.
- [x] No unrelated changes or unapproved scope.
- [x] Branch, tag, and status hygiene are correct.

## Evidence

Commands run on a clean clone at `47a9a90` (`step-00`):

```text
dotnet restore/build/test/format ................ pass (7 smoke tests; 0 warnings/errors)
cargo fmt/clippy/test/build ..................... pass
python -m pytest src/python/tests ............... pass (3)
python -m build src/python ...................... pass (wheel + sdist)
zensical build --strict ......................... pass; no docs/planning in built site
tools/docs/build-docs.sh ........................ pass (DocFX 0 warnings/errors; rustdoc pass)
python tools/contracts/generate.py --check ...... current (17 files)
python tools/check-license-headers.py ........... pass (27 files)
python tools/check-no-secrets.py ................ pass (228 files)
tools/check-api-compat.sh ....................... pass (9 libraries)
```

CI:

- `41733064540` — green on `main` at `47a9a90`, all jobs, Ubuntu + Windows + macOS.
- `41731068135`, `41730146454`, `41727653557` — green step-branch runs.
- `step-00` is an annotated tag on the `--no-ff` merge commit `47a9a90`; `step/00-foundation` is deleted
  locally and remotely.

## Known notes for the auditor

- Step 00 was completed before the standalone audit policy existed. The audit gate was applied retroactively;
  this exception is recorded in the findings and accepted by the human owner.
- `pwsh` is not required locally; `.ps1` scripts should be assessed against the Windows CI path and the
  documented Windows support intent.
- The generated `.NET`/Rust API roots use redirect pages to concrete DocFX/rustdoc entry pages. Assess whether
  that is acceptable discovery/UX and whether the narrative API landing page links remain correct.
- The main CI run includes a macOS validation; step-branch CI intentionally excludes macOS.

## Findings

No blockers or majors. Two Minor findings and three Notes remain; the human owner accepted both Minor findings
on 2026-11-04.

| Severity | Finding | Evidence | Resolution |
| --- | --- | --- | --- |
| **Minor** | Four Step 00 documentation scripts lack the required SPDX header: `tools/docs/build-docs.sh`, `tools/docs/build-docs.ps1`, `tools/docs/serve.sh`, and `tools/docs/serve.ps1`. `AGENTS.md` §13 requires `SPDX-License-Identifier: MIT` on new source files, but `tools/check-license-headers.py` scans only `src/dotnet/**/*.cs`, `src/rust/**/*.rs`, and `src/python/**/*.py`, so CI reports 27/27 and misses them. | `head -5` on each script; `git log --diff-filter=A` shows they were added in Step 00 commit `5fe29a7`. | Accepted by the human owner on 2026-11-04. Follow-up: add the SPDX line after the shebang/header and extend the licence checker to `tools/**/*.sh` and `*.ps1`; no merge blocker. |
| **Minor** | The Step 00 feature log still lists `fix-closeout-audit` as `Awaiting approval` with no squash commit, while the step is `Done` and the branch no longer exists. | Step page feature table on `main`; `git branch -a` and `git ls-remote` show no such feature branch. | Accepted by the human owner on 2026-11-04. Fixed in this record: the row is marked `Superseded` by the closeout commits `0f3252e` and `4f86f8d`; no separate squash commit exists. |
| **Note** | The audit gate was applied retroactively: Step 00 was merged and tagged before the policy existed, and the audit request was created afterward with blank checklist/evidence fields. | Step 00 audit request, status dashboard, and Git history. | Accepted as a documented retroactive exception; no action. |
| **Note** | GitHub Actions are pinned by major-version tags rather than commit SHAs. | `.github/workflows/ci.yml`. | Consider SHA pinning or automated dependency updates in a later hardening step. |
| **Note** | The nightly workflow is intentionally a placeholder, which Step 00 explicitly permits. | `.github/workflows/nightly.yml`. | Enable real nightly suites as the solver, results, and GUI steps land. |

## Verdict

- [ ] Passed
- [x] Passed with notes
- [ ] Fixes required
- [ ] Re-audit required
- [ ] Waived

Follow-up owner: Step agent / human owner
Follow-up issue/step: Step 00 follow-up (SPDX checker coverage and feature-log cleanup); no merge blocker.

The human owner accepted both Minor findings on 2026-11-04, so **Passed with notes** is final.
