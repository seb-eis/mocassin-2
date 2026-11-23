# Step 24 — Particle, Structure, and Transition Editors (superseded)

> **Superseded 2026-11-22.** The human owner split this step into two dependency-ordered steps under the
> complexity rule: it was complexity class **High** with three independent acceptance stories and an audit
> surface too large for one sitting. This page is kept as a pointer and as the historical record of the closed
> editor-layout gate; it is **not** an implementation authorization.

| Step | Page | Outcome |
| --- | --- | --- |
| **24A** | [Shared Editor Framework, Particles, and Structure](step-24a-model-editors-particles-and-structure.md) | The ADR 0003 editor surface and shared framework, particle and particle-set editors, space group and cell parameters, the site table with the P1 preview, dummy positions and the structure summary, and its own tests, documentation, and closeout |
| **24B** | [Transition Editors](step-24b-model-editors-transitions.md) | State changes and groups, the chain builder, the KMC and MMC editors, the transition summary with the compile preview, and its own tests, documentation, and closeout |

- 24A depends on 23B, 03, and 04; 24B depends on **24A** (its editor framework) and 04. Step **25 depends on
  24B**, and Step 27 keeps its existing dependency on the Step 24 line.
- The split decision, both complexity classes, and their justification are recorded in
  [`04-roadmap.md`](../04-roadmap.md) §3.1 and on each step page (24A §16, 24B §14).
- The 24A design review record is [`reviews/step-24a.md`](../reviews/step-24a.md), created at the 24A freeze.
  24B has **no** review record until it is scheduled: its freeze follows 24A feature 1, because it consumes the
  framework that feature delivers.
