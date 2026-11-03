# Step 26 — Parameterization, Job Templates, and Build UI

**Phase:** F. GUI  
**Depends on:** 25, 09–12
**Delivers:** The numerical parameterization workspace, job-template editor, build-template selection, package
build/deploy UI, progress/cancellation, and result summary.

## Implementation status

- **Status:** Not started
- **Step branch:** —
- **Step CI:** —
- **Main merge:** —
- **Step tag:** —
- **Last updated:** —

| Feature | Branch | Status | Local gates | Step CI | Squash commit |
| --- | --- | --- | --- | --- | --- |
| — | — | — | — | — | — |

## Goal

Close the loop from model to simulation package in the GUI. Users must be able to quantify interactions and
frequencies, define parameter sweeps, preview job counts, build a package, and see exactly what was produced.

## Original reference

- **Parameterization templates:** create only when the model validates; duplicate/delete/migrate; editors for
  stable/unstable pair energies, group energies, kinetic rule frequencies, chiral mirroring; a migration tool
  with a report.
- **Job templates:** KMC/MMC collections with name, RNG seed, job multiplier, simulation base, base config,
  per-config overrides, lattice size, doping values, selection optimizers, instruction string.
- **Build/deploy:** local only; choose model/parameterization/job set build instruction; create or append
  `.msl`; progress bar/status text/per-job counter/error log; metadata table; 99,999-job guard; post-build
  optimizers; no remote deploy.
- `ModelHash` was unusable across runtimes; template compatibility was unreliable.
- No inline completeness/quality checks.

## Design

### 1. Parameterization workspace

**Template list**

- Columns: name, model signature status (current/stale/unknown), created, last modified, completeness, usage.
- Actions: new from model, duplicate, rename, delete (with build usage), rebase, export/import JSON.
- Signature badge:
  - green = matches the current compiled model;
  - amber = stale; click for diff/rebase;
  - red = incompatible/newer format.
- "Compile model" action if the model is not yet compiled.

**Energy editors**

- **Stable/unstable pair energies:**
  - grouped by center/partner site and distance;
  - table of particle permutations with energy (eV);
  - chiral partner indicator and linked editing;
  - filters: unset, non-zero, by site, by distance;
  - bulk fill (set all to 0, copy row/column, paste block).
- **Group energies:**
  - grouped by cluster/center;
  - table of occupation states (center + surrounding particles) with energy;
  - symmetry-equivalent rows shown together;
  - filters and bulk fill.
- **Site energy overrides:**
  - matrix like step 25, with "override" badges and "reset to model".
- **Transition rule frequencies:**
  - grouped by transition;
  - columns: rule ID, S0/S1/S2, direction, frequency (Hz), flags, dependent count;
  - filters: transition, unset, non-default;
  - bulk fill (all 1e13, scale by factor, copy transition set);
  - distribution summary (min/median/max) and a histogram.

**Completeness/quality panel**

- Counts of sets/rules, unset values, zero energies, frequency range, chiral conflicts, signature status.
- Actions: "fill unset with defaults", "export report", "show problems".

**Rebase dialog**

- Shows a diff: unchanged, added, removed, geometry-changed.
- Strategies: keep matching, nearest geometry (with tolerances and per-item confirmation), reset.
- Produces a migration report with every copied/dropped/changed value.
- Nothing is applied until the user confirms.

**Import/export**

- JSON template exchange with schema version and signature.
- CSV import/export for energies/frequencies with explicit columns and validation.

### 2. Job template editor

**Collections/configurations tree**

- Left: template → collections → configs.
- Right: detail form for the selected node.
- Add/remove/duplicate/reorder collections and configs.
- Name editing with uniqueness validation.

**Collection detail**

- Simulation picker (MMC/KMC only).
- Base settings: temperature, MCSP, pre-run (KMC), field, break settings (MMC), time limit, min rate, execution
  flags.
- Job count per config, RNG seed (optional), duplicate seed button.
- Selection optimizers: table of site/particle pairs with add/remove; shows removed start options.
- Summary: number of configs, jobs, estimated package size.

**Config detail**

- Override fields (blank = inherit) with an "inherited value" hint.
- Lattice size A/B/C with total positions and memory estimate.
- Doping values: table of dopings with fraction [0,1], requested vs. preview applied counts, charge balance.
- Doping preview uses step 10; warnings for unattainable fractions.
- Per-config name and duplicate.

**Job expansion preview**

- Total jobs, per collection/config counts, job ID range, seed preview (fingerprint).
- Warnings for > 10,000 jobs, large supercells, or impossible dopings.
- "Build this template" shortcut.

