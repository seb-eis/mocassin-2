# Policy adoption — 2026-11-22

**Status:** Merged into `main` as `9bb92e1ec2ec6825a688c4f6e201684ed8815acd` (`--no-ff`), confirmation CI
`45033099422` green, branch deleted locally and remotely, no tag.
**Source:** private `seb-eis/opencode-workflow` repository at commit
`74e3c6dc2fb9228665fa0f785f4565b5b138527f`, read-only clone at `/home/node/tmp/opencode-workflow`.
**Branch:** `docs/policy-adoption` from `main` at `214b09ac0457e8f2203a80086ed9656e1c46f7cf`.
**Scope:** targeted full adoption of the upstream process model into MOCASSIN's existing 32-step process.

## 1. Owner decisions

1. **Adoption profile:** `standard`. Recorded in `AGENTS.md` §1 and §3.15 and in `docs/planning/README.md`.
2. **Role names:** `lead`, `design`, `dev-free`, `dev`, `review` replace `orchestrator`, `planner`,
   `implementor`, `reviewer`. The 32-step model, `step/<NN>-<slug>` branches, `feature/...` branches, `step-NN`
   tags, and the `step-start`/`step-closeout` commands are kept. Steps are not renamed to work packages, and tags
   are not renamed to `wp-NN`.
3. **Model binding:** strong `opencode-go/glm-5.3` for lead, design, and review; free workhorse
   `opencode/deepseek-v4.1-flash-free` for `dev-free`; paid workhorse `opencode-go/deepseek-v4.1-flash` for
   `dev`. Free-first; the paid binding is the fallback on a rate limit, a degraded model, or unavailability.
   Premium models are never a default; a frontier upgrade is an owner decision.
4. **Design classes:** `Full`, `Light`, and `None`, with MOCASSIN-specific triggers, the review stop rule, the
   pre-audit gate, and the finding-origin metric.
5. **`None` pre-authorization (standard-profile adaptation):** under `standard`, `None` is pre-authorized for
   documentation, typos, test-only changes, mechanical refactors, and generated-file refreshes. They need a
   one-line record but **no per-item owner waiver**. This deliberately departs from the upstream rule that a
   `None` class on a reviewing profile is a recorded waiver. `Full` and `Light` are unchanged.
6. **Effective from:** Step 15 and any amendment started after 2026-11-22. Steps 00–14, their reviews, audits,
   closeouts, and tags are historical and are not rewritten. Historical records keep the role names in force
   when they were written.
7. **No independent design review for this process change:** the owner approved the adoption, and the policy
   itself is the design. There is no `docs/planning/reviews/` record for this change and no new review gate.
8. **No upstream hooks:** `.githooks` and `GATE_HISTORY` are not imported. MOCASSIN uses its existing
   `tools/verify.sh` gates, branch protection, review/audit gates, and owner approval.
9. **§1 state correction:** the stale "Current state" paragraph in `AGENTS.md` was corrected from
   `docs/planning/status.md` at `214b09a`: Steps 00–14 plus 23A/23B are done, `XC-04` is accepted, and Step 15 is
   next. The paragraph is non-normative and concise.

## 2. Old → new mapping

| Old | New | Notes |
| --- | --- | --- |
| Orchestrator | Lead | Single owner interface; no edits; spawns only the defined roles |
| Planner / planning agent | Design agent | Design docs, ADRs, review request scope; no code |
| Planning auditor (design reviewer) | Review agent | Same `review` binding as the audit; fresh context |
| Implementor / step agent | Dev (`dev-free` or `dev`) | One role, two model bindings |
| Reviewer / auditor | Review agent | One role for design review, audit, and cross-cutting review |
| `.opencode/agents/orchestrator.md` | `.opencode/agents/lead.md` | `mode: primary` |
| `.opencode/agents/planner.md` | `.opencode/agents/design.md` | `mode: subagent` |
| `.opencode/agents/implementor.md` | `.opencode/agents/dev.md` | Paid workhorse |
| — | `.opencode/agents/dev-free.md` | Free workhorse; delegates the prompt to `dev.md` |
| `.opencode/agents/reviewer.md` | `.opencode/agents/review.md` | Record-only edits; no subagents |
| `default_agent: orchestrator` | `default_agent: lead` | `opencode.json` |
| Upstream `wp-start` / `wp-closeout` | `step-start` / `step-closeout` | MOCASSIN command names kept |
| Upstream `wp-NN` tags | `step-NN` tags | MOCASSIN tag names kept |
| Upstream `.githooks` / `GATE_HISTORY` | Existing MOCASSIN gates | Not imported |
| Design review every work package | Design review per design class | `None` pre-authorized for low-risk categories |
| Review until no findings | Stop at no Blocker/Major | Minor/Note accepted; no re-review for notes alone |
| Audit carries bookkeeping | Pre-audit gate before the request | Request owner records the mechanical checks |

