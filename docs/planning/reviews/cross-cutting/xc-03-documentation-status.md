# XC-03 — Documentation Status Review

> Reviewer: write evidence, findings, and verdict directly into this record. You may edit only your own
> metadata, checklist completion marks and inline verification notes (not item text), `Evidence`, `Findings`,
> and `Verdict`, and commit only this file with `[skip ci]`. Do not edit the scope, checklist item text,
> resolutions, code, tests, configuration, step pages, status dashboard, or any other file.

## Request

| Field | Value |
| --- | --- |
| Review ID | XC-03 |
| Checkpoint | Phase B — Model definition (Steps 03–07) |
| Type | Repository-wide documentation status review |
| Target commit | After the Step 07 merge and `step-07` tag, plus a docs-only checkpoint commit |
| Request owner | Step agent / planner |
| Orchestrator (implementor) | — |
| Review initiated by | Human owner |
| Reviewer | — |
| Review date | — |

> Orchestration: an implementor agent requests the reviewer, fixes findings on a maintenance branch, adds
> resolutions without altering reviewer text, and requests re-review until the reviewer no longer identifies
> issues. The human owner approves the final verdict and the phase gate.

## Purpose

Verify that the published and developer documentation is complete, accurate, navigable, and consistent with
the completed phase. For the Phase B checkpoint, this covers the implemented Steps 00–07.

This is a documentation review, not a repository-policy audit or a design-correctness review.

## Non-goals

- Re-reviewing repository policy or planning records (XC-01).
- Re-reviewing the scientific/design correctness of the model (XC-02).
- Writing new pages or fixing stale text; the reviewer reports only.
- Auditing future Steps 08–31 pages that are explicitly placeholders.

## Scope

- Published docs: `docs/index.md`, `getting-started/`, `user-guide/`, `physics/`, `tutorials/`, `reference/`,
  `development/`, `performance/`, `troubleshooting/`, `migration/`, `examples/`, `glossary/`, `api/`.
- `zensical.toml` navigation, exclude rules, and docs build configuration.
- `tools/docs/` build scripts and requirements.
- Generated API documentation: DocFX, rustdoc, and mkdocstrings landing pages.
- Step pages 00–07 **Documentation** sections and delivered pages.
- Developer docs that are part of the published site: architecture, compilation pipeline, testing, agent
  workflow, contributing.
- Diagrams, images, screenshots, KaTeX assets, and external links.
- The documentation status reflected in the status dashboard and step pages.

## Evidence

Files reviewed:

Commands run:

The reviewer should run:

```sh
git status --short
zensical build --strict
./tools/docs/build-docs.sh
```

Use a link/anchor checker for local Markdown, inspect the generated `site/`, and grep for placeholders
(`TODO`, “not implemented”, “coming soon”, old step numbers, old commands, old paths). Check the published
site navigation and the excluded `docs/planning/` boundary.

## Checklist

### A. Coverage

- [ ] Every implemented feature in Steps 00–07 has a published or developer page.
- [ ] The user guide covers particles, particle sets, structure, transitions, energy, lattice/doping, and
  simulations at the implemented level.
- [ ] Physics pages match the implemented symmetry, transition, energy, and simulation semantics.
- [ ] Reference pages match the current project format and schema.
- [ ] Getting-started and tutorial pages work for the implemented surface.
- [ ] Developer docs cover architecture, project format, testing, compilation boundary, and agent workflow.
- [ ] No page claims a future feature is implemented.

### B. Accuracy

- [ ] Commands, paths, project names, versions, and settings match the current repository.
- [ ] JSON examples match the generated schema and contract field names.
- [ ] Formulas, units, notation, and diagnostic codes are consistent with the model pages.
- [ ] Screenshots and diagrams are current; no stale UI or old workflow.
- [ ] Cross-links and anchors resolve.
- [ ] No stale “Step 01/02” status, old CI matrix, or old package/namespace names remain in published pages.

### C. Navigation and structure

- [ ] `zensical.toml` navigation covers all published pages; no orphan pages.
- [ ] `docs/planning/` remains excluded from the published site.
- [ ] Heading hierarchy, code blocks, tables, and callouts are consistent.
- [ ] Images have alt text; dark/light theme and mobile layout are acceptable.
- [ ] Search index includes the published pages.
- [ ] External links are appropriate; runtime CDN dependencies are identified.

### D. API documentation

- [ ] DocFX builds with no missing public API docs.
- [ ] rustdoc builds with no missing public docs/warnings.
- [ ] mkdocstrings renders the Python API correctly.
- [ ] API landing pages link to the correct generated paths.
- [ ] Public API examples and XML docs are current.

### E. Build and publication

- [ ] `zensical build --strict` passes.
- [ ] Combined docs build (Zensical + DocFX + rustdoc) passes.
- [ ] Licence/attribution and secret scans are clean.
- [ ] The manual docs workflow or local build is documented and current.
- [ ] Packaged/offline docs assumptions are recorded, including KaTeX/CDN assets.

### F. Documentation status and exit criteria

- [ ] Step pages 00–07 document their delivered pages and mark “Documentation written”.
- [ ] Status dashboard and parity matrix documentation entries are current.
- [ ] Known documentation gaps, placeholders, and follow-ups have owners and target steps.
- [ ] No Blocker or Major finding remains, or every exception is explicitly waived by the human owner.
- [ ] Minor/Note findings have owners and follow-ups.
- [ ] The human owner approves the verdict and remediation plan.

## Findings

| ID | Severity | Area | Finding | Evidence | Recommendation |
| --- | --- | --- | --- | --- | --- |
| — | — | — | — | — | — |

## Record state

**State:** Planned — awaiting Step 07 completion

`Re-review required` is a record state, not a verdict.

## Reviewer independence

> I am a fresh context. I did not author or implement the reviewed work and have no shared conversation with the
> request owner or the other reviewers.

## Verdict

- [ ] Passed
- [ ] Passed with notes
- [ ] Fixes required
- [ ] Waived

Follow-up owner:
Follow-up step:

## Remediation status

| Finding | Status | Owner | Commit |
| --- | --- | --- | --- |
| — | — | — | — |

## Human approval

- [ ] Accepted: Passed
- [ ] Accepted: Passed with notes
- [ ] Fixes required
- [ ] Waived

Approved by:
Date:
