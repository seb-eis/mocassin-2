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
| Main merge | `push` to `main` | Ubuntu | Path-filtered jobs, full .NET suite, process/binding checks |
| Step closeout | one manual `workflow_dispatch` per step | Ubuntu | Path-filtered jobs, full .NET suite, process/binding checks |
| Platform gate | manual `workflow_dispatch` with `platforms` | Selected runner only; `windows`/`macos` never start an Ubuntu job except `changes` | Focused .NET platform tests; no Rust, Python, contracts, process/binding, or API jobs |
| Documentation | manual `docs.yml` | Ubuntu | Full docs build |
| Benchmark | manual `benchmark.yml` | Ubuntu | Selected benchmark |
| Nightly | manual placeholder; no schedule until real suites exist | — | — |

Before closeout, run `./tools/verify.sh all --require`. A skipped required gate is not a pass, and `cargo-deny`
must be installed for the closeout run. The `aot` gate is part of `all` and hard-fails, with a clear message,
if clang/lld/ILCompiler is absent; it never silently skips. Because `aot` is in `all`, **every step closeout
from Step 12 requires the Tier 1 toolchain**; the 19.1.7/11.0.0 versions are observed apt/NuGet resolutions
checked by `06` §9, not a containerfile pin, and the gate fails on absence, not a version mismatch. The closeout
GitHub workflow runs restore/build/test/format and does **not** run `tools/verify.sh`, so the local command and
its recorded output are the evidence for the local gates. Record the closeout CI run ID, `head_sha`, and the
closeout evidence file described in [`closeout/README.md`](closeout/README.md).

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

### 2.2 Platform gate scope

The platform gate answers one question: does the platform-sensitive .NET code still work on Windows (or macOS)?
It is not a second closeout run and it never re-runs the Ubuntu path.

**Matrix behaviour.** The `platforms` input selects exactly the runners that start:

| `platforms` | `os` matrix | Jobs that run |
| --- | --- | --- |
| `ubuntu` (default) | `["ubuntu-latest"]` | Ubuntu `changes` plus every path-filtered Ubuntu job |
| `windows` | `["windows-latest"]` | Ubuntu `changes` plus the focused .NET gate on Windows only |
| `macos` | `["macos-latest"]` | Ubuntu `changes` plus the focused .NET gate on macOS only |
| `all` | `["ubuntu-latest","windows-latest","macos-latest"]` | Ubuntu `changes`, the full Ubuntu path, and both focused gates |

`windows` does **not** imply an Ubuntu job beyond the short `changes` detection job. The Rust, Python,
contracts/policy, process/binding, and API-compatibility jobs carry a job-level condition in addition to their
path filter:

```text
needs.changes.outputs.<area> == 'true'
  && (github.event_name == 'push' || inputs.platforms == 'ubuntu' || inputs.platforms == 'all')
```

They therefore do not start for `platforms=windows` or `platforms=macos`, even when the changed paths would
enable them. Only the selected runners are billed, so a Windows platform gate costs the short Ubuntu `changes`
job plus one Windows job with the runner's multiplier and nothing else.

**Focused scope.** The .NET job keeps a single build so both paths share the same compile:

- Ubuntu: `dotnet restore` → `dotnet build Mocassin.slnx -c Release` → full
  `dotnet test Mocassin.slnx` with coverage → `dotnet format --verify-no-changes`.
- Windows/macOS: `dotnet restore` → `dotnet build Mocassin.slnx -c Release` → only
  `Mocassin.Packages.Tests`, `Mocassin.Results.Tests`, and `Mocassin.Cli.Tests`. No coverage, no format check,
  no full-suite run.

**Rationale.** The platform gate exists because of P/Invoke directory `fsync`, atomic rename and temporary-file
handling in the package writer, the SQLite reader's path, case-sensitivity, and file-locking behaviour, and the
CLI's guarded `Console.CancelKeyPress`/SIGTERM path plus its Windows `mocassin-sim.exe` probe selection. Those
tests live in `Mocassin.Packages.Tests`, `Mocassin.Results.Tests`, and `Mocassin.Cli.Tests`. The remaining
projects (core kernel, symmetry, compilation, contracts, view models, architecture policy) are platform-neutral
unit and property tests whose result cannot differ by OS; running them again on Windows spends minutes without
new information. The full suite and the coverage collection stay on Ubuntu, where the closeout evidence is
produced.

Note on current content: the SQLite reader tests arrive with Step 11, so on `main` at the time of this split
`Mocassin.Results.Tests` holds only a smoke test. The gate still builds the whole solution and runs the full
package-writer suite, which already covers atomic rename and temporary-file handling; the results half grows
with the reader. Recheck this paragraph when a project is renamed and update the workflow in the same change.

