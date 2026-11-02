# Step 02 — Symmetry and Geometry

**Phase:** A. Foundations  
**Depends on:** 01
**Delivers:** A validated space-group catalog, exact symmetry operations, coordinate systems, P1 expansion,
orbit/permutation reduction, and periodic geometry kernels.

## Goal

Provide the mathematical foundation for every model domain. The implementation must be exact where
crystallography is discrete, deterministic in its index assignment, and fast enough to expand large cells and
sample interaction environments.

## Original reference

- `ICon.Framework.Symmetry` (5,299 lines) contains the space-group SQLite database, symmetry operations,
  crystal systems, coordinate systems, orbit generation, point groups, and radial sampling.
- The database has 230 international groups, **274 settings**, and **5,420 operations**. It includes origin
  choices, monoclinic unique-axis variants, and rhombohedral/hexagonal axis variants.
- Known defects in the original data/implementation:
  - group 47 (`Pmmm`) is structurally invalid (degenerate z-row, missing identity/inversion);
  - translations are not normalized modulo 1;
  - origin choice is encoded only as a free-text `Specifier` string;
  - the triclinic/rhombohedral cell-vector formula omits a factor `c` in the C-vector y-component;
  - tolerance comparers are used in sorted dictionaries/binary searches, which can break transitivity;
  - `Vector4I.P` assignment depends on DB operation order and tolerance dedup, making indices fragile.

The new implementation keeps the crystallographic data but fixes the schema, uses exact integer operations,
and defines a canonical position order.

## Design

### 1. Space-group catalog

```csharp
public sealed record SpaceGroupSetting(
    int Number,                    // 1..230
    string HermannMauguin,
    string HallSymbol,
    CrystalSystem System,
    AxisSetting Axis,              // None, UniqueA/B/C, Hexagonal, Rhombohedral
    OriginChoice Origin,           // None, One, Two
    ImmutableArray<SymmetryOperation> Operations,
    string DataVersion);
```

- The catalog is an **embedded immutable resource** generated at build time (JSON or a compact binary), not a
  runtime SQLite database.
- Data source: a vetted crystallographic dataset (International Tables / spglib). The original 274 settings
  are used as a cross-check and to preserve the setting names users expect. All 230 groups must be present;
  missing settings are a build error.
- The catalog is validated at startup and by tests:
  - identity present,
  - determinant of the linear part is ±1,
  - every operation has an inverse in the group,
  - closure under composition modulo lattice translations,
  - translations normalized to `[0, 1)`.
- Group 47 is corrected from the authoritative source; a regression test asserts the corrected operations.
- The catalog exposes lookup by number + axis + origin, by Hall symbol, and by Hermann–Mauguin symbol.

### 2. Exact symmetry operations

```csharp
public readonly record struct SymmetryOperation(
    Int3x3 Linear,          // signed integer matrix
    Rational3 Translation); // numerator/denominator 24, normalized
```

- Use integer arithmetic for composition, inversion, and closure.
- Denominator 24 covers all crystallographic translations (1/2, 1/3, 1/4, 1/6).
- Conversion to `double` happens only at the geometry boundary.
- Provide:
  - `Transform(Fractional3)`,
  - `Compose(other)`, `Inverse()`, `Determinant`, `FlipsOrientation`,
  - `NormalizeTranslation()`,
  - `ToAffineMatrix()`.
- Optionally parse `"x,y,z"` notation for diagnostics/tests, but the catalog stores numeric operations.

### 3. Crystal systems and cell parameters

- Seven systems with the same constraints as the original:
  - triclinic: no fixed angles;
  - monoclinic: one non-90° angle depending on unique axis;
  - orthorhombic: all 90°;
  - tetragonal: a=b, all 90°;
  - trigonal/hexagonal: a=b, α=β=90°, γ=120° (hexagonal axes) or a=b=c, α=β=γ (rhombohedral axes);
  - cubic: a=b=c, all 90°.
- Valid angles satisfy the four strict triangle inequalities.
- Length range 0.1–1000 Å (warning at the original bounds).
- **Correct cell-vector formula**:

```text
A = (a, 0, 0)
B = (b cos γ, b sin γ, 0)
C = (c cos β, c (cos α − cos β cos γ) / sin γ,
     sqrt(c² − Cx² − Cy²))
```

- Test against known cells for all seven systems, including a triclinic cell where the original formula fails.
- Keep parameter validation tolerance (1e-3) separate from geometric equality (1e-6) and trim tolerance (1e-10).

### 4. Coordinate types and encoding

```csharp
public readonly record struct Fractional3(double A, double B, double C);
public readonly record struct Cartesian3(double X, double Y, double Z);
public readonly record struct Vector4I(int A, int B, int C, int P);
```

- `Fractional3.TrimToUnitCell()` maps to `[0,1)` with a boundary policy that rounds values within `1e-10` of
  an integer to that integer.
- `Vector4I` encodes `(cell offset A,B,C, P1 position index P)`.
- Linear index is row-major with `P` fastest:
  `index = P + C*Pcount + B*Pcount*Ccount + A*Pcount*Ccount*Bcount`.
- `UnitCellVectorEncoder` maps fractional ↔ `Vector4I` and owns the canonical P1 position list.
- **Canonical position order** (contract for compiled packages):
  1. expand every reference site to its P1 orbit;
  2. trim to `[0,1)`;
  3. deduplicate by exact rational coordinates;
  4. sort lexicographically by `(A, B, C)`;
  5. tie-break by the reference site's stable ID, then by orbit member index.
- The ordering is independent of input collection order and operation order. Golden tests freeze the P index
  for every reference site in the sample models.
- `Vector4I` decoding validates `P` against the position count; compiled data is bounds-checked on load.

### 5. Orbit and point-group algorithms

