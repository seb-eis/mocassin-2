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
| Design freeze commit | `9fc8c7b0327beff529f1a84847e110bf02bedef3` |
| Review request commit | This commit (documentation-only; freeze SHA, design-set file list, and supporting-page clarifications) |
| Request owner | Design agent, with the dev for implementation |
| Lead | — |
| Review initiated by | Lead, on request of the design agent |
| Review agent | DeepSeek V4.1 Flash 1017 (fresh design-review context; `opencode-go/deepseek-v4.1-flash`) |
| Review date | 2026-11-27 |
| Branch | `docs/adopt-workflow-disciplines` |
| Worktree | `/home/node/worktrees/adopt-workflow-disciplines` |
| Upstream reference | `seb-eis/opencode-workflow` at `b3230863c7a526622079ef595236cdc696f5c23a` (2026-11-27) |
| Authority | Owner decisions of 2026-11-27 (section “Owner decisions”) |

This is a **pre-implementation design review**. The design decisions below are the specification the dev
implements after the verdict; a separate, bounded post-implementation verification pass then checks the
implemented commit.

The design set contains the record and the minimal normative edits needed to make this review writable and to
correct the round-1 findings: the policy-update record class and the `policy-*.md` binding
(`docs/planning/reviews/README.md` §9.1 and `.opencode/agents/review.md`), the policy-update class in
`docs/development/design-proportionality.md`, the senior consultation page
`docs/development/senior-consultation.md` and its navigation entry, the retired tier wording in the agent
prompts and `docs/development/orchestration.md`, and the lead's discovery facilitation line in
`docs/development/orchestration.md`. The remaining normative pages are not rewritten here; the dev implements
them after the verdict.

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
binding change. The general policy-update class trigger is in
[`docs/development/design-proportionality.md`](../../development/design-proportionality.md) §1; this change meets
the Full branch because it changes a gate, a role, the agent binding, and a normative process rule.

Review depth: the round-2 design-gate checklist below. Stop when no Blocker or Major remains. Minor and Note
findings are recorded and accepted or deferred; they do not justify another round unless they are design-origin
and material. A third standard round needs an owner decision and a recorded reason.

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

**Published-page link convention.** A published page under `docs/` must not use a relative link to a root file
(`GLOSSARY.md`, `AGENTS.md`) or to a `docs/planning/` file: `zensical build --strict` aborts because those targets
are outside the published site or excluded. Use the absolute repository URL
`https://github.com/seb-eis/mocassin-2/blob/main/<path>` instead; `check_docs_links.py` treats it as external.
Relative links are for published-to-published targets only. This is the convention the existing published pages
already use (`agent-workflow.md`, `model-economics.md`, `design-proportionality.md`).

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
  owner, and routes settled decisions to the design agent or dev. Recommending is not deciding: the design agent
  or owner records the decision, and the lead does not choose for the owner or change scope. The permission for
  this is in `orchestration.md` §2 (“Facilitate discovery”) and §3.
- `/handoff <step-slug>` reads the durable records, builds the handoff card from `agent-workflow.md` §2 plus the
  applicable discipline/skill, and routes the card to the dev. It does not write.
- `/retro <step-slug>` reads the durable evidence, presents candidates grouped by navigation, checks, standards,
  steering files, tool economy, information access, and prototypes/research; every candidate needs an owner and
  an action. It routes the record to the dev and any process change through `docs/<slug>`.
The commands use `step-*` vocabulary and never `/wp-*`.

**A6. Index and navigation.** Add the nine discipline pages **and** `senior-consultation.md` (ten new
`docs/development` pages in total) to `docs/development/index.md` and to the Development section of
`zensical.toml` (a nested “Task disciplines” group is acceptable). Add `research/`, `prototypes/`,
`retrospectives/`, and `reviews/senior/` to the planning README reading order. `docs/planning/` stays excluded.

### B. Senior escalation

**B1. Normative home.** Add `docs/development/senior-consultation.md` as the single home for:
- **Trigger** — only when (a) two completed standard rounds of the *same* design review, audit, cross-cutting
  review, **or policy-update review** each ended blocked (`Changes required` / `Fixes required`), (b) at least one
  Blocker or Major from the second round is still open, and (c) the remaining disagreement reduces to **one**
  answerable question. A first round, a round with only Minor/Note findings, or a wish for another opinion is not
  a trigger.
- **Request contract** — parent record, gate, round numbers and verdicts, frozen commit, open finding IDs and
  contradictory evidence, one question, exact bounded paths/diff/commands, out-of-scope boundary, and what
  counts as an answer.
- **Senior rules** — the senior is a **fresh context** receiving only the bounded assignment and named paths; it
  must not see the lead's history, the owner's preferred answer, another reviewer's reasoning, or the request
  owner's notes. Inspect only the assignment and named paths; if the assignment is incomplete, ambiguous,
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
- a body that states the senior is a fresh context with no access to the lead's history, the owner's preferred
  answer, or another reviewer's reasoning; restates the decline-before-inspecting rule, the one-question rule,
  the record-only commit, and the advisory effect; and points at `docs/development/senior-consultation.md`.

Add `senior` to the lead’s allowed subagents. The lead may request it only on the trigger and only with a
bounded assignment. The lead must not use it as a normal review round or pass a preferred answer.

**B3. Integration.**
- `AGENTS.md` §3.8: “four roles” becomes “four standing roles, one escalation role, and one owner”; add a
  Senior consultant row; update the binding paragraph.
- `AGENTS.md` §3.9 and §3.10: after the re-review/re-audit step, add one paragraph: after two completed full
  rounds still leave a Blocker or Major, the lead may request one senior consultation before a third standard
  round; the paragraph names the gate types as design review, audit, cross-cutting review, **and policy-update
  review**; the answer is advisory, the owner decides, and a fresh standard round at the new commit remains the
  gate. Do not make senior a verdict or a round.
- `AGENTS.md` §3.15: add a `Senior escalation` row pointing at `senior-consultation.md`.
- `orchestration.md`: add senior to the diagram as a dashed escalation, to “Route”, to “Assign the tier”, to the
  context/spawn list, to the boundaries, and to the harness-binding table; add a short “Senior consultation”
  subsection that links the normative page (no duplicated trigger text). In §9, update the prose, not only the
  table: the binding paragraph's role list becomes `(lead, design, dev-free, dev, review, senior)`; “the lead can
  spawn only `design`, `dev-free`, `dev`, and `review`” adds `senior`; “the review agent can edit only
  `step-*.md` and `xc-*.md`” adds `policy-*.md`; “those four allowed” becomes five (four standing plus the
  escalation); the table gains a Senior consultant row and the lead row names senior.
- `reviews/README.md`, `audits/README.md`, `cross-cutting/README.md`: add a “Senior escalation” subsection that
  links the normative page, names the record path, and repeats only the record-format facts the reviewer needs.
- `model-economics.md`: the senior tier and the one-consultation budget.
- `agent-workflow.md` §8 and §12: add the senior trigger and the senior anti-patterns.
- `agent-security.md`: add the senior consultant to the read-only contexts and the least-privilege checklist.
  It has the review agent's threat model but a narrower inspection scope.

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

**C2. Templates.** Add an `Axis` column (values `Standards` / `Spec`) to each findings table:
- `docs/planning/reviews/step-design-review-template.md` and `docs/planning/audits/step-audit-template.md` become
  `ID | Axis | Severity | Origin | Finding | Evidence | Recommendation` (they already have `Origin`);
- `docs/planning/reviews/cross-cutting/cross-cutting-review-template.md` becomes
  `ID | Axis | Severity | Area | Finding | Evidence | Recommendation`; it keeps `Area` and has no `Origin`.
Do not reorder the other columns. The sets stay aligned on `ID`, `Axis`, and `Severity`; `Origin` and `Area` are
family-specific and are not merged.

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

**E5. Retire the old tier words.** Remove `workhorse`, `strong tier`, `strong-tier`, and bare `strong` from the
normative process pages (`model-economics.md`, `orchestration.md`, `design-proportionality.md`,
`agent-workflow.md`) **and** from `.opencode/agents/*.md` (frontmatter and body). The agent prompts are live
instructions, not historical prose:
- `dev.md`'s “strong-tier pass” escalation becomes the re-brief-on-default rule, with senior only after two
  completed blocked review/audit rounds;
