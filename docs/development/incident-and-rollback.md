# Incident and Rollback

This page covers a bad change that reached `main`, a release, or a shared environment. It is deliberately
short: during an incident, clarity beats completeness.

## 1. Severity

| Severity | Meaning | Response |
| --- | --- | --- |
| **Sev 1** | Data loss, security breach, outage, or a broken public release | Stop normal work; owner leads; contain and roll back immediately. |
| **Sev 2** | Major user-visible regression or blocked release | Roll back or fix forward within the same day; owner notified. |
| **Sev 3** | Limited regression with a workaround | Fix in a normal maintenance branch; record the incident. |
| **Sev 4** | Minor defect or process near-miss | Fix normally; add a regression test and a note. |

## 2. Immediate actions

1. **Stop the bleeding.** If the change is still deploying or publishing, stop it.
2. **Preserve evidence.** Save logs, CI run IDs, commit hashes, and relevant records. Do not rewrite history or
   delete branches to “clean up” before the incident record exists.
3. **Contain.** Disable the feature, revert the release, rotate exposed credentials, or isolate the affected
   component.
4. **Notify the owner.** State impact, suspected cause, current action, and next update time.
5. **Record the incident** from
   [`docs/planning/incidents/_template.md`](https://github.com/seb-eis/mocassin-2/blob/main/docs/planning/incidents/_template.md).

## 3. Revert or fix forward

Prefer **revert** when:

- `main` is red or a release is broken;
- the cause is unclear;
- the change touches security, data integrity, or a public contract;
- the fix is larger than the revert;
- users are actively affected.

Prefer **fix forward** when:

- the cause is understood and the fix is small;
- reverting would break dependent changes;
- the regression is limited and the fix is already tested.

Rules:

- Never force-push `main`.
- Revert with a normal commit, not a history rewrite.
- If the bad state is tagged, do not move or delete the tag. Create a new patch release or a suffixed checkpoint.
- Run the full local gate and the relevant CI on the fix or revert.
- Record the exact commit, CI run, and rollback decision.

## 4. Hotfix flow

```sh
git switch main
git pull --ff-only
git switch -c fix/<slug>
# implement the smallest safe fix or revert
./tools/verify.sh all --require
git push -u origin fix/<slug>
```

Then:

1. Get the required owner approval for the merge.
2. Merge with `--no-ff`, push, and wait for the automatic `main` CI run.
3. Tag only a green `main`; use a new patch version or suffixed checkpoint if a tag already exists.
4. Merge the updated `main` into active step branches.
5. Add a regression test if the incident exposed a missing test.

## 5. Postmortem

A Sev 1 or Sev 2 incident gets a durable postmortem in
`docs/planning/incidents/YYYY-MM-DD-<slug>.md`. It is blameless: the goal is the system, not the person.

Include:

- impact and duration;
- timeline of detection, response, and recovery;
- root cause;
- what worked and what did not;
- action items with owners and dates;
- process, ADR, test, or cross-cutting-review changes;
- residual risk.

## 6. After the incident

- Update the relevant process page, ADR, or checklist.
- Add a regression test or a drift check.
- Run the next cross-cutting review early if the incident exposed broad drift.
- Remove temporary access or feature flags.
- Do not close the incident until every action item has an owner and a date.
