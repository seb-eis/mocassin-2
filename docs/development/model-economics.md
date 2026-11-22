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
6. **The review agent is never the dev's context.** A different model is better; a different context is
   mandatory.

## 2. Default policy

Five rules, in order:

1. **Free capacity first.** Boilerplate and throwaway work use a free model. Ordinary implementation uses the free
   variant of the workhorse (`dev-free`). The paid workhorse is the fallback when the free variant is rate-limited,
   degraded, or withdrawn.
2. **Implementation** — the default for an approved feature or fix — uses the **workhorse** tier, free variant
   first and paid variant second.
3. **Routing, design, and review** — anything ambiguous, cross-cutting, public-contract, or high-blast-radius —
   use the **strong** tier. So does a workhorse task that has failed twice.
4. **Never use a premium-cap model as a default.** A model whose monthly allowance cap is small relative to its
   token price burns the plan in a day. If a task genuinely needs frontier capability, that is an owner decision,
   not a silent upgrade. On a metered subscription, compare the per-token price against the model's allowance
   cap, not against the subscription price.
5. **Review with the strong tier plus tests.** The dev's model must not review its own output.

### Work classes

The free tier serves two different kinds of work. Keep them apart:

| Work | Model | Process |
| --- | --- | --- |
| Throwaway scratch, not committed | Any free model, preferably not `dev-free` | None — no branch, no record; discard it |
| Trivial committed (`None` design class) | `dev-free` | Local gates; one-line record; no design review |
| Ordinary implementation | `dev-free`, then `dev` | Full step process |
| Routing, design, review | `lead`, `design`, `review` | Full process |

Throwaway work is a **work class, not a model tier and not an agent**. It is a probe script, a one-off check, or a
snippet you will delete. Keep it out of the repository or under the gitignored `.tmp/` or `/home/node/tmp`, and
prefer a free model other than `dev-free` so the workhorse's free allowance is preserved for committed work.

Once the output will be committed, it is no longer throwaway: classify it as `None` under
[`design-proportionality.md`](design-proportionality.md), use `dev-free`, and run the normal local gates.

## 3. Role assignment

| Role | Tier | Why |
| --- | --- | --- |
| Lead | strong | Intake and routing decide the whole downstream spend; a misroute costs more than the model. |
| Design agent | strong | Ambiguity, trade-offs, and scope are judgment work. |
| Dev | workhorse | Bulk tokens, bounded scope, verified by tests; `dev-free` first, paid fallback. |
| Review agent | strong | Precision and "what is missing" matter more than recall, and the model must differ from the dev. |

Design review and audit use the same `review` agent: strong tier, fresh context.

Design is the strong tier's most expensive output. Right-size it with
[`design-proportionality.md`](design-proportionality.md) so strong-tier tokens are not spent on mechanical changes.

The design agent and the review agent may share a model when one subscription is the constraint. They must not
share a context, and neither may review work that the dev model produced in the same session.

## 4. Escalation

Escalate from the workhorse tier to the strong tier when any of these is true:

- the local gate fails twice on the same feature;
- the same file has been edited three times without a green gate;
- the change crosses a public contract, schema, or dependency;
- the request is ambiguous, or two plausible designs exist;
- the diff is growing while tests still fail.

Escalate once, with the failure evidence attached. Repeating the same prompt on the same model is not escalation.
Record an escalation that changes scope or design in the step page.

## 5. Cost guardrails

- **One subscription.** Do not stack plans to cover a busy week; move work between tiers instead.
- **Hard-cap the balance.** Keep the prepaid balance at zero and extra usage off, so the monthly allowance is the
  ceiling. A runaway loop then stops instead of billing.
- **Do not buy credits for volume.** Credits are metered at list price. If the allowance is genuinely exhausted in
  a normal month, upgrade the plan once; do not top up repeatedly.
- **Free capacity before paid.** Use free models for scratch work and the free variant of the workhorse for
  ordinary implementation while its allowance lasts. Prefer a free model other than `dev-free` for scratch work,
  so the workhorse allowance is not spent on output you will discard. Fall back to the paid workhorse only when
  the free variant is rate-limited, degraded, or withdrawn.
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
| strong | `lead`, `design`, `review` | `opencode-go/glm-5.3` |
| workhorse, free | `dev-free` | `opencode/deepseek-v4.1-flash-free` |
| workhorse, paid | `dev` | `opencode-go/deepseek-v4.1-flash` |

The dev role has two bindings so the lead can execute the free-first rule: it spawns `dev-free`, and when the
spawn fails or that binding reports a rate limit, a degraded model, or unavailability, it re-spawns the same
assignment on `dev` and records the fallback. Both bindings follow `.opencode/agents/dev.md`; only the model line
differs. The lead never upgrades a role to a premium model on its own.

Free variants are time-limited and can disappear, so the paid binding is part of the policy rather than an
optional convenience. Change the provider prefix and model IDs when a project uses another provider; keep the
tiers, the two workhorse bindings, and the role assignment. Model names change faster than the policy does.

## 7. Anti-patterns

- the most expensive model as the default;
- the workhorse arbitrating a design disagreement;
- the dev reviewing its own diff, or a review agent sharing the dev's context;
- treating a retry as escalation;
- buying credits or stacking subscriptions instead of changing tier;
- routing on task size alone, so a large mechanical change gets the strong tier and a small contract change does
  not;
- spending strong-tier tokens on boilerplate;
- the lead silently promoting itself or a role to a premium model.

## 8. Relation to other rules

- Design depth: [`design-proportionality.md`](design-proportionality.md).
- Role write scopes and spawn rules: [`orchestration.md`](orchestration.md) and
  [`AGENTS.md` §3.8](https://github.com/seb-eis/mocassin-2/blob/main/AGENTS.md).
- Review and audit records: [`docs/planning/reviews/README.md`](https://github.com/seb-eis/mocassin-2/blob/main/docs/planning/reviews/README.md) and
  [`docs/planning/audits/README.md`](https://github.com/seb-eis/mocassin-2/blob/main/docs/planning/audits/README.md).
- Adoption profile: [`AGENTS.md` §3.15](https://github.com/seb-eis/mocassin-2/blob/main/AGENTS.md) and
  [`docs/planning/README.md`](https://github.com/seb-eis/mocassin-2/blob/main/docs/planning/README.md).