- `dev-free.md`'s “free workhorse tier” becomes “free variant of the default tier”;
- the `# Reference tier:` comments in `lead.md`, `design.md`, `review.md`, `dev.md`, and `dev-free.md` use
  `default` (and `default, free variant`), not `strong`/`workhorse`;
- `review.md` says independence is a fresh context, not a different model.
This design set already applies those agent-prompt edits. The dev applies the remaining process-page rewrite.
Historical records (`docs/planning/maintenance/2026-11-22-policy-adoption.md`, the `status.md` note about the
earlier strong-tier decision) are not rewritten. Add a new `status.md` note that records the adoption and the
default/senior names.

**E6. Binding tests.** `tools/tests/test_opencode_binding.py` keeps the existing model assertions and adds:
senior model and mode; lead may spawn senior; review may edit `docs/planning/reviews/policy-*.md`; the three new
commands use the lead; `senior` denies subagents and edits outside its record paths. It also asserts that the
agent prompts and the four process pages contain none of `workhorse`, `strong tier`, `strong-tier`, or bare
`strong` (word-boundary), so the live-instruction regression is caught.

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
- Add to `REQUIRED_INTEGRATION`: `AGENTS.md` contains `output contract`, `senior consultation`, `policy-*.md`,
  and `Senior consultant`; `docs/development/senior-consultation.md` contains `two completed standard rounds`
  and `policy-update review`; `docs/development/review-axes.md` contains `Standards` and `Spec`; the three
  findings templates contain `| Axis |`; `.opencode/agents/review.md` contains `policy-*.md`;
  `.opencode/agents/lead.md` contains `senior`; `docs/development/model-economics.md` contains `default` and
  `senior`; `docs/development/orchestration.md` contains `senior`, `policy-*.md`, and `Senior consultant`;
  `docs/development/agent-workflow.md` contains `senior consultation`;
  `docs/planning/reviews/README.md` contains `policy-update` and `senior-consultation.md`. Require
  `policy-*.md` and `Senior consultant` in the orchestration/AGENTS binding prose, not merely `senior` somewhere
  in the file (m-5).
- Add a targeted `check_tier_vocabulary` that fails if `model-economics.md`, `orchestration.md`,
  `design-proportionality.md`, `agent-workflow.md`, **or `.opencode/agents/*.md`** still contain `workhorse`,
  `strong tier`, `strong-tier`, or bare `strong` (word-boundary; case-insensitive). Historical `status.md` and
  maintenance records are not scanned. This prevents the old tier names from returning in live instructions
  without forcing a rewrite of history.
- Add `GLOSSARY.md` to `tools/check_docs_links.py`'s `ROOT_FILES` so its own links are scanned. Do not add a
  relative published-page link to a root or `docs/planning/` file; use the absolute URL in A2.
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
- `GLOSSARY.md` is a root file. Link it from `AGENTS.md` §2 with a relative root link (the root guides are in
  `check_docs_links.ROOT_FILES`), and from `docs/development/domain-language.md` with the absolute URL
  `https://github.com/seb-eis/mocassin-2/blob/main/GLOSSARY.md`. Do **not** use a relative link from a published
  page to the root file: `zensical build --strict` aborts. Add `GLOSSARY.md` to
  `tools/check_docs_links.py`'s `ROOT_FILES` so its own links are scanned; “discoverable” does not by itself
  mean “link-checked”.
- `docs/glossary/index.md` gets the boundary note; it is not renamed.
- `docs/planning/README.md` gains the new record homes and the senior record index in its reading order, but the
  content stays a link, not a copy.
- `tools/check_docs_links.py`, `python tools/check_doc_math.py`, and `zensical build --strict` are the link
  gates. Any new page that is not linked is a drift finding, not an orphan.

**H2. Supporting pages.** Keep these one-line changes; do not add a second copy of a rule:

- `docs/development/drift-checks.md`: add a policy-and-process item covering the task disciplines, review
  axes, senior escalation, output contract, and retired tier names. Run the drift check after this process
  change.
- `docs/development/agent-security.md`: add the senior consultant to the read-only contexts and the
  least-privilege checklist. It has the review agent's threat model but a narrower inspection scope.
- `docs/development/contributing.md`: add gate-reference rows for the task disciplines, review axes, senior
  escalation, and output contract. Link; do not restate.

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
| Design classes and the policy-update class trigger, stop rule, pre-audit gate, finding-origin metric | `docs/development/design-proportionality.md` §1 | review/audit READMEs (including `reviews/README.md` §9.1, which links the class), `AGENTS.md` §3.9 |
| Senior trigger **including policy-update reviews**, request contract, senior rules, effect | `docs/development/senior-consultation.md` §1–§4 | `AGENTS.md` §3.9/§3.10, review/audit/cross-cutting/policy-update READMEs, `orchestration.md` |
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
- `tools/check_docs_links.py` (`ROOT_FILES` gains `GLOSSARY.md`)
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

| Risk | Severity | Owner | Mitigation |
| --- | --- | --- | --- |
| The review agent cannot load the updated `review.md` in the frozen worktree, so it cannot write the policy record. | Major | Lead (human owner for a one-off permission) | Spawn from the frozen worktree; if the harness still loads the original root, stop and report `Blocked`; restart the service or obtain a one-off owner permission. Never transcribe. Round-1 evidence showed the frozen definition loaded for this worktree. |
| The new discipline pages duplicate existing MOCASSIN pages and drift. | Major | Design agent; dev implements | Each page is a discipline + pointer; the normative-home map and the checker’s required integration text enforce the links. |
| The senior escalation becomes a third standard round or a verdict. | Major | Lead; design agent for the rule text | Trigger is exactly two completed blocked standard rounds and one question; the record says advisory; the owner decides; fresh standard round remains the gate. |
| The old `strong`/`workhorse` vocabulary survives in a normative page or agent prompt. | Minor | Dev (checker); design agent (prose) | Targeted `check_tier_vocabulary` on the four process pages and `.opencode/agents/*.md`, including `strong-tier` and bare `strong`; historical records are excluded. |
| The Axis column is added but findings are still merged or re-ranked. | Minor | Review agent; design agent for the prompt | The review prompt and the axis page require one label per finding and one verdict; the templates show the column. |
| New development pages become orphans or break links. | Minor | Dev | Add to `index.md` and `zensical.toml`; `check_docs_links.py` and `zensical build --strict` are gates; published-to-root/planning links use absolute URLs. |
| Historical records are accidentally rewritten. | Major | Dev; verification agent | File manifest excludes them; the verification pass checks `git diff --name-only origin/main...HEAD`. |
| The senior model is unavailable or more expensive than expected. | Minor | Lead; human owner | Owner confirmed it resolves; one consultation per question; the lead reports the cost impact in the approval report. |
| `GLOSSARY.md` and `docs/glossary/` overlap. | Minor | Design agent | Explicit boundary note; no term defined in both; the discipline page points to the right one. |

## Open questions for the lead

1. **Agent-definition load path.** Does the OpenCode harness load `.opencode/agents/*.md` from the session’s
   worktree, or only from the original `/workspace` project root? The design requires the former so the review
   agent sees the `policy-*.md` allow rule. If the latter, the review of this very change cannot be written under
   the binding until the service is restarted with the frozen worktree or the owner grants a one-off permission.
   Round-1 evidence: the reviewer's loaded definition contained the allow rule and its record edit succeeded in
   this worktree, so the fallback was not needed for that round. That is evidence for the round, not a general
   harness guarantee; the stop-and-report fallback in section F stays.
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
- `git diff --name-only origin/main...9fc8c7b` — exactly
  `docs/planning/reviews/policy-update-2026-11-27-workflow-disciplines.md`,
  `docs/planning/reviews/README.md`, and `.opencode/agents/review.md`. No other path is changed at the design
  freeze. The request commit adds this record's freeze pointer, this evidence line, and the supporting-page
  clarifications; it changes no rule.

### Round 2 request owner evidence

Commands and results at the round-2 freeze `fcbd20ed8ef2ad201746fb46b064c9db2e80d056` (the design-set
commit), recorded by the design agent:

- `git rev-parse origin/main` — `245e34aff7255476d73539d4cc43f068627f0cba` (unchanged).
- `git diff --check` — exit 0, no output.
- `python tools/check_docs_links.py` — `Documentation links OK (218 files scanned)`.
- `python tools/check_process_consistency.py` — `Process consistency: OK`.
- `./tools/verify.sh docs --require` — exit 0. Strict Zensical, DocFX (warnings as errors), and rustdoc;
  `Documentation built in <worktree>/site`; `Closeout gate completed with required checks enabled.`
