# Senior focused consultation

This page is the normative home for the senior focused consultation: one advisory answer to one question after
two completed standard rounds of the same gate still leave a Blocker or Major. It is an escalation aid, not a
third standard round, not a gate verdict, and not a standing role. The lead requests it; the human owner decides.

The design classes and stop rule are in [`design-proportionality.md`](design-proportionality.md). The lead role
and spawn rules are in [`orchestration.md`](orchestration.md). The model tier is in
[`model-economics.md`](model-economics.md).

## 1. Trigger

The lead may request a senior consultation only when **all** of these are true:

1. two completed standard rounds of the **same** design review, audit, cross-cutting review, or policy-update
   review each ended blocked (`Changes required`, `Fixes required`, or the equivalent);
2. at least one Blocker or Major from the second round is still open;
3. the remaining disagreement can be reduced to **one** answerable question.

A first round, a round with only Minor or Note findings, a general wish for another opinion, or a request to
avoid a standard round is not a trigger. Do not use the senior agent to manufacture a preferred verdict.

## 2. Request contract

The request owner creates the senior record from the template in `docs/planning/reviews/senior/_template.md`.
The senior record is named `step-NN-M.md` for a step design or audit escalation, `xc-NN-M.md` for a
cross-cutting review, and `policy-<date>-<slug>-M.md` for a policy-update review. The parent review or audit
record links the senior record.

The request must contain:

- the parent record, gate, standard round numbers, and each round's verdict;
- the frozen commit under review;
- the open Blocker/Major finding IDs and the contradictory evidence;
- exactly one question;
- the exact files, diff range, commands, or other bounded evidence the senior may inspect;
- the out-of-scope boundary;
- what would count as an answer.

The parent record is a link or path, not a summary that replaces reading the named evidence.

## 3. Senior rules

- The senior consultant is a **fresh context**. It receives only the bounded assignment and the named paths. It
  must not see the lead's conversation history, the owner's preferred answer, another reviewer's private
  reasoning, or the request owner's internal notes. The lead must not pass any of these.
- Inspect only the assignment, the frozen commit, and the named paths, plus the minimum direct context needed to
  answer. Do not scan the repository or open unrelated files.
- If the assignment is incomplete, ambiguous, contradictory, too broad, or needs an unstated assumption or a
  repository-wide scan, **stop before inspecting anything**. Write `Declined` in the senior record, name the
  exact missing information, state the clarification needed, and report the same refusal to the lead. Do not
  guess and do not answer a different question.
- Answer only the single question, with evidence, assumptions, confidence, and what was not inspected.
- Edit only the assigned senior record and commit only that file with a documentation-only message and
  `[skip ci]`. Do not edit code, tests, configuration, contracts, design docs, the parent record, or any other
  file.
- Do not fix findings, write a gate verdict, approve or waive anything, spawn another agent, merge, tag, push
  `main`, or delete branches.

## 4. Effect

The senior answer is **advisory**. It does not change the parent record's status or verdict, close or reopen a
finding, waive a gate, or authorize a merge or tag. The lead relays it to the owner and the request owner. The
owner decides. After any fix, a fresh standard review or audit round at the new commit remains the gate. If the
senior declines, the lead obtains the missing information from the owner or request owner; it does not re-spawn
the same vague assignment.

A senior consultation is not a round and is not counted as one in the cumulative approval report. The report
records the senior record, the single question, whether it was answered or declined, and how the decision used
the advisory answer.

## 5. Record lifecycle

- Create the senior record only on escalation, from the template; do not pre-seed it.
- The record is append-only; never rewrite an earlier consultation.
- The senior writes the assignment check, answer or decline, evidence, and limitations. The lead and request
  owner may add follow-up status after the answer without altering the senior's text.
- A senior record that is not answered is `Declined`; it is not a failed gate and does not block a fix.
- The record is a planning record and is excluded from the published site.

## 6. Spawn card

The lead uses the senior spawn card in [`agent-workflow.md`](agent-workflow.md) §15. It checks the trigger
first, supplies the frozen commit, the parent record, the finding IDs, and an exact bounded inspection scope,
and never supplies a preferred answer or a request to review the repository.

## 7. Anti-patterns

- a senior consultation before two completed standard rounds, for Minor or Note findings only, or to avoid a
  standard round;
- a broad question such as "review this repository" or "find the problem";
- letting the senior guess, scan the repository, or answer a different question after a vague request;
- passing the lead's history, the owner's preference, or another reviewer's reasoning to the senior;
- treating the senior answer as a verdict, waiver, merge authorization, or an extra approval round;
- re-spawning the same vague assignment after a decline instead of obtaining the missing information.
