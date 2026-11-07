# Step 02 Amendment 2 Design Review

> Reviewer: write the evidence, findings, and verdict directly into
> `docs/planning/reviews/step-02-amend-2.md`. You may edit only your own metadata (`Reviewer`, `Review date`,
> `Design review status`), the `Evidence`, `Findings`, and `Verdict` sections, and commit only that file with
> a documentation-only message and `[skip ci]`. Do not edit the request scope, checklists, resolutions, code,
> tests, configuration, status dashboard, step page, or any other file.

## Request

| Field | Value |
| --- | --- |
| Step | 02 — Symmetry and Geometry |
| Amendment | `step-02-amend-2` — Documentation Math Rendering with KaTeX |
| Request owner | Step agent |
| Design review status | Ready with notes |
| Reviewed commit | `aad0835be9a0f0534e58a29392b64bf6aac2d588` (`aad0835`) |
| Docs branch | `main` |
| Review date | 2026-11-07 |
| Reviewer | Independent planning auditor |

Parent step page: [`Step 02 — Symmetry and Geometry`](../steps/step-02-symmetry-and-geometry.md)

Amendment plan: [`Step 02 Amendment 2 — Documentation Math Rendering with KaTeX`](../amendments/step-02-amend-2-docs-math.md)

## Step summary

| Field | Value |
| --- | --- |
| Parent phase | A. Foundations |
| Parent depends on | 01 — Domain Kernel and Project Format |
| Amendment delivers | KaTeX math rendering for published documentation and conversion of Step 02 formulas from fenced `text` blocks |

## Design decisions reviewed

- [ ] KaTeX `0.19.1` pinned in `zensical.toml`
- [ ] `extra_javascript` / `extra_css` asset configuration
- [ ] `docs/javascripts/katex.js` initializer with `document$`
- [ ] `pymdownx.arithmatex` already enabled and used correctly
- [ ] Conversion of Step 02 formulas to `$$...$$` / `$...$`
- [ ] Documentation convention against fenced `text` math
- [ ] KaTeX attribution in `NOTICE.md`
- [ ] Strict docs build, asset-reference, navigation, and accessibility checks
- [ ] No code, public API, data-contract, or scientific-behavior changes

## Dependency and interface checks

- [ ] Step 02 documentation is active and not yet audit-frozen
- [ ] Zensical `0.0.74` and `pymdownx.arithmatex` are available
- [ ] The KaTeX asset source and version are pinned and reproducible
- [ ] No unresolved docs-toolchain or offline/privacy question remains

## Standard design-review checklist

- [ ] Scope, non-goals, and acceptance criteria are explicit and testable
- [ ] Design decisions are complete and internally consistent
- [ ] Required tests, property tests, golden fixtures, and performance targets are defined
- [ ] Documentation, status, parity, and audit updates are planned
- [ ] Licensing, provenance, and dependency checks are resolved
- [ ] Risks and mitigations are realistic
- [ ] No hidden scope, stale placeholders, or unowned decisions

## Evidence

Files reviewed:

- `AGENTS.md`
- `docs/planning/amendments/README.md`
- `docs/planning/amendments/step-02-amend-2-docs-math.md`
- `docs/planning/steps/step-02-symmetry-and-geometry.md`
- `docs/planning/03-quality-strategy.md`
- `docs/planning/05-open-decisions.md`
- `docs/development/agent-workflow.md`
- `docs/development/contributing.md`
- `docs/physics/symmetry.md`
- `docs/development/compilation-pipeline.md`
- `docs/reference/file-layout.md`
- `docs/planning/reviews/step-02.md`
- `docs/planning/audits/step-02.md`
- `zensical.toml`
- `NOTICE.md`
- `tools/docs/requirements.txt`

Commands run:

