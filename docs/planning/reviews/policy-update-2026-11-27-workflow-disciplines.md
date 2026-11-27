# Policy Update 2026-11-27 — Adopt Upstream Workflow Disciplines

> Review agent: write evidence, findings, and verdict directly into this record. You may edit only your own
> metadata (`Review agent`, `Review date`), `Record state`, the review checklist marks and short inline
> verification notes (not item text), `Evidence`, `Findings`, `Verdict`, and the `Post-implementation
> verification` section. Commit only this file with a documentation-only message and `[skip ci]`. Do not edit
> the request, scope, non-goals, mapping table, design decisions, file manifest, open questions, resolutions, or
> any other file.

## Request

| Field | Value |
| --- | --- |
| Review ID | `POLICY-2026-11-27` |
| Type | Process and workflow policy update |
| Design class | **Full** |
| Base commit | `245e34aff7255476d73539d4cc43f068627f0cba` (`origin/main` at start) |
| Design freeze commit | Recorded after the design-set commit; the lead handoff carries the full SHA |
| Review request commit | This commit (documentation-only; adds the review record and the record-home enablement) |
| Request owner | Design agent, with the dev for implementation |
| Lead | — |
| Review initiated by | Lead, on request of the design agent |
| Review agent | — |
| Review date | — |
| Branch | `docs/adopt-workflow-disciplines` |
| Worktree | `/home/node/worktrees/adopt-workflow-disciplines` |
| Upstream reference | `seb-eis/opencode-workflow` at `b3230863c7a526622079ef595236cdc696f5c23a` (2026-11-27) |
| Authority | Owner decisions of 2026-11-27 (section “Owner decisions”) |

This is a **pre-implementation design review**. The normative pages are not rewritten in the design set. The
design decisions below are the specification the dev implements after the verdict; a separate, bounded
post-implementation verification pass then checks the implemented commit.

## Purpose

Adopt the selected improvements from the owner's upstream workflow repository into MOCASSIN's existing step
process, without renames, migrations, hooks, or model changes. The goal is a cheaper, more disciplined process:
task disciplines that route to existing MOCASSIN pages, one advisory senior escalation after a review deadlock,
explicit review axes, a single agent output contract, and a default-tier model policy whose independence comes
from a fresh context rather than a different model.

The change must leave the step model, `docs/planning/steps/`, `step/<NN>-<slug>` branches, `step-NN` tags, step
audits, and closeout evidence intact. It applies from adoption onward and does not rewrite Steps 00–14 records,
audits, closeouts, tags, or the accepted `XC-04` record.

## Owner decisions (binding)

1. Adopt groups A–E only, adapted to MOCASSIN's step model and `docs/development` home. Keep `step-*` naming,
   `docs/planning/steps/`, step branches, step audits, and closeout evidence.
2. Do **not** adopt the steps→work-packages rename, the `docs/development`→`docs/process` migration,
   `.githooks`/`GATE_HISTORY`, or any change to model IDs.
3. Full design class. This record needs an independent review before implementation.
4. Apply from adoption onward. Do not rewrite Steps 00–14 records, audits, closeouts, tags, or `XC-04`.
5. Land before Step 15 starts, then merge `main` into `docs/step-15-design` and re-freeze Step 15.
6. After implementation there is one bounded fresh verification pass at the implemented commit, separate from
   this design review.

**Environment facts confirmed for this design:** `opencode-go/deepseek-v4-pro` and
`opencode/deepseek-v4.1-flash-free` resolve in this environment; provider and model IDs are not changed by this
change except that the new `senior` binding uses `opencode-go/deepseek-v4-pro`.

## Scope

- Group A: eight task-discipline skills, their `docs/development` pages, `GLOSSARY.md`, research/prototype/retro
  record homes, and the `/grill`, `/handoff`, `/retro` commands.
- Group B: the `senior` escalation agent, its advisory record home, spawn card, trigger, and integration into
  the lead, orchestration, review, audit, and cross-cutting rules.
- Group C: Standards/Spec review-axis labels, an `Axis` column in the design-review, audit, and cross-cutting
  templates, and matching review-agent text.
- Group D: an explicit agent output contract in `AGENTS.md` and refreshed role prompts.
- Group E: a default-tier model-economics page; review independence is a fresh context, not a different model;
  `senior` is the only escalation.
- The checker, binding-test, link, navigation, and status updates needed to keep the above from drifting.
- The record-home decision for this change and future policy-update reviews.

## Non-goals

- No steps→work-packages rename, no `docs/planning/work-packages/` directory, and no `wp-*` vocabulary.
- No `docs/development`→`docs/process` migration. All new discipline pages live under `docs/development/`.
- No `.githooks`, `GATE_HISTORY`, `tools/check_record_history.py`, or `tools/setup_hooks.sh`.
- No change to provider IDs, existing model IDs, the two dev bindings, or the free-first fallback.
- No change to the step model, `docs/planning/steps/`, step branches, tags, audits, closeout evidence, CI
  policy, adoption profile, or the gate matrix.
- No new dependency, no code/contract/format/API change, no runtime behaviour change.
- No rewrite of historical records, including `docs/planning/maintenance/2026-11-22-policy-adoption.md` and the
  `status.md` note that records the earlier strong-tier decision.
- No new top-level `docs/templates/` directory; MOCASSIN keeps templates beside their record homes or in the
  playbook.

## Design class and review depth

**Full.** The change alters process gates, the role set (adds `senior`), the agent binding, and the normative
process pages. Those are public process contracts for every following step, and the review-record home needs a
binding change. It is not an architecture or format change, but the blast radius and irreversibility match the
Full trigger (“security or concurrency” is not the only high-trigger case; a gate/role/binding change is).

Review depth: the full checklist below. Stop when no Blocker or Major remains. Minor and Note findings are
recorded and accepted or deferred; they do not justify another round unless they are design-origin and
material. A third standard round needs an owner decision and a recorded reason.

## Mapping table

Legend: **Adopted** = taken with path/reference adaptation only; **Adapted** = content or process changed for
MOCASSIN; **Rejected** = deliberately not adopted. Every adopted or adapted row names the destination the dev
must create or change.

### A. Task disciplines