- `./tools/verify.sh policy --require` — exit 0. Release build; NuGet licence/vulnerability audit; ApiCompat
  (`Public API is compatible with the checked-in baseline.`); contract/symmetry drift; `Process consistency: OK`;
  `Ran 62 tests … OK`; `SPDX headers present in 1210 source files`; `No secret patterns found in 1757 tracked
  text files`; `advisories ok, bans ok, licenses ok, sources ok`; `Closeout gate completed with required checks
  enabled.`
- `git diff --name-only origin/main` (plus the untracked new page) — `.opencode/agents/design.md`,
  `.opencode/agents/dev-free.md`, `.opencode/agents/dev.md`, `.opencode/agents/lead.md`,
  `.opencode/agents/review.md`, `docs/development/design-proportionality.md`,
  `docs/development/index.md`, `docs/development/orchestration.md`,
  `docs/development/senior-consultation.md`, `docs/planning/reviews/README.md`,
  `docs/planning/reviews/policy-update-2026-11-27-workflow-disciplines.md`, and `zensical.toml`. No `src/`,
  `tests/`, `contracts/`, workflow, generated artifact, historical record, step page, audit, closeout, or `XC-04`
  path.

**n-2 context.** The 1755 count was taken at `9fc8c7b` before this record was tracked. At the round-1 freeze
(`7b3148d`) the scan reported **1756** (this record); at the round-2 freeze (`fcbd20e`) it reports **1757**
(this record plus the new `docs/development/senior-consultation.md`). The gate command's commit is recorded here so the
count is reproducible.

> The review agent may rerun the commands and replace this evidence with its own observations. The request
> owner evidence is part of the request and is not the review agent’s verdict.

## Review checklist

Mark what was reviewed; add short inline notes only. Do not change item text.

> **Round-2 scope.** The checklist below is round-1 history; its wording uses “exists” for the target state.
> Round 2 uses the design-gate checklist in `## Re-review request (round 2)`, which asks whether the design
> specifies each item and names the destination. Existence and implemented-state checks belong to
> `## Post-implementation verification`. This clarification does not alter any round-1 item text.

### A. Task disciplines

- [ ] Eight skill files exist, name the discipline, and point at the matching `docs/development` page. — Design A1 and the manifest specify eight; existence is verified in the post-implementation pass (nothing to inspect at the freeze).
- [ ] Nine discipline pages exist and link to the existing MOCASSIN homes instead of duplicating them. — Design A2 specifies the nine pages and the link list; see m-1 for the published-link form.
- [ ] `GLOSSARY.md` is the agent/human ubiquitous language; `docs/glossary/` keeps the published scientific
      glossary; the boundary is explicit and no term is defined twice. — Design A3 defines the boundary and the no-duplication rule; the file and the glossary note are implementation.
- [ ] `docs/planning/research/`, `prototypes/`, and `retrospectives/` each have a README and template with the
      fields in the design. — Design A4 specifies the fields; the record homes are implementation.
- [ ] `/grill`, `/handoff`, and `/retro` use `agent: lead`, `step-*` vocabulary, and route records to the
      design agent or dev without writing them. — Design A5 specifies the commands; see m-7 for the lead-boundary wording.
- [ ] The new pages are in `docs/development/index.md` and the Zensical navigation; `docs/planning/` stays
      excluded. — A6 and H1 specify index/nav for all ten new development pages; see m-8 on the count.
- [ ] `drift-checks.md`, `agent-security.md`, and `contributing.md` get the one-line supporting updates in H2,
      not a second rule copy. — H2 (request clarification) specifies the three one-line updates; implementation only.

### B. Senior escalation

- [ ] `senior-consultation.md` is the single home for the trigger, request contract, senior rules, effect, and
      record naming. — Design B1 defines the home; see M-2 for the trigger's policy-review omission.
- [ ] `.opencode/agents/senior.md` uses `opencode-go/deepseek-v4-pro`, edits only its record paths, and cannot
      spawn. — Design B2 specifies the binding; implementation only.
- [ ] The lead can spawn `senior` only on the trigger; senior is not a standing role and not a verdict. — Design B1/B2/B3 specify the trigger and advisory effect; implementation only.
- [ ] `AGENTS.md` §3.8/§3.9/§3.10/§3.15, `orchestration.md`, and the review/audit/cross-cutting READMEs
      integrate the escalation with one link and no duplicated trigger text. — Design B3 specifies the integration; see m-5 for the stale `orchestration.md` §9 prose.
- [x] The senior record names are `step-NN-M.md`, `xc-NN-M.md`, and `policy-<date>-<slug>-M.md`. — B1 names match `reviews/README.md` §9.1 and the B2 allow rules; the trigger still omits policy reviews (M-2).

### C. Review axes

- [ ] `review-axes.md` defines Standards and Spec, when each applies, the pinned-commit rule, and one verdict. — Design C1 specifies the content; implementation only.
- [ ] The design-review, audit, and cross-cutting findings tables have an `Axis` column. — Design C2; see m-4 on the cross-cutting template's `Area` column.
- [ ] The review prompt and spawn card require one axis label per finding without merging or re-ranking. — Design C3; implementation only.
- [x] A design review does not spend a round on code layout, prose, or bookkeeping. — C1 repeats the existing `design-proportionality.md` §2 rule and does not weaken it.

### D. Output contract

- [ ] `AGENTS.md` §17 exists and carries the eight fields and the evidence/approval rules. — Design D1 fixes the eight fields and the rules; implementation only.
- [ ] Lead, design, dev, dev-free, review, and senior prompts finish with the applicable §17 fields. — Design D2; implementation only.
- [x] The everyday output contract has one home; `agent-workflow.md` §5 links to it and keeps only the
      cumulative approval report. — Design D3 keeps `AGENTS.md` §3.12 and `agent-workflow.md` §5 as the approval-report home and adds no second output template.

### E. Model economics

- [ ] `model-economics.md` uses `default`/`senior`, puts lead/design/dev/review on the default binding, and
      makes review independence a fresh context, not a different model. — Design E1/E2 specify this; implementation only.
- [x] `dev-free` then `dev` remains the only free-first fallback; no existing model ID changed. — E1/E2 keep the two dev bindings and the current model IDs; I checked the four current agent `model:` lines and the senior model is the only addition.
- [x] `senior` is `opencode-go/deepseek-v4-pro` and is escalation-only. — E1/E2 state the model and the escalation-only role.
- [ ] `workhorse` and `strong tier` are gone from the normative process pages; historical records are untouched. — Design E5; see M-3 for the agent-prompt and checker gaps.
- [x] The senior budget is one consultation per unresolved gate question. — E4 states it; the repeat rule requires a new question and an explanation to the owner.

### F. Record home and binding

- [x] This record is at `docs/planning/reviews/policy-update-2026-11-27-workflow-disciplines.md`. — Verified at the frozen commit.
- [x] `docs/planning/reviews/README.md` §9.1 defines the policy-update review class and its write scope. — Verified (`reviews/README.md:217-239`); see M-1 on the design-class home.
- [x] `.opencode/agents/review.md` allows `docs/planning/reviews/policy-*.md` and the body names the policy
      record. — Verified in the frozen diff and the loaded definition.
- [x] The review agent can write and commit its record under the binding when spawned from the frozen worktree;
      the fallback if it cannot load the definition is stop-and-report, not transcription. — The loaded definition contains the allow rule and this record's edit succeeded in this worktree; the fallback was not needed (Evidence, Open questions 1).
- [x] The one-file record commit and `[skip ci]` rules still apply. — Stated in `reviews/README.md` §9.1 and the record header; this record commit touches only this file and uses `[skip ci]`.

### G. Checker, tests, and gates

- [ ] `check_process_consistency.py` requires the new files and integration text, and checks the retired tier
      vocabulary without weakening an existing check. — Planned in G1; not implemented at the design freeze. See M-3 (agent prompts and `strong-tier`/`strong` gaps) and m-5 (the `senior` integration assertion).
- [ ] `test_opencode_binding.py` covers senior, the policy path, and the three new commands. — Planned in E6/G3; implementation only.
- [ ] `test_process_consistency.py` has negative coverage for the new required paths and the Axis column. — Planned in G2; implementation only.
- [x] `./tools/verify.sh docs --require` passes at the freeze. — Exit 0; see Evidence.
- [x] `./tools/verify.sh policy --require` passes at the freeze. — Exit 0; 62 tests OK; see Evidence.
- [x] `git diff --check` passes. — Exit 0, no output.