## 3. Effective-from rule

The new rules apply from **Step 15 (Energy Engine)** and to any amendment started after **2026-11-22**:

- The role names are used in process docs and in new records from Step 15. Historical records keep the old names;
  do not rewrite them.
- The `Design class` field is added to Step 15 onward and to the amendment template. Completed step pages are not
  backfilled.
- The standard profile, model economics, design classes, stop rule, pre-audit gate, and `None`
  pre-authorization apply to new work from the adoption date.
- Existing CI policy and closeout evidence rules are unchanged. The standard profile's gate matrix maps onto the
  current MOCASSIN gates: local gates, one closeout CI run, one automatic `main` run, review, audit, and owner
  approval.
- The binding/config/tool changes take effect when the implementor lands them on this branch; until then, the
  old agent files remain the working binding.

## 4. Expected cost benefit

- **Fewer review rounds.** A Full review stops at no Blocker/Major; a Light review is one pass; Minor/Note
  findings do not trigger another round; a third Full round needs the owner. This removes the previous
  incentive to keep reviewing for notes.
- **Bookkeeping leaves the audit.** The pre-audit gate maps every acceptance criterion to evidence and checks the
  step page/status/feature log/closeout consistency before the audit request. The review agent spends its pass on
  judgment.
- **Free-first implementation.** Ordinary implementation runs on `dev-free`; the paid `dev` is used only on a
  rate limit, degradation, or unavailability. Scratch work uses a free model other than `dev-free`.
- **Proportional design.** `None` work (docs, typos, tests, mechanical refactors, generated refreshes) skips the
  design review with a one-line record; `Light` keeps a one-page design and one pass; `Full` is reserved for
  contracts, formats, dependencies, cross-package interfaces, migrations, security/concurrency, irreversible
  decisions, and architecture boundaries.
- **Strong tier where judgment matters.** Lead, design, and review use the strong model; the dev workhorse is
  bounded by tests and scope.

## 5. Implementor task list (binding, config, and tooling)

The planner must not edit `.opencode/` or `tools/`; the implementor applies this list on the same branch after
the docs commit. The docs commit is documentation-only; the binding commit is separate.

### 5.1 `.opencode/agents/lead.md`

Create from the current `orchestrator.md` body, with:

```yaml
---
description: Owner's only interface. Routes step work, spawns the role agents, and relays decisions. Never designs, implements, reviews, approves, merges, or tags.
mode: primary
# Reference tier: strong. See docs/development/model-economics.md.
model: opencode-go/glm-5.3
permissions:
  - { action: edit, resource: "*", effect: deny }
  - { action: subagent, resource: "*", effect: deny }
  - { action: subagent, resource: "design", effect: allow }
  - { action: subagent, resource: "dev-free", effect: allow }
  - { action: subagent, resource: "dev", effect: allow }
  - { action: subagent, resource: "review", effect: allow }
---
```

Body: coordinate the 32-step process; spawn `design` for design, `dev-free` first for approved features/fixes,
paid `dev` only as the fallback, and `review` for a frozen design or closeout; assign tiers per
`docs/development/model-economics.md`; never upgrade to a premium model; create worktrees under
`/home/node/worktrees`; start with `/step-start` and finish with `/step-closeout`.

### 5.2 `.opencode/agents/design.md`