| Upstream item | Decision | MOCASSIN destination | Rationale |
| --- | --- | --- | --- |
| `docs/process/codebase-design.md` | Adapted | `docs/development/codebase-design.md` | Keep the deep-module vocabulary and design questions; point at `code-structure.md` for layout and `testing-and-parity.md` for tests instead of restating them. |
| `.opencode/skills/codebase-design/SKILL.md` | Adapted | `.opencode/skills/codebase-design/SKILL.md` | Keep the skill trigger and vocabulary; read path becomes `docs/development/codebase-design.md`; no `docs/process`. |
| `docs/process/diagnosing-bugs.md` | Adapted | `docs/development/diagnosing-bugs.md` | Keep the red-capable loop, minimisation, hypothesis ranking, instrumentation, and cleanup; point at MOCASSIN's testing policy, stop-and-ask triggers, and incident page. |
| `.opencode/skills/diagnosing-bugs/SKILL.md` | Adapted | `.opencode/skills/diagnosing-bugs/SKILL.md` | Same discipline, MOCASSIN paths. |
| `docs/process/domain-language.md` | Adapted | `docs/development/domain-language.md` | Keep the glossary/ADR discipline; define the boundary between root `GLOSSARY.md` and the existing published `docs/glossary/` page; ADR rules stay in `docs/planning/adr/`. |
| `.opencode/skills/domain-modeling/SKILL.md` | Adapted | `.opencode/skills/domain-modeling/SKILL.md` | Same, with the root glossary path. |
| `docs/process/discovery.md` | Adapted | `docs/development/discovery.md` | Keep frontier rounds and the 5–8 question budget; record outcomes in the step page, glossary, ADR, or `docs/planning/05-open-decisions.md`; step vocabulary. |
| `.opencode/skills/grilling/SKILL.md` | Adapted | `.opencode/skills/grilling/SKILL.md` | Same, step vocabulary; the question tool is preferred when available, not required. |
| `docs/process/prototypes.md` | Adapted | `docs/development/prototypes.md` | Keep one-question, throwaway, no-`main` rules; record the brief under `docs/planning/prototypes/`; never bypass the design class. |
| `.opencode/skills/prototype/SKILL.md` | Adapted | `.opencode/skills/prototype/SKILL.md` | Same. |
| `docs/process/research.md` | Adapted | `docs/development/research.md` | Keep primary-source and citation rules; note home is `docs/planning/research/`; redact secrets; link the note from the step or record that uses it. |
| `.opencode/skills/research/SKILL.md` | Adapted | `.opencode/skills/research/SKILL.md` | Same. |
| `docs/process/review-axes.md` | Adapted | `docs/development/review-axes.md` | Keep Standards/Spec, the pinned-commit rule, and one-verdict rule; point at MOCASSIN's design-review and audit records, not `reviews-and-audits.md`. |
| `.opencode/skills/review-axes/SKILL.md` | Adapted | `.opencode/skills/review-axes/SKILL.md` | Same, MOCASSIN record paths. |
| `docs/process/tdd.md` | Adapted | `docs/development/tdd.md` | Keep seams-first and red-green; defer layers, doubles, and fixtures to `docs/planning/03-quality-strategy.md` and `docs/development/testing-and-parity.md`. |
| `.opencode/skills/tdd/SKILL.md` | Adapted | `.opencode/skills/tdd/SKILL.md` | Same. |
| `docs/process/retro.md` | Adapted | `docs/development/retrospectives.md` | Keep the environment-improvement retro; output under `docs/planning/retrospectives/`; process change still follows `AGENTS.md` §3.15. |
| `docs/templates/grill.md` | Adapted | Section in `docs/development/discovery.md` | One home for the interview format; no new `docs/templates/` directory. |
| `docs/templates/prototype-brief.md` | Adapted | `docs/planning/prototypes/_template.md` | Record template lives with the record home. |
| `docs/templates/handoff-card.md` | Adapted | `docs/development/agent-workflow.md` §2 | MOCASSIN already has the handoff card; `/handoff` uses it and adds a discipline/skill line. |
| `docs/templates/approval-report.md` | Rejected | `AGENTS.md` §3.12 and `agent-workflow.md` §5 | MOCASSIN already has the normative cumulative approval report; a second template would drift. |
| `docs/process/context-and-agents.md` | Rejected | `docs/development/agent-workflow.md` | MOCASSIN's playbook already owns context, prompts, and handoff. |
| `docs/process/quality-strategy.md`, `code-structure.md`, `drift-checks.md`, `agent-security.md`, `incident-and-rollback.md`, `ci-and-gates.md`, `adoption-profiles.md`, `cross-cutting-reviews.md` | Rejected | Existing MOCASSIN homes | No duplication; the new pages link to them. |
| `docs/process/workflow.md`, `docs/process/reviews-and-audits.md` | Rejected | `AGENTS.md` §3, `docs/planning/reviews/README.md`, `docs/planning/audits/README.md` | MOCASSIN's branch/gate/record homes already exist and are more specific to the step model. |
| `docs/planning/work-packages/`, `wp-*` commands, `docs/planning/phases.md`, `project-brief.md`, `open-decisions.md`, `learnings.md` | Rejected | Existing step pages, `05-open-decisions.md`, `status.md`, maintenance records | Owner decision 2 and the step model. |
| `.githooks/`, `tools/check_record_history.py`, `tools/setup_hooks.sh`, `GATE_HISTORY` | Rejected | Existing local gates and owner approval | Owner decision 2; MOCASSIN's controls are `tools/verify.sh`, branch protection, and the review/audit gates. |
| `GLOSSARY.md` at root | Adopted with a boundary | `GLOSSARY.md` plus a boundary note in `docs/glossary/index.md` | The root file is the agents' ubiquitous language; `docs/glossary/` remains the published scientific glossary/notation/units reference. No term is duplicated across both. |

### B. Senior escalation

| Upstream item | Decision | MOCASSIN destination | Rationale |
| --- | --- | --- | --- |
| `.opencode/agents/senior.md` | Adapted | `.opencode/agents/senior.md` | Model `opencode-go/deepseek-v4-pro`; only `docs/planning/reviews/senior/step-*.md`, `xc-*.md`, and `policy-*.md` editable; no subagents. |
| Senior consultation rules in `reviews-and-audits.md` §2.4 | Adapted | `docs/development/senior-consultation.md` | One normative home for the trigger, request contract, senior rules, effect, and record naming. |
| `docs/planning/reviews/senior/README.md`, `_template.md` | Adapted | Same paths | Record names become `step-NN-M.md`, `xc-NN-M.md`, and `policy-<date>-<slug>-M.md`; step vocabulary retained. |
| Senior spawn card in `docs/templates/senior-review-spawn.md` | Adapted | `docs/development/agent-workflow.md` §15 | Keep one spawn-card home; no new templates directory. |
| Trigger: two completed standard rounds of the same gate, at least one Blocker/Major still open, one answerable question | Adopted | `senior-consultation.md`, `AGENTS.md` §3.9/§3.10, review/audit/cross-cutting READMEs | This is the whole point of the escalation; it is not a third standard round and not a verdict. |
| Senior as a standing reviewer or a general upgrade | Rejected | — | Owner decision; senior is advisory and escalation-only. |

### C. Review axes

