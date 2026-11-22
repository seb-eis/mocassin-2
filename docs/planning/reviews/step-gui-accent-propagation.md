# GUI Accent Propagation Design Review

> Reviewer: write the evidence, findings, and verdict directly into this record. You may edit only your own
> metadata (`Reviewer`, `Review date`, `Record state`), the checklist completion marks and inline verification
> notes (not item text), the `Evidence`, `Findings`, and `Verdict` sections, and commit only this file with a
> documentation-only message and `[skip ci]`. Do not edit the request scope, checklist item text, resolutions,
> code, tests, configuration, the maintenance record, or any other file.

## Request

| Field | Value |
| --- | --- |
| Review ID | `MAINT-2026-11-22-GUI-ACCENT` |
| Type | Out-of-step maintenance design review (not a roadmap step) |
| Target commit | `6ed4858f562ec9f29e1042b6ae4294bd3cac6fef` |
| Docs branch | `fix/gui-accent-propagation` |
| Request owner | Planner |
| Orchestrator | — |
| Step agent (implementor) | — (no step; the fix is a bounded maintenance branch) |
| Review initiated by | Orchestrator, on request of the planner |
| Review date | — |
| Reviewer | — |

Maintenance record:
[`2026-11-22-gui-accent-propagation.md`](../maintenance/2026-11-22-gui-accent-propagation.md)

Normative contract:
[`docs/development/ui-visual-contract.md`](../../development/ui-visual-contract.md) §3.6

> This is an out-of-step maintenance design review under `AGENTS.md` §3.5, not a roadmap-step design review.
> The file uses the `step-*` prefix so the reviewer harness can edit it. The reviewed design is the visual
> contract and the maintenance record at the target commit.

## Scope

The review covers the design delta only:

- the accent propagation model in visual contract §2 and §3.2–§3.6;
- the status bar, focus, selected-control, tab-pipe, and contrast rules in §6, §7.1, §7.3, §7.4, §11.1, and
  §13;
- the maintenance record's owner report, verified fault line, full accent-resource inventory, fix shape,
  expected overlay set, test plan, documentation plan, acceptance criteria, risks, and in-scope/deferred list;
- the owner decisions A1–A3 recorded in the maintenance record.

Non-goals of the design review:

- implementation code, tests, configuration, or `contracts/manifest.json`;
- the UI interaction contract, control templates, a theme redesign, or new accent keys;
- the parked `fix/gui-palette-freeze` branch or its record;
- step status, parity, audit, or tag changes.

## Design decisions reviewed

- [ ] Accent overlay completeness and precedence (default/`blue`, optional accents, high contrast)
- [ ] `OnAccent`, `AccentText`, `FocusAccent`, `SelectedFill`, and `SelectedText` derivation
- [ ] `Mocassin.FocusBrush` as the contrast-safe focus/indicator role
- [ ] Status-bar background/foreground and progress contrast
- [ ] Selected list/tree/combo and text-selection treatment
- [ ] Default restoration and live reapply
- [ ] Test plan, contrast matrix, and screenshot scope
- [ ] Documentation plan and acceptance criteria
- [ ] Owner decisions A1–A3 and the superseded refresh wording
- [ ] No ADR, interaction-contract, control-template, dependency, or format change

## Dependency and interface checks

- [ ] The base is current `main` at `19cb6ffe190e02449ce8b827706f365f2ea7afe2`
- [ ] The design uses only existing accent keys and resources, plus the new derived `Mocassin.FocusBrush`
- [ ] The overlay/removal interface with `AvaloniaThemeApplier` is defined
- [ ] High-contrast and density precedence remain authoritative
- [ ] No unresolved cross-language, format, or public-API interface question remains

## Standard design-review checklist

- [ ] Scope, non-goals, and acceptance criteria are explicit and testable
- [ ] The change is bounded to one maintenance branch and one reviewable diff; no step sizing applies
- [ ] Design decisions are complete and internally consistent
- [ ] Required tests, contrast checks, and rendered-surface checks are defined
- [ ] Documentation, evidence, and owner-approval updates are planned
- [ ] Licensing, provenance, and dependency checks are resolved (no new dependency)
- [ ] Risks and mitigations are realistic
- [ ] Code layout and namespace plan follow the structure policy (no new source area)
- [ ] No hidden scope, stale placeholders, or unowned decisions

