# Contributing and Review Workflow

This page is the developer-facing contributor guide. The normative branch, approval, and merge rules are in
[`AGENTS.md`](https://github.com/seb-eis/mocassin-2/blob/main/AGENTS.md); the short repository guide is
[`CONTRIBUTING.md`](https://github.com/seb-eis/mocassin-2/blob/main/CONTRIBUTING.md). If these disagree,
`AGENTS.md` wins.

## 1. Branches

| Purpose | Pattern | Example |
| --- | --- | --- |
| Integration/release | `main` | `main` |
| One roadmap step | `step/<NN>-<slug>` | `step/00-foundation` |
| One feature inside a step | `feature/step-<NN>-<slug>/<feature>` | `feature/step-00-foundation/repo-skeleton` |
| Bug fix inside a step | `feature/step-<NN>-<slug>/fix-<description>` | `feature/step-00-foundation/fix-docs-link` |
| Maintenance outside a step | `fix/<slug>` or `docs/<slug>` | `docs/correct-typo` |

Git cannot store both `step/00-foundation` and `step/00-foundation/<feature>` because a ref cannot be both a
file and a directory. The `feature/` prefix keeps the step grouping without that conflict.

`main` is integration/release only. One step is active at a time unless the owner starts parallel work.

## 2. Step workflow

1. Read `AGENTS.md`, the planning README, the status dashboard, the step page, and its dependencies.
2. Propose the complete feature-branch list for the step; the owner approves it once.
3. Create the step branch from current `main` and push it.
4. Create one feature branch from the step branch for each deliverable and push it.
5. Implement only that feature's scope; add the tests named in the step plus regression tests.
6. Update the step page, status dashboard, parity matrix, and documentation in the same feature where
   applicable.
7. Run the local quality gates and report the exact commands and results.
8. Ask for explicit approval before every feature squash merge. The report must include branch, commits, diff
   summary, tests, expected CI, docs/status updates, risks, and known limitations.
9. After approval, squash-merge into the step branch, push the step branch, and delete the feature branch
   locally and remotely.
10. Do not start the next feature until the step-branch CI run is green.

A final closeout feature records the step status, deviations, acceptance evidence, and main merge. The
step-to-`main` merge is `--no-ff` and requires explicit permission. Tags are created only as part of the
corresponding approval.

## 3. Review gates

| Gate | Approval |
| --- | --- |
| Step feature list | Once, at step start |
| Each feature squash | Before every squash into the step branch |
| Step-to-main merge | Before the `--no-ff` merge |
| Tag creation/push | Included in the step/milestone/release approval |
| Release | After Step 31 gates |

Review the diff itself, not only the prose report:

```sh
git diff step/00-foundation...feature/step-00-foundation/<feature>
```

Check scope, non-goals, public APIs/formats/scientific semantics, test quality, dependency justification,
generated artifacts, status/docs/parity updates, unrelated changes, and licensing.

## 4. Local quality gates

```sh
dotnet format Mocassin.slnx --verify-no-changes
dotnet build Mocassin.slnx -c Release
dotnet test Mocassin.slnx -c Release

cargo fmt --manifest-path src/rust/Cargo.toml --all --check
cargo clippy --manifest-path src/rust/Cargo.toml --workspace -- -D warnings
cargo test --manifest-path src/rust/Cargo.toml --workspace

python -m pytest src/python/tests
python -m build src/python

python tools/contracts/generate.py --check
python tools/check-license-headers.py
python tools/check-no-secrets.py
./tools/check-api-compat.sh
zensical build --strict
```

Feature branches are validated locally; CI runs on `main` and `step/*` integration branches only. Report the
commands and their results, including the exit status.

## 5. Continuous integration

CI runs on pushes to `main` and integration branches matching `step/*`. GitHub's `*` does not match `/`, so
feature branches and nested `step/*/*` branches are not triggered. Pull-request CI is not enabled until the
project adopts pull requests.

Step branches run Ubuntu and Windows to keep squash validation responsive. The full matrix, including macOS,
runs on `main` after the step merge.

The workflow has these jobs:

| Job | What it checks |
| --- | --- |
| `.NET` | Ubuntu + Windows on step branches, plus macOS on `main`; restore, Release build, tests with TRX/coverage, `dotnet format` |
| `Rust` | Ubuntu + Windows on step branches, plus macOS on `main`; `cargo fmt`, `clippy -D warnings`, tests, Release build |
| `Python` | Editable install on 3.11–3.13, pytest with JUnit/coverage, wheel/sdist |
| `Contracts and policy` | Manifest drift, SPDX headers, secret scan, `cargo deny` |
| `Public API compatibility` | ApiCompat against the checked-in baseline assemblies |
| `Documentation` | Strict Zensical build plus DocFX and rustdoc output |

The scheduled `Nightly` workflow is a placeholder for parity, fuzzing, benchmarks, and screenshot suites.

## 6. Tests

Follow the testing trophy in
[`docs/planning/03-quality-strategy.md`](https://github.com/seb-eis/mocassin-2/blob/main/docs/planning/03-quality-strategy.md):

- most effort on contract/acceptance, property, and toy-integration tests;
- real objects, real files, real databases, and real writers;
- test doubles only at process/OS/UI/network/time boundaries or for failure injection;
- no assertions on private call counts or collaborator interactions;
- every bug fix gets a regression test; every scientific change gets a physics/parity test;
- declare the test tier (`PR-fast`, `PR-medium`, `nightly`, `release`).

## 7. Documentation as code

- Narrative documentation is in `docs/` and built with Zensical; `docs/planning/` is internal and excluded
  from the published site.
- Every feature updates the documentation pages named by its step and keeps the strict build green.
- API reference is generated with DocFX, rustdoc, and mkdocstrings; do not hand-write API pages.
- Update the step page checkboxes/feature log, `docs/planning/status.md`, and the parity matrix as part of the
  feature, not as a separate status-only commit.
- Use Mermaid fenced blocks for diagrams.

## 8. Licensing and dependencies

- New files use `SPDX-License-Identifier: MIT`.
- Do not copy original MOCASSIN source, documentation, or database content into MIT files.
- The original project is GPL-3.0-or-later and is attributed in
  [`NOTICE.md`](https://github.com/seb-eis/mocassin-2/blob/main/NOTICE.md).
- New dependencies require a justification, a pinned version, and a licence check. GPL/AGPL dependencies need
  explicit review.

## 9. Evidence and approvals

A feature is ready for approval only with the report template from
[`agent-workflow.md`](agent-workflow.md) §5. Do not approve from prose alone. If the working tree is on
`main`, a step branch with feature work, or the next action would merge/tag/push `main`, stop and ask the
owner.