| Upstream item | Decision | MOCASSIN destination | Rationale |
| --- | --- | --- | --- |
| `docs/process/review-axes.md` | Adapted | `docs/development/review-axes.md` | Standards and Spec definitions, when each applies, pinned commits, one verdict. |
| `Axis` column in the findings table | Adopted | Design-review, audit, and cross-cutting templates | Makes the separation visible in every record without merging the axes. |
| Review-agent prompt text | Adapted | `.opencode/agents/review.md`, `agent-workflow.md` §15 | One review context, two passes for an audit; Spec plus the five questions for a design review; cross-cutting uses the axes where they fit. |
| One record per axis, or a separate axis verdict | Rejected | — | One record, one verdict; the axes are labels, not separate gates. |

### D. Output contract

| Upstream item | Decision | MOCASSIN destination | Rationale |
| --- | --- | --- | --- |
| `AGENTS.md` §8 output contract | Adapted | `AGENTS.md` §17 “Agent output contract” | Same fields with MOCASSIN step vocabulary; no section renumbering of §4–§16. |
| Output-contract reminder in role prompts | Adopted | `lead`, `design`, `dev`, `dev-free`, `review`, `senior` prompts | Every role reports branch/commit, commands/results, CI, docs/status, limitations, approval requested. |
| Duplicate output template in `agent-workflow.md` | Rejected | Link to `AGENTS.md` §17 | One home; the approval report in §5 remains separate. |

### E. Model economics

| Upstream item | Decision | MOCASSIN destination | Rationale |
| --- | --- | --- | --- |
| Default-tier policy: lead/design/dev/review all on DeepSeek Flash | Adopted | `docs/development/model-economics.md` | The standing roles already use these model IDs; the page is restructured to say so. |
| Review independence from a fresh context, not a different model | Adopted | `model-economics.md`, `reviews/README.md`, `audits/README.md` | The current “must differ from the dev” wording is retired; fresh context is the hard rule. |
| Senior as the only escalation tier | Adopted | `model-economics.md` §3/§4/§6, `orchestration.md` | `opencode-go/deepseek-v4-pro`, one focused consultation. |
| `strong`/`workhorse` tier names | Adapted | Retired in normative pages; historical records keep them | The new names are `default` and `senior`; `dev-free` is the free variant of the default. |
| Free-first dev binding | Retained | `dev-free` then `dev` | Owner decision: no model-ID change; only the tier prose changes. |
| Frontier/premium model as a default | Rejected | — | Cost policy; senior is one step above the default, not the frontier. |

## Design decisions

### A. Task disciplines

**A1. Skill layout.** Add eight `.opencode/skills/<name>/SKILL.md` files with frontmatter `name` and
`description`, and a body that reads the matching `docs/development` page. The skill must not duplicate the page;
it is a trigger plus a pointer. Adapt the optional `question`/ask-tool language to “use the harness question tool
when it is available; otherwise ask the lead/owner in numbered text.” No skill may assume a tool that MOCASSIN
does not guarantee.

**A2. Discipline pages.** Add nine `docs/development` pages: `codebase-design.md`, `diagnosing-bugs.md`,
`domain-language.md`, `discovery.md`, `prototypes.md`, `research.md`, `review-axes.md`, `tdd.md`, and
`retrospectives.md`. They are task disciplines, not step records. Each must:
- state the discipline and when to use it;
- link to the existing MOCASSIN page that owns the related rule instead of restating it:
  `code-structure.md`, `testing-and-parity.md`, `agent-workflow.md`, `design-proportionality.md`,
  `drift-checks.md`, `agent-security.md`, `incident-and-rollback.md`, `docs/planning/03-quality-strategy.md`,
  `docs/planning/05-open-decisions.md`, and `docs/planning/adr/`;
- record its durable output in the named home, not in chat;
- keep the anti-pattern list short and concrete.

**A3. `GLOSSARY.md` boundary.** Create a root `GLOSSARY.md` as the ubiquitous language for contributors and
agents: project-specific terms, one or two sentences each, rejected synonyms under `_Avoid_`. It must not become
a specification. Add a boundary note to `docs/glossary/index.md`: root `GLOSSARY.md` is the working language for
contributors/agents; `docs/glossary/` remains the published scientific glossary, notation, and units reference.
No term is defined in both. Initial terms may include Step, Design class, Review axis, Record state, Senior
consultation, Discipline, Prototype, Research note, Retrospective, and Handoff; the dev adds them only as they
are resolved.

**A4. Record homes.**
- `docs/planning/research/README.md` and `_template.md`: one bounded question, answer with confidence, sources
  with version/section, what was not checked, effect on the step/decision.
- `docs/planning/prototypes/README.md` and `_template.md`: one question, cheapest shape, location/branch,
  answer and evidence, decision and where recorded, what was discarded.
- `docs/planning/retrospectives/README.md` and `_template.md`: observed problem and evidence, proposed change,
  owner and carrying page, expected effect, check/pointer/process/no-op, next action.
All three are under `docs/planning/` and remain excluded from the published site.

**A5. Commands.** Add `.opencode/commands/grill.md`, `handoff.md`, and `retro.md`, all `agent: lead`:
- `/grill <step-slug>` runs one discovery frontier round, numbers questions, recommends an answer, waits for the
  owner, and routes settled decisions to the design agent or dev. It does not design or write records.
- `/handoff <step-slug>` reads the durable records, builds the handoff card from `agent-workflow.md` §2 plus the
  applicable discipline/skill, and routes the card to the dev. It does not write.
- `/retro <step-slug>` reads the durable evidence, presents candidates grouped by navigation, checks, standards,
  steering files, tool economy, information access, and prototypes/research; every candidate needs an owner and
  an action. It routes the record to the dev and any process change through `docs/<slug>`.
The commands use `step-*` vocabulary and never `/wp-*`.

**A6. Index and navigation.** Add the nine pages to `docs/development/index.md` and to the Development section
of `zensical.toml` (a nested “Task disciplines” group is acceptable). Add `research/`, `prototypes/`,
`retrospectives/`, and `reviews/senior/` to the planning README reading order. `docs/planning/` stays excluded.

### B. Senior escalation

**B1. Normative home.** Add `docs/development/senior-consultation.md` as the single home for:
- **Trigger** — only when (a) two completed standard rounds of the *same* design review, audit, or cross-cutting
  review each ended blocked (`Changes required` / `Fixes required`), (b) at least one Blocker or Major from the
  second round is still open, and (c) the remaining disagreement reduces to **one** answerable question. A
  first round, a round with only Minor/Note findings, or a wish for another opinion is not a trigger.
- **Request contract** — parent record, gate, round numbers and verdicts, frozen commit, open finding IDs and
  contradictory evidence, one question, exact bounded paths/diff/commands, out-of-scope boundary, and what
  counts as an answer.
