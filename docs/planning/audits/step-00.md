# Step 00 Audit Request

## Request

| Field | Value |
| --- | --- |
| Step | 00 — Repository, Toolchain, and CI |
| Request owner | Step agent |
| Audit status | **Ready for audit** |
| Audited commit | `47a9a90` (`step-00` tag) |
| Step branch | `step/00-foundation` — merged and deleted |
| Step tag | `step-00` |
| CI run | [41733064540](https://github.com/seb-eis/mocassin-2/actions/runs/41733064540) — green, Ubuntu + Windows + macOS |
| Audit date | — |
| Auditor | — |

Step page: [`Step 00 — Repository, Toolchain, and CI`](../steps/step-00-repository-toolchain-and-ci.md)

The audit covers the step merge commit and the `step-00` tag, not later documentation-only main commits.

## Step summary

| Field | Value |
| --- | --- |
| Phase | A. Foundations |
| Depends on | — |
| Delivers | A reproducible monorepo skeleton with .NET 10, Rust, CI on three operating systems, the decision records that constrain later steps, and the initial standalone documentation site skeleton. |

## Audit scope

- [ ] Repository skeleton, licences, `NOTICE.md`, and absence of copied original source
- [ ] .NET solution, projects, smoke tests, formatting, and warning gates
- [ ] Rust workspace, crates, smoke tests, fmt/clippy, and CLI stub
- [ ] Python placeholder package, pytest smoke test, wheel/sdist build
- [ ] Contracts manifest, generator/validator, drift check, and public-API baseline
- [ ] Zensical docs skeleton, required sections, strict build, and planning exclusion
- [ ] DocFX/rustdoc API integration and API navigation
- [ ] CI matrix, branch triggers, macOS-on-main policy, docs/contracts/security jobs
- [ ] Step status, feature log, parity entries, branch deletion, and `step-00` tag
- [ ] Public/protected XML documentation quality
- [ ] No stale placeholders, unrelated changes, or unapproved scope

## Step-specific acceptance checklist

- [ ] `dotnet build -c Release` succeeds from a clean clone on all three OSes.
- [ ] `dotnet test` runs and passes the smoke tests.
- [ ] `cargo build --workspace` and `cargo test --workspace` succeed on all three OSes.
- [ ] `dotnet format --verify-no-changes`, `cargo fmt --check`, and `cargo clippy -- -D warnings` pass.
- [ ] CI is green on `step/*` integration-branch pushes (Ubuntu + Windows); see the step-branch run history.
- [ ] CI is green on the initial `main` push after the step merge (full Ubuntu + Windows + macOS matrix): run `41733064540`.
- [ ] `README.md` describes prerequisites and the exact build/test/docs commands.
- [ ] `zensical build --strict` succeeds and the skeleton contains every required top-level section.
- [ ] The docs CI job is green and `docs/planning/` is absent from the built site.
- [ ] `contracts/manifest.json` exists, generated checks are current, and the CI drift check passes.
- [ ] The manifest defines the canonical signature-projection layouts and the generator emits the initial fixture/vector scaffolding; signed-type vectors are added with the types in Steps 08/09/11.
- [ ] Public-API baseline files and the `ApiCompat` CI check are in place.
- [ ] The repository contains no copied original source files.
- [ ] Documentation written

## Standard audit checklist

- [ ] Required tests from the step page pass.
- [ ] Public/protected XML documentation policy is followed.
- [ ] Documentation, status dashboard, feature log, and parity matrix are current.
- [ ] CI is green at the audited commit.
- [ ] Format/version/fixture changes are documented and tested.
- [ ] Security, dependency, licence, and secret-scan gates pass.
- [ ] No unrelated changes or unapproved scope.
- [ ] Branch, tag, and status hygiene are correct.

## Evidence

Commands run:

```text

```

## Known notes for the auditor

- Step 00 was completed before the standalone audit policy existed.
- `pwsh` is not required locally; `.ps1` scripts should be assessed against the Windows CI path and the
  documented Windows support intent.
- The generated `.NET`/Rust API roots use redirect pages to concrete DocFX/rustdoc entry pages. Assess whether
  that is acceptable discovery/UX and whether the narrative API landing page links remain correct.
- The main CI run includes a macOS validation; step-branch CI intentionally excludes macOS.

## Findings

| Severity | Finding | Evidence | Resolution |
| --- | --- | --- | --- |
| — | — | — | — |

## Verdict

- [ ] Passed
- [ ] Passed with notes
- [ ] Fixes required
- [ ] Re-audit required
- [ ] Waived

Follow-up owner:
Follow-up issue/step:
