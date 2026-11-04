# Step 12 Audit Request

## Request

| Field | Value |
| --- | --- |
| Step | 12 — Build Orchestration and `mocassin` CLI |
| Request owner | Step agent |
| Audit status | Not requested |
| Audited commit | — |
| Step branch | — |
| Step tag | — |
| CI run | — |
| Audit date | — |
| Auditor | — |

## Step summary

| Field | Value |
| --- | --- |
| Phase | C. Compilation |
| Depends on | 10, 11 |
| Delivers | A headless CLI that loads a project, validates, compiles, parameterizes, expands jobs, writes a package, reports progress, and supports cancellation and partial builds. |

Step page: [`Step 12 — Build Orchestration and mocassin CLI`](../steps/step-12-build-orchestration-and-cli.md)

## Step-specific acceptance checklist

- [ ] `mocassin validate samples/ceria/ceria.mocprj` exits 0 with the expected warnings.
- [ ] `mocassin build samples/ceria -o /tmp/ceria.msl` produces a package that `mocassin verify` accepts.
- [ ] `--jobs 1-3` writes exactly jobs 1–3 with stable IDs.
- [ ] Cancelling a build leaves no temp file and preserves an existing package.
- [ ] `--json` output validates against a schema and contains the exit code.
- [ ] `doctor` reports a healthy environment on CI.
- [ ] The CLI can build 10⁴ jobs without the GUI.
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
