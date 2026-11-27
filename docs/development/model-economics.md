# Model economics

This page is the normative home for choosing a model for each role in the MOCASSIN step process. The role write
scopes are in [`orchestration.md`](orchestration.md) and [`AGENTS.md` §3.8](https://github.com/seb-eis/mocassin-2/blob/main/AGENTS.md);
this page decides which tier performs the work and how cost is capped.

Model choice is a cost decision, not a prestige decision. An agent that finishes in one pass on a cheap model is
cheaper than an expensive model that also finishes in one pass, and both are cheaper than three failed attempts.

## 1. Principles

1. **Spend where tokens are few and consequences are large.** Lead routing, design, and review read little and
   decide much. Bulk implementation writes a lot and is bounded by tests and scope.
2. **Cost per completed step, not per token.** A failed attempt is not a discount. Count the retries.
3. **Route on ambiguity and blast radius, not size.** A thirty-file rename is cheap. A three-line contract change
   is not.
4. **The plan is not the lever; the model inside it is.** With one subscription, switching tier is free.
   Stacking subscriptions is not.
5. **Cap risk with permissions and gates, not with a bigger model.** MOCASSIN's branches, local gates,
   design review, audit, and owner approval are the controls.
6. **Review independence comes from a fresh context, not a different model.** The review agent may use the same
   model as the dev. It must not share the dev's context.

## 2. Default policy

Five rules, in order:

1. **Free capacity first.** Boilerplate and throwaway work use a free model. Ordinary implementation uses the free
   variant of the default tier (`dev-free`, bound to a temporary free model until 2026-12-11 — see §6). The paid
   default (`dev`) is the fallback when the free variant is rate-limited, degraded, or withdrawn.
2. **The default tier carries the standing roles.** Lead, design, dev, and review use
   `opencode-go/deepseek-v4.1-flash`. Implementation is free variant first and paid variant second.
3. **Senior is the only escalation.** A focused senior consultation uses `opencode-go/deepseek-v4-pro`, after the
   trigger in [`senior-consultation.md`](senior-consultation.md) is met. It is not a standing role.
4. **Never use a premium-cap or frontier model as a default.** A model whose monthly allowance cap is small
   relative to its token price burns the plan in a day. If a task genuinely needs frontier capability, that is an
   owner decision, not a silent upgrade. On a metered subscription, compare the per-token price against the model's
   allowance cap, not against the subscription price.
5. **Review on the default tier plus tests.** The dev's context must not review its own output. Independence is the
   fresh context, not a model difference.

### Work classes

The free variant serves two different kinds of work. Keep them apart:

| Work | Model | Process |
| --- | --- | --- |
| Throwaway scratch, not committed | Any free model, preferably not `dev-free` | None — no branch, no record; discard it |
| Trivial committed (`None` design class) | `dev-free` | Local gates; one-line record; no design review |
| Ordinary implementation | `dev-free`, then `dev` | Full step process |
| Routing, design, review | `lead`, `design`, `review` | Full process |
| Senior consultation | `senior`, escalation only | One bounded question; advisory |

Throwaway work is a **work class, not a model tier and not an agent**. It is a probe script, a one-off check, or a
snippet you will delete. Keep it out of the repository or under the gitignored `.tmp/` or `/home/node/tmp`, and
prefer a free model other than `dev-free` so the default tier's free allowance is preserved for committed work.

Once the output will be committed, it is no longer throwaway: classify it as `None` under
[`design-proportionality.md`](design-proportionality.md), use `dev-free`, and run the normal local gates.

## 3. Role assignment

| Role | Tier | Why |
| --- | --- | --- |
| Lead | default | Intake and routing decide the whole downstream spend; a misroute costs more than the model. |
| Design agent | default | Ambiguity, trade-offs, and scope are judgment work. |
| Dev | default | Bulk tokens, bounded scope, verified by tests; `dev-free` first, paid fallback. |
| Review agent | default | Precision and "what is missing" matter more than recall; independence is the fresh context. |
| Senior consultant | senior | One focused consultation after two blocked standard rounds; advisory only. |

Design review and audit use the same `review` agent: default tier, fresh context each time.

Design is the default tier's most expensive output. Right-size it with
[`design-proportionality.md`](design-proportionality.md) so default-tier tokens are not spent on mechanical changes.

The design agent and the review agent may share a model; with one subscription that is the normal case. They must
not share a context, and neither may review work the dev model produced in the same session.

## 4. Escalation

### 4.1 Within the default model

Before any senior consultation, escalate by **re-briefing on the default model**: a fresh context, the same bounded
assignment, and the failure evidence attached. This is the ordinary response to a stalled task.

Re-brief when any of these is true:

- the local gate fails twice on the same feature;
- the same file has been edited three times without a green gate;
- the change crosses a public contract, schema, or dependency;
- the request is ambiguous, or two plausible designs exist;
- the diff is growing while tests still fail.

Record a re-brief that changes scope or design in the step page. Repeating the same prompt on the same model is not
escalation.

### 4.2 Senior focused consultation

The senior tier is the only model escalation. The lead may request **one** focused consultation only when:

- two completed standard rounds of the same design review, audit, cross-cutting review, or policy-update review
  each ended blocked;
- at least one Blocker or Major from the second round is still open;
- the remaining disagreement reduces to one answerable question.

The trigger, request contract, senior rules, and advisory effect are in
[`senior-consultation.md`](senior-consultation.md); do not repeat them here. The answer is advisory, the owner
decides, and a fresh standard round at the new commit remains the gate. A senior consultation is not a third
standard round and not a verdict.

## 5. Cost guardrails

- **One subscription.** Do not stack plans to cover a busy week; move work between tiers instead.
- **Hard-cap the balance.** Keep the prepaid balance at zero and extra usage off, so the monthly allowance is the
  ceiling. A runaway loop then stops instead of billing.
- **Do not buy credits for volume.** Credits are metered at list price. If the allowance is genuinely exhausted in
  a normal month, upgrade the plan once; do not top up repeatedly.
- **Free capacity before paid.** Use free models for scratch work and the free variant of the default tier for
  ordinary implementation while its allowance lasts. Prefer a free model other than `dev-free` for scratch work,
  so the default allowance is not spent on output you will discard. Fall back to the paid default only when the
  free variant is rate-limited, degraded, or withdrawn.
- **Senior budget: one consultation per unresolved gate question.** Repeat only with a new explicit question, and
  only after telling the owner why the earlier answer did not settle it. Never use senior as a standing reviewer or
  a general upgrade.
- **Review the bill with the work.** If a step cost more than its risk justifies, record why in the step page or
  the closeout note.

Free variants are time-limited and provider-specific. One free model may publish a daily request cap while another
publishes none, so treat "free first" as a preference with a fallback, not an entitlement. Do not hard-code a free
model as the only implementation path.

Provider-specific discounts and time-limited promotions are deliberately out of scope here. They change, and a
process rule that depends on them rots.

## 6. Reference binding

The MOCASSIN OpenCode binding uses OpenCode Go as the reference provider and keeps `dev-free` and `dev` as two
bindings of one dev role:

| Tier | Binding | Model |
| --- | --- | --- |
| default | `lead`, `design`, `review` | `opencode-go/deepseek-v4.1-flash` |
| default | `dev` | `opencode-go/deepseek-v4.1-flash` |
| default, free (temporary) | `dev-free` | `opencode-go/hungerg-free` |
| senior | `senior` | `opencode-go/deepseek-v4-pro` |

**Re-pinned 2026-11-23.** By owner decision the lead, design, and review binding moved from
`opencode-go/glm-5.3` to `opencode-go/deepseek-v4.1-flash` for cost. **Adopted 2026-11-27.** The tier names are now
`default` and `senior`; `senior` is the only escalation. The agent files (`.opencode/agents/*.md`) carry the binding
and `tools/tests/test_opencode_binding.py` enforces this table, so a tier change updates both with it.

**Temporary dev-free exception, 2026-11-27 to 2026-12-11.** By owner decision the `dev-free` binding runs
HungerG Free (`opencode-go/hungerg-free`, cost 0 in the provider catalog) for a cost-measurement experiment. The
scope is only the `dev-free` binding: `dev` remains the paid `default` binding and the fallback, and `lead`,
`design`, `review`, and `senior` are unchanged. This is a temporary exception to "free variant of the default
tier" (section 2 rule 1), not a tier change. On **2026-12-11** the binding reverts to the standard
`opencode/deepseek-v4.1-flash-free`; the revert steps are in
[`docs/planning/maintenance/2026-11-27-temporary-free-dev-binding.md`](https://github.com/seb-eis/mocassin-2/blob/main/docs/planning/maintenance/2026-11-27-temporary-free-dev-binding.md).

The dev role has two bindings so the lead can execute the free-first rule: it spawns `dev-free`, and when the
spawn fails or that binding reports a rate limit, a degraded model, or unavailability, it re-spawns the same
assignment on `dev` and records the fallback. Both bindings follow `.opencode/agents/dev.md`; only the model line
differs. The lead never upgrades a role to a premium or frontier model on its own.

Free variants are time-limited and can disappear, so the paid binding is part of the policy rather than an
optional convenience. Change the provider prefix and model IDs when a project uses another provider; keep the
tiers, the two dev bindings, and the role assignment. Model names change faster than the policy does.

## 7. Anti-patterns

- the most expensive model as the default;
- a dev model arbitrating a design disagreement;
- the dev reviewing its own diff, or a review agent sharing the dev's context;
- treating a retry as escalation;
- buying credits or stacking subscriptions instead of changing tier;
- routing on task size alone, so a large mechanical change gets the senior tier and a small contract change does
  not;
- spending senior tokens on boilerplate, or using senior as a standing role;
- a senior consultation before two completed blocked rounds, as a third standard round, or as a verdict;
- the lead silently promoting itself or a role to a premium model.

## 8. Relation to other rules

- Design depth: [`design-proportionality.md`](design-proportionality.md).
- Senior trigger and effect: [`senior-consultation.md`](senior-consultation.md).
- Role write scopes and spawn rules: [`orchestration.md`](orchestration.md) and
  [`AGENTS.md` §3.8](https://github.com/seb-eis/mocassin-2/blob/main/AGENTS.md).
- Review and audit records: [`docs/planning/reviews/README.md`](https://github.com/seb-eis/mocassin-2/blob/main/docs/planning/reviews/README.md) and
  [`docs/planning/audits/README.md`](https://github.com/seb-eis/mocassin-2/blob/main/docs/planning/audits/README.md).
- Adoption profile: [`AGENTS.md` §3.15](https://github.com/seb-eis/mocassin-2/blob/main/AGENTS.md) and
  [`docs/planning/README.md`](https://github.com/seb-eis/mocassin-2/blob/main/docs/planning/README.md).