- **Senior rules** — inspect only the assignment and named paths; if the assignment is incomplete, ambiguous,
  contradictory, or too broad, write `Declined` before inspecting and name the missing information; answer only
  the question with evidence, assumptions, confidence, and what was not inspected; write only the assigned
  record and commit it with `[skip ci]`; no gate verdict, no waiver, no fix, no merge/tag/push, no subagents.
- **Effect** — advisory only. It does not change the parent record’s status or verdict, close or reopen a
  finding, waive a gate, or authorize a merge or tag. After any fix, a fresh standard round at the new commit
  remains the gate. The owner decides.
- **Record naming** — `docs/planning/reviews/senior/step-NN-M.md` for a step design/audit escalation,
  `xc-NN-M.md` for a cross-cutting escalation, and `policy-<date>-<slug>-M.md` for a policy review.
- **Spawn card** — in `agent-workflow.md` §15, after the standard review card.

**B2. Binding.** Add `.opencode/agents/senior.md` with:
- `description`, `mode: subagent`, `model: opencode-go/deepseek-v4-pro`;
- deny all edits, then allow `docs/planning/reviews/senior/step-*.md`,
  `docs/planning/reviews/senior/xc-*.md`, and `docs/planning/reviews/senior/policy-*.md`;
- deny `subagent *`;
- a body that restates the decline-before-inspecting rule, the one-question rule, the record-only commit, and
  the advisory effect, and points at `docs/development/senior-consultation.md`.

Add `senior` to the lead’s allowed subagents. The lead may request it only on the trigger and only with a
bounded assignment. The lead must not use it as a normal review round or pass a preferred answer.

**B3. Integration.**
- `AGENTS.md` §3.8: “four roles” becomes “four standing roles, one escalation role, and one owner”; add a
  Senior consultant row; update the binding paragraph.
- `AGENTS.md` §3.9 and §3.10: after the re-review/re-audit step, add one paragraph: after two completed full
  rounds still leave a Blocker or Major, the lead may request one senior consultation before a third standard
  round; the answer is advisory, the owner decides, and a fresh standard round at the new commit remains the
  gate. Do not make senior a verdict or a round.
- `AGENTS.md` §3.15: add a `Senior escalation` row pointing at `senior-consultation.md`.
- `orchestration.md`: add senior to the diagram as a dashed escalation, to “Route”, to “Assign the tier”, to the
  context/spawn list, to the boundaries, and to the harness-binding table; add a short “Senior consultation”
  subsection that links the normative page (no duplicated trigger text).
- `reviews/README.md`, `audits/README.md`, `cross-cutting/README.md`: add a “Senior escalation” subsection that
  links the normative page, names the record path, and repeats only the record-format facts the reviewer needs.
- `model-economics.md`: the senior tier and the one-consultation budget.
- `agent-workflow.md` §8 and §12: add the senior trigger and the senior anti-patterns.

### C. Review axes

**C1. Normative home.** Add `docs/development/review-axes.md`:
- **Spec** — does the change implement the frozen step page, feature request, design decisions, and acceptance
  criteria? Quote the requested line for every finding.
- **Standards** — does it follow the repository’s documented standards (`AGENTS.md`, `code-structure.md`,
  `testing-and-parity.md`, `design-decisions.md`, language conventions)? Skip what tooling already enforces.
- **Design review** — Spec plus the five design questions; Standards only for design-surface standards such as
  contracts, interfaces, and ADR consistency. Do not spend a design round on code layout, prose, file limits, or
  status bookkeeping; those stay at the pre-audit gate.
- **Audit** — both axes, one review context, two passes; label every finding; write one verdict reflecting the
  worst outcome.
- **Cross-cutting** — use the axes only where they fit; its repository-wide checklist remains primary.
- **No spec** — report it as a finding (at least Major for a required gate) and continue the Standards pass; do
  not invent a spec.
- Pin the base and reviewed commit; use a three-dot diff; a bad ref or empty diff is a failed request.

**C2. Templates.** Add an `Axis` column (values `Standards` / `Spec`) to the findings tables in
`docs/planning/reviews/step-design-review-template.md`, `docs/planning/audits/step-audit-template.md`, and
`docs/planning/reviews/cross-cutting/cross-cutting-review-template.md`. Keep the existing `Origin` column; axis
and origin are different dimensions.

**C3. Record and prompt text.** Add the axis rule to `reviews/README.md` §6, `audits/README.md` §5, and the
cross-cutting README. Update `.opencode/agents/review.md` and the spawn card in `agent-workflow.md` §15. Add a
short reference in `AGENTS.md` §3.9/§3.10 and a `Review axes` row in §3.15.

### D. Output contract

**D1. `AGENTS.md` §17.** Add a new section after §16 (no renumbering of §4–§16):

```text
Step / feature:
Branch:
Commits:
Commands run and results:
CI run (if any):
Docs/status/parity updated:
Known limitations and risks:
Approval requested for:
```

Rules: do not ask for approval from prose alone; provide the diff and evidence. For a merge, tag, or
Blocker/Major waiver, also provide the cumulative approval report from §3.12. A review or senior agent reports
the record path, verdict/answer, and exact commands instead of a narrative.

**D2. Role prompts.** End each of `lead`, `design`, `dev`, `dev-free`, `review`, and `senior` with the applicable
fields from §17. Replace the dev’s current “output contract in agent-workflow.md” with “`AGENTS.md` §17”. Keep
the review agent’s record-only rule and the senior’s advisory rule unchanged.

**D3. One home.** `agent-workflow.md` §5 keeps the cumulative approval report and links to §17 for the everyday
output contract. Do not copy the block into a second page.

### E. Model economics

**E1. Rewrite `model-economics.md`.** Use `default` and `senior` as the tier names. The default policy is:
- lead, design, review, and dev use the default binding `opencode-go/deepseek-v4.1-flash`;
- `dev-free` uses `opencode/deepseek-v4.1-flash-free`; fall back to `dev` only on a rate limit, degraded model,
  or unavailability;
- review independence is a fresh context, not a different model; the review agent may use the same model as the
  dev;
- `senior` is `opencode-go/deepseek-v4-pro`, one focused consultation after the trigger, not a standing role;
- never use a premium-cap or frontier model as a default; a frontier upgrade is an owner decision.

**E2. Role table.** Lead/default, Design/default, Dev/default (`dev-free` first), Review/default, Senior/senior
(escalation only). State that design review and audit use the same `review` agent, fresh context.

**E3. Escalation.** Split into (a) within the default model (re-brief, fresh context, same bounded assignment)
and (b) senior focused consultation (the trigger and effect in `senior-consultation.md`). Remove the old
“escalate from workhorse to strong” list or rewrite it as “re-brief on the default model; use senior only on the
review/audit deadlock”.