```text
git status --short
git log --oneline -8 main
git show main:docs/planning/amendments/step-02-amend-2-docs-math.md
git diff main HEAD -- docs/planning/amendments
git diff main HEAD -- zensical.toml
grep -n "^```text" docs/physics/symmetry.md
zensical build --strict
curl -sS -o /dev/null -w "%{http_code}\n" https://unpkg.com/katex@0.19.1/dist/katex.min.js
```

Results: the working tree was clean; the two review records are on `main` (`ce6a563`) and absent
from the closeout branch. `zensical build --strict` failed with one issue:
`docs/development/agent-workflow.md:308` links to `../planning/amendments/step-amendment-template.md`,
which `exclude_docs` removes. `docs/physics/symmetry.md` has six fenced `text` blocks at lines 31, 50,
74, 83, 109, and 149. The pinned KaTeX asset URL returned `200`. The Zensical 0.0.74 bundle exposes
`document$` as the `DOMContentLoaded`/instant-navigation observable used by the official KaTeX example.

A scratch checkout under `/tmp/opencode` (not committed) applied the plan's `zensical.toml` keys,
`docs/javascripts/katex.js`, and all six formula conversions, and replaced the broken planning link with the
GitHub URL already used for the other planning links. `zensical build --strict` then reported
`No issues found`; the built symmetry page referenced `katex.min.css`, `katex.min.js`, and
`javascripts/katex.js`, and contained six `class="arithmatex"` blocks.

## Known notes for the reviewer

- The amendment is **planned, not implemented**. The Step 02 implementor is active on the closeout feature.
- The reviewed design commit is `bd48763`. This review record is metadata added after that commit and does not
  change the design.
- Zensical already has `pymdownx.arithmatex.generic = true`; the amendment supplies the missing KaTeX assets
  and initializer.
- The plan uses pinned CDN assets (`0.19.1`) and documents a vendored-assets fallback for offline or privacy
  requirements. Check whether the CDN choice is acceptable for this project.
- Check the `document$` initializer against Zensical instant navigation, the four delimiters, and the
  `renderMathInElement` call.
- Check the formula conversions for KaTeX-supported syntax, especially `\text`, `\begin{aligned}`, roots,
  fractions, and subscripts.
- Check that the amendment does not convert structural `text` blocks such as file trees or command
  transcripts.
- Check that no code, public API, data contract, or scientific behavior changes.
- The amendment must be integrated before the Step 02 audit freeze; the parent audit then covers the combined
  solution.

## Findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| **Major** | **A2-1 — The strict docs build is already red, so the primary acceptance criterion cannot pass as written.** `zensical build --strict` fails with one issue: `docs/development/agent-workflow.md:308` links to `../planning/amendments/step-amendment-template.md`, which `exclude_docs` removes. The plan requires the strict build to succeed and the formulas to be converted, but its scope does not include fixing this link. | `docs/planning/amendments/step-02-amend-2-docs-math.md:38-43, 197, 207-214`; command output `Warning: page does not exist [development/agent-workflow.md:308:60] ... 1 issue found`; `zensical.toml` has `exclude_docs = "planning/"`. A scratch checkout with the link replaced by the GitHub URL and the plan applied produced `No issues found` and six `arithmatex` blocks. Required: add the one-line link fix to the amendment scope or resolve it separately before review. |
| **Minor** | **A2-2 — The TOML snippet repeats `[project]`.** The existing `zensical.toml` already has a `[project]` table at line 7; appending the plan’s snippet literally would create a duplicate table and invalid TOML. | `docs/planning/amendments/step-02-amend-2-docs-math.md:60-68`; `zensical.toml:7`. Required: say “add these keys under the existing `[project]` table” or show only the keys. |
| **Minor** | **A2-3 — Parent review/audit links and branch state are not handled.** The amendment process requires the parent review record to link to the amendment review and the parent audit request to list amendment IDs; the Documentation checklist says only “Amendment page and index updated” and Integration says “Parent audit request updated: —”. The closeout branch still shows the amendment as `Planned` and lacks the review records, which are on `main`. | `docs/planning/amendments/README.md:69-87`; `docs/planning/amendments/step-02-amend-2-docs-math.md:216-223, 241-248`; `docs/planning/reviews/step-02.md` has no amendment link; `docs/planning/audits/step-02.md` has no amendment list; `git diff main HEAD` shows closeout status `Planned` versus main `Design review`. Required: add both links/updates and synchronize the parent step branch before implementation. |
| **Note** | **A2-4 — CDN, delimiter, and manual-check risks are documented but not fully bounded.** The plan pins `0.19.1` and documents a vendored fallback, but leaves the CDN decision open; the global `$...$` delimiter can render prose or currency pairs; the navigation, accessibility, and visual checks are manual with no concrete evidence format. | `docs/planning/amendments/step-02-amend-2-docs-math.md:55-75, 98-104, 205-214, 225-233`; the official Zensical KaTeX example matches the initializer; the scratch build produced six `arithmatex` blocks. Required: record the CDN/vendored choice in `NOTICE.md`, consider limiting `$...$` to prose that needs it or documenting the risk, and specify the exact page and expected rendered elements for the manual checks. |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. The planning auditor writes the findings and
verdict directly. The step agent adds resolutions in the separate `Resolution status` section without altering
the auditor's findings, evidence, or verdict.

## Verdict

- [ ] Ready for implementation
- [ ] Ready with notes
- [x] Changes required
- [ ] Re-review required
- [ ] Waived

**Changes required.** One major finding remains: the strict docs build is already red because
`docs/development/agent-workflow.md:308` links into the excluded planning tree, so the amendment’s primary
acceptance criterion cannot pass until that link is fixed (A2-1). The duplicate `[project]` snippet and the
missing parent review/audit links are minor corrections; the CDN, delimiter, and manual-check items are notes.
The KaTeX design itself is sound and was verified in a scratch checkout.

Follow-up owner: Planner / step agent

Follow-up step: Add the `agent-workflow.md` link fix to the amendment scope (use the GitHub URL form already
used for the other planning links), fix the duplicate `[project]` wording, add the parent review/audit links,
record the CDN/vendored decision and the manual visual/navigation evidence, synchronize the parent step branch,
and request a fresh re-review.

## Resolution status

| Finding | Status | Owner | Commit |
| --- | --- | --- | --- |
| A2-1 | Fixed — `agent-workflow.md` amendment-template link now uses GitHub URL and is in scope | Planner | `aad0835` |
| A2-2 | Fixed — TOML snippet now says to add keys under the existing `[project]` table | Planner | `aad0835` |
| A2-3 | Fixed — parent review/audit links and branch-sync prerequisite added | Planner | `aad0835` |
| A2-4 | Fixed — pinned CDN choice, `$` delimiter disabled, manual evidence specified | Planner | `aad0835` |

The step agent fills this section after the verdict. Do not alter the reviewer-authored findings, evidence, or
verdict.

## Re-review round 2

### Re-review request

| Field | Value |
| --- | --- |
| Amendment | `step-02-amend-2` — Documentation Math Rendering with KaTeX |
| Request owner | Step agent |
| Design review status | Ready with notes |
| Reviewed commit | `aad0835be9a0f0534e58a29392b64bf6aac2d588` (`aad0835`) |
| Delta reviewed | `bd48763..aad0835` |
| Docs branch | `main` |
| Review date | 2026-11-07 |
| Reviewer | Independent planning auditor (fresh re-review) |

### Round-1 finding verification

| Finding | Status | Verification |
| --- | --- | --- |
| **A2-1** — strict docs build red | **Fixed** | `docs/development/agent-workflow.md:308` now uses the GitHub URL for the amendment template. `zensical build --strict` at `aad0835` reports `No issues found`. |
| **A2-2** — duplicate `[project]` | **Fixed** | `docs/planning/amendments/step-02-amend-2-docs-math.md:59-68` now says to add the keys under the existing `[project]` table and does not repeat the table header. |
| **A2-3** — parent review/audit links and branch sync | **Fixed** | `docs/planning/reviews/step-02.md:34-41` links both amendment reviews; `docs/planning/audits/step-02.md:17-27` lists both; the plan's Integration records the parent branch-sync and review-link prerequisites. The index, plan pages, and audit status are aligned at `Re-review required` in `01a485d`. |
| **A2-4** — CDN, delimiter, and manual evidence | **Fixed** | The plan records the pinned CDN source for `NOTICE.md`, removes the `$` delimiter in favour of `\(...\)`, adds a delimiter-scope test, and names `<site>/physics/symmetry/index.html` for the manual visual check. |

### Re-review evidence

Files reviewed:

- `docs/planning/amendments/step-02-amend-2-docs-math.md`
- `docs/planning/steps/step-02-symmetry-and-geometry.md`
- `docs/development/agent-workflow.md`
- `docs/development/contributing.md`
- `docs/physics/symmetry.md`
- `docs/planning/reviews/step-02.md`
- `docs/planning/audits/step-02.md`
- `docs/planning/amendments/README.md`
- `zensical.toml`
- `NOTICE.md`
- `tools/docs/requirements.txt`

Commands run:

```text
git diff --stat bd48763 aad0835
git diff bd48763 aad0835 -- docs/planning/amendments/step-02-amend-2-docs-math.md \
  docs/development/agent-workflow.md docs/planning/reviews/step-02.md docs/planning/audits/step-02.md