- `ExpandOrbit(SpaceGroupSetting, Fractional3) -> ImmutableArray<Fractional3>`:
  apply every operation, trim, deduplicate exactly, sort canonically.
- `GetStabilizer(setting, site) -> ImmutableArray<SymmetryOperation>`:
  operations that map the site to itself modulo lattice translation.
- `GetPointGroup(setting, center, geometry) -> PointGroup`:
  - stabilizer operations,
  - unique sequence images under the stabilizer,
  - order-preserving vs order-ignoring sets,
  - self-projection orders.
- Pair reduction:
  - enumerate periodic images within the cutoff;
  - group by exact distance bucket (integer femtometres or a documented tolerance);
  - for the first unassigned candidate, apply the site stabilizer and mark all equivalent candidates;
  - the representative carries the distance and geometry;
  - detect chiral partners (the inverse direction is not in the orbit) and link them.
- Cluster/group reduction:
  - build the point group for the center + surrounding geometry;
  - enumerate occupation permutations;
  - canonicalize under the self-projection orders;
  - produce unique occupation states and the expansion map back to all equivalent states.
- Use integer particle masks (`ulong`) for occupation sets; never `1 << id` on a 32-bit `int`.
- Complexity targets: orbit expansion `O(|G| log |G|)`; pair reduction `O(C · |G|)` after distance bucketing;
  cluster reduction `O(P · |G|)` where `P` is the raw permutation count, with early pruning for large groups.

### 6. Periodic geometry

- `RadialSearch(center, cutoff)` enumerates unit-cell images whose Cartesian distance is within the cutoff.
  Use a bounding box derived from the cell vectors; do not scan an arbitrary number of cells.
- Precompute Cartesian positions for all P1 positions and cell offsets used by a job.
- Distance equality uses a documented tolerance (e.g. 1e-5 Å) but distance *bucketing* uses integer
  femtometres to avoid non-transitive comparers.
- Minimum-image handling is explicit: callers receive the image offset; no implicit wrapping that loses
  information.
- Provide `GetPositionsInBox`, `GetCellVolume`, `ToCartesian`, `ToFractional`, and `GetDistance`.

### 7. Caching and thread safety

- `SpaceGroupCatalog` is immutable and thread-safe.
- Orbit and point-group results are cached in a `ConcurrentDictionary` keyed by
  `(settingId, siteId, geometryHash)`.
- No mutable global loaded group; the setting is passed explicitly.
- Compilation may expand positions in parallel; all caches are concurrent and deterministic.

## Implementation tasks

- [ ] Add the space-group data source and a build-time generator for the embedded catalog.
- [ ] Implement `SymmetryOperation` with integer composition/inverse/closure.
- [ ] Implement `SpaceGroupSetting`, `SpaceGroupCatalog`, and lookup APIs.
- [ ] Implement crystal-system constraints and the corrected cell-vector transform.
- [ ] Implement `Fractional3`, `Cartesian3`, `Vector4I`, and the encoder.
- [ ] Implement canonical P1 expansion and position ordering.
- [ ] Implement stabilizer/point-group/permutation reduction.
- [ ] Implement radial sampling and periodic geometry helpers.
- [ ] Add golden tests for all 230 groups/274 settings.
- [ ] Document the space-group data version and index-order contract.

## Acceptance criteria

- [ ] All 230 international groups and 274 settings load.
- [ ] Every setting passes identity, inverse, closure, determinant, and translation-normalization checks.
- [ ] Group 47 is valid and matches the authoritative setting.
- [ ] General-position multiplicities match the International Tables for every setting.
- [ ] P1 expansion of every sample site matches a brute-force expansion.
- [ ] The canonical position order is stable across runs, OSes, and input ordering.
- [ ] The triclinic cell test produces the correct C vector and angles.
- [ ] Pair and cluster reductions match brute-force orbit enumeration for random geometries.
- [ ] A 20×20×20 cell's position expansion completes in under 1 second.
- [ ] Documentation written

## Documentation

At this point, document the symmetry and geometry foundation:

- `docs/physics/symmetry.md` — space groups, exact operations, cell geometry, P1 expansion, canonical position order.
- `docs/development/compilation-pipeline.md` — the symmetry stage and its inputs/outputs.
- `docs/glossary/notation.md` — fractional/Cartesian coordinates and `Vector4I`.
- `docs/reference/project-format.md` — space-group/cell/site fields and the data-version contract.

## Tests

| Test | Assertion |
| --- | --- |
| Catalog integrity | 230 groups, 274 settings, expected operation counts |
| Group laws | Identity, inverse, closure, determinant ±1 for all settings |
| Orbit sizes | General and special positions match known multiplicities |
| Canonical order | Shuffled sites/operations produce the same `P` assignment |
| Coordinate round-trip | Fractional ↔ `Vector4I` ↔ fractional for random points |
| Cell geometry | Seven systems, including the triclinic regression |
| Pair reduction | Reduced set equals brute-force orbit for random pairs |
| Cluster reduction | Unique permutations equal brute-force canonicalization |
| Radial search | Distances match brute-force enumeration of neighboring cells |
| Thread safety | Parallel expansion produces identical results |

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Space-group data errors | Cross-check two authoritative sources; validate group laws; freeze a checksum |
| Exact arithmetic performance | Use small fixed-size structs and integer ops; convert to double only at boundaries |
| Canonical order changes later | Treat as a format contract; version the catalog and add golden tests |
| Tolerance bugs | Use exact/rational comparisons for discrete symmetry; reserve tolerances for geometry |
| Large orbit/permutation counts | Bound counts, warn in validation, use pruning and parallelism |

## Done when

The symmetry engine is exact, validated for all 230 groups, produces deterministic P1 indices, and passes
brute-force equivalence tests for orbits, pairs, and clusters.
