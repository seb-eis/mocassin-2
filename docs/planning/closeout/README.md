# Step Closeout Evidence

This directory records the machine-checkable evidence for a step closeout. It ties together the frozen audit
commit, the closeout CI run, the `main` SHA used for the integration tree, and the final merge candidate.

## When to create it

Create `docs/planning/closeout/step-NN.json` from `_template.json` when the step reaches closeout, before the
independent audit. A split step uses `step-NN[a-z].json`, for example `step-23a.json`. Do not pre-seed files for
steps that have not started.

**Post-merge amendments do not create an evidence file.** An amendment integrated after its parent step is
merged (`step-NN-amend-M`, tagged `step-NN.M`) is not a step and has no step branch closeout. Its audit record
`docs/planning/audits/step-NN-amend-M.md` is the single evidence source for the audited commit, closeout CI and
`head_sha`, `main` SHA at freeze, merge commit, merge-tree CI, platform CI, and tag. The parent step's
`step-NN.json` is historical and is not rewritten; `tools/check_closeout_evidence.py` is not run for the
amendment. See [`../audits/README.md` §3.1](../audits/README.md#31-post-merge-amendment-audits).

## Step ID form

A step ID is `step-NN` or, for a split step, `step-NN` followed by one lowercase letter: `step-23a`, `step-23b`.
The letter is not a sub-index (`step-23aa` is invalid) and uppercase (`step-23A`) is invalid. The rule exists
because Step 23 was split into Step 23A and Step 23B; the suffix gives each part a distinct evidence file, audit
record, status row, and tag without renumbering the later steps. The rule home is
[`tools/check_closeout_evidence.py`](../../../tools/check_closeout_evidence.py) for the evidence check and
[`tools/check_process_consistency.py`](../../../tools/check_process_consistency.py) for audit records and status
rows. A post-merge amendment tag such as `step-11.1` is **not** a step ID: the checker's pattern deliberately
does not accept it, and the amendment uses its audit record instead of an evidence file.

## Required fields

| Field | Meaning |
| --- | --- |
| `step` | Step ID, for example `step-03`, or `step-23a` for a split step. |
| `audit_commit` | Full 40-character SHA of the commit the auditor reviews. |
| `closeout_ci` | Closeout CI run ID and the full `head_sha` the run checked. |
| `main_sha_at_freeze` | Full SHA of `main` that was merged into the step branch before the freeze. |
| `merge_candidate` | Full SHA of the commit that will be merged into `main`. |
| `merge_tree_ci` | Run ID and `head_sha` for the merge-tree validation, when required. |
| `platform_ci` | `not run`, or the Windows/macOS run reference. |
| `tag` | The step tag, when it exists. |

`platform_ci` and `tag` were added after Steps 00–10; those evidence files are historical and do not carry them.

From Step 11, this file is the single source for the audited commit, closeout CI, `main` SHA at freeze, merge
candidate, merge-tree CI, platform CI, and tag. The audit record links to it and does not restate those values.
Older records and evidence files are historical and are not rewritten.

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
