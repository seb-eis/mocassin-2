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

The normative branch, approval, merge, tag, and gate rules are in `AGENTS.md` §3. This page is the short
contributor sequence.

1. Read `AGENTS.md`, the planning README, the status dashboard, the step page, and its dependencies.
2. Propose the complete feature-branch list for the step; the owner approves it once. This is the only
   feature-level approval.
3. Create the step branch from current `main` and push it.
4. Create one feature branch from the step branch for each deliverable.
5. Implement only that feature's scope; add the tests named in the step plus regression tests.
6. Update the step page, status dashboard, parity matrix, and documentation in the same feature.
7. Run the local quality gates (`./tools/verify.sh <area>`).
8. After the local gates pass, squash-merge the feature into the step branch. Do not wait for a per-feature
   approval; the step-start feature-list approval covers it.
9. Delete the feature branch. Do not push the step branch just to trigger CI.
10. At closeout, merge current `main`, run `./tools/verify.sh all --require`, dispatch one closeout CI run,
    fill `docs/planning/closeout/step-NN.json`, check it, freeze the branch, and request the independent audit.
11. After a passed audit and owner approval, merge with `--no-ff`, wait for green `main` CI, tag `step-NN`, and
    delete the step branch.

## 3. Gate references

| Gate | Normative home |
| --- | --- |
| Branch model, feature squash, step completion, tags | `AGENTS.md` §3.1–§3.7 |
| Design review | `AGENTS.md` §3.9 and `docs/planning/reviews/README.md` |
| Step audit | `AGENTS.md` §3.10 and `docs/planning/audits/README.md` |
| Closeout evidence and merge-tree rule | `docs/planning/closeout/README.md` |
| Amendments | `AGENTS.md` §3.11 and `docs/planning/amendments/README.md` |
| Cross-cutting phase reviews | `docs/planning/reviews/cross-cutting/README.md` |
| Drift checks | `docs/development/drift-checks.md` |
| Agent security | `docs/development/agent-security.md` |
| Incident and rollback | `docs/development/incident-and-rollback.md` |
| CI cost and gates | `AGENTS.md` §3.13 and `docs/planning/ci-policy.md` |

Do not duplicate a rule from the normative page here. If a summary and the normative page disagree, the
normative page wins and the summary is fixed in the same change.

## 4. Local quality gates

Run the gates relevant to your change before asking for review. Before a step closeout, use
`./tools/verify.sh all --require` and check the closeout evidence; a skipped required check is not a pass.

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
./tools/verify.sh api
zensical build --strict
```

`./tools/verify.sh docs` alone builds Zensical and rustdoc, but DocFX needs the .NET Release assemblies. Run
`./tools/verify.sh all` (which builds first) or build the solution before a standalone docs run.

Feature branches are validated locally; CI runs only at step closeout and on `main`. Report the commands and
their results, including the exit status. Code layout follows
[`code-structure.md`](code-structure.md); run `./tools/verify.sh structure` after adding or moving files.

## 5. Continuous integration

CI is intentionally minimal. Automatic CI runs only on `main` after a step merge, on Ubuntu, with path filters
so only affected jobs run. The step closeout run is a single manual `CI` dispatch on the step branch. There are
no per-feature and no per-push step-branch runs. Pull-request CI is not enabled until the project adopts pull
requests.

Windows and macOS are manual and run only when platform-specific code appears. Documentation, benchmarks, and
long-running suites have separate manual workflows.

The `CI` workflow has these jobs:

| Job | What it checks |
| --- | --- |
| `Detect changes` | Enables only the jobs affected by the pushed or dispatched range |
| `.NET` | Ubuntu by default; restore, Release build, tests with TRX/coverage, code-structure test, `dotnet format` |
| `Rust` | Ubuntu by default; `cargo fmt`, `clippy -D warnings`, tests, Release build |
| `Python` | Ubuntu, Python 3.13; pytest with JUnit/coverage, wheel/sdist |
| `Contracts and policy` | Manifest drift, SPDX headers, secret scan, `cargo deny` |
| `Public API compatibility` | ApiCompat against the checked-in baseline assemblies |

Documentation is built by the separate manual `Documentation` workflow. Benchmarks and long-running suites
have their own manual workflows. `Nightly` is a manual placeholder; it has no schedule until real suites exist.

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

- Math delimiters depend on the audience; see [Math rendering](#math-rendering) below.
- Narrative documentation is in `docs/` and built with Zensical; `docs/planning/` is internal and excluded
  from the published site.
- Every feature updates the documentation pages named by its step and keeps the strict build green.
- API reference is generated with DocFX, rustdoc, and mkdocstrings; do not hand-write API pages.
- Update the step page checkboxes/feature log, `docs/planning/status.md`, and the parity matrix as part of the
  feature, not as a separate status-only commit.
- Use Mermaid fenced blocks for diagrams.

### Math rendering

Published pages under `docs/` (except `docs/planning/`) render math with KaTeX through
`docs/javascripts/katex.js`. Use `$$...$$` for display math and `\(...\)` for inline math. The single-dollar
`$...$` delimiter is intentionally not enabled. GitHub and IDE previews do not run the initializer, so preview
with `zensical serve` or the built `site/`.

Planning records under `docs/planning/` are read outside the Zensical build. Use `$...$` for inline math and
`$$...$$` for display math. Do not use `\(...\)` or `\[...\]` there.

Never use fenced `text` blocks for formulas. The KaTeX assets are pinned to 0.19.1 on unpkg; an offline
preview needs vendored assets, and [`NOTICE.md`](https://github.com/seb-eis/mocassin-2/blob/main/NOTICE.md)
records the attribution. Run `python tools/check_doc_math.py` to report delimiter warnings; it is
warning-only by default and `./tools/verify.sh docs` runs it.

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