### H. Verification pass

- [x] The post-implementation verification is one bounded fresh pass at the implemented commit. — Design I defines one pass, before the merge, by a fresh context.
- [x] It records in this same record under `## Post-implementation verification`, not as a second design round. — Design I and `reviews/README.md` §9.1.
- [x] It checks the mapping, normative homes, historical-record boundary, checker/test coverage, both gates,
      and the no-`src/`/`tests/`/`contracts/` boundary. — Design I items 1–9; item 7 uses the `origin/main...HEAD` allowlist.
- [x] A Blocker/Major in verification triggers a fix and a new verification pass; a design change reopens the
      design review instead. — Design I; the repair loop is bounded and does not reopen the design verdict.

### I. Full-design checklist

- [x] The design class is Full and justified by the gate/role/binding change. — The record justifies it at lines 92–95; see M-1 for where the class rule must live.
- [x] Scope, non-goals, and outcomes are explicit and reviewable. — Scope, non-goals, mapping, manifest, and verification are explicit; owner decisions 1–6 bound the change.
- [x] Every adopted/adapted mapping has a destination and a rationale. — I checked every adopted/adapted row against the destination and the upstream tree; no row is missing a destination.
- [ ] The normative-home map has one home per rule; summaries link. — The map gives one home per row, but M-1 (design class) and M-2 (senior trigger) show design text that contradicts or omits the stated home.
- [x] The file manifest distinguishes added, changed, and deliberately untouched paths. — Added/Changed/Not-touched sections; no code, workflow, or historical record in Changed.
- [x] No new dependency, format, API, contract, or runtime change is introduced. — G4 uses the standard library; the design states documentation/process only.
- [x] Documentation, navigation, status, and drift-check updates are planned. — A6, H1, H2, E5 status note, and the drift-check item.
- [x] Historical records, tags, and `XC-04` are explicitly protected. — Non-goals, manifest "Deliberately not touched", and verification item 7.
- [ ] Risks have mitigations and owners. — Mitigations are present; no owner column (m-3).
- [x] Open questions are reported to the lead, not guessed. — Two questions are recorded; OQ1 is resolved for this review (Evidence) and OQ2 is procedural.
- [x] The change can land before Step 15 and be merged into `docs/step-15-design` without touching Step 15's
      design decisions. — Owner decision 5; no `docs/planning/steps/` or Step 15 content is in the manifest.

## Evidence

Review agent: DeepSeek V4.1 Flash 1017 (`opencode-go/deepseek-v4.1-flash`), fresh design-review context.
Date: 2026-11-27. Frozen commit: `7b3148d44496b6feab0bf33c437085e122bd5481`. Base `origin/main`:
`245e34aff7255476d73539d4cc43f068627f0cba`. Branch `docs/adopt-workflow-disciplines`; worktree
`/home/node/worktrees/adopt-workflow-disciplines`.

### Frozen state and diff

| Command | Result |
| --- | --- |
| `git rev-parse HEAD` | `7b3148d44496b6feab0bf33c437085e122bd5481` |
| `git status --short` | empty (clean worktree) |
| `git diff --name-only origin/main...HEAD` | exactly `.opencode/agents/review.md`, `docs/planning/reviews/README.md`, `docs/planning/reviews/policy-update-2026-11-27-workflow-disciplines.md` |
| `git diff --stat origin/main...HEAD` | 3 files, +869/−5 — documentation and agent configuration only; no `src/`, `tests/`, `contracts/`, workflow, or generated artifact |
| `git diff --check` | exit 0, no output |
| `git show --stat 9fc8c7b` | design-set commit: 3 files, +853/−5 |
| `git show --stat 7b3148d` | request/clarification commit: this record only, +17/−2 |
| `git diff --name-only origin/main...9fc8c7b` | the same three design-set paths, matching the request-owner evidence |

### Required gates at the frozen commit

| Command | Result |
| --- | --- |
| `./tools/verify.sh docs --require` | **exit 0**. `git diff --check` clean; `python tools/check_doc_math.py`; `python tools/check_docs_links.py` → `Documentation links OK (217 files scanned).`; Release build for DocFX input; `zensical build --strict`; DocFX with warnings as errors (0 warnings, 0 errors); rustdoc for all 7 crates; `Documentation built in …/site`; `Closeout gate completed with required checks enabled.` |
| `./tools/verify.sh policy --require` | **exit 0**. Release build; NuGet licence/vulnerability audit; ApiCompat `Public API is compatible with the checked-in baseline.`; `Contract artifacts are current (26 files)`; 5 + 4 + 2 projection fixtures; 274 space-group settings; `Process consistency: OK`; `Ran 62 tests … OK`; `SPDX headers present in 1210 source files`; `No secret patterns found in 1756 tracked text files`; `advisories ok, bans ok, licenses ok, sources ok`; `Closeout gate completed with required checks enabled.` |
| `python tools/check_docs_links.py` | exit 0, `Documentation links OK (217 files scanned).` |
| `python tools/check_process_consistency.py` | exit 0, `Process consistency: OK` |

The request-owner evidence's counts (26 contract files, 274 space groups, 62 tests, 1210 SPDX files) reproduce.
The secret scan reports **1756** tracked text files, not the 1755 in the request-owner evidence; consistent with
this record being untracked when the owner ran the gate, and recorded as n-2.

### Scratch checks (read-only, outside the repository)

I checked the published-build behaviour of the link plan in A2/H1, because the design calls `zensical build
--strict` a link gate:

- `/tmp/gloss-test`: a minimal Zensical project with `docs/development/index.md` linking
  `[GLOSSARY.md](../../GLOSSARY.md)` and a root `GLOSSARY.md` → `zensical build --strict` printed
  `Warning: page does not exist … development/index.md:3:30` (and the same for `docs/index.md`), then
  `RuntimeError: Aborted because --strict flag is set`, **exit 1**.
- `/tmp/gloss-test2`: a minimal project with `exclude_docs = "planning/"` and a published page linking
  `[quality](../../planning/qs.md)` → same strict abort, **exit 1**.

No repository file was written by either check. The repository already links root and planning files from
published pages with absolute GitHub URLs (`docs/development/agent-workflow.md:27-28,207,267,336-344`;
`docs/development/model-economics.md:8,96-132`; `docs/development/design-proportionality.md:4-5,95-101`), which
both `check_docs_links.py` and Zensical treat as external.

### Files read

- `AGENTS.md` §2, §3.1–§3.15, §5–§17; `docs/planning/reviews/README.md` (especially §2, §5–§9.1);
  this record in full.
- `docs/development/`: `model-economics.md`, `orchestration.md`, `design-proportionality.md`,
  `agent-workflow.md`, `agent-security.md`, `drift-checks.md`, `contributing.md`, `index.md`.
- `docs/planning/`: `README.md` (reading order), `status.md` (the 2026-11-22 strong-tier note),
  `reviews/step-design-review-template.md`, `audits/step-audit-template.md`, `audits/README.md`,
  `reviews/cross-cutting/README.md`, `reviews/cross-cutting/cross-cutting-review-template.md`.
- `docs/glossary/index.md`; `zensical.toml`; `tools/verify.sh`; `tools/check_docs_links.py`;
  `tools/check_process_consistency.py`; `tools/check-no-secrets.py`; `tools/tests/test_opencode_binding.py`;
  `tools/docs/build-docs.sh`.
- `.opencode/agents/{lead,design,dev,dev-free,review}.md`; `.opencode/commands/{step-start,step-closeout}.md`;
  `opencode.json`.
- Upstream reference `/tmp/opencode-workflow` at `b3230863c7a526622079ef595236cdc696f5c23a`: `AGENTS.md` §8,
  `docs/process/{reviews-and-audits,model-economics,review-axes}.md`, `.opencode/agents/{senior,review}.md`,
  `.opencode/commands/{grill,handoff,retro}.md`, and the `docs/templates/` and `docs/process/` listings. The
  adopted/adapted/rejected claims in the mapping table checked out against this tree (eight skills, three
  commands, senior agent and spawn card, §8 output contract, §2.4 senior rules, the default/senior tier
  policy).

### Open questions