## Evidence

Files reviewed:

- `docs/development/ui-visual-contract.md`
- `docs/planning/maintenance/2026-11-22-gui-accent-propagation.md`
- `src/dotnet/Mocassin.App.Views/Theming/AvaloniaThemeApplier.cs`
- `src/dotnet/Mocassin.App.Views/Themes/Palette.axaml`
- `src/dotnet/Mocassin.App.Views/Themes/HighContrast.axaml`
- `src/dotnet/Mocassin.App.Views/Styles/ShellStyles.axaml`
- `src/dotnet/Mocassin.App.Views/Shell/ShellView.axaml`
- `src/dotnet/Mocassin.App.Views/Shell/StatusBarView.axaml`
- `src/dotnet/Mocassin.App.ViewModels/Settings/AccentPalette.cs`
- `src/dotnet/Mocassin.App.Tests/Settings/ThemeApplierTests.cs`
- `src/dotnet/Mocassin.App.Tests/Visual/ThemeContrastTests.cs`

Commands run by the request owner:

- `git rev-parse main` — `19cb6ffe190e02449ce8b827706f365f2ea7afe2`
- `./tools/verify.sh docs` — exit 0. Strict Zensical build passed; DocFX built 465 models with 0 warnings and
  0 errors; rustdoc built the workspace.
- `./tools/verify.sh policy` — exit 0. ApiCompat found no breaking changes; contract artifacts current;
  space-group catalog current; process consistency OK; 60 policy tests passed; SPDX, secret scan, and
  `cargo-deny` clean.
- `python tools/check_docs_links.py` — `Documentation links OK (204 files scanned)`.
- `python tools/check_doc_math.py` — `Documentation math conventions OK (201 files scanned)`.

## Findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| — | — | — |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. The planning auditor writes the findings and
verdict directly. The request owner adds resolutions in the separate `Resolution status` section without
altering the auditor's findings, evidence, or verdict.

## Record state

**State:** Ready for review

`Re-review required` is a record state, not a verdict.

## Reviewer independence

> Reviewer: state here that you are a fresh context. You did not author or implement the reviewed work and have
> no shared conversation with the planner, implementor, or orchestrator.

## Verdict

- [ ] Ready for implementation
- [ ] Ready with notes
- [ ] Changes required
- [ ] Waived

Follow-up owner:
Follow-up step: —

## Approval summary

The request owner completes this section before the orchestrator presents a merge, tag, or Blocker/Major waiver
request. It is not required for the design-review verdict itself.

- [ ] Requested decision and frozen commit recorded
- [ ] Every review/audit round recorded (round, date, commit, reviewer, verdict)
- [ ] Every finding recorded (ID, severity, issue, evidence, resolution, fix commit, status)
- [ ] Accepted notes and waivers recorded with rationale
- [ ] Residual risks, deferred items, and follow-up owners recorded
- [ ] Explicit no-Blocker/no-Major statement, or the exact exceptions being waived
- [ ] Final CI and documentation-build results recorded

Round history:

| Round | Date | Commit | Reviewer | Verdict |
| --- | --- | --- | --- | --- |
| — | — | — | — | — |

Findings and resolutions:

| Finding | Severity | Issue | Resolution | Commit | Status |
| --- | --- | --- | --- | --- | --- |
| — | — | — | — | — | — |

## Human approval

- [ ] Accepted: Ready for implementation
- [ ] Accepted: Ready with notes
- [ ] Changes required
- [ ] Waived

Approved by:
Date:

## Resolution status

| Finding | Status | Owner | Commit |
| --- | --- | --- | --- |
| — | — | — | — |

The request owner fills this section after the verdict. Do not alter the reviewer-authored findings, evidence, or
verdict.

## Review rounds

No review round has been recorded yet. Append each new round below and never overwrite an earlier round.

### Review round 1

#### Independence and frozen state

#### Commands and results

#### Findings

#### Verdict
