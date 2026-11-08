# Contributing to MOCASSIN

Thank you for contributing. This project is a scientific reimplementation, so correctness, reproducibility,
and review evidence matter more than raw speed.

The normative rules are in [`AGENTS.md`](AGENTS.md). This page is the short contributor guide; the published
developer guide is [`docs/development/contributing.md`](docs/development/contributing.md).

## 1. Prerequisites

Install the toolchain listed in [`README.md`](README.md#prerequisites) or build the development image from
[`docs/opencode/mocassin.containerfile`](docs/opencode/mocassin.containerfile).

## 2. Branch workflow

- `main` is integration/release only. Never commit feature work directly to it.
- Work on one roadmap step at a time on a step branch: `step/<NN>-<slug>`.
- Create one feature branch per deliverable from the step branch:
  `feature/step-<NN>-<slug>/<feature>`.
- Bug fixes inside a step use `feature/step-<NN>-<slug>/fix-<short-description>`.
- Keep feature branches short-lived. Rebase only your own unshared branch; never rewrite a shared step branch.
- Feature branches are validated locally. CI runs only at step closeout and on `main`; see `AGENTS.md` §3.13.

The full branch, approval, squash-merge, tag, and closeout rules are in
[`AGENTS.md`](AGENTS.md) §3. Do not merge into a step branch or into `main` without explicit approval.

## 3. Local quality gates

Run the gates relevant to your change before asking for review:

```sh
# .NET
dotnet format Mocassin.slnx --verify-no-changes
dotnet build Mocassin.slnx -c Release
dotnet test Mocassin.slnx -c Release

# Rust
cargo fmt --manifest-path src/rust/Cargo.toml --all --check
cargo clippy --manifest-path src/rust/Cargo.toml --workspace -- -D warnings
cargo test --manifest-path src/rust/Cargo.toml --workspace

# Python
python -m pytest src/python/tests
python -m build src/python

# Code structure
./tools/verify.sh structure

# Contracts, policy checks, API compatibility, and docs
python tools/contracts/generate.py --check
python tools/check-license-headers.py
python tools/check-no-secrets.py
./tools/verify.sh api
zensical build --strict
```

Additional gates for specific changes:

- format or schema changes: update `contracts/manifest.json`, generated artifacts, fixtures, versions, and
  compatibility tests;
- hot-path changes: run the relevant benchmark and report the result;
- documentation changes: strict Zensical build and no broken links.

Warnings are errors. Do not disable an analyzer or linter to make a gate pass without an approved reason.

## 4. Tests

Follow the testing trophy in [`docs/planning/03-quality-strategy.md`](docs/planning/03-quality-strategy.md):

- prefer real objects, real files, real SQLite databases, and files produced by the real writers;
- use property tests for geometry, symmetry, compilation, supercells, and round-trips;
- use statistical tests for physics;
- use test doubles only at true process/OS/UI/network/time boundaries or for failure injection;
- assert observable behaviour, never private call counts or collaborator interactions;
- add a regression test with every bug fix.

Declare the test tier (`PR-fast`, `PR-medium`, `nightly`, `release`). PR-fast tests must not depend on
nightly-only assets.

## 5. Code style

- .NET: .NET 10, nullable enabled, implicit usings, warnings as errors, XML docs for public APIs.
- Code structure: follow [`docs/development/code-structure.md`](docs/development/code-structure.md); group files
  by domain concept, keep the project root for entry points, and run `./tools/verify.sh structure`.
- Rust: stable Rust, edition 2024, `rustfmt`, `clippy`, no `unsafe` without a `// SAFETY:` comment and a test.
- Python: typed public APIs, `ruff`-clean, pytest for behaviour.
- Use the units, diagnostics, geometry, and contract types defined by the plan; do not introduce bare
  `double` values where a unit type is required.
- Do not add a dependency without a justification in the change description and a licence check.

## 6. Documentation is part of the change

- Narrative documentation lives in `docs/` and is built with Zensical.
- API reference is generated with DocFX, rustdoc, and mkdocstrings; do not hand-write API pages.
- `docs/planning/` is internal and must never be published.
- Update the step page, status dashboard, and parity matrix when your change affects them.
- Every feature step updates its named documentation pages and keeps `zensical build --strict` green.

## 7. Review evidence

A feature is ready for approval only with:

```text
Branch:
Commits:
Diff summary:
Tests/commands and results:
Structure check:
CI (expected or observed):
Docs/status updated:
Risks and limitations:
Approval requested for:
```

Do not approve from prose alone. Review the diff against the step branch, check scope and non-goals, and
confirm that generated contract artifacts came from the generator rather than being hand-edited.

- Independent reviewers write findings and verdict directly into the review or audit record and commit only
  that file. Do not use a separate scribe or transcription step.

## 8. Licensing

- New files use `SPDX-License-Identifier: MIT`.
- Do not copy original MOCASSIN source, documentation text, or database content into MIT files.
- The original project is GPL-3.0-or-later and remains attributed in [`NOTICE.md`](NOTICE.md).
- Dependencies must be MIT-compatible unless explicitly reviewed.

## 9. Reporting issues

Use the GitHub issue templates for bugs and feature requests. Include the affected step, the relevant
parity-matrix item, reproduction steps, expected/actual behaviour, and whether documentation needs updating.