1. **Agent-definition load path — resolved for this review.** The review-agent definition loaded in this
   worktree contains the `docs/planning/reviews/policy-*.md` allow rule (the record edit above succeeded), so
   the harness used the frozen `.opencode/agents/review.md`; the `Blocked` fallback was not needed. This is
   evidence for this review only, not a general harness guarantee.
2. **Policy record glob breadth.** The broad `policy-*.md` allow rule is present; the procedural
   assigned-record rule is the control. No finding.

## Findings

| ID | Axis | Severity | Origin | Finding | Evidence | Recommendation |
| --- | --- | --- | --- | --- | --- | --- |
| M-1 | Spec | **Major** | **design** | **The new policy-update design class has no home in `design-proportionality.md`, and its Full trigger is defined twice.** The record's normative-home map says design classes live in `design-proportionality.md`, but that page still says "The design class is a property of a step or an amendment" and classifies only steps; `reviews/README.md` §9.1 instead defines "A policy update is `Full` when …", and the record's own "Design class and review depth" adds a further trigger ("a gate/role/binding change is"). A future policy reviewer following the stated home finds no class rule, while a reader of §9.1 finds a second trigger list that is not reconciled with the first. | Record lines 92–95, 483–501 (the "Design classes …" and "Policy-update review records" rows); `docs/planning/reviews/README.md:230-232`; `docs/development/design-proportionality.md:7-9,16-23`; `AGENTS.md` §3.15 "Design classes …" row | Move the policy-update class trigger into `design-proportionality.md` §1, or add one sentence there that policy-update reviews use the same classes and that §9.1 defines the record-class trigger; then update the one-home map row. `design-proportionality.md` is already in the Changed manifest (E5), so this is a one-line correction. |
| M-2 | Spec | **Major** | **design** | **The senior trigger omits policy-update reviews that the same design set enables.** B1's trigger is limited to "two completed standard rounds of the *same* design review, audit, or cross-cutting review". The same record defines the senior record name `policy-<date>-<slug>-M.md`, B2 allows `docs/planning/reviews/senior/policy-*.md`, and `reviews/README.md` §9.1 promises senior after two blocked policy-update rounds. A dev writing `senior-consultation.md` from B1 would forbid exactly the escalation §9.1 and the record naming use, and the planned required-text check (`two completed standard rounds`) would still pass. | Record lines 233–235, 247–248, 253–254; `docs/planning/reviews/README.md:234-236`; upstream `/tmp/opencode-workflow/docs/process/reviews-and-audits.md:104-126` (the adapted trigger also enumerates only three gate types) | Add "policy-update review" to the trigger enumeration in B1 (and to the `AGENTS.md` §3.9/§3.10 paragraph if it names gate types), and add it to the required-text check. |
| M-3 | Standards | **Major** | **design** | **The tier retirement leaves the dev prompt's escalation rule live and the planned guard cannot catch it.** E5 scopes agent files to "the agent file comments", so `.opencode/agents/dev.md:28-30` still tells the dev to "Escalate to the lead for a strong-tier pass" — the exact rule E3 removes from `model-economics.md` and replaces with re-brief-on-default / senior-only-on-review-deadlock. `dev-free.md:2` still describes the "free workhorse tier". The planned `check_tier_vocabulary` scans only four process pages for `workhorse`/`strong tier`, so it misses agent prompts, the hyphenated `strong-tier` (`model-economics.md:121`) and bare `strong` (`orchestration.md:36`). The role prompt is a live instruction, not historical prose. | `.opencode/agents/dev.md:28-30`; `.opencode/agents/dev-free.md:2-4`; record lines 355–359, 406–409, 585; `docs/development/model-economics.md:121`; `docs/development/orchestration.md:36` | Extend E5 to `.opencode/agents/*.md` (frontmatter and body), rewrite the dev escalation to the new re-brief/senior rule, and extend the checker or a binding test to the agent files and the `strong-tier`/`strong` forms so a miss is caught. |
| m-1 | Standards | **Minor** | **design** | **The planned links from published pages to root and planning files are not implementable as written under `zensical build --strict`.** A2 tells the nine published discipline pages to link `docs/planning/03-quality-strategy.md`, `05-open-decisions.md`, and `docs/planning/adr/`; H1 tells `docs/development/index.md` and `domain-language.md` to link root `GLOSSARY.md` "so it is discoverable and link-checked". A relative link from a page under `docs/` to a root or `planning/` file fails the strict build, and the repository's existing published pages use absolute GitHub URLs for those targets, which `check_docs_links.py` skips as external. Either the dev deviates from the wording to pass the gate, or the gate fails. | Record A2 lines 188–197 and H1 lines 422–433; `docs/development/agent-workflow.md:27-28,207`; scratch builds above: `zensical build --strict` exit 1 on `[GLOSSARY.md](../../GLOSSARY.md)` and on `[quality](../../planning/qs.md)` | State the convention explicitly (published pages link root and `docs/planning/` files with absolute `https://github.com/seb-eis/mocassin-2/blob/main/...` URLs) and correct H1's "link-checked" claim, or add those paths to `check_docs_links.ROOT_FILES` and verify the published-page restriction. |
| m-2 | Spec | **Minor** | **documentation** | **The design-review checklist is written as post-implementation existence checks, so most items cannot be answered at the design freeze and duplicate the verification pass.** Items such as "Eight skill files exist", "`AGENTS.md` §17 exists", "`model-economics.md` uses `default`/`senior`", and "The design-review, audit, and cross-cutting findings tables have an `Axis` column" assert a state the record says does not exist until after the verdict. The reviewer can only add deferral notes, so the checklist does not record what the design review actually verified. | Record lines 636–727; the record's own statement at lines 30–32 that "the normative pages are not rewritten in the design set"; the separate verification section at lines 445–481 | Phrase the design-round checklist as "the design specifies … / the destination and content are defined" and keep the existence checks in the verification section, or mark those items "verification only" in the request. |
| m-3 | Spec | **Minor** | **design** | **The risk table has no owner, although the design review's five questions require risks "with an owner" and checklist I asks for owners.** The table has Risk/Severity/Mitigation only, and the three Major risks have no named role to carry the mitigation. | Record lines 578–590 and checklist line 724; `docs/development/design-proportionality.md` §2 question 5; `docs/planning/reviews/README.md` §5 | Add an Owner column (design agent, dev, lead, or human owner as appropriate) and assign the Major risks. |
| m-4 | Standards | **Minor** | **documentation** | **C2 says "Keep the existing `Origin` column" for all three findings tables, but the cross-cutting template has `Area`, not `Origin`.** The design-review and audit templates do have Origin; the cross-cutting one does not. The instruction remains implementable (add Axis, keep Area), but the design statement is inaccurate and does not say whether the other columns are aligned. | Record lines 297–300; `docs/planning/reviews/cross-cutting/cross-cutting-review-template.md:80-82`; `docs/planning/reviews/step-design-review-template.md:86-88`; `docs/planning/audits/step-audit-template.md:90-92` | Say "add Axis to each findings table; the cross-cutting template keeps `Area`" and state whether the column sets are aligned. |
| m-5 | Standards | **Minor** | **design** | **The orchestration harness-binding update is under-specified for the stale prose.** B3 lists the diagram, Route, tier assignment, context/spawn list, boundaries, and the binding *table*, but §9 also has prose that enumerates "(`lead`, `design`, `dev-free`, `dev`, `review`)", "the lead can spawn only `design`, `dev-free`, `dev`, and `review`", and "the review agent can edit only `step-*.md` and `xc-*.md` records … those four allowed". The checker only requires the word `senior` somewhere in the file, so those sentences can stay stale while the gate passes. | Record B3 lines 262–278 and G1 lines 399–409; `docs/development/orchestration.md:165-175` | Name the §9 sentences explicitly in B3 and add a checker assertion for the binding table (for example `policy-*.md` and `senior` in the table, not just anywhere in the file). |
| m-6 | Spec | **Minor** | **design** | **The senior consultation's fresh-context independence is not stated.** B1 lists the trigger, request, senior rules, effect, and naming, but not that the senior is a fresh context that must not see the lead's history, the owner's preferred answer, or the reviewer's reasoning. B2's agent body restates decline / one-question / record-only / advisory but not freshness, and the review, audit, and cross-cutting subsections repeat only "record-format facts". The design does say the lead must not pass a preferred answer, but the independence rule is weaker than the upstream §2.1 it adapts. | Record B1 lines 232–257, B2 lines 251–260, B3 lines 273–274; upstream `/tmp/opencode-workflow/docs/process/reviews-and-audits.md:50-51` | Add one sentence to `senior-consultation.md` and the senior agent body that the senior is a fresh context receiving only the bounded assignment, and add the senior consultant to the review prompt's independence sentence. |
| m-7 | Spec | **Minor** | **design** | **`/grill` gives the lead a design-adjacent duty the lead boundary does not currently permit.** A5 says the lead "recommends an answer" for each frontier question, while `orchestration.md` §3 says the lead must not "make design decisions, choose between plausible designs, or change scope" and `AGENTS.md` §3.8 says the lead does not design. The upstream command behaves the same, but MOCASSIN's lead boundary is stricter and is not updated. | Record A5 lines 216–224; `docs/development/orchestration.md` §3; `.opencode/agents/lead.md:20-27`; upstream `/tmp/opencode-workflow/.opencode/commands/grill.md` | Add one line to `orchestration.md` §2/§3 permitting the lead to facilitate discovery and recommend options while the design agent or owner records the decision, or route `/grill` to the design agent. |
| m-8 | Spec | **Minor** | **design** | **A6 says nine pages are added to `docs/development/index.md` and the Zensical navigation, but the design adds ten `docs/development` pages** — the nine disciplines plus `senior-consultation.md`. H1's "All new `docs/development` pages" covers the tenth, so a careful dev can comply, but the two statements disagree and a literal reading of A6 leaves the senior page unnavigated. | Record A6 lines 226–228, file manifest lines 511–513, H1 line 424; `docs/development/index.md` and `zensical.toml` development nav | Say "ten", or make A6 explicitly cover the senior-consultation page. |
| n-1 | Spec | **Note** | **documentation** | **The design set changes two process files despite "the normative pages are not rewritten in the design set".** `docs/planning/reviews/README.md` §9.1 and `.opencode/agents/review.md` are changed at the freeze to make this review writable. Section F explains why, but the Purpose sentence is overbroad; state the exception so a reader does not read the design set as documentation-only. | Record lines 30–32 and 377–390; `git diff --name-only origin/main...HEAD` | Amend the Purpose sentence to name the policy-record class and binding as the only pre-verdict process edits, subject to this verdict. |
| n-2 | Spec | **Note** | **evidence** | **The request-owner evidence reports 1755 tracked text files in the secret scan; the scan at the frozen commit reports 1756.** This is consistent with the record being untracked when the owner ran the gate, but the number is not reproducible from the commit; no action beyond recording the command context. | Record line 621; `./tools/verify.sh policy --require` output | None; optionally record the gate command's commit when quoting the count. |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. Origin: **design**, **implementation**,
**evidence**, or **documentation**. Axis: **Standards** or **Spec**.

