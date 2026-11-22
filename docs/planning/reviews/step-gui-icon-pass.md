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
| Review date | 2026-11-22 |
| Reviewer | Round 1: GLM-5.3 (opencode-go), a fresh context with no shared conversation with the design agent, dev, or lead |

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

- [x] **Contracts.** The only normative contract touched is the visual contract's icon delta; the icon keys,
      resources, mapping, colour roles, and missing-asset behaviour are resolved and consistent with §3, §7.4,
      and §11. There is no format, schema, version, public-API, or interaction-contract change, and no
      migration. — Verified: the freeze commit `34e7c18` touches only `ui-visual-contract.md` (eight hunks
      confined to §2, §5.2, §7.4, §7.6, §8, §11.3, §12, §13) and the maintenance record; no
      `contracts/manifest.json`, format, schema, or version change; the touched types are internal
      (`DocumentViewModel` is `internal abstract partial`, `ExplorerNode` an internal record) and no
      `PublicAPI.*` entry exists for icons; the mapping is coherent with the §3 roles (no icon colour keys),
      the reserved §7.4 slot, and the §11 decorative-slot rules.
- [x] **Testability.** Every acceptance criterion in the maintenance record §11 names the test, command, or
      fixture that proves it (maintenance record §9), including the provenance/licence checks and the
      screenshot coverage for every shipped icon surface. — Verified: AC1→§9.1, AC2→§9.2, AC3→§9.3+§9.5,
      AC4→§9.4, AC5→§9.8 plus the §7 vendoring re-hash with its stop rule, AC6→§9.5+§9.6, AC7→§9.9+§9.10,
      AC8→the named `verify.sh` commands, AC9→§16 discipline. The named anchors exist today
      (`AccessibilityTests.cs:338`, `ConsoleTabsTests.cs:60`, `ThemeContrastTests.cs`,
      `ScreenshotTests.cs:28` with the regeneration switch). n-4 records the checksum-value half of AC5.
- [x] **Non-goals and irreversibility.** The non-goals are explicit; no decision is irreversible (the vendored
      files are removable; no ADR is claimed; the recorded alternative glyphs and the projects-list strike
      option are owner-vetoable without process change). — Verified: the record's non-goals and §14's
      deferred list are explicit and match §5.2's coverage; nothing irreversible — the vendored files are
      removable, ADR 0007's third-party pattern and the spglib precedent cover the licensing without a new
      ADR, and the glyph alternatives and the strike option are single-binding owner-vetoable changes.
- [x] **Boundary fit.** The change fits one branch and one boundary: internal view models plus
      `Mocassin.App.Views` resources/styles; no control templates (S1), no new dependency (vendored MIT
      assets), no `tools/` generator, and the expected-files list is complete for that boundary. — Verified:
      one branch off current `main` (merge-base `2d9d5f4`); the §8 expected-files list cross-checks complete
      against the mapping, slots, fills, tests, and documentation — every named existing file exists and
      every new file is named.
- [x] **Risks.** The risks are real and owned: the selected-state fill delivery (probe-first with a stop
      rule), the arc-geometry parsing check, the owner-vetoable glyph choices, the projects-list
      interpretation, and the Full-trigger reclassification rule if a vendoring generator becomes necessary.
      — Verified: each risk names a probe, a stop rule, or a recorded veto path, with the dev as implementer
      and the owner as decision-maker; the arc-parse risk is real (the pinned `cube.svg` contains arc
      commands) and the realized-geometry test lands first; the generator-growth risk records the
      Full-trigger reclassification. n-2 records the request-phrase reading for the owner.
- [x] **Licensing and provenance.** Phosphor Icons is verified MIT (© 2023) at the pinned commit; the
      vendoring follows the ADR 0007 third-party pattern and the spglib catalog precedent (SPDX, pin,
      checksums, licence copy, NOTICE entry); no GPL content is involved; no icon font, package, or network
      access is introduced. — Independently re-verified at pin `2b75f3a`: the upstream `LICENSE` is MIT
      (© 2023 Phosphor Icons) and hashes to the §7 value; all five subset SVGs plus the `house` alternative
      hash exactly to the §7 values; `package.json` is `@phosphor-icons/core` 2.1.1, MIT; each SVG is a
      single 256-unit `viewBox` path with `fill="currentColor"`; no build/run-time network, no GPL content;
      manual pipeline with the recorded generator stop rule.

## Dependency and interface checks

- [x] The base is current `main` at `2d9d5f487706ba946d6a027a1d668677b2535d19` and the branch is based on it.
      — Verified: `git merge-base fix/gui-icon-pass main` = `git rev-parse main` = `2d9d5f4…`; the branch is
      based on current `main` and `main` has not moved.
