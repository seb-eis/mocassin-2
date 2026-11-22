# GUI Icon Pass Design Review

> Review agent: write the evidence, findings, and verdict directly into this record. You may edit only your
> own metadata (`Reviewer`, `Review date`, `Record state`), the checklist completion marks and inline
> verification notes (not item text), the `Evidence`, `Findings`, and `Verdict` sections, and commit only this
> file with a documentation-only message and `[skip ci]`. Do not edit the request scope, checklist item text,
> resolutions, code, tests, configuration, the maintenance record, the visual contract, or any other file.

## Request

| Field | Value |
| --- | --- |
| Review ID | `MAINT-2026-11-22-GUI-ICONS` |
| Type | Out-of-step maintenance design review (not a roadmap step) |
| Target commit | `34e7c18d71ee29ab5d81885ba13bb761758f1993` |
| Docs branch | `fix/gui-icon-pass` |
| Request owner | Design agent (the dev owns resolutions after the verdict) |
| Lead | — |
| Dev | — (not yet started; the fix is a bounded maintenance branch) |
| Review initiated by | Lead, on request of the design agent |
| Design class | **Light** |
| Request date | 2026-11-22 |
| Review date | — |
| Reviewer | — |

Maintenance record:
[`2026-11-22-gui-icon-pass.md`](../maintenance/2026-11-22-gui-icon-pass.md)

Normative contract:
[`docs/development/ui-visual-contract.md`](../../development/ui-visual-contract.md) §2, §5.2, §7.4, §7.6,
§8, §11.3, §12, §13

> This is an out-of-step maintenance design review under `AGENTS.md` §3.5, not a roadmap-step design review.
> The file uses the `step-` prefix so the reviewer harness can edit it. The reviewed design is the visual
> contract icon delta and the maintenance record at the target commit. No implementation code exists yet.
>
> Design depth: this is a **Light** review. Answer the five design questions below; Minor and Note findings are
> accepted and do not trigger a re-review. If you find a Blocker or Major, the step was misclassified — record
> it and the class moves to Full. The licensing/provenance item is included because the change vendors
> third-party assets.

## Scope

The review covers the design delta only:

- the icon section added to the visual contract (§2 token home, §5.2 geometry/slots/keys/colours/missing
  marker/coverage/provenance, §7.4 populated tab slot, §7.6 tree and project-row slots, §8 collapsed console
  slot, §11.3 decorative-slot names, §12 provenance boundary, §13 verification rows);
- the maintenance record's owner request and fixed decisions, the recorded projects-list interpretation, the
  design class and justification, the icon mapping, the colour and state rules, the vendoring pipeline
  (source pin, subset, checksums, files, licence header, NOTICE entry), the expected files, the test plan,
  the documentation plan, the acceptance criteria, the risks, and the in-scope/deferred list;
- the owner decisions C1, L1, and S1 recorded in the maintenance record.

Non-goals of the design review:

- implementation code, tests, configuration, or `contracts/manifest.json` (none exists yet);
- the UI interaction contract, control templates, a theme redesign, or new colour keys;
- icons for console tabs, the welcome recent-project list, settings, dialogs, or notifications;
- the parked `fix/gui-palette-freeze` branch, frozen records, completed step pages, and the `main` history;
- step status, parity, audit, or tag changes.

## Design questions (Light depth)

- [ ] **Contracts.** The only normative contract touched is the visual contract's icon delta; the icon keys,
      resources, mapping, colour roles, and missing-asset behaviour are resolved and consistent with §3, §7.4,
      and §11. There is no format, schema, version, public-API, or interaction-contract change, and no
      migration.
- [ ] **Testability.** Every acceptance criterion in the maintenance record §11 names the test, command, or
      fixture that proves it (maintenance record §9), including the provenance/licence checks and the
      screenshot coverage for every shipped icon surface.
- [ ] **Non-goals and irreversibility.** The non-goals are explicit; no decision is irreversible (the vendored
      files are removable; no ADR is claimed; the recorded alternative glyphs and the projects-list strike
      option are owner-vetoable without process change).
- [ ] **Boundary fit.** The change fits one branch and one boundary: internal view models plus
      `Mocassin.App.Views` resources/styles; no control templates (S1), no new dependency (vendored MIT
      assets), no `tools/` generator, and the expected-files list is complete for that boundary.