## Record state

**State:** Ready with notes

> Round-2 state is `Ready with notes`: no Blocker or Major remains. R2-1 is Minor and R2-2/R2-3 are Notes; the
> human owner must accept or defer them explicitly before implementation starts. Round-1 findings, evidence, and
> verdict are unchanged below; the round-2 verdict is in `### Re-review round 2`.

`Re-review required` is a record state, not a verdict. Round 2 used `Ready with notes` as its verdict.

## Reviewer independence

I am a fresh, independent review context. I did not author or implement this design and have no shared
conversation with the design agent, the dev, the lead, or any other review agent. The lead supplied only the
frozen commit `7b3148d44496b6feab0bf33c437085e122bd5481`, this record, the repository, and the two required
commands; no desired verdict, summary, or conversation history was provided. My model is DeepSeek V4.1 Flash
1017 (`opencode-go/deepseek-v4.1-flash`).

## Verdict

- [ ] Ready for implementation
- [ ] Ready with notes
- [x] Changes required
- [ ] Waived

Follow-up owner: Design agent (correct M-1, M-2, and M-3 and the accepted Minors); dev implements only after a
new frozen commit.
Follow-up change: Re-freeze the design set with the three Major findings and the accepted Minor findings
corrected, record the resolutions, set the record state to `Re-review required`, and request a fresh review
agent for the new commit. Minor and Note findings do not by themselves justify another round, but M-1–M-3 are
Majors and do.

Round-1 scope note: this is a **pre-implementation** review of the design specification, not of an
implementation. Checklist items phrased as post-implementation existence are assessed against the design text
and left unticked where there is nothing to inspect at this commit; the existence checks belong to the
`Post-implementation verification` pass (finding m-2).

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

The design agent fills this section after a `Changes required` verdict. Do not alter the review agent-authored
findings, evidence, or verdict.

| Finding | Status | Owner | Commit |
| --- | --- | --- | --- |
| M-1 | Fixed | Design agent | fcbd20e |
| M-2 | Fixed | Design agent | fcbd20e |
| M-3 | Fixed | Design agent (prompts); dev (checker) | fcbd20e |
| m-1 | Fixed | Design agent (plan); dev (checker) | fcbd20e |
| m-2 | Fixed | Design agent | fcbd20e |
| m-3 | Fixed | Design agent | fcbd20e |
| m-4 | Fixed | Design agent | fcbd20e |
| m-5 | Fixed | Design agent (plan); dev (checker) | fcbd20e |
| m-6 | Fixed | Design agent | fcbd20e |
| m-7 | Fixed | Design agent | fcbd20e |
| m-8 | Fixed | Design agent | fcbd20e |
| n-1 | Fixed | Design agent | fcbd20e |
| n-2 | Accepted | Design agent | fcbd20e |

No finding is deferred. n-2 is accepted as an evidence-context note; the gate count is now recorded with its
commit context.

### Resolution detail

- **M-1 (Fixed).** The policy-update class trigger now lives in
  `docs/development/design-proportionality.md` §1; `docs/planning/reviews/README.md` §9.1 links that class
  instead of defining a second trigger, and the one-home map names §1 as the home.
- **M-2 (Fixed).** `docs/development/senior-consultation.md` §1 includes a policy-update review in the trigger
  enumeration; B1, B3, and the checker's required text name the four gate types and `policy-update review`.
- **M-3 (Fixed).** The agent prompts now use `default`/`default, free variant`; `dev.md` no longer prescribes a
  “strong-tier pass” and instead points to re-brief-on-default or the two-blocked-round senior escalation;
  `dev-free.md` says “free variant of the default tier”. The checker plan scans `.opencode/agents/*.md` and
  matches `workhorse`, `strong tier`, `strong-tier`, and bare `strong`; the binding test also covers it.
- **m-1 (Fixed).** Published pages use absolute GitHub URLs for root and `docs/planning/` targets; H1 states the
  rule and corrects the “link-checked” claim; `GLOSSARY.md` joins `check_docs_links.ROOT_FILES`.
- **m-2 (Fixed).** A round-2 design-gate checklist asks whether the design specifies each item and names the
  destination; existence and implemented-state checks are explicitly in `## Post-implementation verification`.
  The round-1 checklist text is unchanged.
- **m-3 (Fixed).** The risk table has an Owner column and every risk, including the three Majors, has an owner.
- **m-4 (Fixed).** C2 now says the design-review and audit tables gain `Axis` and keep `Origin`; the
  cross-cutting table gains `Axis` and keeps `Area`; the shared columns are aligned on `ID`, `Axis`, `Severity`.
- **m-5 (Fixed).** B3 names the §9 prose sentences to change, and G1 requires `policy-*.md` and
  `Senior consultant` in the binding prose, not merely `senior` somewhere in the file.
- **m-6 (Fixed).** `senior-consultation.md` §3 and B2 state that the senior is a fresh context receiving only the
  bounded assignment; `.opencode/agents/review.md` adds the senior consultant to its independence sentence.
- **m-7 (Fixed).** `orchestration.md` §2 now allows the lead to facilitate discovery and recommend an option;
  §3 says recommending is not deciding; A5 says the design agent or owner records the decision.
- **m-8 (Fixed).** A6 now says ten new `docs/development` pages, including `senior-consultation.md`.
- **n-1 (Fixed).** The Purpose paragraph names the minimal normative edits in the design set and the remaining
  implementation.
- **n-2 (Accepted).** The 1755 → 1756 → 1757 progression is explained by the record and the new senior page
  being tracked at each freeze; the new evidence records the count and its commit. No action.

## Re-review request (round 2)

