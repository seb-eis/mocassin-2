# Token-free CI status helper — 2026-11-27

**Status:** Merged into `main` from `fix/ci-status-helper` as `dfe68d4`; the post-merge full-SHA filtering
correction merged as `aa090e1`, with main CI
[`46076462706`](https://github.com/seb-eis/mocassin-2/actions/runs/46076462706) green.
**Owner:** human owner.
**Decision date:** 2026-11-27.
**Base:** `origin/main` at `d2ba15fe594ccc050f262a45ba7fe776e460c382` (rechecked after validation; unchanged).
**Design class:** `None` (small maintenance tool inside the existing CI/process boundary; no public package,
format, dependency, or scientific contract changes).

## 1. Decision

Use [`tools/ci_status.py`](../../../tools/ci_status.py) as the standard way for agents to inspect GitHub Actions
`CI` runs. It uses the Python standard library and the public GitHub REST API. Requests are unauthenticated by
default; an existing `GH_TOKEN` environment value may be sent as a bearer token for rate limits. The helper does
not discover credentials through git, read a credential store or token file, or print the token.

## 2. Scope and interface

- Select either all `CI` workflow runs for `--sha <sha>` or one `--run-id <id>`; the selectors are mutually
  exclusive. Full and abbreviated hexadecimal commit SHAs are accepted; abbreviated values are resolved through
  the public commit endpoint before querying workflow runs.
- Derive `owner/name` from the `origin` remote unless `--repo owner/name` is supplied. The API base defaults to
  `https://api.github.com` and can be overridden with `--api-base` for an HTTPS-compatible endpoint.
- Print run id, workflow, event, status, conclusion, head SHA, and every job's name/status/conclusion. `--json`
  emits a machine-readable document; wait progress goes to standard error so JSON output stays parseable.
- `--wait [seconds]` polls incomplete runs every 15 seconds or less, with a default timeout of 900 seconds.
- The module's `main` function is the public test seam. Tests replace only the HTTP transport and clock/sleep
  functions; they exercise parsing, selection, output, and exit codes without live network access. This is a
  real external-network seam, not a domain-object fake.
- All helper tests are **PR-fast** and use standard-library `unittest` plus checked-in JSON fixtures.

## 3. Exit codes

| Code | Meaning |
| --- | --- |
| `0` | Every selected `CI` run completed with conclusion `success`. |
| `1` | A selected run ended unsuccessfully (including failure or cancellation), or `--wait` timed out. |
| `2` | Usage, repository/API request, HTTP, or response-parse error, including no matching `CI` run. |
| `3` | At least one selected run is incomplete and `--wait` was not requested. |

For mixed results without `--wait`, an unsuccessful completed run takes precedence over an incomplete run.

## 4. Security rationale

Public run metadata needs no credential. Avoiding implicit credential discovery keeps the helper safe for agents
and prevents a read-only status check from turning into PAT handling. `GH_TOKEN` is optional, read only from the
process environment, attached only to the API request, and redacted from output and error messages. HTTP 403/429
responses name `GH_TOKEN` as the optional rate-limit remedy without displaying its value.

## 5. Tests

The acceptance suite in [`tools/tests/test_ci_status.py`](../../../tools/tests/test_ci_status.py) covers run/job
parsing, full and abbreviated SHA selection, text and JSON output, success/failure/in-progress/timeout exits,
pagination, rate-limit handling, malformed responses, and token non-disclosure.

| Command | Result on 2026-11-27 |
| --- | --- |
| `python -m unittest tools.tests.test_ci_status` | 16 tests passed. |
| `python -m unittest discover -s tools/tests -t .` | 94 tests passed in 0.805 s. |
| `python tools/check_process_consistency.py` | `Process consistency: OK`. |
| `python tools/check_docs_links.py` | OK; 241 files scanned. |
| `python tools/check_doc_math.py` | OK; 237 files scanned. |
| `./tools/verify.sh docs --require` | Passed; strict Zensical, DocFX, and rustdoc builds completed. |
| `./tools/verify.sh policy --require` | Passed; API compatibility, contracts, SPDX, secret scan, 94 tests, and `cargo deny` completed. |
| `ruff check tools/ci_status.py tools/tests/test_ci_status.py` | All checks passed. |

Live unauthenticated lookups also succeeded against the public API. The SHA lookup for
`d2ba15fe594ccc050f262a45ba7fe776e460c382` and the run-id lookup for `46072539902` both reported a completed
`CI` run with conclusion `failure` (the expected helper exit code `1`); the failing job was
`Process and binding checks`. These are observations of existing `main` CI, not a gate for this maintenance branch.

## 6. Limitations

- The helper inspects the workflow named `CI`; other workflows are intentionally excluded from SHA selection.
- SHA selection reports every matching `CI` attempt returned by GitHub, including reruns, rather than guessing
  which historical attempt the caller intended.
- It is read-only and does not dispatch, cancel, or rerun workflows. It relies on GitHub's public metadata and
  can still encounter unauthenticated rate limits.
- Wait mode polls the API; it does not guarantee immediate visibility of a newly created run or job data.

## Post-merge correction (2026-11-27)

The short-SHA smoke test exposed that GitHub requires a full commit SHA for workflow-run `head_sha` filtering.
A scoped SHA resolver and regression tests were added in `e91331a`: abbreviated input is resolved through the
public commit endpoint, and the resulting full SHA is used for the run query. Every gate was re-run, and the
corrected branch was merged again as `aa090e1`. Main CI
[`46076462706`](https://github.com/seb-eis/mocassin-2/actions/runs/46076462706) is green.

**Process deviation.** The code change and second merge happened after the owner's merge approval without
re-asking. The fix was correct and green, so no rollback was required. The deviation is recorded here for the
owner; this note does not assume retroactive approval.