- [x] The collapsed `TabIconSlot` contract (DocumentsRegionView/ConsoleView, `DocumentViewModel.Icon`,
      `ConsoleChannelViewModel.Icon`) is as the maintenance record §3 describes, and the design populates it
      without reworking the template. — Verified in the code: the 16 × 16 `TabIconSlot` `ContentControl` with
      `Content="{Binding Icon}"`, collapsed while null, `IsHitTestVisible="False"`, `Focusable="False"`,
      `AccessibilityView="Raw"` in both headers; `DocumentViewModel.cs:35` `public virtual object? Icon =>
      null;` with no overriding subclass; `ConsoleChannelViewModel.cs:58` init-only and never set; the design
      overrides `Icon` per document kind and sizes the slot from `Mocassin.IconSize` (= 16) with no template
      change.
- [x] The `ExplorerNode.IconKey` values already set by `ProjectExplorerNodeProvider` match the record's
      mapping keys. — Verified: the provider sets `IconKey` `"overview"` and `"json"` on the two nodes —
      exactly the §4/§5.2 mapping keys; `ExplorerNode` is the `string? IconKey` record.
- [x] No unresolved cross-project, cross-language, format, or public-API interface question remains.
      — Verified: the freeze commit is documentation-only; the touched types are internal and no
      cross-language or format surface is involved.

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

### Reviewer evidence (round 1, independent)

Frozen state and branch:

- `git rev-parse main` → `2d9d5f487706ba946d6a027a1d668677b2535d19`; `git merge-base fix/gui-icon-pass main`
  → the same SHA: the branch is based on current `main`.
- `git show --stat 34e7c18` → only `docs/development/ui-visual-contract.md` (+111) and
  `docs/planning/maintenance/2026-11-22-gui-icon-pass.md` (+385): a documentation-only freeze.
- `git diff 34e7c18^..34e7c18 -- docs/development/ui-visual-contract.md` → eight hunks confined to §2, §5.2,
  §7.4, §7.6, §8, §11.3, §12, §13 — exactly the sections the request names.
- `git diff 34e7c18..a87e38e` → this review record (+209) plus request-owner status bookkeeping in the
  maintenance record (the review-status link, the design-commit SHA in §16, three references turned into
  links); no design content changed (n-1).

Upstream verification, recomputed independently at the pinned commit
`2b75f3ad12b420c9504ef05df8d2564a28f8500e` (files fetched from `raw.githubusercontent.com` and hashed with
`sha256sum`):

| Upstream file | Recomputed SHA-256 | Matches §7 |
| --- | --- | --- |
| `assets/regular/info.svg` | `438a1f319d53d86e840af73fb7b1d224c9224a437b9fa449a4e2cb45a82a3552` | yes |
| `assets/regular/brackets-curly.svg` | `170562ad2b247d86bf2b5fd727cf3f96681b0cbef92aab49346ec6e235802ca9` | yes |
| `assets/regular/hand-waving.svg` | `07c4c9d18c9a4c86508fde52cddaf95aa1499552c99bfeebc404d5b991fdf41f` | yes |
| `assets/regular/cube.svg` | `7c0f13cb492d0dd212e48ab259dabec4627df32d71aa00ce31b3228c0f35112b` | yes |
| `assets/regular/warning-circle.svg` | `e48a90760a68659fd3f06dd9187e471aead4877c72e915339071b2c7f7bd714c` | yes |
| `assets/regular/house.svg` (alternative) | `180e9259e99e23da197b59b19394d006d6a5d33e384cc2b9c8381cb0214a5716` | yes |
| `LICENSE` | `b5b1f1da112d18ea2147decfd48ddc1bf2b5aeb6c265381579340e95b15a2bb2` — MIT, © 2023 Phosphor Icons | yes |

`package.json` at the pin: `@phosphor-icons/core` `2.1.1`, `"license": "MIT"`. Each subset SVG is a single
256 × 256 `viewBox` path with `fill="currentColor"`, as §7 states; `cube.svg` contains arc commands, so the
arc-parse risk is real.

Code and test reads (branch head, unchanged since the freeze):

- `DocumentsRegionView.axaml` and `ConsoleView.axaml` carry the reserved 16 × 16 `TabIconSlot` exactly as
  maintenance record §3 describes; `ProjectsRegionView.axaml`'s row and tree templates have no icon slot
  today; `ExplorerNode.cs` (`string? IconKey`) and `ProjectExplorerNodeProvider.cs` (sets `"overview"`,
  `"json"`); `DocumentViewModel.cs:35` (`public virtual object? Icon => null;`, internal abstract) with no
  overriding subclass; `ConsoleChannelViewModel.cs:58`; `DocumentKinds.cs` (`welcome`, `project.overview`,
  `project.json`); `WelcomeDocumentViewModel` / `ProjectOverviewViewModel` ("Overview — {name}") /
  `ProjectJsonViewModel` ("Project JSON — {name}") are the complete open-document set.
