# Step 25 — Energy, Lattice, and Simulation Editors

**Phase:** F. GUI  
**Depends on:** 24, 05, 06
**Delivers:** Editors for interaction environments, clusters, site energies, doping/lattice definitions, and
MMC/KMC simulations, with cost previews and live validation.

## Implementation status

- **Status:** Not started
- **Step branch:** —
- **Step CI:** —
- **Main merge:** —
- **Step tag:** —
- **Audit:** Not requested
- **Design review:** Not requested
- **Last updated:** —

| Feature | Branch | Status | Local gates | Step CI | Squash commit |
| --- | --- | --- | --- | --- | --- |
| — | — | — | — | — | — |

## Goal

Complete the model-authoring GUI. These editors deal with the most expensive and least intuitive parts of the
model, so they must show costs, geometric relationships, and the effect of changes before the user commits.

## Original reference

- **Energy:** stable environment cutoff + hollow-sphere filters; per-unstable-site environment (cutoff +
  filters); group interactions (center + absolute geometry); site defect energies (particle × site × energy)
  with a force-refresh action.
- **Lattice:** read-only default building block; dopings (dopable/dopant/site); doping applications (primary +
  counter doping, building block, priority, `UseCounterDoping`); building-block viewer.
- **Simulations:** MMC/KMC grids; transition assignment via selection dialogs; parameter editors for
  temperature, MCSP, block count, time limit, job count; KMC adds pre-run, normalization, field; MMC adds
  break tolerance/sample length/interval/result sample.
- Validation was separate; no cost preview; no inline explanations.

## Design

### 1. Energy editor

**Stable environment**

- Cutoff field in Å with a live "neighbor shells" summary: number of images, unique pairs, estimated table
  size.
- Filter grid: start/end radius, site A/B, enabled flag, remove/duplicate.
- A visual radial ring diagram shows the cutoff and filter shells; selecting a filter highlights the affected
  site pair.
- Warning when the cutoff exceeds half the smallest cell dimension.

**Unstable environments**

- List of unstable sites; each expands to its own cutoff/filter editor.
- "Inherit stable cutoff" toggle; "copy from another unstable site" action.
- Shows which unstable-stable pairs will be sampled and their count.

**Clusters**

- List of clusters with center site, offset count, permutation estimate.
- Cluster geometry editor:
  - add/remove offsets;
  - enter fractional vectors or pick a site/position;
  - show the center and surrounding points in a mini 2D/3D preview;
  - distance and range indicators;
  - ring/duplicate warnings.
- Permutation preview: raw count, unique count, table size; "too large" warnings with suggestions (fewer
  offsets, smaller particle sets).
- Cluster identity/name and usage in parameterization templates.

**Site energies**

- Matrix editor: rows = sites, columns = particles (only valid combinations), cells = eV.
- Bulk actions: set row/column to 0, copy/paste a rectangular block, import/export CSV.
- Highlight non-zero values and unstable-site barrier values.
- Inline validation for range and invalid particle/site combinations.

**Cost preview**

- A panel summarizes:
  - P1 positions, pair images, unique pairs, clusters, unique permutations, estimated rule count, memory.
- Updates are debounced and cancellable.
- A "compile preview" button opens the compiled model summary (step 08).

### 2. Lattice editor

**Default building block**

- Read-only table of stable sites with the first particle of their set, and unstable sites with void.
- Explanation of the implicit default and how dopings modify it.
- Links to the particle set editor.

**Occupation exchanges**

- Grid: name, site, source particle, target particle, charge delta, usage.
- Add/edit/remove; source/target pickers filtered to the site's set.
- Inline warnings for source == target, source not default, or charged vacancy.

**Dopings**

- Ordered list with drag/reorder or up/down buttons; priority is explicit and unique.
- Detail form:
  - name, priority;
  - primary exchange;
  - dependent exchange + charge compensation toggle;
  - charge delta summary and required ratio;
  - usage in job templates.
- A "doping sequence" visual shows the order and the cumulative effect on occupations.
- Delete shows dependents.

**Doping preview**

- Inputs: supercell A/B/C and doping fractions (defaults from the selected job template or 10×10×10).
- Outputs:
  - requested vs. applied counts;
  - dependent counts;
  - remaining particles per site;
  - total charge before/after and neutrality;
  - warnings for unattainable fractions.
- The preview uses the same supercell generator as step 10.

### 3. Simulation editor

**List and detail**

- Grid: name, kind, temperature, MCSP, transitions, job count, status/usage.
- Add KMC/MMC; duplicate; delete with dependent job templates.
- Kind is immutable after creation (delete/recreate with a warning).

