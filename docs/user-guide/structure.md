# Crystal Structure

The crystal structure defines the fixed unit cell and the reference sites on which particles and transitions
act. MOCASSIN does not move the lattice itself; particles move between fixed positions.

## Structure fields

| JSON field | Meaning |
| --- | --- |
| `name` | User-visible structure name. Defaults to `New Structure`; empty or whitespace-only is `STR013`. |
| `spaceGroup` | Persisted `(number, axis, origin)` reference into the embedded catalog. |
| `cell` | The six cell parameters in ångström and degrees. |
| `sites` | Authored reference sites. |
| `dummyPositions` | Visual-only helper positions. |

The default structure is space group P1, `a = b = c = 1 Å`, all angles `90°`, no sites, and no dummies. A
missing `structure` section loads as this default.

## Space groups

`spaceGroup` persists only the tuple:

```json
{ "number": 225, "axis": "none", "origin": "none" }
```

- `number` is the International Tables space-group number, 1..230.
- `axis` is `none`, `uniqueAxisB`, `uniqueAxisC`, `hexagonal`, or `rhombohedral`. `uniqueAxisA` is reserved
  for a future catalog extension and is rejected with `STR001`.
- `origin` is `none`, `originChoice1`, or `originChoice2`.

The Hermann–Mauguin symbol, Hall symbol, crystal system, operations, and catalog data version are derived from
the embedded catalog and are never persisted. An unknown or unsupported combination is `STR001`; the
diagnostic hint lists the allowed settings for the space-group number when the catalog can provide them.

## Cell parameters

- Lengths are in ångström and must be in the inclusive range `0.1..1000 Å` (`STR002`).
- Angles are in degrees and must be strictly between `0°` and `180°`.
- The Gram determinant must be positive and above the near-degeneracy tolerance.
- The cell-matrix condition number must not exceed `1e12`.
- Crystal-system constraints apply: monoclinic unique-axis angles, orthorhombic/tetragonal/cubic right angles,
  tetragonal/cubic equal lengths, trigonal/hexagonal `a=b` with `γ=120°`, and rhombohedral `a=b=c` with
  `α=β=γ<120°`.

Inconsistent values are rejected with `STR003`; the API never silently overwrites user input. A user interface
may lock dependent fields as a convenience, but the model validation is authoritative.

## Editing the space group and cell in the GUI

Open **Structure** from the **Model** menu or the project explorer's **Model** node. The structure document is a
form: a space-group section and a cell-parameter section, both on the shared editor validation, undo, and
read-only plumbing.

### Space group

- The search box matches the International Tables number, the Hermann–Mauguin symbol, the Hall symbol, the crystal
  system, the axis setting, and the origin choice.
- The **Settings** selector lists every catalog setting for the selected group number, so the origin choice and
  the unique-axis/rhombohedral setting are explicit.
- The details panel shows the operation count, the general multiplicity, the crystal system, and every operation
  in matrix/translation and human-readable form (for example `x, y, z` for the identity).
- Selecting a setting never edits the project. It builds a *candidate* structure and shows the current versus
  candidate orbit size for every reference site. Overlapping candidates report `STR007`; cell violations report
  `STR003`; an incompatible candidate disables **Apply**.
- **Apply** records one undo entry. **Cancel** discards the candidate and changes nothing.

### Cell parameters

- The six fields (`a`, `b`, `c` in Å; `α`, `β`, `γ` in degrees) accept the same numeric expressions as the other
  model editors: `+ - * / ^`, parentheses, `pi`/`e`, and `sqrt`/`ln`/`exp`/`abs`.
- Fields that the resolved crystal system fixes are locked and show the reason: right angles for
  orthorhombic/tetragonal/cubic, `a = b` for tetragonal/hexagonal, `a = b = c` for cubic/rhombohedral,
  `α = β = γ` for rhombohedral, the unique-axis right angles for monoclinic, and `γ = 120°` for hexagonal. The
  locks are an affordance; `CellGeometry` remains the validity authority.
- The live cell vectors and volume update as you type. An invalid length, angle, Gram determinant, condition
  number, or crystal-system constraint is shown with the violated inequality (for example “The cell angle α must
  be 90°, but was 80°”) and disables **Apply**.
- **Apply** records one undo entry; **Revert** reloads the committed cell.
- **Reset to cubic** (cubic structures only) and **Reset to defaults** ask for confirmation and are undoable. The
  defaults reset uses the current crystal-system locks, so it is valid for every system.

### Interaction with the space group

The cell form validates against the committed space group. Changing the space group first and then checking the
cell diagnostics is the safe order; the candidate preview already reports a candidate cell violation before it is
applied.

## Reference sites

A site stores a stable `CellSiteId`, a name, a fractional position, a particle-set reference, and a stability
flag.

- `stability` is `stable` or `unstable`. Stable sites may host particles and define interactions; unstable
  sites are transition states.
- A stable site defaults to the first particle in its particle set. An unstable site defaults to the implicit
  void.
- The stability value is written as `stable` or `unstable` in project JSON.
- Every site needs a particle set. The default value or `ParticleSetId.Empty` is `STR006`; an unknown
  identifier is `PROJ010`.
- Site names are unique case-insensitively and must not be empty (`STR005`).
- A structure may define at most 1000 reference sites (`STR012`).

