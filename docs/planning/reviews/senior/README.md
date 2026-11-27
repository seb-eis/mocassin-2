# Senior focused consultation

This directory holds **senior consultation records**. A senior consultation is not a design review, an audit, a
cross-cutting review, or a gate verdict. It answers one explicit question after two standard rounds of the same
gate have both left a Blocker or Major unresolved.

The normative rules are in
[`docs/development/senior-consultation.md`](../../../development/senior-consultation.md). This page is the local
index and record reminder.

## When a record is created

Only when all of these are true:

- two completed standard rounds of the same design review, audit, cross-cutting review, or policy-update review
  ended with `Changes required`, `Fixes required`, or an equivalent blocked state;
- at least one Blocker or Major from the second round is still open;
- the remaining disagreement can be reduced to **one** answerable question;
- the lead has provided the frozen commit, the parent record, the finding IDs, and the exact bounded evidence.

Do not create a senior record for a first round, for Minor or Note findings only, or to replace a standard round.

## Record name and ownership

- Name the file `step-NN-M.md` for step `NN` and consultation `M`, `xc-NN-M.md` for a cross-cutting review, or
  `policy-<date>-<slug>-M.md` for a policy-update review.
- The request owner creates the record from [`_template.md`](_template.md) and writes the request fields.
- The senior agent writes only the assignment check, answer or decline, evidence, and limitations.
- The lead and request owner may add follow-up status after the senior answer without altering the senior's text.

## Hard rules

- The senior agent edits only its assigned senior record and commits only that file with `[skip ci]`.
- The record is append-only; never rewrite an earlier consultation.
- The senior agent is a fresh context and inspects only the paths named in the request. It must not scan the
  repository or see the lead's history, the owner's preference, or another reviewer's reasoning.
- If the assignment is unclear or too broad, the senior agent writes `Declined`, names the missing information,
  and stops. It does not guess, broaden the question, or answer a different one.
- A senior answer is advisory. It does not change the parent record's status or verdict, close a finding, waive a
  gate, or authorize a merge or tag.
- After any fix, a fresh standard review or audit round at the new commit remains the gate.

## Index

| Step / review | Consultation | Question | Status | Record |
| --- | --- | --- | --- | --- |
| — | — | — | — | — |
