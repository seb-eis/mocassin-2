# CI Cost Policy

The project targets the GitHub Free private-repository allowance of **2,000 billable minutes per month**.
CI is a confirmation gate, not the development test loop.

## 1. Cost snapshot

### 1.1 Before the policy

November 2026, first eight days, three steps:

| Runner | Billable minutes | Share |
| --- | ---: | ---: |
| Ubuntu | 434 | 40% |
| Windows | 308 | 28% |
| macOS | 350 | 32% |
| **Total** | **1,092** | **100%** |

Most of the cost came from running CI on every step-branch push: Step 02 used 358 minutes across 18 runs.
A single Ubuntu run is cheap (~12 minutes); Windows is 2× and macOS is 10×.

### 1.2 After the policy

The policy took effect with `91c2f24` on 2026-11-08. From then to 2026-11-14 the repository produced:

| Run type | Runs | Billable minutes |
| --- | ---: | ---: |
| Step closeout (`workflow_dispatch`) | 18 | 125 |
| Main merge (`push`) | 14 | 97 |
| **Total (all Ubuntu)** | **32** | **222** |

Month-to-date usage is therefore about **1,320–1,340 of the 2,000 minutes (66–67%)**, leaving about
**660–680 minutes**. This is reconstructed from the GitHub Actions job timings using
`ceil(job minutes) × runner multiplier`; the same method reproduces the recorded pre-policy snapshot within 2%.
The `/timing` endpoint reports `total_ms: 0` for this repository and the user billing endpoint is not reachable
with the repository token, so confirm the exact figure on the billing page when a decision depends on it.

The closeout share is the avoidable part: 125 minutes for seven steps (02–08), because 18 dispatches were used
where seven would have sufficed — about 74 minutes of re-dispatch overhead. Step 08 alone used six dispatches
(44 minutes) across its audit rounds, including one on an intermediate fix commit.

## 2. Automatic and manual runs

| Run | Trigger | Runner | Scope |
| --- | --- | --- | --- |
| Main merge | `push` to `main` | Ubuntu | Path-filtered jobs |
| Step closeout | one manual `workflow_dispatch` per step | Ubuntu | Path-filtered jobs |
| Platform gate | manual `workflow_dispatch` with `platforms` | Windows/macOS | On demand only |
| Documentation | manual `docs.yml` | Ubuntu | Full docs build |
| Benchmark | manual `benchmark.yml` | Ubuntu | Selected benchmark |
| Nightly | manual placeholder; no schedule until real suites exist | — | — |

Before closeout, run `./tools/verify.sh all --require`. A skipped required gate is not a pass, and `cargo-deny`
must be installed for the closeout run. Record the closeout CI run ID, `head_sha`, and the closeout evidence file
described in [`closeout/README.md`](closeout/README.md).

There are **no per-feature and no per-push step-branch CI runs**. Feature branches are validated locally with
`tools/verify.sh`.

### 2.1 Dispatch discipline

- Dispatch **one** closeout run per frozen commit. Batch every audit or review fix into one commit set, run the
  full local gate, then dispatch once for the new freeze.
- A replacement dispatch is allowed only when the frozen commit changed after a substantive fix. Do not
  dispatch after documentation-only commits or to retry a run that failed for infrastructure reasons.
- Record every earlier dispatch in the closeout notes as process history; only the run for the final frozen
  commit is the closeout run bound by the evidence.
- A red or superseded dispatch is not a reason to tag; the green run on the final commit is.

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
- **Status 2026-11-14:** about 1,320–1,340 of 2,000 minutes used (66–67%), so the 50% guard is active. Stay
  closeout-only, add no new run without owner approval, and defer Windows/macOS platform gates to the next
  monthly reset unless a platform-specific change makes one mandatory.

## 6. Commands

Step closeout (Ubuntu, path-filtered):

```sh
gh workflow run ci.yml --ref <step-branch> -f platforms=ubuntu
```

Use the active step branch. Platform gate (only when required):

```sh
gh workflow run ci.yml --ref <step-branch> -f platforms=windows
gh workflow run ci.yml --ref <step-branch> -f platforms=all
```

Force every job (for example after a CI-workflow change):

```sh
gh workflow run ci.yml --ref <step-branch> -f platforms=ubuntu -f force_all=true
```

Local validation:

```sh
./tools/verify.sh dotnet
./tools/verify.sh all --require   # before closeout
```

## 7. Main merge

The merge commit to `main` triggers one automatic Ubuntu run. Wait for it to pass before creating the
`step-NN` tag. A red `main` is fixed or reverted; it is never tagged.