**E4. Cost guardrails.** Keep one subscription, hard cap, no volume credits, free capacity first, and review the
bill with the work. Add a senior budget: one consultation per unresolved gate question; repeat only with a new
explicit question and after telling the owner why the earlier answer did not settle it.

**E5. Retire the old tier words.** Remove `workhorse` and `strong tier` from the normative process pages:
`model-economics.md`, `orchestration.md`, `design-proportionality.md`, `agent-workflow.md`, and the agent file
comments. Historical records (`docs/planning/maintenance/2026-11-22-policy-adoption.md`, the `status.md` note
about the earlier strong-tier decision) are not rewritten. Add a new `status.md` note that records the adoption
and the default/senior names.

**E6. Binding tests.** `tools/tests/test_opencode_binding.py` keeps the existing model assertions and adds:
senior model and mode; lead may spawn senior; review may edit `docs/planning/reviews/policy-*.md`; the three new
commands use the lead; `senior` denies subagents and edits outside its record paths.

### F. Record home for policy-update reviews

**Decision.** The review record for this change is
`docs/planning/reviews/policy-update-2026-11-27-workflow-disciplines.md`. Future policy updates use
`docs/planning/reviews/policy-<YYYY-MM-DD>-<slug>.md`. This is a **policy-update review**, not a step design
review, an audit, or a phase checkpoint, so the step/audit/XC namespaces are wrong.

**Binding.** `.opencode/agents/review.md` gains an allow rule for `docs/planning/reviews/policy-*.md` (already
in this design set) and the body names the policy record. `docs/planning/reviews/README.md` §9.1 defines the
class and its write scope (already in this design set). The review agent still edits only the assigned record
and commits only that file with `[skip ci]`.

**How this very review is written.** The review agent must be spawned in a session whose project root is a
worktree of the frozen branch (`/home/node/worktrees/adopt-workflow-disciplines`), so the frozen
`.opencode/agents/review.md`—which contains the `policy-*.md` allow rule—is the loaded definition. It reads this
record and the design set, writes its metadata/checklist/evidence/findings/verdict into this file, and commits
only this file. The design freeze is the commit before the request commit; the request commit is the parent of
the review agent’s record commit.

**If the updated definition cannot be loaded.** If the harness loads agent definitions from the original
`/workspace` project root rather than the frozen worktree, the new allow rule is absent and the review agent will
be denied on edit. The agent must stop and report `Blocked — binding does not permit the assigned policy
record`, not use shell to write and not ask the dev to transcribe its findings. The lead then either restarts the
OpenCode service with the frozen worktree as the project root or obtains a one-off owner permission. The dev
must not transcribe a review; that would reintroduce the scribe step this process forbids. This is reported as an
open question to the lead (section “Open questions”), because the harness load path is not a repository fact.

### G. Checker and binding alignment

**G1. `tools/check_process_consistency.py`.**
- Add to `REQUIRED_FILES`: the nine discipline pages, `docs/development/senior-consultation.md`,
  `docs/development/review-axes.md`, root `GLOSSARY.md`, the three record-home README/template pairs,
  `docs/planning/reviews/senior/README.md` and `_template.md`, `.opencode/agents/senior.md`,
  `.opencode/commands/{grill,handoff,retro}.md`, and the eight skill files.
- Add to `REQUIRED_INTEGRATION`: `AGENTS.md` contains `output contract` and `senior consultation`;
  `docs/development/senior-consultation.md` contains `two completed standard rounds`;
  `docs/development/review-axes.md` contains `Standards` and `Spec`; the three findings templates contain
  `| Axis |`; `.opencode/agents/review.md` contains `policy-*.md`; `.opencode/agents/lead.md` contains `senior`;
  `docs/development/model-economics.md` contains `default` and `senior`;
  `docs/development/orchestration.md` contains `senior`; `docs/development/agent-workflow.md` contains
  `senior consultation`; `docs/planning/reviews/README.md` contains `policy-update`.
- Add a targeted `check_tier_vocabulary` that fails if `model-economics.md`, `orchestration.md`,
  `design-proportionality.md`, or `agent-workflow.md` still contain `workhorse` or `strong tier` (the historical
  `status.md` and maintenance records are not scanned). This prevents the old tier names from returning without
  forcing a rewrite of history.
- Add the new pages to `NORMATIVE_FILES` so the forbidden-term scan covers them.
- Do not weaken an existing check. If a new required path cannot be satisfied without weakening one, stop and
  report.

**G2. `tools/tests/test_process_consistency.py`.** Add focused temporary-tree tests for the new required file,
the missing `Axis` column, and the retired tier vocabulary. The repository-wide positive test remains.

**G3. `tools/tests/test_opencode_binding.py`.** Extend as in E6. Add a command test for `grill`, `handoff`, and
`retro`, and a review-path test for `policy-*.md`.

**G4. No new dependency.** The checker and tests use the standard library only.

### H. Link integrity and publication

- Renaming or moving pages is out of scope; no existing page is moved. All new `docs/development` pages are
  linked from `docs/development/index.md` and added to `zensical.toml`; the planning record homes are excluded
  by `exclude_docs = "planning/"`.
- `GLOSSARY.md` is a root file; link it from `docs/development/domain-language.md`, `AGENTS.md` §2, and
  `docs/development/index.md` so it is discoverable and link-checked.
- `docs/glossary/index.md` gets the boundary note; it is not renamed.
- `docs/planning/README.md` gains the new record homes and the senior record index in its reading order, but the
  content stays a link, not a copy.
- `tools/check_docs_links.py`, `python tools/check_doc_math.py`, and `zensical build --strict` are the link
  gates. Any new page that is not linked is a drift finding, not an orphan.

### I. Post-implementation verification pass

**Purpose.** A single, bounded, fresh-context pass that checks the dev implemented the frozen design exactly. It
is **not** a second design round and does not re-open the design verdict.

**When.** After the dev has implemented groups A–E, updated the checker/tests/navigation/status, run the local
gates, and frozen the implemented commit. Before the change is merged.

**Agent.** A fresh review agent that did not author the design and did not implement the change. It must be
different from the design reviewer (or, if the same context is reused, it must have stayed read-only and outside
the implementation; a fresh context is preferred). Read-only except this record.

**What it verifies.**
1. Every mapping row marked Adopted/Adapted has a corresponding file/content change, or a recorded deviation.
2. The normative-home map holds: no rule is duplicated in a second page; the summaries link.
3. `AGENTS.md` §3.8/§3.9/§3.10/§3.15/§17 and §2 are updated in the same change; no section was renumbered.
4. The reviewer can write this record under the binding (the verification section itself is the evidence); the
   review agent path `policy-*.md` is present in the frozen `review.md`.