### Fractional positions

`fractional` is a three-number array `[a, b, c]`, not an object:

```json
{ "fractional": [0.25, 0.25, 0.25] }
```

Before trimming, every component must be inside `[0,1)`. A component within `FractionalTrimTolerance`
(`1e-10`) of `0` or `1` snaps to the equivalent canonical boundary value; anything further outside the
interval is `STR004` and is not silently wrapped. The stored project value stays decimal; Symmetry converts it
to the exact `24 × 10^12` grid for P1 expansion and ordering.

### Orbit overlap

Two reference sites must not expand to the same P1 position. Overlap detection uses exact grid coordinates
after symmetry expansion, not a tolerance hash set. The structured encoder returns `OrbitOverlap` records and
emits `STR007` with a `CellSiteId` path to each offending site, so a UI can highlight both sites without
parsing message text. Duplicate detection compares the full orbit, not only the seed position.

## Dummy positions

Dummy positions are visual helpers. They may coincide with reference sites and are excluded from all
simulation compilation.

```json
{ "id": "…", "name": "Center", "fractional": [0.5, 0.5, 0.5] }
```

They use the same coordinate rule as sites; invalid coordinates are `STR010`. Dummy names are unique
case-insensitively and must not be empty (`STR015`).

## Structure preview

`StructurePreviewService` builds a deterministic, non-compiled preview for the UI and validation:

- resolve the space-group reference;
- validate the cell against the crystal system;
- expand the reference sites to the canonical P1 order;
- compute Cartesian positions and the cell volume;
- report per-site orbit size/multiplicity and stability;
- echo the requested supercell counts `(A, B, C)`.

The preview rejects a requested `A*B*C*P` product above the signed 32-bit linear-index cap with `STR014`.
More than 10,000 P1 positions emits the `STR009` warning, not an error. The preview is cached by the canonical
structure hash, the requested cell counts, and the catalog/contract versions. It is not the compiled model;
the [compilation pipeline](../development/compilation-pipeline.md) produces the authoritative compiled
structure.

See [Symmetry and Geometry](../physics/symmetry.md) for the P1 order, exact grid, and orbit rules.

## JSON example

```json
{
  "name": "Ceria",
  "spaceGroup": { "number": 225, "axis": "none", "origin": "none" },
  "cell": { "a": 5.41, "b": 5.41, "c": 5.41, "alpha": 90.0, "beta": 90.0, "gamma": 90.0 },
  "sites": [
    {
      "id": "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5f05",
      "name": "Ce",
      "fractional": [0.0, 0.0, 0.0],
      "particleSetId": "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5f03",
      "stability": "stable"
    }
  ],
  "dummyPositions": []
}
```

Canonical project JSON sorts sites and dummies by ID. The ceria and barium-zirconate samples under
`samples/` are generated by the real project writer and mirrored in `tests/contracts/`. The
barium-zirconate sample models partially hydrated Y-doped BaZrO₃: Y³⁺ shares the Zr site, the oxygen set
includes an oxygen vacancy, and one stable proton reference site expands to the twelve proton positions
around the three oxygen sites. The oxygen and proton sets share one uncharged vacancy particle; additional
vacancy particles are only needed for distinct charged vacancy states. The proton position is a stable defect
site; proton-hopping transitions between the symmetry-equivalent positions are a later transition-model step.

## Validation

| Code | Severity | Meaning |
| --- | --- | --- |
| `STR001` | Error | Space-group reference is not in the embedded catalog. |
| `STR002` | Error | A cell length is outside `0.1..1000 Å`. |
| `STR003` | Error | Cell angles, determinant, conditioning, or crystal-system constraints are invalid. |
| `STR004` | Error | A site is outside `[0,1)` beyond the trim tolerance, or is not finite. |
| `STR005` | Error | A site name is empty or duplicated case-insensitively. |
| `STR006` | Error | A site has no particle set or uses the reserved empty identifier. |
| `STR007` | Error | Two sites expand to the same P1 orbit position. |
| `STR009` | Warning | The structure exceeds the 10,000-position P1 performance budget. |
| `STR010` | Error | A dummy position is outside `[0,1)` beyond tolerance, or is not finite. |
| `STR012` | Error | More than 1000 reference sites. |
| `STR013` | Error | Structure name is empty or whitespace-only. |
| `STR014` | Error | The requested supercell exceeds the signed 32-bit index cap. |
| `STR015` | Error | A dummy name is empty or duplicated case-insensitively. |
| `PROJ010` | Error | A site references a particle set that does not exist. |
| `PROJ034` | Warning | Project symmetry metadata differs from the embedded catalog/contract versions. |

## Queries and editing

The immutable `Project` snapshot implements `IStructureQuery` with `Structure` and `Find(CellSiteId)`.
`ProjectEditor` provides `SetStructureName`, `SetSpaceGroup`, `SetCellParameters`, `AddCellSite`,
`UpdateCellSite`, `RemoveCellSite`, `AddDummyPosition`, `UpdateDummyPosition`, and `RemoveDummyPosition`.
The default editor uses Core validation; pass `ProjectValidationService.Validate` for catalog-dependent
space-group and cell checks. See [Particles and particle sets](particles.md) and the
[architecture guide](../development/architecture.md).