```yaml
---
description: Turns an approved request into a frozen step design. Writes design docs and the review request; never code or verdicts.
mode: subagent
# Reference tier: strong. See docs/development/model-economics.md.
model: opencode-go/glm-5.3
permissions:
  - { action: edit, resource: "src/**", effect: deny }
  - { action: edit, resource: "tests/**", effect: deny }
  - { action: edit, resource: "tools/**", effect: deny }
  - { action: edit, resource: "contracts/**", effect: deny }
---
```

Body: the current `planner.md` body, with role names updated to `design agent`, `dev`, and `lead`; require the
step page to record the design class and the review record to record it.

### 5.3 `.opencode/agents/dev-free.md`

```yaml
---
description: Implements an approved feature or fix on the free workhorse tier, and reports a rate limit instead of switching models. Never reviews its own work or merges without approval.
mode: subagent
# Reference tier: workhorse, free variant first. See docs/development/model-economics.md.
model: opencode/deepseek-v4.1-flash-free
---
```

Body: "Read `.opencode/agents/dev.md` now and treat its body as your instructions; it is the single source of the
dev prompt, and this file changes only the model. If the model is unavailable, rate-limited, or degraded, stop
and report it to the lead. Do not switch to another model yourself; the lead chooses the fallback and re-spawns
the same bounded assignment."

### 5.4 `.opencode/agents/dev.md`

```yaml
---
description: Implements an approved feature or fix, runs the local gates, and records the evidence. Never reviews its own work or merges without approval.
mode: subagent
# Reference tier: workhorse. See docs/development/model-economics.md.
model: opencode-go/deepseek-v4.1-flash
---
```

Body: the current `implementor.md` body, with these changes:

- role name `dev`;
- follow the design class and the step page;
- run the pre-audit gate before the audit request;
- use `dev-free` as the ordinary path; the lead handles the fallback;
- escalate to the lead for a strong-tier pass if the local gate fails twice, the same file is edited three times
  without a green gate, or the change crosses a public contract, schema, or dependency;
- finish with the output contract in `docs/development/agent-workflow.md`.

### 5.5 `.opencode/agents/review.md`

```yaml
---
description: Fresh, read-only review agent. Writes only its assigned record; never fixes findings, implements, merges, or tags.
mode: subagent
# Reference tier: strong, and different from the dev. See docs/development/model-economics.md.
model: opencode-go/glm-5.3
permissions:
  - { action: edit, resource: "*", effect: deny }
  - { action: edit, resource: "docs/planning/reviews/step-*.md", effect: allow }
  - { action: edit, resource: "docs/planning/reviews/cross-cutting/xc-*.md", effect: allow }
  - { action: edit, resource: "docs/planning/audits/step-*.md", effect: allow }
  - { action: subagent, resource: "*", effect: deny }
---
```

Body: the current `reviewer.md` body, renamed to review agent; add the design class/stop rule (Full stops at no
Blocker/Major; Light one pass; no new round for Minor/Note alone) and the pre-audit gate as context.

### 5.6 `opencode.json`

```json
{
  "$schema": "https://opencode.ai/config.json",
  "default_agent": "lead"
}
```

### 5.7 `.opencode/commands/step-start.md`

- Frontmatter: `description: Start a step through the lead`, `agent: lead`.
- Body: spawn `design` to complete the step page and `docs/planning/reviews/step-NN.md`; when frozen, spawn
  `review`; present the verdict and the feature-branch list and stop for approval. Do not create the step branch
  before approval.

### 5.8 `.opencode/commands/step-closeout.md`

- Frontmatter: `description: Run the step closeout, audit, and merge-approval sequence`, `agent: lead`.
- Body: spawn `dev-free` (fallback `dev`) to finish the closeout feature, run the pre-audit gate, merge `main`,
  run `./tools/verify.sh all --require`, dispatch one closeout CI run, fill the closeout evidence and check it,
  and create `docs/planning/audits/step-NN.md`; freeze; spawn `review`; route fixes to the dev; present the
  cumulative approval report and stop for approval. Do not merge, tag, or write the record as the lead.

### 5.9 `tools/check_process_consistency.py`

Replace the four old agent filenames in `REQUIRED_FILES` with the new binding:

```python
    "opencode.json",
    ".opencode/agents/lead.md",
    ".opencode/agents/design.md",
    ".opencode/agents/dev-free.md",
    ".opencode/agents/dev.md",
    ".opencode/agents/review.md",
    ".opencode/commands/step-start.md",
    ".opencode/commands/step-closeout.md",
```

Do not remove `("AGENTS.md", ".opencode/agents")`, `("docs/development/orchestration.md", ".opencode/agents")`,
or `("docs/development/orchestration.md", "default_agent")` from `REQUIRED_INTEGRATION`; the updated docs keep
those strings. Run `./tools/verify.sh policy --require` after the change.

### 5.10 Delete/rename

Delete the old agent files after the new files are in place and the tool list is updated. Do not leave both
bindings in place: the lead's `subagent` allow-list names the new agents, and an old file would be an
unreachable duplicate. Restart or warm the OpenCode service if the agent list is cached
(`opencode api get /api/info`).

## 6. Verification and residual risks

Verification for this docs commit:

- `./tools/verify.sh docs` — link/index and docs build.
- `./tools/verify.sh policy` — process consistency. It is expected to pass on this docs commit because the old
  agent files still exist. It will fail after the agent rename until `tools/check_process_consistency.py` is
  updated; that update is item 5.9 and is part of the same branch before the binding commit is considered done.

Residual risks:

- **Policy gate ordering.** The rename and the checker update must land together. A commit that renames the
  agents without updating `REQUIRED_FILES` fails the policy gate by design.
- **Two vocabularies in history.** Steps 00–14 and their records use the old role names. Readers must treat the
  mapping in §2 as the bridge; do not rewrite historical records.
- **Free-tier availability.** `dev-free` can be rate-limited or withdrawn. The paid `dev` is the policy fallback;
  the lead must record the fallback, not silently switch models.
- **Lead cannot edit.** Routing every typo/link fix through the dev adds a spawn. This is deliberate: the
  binding makes the lead's no-edit rule enforceable, and the dev is the cheapest committed-work tier.
- **`None` pre-authorization.** It could be used to skip design on work that deserves a review. The mitigation is
  the one-line record, the explicit trigger list, and the audit; a `None` step that later reveals a Full trigger
  is reclassified before implementation.
- **No independent review of this adoption.** The owner approved the policy and accepted the source as the
  design. A later cross-cutting review can check the adoption against the upstream source.
- **Stale status.** The §1 paragraph is corrected from `status.md` at `214b09a`; if the dashboard moves before
  the merge, the paragraph may need a one-line refresh in the closeout commit.

## 7. Related records

- [`AGENTS.md` §3.8–§3.12, §3.15](../../../AGENTS.md).
- [`docs/development/orchestration.md`](../../development/orchestration.md).
- [`docs/development/agent-workflow.md`](../../development/agent-workflow.md).
- [`docs/development/model-economics.md`](../../development/model-economics.md).
- [`docs/development/design-proportionality.md`](../../development/design-proportionality.md).
- [`docs/planning/README.md`](../README.md), [`status.md`](../status.md), [`reviews/README.md`](../reviews/README.md),
  [`audits/README.md`](../audits/README.md).

## 8. Implementation evidence

**Binding commit(s):**

- `chore: bind lead, design, dev-free, dev, and review agents` — adds
  `.opencode/agents/{lead,design,dev-free,dev,review}.md`, sets `opencode.json` `default_agent` to `lead`,
  updates `.opencode/commands/step-start.md` and `step-closeout.md`, and updates
  `tools/check_process_consistency.py` and `tools/tests/test_opencode_binding.py`. Exact SHA:
  `ea8b4e512f9b7d54fddd7b4b096bb394a92174a7`.
- `chore: remove superseded role bindings` — deletes `.opencode/agents/{orchestrator,planner,implementor,reviewer}.md`
  after the new bindings are in place, and re-runs the policy gate. Exact SHA:
  `38215fb5bc66fd7d8df9712d23a4f78e4e5f05e1`. This is the last binding commit on the branch; the §8 evidence
  update that follows it is documentation-only.

**Local gates:**

