# CI process job dependency correction — 2026-11-27

**Status:** Correction committed on `fix/ci-process-job-deps` at `8c98d72` on top of `main` `d2ba15f`; owner
approved the merge as an urgent correction. This record is committed before the merge, so the merge commit and
the confirming `main` CI run are reported to the lead under `AGENTS.md` §17 rather than amended into this file.
**Owner:** human owner.
**Decision date:** 2026-11-27.
**Design class:** `Full` family (CI gate behaviour and cost policy). Executed as an urgent post-merge correction
under the owner decision in §6, with no separate Full round. Not a public contract, format, or dependency change
outside CI tooling.

## 1. Decision

The `Process and binding checks` job was merged into `main` at `d2ba15f` as part of the `POLICY-2026-11-27-CI`
change. On the first `main` run it failed. The owner approved a one-commit urgent correction that installs the
already-pinned contracts test requirement before the tool tests run, and approved recording that decision here
instead of opening a new Full policy round.

- **Scope:** the `process` job in `.github/workflows/ci.yml`, its pinning test, and the three pages that describe
  the job's cost and dependency posture.
- **Unchanged:** every other CI job, the path filters, the concurrency group, the `10`-minute timeout, Ubuntu-only
  execution, and the no-.NET/no-Rust-build constraint.
- **Not a new policy:** the install is the command that `tools/verify.sh contracts` already runs and that the
  failing tool already told the reader to run.

## 2. Symptom — red `main`