Round 1 verdict: `Changes required` at `7b3148d44496b6feab0bf33c437085e122bd5481` (record commit
`669bb733aeaacfb93ec2c134a745d6d22503a963`).

Round 2 target: `fcbd20ed8ef2ad201746fb46b064c9db2e80d056` (the design-set commit; the request commit adds
the freeze SHA and the gate evidence). The lead handoff carries both full SHAs. The reviewed design is
`fcbd20ed8ef2ad201746fb46b064c9db2e80d056` plus this request record; no code, test, contract, or historical
record changes.

What changed: M-1, M-2, and M-3 are fixed in the normative pages and agent prompts; m-1–m-8 are fixed; n-1 is
fixed; n-2 is accepted. The resolution table above is the per-finding record.

Round-2 reviewer: record your metadata, evidence, findings, and verdict in a new `### Re-review round 2` under
`## Review rounds`; leave the round-1 `Request` metadata, checklist marks, findings, evidence, and verdict
unchanged.

### Round 2 design-gate checklist

This checklist asks what the design specifies, not whether the implementation exists. Existence checks are in
`## Post-implementation verification`. The round-1 checklist above is historical and its item text is unchanged.

- [x] **M-1:** `design-proportionality.md` §1 is the single home for the policy-update class trigger; §9.1 and
      the one-home map link it and do not define a second trigger. — Verified: the trigger is only at
      `design-proportionality.md:25-32`; `reviews/README.md:230-233` says "the trigger is not repeated here". R2-1
      records that the class *definition* sentence is still duplicated.
- [x] **M-2:** the senior trigger names design review, audit, cross-cutting review, and policy-update review;
      the record naming and binding allow `policy-<date>-<slug>-M.md`. — Verified in `senior-consultation.md:13-21`,
      record B1/B3, and the round-2 checklist. R2-2 notes `dev.md`'s "review/audit" shorthand.
- [x] **M-3:** the agent prompts contain no `workhorse`, `strong tier`, `strong-tier`, or bare `strong`; the dev
      escalation is re-brief-on-default plus the two-blocked-round senior escalation; the checker/binding plan
      covers `.opencode/agents/*.md` and the retired forms. — Verified by grep over the changed agents and
      process pages; `dev.md:30-32` uses the re-brief/senior rule; E6 and G1 cover the same file set.
- [x] **m-1:** published-to-root/planning links use absolute GitHub URLs; `GLOSSARY.md` is in the checker's
      `ROOT_FILES` plan; H1 no longer claims the relative link is link-checked. — Verified at record A2/H1/G1; the
      docs gate passes with the current absolute-URL pages. GitHub redirects a directory `blob` URL to `tree`.
- [x] **m-2:** the round-2 checklist asks design-specification questions; existence checks are in the
      verification section; the round-1 checklist text is unchanged. — Verified by diff: only the round-2 scope
      note was added; no round-1 item text or mark changed.
- [x] **m-3:** the risk table has an Owner for every risk. — Verified at record lines 634-644; every row names an
      owner.
- [x] **m-4:** C2 states the `Origin`/`Area` difference and the aligned columns. — Verified at record lines
      326-332. R2-3 is a Note on the added `ID`/`Recommendation` columns not being in the checker.
- [x] **m-5:** B3 names the §9 prose sentences; G1 requires the binding text, not just the word `senior`. —
      Verified at record B3 and G1; the §9 prose sentences are named and the required strings are specific.
- [x] **m-6:** the senior is a fresh context in the page, the agent body, and the review-prompt independence
      sentence. — Verified in `senior-consultation.md:42-46`, record B2, and `.opencode/agents/review.md`.
- [x] **m-7:** the lead may facilitate discovery and recommend; recommending is not deciding. — Verified in
      `orchestration.md` §2 ("Facilitate discovery") and §3 ("recommending is not deciding").
- [x] **m-8:** A6 says ten development pages and includes `senior-consultation.md`. — Verified at record A6;
      `docs/development/index.md` and `zensical.toml` both contain the senior page.
- [x] **n-1:** the Purpose states the minimal normative edits in the design set. — Verified at record lines
      34-41.
- [x] **n-2:** accepted with the count/commit context. — Accepted; the policy gate at `8f55aeb` reports 1757
      tracked text files, matching the round-2 evidence.
- [x] The normative-home map is consistent with the fixed pages. — Verified rows 543-544 name the fixed homes;
      R2-1 is the only remaining duplication.
- [x] The file manifest lists the round-2 additions/changes and still protects historical records. — Verified
      against `git diff --name-only origin/main...HEAD`: the 12 changed paths match the manifest, and no
      historical/step/audit/closeout path is present.
- [x] The post-implementation verification remains one bounded fresh pass, separate from this design round. —
      Section I is unchanged and the verification section is still `Not run`.

## Review rounds

Append each new round below. Never overwrite an earlier round.

### Review round 1

#### Independence and frozen state

I am a fresh, independent review context. I did not author or implement this design and have no shared
conversation with the design agent, the dev, the lead, or any other review agent. The lead supplied the frozen
commit `7b3148d44496b6feab0bf33c437085e122bd5481`, this record, and the repository; no desired verdict, summary,
or conversation history was supplied. My model is DeepSeek V4.1 Flash 1017
(`opencode-go/deepseek-v4.1-flash`).

The frozen state is the three-file design/request set on `docs/adopt-workflow-disciplines` over `origin/main`
`245e34a`: `.opencode/agents/review.md`, `docs/planning/reviews/README.md`, and this record. The design set is
documentation and agent configuration only; no code, test, contract, format, CI, or generated artifact is
present in the diff. The request lists the reviewed commit, the design class, scope, non-goals, mapping,
decisions, normative-home map, manifest, checker plan, and verification plan; I reviewed those against the
repository at the frozen commit, not against a summary.

#### Commands and results

| Command | Result |
| --- | --- |
| `git rev-parse HEAD` | `7b3148d44496b6feab0bf33c437085e122bd5481` |
| `git status --short` | empty |
| `git diff --name-only origin/main...HEAD` | the three expected paths; no other file |
| `git diff --stat origin/main...HEAD` | 3 files, +869/−5 |
| `git diff --check` | exit 0 |
| `./tools/verify.sh docs --require` | exit 0; links 217 files; strict Zensical, DocFX 0 warnings/0 errors, rustdoc |
| `./tools/verify.sh policy --require` | exit 0; 26 contract files, 274 space groups, 62 tests OK, 1210 SPDX files, 1756 tracked text files, cargo-deny ok |
| `python tools/check_docs_links.py` | exit 0, `Documentation links OK (217 files scanned).` |
| `python tools/check_process_consistency.py` | exit 0, `Process consistency: OK` |
| zensical scratch builds (`/tmp/gloss-test`, `/tmp/gloss-test2`) | exit 1 on relative links from published pages to a root file and to an excluded `docs/planning/` file |

The full evidence, files read, and the request-owner evidence check are in `Evidence`.

#### Findings

Three **Major** findings and eight **Minor** findings; two Notes.

- **M-1 (Major, Spec, design)** — the policy-update design class has no home in `design-proportionality.md`, and
  the Full trigger is defined there, in §9.1, and in this record.
- **M-2 (Major, Spec, design)** — the senior trigger enumerates design review, audit, and cross-cutting only,
  while §9.1, the record naming, and the B2 binding include policy-update reviews.
- **M-3 (Major, Standards, design)** — the tier retirement leaves `.opencode/agents/dev.md`'s "strong-tier
  pass" escalation rule and `dev-free.md`'s "workhorse" description live, and `check_tier_vocabulary` does not
  scan agent files or catch the `strong-tier`/`strong` forms.
- **m-1 (Minor)** — published-page links to root and `docs/planning/` files fail `zensical build --strict` when
  relative; the design's "link-checked" claim does not match the repository's absolute-URL convention.
- **m-2 (Minor)** — the design-review checklist is written as post-implementation existence checks and
  duplicates the verification pass.
- **m-3 (Minor)** — the risk table has no owner.
- **m-4 (Minor)** — C2 says "keep the existing Origin column" for a template that has `Area`.
- **m-5 (Minor)** — B3 does not name the stale `orchestration.md` §9 prose, and the checker can pass while it
  stays stale.