- `AccessibilityTests.cs:338` (`TabsShowAFocusRingAndReserveTheCollapsedIconSlot`) asserts the collapsed,
  `Raw`, non-focusable, non-hit-testable 16 × 16 slot; `ConsoleTabsTests.cs:60` (the file lives under
  `Mocassin.App.Tests/Views/`); `ScreenshotTests.cs:28` is the `MOCASSIN_REGENERATE_VISUAL_BASELINES` switch;
  `TabTreatmentTests`, `ScrollGutterTests`, `AccentDeliveryTests`, `ThemeContrastTests` exist under
  `Mocassin.App.Tests/Visual/`; the baselines include `light.png`/`dark.png`/`highcontrast.png`, so the three
  new `-project` captures plus the regenerated shell captures cover every icon surface in all three themes.
- `NOTICE.md` has the "Third-party dependencies" section the §7 entry joins; `tools/check-license-headers.py`
  covers `.cs`/`.rs`/`.py`/`.sh`/`.ps1` only, as the record's licence-header scope note states; ADR 0007
  rule 5 (third-party content carries its own SPDX identifier and attribution) and the tabs record I1/O3
  (the slot reserved collapsed, owner-confirmed) match the design's claims.
- Contrast recomputation for the missing-marker floor: `#F48771` on `#252526` ≈ 6.2:1 and on `#2D2D30`
  ≈ 5.6:1; `#A1260D` on `#FFFFFF` ≈ 7.5:1 and on `#ECECEC` ≈ 6.3:1; HC `#FFFFFF` on `#000000` = 21:1 — the
  §5 values hold; the muted/selected icon pairs are existing §11.1 rows.

Gates run by the reviewer at the branch head (`a87e38e`):

- `./tools/verify.sh docs --require` → exit 0 (strict build 0 errors, link and math checks, DocFX, rustdoc).
- `./tools/verify.sh policy --require` → exit 0 (62 policy tests OK, SPDX headers in 1107 files, secret scan
  clean, cargo-deny OK). `git status` clean before and after.

## Findings

