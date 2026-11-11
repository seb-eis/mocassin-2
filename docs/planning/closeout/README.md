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
- the merge candidate changes non-documentation files and no matching merge-tree CI run is recorded.

## Merge-tree rule

If `main` moves after the audit freeze and before the merge, do not merge an untested combination. Either:

1. merge the new `main` into the step branch, run the full local gate, dispatch one replacement closeout CI run,
   and re-audit the new commit; or
2. run an equivalent merge-tree CI job on the final merge candidate and record its run ID and `head_sha` in
   `merge_tree_ci`.

The evidence file must show that the audited commit, the closeout CI `head_sha`, and the merge candidate are
consistent. Never tag a merge result that was not validated.