5. `tools/check_process_consistency.py` and `tools/tests/test_opencode_binding.py` cover the new roles, paths,
   axes, and retired tier names; the new negative tests fail on the old tree.
6. `./tools/verify.sh docs --require` and `./tools/verify.sh policy --require` pass at the implemented commit.
7. No historical record changed: the diff against `origin/main` contains no Step 00–14 page, review, audit,
   closeout, amendment, tag record, `XC-04`, or `docs/planning/maintenance/` edit. Use
   `git diff --name-only origin/main...HEAD` and check against the allowlist.
8. No `src/`, `tests/`, `contracts/`, or generated-artifact change; no provider/model-ID change beyond adding
   `senior`; no `docs/process` directory; no `wp-*` vocabulary; no `.githooks`/`GATE_HISTORY`.
9. Links, navigation, and the published-site exclusion are intact.

**Where it records.** Append to this same record under `## Post-implementation verification`, with
`Verification state` (`Not run` / `In progress` / `Verified` / `Changes required`), `Verification evidence`,
`Verification findings` (same ID/axis/severity/origin table), and `Verification verdict`. The design verdict and
its round history stay unchanged. If the pass finds a Blocker/Major, the dev fixes and a new verification pass
runs; if the fix changes the design, a design re-review is required instead.

**Why it is distinct.** The design review answers “is the design complete, consistent, and safe to implement?” at
the design freeze. The verification pass answers “was the frozen design implemented exactly, and do the gates
pass?” at the implemented commit. Different commit, different question, different checklist, different agent.

## Normative home map

