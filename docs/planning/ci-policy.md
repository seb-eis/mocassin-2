# CI Cost Policy

The project targets the GitHub Free private-repository allowance of **2,000 billable minutes per month**.
CI is a confirmation gate, not the development test loop.

## 1. Cost snapshot

November 2026, first eight days, three steps:

| Runner | Billable minutes | Share |
| --- | ---: | ---: |
| Ubuntu | 434 | 40% |
| Windows | 308 | 28% |
| macOS | 350 | 32% |
| **Total** | **1,092** | **100%** |

Most of the cost came from running CI on every step-branch push: Step 02 used 358 minutes across 18 runs.
A single Ubuntu run is cheap (~12 minutes); Windows is 2× and macOS is 10×.

## 2. Automatic and manual runs

| Run | Trigger | Runner | Scope |
| --- | --- | --- | --- |
| Main merge | `push` to `main` | Ubuntu | Path-filtered jobs |
| Step closeout | one manual `workflow_dispatch` per step | Ubuntu | Path-filtered jobs |
| Platform gate | manual `workflow_dispatch` with `platforms` | Windows/macOS | On demand only |
| Documentation | manual `docs.yml` | Ubuntu | Full docs build |
| Benchmark | manual `benchmark.yml` | Ubuntu | Selected benchmark, once the project lands |
| Nightly | disabled until real suites exist | — | Weekly on Ubuntu if re-enabled |

There are **no per-feature and no per-push step-branch CI runs**. Feature branches are validated locally with
`tools/verify.sh`.

## 3. Path filters

The `changes` job compares the pushed range (main) or the branch merge-base with `main` (manual dispatch) and
enables only affected jobs:

| Job | Runs when |
| --- | --- |
| .NET | `src/dotnet/`, `tests/`, solution/props/SDK files change |
| Rust | `src/rust/`, Cargo files, toolchain, or `deny.toml` change |
| Python | `src/python/` or its `pyproject.toml` change |
| Contracts and policy | any code changes, or contract/symmetry/licence/secret tooling changes |
| Public API compatibility | .NET public surface changes |
| Documentation | separate manual workflow |

Python runs on 3.13 only. The full 3.11/3.12/3.13 matrix is a manual/nightly concern until the Python surface
grows enough to justify it.

A change to `.github/workflows/ci.yml` forces all jobs so the pipeline itself is validated.

## 4. Platform promotion

Windows and macOS stay manual while the code is platform-neutral. Add a platform to the automatic closeout
and main gate when the step first touches:

- P/Invoke, `LibraryImport`, or `System.Runtime.InteropServices`;
- native libraries, RID-specific packages, or platform assets;
- path separator, case sensitivity, long-path, or file-locking logic;
- line-ending or encoding-sensitive file handling;
- GUI/desktop projects;
- Rust `cfg(windows)` or `cfg(target_os = "...")`;
- installers, packaging, or OS-specific launch behaviour.

Promote Windows first. Keep macOS manual until there is actual macOS-specific behaviour to validate.

## 5. Budget guard

- Target: **≤300 billable minutes per month**.
- If usage passes 50% of the allowance before mid-month, switch to closeout-only CI and ask the owner before
  adding any run.
- If a platform gate is needed, prefer Windows before macOS. A normal macOS gate costs about 30 billable
  minutes; a long macOS job can cost hundreds because of the 10× multiplier.
- Use `[skip ci]` for docs-only commits and merges.

## 6. Commands

Step closeout (Ubuntu, path-filtered):

```sh
gh workflow run ci.yml --ref step/02-symmetry-and-geometry -f platforms=ubuntu
```

Platform gate (only when required):

```sh
gh workflow run ci.yml --ref step/02-symmetry-and-geometry -f platforms=windows
gh workflow run ci.yml --ref step/02-symmetry-and-geometry -f platforms=all
```

Force every job (for example after a CI-workflow change):

```sh
gh workflow run ci.yml --ref step/02-symmetry-and-geometry -f platforms=ubuntu -f force_all=true
```

Local validation:

```sh
./tools/verify.sh dotnet
./tools/verify.sh all      # before closeout
```

## 7. Main merge

The merge commit to `main` triggers one automatic Ubuntu run. Wait for it to pass before creating the
`step-NN` tag. A red `main` is fixed or reverted; it is never tagged.