- **m-6 (Minor)** — the senior's fresh-context independence is not stated.
- **m-7 (Minor)** — `/grill` gives the lead a recommend-an-answer duty the lead boundary does not permit.
- **m-8 (Minor)** — A6 says nine development pages while the design adds ten.
- **n-1 (Note)** — the design set changes two process files despite the "normative pages are not rewritten"
  sentence; the exception is explained in F but should be stated.
- **n-2 (Note)** — the secret-scan file count (1755) is not reproducible at the frozen commit (1756).

The full table, with per-finding evidence and recommendations, is in `Findings`.

#### Verdict

**Changes required.** The design is broadly coherent and implementable: the mapping is complete with a
destination and rationale for every adopted/adapted item, the one-home map is mostly sound, the checker and
binding plan is specific, the post-implementation verification is properly separated, and both required gates
pass at the frozen commit. The three Major findings are material inconsistencies inside the design itself
(two rule-home/trigger contradictions and one live role-prompt contradiction with the new model policy), each
fixable in a small edit. The Minor and Note findings are recorded for the owner's accept/defer decision and do
not by themselves justify another round; the Majors do. A fresh round on a re-frozen commit is required, and a
third Full round would need the owner's decision and a recorded reason.

### Re-review round 2

#### Independence and frozen state

I am a fresh, independent review context. I did not author or implement this design and have no shared
conversation with the design agent, the dev, the lead, or the round-1 review agent. The lead supplied only the
design freeze `fcbd20ed8ef2ad201746fb46b064c9db2e80d056`, the round-2 request tip
`8f55aeb485e640fe6b273d6b2c21afa757f784aa`, this record, and the repository; no desired verdict, summary, or
conversation history was supplied. My model is DeepSeek V4.1 Flash 1017 (`opencode-go/deepseek-v4.1-flash`).

The design under review is the 12-file design set at `fcbd20e` over `origin/main` `245e34a`. The request commit
`8f55aeb` changes only this record (`git diff --stat fcbd20e HEAD`: one file, +33/−25), so the gate results below
apply to the design tree; the record text is the round-2 request. Round 1 is preserved: the `### Review round 1`
block is byte-identical to `669bb73` (74 lines, 4972 bytes, `cmp` clean), and the round-1 findings and checklist
item text are unchanged (only the design agent's `Record state` update and the round-2 scope note were added).

#### Commands and results

| Command | Result |
| --- | --- |
| `git rev-parse HEAD` | `8f55aeb485e640fe6b273d6b2c21afa757f784aa` |
| `git rev-parse origin/main` | `245e34aff7255476d73539d4cc43f068627f0cba` |
| `git status --short` | empty |
| `git diff --stat fcbd20e HEAD` | this record only, +33/−25 |
| `git diff --check` | exit 0, no output |
| `./tools/verify.sh docs --require` | **exit 0**; math 215 files, links 218 files; Zensical strict "No issues found"; DocFX 0 warnings/0 errors; rustdoc 7 crates; `Documentation built in …/site`; `Closeout gate completed with required checks enabled.` |
| `./tools/verify.sh policy --require` | **exit 0**; 26 contract files; 5 + 4 + 2 projection fixtures; 274 space-group settings; `Process consistency: OK`; `Ran 62 tests … OK`; 1210 SPDX files; 1757 tracked text files; `advisories ok, bans ok, licenses ok, sources ok`; `Closeout gate completed with required checks enabled.` |
| `python tools/check_docs_links.py` | exit 0, `Documentation links OK (218 files scanned).` |
| `python tools/check_process_consistency.py` | exit 0, `Process consistency: OK` |
| `python tools/check_doc_math.py` | exit 0, `Documentation math conventions OK (215 files scanned).` |
| round-1 block `cmp` against `669bb73` | identical |

#### Round-1 finding verification

| Finding | Fix location | Result |
| --- | --- | --- |
| M-1 | `design-proportionality.md:25-32`; `reviews/README.md:230-233`; record lines 103-105 and map row 543 | **Fixed.** One trigger home; §9.1 links and disclaims. R2-1 notes the definition sentence is repeated. |
| M-2 | `senior-consultation.md:13-21`; record B1/B3 and checklist M-2 | **Fixed.** All four families named; the binding and record naming allow policy records. R2-2 notes `dev.md`'s shorthand. |
| M-3 | `.opencode/agents/{design,dev-free,dev,lead,review}.md`; `design-proportionality.md:11`; `orchestration.md:21-22,36-39`; record E5/E6/G1 | **Fixed.** Grep finds no retired forms in the changed agents or the rewritten pages; the checker/binding plan covers the same set. |
| m-1 | record A2 (209-214), H1 (476-481), G1 (457-458) | **Fixed.** Absolute GitHub URLs; docs gate passes; a directory `blob` URL redirects to `tree`. |
| m-2 | record round-2 checklist | **Fixed.** Design-specification questions; round-1 item text unchanged. |
| m-3 | record risks (634-644) | **Fixed.** Owner column present for every risk. |
| m-4 | record C2 (326-332) | **Fixed.** Origin/Area difference and aligned columns stated. R2-3 notes the added `ID`/`Recommendation` columns are not checked. |
| m-5 | record B3 (286-301), G1 (442-451) | **Fixed.** §9 prose named; the required strings are specific. |
| m-6 | `senior-consultation.md:42-46`; record B2 (279-281); `.opencode/agents/review.md` | **Fixed.** Fresh context stated in all three. |
| m-7 | `orchestration.md` §2/§3; record A5 (233-237) | **Fixed.** "Facilitate discovery"; "recommending is not deciding". |
| m-8 | record A6 (245-248); `docs/development/index.md`; `zensical.toml` | **Fixed.** Ten pages named; the senior page is in the index and nav. |
| n-1 | record Purpose (34-41) | **Fixed.** Minimal normative edits named. |
| n-2 | record round-2 evidence (710-713); policy gate | **Accepted.** 1757 matches the gate at `8f55aeb`. |

#### Findings

Three findings: one **Minor** and two **Notes**. No Blocker or Major.

| ID | Axis | Severity | Origin | Finding | Evidence | Recommendation |
| --- | --- | --- | --- | --- | --- | --- |
| R2-1 | Spec | **Minor** | **documentation** | **The policy-update class definition is now stated twice.** `design-proportionality.md` §1 and `reviews/README.md` §9.1 open with the same sentence ("A policy-update review is the design-review gate for a process, workflow, role, or agent-prompt change that is not a step design"). The *trigger* has one home and §9.1 says it is not repeated, so the round-1 M-1 defect is fixed; the duplicated definition is a drift risk, not a contradiction. | `docs/development/design-proportionality.md:25-27`; `docs/planning/reviews/README.md:219-221` | Make §9.1's opening a one-line summary that links §1, or accept the duplication as a deliberate summary. |
| R2-2 | Spec | **Note** | **documentation** | **The dev prompt compresses the senior trigger to "a review/audit deadlock".** The normative trigger names design review, audit, cross-cutting review, and policy-update review; "review/audit" is a generic shorthand that could be read as excluding the last two. The lead, not the dev, decides the spawn, so this is not material. | `.opencode/agents/dev.md:30-32`; `docs/development/senior-consultation.md:15-16` | Say "review or audit deadlock" or name the four gate types if the prompt should mirror the trigger exactly. |
| R2-3 | Standards | **Note** | **evidence** | **C2 adds `ID` and `Recommendation` to the step-design and audit findings tables, but G1 only checks for an `Axis` column, and the resolution detail says only "gain Axis and keep Origin".** The target schema is explicit and implementable; the two extra columns are not covered by the planned checker, so a partial implementation would pass. | record C2 (326-332), G1 (445); `docs/planning/reviews/step-design-review-template.md:86`; `docs/planning/audits/step-audit-template.md:90` | Either state that only the `Axis` column is required, or add the `ID`/`Recommendation` requirement to G1. |

#### Verdict

- [ ] Ready for implementation
- [x] Ready with notes
- [ ] Changes required
- [ ] Waived

**Ready with notes.** No Blocker or Major remains, so the round-2 design review passes the Full stop rule and a
third round is not justified. The three round-1 Majors are fixed in the normative documents and agent prompts,
the eight Minors are fixed, and n-2 is accepted. The revised published-page link plan is implementable: the
absolute-URL convention matches the existing pages, `check_docs_links.py` treats the URLs as external, and the
docs gate passes with strict Zensical. The remaining R2-1 (Minor) and R2-2/R2-3 (Notes) are recorded for explicit
owner acceptance or deferral; none blocks implementation. Round-1 text is unchanged.

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