| Field | Value |
| --- | --- |
| Run | [`46072539902`](https://github.com/seb-eis/mocassin-2/actions/runs/46072539902) (run 166, `push`, `CI`) |
| `head_sha` | `d2ba15fe594ccc050f262a45ba7fe776e460c382` |
| Conclusion | `failure` |
| Failing job | `Process and binding checks` (job `140752666388`) |
| Failing step | step 7, `Tool tests` — `python -m unittest discover -s tools/tests -t .` |
| Output | `Ran 77 tests in 0.548s`, `FAILED (errors=1)` |
| Error | `ModuleNotFoundError: No module named 'msgpack'`, surfaced as `SystemExit: msgpack is required; install tools/contracts/requirements.txt` |

Every other job passed: `Detect changes`, `.NET (ubuntu-latest)`, `Python (3.13)`, `Public API compatibility`,
`Contracts and policy`, and `Rust`. The failure is isolated to the new job, and it is deterministic: the
`test_parameterization_projection` module imports `tools.contracts.check_model_projection`, which imports
`msgpack` at module scope. One failed module import counts as one unittest error, so the run stops at 77 tests.

The reviewed premise was wrong. The policy review rounds 1 and 2
([`POLICY-2026-11-27-CI`](../reviews/policy-2026-11-27-ci-process-guardrails.md), reviewed commits `f00fd0f` and
`06a861b`, verdict `Ready for implementation`) accepted a job that "installs nothing", and
`tools/tests/test_ci_process_job.py` asserted that `pip install` was absent from the job. The four guardrails are
standard-library tools, but the `tools/tests` suite also covers the projection checkers, which are not. Neither
the review nor the test executed the job in a clean environment, so the defect reached `main`.

## 3. Clean-venv reproduction

Reproduced on the fix branch with Python 3.13.5 in a fresh virtual environment with no site packages:

| State | Command | Result |
| --- | --- | --- |
| No dependency installed | `python -m unittest discover -s tools/tests -t .` | `Ran 77 tests`, `FAILED (errors=1)`, `ModuleNotFoundError: No module named 'msgpack'` |
| Pinned requirements installed | `python -m pip install -r tools/contracts/requirements.txt` | `msgpack (1, 2, 4)` |
| After install | `python -m unittest discover -s tools/tests -t .` | `Ran 78 tests`, `OK` |

The reproduction matches the CI log exactly: 77 tests and one import error without `msgpack`; 78 tests and a
green suite with it. The single extra test is the projection module that could not be imported.

## 4. Fix

`.github/workflows/ci.yml`, in the `process` job after `actions/setup-python`:

```yaml
      - name: Install contracts test requirements
        run: python -m pip install -r tools/contracts/requirements.txt
```

`tools/contracts/requirements.txt` contains only `msgpack==1.2.4`, so the job gains one pinned, pure-Python wheel
and no transitive graph. The step is placed before `Process consistency`, so every later step, including the
test discovery, sees the dependency. The comment and the three documentation surfaces now state the real posture:

- `AGENTS.md` §3.13 — the job "installs only the pinned contracts requirements (`msgpack==1.2.4`)".
- `docs/planning/ci-policy.md` §3 — the same qualification, and why the install exists while the checkers
  themselves remain standard-library only.
- `docs/development/contributing.md` — the `Process and binding` row in the CI job table.

## 5. Test changes

`tools/tests/test_ci_process_job.py` keeps pinning the cheapness constraint, but pins the one permitted install
instead of forbidding every install:

- new constants `CONTRACTS_REQUIREMENTS` (`tools/contracts/requirements.txt`) and
  `CONTRACTS_INSTALL_COMMAND` (`python -m pip install -r tools/contracts/requirements.txt`);
- the job's `run:` lines that contain `install` must equal exactly `[CONTRACTS_INSTALL_COMMAND]`;
- the requirements file content must equal exactly `["msgpack==1.2.4"]`, so an unpinned or widened install fails
  the test;
- `dotnet` and `cargo` remain forbidden in the job; `pip install` is no longer in the forbidden list because the
  exact-match assertion above is stricter than its old substring check.

This changes an assertion from the reviewed tree. It is recorded here because the old assertion encoded the
incorrect "installs nothing" premise, not because the cheapness requirement changed.

## 6. Review and owner decision

- The original job design was reviewed as a Full policy update in rounds 1 and 2 at `f00fd0f` and `06a861b`
  (verdict `Ready for implementation`, no Blocker or Major remaining) and merged at `d2ba15f`.
- This correction is a **post-merge defect fix** to that reviewed job. `main` was red, so the owner approved
  treating it as an urgent correction: fix forward on `fix/ci-process-job-deps`, record the decision in this
  maintenance note, and do not open a separate Full policy round.
- Compensating evidence: the clean-venv reproduction, the affected local gates in §7, and the full `main` run that
  includes the `process` job. The correction is not cleaned up unless that run is green.

## 7. Verification

Run on `fix/ci-process-job-deps` at `8c98d72`:

| Command | Result |
| --- | --- |
| Clean-venv reproduction without `msgpack` | `Ran 77 tests`, `FAILED (errors=1)` — matches CI |
| Clean-venv reproduction after the pinned install | `Ran 78 tests`, `OK` |
| `./tools/verify.sh contracts` | exit 0: contracts current (26 files), three projection checks OK, space-group catalog current, `Process consistency: OK`, `Ran 78 tests ... OK`, SPDX headers in 1211 source files, no secret patterns in 1791 tracked text files, cargo-deny advisories/bans/licenses/sources OK |
| `./tools/verify.sh docs` | exit 0: link and math checks, DocFX build with 0 warnings and 0 errors, rustdoc, site built |
| `git status --porcelain` after the gates | empty (generated `site/`, `artifacts/`, and build output are ignored) |

## 8. Residual risk

- The job is no longer dependency-free. A PyPI or network outage now fails it, where before it needed no network
  beyond checkout. The pinned single wheel keeps the surface small; the timeout stays at 10 minutes.
- The version appears in four places: `tools/contracts/requirements.txt`, the exact-content assertion in
  `test_ci_process_job.py`, `AGENTS.md` §3.13, and `docs/planning/ci-policy.md`. A bump such as `msgpack 1.2.5`
  requires updating all four together; the test fails loudly if the first two drift, and review must catch the
  prose.
- No independent Full review round was run, by owner decision. The evidence is the local reproduction and gates
  plus the confirming `main` run; a red `main` run reopens the correction rather than closing it.
- The test's exact-install assertion is intentionally strict: any future legitimate install in this job needs a
  test update and a recorded reason, which is the desired friction for a cheap job.
- The projection test now depends on the contracts requirements in CI exactly as it does locally; it no longer
  has a standard-library-only path, which was never true.

## 9. Provenance and rollback

- Branch `fix/ci-process-job-deps`, one commit `8c98d72` on `main` `d2ba15f`; squash is not used because this is
  an out-of-step maintenance branch merged with `--no-ff`.
- Executed on the paid `dev` binding because the `dev-free` concurrency cap was full; the lead recorded the
  fallback per [`docs/development/model-economics.md`](../../development/model-economics.md) §6.
- Rollback is `git revert` of the squash commit on `main`; there is no tag and no published artifact to move.
  Reverting would restore red `main` unless the test binding and job are reverted together, so the forward fix is
  the intended path.
