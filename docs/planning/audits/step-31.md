# Step 31 Audit Request

## Request

| Field | Value |
| --- | --- |
| Step | 31 — Packaging and Release |
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
| Depends on | 22, 29, 30 |
| Delivers | Reproducible cross-platform binaries, NuGet and Python packages, installable archives/packages, the combined documentation artifact, samples, and a signed release process. |

Step page: [`Step 31 — Packaging and Release`](../steps/step-31-packaging-and-release.md)

## Step-specific acceptance checklist

- [ ] A clean machine can extract an archive and run the ceria tutorial.
- [ ] All artifacts report the correct version/SHA and pass the smoke test.
- [ ] `dotnet add package Mocassin.Results` works without manual path setup.
- [ ] `pip install mocassin-analysis` works and can read a release fixture `.mcs`.
- [ ] `mocassin-sim` runs standalone on an HPC node.
- [ ] Checksums/SBOM/provenance are attached to the release.
- [ ] The combined documentation site artifact is attached and its links/API subpaths verify.
- [ ] The release links to the migration guide and citation block.
- [ ] Licence/attribution/citation are present in the app and artifacts.
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
