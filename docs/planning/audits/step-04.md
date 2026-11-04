# Step 04 Audit Request

## Request

| Field | Value |
| --- | --- |
| Step | 04 — Transition Model |
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
| Phase | B. Model definition |
| Depends on | 03 |
| Delivers | State changes, state-change groups, abstract chains, and KMC/MMC transitions bound to reference geometry, with validation of transition semantics. |

Step page: [`Step 04 — Transition Model`](../steps/step-04-transition-model.md)

## Step-specific acceptance checklist

- [ ] Ceria oxygen migration can be defined as a 3-site chain with `[O,V_O]`, `[O,∅]`, `[O,V_O]` and a stable-unstable-stable path.
- [ ] A 2-site MMC exchange `[A,B]` can be defined and previewed.
- [ ] A 5-site vehicle with five chain groups (`[A,V_A]`, `[A,V_A]`, `[AB,∅]`, `[B,V_B]`, `[B,V_B]`), a `D,S,S,D` chain, and an `S,S,U,S,S` reference path validates.
- [ ] Invalid paths (unstable endpoint, consecutive unstable, ring) are rejected with precise diagnostics.
- [ ] Complex transitions warn when no reverse is defined.
- [ ] The preview enumerates the expected symmetry-equivalent paths.
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