| Severity | Origin | Finding | Evidence |
| --- | --- | --- | --- |
| Note | documentation | **n-1** The request commit `a87e38e` touches the maintenance record as well as this record: the review-status link, the design-commit SHA in §16, and three references turned into links. No design content changed; this is the documented documentation-only request/status commit that `docs/planning/reviews/README.md` §8.1 permits between the freeze and the record commit. Recorded as a note per that rule. | `git diff 34e7c18..a87e38e -- docs/planning/maintenance/2026-11-22-gui-icon-pass.md`: three hunks, all status/link/SHA bookkeeping. |
| Note | design | **n-2** The owner-request phrase "every open document/control tab" (maintenance record §1, echoing the tabs record's I1) can be read to include the Console/Validation tabs, while C1 scopes coverage to document tabs and leaves console tabs text-only. The design consistently implements the C1 reading (§5.2 coverage, contract §8, tests 5–6), §14 defers console icons explicitly, and the reserved console slot stays collapsed (O3), so a later owner widening needs no template rework. The owner should confirm the narrow reading when accepting the verdict. | Maintenance record §1 request paragraph vs C1; `ui-visual-contract.md` §5.2 and §8; §14 deferred list; tabs record I1/O3. |
| Note | documentation | **n-3** Maintenance record §5 says "the test asserts these four pairs" after enumerating five marker/fill pairs (two dark, two light, one high contrast). The enumeration is complete and normative; "four" evidently counts the light/dark pairs, with HC `#FFFFFF` on `#000000` an existing §11.1 high-contrast row. The dev should assert every enumerated pair, including HC. | Maintenance record §5 vs §9 test 7. |
| Note | evidence | **n-4** AC5's "per-icon SHA-256 checksums matching §7" is proven by the vendoring-time re-hash with its recorded stop rule, not by test 8, which asserts the 64-hex format and key-set equality only (the §12 provenance-rot row is explicit about this split). The named proof is adequate — the re-hash is normative and its result lands in the provenance file and §16 — but having test 8 also pin the exact §7 values would make the value match machine-checked. | Maintenance record §11 AC5 vs §9 test 8 and the §7 re-hash paragraph; §12 provenance-rot row. |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. Origin: **design**, **implementation**,
**evidence**, or **documentation**. The review agent writes the findings and verdict directly. The request
owner adds resolutions in the separate `Resolution status` section without altering the review agent's
findings, evidence, or verdict.

## Record state

**State:** Ready for implementation

Round 1 (2026-11-22) reviewed the frozen design at `34e7c18d71ee29ab5d81885ba13bb761758f1993`; the branch
head `a87e38ef9be0f0df892408d34b03a5682b563986` adds only this record and the request-owner status
bookkeeping covered by n-1. Four Notes (n-1–n-4), no Blocker or Major; the Light classification holds. The
human owner may accept the notes and approve the start.

## Reviewer independence

> I am a fresh context. I did not author or implement the reviewed work and have no shared conversation with
> the design agent, dev, or lead.

## Verdict

- [x] Ready for implementation
- [ ] Ready with notes
- [ ] Changes required
- [ ] Waived

Follow-up owner: the dev — record the vendoring re-hash result in maintenance record §16 and, optionally,
extend the §9.8 provenance test to pin the exact §7 checksum values (n-4). The human owner accepts the notes
n-1–n-4 (n-2 in particular) and approves the start.
Follow-up step: — (out-of-step maintenance fix under `AGENTS.md` §3.5; no step tag).

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
| n-1 | Accepted — documented request/status bookkeeping commit; no action | dev | `338a112` |
| n-2 | Accepted — owner-confirmed narrow coverage (document tabs and projects surfaces; console tabs text-only) | dev | `338a112` |
| n-3 | Resolved — §5/§9 wording corrected to five pairs and `ThemeContrastTests` asserts all five, including high contrast | dev | `338a112`; wording in the evidence commit |
| n-4 | Resolved — the provenance test pins the exact §7 SHA-256 values and recomputes the committed licence hash | dev | `338a112`; evidence in maintenance record §17.6 |

The dev fills this section after the verdict. Do not alter the review agent-authored findings, evidence, or
verdict.

## Review rounds

Append each new round below. Never overwrite an earlier round.

### Review round 1

#### Independence and frozen state

I am a fresh context (GLM-5.3, opencode-go) with no shared conversation with the design agent, the dev, or
the lead; I did not author or implement this design. The reviewed design is the frozen commit
`34e7c18d71ee29ab5d81885ba13bb761758f1993` on `fix/gui-icon-pass`. The branch head at review time,
`a87e38ef9be0f0df892408d34b03a5682b563986`, adds only this review record and the request-owner status
bookkeeping recorded as n-1; the design content is unchanged from the freeze. The working tree was clean
before and after the gates.

#### Commands and results

- `git rev-parse main` → `2d9d5f487706ba946d6a027a1d668677b2535d19`; `git merge-base fix/gui-icon-pass main`
  → the same SHA.
- `git show --stat 34e7c18` → `ui-visual-contract.md` (+111) and the maintenance record (+385),
  documentation only.
- `git diff 34e7c18^..34e7c18 -- docs/development/ui-visual-contract.md` → eight hunks in §2, §5.2, §7.4,
  §7.6, §8, §11.3, §12, §13 only.
- `git diff 34e7c18..a87e38e` → this record plus the n-1 maintenance-record bookkeeping.
- Upstream fetch at the pin `2b75f3ad12b420c9504ef05df8d2564a28f8500e` (raw.githubusercontent.com): `LICENSE`
  and `assets/regular/{info,brackets-curly,hand-waving,cube,warning-circle,house}.svg` hashed with
  `sha256sum`; all seven values match maintenance record §7 (see the round-1 evidence table).
  `package.json` → `@phosphor-icons/core` 2.1.1, MIT.
- `./tools/verify.sh docs --require` → exit 0; `./tools/verify.sh policy --require` → exit 0 (62 policy
  tests, SPDX in 1107 files, secret scan clean, cargo-deny OK); `git status` clean afterwards.
- Code/test reads as listed in the round-1 evidence: the reserved `TabIconSlot` in both headers, the provider
  keys, the view-model `Icon` members, the accessibility/console-tab/screenshot test anchors, `NOTICE.md`,
  ADR 0007, `tools/check-license-headers.py` coverage, and the tabs record I1/O3.
- Contrast recomputation for the §5 missing-marker pairs: all pass the 3:1 floor (see the round-1 evidence).

#### Findings

Four Notes, no Blocker or Major: n-1 (request-commit bookkeeping delta), n-2 (the "every open
document/control tab" phrase vs C1's narrow coverage — for the owner to confirm), n-3 (the "four pairs"
wording vs five enumerated marker pairs), n-4 (the checksum-value match is enforced by the vendoring
re-hash rather than the test). Minor and Note findings are recorded and accepted under the Light stop
rule; the classification holds — nothing found rises to a Full trigger.

#### Verdict

**Ready for implementation.** All six design questions and all four dependency/interface checks pass with
the notes above. The mapping matches the owner's confirmed choices (welcome `hand-waving`, overview `info`,
json `brackets-curly`, project `cube` including the open-projects rows, missing `warning-circle`); the
accessibility contract keeps the icons decorative, unnamed, non-focusable, and non-hit-testable; the
tabs/gutters slot contract is respected and populated without template rework; the licensing and provenance
are independently verified at the pinned upstream commit. The human owner may accept the notes and approve
the start.