- The editor-layout gate is **closed**: the owner picked the layout on 2026-11-22 and it is recorded in
  [ADR 0003](../adr/0003-avalonia-mvvm-stack.md#editor-layout-decision-2026-11-22). The POC report
  `docs/planning/maintenance/2026-11-22-step-24-layout-poc.md` and its PNG subset are the durable evidence and
  land on `main` through the spike branch's owner-approved `--no-ff` merge; the spike code is discarded.
- The feature list, acceptance criteria, tests, and documentation plan live on the 24A and 24B pages. This page
  is history: do not build, branch, or design from it.

## Historical layout gate record (closed 2026-11-22)

This section is the frozen §0 gate specification of the unsplit step, retained because the POC report cites it
as its specification. The outcome is recorded in §0.8.

### 0. Editor layout spike (owner-reviewed POC, closed 2026-11-22)

Before the final editor design is frozen, build an owner-reviewable POC of the candidate layouts. This is a
gated decision: the owner sees every option with representative particle, site, and transition data and picks
the layout before Step 24 implementation starts. The owner decided on 2026-11-22 that the spike covers **all
five layout options** below, and that the custom matrix control (option 6) is evaluated as a data-shape
control inside whichever layout wins, not as a sixth layout. The same owner decision assigns the spike build to
the paid `dev` binding (the `dev-free` allowance was reported exhausted for that day); the lead records the
fallback as [`model-economics.md`](../../development/model-economics.md) §2 requires.

**Hard constraint — repeat of the standing ADR 0003 rule: no data-grid dependency while the spike is open.** The
spike adds no package and no third-party control. It must not adopt `Avalonia.Controls.DataGrid` (deprecated),
`TreeDataGrid` Pro/paid controls, or any third-party grid. If an option appears to require a new dependency,
stop and relay the question to the owner: a new control dependency needs its own ADR recording the owner
decision, licence, maintenance risk, and Avalonia 12 compatibility, and it is not part of this step.

#### 0.1 Options in the spike

1. **ListBox table + detail editor** (baseline) — header `Grid`, virtualized `ListBox` rows, and a
   `ContentControl` that shows the selected item's type-specific editor.
2. **TreeView + detail editor** — hierarchy for model/domain objects, editor on the right.
3. **TabControl + list + detail** — one tab per domain with a table/list and editor.
4. **SplitView / GridSplitter master-detail** — resizable selection and editor panes.
5. **Custom `TableView` `TemplatedControl`** — reusable table surface with headers, selection, sorting hooks,
   and virtualization.
6. **Custom matrix control** — *not* a layout option (owner decision, 2026-11-22). It is a data-shape control
   for matrix-shaped data — the chain builder's S0/S1/S2 occupation matrices, the MMC exchange-matrix preview,
   and Step 25's site-energy matrices — and is evaluated where those surfaces appear **inside** the five
   layouts, not as a competitor window shape.

#### 0.2 What every option must demonstrate

Every option is exercised as the *selection surface* of the same master-detail editor: the table/list only
selects; the type-specific editor owns the fields, validation, reference pickers, and undo/redo. Per option,
the spike must demonstrate and record:

- **Selection, keyboard navigation, focus, and multi-select where needed.** Real-key headless input
  (`HeadlessSession.DispatchAsync`, `window.KeyPress`) proves arrow/Home/End navigation, Tab order into the
  editor pane, and Enter/Escape behaviour; multi-select is required only where bulk delete is safe (particles,
  sites).
- **Virtualization with 1,000+ rows.** The generated project value of §0.4 must scroll, select, and filter
  without losing rows or alignment; the spike records the realized-container behavior (and any option that
  cannot virtualize fails here).
- **Stable header/row column alignment.** The known failure mode is two independently auto-sized grids drifting;
  each option must show columns stay aligned on window resize and long content.
- **Search/filter/sort behavior.** A search box, a filter, and a sort over the real ceria particle and site
  lists, recording how each option wires them (built-in support vs view-model code).
- **Opening a type-specific editor with validation, reference pickers, and undo/redo.** Selecting a particle,
  a site, and a transition opens that type's editor; an invalid edit shows an inline error and changes nothing;
  a valid edit goes through the real `ProjectEditCoordinator.Apply` and is undoable. Reference pickers are
  searchable name-plus-details combos, not raw IDs.
- **Theme, high-contrast, and screen-reader labeling constraints.** Every option is captured in light, dark, and
  high-contrast themes using the production theme system (§0.5); every interactive control must carry an
  automation name and role; status is never colour-only (glyph plus text, as in the 23B status bar).
- **Implementation size and maintenance cost.** A per-option estimate of new files/lines, what the six domains
  reuse, what each later domain must repeat, and who owns the sorting/filtering/column/a11y code afterwards.

#### 0.3 Per-option requirements

Each option must additionally answer the weakness ADR 0003 records for it:

| Option | Must additionally demonstrate |
| --- | --- |
| 1. ListBox table + detail | how sorting, filtering, and column sizing work without a grid: one shared column-width policy (fixed/star), header-click sort, view-model filter; the cost of having no built-in column resize/reorder |
| 2. TreeView + detail | whether a hierarchy earns its place for flat domains (particles, sites): dense columnar comparison inside tree rows, cross-object sort/filter, expansion and keyboard cost; where a tree genuinely helps (domain navigation, chain patterns) |
| 3. TabControl + list + detail | the cost of nested tabs under the shell's document tabs (two tab levels, gesture conflicts), how many tabs six domains produce, and cross-domain context loss (editing a particle while checking a transition reference) |
| 4. SplitView / GridSplitter | pane reflow when narrow and wide, keyboard focus across the splitter, minimum pane sizes, whether resizable panes beat fixed star columns enough to justify the chrome, and interaction with the shell's persisted panel sizes |
| 5. Custom `TableView` | the real cost of owning selection, keyboard, a11y, headers, and a column policy in a `TemplatedControl`; that it composes with the merged theme/accent tokens; an LOC/maintenance estimate against the composed options; whether resize/reorder can be added later without a rewrite |

#### 0.4 Representative data

Real data through the real store — the spike does not invent model shapes:

- **Particles, sets, sites, structure:** `samples/ceria/ceria.mocprj`, loaded through `ProjectStore`
  (the `ProjectFixtures` pattern): 3 particles including the pinned read-only vacancy, 3 particle sets, 3 cell
  sites, space group 225, the cubic cell, and 1 dummy position.
- **Transitions:** `samples/transitions/ceria-kmc.mocprj` and `samples/transitions/ceria-mmc.mocprj` — the ceria
  structure with a KMC oxygen migration and an MMC exchange; pattern breadth from
  `samples/transitions/interstitialcy5.mocprj`, `vehicle5.mocprj`, and `polaron-hop.mocprj`. **The base ceria
  sample has no transition model — do not demonstrate transitions from it.**
- **1,000+ row virtualization:** generated toy data — an in-memory project value with 1,000+ generated sites
  (and a padded particle list) from a spike-local generator. Breadth only, not a plausible physical model, per
  the testing strategy's "generated toy data for breadth".
- **Edits and undo:** the spike binds real documents and the real 23B `ProjectEditCoordinator`/`ProjectDocument`
  for the validation/undo/redo demonstrations; it must not fake an undo stack in spike-local code.

#### 0.5 Capture and accessibility evidence

The spike uses the repo's existing headless/screenshot infrastructure, not a new harness:

- Build the spike surfaces against the same composition and theming as the shell (`AppHost.Build`, the shell
  window or a spike window using the merged theme resources) and capture with
  `window.CaptureRenderedFrame()` after `HeadlessSession.Pump`, as `Visual/ScreenshotTests.cs` does.
- Per option, capture at least: the list with a selection, the type-specific editor open, and a filtered state —
  each in **light**, **dark**, and **high contrast** (`ThemeSelection.Light`, `ThemeSelection.Dark`, and the
  dark + high-contrast + compact + reduced-motion settings the existing baselines use).
- Keyboard and a11y evidence: real-key navigation (`window.KeyPress`) and automation-name/role assertions on
  every interactive control, following `Visual/AccessibilityTests.cs`; record any control that cannot be named
  or driven by keyboard.
- The spike writes PNGs to a spike-local output folder, **not** to `Visual/Baselines/` — no committed baseline
  is added or changed, and no cross-OS pixel comparison is attempted (the existing screenshot policy holds).
- The fixed accent palette (`#007ACC`) and the merged theme tokens are used as-is; the spike restyles nothing
  and must not reopen the accent/palette maintenance work.

#### 0.6 Spike execution, deliverable, and disposition

- **Branch:** `fix/step-24-layout-poc`, a short-lived out-of-step maintenance branch from current `main`
  (`AGENTS.md` §3.5). No step branch exists yet; the spike is not Step 24 feature work.
- **Binding:** the paid `dev` binding builds the spike (owner decision, 2026-11-22).
- **Code location:** spike code is test-only — under `Mocassin.App.Tests` (a spike folder), so the production
  projects gain nothing and discarding is a folder delete. No production project, contract, settings, or CLI
  change is merged from the spike.
- **Deliverable:** a short POC report — one section per option with the screenshots, the §0.2 checklist
  results, the §0.3 answers, the measured/estimated size and maintenance cost, a comparison table, and a
  **recommended** option with rationale. The recommendation is input only: the owner picks the layout, and the
  spike must not pre-write a "chosen layout" into ADR 0003 or this step page.
- **Durable location of the report:** `docs/planning/maintenance/<date>-step-24-layout-poc.md`, with a bounded
  committed PNG subset under `docs/planning/maintenance/assets/step-24-layout-poc/`. Rationale: the spike runs
  as an out-of-step `fix/` branch exactly like the GUI maintenance passes (visual refresh, fixed accent, icon
  pass), whose records already live there; the report is decision-input evidence, not a normative design (the
  pick itself belongs in ADR 0003) and not an implementation authorization, so `docs/planning/drafts/` is the
  wrong home. Planning records stay out of the published site, which is correct for internal decision
  evidence. The committed PNG subset is bounded — at minimum the light and high-contrast editor-open captures
  per option plus the interaction states that justify the recommendation; the full capture matrix stays on the
  spike branch for the owner's review.
- **Disposition (owner decision, 2026-11-22):** after the owner records the pick in ADR 0003, the spike **code
  is discarded** — the spike folder is deleted and the branch is deleted locally and remotely. The durable
  artifacts are (a) the maintenance-record report with its PNG subset and (b) the ADR 0003 update; both land on
  `main` through the branch's owner-approved `--no-ff` merge (`[skip ci]`). The implementation does not seed
  from spike code; it re-derives from the frozen design.

#### 0.7 Gate sequence (explicit)

No step branch, feature branch, or design-review record exists until this sequence completes:

1. **Spike** — the five options are built, captured, and documented on `fix/step-24-layout-poc` (§0.1–§0.6).
2. **Owner picks the layout** from the POC report; the owner also decides the split question
   (the split decision is recorded on the [24A page](step-24a-model-editors-particles-and-structure.md) §16) at
   the same gate.
3. **The pick is recorded in ADR 0003** — the option table gains the chosen layout and the reasons. If the
   split is approved, the step index, the `04-roadmap.md` §3 forecasts, the dependency graph, and the
   milestone rows are updated in the same change, before any design freeze
   ([`04-roadmap.md` §3.1](../04-roadmap.md#31-step-sizing-and-split-triggers)).
4. **Design freeze** — the step page is completed against the chosen layout and reconciled with the 23A/23B
   seams as built (see the §1 reconciliation note); the docs are frozen at a specific commit.
5. **Design review** — `docs/planning/reviews/step-24.md` is created **at the freeze** from the template (never
   pre-seeded); a fresh review agent reviews at Full depth and stops when no Blocker or Major remains. *(After the
   2026-11-22 split this is `reviews/step-24a.md`, with `reviews/step-24b.md` at the 24B freeze.)*
6. **Owner start-gate approval** — the owner approves the verdict and the feature list.
7. **Step branch** — created from the reviewed `main` commit; implementation starts.

**Avoid (unchanged from ADR 0003):** Avalonia `DataGrid` (deprecated), `TreeDataGrid` Pro/paid controls,
third-party grid dependencies, inline cell editing for complex model objects, non-virtualized `ItemsControl`
in a `ScrollViewer`, and independently auto-sized header/row grids.

#### 0.8 Gate outcome (recorded 2026-11-22; design record completed 2026-11-23)

| §0.7 step | Outcome | Evidence |
| --- | --- | --- |
| 1. Spike | Complete. All five layout options were built with real ceria data, a 1,024-row generated breadth value, and the real edit/undo path; the matrix control was evaluated inside each detail pane. | POC report `docs/planning/maintenance/2026-11-22-step-24-layout-poc.md`, 15 PNGs, `fix/step-24-layout-poc` |
| 2. Owner picks the layout and the split | **Layout: option 1 (`ListBox` table + detail) composed with option 4's `GridSplitter` master-detail.** **Split: 24A/24B approved.** | Owner decisions, 2026-11-22 |
| 3. Record the pick and the split | **Done.** ADR 0003 records the composition, its reasons, and the evidence; the step index, `04-roadmap.md` §3/§3.1/§5, the dashboard rows, and `05-open-decisions.md` are updated in the same design change. | [ADR 0003](../adr/0003-avalonia-mvvm-stack.md#editor-layout-decision-2026-11-22), this branch |
| 4. Design freeze | **24A frozen for review.** 24B is drafted and is frozen only after 24A feature 1 (`shared-editor-framework`) lands, because 24B consumes that framework. | [24A page](step-24a-model-editors-particles-and-structure.md) §18, [24B page](step-24b-model-editors-transitions.md) §16 |
| 5. Design review | 24A: `Ready for review` in [`reviews/step-24a.md`](../reviews/step-24a.md); a fresh review agent is requested by the lead. 24B: not requested. | review record |
| 6. Owner start gate | **Pending** for both halves — the verdict, the feature list, and the class-**High** acceptance are owner decisions. | — |
| 7. Step branch | **Not created.** No `step/24a-*` or `step/24b-*` branch exists. | — |

**Disposition confirmed (owner decision, 2026-11-22).** Delete the spike code per §0.6: the `LayoutSpike/` test
folder is deleted on `fix/step-24-layout-poc`, the branch is deleted locally and remotely after its
owner-approved `--no-ff` merge, and the POC report plus its PNG subset are the durable artefacts. The
implementation does not seed from spike code.

**POC findings folded into the split designs.**

1. **Tab order after `Home`/`End`** — after `Home` or `End` the list kept `Tab` inside its row containers instead
   of leaving for the detail pane. This is a 24A **framework requirement**, not a layout argument: the selection
   surface is exactly one tab stop and `Tab` always reaches the detail pane. The 24A page §3 states the
   requirement and the real-key regression test.
2. **The 1,000-reference-site limit** — `StructureModel.MaxReferenceSites` is 1,000 and `STR012` rejects more,
   so the spike's 1,024-site value is display-only. The 24A/24B test plans name legal sources for a
   virtualization acceptance instead of "more than 1,000 reference sites".
3. **Tree virtualization** — Avalonia's `TreeView` realizes every expanded child. This is recorded as a
   permanent rule in [ADR 0003](../adr/0003-avalonia-mvvm-stack.md#avoid): a tree is never the primary
   selection surface for a flat, large domain.
4. **Split-size persistence** — the chosen detail pane is resizable, so it needs a persisted fraction. 24A adds
   the app-local, additive `layout.editorFraction`; no format or manifest change.
5. **Reference pickers** — Avalonia has no searchable combo; 24A delivers one reusable search-box + filtered
   combo control instead of repeating two controls per reference field.
