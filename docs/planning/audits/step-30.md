# Step 30 Audit Request

## Request

| Field | Value |
| --- | --- |
| Step | 30 — Standalone Documentation Completion and Publishing |
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
| Phase | G. Release and documentation |
| Depends on | 19, 21, 22, 28 |
| Delivers | The completed, audited, and published standalone documentation site built with Zensical, plus generated .NET (DocFX), Rust (rustdoc), and Python (mkdocstrings) API references under one site. |

Step page: [`Step 30 — Standalone Documentation Completion and Publishing`](../steps/step-30-standalone-documentation.md)

## Step-specific acceptance checklist

- [ ] `zensical build --strict` succeeds with no broken links/anchors.
- [ ] DocFX builds the .NET API with no missing-doc errors for public APIs.
- [ ] `cargo doc --workspace --no-deps` builds the Rust API with no warnings.
- [ ] mkdocstrings renders the Python API at `/api/python/` with no import/docstring failures.
- [ ] The combined site serves narrative pages at `/` and API docs at `/api/dotnet/`, `/api/rust/`, and `/api/python/`.
- [ ] All required and selected sections exist and are reachable from the navigation.
- [ ] No placeholder/TODO pages remain except the API landing placeholders, which are replaced by generated output at build time; every narrative navigation entry has real content.
- [ ] The ceria tutorial can be completed using only the docs and samples.
- [ ] Every public GUI screen has a user-guide page and screenshot.
- [ ] Physics pages match the implemented formulas and units tests.
- [ ] Migration/clean-break and glossary pages are present.
- [ ] `docs/planning/` is not published.
- [ ] The Docs CI job has remained green throughout implementation and the release artifact contains the complete site.
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