| Rule | Normative home | Summary/link only |
| --- | --- | --- |
| Branch model, feature integration, step completion, tags, roles, approval report | `AGENTS.md` §3 (this page's summaries do not replace it) | `orchestration.md`, `agent-workflow.md`, `CONTRIBUTING.md` |
| Lead role, delegation, spawning | `docs/development/orchestration.md` | `AGENTS.md` §3.8, `agent-workflow.md` |
| Model tiers, role assignment, free-first binding, escalation | `docs/development/model-economics.md` | `orchestration.md` §2, `AGENTS.md` §3.15 |
| Design classes, stop rule, pre-audit gate, finding-origin metric | `docs/development/design-proportionality.md` | review/audit READMEs, `AGENTS.md` §3.9 |
| Senior trigger, request contract, senior rules, effect | `docs/development/senior-consultation.md` | `AGENTS.md` §3.9/§3.10, review/audit/cross-cutting READMEs, `orchestration.md` |
| Review axes | `docs/development/review-axes.md` | templates' `Axis` column, review/audit READMEs, `review.md` |
| Agent output contract | `AGENTS.md` §17 | role prompts, `agent-workflow.md` §5 |
| Task disciplines | each `docs/development/<discipline>.md` | `.opencode/skills/*/SKILL.md`, `docs/development/index.md` |
| Research, prototype, retrospective records | their `README.md`/`_template.md` under `docs/planning/` | discipline pages |
| Policy-update review records | `docs/planning/reviews/README.md` §9.1 | `AGENTS.md` §3.15, `review.md` |
| Step sizing, complexity class, split triggers | `docs/planning/04-roadmap.md` §3.1 | `AGENTS.md` §7.2, `agent-workflow.md` |
| Drift checks | `docs/development/drift-checks.md` | `status.md` |
| Agent security | `docs/development/agent-security.md` | `AGENTS.md` §3.6 |
| Incident and rollback | `docs/development/incident-and-rollback.md` | `AGENTS.md` §3.6 |
| Closeout evidence | `docs/planning/closeout/README.md` | `AGENTS.md` §3.4/§3.10 |

## File manifest

### Added

- `.opencode/skills/{codebase-design,diagnosing-bugs,domain-modeling,grilling,prototype,research,review-axes,tdd}/SKILL.md`
- `.opencode/commands/{grill,handoff,retro}.md`
- `.opencode/agents/senior.md`
- `GLOSSARY.md`
- `docs/development/codebase-design.md`, `diagnosing-bugs.md`, `domain-language.md`, `discovery.md`,
  `prototypes.md`, `research.md`, `review-axes.md`, `tdd.md`, `retrospectives.md`,
  `senior-consultation.md`
- `docs/planning/research/README.md`, `docs/planning/research/_template.md`
- `docs/planning/prototypes/README.md`, `docs/planning/prototypes/_template.md`
- `docs/planning/retrospectives/README.md`, `docs/planning/retrospectives/_template.md`
- `docs/planning/reviews/senior/README.md`, `docs/planning/reviews/senior/_template.md`

### Changed

- `AGENTS.md` (section 2; 3.8; 3.9; 3.10; 3.15; new 17; 16 checklist)
- `docs/development/orchestration.md`
- `docs/development/model-economics.md`
- `docs/development/design-proportionality.md`
- `docs/development/agent-workflow.md`
- `docs/development/agent-security.md`
- `docs/development/drift-checks.md`
- `docs/development/contributing.md`
- `docs/development/index.md`
- `docs/glossary/index.md` (boundary note only)
- `docs/planning/README.md`
- `docs/planning/status.md` (adoption note; no historical rewrite)
- `docs/planning/reviews/README.md` (§9.1 already added; axis and senior links)
- `docs/planning/audits/README.md`
- `docs/planning/reviews/cross-cutting/README.md`
- `docs/planning/reviews/step-design-review-template.md`
- `docs/planning/audits/step-audit-template.md`
- `docs/planning/reviews/cross-cutting/cross-cutting-review-template.md`
- `.opencode/agents/lead.md`, `design.md`, `dev.md`, `dev-free.md`, `review.md`
- `opencode.json` only if the harness requires the senior agent to be registered; prefer no change because
  `default_agent` stays `lead`
- `zensical.toml`
- `tools/check_process_consistency.py`
- `tools/tests/test_process_consistency.py`
- `tools/tests/test_opencode_binding.py`

### Deliberately not touched

- `docs/planning/steps/**`, including `step-15-energy-engine.md`
- `docs/planning/audits/step-*.md`, `docs/planning/reviews/step-*.md`,
  `docs/planning/reviews/cross-cutting/xc-*.md`, `docs/planning/closeout/**`,
  `docs/planning/amendments/**`, and every tag
- `docs/planning/maintenance/**` (including `2026-11-22-policy-adoption.md`)
- `src/`, `tests/` (except `tools/tests/`), `contracts/`, generated artifacts
- `.github/workflows/**`, `tools/verify.sh`, `docs/planning/ci-policy.md`
- `README.md`, `CONTRIBUTING.md`, `SECURITY.md`, `NOTICE.md`, `LICENSE`
- `docs/process/**` (does not exist and is not created)
- `.githooks/**`, `GATE_HISTORY`, `tools/check_record_history.py`, `tools/setup_hooks.sh`

## Gates at the freeze

Both modes exist in `tools/verify.sh`:

- `./tools/verify.sh docs --require` — `git diff --check`; `tools/check_doc_math.py`;
  `tools/check_docs_links.py`; `tools/docs/build-docs.sh` (Zensical strict, DocFX with warnings as errors,
  rustdoc). It builds the published site from `docs/`, so it catches broken links, anchors, navigation, and
  DocFX/rustdoc regressions.
- `./tools/verify.sh policy --require` — `run_api` (Release build, NuGet licence/vulnerability audit,
  ApiCompat) plus `run_contracts` (manifest/projection/symmetry drift, `check_process_consistency.py`, the
  `tools/tests` suite, SPDX headers, secret scan, `cargo-deny`). `--require` makes a missing `cargo-deny` a
  failure.

Both exist in `tools/verify.sh` and were run on the design set. The design set is documentation-only; the policy
mode still runs the code/platform gates because that is what the mode covers. Exact commands and results at the
design freeze are recorded in the request owner evidence below. If a gate cannot pass for an unrelated
environment reason, it is reported, not waived, and the change stops.

## Risks

| Risk | Severity | Mitigation |
| --- | --- | --- |
| The review agent cannot load the updated `review.md` in the frozen worktree, so it cannot write the policy record. | Major | Spawn from the frozen worktree; if the harness still loads the original root, stop and report `Blocked`; restart the service or obtain a one-off owner permission. Never transcribe. |
| The new discipline pages duplicate existing MOCASSIN pages and drift. | Major | Each page is a discipline + pointer; the normative-home map and the checker’s required integration text enforce the links. |
| The senior escalation becomes a third standard round or a verdict. | Major | Trigger is exactly two completed blocked standard rounds and one question; the record says advisory; the owner decides; fresh standard round remains the gate. |
| The old `strong`/`workhorse` vocabulary survives in a normative page. | Minor | Targeted `check_tier_vocabulary` on the four process pages; historical records are excluded. |
| The Axis column is added but findings are still merged or re-ranked. | Minor | The review prompt and the axis page require one label per finding and one verdict; the template shows the column. |
| New development pages become orphans or break links. | Minor | Add to `index.md` and `zensical.toml`; `check_docs_links.py` and `zensical build --strict` are gates. |
| Historical records are accidentally rewritten. | Major | File manifest excludes them; the verification pass checks `git diff --name-only origin/main...HEAD`. |
| The senior model is unavailable or more expensive than expected. | Minor | Owner confirmed it resolves; one consultation per question; the lead reports the cost impact in the approval report. |
| `GLOSSARY.md` and `docs/glossary/` overlap. | Minor | Explicit boundary note; no term defined in both; the discipline page points to the right one. |

## Open questions for the lead

1. **Agent-definition load path.** Does the OpenCode harness load `.opencode/agents/*.md` from the session’s
   worktree, or only from the original `/workspace` project root? The design requires the former so the review
   agent sees the `policy-*.md` allow rule. If the latter, the review of this very change cannot be written under
   the binding until the service is restarted with the frozen worktree or the owner grants a one-off permission.
   This is not a repository fact and is not guessed. The fallback is recorded in section F.
2. **Policy record glob breadth.** The allow rule is `docs/planning/reviews/policy-*.md`; the procedural rule
   limits the reviewer to the assigned record. If the lead prefers a narrower pattern
   (`policy-20??-??-??-*.md`), the dev can narrow it and the binding test can assert the narrower path. The
   design prefers the broad glob for simplicity.

No other question is open. The owner decisions and the environment facts resolve the rest.

## Request owner evidence

Commands and results at the design freeze (the design-set commit) are recorded here by the design agent:

- `git rev-parse origin/main` — `245e34aff7255476d73539d4cc43f068627f0cba`.
- `git diff --check` — exit 0, no output.
- `python tools/check_docs_links.py` — `Documentation links OK (217 files scanned)`.
- `python tools/check_process_consistency.py` — `Process consistency: OK`.
- `./tools/verify.sh docs --require` — exit 0. It ran `check_doc_math.py`, `check_docs_links.py`, the strict
  Zensical build, DocFX, and rustdoc, and finished with
  `Closeout gate completed with required checks enabled.` and `Documentation built in <worktree>/site`.
- `./tools/verify.sh policy --require` — exit 0. It ran the Release build, the NuGet licence/vulnerability
  audit, ApiCompat (`Public API is compatible with the checked-in baseline.`), the contract/symmetry drift
  checks (`Contract artifacts are current (26 files)`, 5 + 4 + 2 projection fixtures, 274 space-group
  settings), `Process consistency: OK`, the tool suite (`Ran 62 tests … OK`), SPDX headers (1210 source
  files), the secret scan (1755 tracked text files), and `cargo-deny` (`advisories ok, bans ok, licenses ok,
  sources ok`), finishing with `Closeout gate completed with required checks enabled.`
- `git diff --name-only origin/main...HEAD` — recorded in the request commit (commit B) as the design-set
  delta: this record, `docs/planning/reviews/README.md`, and `.opencode/agents/review.md`. No other path is
  changed at the design freeze.

> The review agent may rerun the commands and replace this evidence with its own observations. The request
> owner evidence is part of the request and is not the review agent’s verdict.

## Review checklist

Mark what was reviewed; add short inline notes only. Do not change item text.

### A. Task disciplines

- [ ] Eight skill files exist, name the discipline, and point at the matching `docs/development` page.
- [ ] Nine discipline pages exist and link to the existing MOCASSIN homes instead of duplicating them.
- [ ] `GLOSSARY.md` is the agent/human ubiquitous language; `docs/glossary/` keeps the published scientific
      glossary; the boundary is explicit and no term is defined twice.
- [ ] `docs/planning/research/`, `prototypes/`, and `retrospectives/` each have a README and template with the
      fields in the design.
- [ ] `/grill`, `/handoff`, and `/retro` use `agent: lead`, `step-*` vocabulary, and route records to the
      design agent or dev without writing them.
- [ ] The new pages are in `docs/development/index.md` and the Zensical navigation; `docs/planning/` stays
      excluded.

### B. Senior escalation

- [ ] `senior-consultation.md` is the single home for the trigger, request contract, senior rules, effect, and
      record naming.
- [ ] `.opencode/agents/senior.md` uses `opencode-go/deepseek-v4-pro`, edits only its record paths, and cannot
      spawn.
- [ ] The lead can spawn `senior` only on the trigger; senior is not a standing role and not a verdict.
- [ ] `AGENTS.md` §3.8/§3.9/§3.10/§3.15, `orchestration.md`, and the review/audit/cross-cutting READMEs
      integrate the escalation with one link and no duplicated trigger text.
- [ ] The senior record names are `step-NN-M.md`, `xc-NN-M.md`, and `policy-<date>-<slug>-M.md`.

### C. Review axes

- [ ] `review-axes.md` defines Standards and Spec, when each applies, the pinned-commit rule, and one verdict.
- [ ] The design-review, audit, and cross-cutting findings tables have an `Axis` column.
- [ ] The review prompt and spawn card require one axis label per finding without merging or re-ranking.
- [ ] A design review does not spend a round on code layout, prose, or bookkeeping.

### D. Output contract

- [ ] `AGENTS.md` §17 exists and carries the eight fields and the evidence/approval rules.
- [ ] Lead, design, dev, dev-free, review, and senior prompts finish with the applicable §17 fields.
- [ ] The everyday output contract has one home; `agent-workflow.md` §5 links to it and keeps only the
      cumulative approval report.

### E. Model economics

- [ ] `model-economics.md` uses `default`/`senior`, puts lead/design/dev/review on the default binding, and
      makes review independence a fresh context, not a different model.
- [ ] `dev-free` then `dev` remains the only free-first fallback; no existing model ID changed.
- [ ] `senior` is `opencode-go/deepseek-v4-pro` and is escalation-only.
- [ ] `workhorse` and `strong tier` are gone from the normative process pages; historical records are untouched.
- [ ] The senior budget is one consultation per unresolved gate question.

### F. Record home and binding

- [ ] This record is at `docs/planning/reviews/policy-update-2026-11-27-workflow-disciplines.md`.
- [ ] `docs/planning/reviews/README.md` §9.1 defines the policy-update review class and its write scope.
- [ ] `.opencode/agents/review.md` allows `docs/planning/reviews/policy-*.md` and the body names the policy
      record.
- [ ] The review agent can write and commit its record under the binding when spawned from the frozen worktree;
      the fallback if it cannot load the definition is stop-and-report, not transcription.
- [ ] The one-file record commit and `[skip ci]` rules still apply.

### G. Checker, tests, and gates

- [ ] `check_process_consistency.py` requires the new files and integration text, and checks the retired tier
      vocabulary without weakening an existing check.
- [ ] `test_opencode_binding.py` covers senior, the policy path, and the three new commands.
- [ ] `test_process_consistency.py` has negative coverage for the new required paths and the Axis column.
- [ ] `./tools/verify.sh docs --require` passes at the freeze.
- [ ] `./tools/verify.sh policy --require` passes at the freeze.
- [ ] `git diff --check` passes.

### H. Verification pass

- [ ] The post-implementation verification is one bounded fresh pass at the implemented commit.
- [ ] It records in this same record under `## Post-implementation verification`, not as a second design round.
- [ ] It checks the mapping, normative homes, historical-record boundary, checker/test coverage, both gates,
      and the no-`src/`/`tests/`/`contracts/` boundary.
- [ ] A Blocker/Major in verification triggers a fix and a new verification pass; a design change reopens the
      design review instead.

### I. Full-design checklist

- [ ] The design class is Full and justified by the gate/role/binding change.
- [ ] Scope, non-goals, and outcomes are explicit and reviewable.
- [ ] Every adopted/adapted mapping has a destination and a rationale.
- [ ] The normative-home map has one home per rule; summaries link.
- [ ] The file manifest distinguishes added, changed, and deliberately untouched paths.
- [ ] No new dependency, format, API, contract, or runtime change is introduced.
- [ ] Documentation, navigation, status, and drift-check updates are planned.
- [ ] Historical records, tags, and `XC-04` are explicitly protected.
- [ ] Risks have mitigations and owners.
- [ ] Open questions are reported to the lead, not guessed.
- [ ] The change can land before Step 15 and be merged into `docs/step-15-design` without touching Step 15's
      design decisions.

## Evidence

Review agent: record the exact files read, commands run, and results here.

## Findings

| ID | Axis | Severity | Origin | Finding | Evidence | Recommendation |
| --- | --- | --- | --- | --- | --- | --- |
| — | — | — | — | — | — | — |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. Origin: **design**, **implementation**,
**evidence**, or **documentation**. Axis: **Standards** or **Spec**.

## Record state

**State:** Ready for review

`Re-review required` is a record state, not a verdict.

## Reviewer independence

> Review agent: state here that you are a fresh context. You did not author or implement this work and have no
> shared conversation with the design agent, dev, or lead.

## Verdict

- [ ] Ready for implementation
- [ ] Ready with notes
- [ ] Changes required
- [ ] Waived

Follow-up owner:
Follow-up change:

## Approval summary

The request owner completes this before a merge, tag, or Blocker/Major waiver request. It must cover the full
history, not only the latest round.

- [ ] Requested decision and frozen commit recorded
- [ ] Every review/verification round recorded (round, date, commit, agent, verdict)
- [ ] Every finding recorded (ID, axis, severity, issue, evidence, resolution, fix commit, status)
- [ ] Accepted notes and waivers recorded with rationale
- [ ] Residual risks, deferred items, and follow-up owners recorded
- [ ] Explicit no-Blocker/no-Major statement, or the exact exceptions being waived
- [ ] Final gate and documentation-build results recorded

Round history:

| Round | Date | Commit | Review agent | Verdict |
| --- | --- | --- | --- | --- |
| — | — | — | — | — |

Findings and resolutions:

| Finding | Axis | Severity | Issue | Resolution | Commit | Status |
| --- | --- | --- | --- | --- | --- | --- |
| — | — | — | — | — | — | — |

## Human approval

- [ ] Accepted: Ready for implementation
- [ ] Accepted: Ready with notes
- [ ] Changes required
- [ ] Waived

Approved by:
Date:

## Resolution status

The dev fills this section after a `Changes required` verdict. Do not alter the review agent-authored findings,
evidence, or verdict.

| Finding | Status | Owner | Commit |
| --- | --- | --- | --- |
| — | — | — | — |

## Review rounds

Append each new round below. Never overwrite an earlier round.

### Review round 1

#### Independence and frozen state

#### Commands and results

#### Findings

#### Verdict

## Post-implementation verification

Not run. The dev implements the frozen design, then a fresh verification agent records here. This section is not
a second design round and does not change the design verdict.

**Verification state:** Not run

### Verification evidence

### Verification findings

| ID | Axis | Severity | Origin | Finding | Evidence | Recommendation |
| --- | --- | --- | --- | --- | --- | --- |
| — | — | — | — | — | — | — |

### Verification verdict

- [ ] Verified
- [ ] Changes required