**Common settings**

- Temperature (K), target MCSP, time limit, minimum success rate, job count, RNG seed (optional), execution
  flags (checkboxes with explanations), instruction (advanced/extension).
- Numeric expression support and unit labels.
- Defaults button restores documented defaults.

**KMC settings**

- Pre-run MCSP, normalization energy (auto/fixed), max attempt frequency, electric field modulus and
  direction (fractional vector or crystal direction presets).
- Field preview: normalized Cartesian direction and magnitude.
- Warning when the field is zero but the modulus is non-zero.

**MMC settings**

- Break tolerance, sample length, sample interval, result sample MCSP.
- Explanation of equilibrium detection and how to disable it (tolerance 0).

**Transition assignment**

- Dual-list or checkbox table of available transitions of the matching kind.
- Shows transition mechanism, path length, expected rules, and usage.
- "Select all/none" and search.
- Validation: no transitions, mixed kind (not possible in the filtered list), missing mobile species.

**Simulation preview**

- Estimated total jobs (with template overrides), estimated runtime hint, memory estimate.
- "Create job template from this simulation" shortcut.

### 4. Cross-editor behavior

- All editors use the shared list/detail framework and undo/redo.
- Changing a site/particle transition invalidates dependent editors; show a banner and refresh.
- Cost previews are shared with the compiler (step 08) and job builder (step 10).
- Validation errors are inline and in the Problems panel.
- The inspector shows summary/statistics for the selected object.
- "Show in 3D" actions open the viewer with the object highlighted (step 27).

## Implementation tasks

- [ ] Implement stable/unstable environment editors and filter grid.
- [ ] Implement the radial filter diagram.
- [ ] Implement the cluster geometry editor and permutation preview.
- [ ] Implement the site-energy matrix with bulk edit/import/export.
- [ ] Implement the cost preview panel.
- [ ] Implement occupation exchange and doping editors with ordering.
- [ ] Implement the doping/supercell preview.
- [ ] Implement simulation list/detail, common/KMC/MMC forms, and transition assignment.
- [ ] Implement simulation/job-template shortcuts and usage views.
- [ ] Add headless tests and screenshots.

## Acceptance criteria

- [ ] A user can define the ceria stable environment, migration-edge cluster, and site energies in the GUI.
- [ ] Filters are editable as a grid and reflected in the radial diagram.
- [ ] Cluster geometry shows offsets and rejects rings/duplicates inline.
- [ ] Site energies support bulk edit and CSV import/export.
- [ ] The cost preview matches the compiler counts for the ceria sample.
- [ ] A ceria reduction doping can be defined and previewed for charge neutrality.
- [ ] KMC/MMC simulations can be created with all settings and transitions.
- [ ] Zero-field/modulus mismatch and missing transitions are caught inline.
- [ ] Undo/redo works across all edits.
- [ ] Documentation written

## Documentation

At this point, document the energy/lattice/simulation editors:

- `docs/user-guide/energy.md`, `lattice.md`, `simulations.md` — editor workflows, cost previews, doping preview.
- `docs/physics/energies.md` — how UI previews map to the energy model.
- `docs/development/ui-architecture.md` — list/detail editors, dependency invalidation.
- `docs/tutorials/ceria-kmc.md` — parameterization/simulation setup in the GUI.

## Tests

| Test | Assertion |
| --- | --- |
| Preview acceptance | Preview counts match the real compiler on the sample projects |
| Environments | Cutoff/filter edits, validation, preview counts |
| Filters | Add/remove/duplicate, radial diagram data |
| Clusters | Offset add/remove/pick, ring/duplicate |
| Site energies | Matrix edit, bulk fill, CSV |
| Cost preview | Matches compiler for samples |
| Exchanges | Source/target constraints, usage |
| Dopings | Priority reorder, compensation ratio, preview |
| Simulations | Common/KMC/MMC forms, defaults, validation |
| Transitions | Assignment, kind filtering, empty error |
| Undo/redo | All operations |

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Cost preview expensive | Debounce, cancel, reuse compiled caches |
| Cluster geometry hard to visualize | Mini 2D/3D preview + numeric table |
| Doping preview diverges from build | Shared supercell generator |
| Simulation settings overwhelming | Grouped sections, defaults, explanations |
| Invalid states via cascades | Dependency checks and banners |

## Done when

Users can define the complete energy/lattice/simulation model in the GUI, see the cost and doping effects
before building, and fix every validation issue inline.