### 3. Build template and build UI

**Build template editor**

- Select parameterization + job template; name; default output name.
- Signature check and missing-reference diagnostics.
- Multiple templates per project; mark one as default.

**Build panel**

- Output path (file picker), overwrite/force, compact Zstd, partial job range, parallelism.
- Preflight summary: model signature, parameterization, job template, job count, package size estimate,
  warnings.
- Build button with progress:
  - stage list (validate/compile/parameterize/expand/write/verify);
  - progress bar, current job, ETA;
  - cancel button;
  - log pane with timestamps and diagnostics.
- On success:
  - package path, size, job count, duration;
  - "open folder", "inspect", "verify", "copy summary";
  - job table (job ID, collection/config, kind, temperature, lattice, doping); collected results live in the
    `.msr` view, not the package.
- On failure:
  - failed stage, diagnostic list, log export;
  - no partial package left.
- Build history per project (last N builds with path/status/time).

**Deploy**

- Local: write to a chosen path, optionally copy job folders/scripts.
- Remote (optional improvement): SSH/SFTP upload and optional submit command; credentials via agent/key, never
  stored in the project. If not implemented in the first release, mark as deferred in the parity matrix.
- HPC helper: generate a SLURM/PBS script and a job list next to the package.

### 4. Cross-cutting UX

- All long operations are cancellable and report progress.
- The build panel can stay open while editing; it warns if the model changed since the build started.
- A "model changed" banner marks stale parameterizations/build results.
- Keyboard: F5 build, Shift+F5 cancel, Ctrl+Shift+B build template picker.
- The Problems panel aggregates model/template/build diagnostics.
- Notifications on background completion.

## Implementation tasks

- [ ] Implement parameterization list/signature status and create/duplicate/rebase.
- [ ] Implement pair/group/site/frequency editors with filters and bulk fill.
- [ ] Implement completeness/quality and import/export.
- [ ] Implement the rebase diff/report dialog.
- [ ] Implement job template tree, collection/config forms, and optimizers.
- [ ] Implement lattice/doping preview and job expansion preview.
- [ ] Implement build template editor and preflight summary.
- [ ] Implement build progress/cancel/log/summary/verify.
- [ ] Implement optional remote deploy or mark deferred.
- [ ] Add headless tests and screenshots.

## Acceptance criteria

- [ ] A parameterization can be created from the ceria model, edited, and applied.
- [ ] Stale signatures are visible and rebase produces a report.
- [ ] Chiral pair values mirror and conflicts are flagged.
- [ ] Bulk fill/import/export work and validate.
- [ ] A KMC job template with a doping sweep previews correct job counts and charge balance.
- [ ] Building from the GUI produces the same model/lattice/job payload hashes as the CLI for the same inputs
  (SQLite page layout may differ).
- [ ] Progress/cancel/error paths behave correctly.
- [ ] The build summary matches `mocassin inspect`.
- [ ] Undo/redo works across template edits.
- [ ] Documentation written

## Documentation

At this point, document parameterization/job/build UI:

- `docs/user-guide/parameterization.md`, `job-templates.md`, `building.md` — template, sweep, build, verify UI.
- `docs/reference/project-format.md` — template fields/signatures shown by the UI.
- `docs/development/ui-architecture.md` — build pipeline integration, progress/cancel, deploy.
- `docs/troubleshooting/errors.md` — stale signatures, impossible dopings, build failures.

## Tests

| Test | Assertion |
| --- | --- |
| Build acceptance | A real GUI build produces the same package content as the CLI for the sample projects |
| Template create | Sets/rules match compiled model |
| Signature | Current/stale/incompatible states |
| Rebase | Diff/report and strategy application |
| Energy editors | Edit/filter/bulk/chiral |
| Frequencies | Edit/scale/distribution |
| Job templates | Collections/configs/overrides/optimizers |
| Doping preview | Requested vs. applied/charge |
| Build | GUI vs. CLI model/lattice/job payload hash equality |
| Cancel/error | No partial package |
| Deploy | Local path/scripts; remote if implemented |

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Huge energy tables in UI | Virtualized grids, filters, paging |
| Rebase data loss | Explicit diff/report and confirm |
| GUI/CLI drift | Both call the same build pipeline |
| Long builds blocking UI | Async pipeline, progress, cancellation |
| Remote deploy security | Agent/key auth, no stored secrets, opt-in |

## Done when

A user can parameterize the model, define a sweep, build a package, verify it, and inspect the result entirely
in the GUI, with the same content as the CLI.
