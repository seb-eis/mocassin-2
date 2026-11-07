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
| Design review status | Ready for review |
| Reviewed commit | `bd4876314ed962da64c49ea747d7e3e4ed2e7552` (`bd48763`) |
| Docs branch | `main` |
| Review date | 2026-11-07 |
| Reviewer | — |

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
- `zensical.toml`
- `NOTICE.md`
- `tools/docs/requirements.txt`

Commands run:

```text

```

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
| — | — | — |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. The planning auditor writes the findings and
verdict directly. The step agent adds resolutions in the separate `Resolution status` section without altering
the auditor's findings, evidence, or verdict.

## Verdict

- [ ] Ready for implementation
- [ ] Ready with notes
- [ ] Changes required
- [ ] Re-review required
- [ ] Waived

Follow-up owner:
Follow-up step:

## Resolution status

| Finding | Status | Owner | Commit |
| --- | --- | --- | --- |
| — | — | — | — |

The step agent fills this section after the verdict. Do not alter the reviewer-authored findings, evidence, or
verdict.
