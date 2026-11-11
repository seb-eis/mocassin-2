# Step Closeout Evidence

This directory records the machine-checkable evidence for a step closeout. It ties together the frozen audit
commit, the closeout CI run, the `main` SHA used for the integration tree, and the final merge candidate.

## When to create it

Create `docs/planning/closeout/step-NN.json` from `_template.json` when the step reaches closeout, before the
independent audit. Do not pre-seed files for steps that have not started.

## Required fields

| Field | Meaning |
| --- | --- |
| `step` | Step ID, for example `step-03`. |
| `audit_commit` | Full 40-character SHA of the commit the auditor reviews. |
| `closeout_ci` | Closeout CI run ID and the full `head_sha` the run checked. |
| `main_sha_at_freeze` | Full SHA of `main` that was merged into the step branch before the freeze. |
| `merge_candidate` | Full SHA of the commit that will be merged into `main`. |
| `merge_tree_ci` | Run ID and `head_sha` for the merge-tree validation, when required. |

## Check

```sh
python tools/check_closeout_evidence.py docs/planning/closeout/step-NN.json
```

The checker fails when:

- a SHA is missing, malformed, or does not resolve to a commit;
- `audit_commit` and `closeout_ci.head_sha` differ;
- the closeout CI run ID is missing;
- the audit commit is not an ancestor of the merge candidate;
- `main_sha_at_freeze` is not an ancestor of the merge candidate;
- the merge candidate changes files outside `docs/` and the root guides (`AGENTS.md`, `README.md`,
  `CONTRIBUTING.md`, `SECURITY.md`) and no matching merge-tree CI run is recorded.

## Completed-step backfill

The closeout-evidence policy was introduced after Steps 00–06 were complete. Their files were reconstructed
from the durable audit records and GitHub Actions history:

| Step | Audit commit | Closeout CI | Merge candidate | Note |
| --- | --- | --- | --- | --- |
| `step-00` | `47a9a90` | `41733064540` | `47a9a90` | Audit covered the step merge commit and tag. |
| `step-01` | `05c41ab` | `41927992251` | `1a94a68` | Later delta is documentation-only. |
| `step-02` | `6341b56` | `42475359256` | `09a3317` | CI/workflow files changed after the audit; merge-tree run `42478356751` covers `09a3317`, and the remaining delta is documentation-only. |
| `step-03` | `b6a97ec` | `42514332091` | `ef4de06` | Later delta is documentation-only. |
| `step-04` | `8c44709` | `42695864895` | `6bebcf9` | Later delta is documentation-only. |
| `step-05` | `e6a7839` | `42881426273` | `4886716` | Later delta is documentation-only. |
| `step-06` | `ea6293e` | `43085605850` | `cc6926d` | Later delta is documentation-only. |

## Merge-tree rule

If `main` moves after the audit freeze and before the merge, do not merge an untested combination. Either:

1. merge the new `main` into the step branch, run the full local gate, dispatch one replacement closeout CI run,
   and re-audit the new commit; or
2. run an equivalent merge-tree CI job on the final merge candidate and record its run ID and `head_sha` in
   `merge_tree_ci`.

The evidence file must show that the audited commit, the closeout CI `head_sha`, and the merge candidate are
consistent. Never tag a merge result that was not validated.