**Rust, Python, contracts, process/binding, and API compatibility stay Ubuntu-only.** The platform gate is a
.NET gate: the Rust job no longer uses the OS matrix (`platforms=windows` does not start a Windows Rust job), the
Python, contracts/policy, process/binding, and API-compatibility jobs are pinned to `ubuntu-latest`, and all five
are skipped by the job-level condition above for `platforms=windows`/`macos`. If the Rust solver later grows
`cfg(windows)`/`cfg(target_os = ...)` code, promote it under section 4 with its own decision; do not widen the
current Windows gate by accident.

**Expected saving.** The previous `platforms=windows` dispatch ran an implicit full Ubuntu job (~12 billable
minutes) plus a full Windows .NET run (~12 min wall × 2 = ~24 billable), about **36 billable minutes**. The
focused gate keeps the solution build but drops the boilerplate suite, coverage, and a second Ubuntu job,
leaving roughly 8–10 min wall × 2 = **16–20 billable minutes**, a saving of about **16–20 minutes per dispatch
(≈45–55%)**. The old dispatch could also start the path-filtered Rust, Python, contracts, and API jobs on
Ubuntu; the job-level condition above removes that spend as well, so the real saving is larger when those paths
changed. The estimate uses the same `ceil(job minutes) × runner multiplier` method as section 1; confirm the
first real gate against the billing page and update this paragraph with the measured figure.

## 3. Path filters

The `changes` job compares the pushed range (main) or the branch merge-base with `main` (manual dispatch) and
enables only affected jobs:

| Job | Runs when | Runner |
| --- | --- | --- |
| .NET | `src/dotnet/`, `tests/`, solution/props/SDK files change | Ubuntu (full) or the selected platform gate (focused) |
| Rust | `src/rust/`, Cargo files, toolchain, or `deny.toml` change | Ubuntu only |
| Python | `src/python/` or its `pyproject.toml` change | Ubuntu only |
| Contracts and policy | any code changes, or contract/symmetry/licence/secret tooling changes | Ubuntu only |
| Process and binding | `.opencode/**`, `AGENTS.md`, `GLOSSARY.md`, `opencode.json`, `docs/development/**`, `docs/planning/reviews/**`, `docs/planning/audits/**`, `zensical.toml`, `tools/check_process_consistency.py`, `tools/check_docs_links.py`, `tools/check_doc_math.py`, or `tools/tests/**` change | Ubuntu only |
| Public API compatibility | .NET public surface changes | Ubuntu only |
| Documentation | separate manual workflow | Ubuntu only |

Path filters decide *whether* a job is enabled; the `platforms` input decides *where* the .NET job runs and
*whether* the Ubuntu-only jobs (Rust, Python, contracts/policy, process/binding, API compatibility) start at all.
A platform gate keeps the same path filters, concurrency group, permissions, and timeouts as the Ubuntu path.

Python runs on 3.13 only. The full 3.11/3.12/3.13 matrix is a manual/nightly concern until the Python surface
grows enough to justify it.

The process/binding job is deliberately cheap: it installs nothing and builds neither .NET nor Rust. It runs
`tools/check_process_consistency.py`, `tools/check_docs_links.py`, `tools/check_doc_math.py`, and the
`tools/tests` suite, which use only the Python standard library. It exists because the 2026-11-27 workflow
adoption added those guardrails without a CI consumer; licence, secret, contract, and dependency checks stay in
the contracts/policy job.

A change to `.github/workflows/ci.yml` forces all jobs so the pipeline itself is validated. Because
`platforms=windows`/`macos` suppresses the Ubuntu-only jobs even when their paths changed, a workflow-validation
dispatch must use `platforms=all` (or one `ubuntu` plus one `windows` dispatch): `platforms=windows` would
validate the Windows gate but skip the Ubuntu full-suite path and the Rust, Python, contracts, process/binding,
and API jobs.

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

Promote Windows first. Keep macOS manual until there is actual macOS-specific behaviour to validate. Promotion
means adding the platform to the automatic closeout/main gate; it does not mean widening the focused platform
gate to the full suite. If a platform-specific defect ever needs the full suite on Windows, add an explicit
full-platform dispatch option in a reviewed workflow change and record the reason; do not silently recombine the
two scopes.

## 5. Budget guard

- Target: **≤300 billable minutes per month**.
- If usage passes 50% of the allowance before mid-month, switch to closeout-only CI and ask the owner before
  adding any run.
- If a platform gate is needed, prefer Windows before macOS. The focused Windows gate costs about 16–20 billable
  minutes, not the ~36 of the former Ubuntu-plus-Windows run. A normal macOS gate still costs about 30 billable
  minutes even focused, because of the 10× multiplier; a long macOS job can cost hundreds.
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
# Focused Windows gate: the short Ubuntu `changes` job plus one Windows .NET
# job (build and the two platform test projects). No other Ubuntu job starts.
gh workflow run ci.yml --ref <step-branch> -f platforms=windows

# Windows, macOS, and the Ubuntu full suite in one dispatch.
gh workflow run ci.yml --ref <step-branch> -f platforms=all
```

A gate that must validate the Ubuntu path as well needs a separate closeout dispatch; `platforms=windows` is
deliberately Windows-only so the two scopes do not get recombined by accident.

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