git show 01a485d:docs/planning/reviews/step-02-amend-2.md
git show aad0835:zensical.toml
grep -n "^```text" docs/physics/symmetry.md
git worktree add --detach /tmp/opencode/mocassin-rereview aad0835
zensical build --strict
```

Results: the design delta is documentation-only; `docs/physics/symmetry.md` still has six fenced `text`
formula blocks because the amendment is not implemented yet. `zensical build --strict` at `aad0835` reported
`No issues found`; the pinned KaTeX 0.19.1 asset URL remains reachable. The plan is internally consistent on
the `[project]` keys and the three configured delimiters.

### Re-review findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| **Minor** | **A2-5 — The manual visual check has no evidence slot.** The plan asks the implementor to record the command and a screenshot path in the amendment page, but the amendment page has no evidence/verification section. Add a short verification section or record the evidence in the amendment page's integration/audit section. | `step-02-amend-2-docs-math.md:205-214, 216-229`; the amendment page currently ends with its Audit section. |
| **Note** | **A2-6 — “Matching the official Zensical KaTeX example” is slightly inaccurate.** The plan intentionally removes the single-dollar delimiter, while the official example includes it. State that the initializer matches the example except for the removed `$` delimiter. | `step-02-amend-2-docs-math.md:79-105`; the official Zensical KaTeX example includes `{ left: "$", right: "$", display: false }`. |

### Re-review verdict

- [ ] Ready for implementation
- [x] Ready with notes
- [ ] Changes required
- [ ] Re-review required
- [ ] Waived

**Ready with notes.** The four round-1 findings are fixed: the strict docs build is green at `aad0835`, the
TOML wording is correct, the parent review/audit links and statuses are in place, and the CDN/single-dollar/
manual-evidence decisions are recorded. The remaining items are one minor evidence-slot addition and one wording
clarification; no blocker or major finding remains.

Follow-up owner: Planner / implementor

Follow-up step: Implementation may proceed after the human owner accepts these notes; add the manual visual
evidence slot and clarify the “matching the official example” wording.