- [ ] **Risks.** The risks are real and owned: the selected-state fill delivery (probe-first with a stop
      rule), the arc-geometry parsing check, the owner-vetoable glyph choices, the projects-list
      interpretation, and the Full-trigger reclassification rule if a vendoring generator becomes necessary.
- [ ] **Licensing and provenance.** Phosphor Icons is verified MIT (© 2023) at the pinned commit; the
      vendoring follows the ADR 0007 third-party pattern and the spglib catalog precedent (SPDX, pin,
      checksums, licence copy, NOTICE entry); no GPL content is involved; no icon font, package, or network
      access is introduced.

## Dependency and interface checks

- [ ] The base is current `main` at `2d9d5f487706ba946d6a027a1d668677b2535d19` and the branch is based on it.
- [ ] The collapsed `TabIconSlot` contract (DocumentsRegionView/ConsoleView, `DocumentViewModel.Icon`,
      `ConsoleChannelViewModel.Icon`) is as the maintenance record §3 describes, and the design populates it
      without reworking the template.
- [ ] The `ExplorerNode.IconKey` values already set by `ProjectExplorerNodeProvider` match the record's
      mapping keys.
- [ ] No unresolved cross-project, cross-language, format, or public-API interface question remains.

## Evidence

Files reviewed (by the request owner at the target commit):

- `docs/development/ui-visual-contract.md` (§2, §5.2, §7.4, §7.6, §8, §11.3, §12, §13 delta)
- `docs/planning/maintenance/2026-11-22-gui-icon-pass.md`
- `docs/planning/maintenance/2026-11-22-gui-tabs-scroll-gutters.md` (the slot reservation this pass populates)
- `docs/development/design-proportionality.md` (class and review depth)
- `AGENTS.md` §3.5, §3.8–§3.12, §13, §15; `NOTICE.md`; `docs/planning/adr/0007-mit-license.md`
- `src/dotnet/Mocassin.App.Views/Shell/DocumentsRegionView.axaml`,
  `src/dotnet/Mocassin.App.Views/Shell/ProjectsRegionView.axaml`,
  `src/dotnet/Mocassin.App.ViewModels/Documents/DocumentViewModel.cs`,
  `src/dotnet/Mocassin.App.ViewModels/Documents/DocumentKinds.cs`,
  `src/dotnet/Mocassin.App.ViewModels/Navigation/ExplorerNode.cs`,
  `src/dotnet/Mocassin.App.ViewModels/Navigation/ProjectExplorerNodeProvider.cs`,
  `src/dotnet/Mocassin.App.ViewModels/Consoles/ConsoleChannelViewModel.cs`
- Upstream verification: `https://github.com/phosphor-icons/core` at commit
  `2b75f3ad12b420c9504ef05df8d2564a28f8500e` — repo licence MIT, `package.json` version 2.1.1, and the five
  subset SVGs (`assets/regular/{info,brackets-curly,hand-waving,cube,warning-circle}.svg`) plus `LICENSE`
  fetched and hashed; the checksums are in the maintenance record §7.

Commands run by the request owner on the design-freeze state (`34e7c18`):

- `./tools/verify.sh docs` — exit 0 (strict Zensical build, link and math checks, DocFX, rustdoc).
- `./tools/verify.sh policy` — exit 0 (ApiCompat, contract/symmetry drift, process consistency, 62 policy
  tests, SPDX headers in 1107 source files, secret scan, cargo-deny).

## Findings

| Severity | Origin | Finding | Evidence |
| --- | --- | --- | --- |
| — | — | — | — |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. Origin: **design**, **implementation**,
**evidence**, or **documentation**. The review agent writes the findings and verdict directly. The request
owner adds resolutions in the separate `Resolution status` section without altering the review agent's
findings, evidence, or verdict.

## Record state

**State:** Ready for review

## Reviewer independence

> I am a fresh context. I did not author or implement the reviewed work and have no shared conversation with
> the design agent, dev, or lead.

## Verdict

- [ ] Ready for implementation
- [ ] Ready with notes
- [ ] Changes required
- [ ] Waived

Follow-up owner:
Follow-up step:

## Approval summary

The request owner completes this section before the lead presents a merge, tag, or Blocker/Major waiver
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

The dev fills this section after the verdict. Do not alter the review agent-authored findings, evidence, or
verdict.

## Review rounds

Append each new round below. Never overwrite an earlier round.

### Review round 1

#### Independence and frozen state

#### Commands and results

#### Findings

#### Verdict
