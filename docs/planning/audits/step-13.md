# Step 13 Audit Request

## Request

| Field | Value |
| --- | --- |
| Step | 13 — Rust Solver Foundation and Package Reader |
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
| Phase | D. Solver |
| Depends on | 11 |
| Delivers | A Rust workspace with typed package structures, a validated `.msl` reader, deterministic RNG, CLI scaffolding, structured logging, and a dry-run job inspector. |

Step page: [`Step 13 — Rust Solver Foundation and Package Reader`](../steps/step-13-rust-solver-foundation-and-package-reader.md)

## Step-specific acceptance checklist

- [ ] `mocassin-sim --db ceria.msl --job 1 --dry-run` reports the same counts as `mocassin inspect`.
- [ ] Future schema versions and corrupt/truncated payloads are rejected with clear errors.
- [ ] Signature mismatches are detected.
- [ ] Every index/length is validated before allocation.
- [ ] Seed derivation is deterministic and domain-separated; golden vectors pass.
- [ ] Loading a ceria job takes < 100 ms.
- [ ] The reader never opens the database writable.
- [ ] Fuzzing finds no panic/OOM on malformed input.
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