| Command | Result |
| --- | --- |
| `python -m pytest tools/tests/test_opencode_binding.py tools/tests/test_process_consistency.py -q` | `14 passed, 11 subtests passed in 0.57s` before removal; re-run after removal: `14 passed, 11 subtests passed in 0.57s` |
| `./tools/verify.sh policy --require` | On the tree after removal: `Process consistency: OK`; `Ran 62 tests ... OK`; `Closeout gate completed with required checks enabled.` |
| `./tools/verify.sh docs --require` | `Build succeeded. 0 warning(s), 0 error(s)`; `Documentation built in /workspace/site` |
| `./tools/verify.sh all --require` | `Process consistency: OK`; `Ran 62 tests ... OK`; `AOT passed`; `Closeout gate completed with required checks enabled.` |

**Restart requirement.** `opencode api get /api/info` reports OpenCode `2.3.2` (pid 13) on the running service.
That service still holds the pre-rename agent list, so `lead`/`design`/`dev-free`/`dev`/`review` take effect
after the owner or a new session restarts or warms the service. This run did not restart it, because a restart
would kill the active session. `opencode.json` now names `lead` as `default_agent`; a restarted service selects
it by default.

**Deviations.** None from §5. The binding test in `tools/tests/test_opencode_binding.py` was updated because it is
the process-check test for this binding, not an unrelated test. `./tools/verify.sh policy --require` also runs the
API and contract checks by design; the process-consistency result is the row that covers §5.9.

## Merge closeout (2026-11-22)

- **Merge commit:** `9bb92e1ec2ec6825a688c4f6e201684ed8815acd` — `docs: adopt standard-profile policy and role
  bindings [skip ci]`. It is a `--no-ff` merge with parents `214b09ac0457e8f2203a80086ed9656e1c46f7cf`
  (`docs: record tabs and scroll gutter merge closeout [skip ci]`, the `main` base) and
  `2d2cf2df272e7b9488b1c163ad0d4f358d988d53` (`docs: record policy-adoption binding evidence [skip ci]`, the tip
  of `docs/policy-adoption`). No commit was added, amended, rebased, or squashed.
- **`[skip ci]` deviation and the owner decision.** The merge commit carried `[skip ci]`, so no automatic `main`
  CI run followed the merge. Rather than accept an untested merge on the strength of the branch-local gates, the
  owner approved one confirmation `workflow_dispatch` run instead of accepting the skip, and it was dispatched
  with `platforms=ubuntu` and `force_all=true`:
  - Run: [`45033099422`](https://github.com/seb-eis/mocassin-2/actions/runs/45033099422), `head_sha`
    `9bb92e1ec2ec6825a688c4f6e201684ed8815acd`, conclusion `success`.
  - Jobs that ran, all green: **Detect changes**; **Rust** (format, clippy, test, release build);
    **.NET (ubuntu-latest)** (restore, build, test with coverage, format); **Contracts and policy** (contract
    drift, space-group catalog drift, SPDX licence headers, secret scan, cargo-deny); **Python (3.13)** (tests
    with coverage, wheel and sdist build); and **Public API compatibility** (restore, build, NuGet licence
    policy, NuGet vulnerability audit, ApiCompat baseline).
  - Skipped by design for an Ubuntu dispatch: the conditional `Platform tests - *` and `Upload test results and
    distributions` steps. Windows and macOS were not run, per the CI cost policy.
- **Model-economics fallback.** This closeout was assigned to `dev-free` first. That binding reported its free
  usage limit before doing any work, so the lead re-spawned the same bounded assignment on the paid `dev`
  binding, as [`docs/development/model-economics.md`](../../development/model-economics.md) requires. The fallback
  is recorded here rather than left implicit.
- **Branch and tag.** `docs/policy-adoption` was deleted locally and on `origin` after the green confirmation run.
  No tag was created: this was a process and tooling change on `main`, not a step or milestone closeout, and
  `step-NN`/`milestone-mN` tags remain reserved for those.
- **Restart note.** The running service still holds the pre-rename agent list (see §8). New sessions select
  `lead` by default through `opencode.json`; existing sessions keep their loaded binding until they restart. This
  run did not restart the service.
