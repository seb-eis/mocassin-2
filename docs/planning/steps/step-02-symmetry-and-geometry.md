# Step 02 — Symmetry and Geometry

**Phase:** A. Foundations  
**Depends on:** 01
**Delivers:** A validated space-group catalog, exact symmetry operations, coordinate systems, P1 expansion,
orbit/permutation reduction, and periodic geometry kernels.

## Implementation status

- **Status:** In progress
- **Step branch:** `step/02-symmetry-and-geometry`
- **Step CI:** —
- **Main merge:** —
- **Step tag:** —
- **Audit:** Not requested
- **Design review:** Ready with notes — accepted by the human owner on 2026-11-07
- **Last updated:** 2026-11-07

| Feature | Branch | Status | Local gates | Step CI | Squash commit |
| --- | --- | --- | --- | --- | --- |
| `core-geometry` | `feature/step-02-symmetry-and-geometry/core-geometry` | Squashed | Core tests 158/158; `dotnet format --verify-no-changes` passes | [`42395501397`](https://github.com/seb-eis/mocassin-2/actions/runs/42395501397) — green | `e6f5e1d` |
| `exact-grid` | `feature/step-02-symmetry-and-geometry/exact-grid` | Squashed | Symmetry tests 34/34; full solution tests pass; `dotnet format --verify-no-changes` passes | [`42396745263`](https://github.com/seb-eis/mocassin-2/actions/runs/42396745263) — green | `04e3213` |
| `space-group-catalog` | `feature/step-02-symmetry-and-geometry/space-group-catalog` | Squashed | Symmetry tests 60/60; generator `--check` current; full solution tests pass; `dotnet format --verify-no-changes` passes | [`42399290576`](https://github.com/seb-eis/mocassin-2/actions/runs/42399290576) — green | `24f1b12` |
| `cell-geometry` | `feature/step-02-symmetry-and-geometry/cell-geometry` | Squashed | Symmetry tests 74/74; full solution tests pass; `dotnet format --verify-no-changes` passes | [`42404289388`](https://github.com/seb-eis/mocassin-2/actions/runs/42404289388) — green | `549a03a` |
| `p1-encoder` | `feature/step-02-symmetry-and-geometry/p1-encoder` | Awaiting approval | Symmetry tests 87/87; full solution tests pass; `dotnet format --verify-no-changes` passes | — | — |

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

The new implementation uses the same crystallographic definitions but regenerates the catalog from spglib,
fixes the schema, uses exact integer operations, and defines a canonical position order.

## Design

### 1. Space-group catalog

```csharp
public sealed record SpaceGroupSetting(
    int Number,                    // 1..230
    string HermannMauguin,
    string HallSymbol,
    CrystalSystem System,
    AxisSetting Axis,              // None, UniqueAxisA/B/C, Hexagonal, Rhombohedral
    OriginChoice Origin,           // None, OriginChoice1, OriginChoice2
    ImmutableArray<SymmetryOperation> Operations);

public enum AxisSetting
{
    None,
    UniqueAxisA,
    UniqueAxisB,
    UniqueAxisC,
    Hexagonal,
    Rhombohedral,
}

public enum OriginChoice
{
    None,
    OriginChoice1,
    OriginChoice2,
}
```

The enum member names are chosen so the default camelCase serializer emits the contract strings
`none`, `uniqueAxisB`, `uniqueAxisC`, `hexagonal`, `rhombohedral`, `originChoice1`, and `originChoice2`.
`uniqueAxisA` is a reserved enum value only and is rejected by the v1 project validator.
The catalog-level `dataVersion` is stored in the catalog root and compiled payload; it is not repeated per
setting.

`AxisSetting` and `OriginChoice` are persisted Core vocabulary types. `CrystalSystem` is Core vocabulary but
catalog-derived and never serialized. `SpaceGroupSetting` and `SymmetryOperation` are Symmetry types. The
catalog assembly may reference Core, but Core must not reference the catalog assembly.

The project persists a `SpaceGroupRef(number, axis, origin)`. `SpaceGroupSetting` is the resolved catalog
value used by previews and compilation; its Hermann–Mauguin symbol, Hall symbol, crystal system, operations,
and data version are not persisted in the project.

- The catalog is an **embedded immutable resource** generated from a pinned source (JSON or a compact binary),
  not a runtime SQLite database. The generated data is committed with its checksum and provenance; normal
  builds do not require the source database or a Python toolchain.
- **Source:** spglib, pinned to an exact release and commit. spglib is BSD-3-Clause and distributes the
  space-group database. International Tables is a scientific reference, not a copied data source. The original
  MOCASSIN SQLite database is GPL-3.0-or-later and is never copied; it is only a local cross-check.
- The v1 pin is `spglib==2.8.0`; the exact commit is recorded in `Data/README.md` at generation and asserted
  by the generator.
- Startup validation is cheap and fail-fast:
  - the embedded checksum and schema version match;
  - 230 group numbers and 274 settings are present;
  - identity is present, every determinant is ±1, and every translation is normalized to `[0, 1)`.
- Test/CI validation is exhaustive:
  - every operation has an inverse in the group;
  - closure under composition modulo lattice translations;
  - group-law and orbit property tests;
  - golden counts for all 230 groups and 274 settings.
- A full startup validation may be enabled in Debug builds only.
- Group 47 is corrected from spglib; the regression test expects the full `Pmmm` operation set (8 operations,
  identity and inversion present, no degenerate row) rather than the original 4-operation set.
- The catalog exposes lookup by number + axis + origin, by Hall symbol, and by Hermann–Mauguin symbol. A
  Hermann–Mauguin lookup may return multiple settings; callers must disambiguate with axis/origin.

#### 274-setting selection

The compatibility catalog contains exactly 274 settings:

| Category | spglib selection | Count |
| --- | --- | ---: |
| Standard setting | `choice = ""` for the 186 groups with a single standard setting | 186 |
| Origin choices | `choice = "1"` and `"2"` for the 24 groups with two origins | 48 |
| Monoclinic unique axis B/C | `choice = "b"` or `"b1"` for unique axis B, and `"c"` or `"c1"` for unique axis C, for the 13 monoclinic groups | 26 |
| Rhombohedral axes | `choice = "H"` and `"R"` for the 7 rhombohedral groups | 14 |
| **Total** | | **274** |

- Apply the category by group type so each group contributes exactly one category. The generator asserts
  186 + 48 + 26 + 14 = 274 and that all 230 group numbers are present.
- Primitive monoclinic groups 3, 4, 6, 10, and 11 use `b`/`c`; cell-choice groups 5, 7–9, and 12–15 use
  `b1`/`c1`. The generator asserts exactly 26 monoclinic settings and two selected settings per monoclinic
  group.
- Do not include unique axis A, monoclinic cell choices 2/3, orthorhombic axis permutations, or other
  non-standard settings in v1. They are a documented extension; adding them requires a `CellChoice`/`Choice`
  field and a project-format decision.
- `AxisSetting.UniqueAxisA` remains in the enum for future extension but is not populated by the 274
  compatibility catalog. The v1 project validator rejects `uniqueAxisA` with `STR001`.
- `DataVersion = spglib-2.8.0/catalog-1`; `catalog-1` is the generator/schema version and is bumped when the
  selection rule, encoding, or schema changes.
- The SHA-256 checksum covers the canonical settings payload, not the whole file and not a timestamp. The
  committed JSON is compact because it is machine-consumed and hashed, not hand-reviewed; the checksum is
  computed over the canonical settings payload.
- The loader recomputes the checksum at startup and treats a mismatch as a build/installation error.
- The original database may be used locally to confirm the expected count and setting-name categories. It is
  not a CI dependency, is not copied, and is not used to generate catalog data.

#### Generation and packaging

- The generated catalog is committed; normal .NET builds do not run Python, spglib, or any data download.
- Layout:
  - `src/dotnet/Mocassin.Symmetry/Data/space-groups.json` — committed catalog using the compact positional
    encoding;
  - `src/dotnet/Mocassin.Symmetry/Data/space-groups.sha256` — checksum of the canonical settings payload;
  - `src/dotnet/Mocassin.Symmetry/Data/README.md` — provenance, selection rule, regeneration command, and
    known deviations;
  - `tools/symmetry/generate_space_groups.py` — dev-only generator;
  - `tools/symmetry/requirements.txt` — pinned `spglib==2.8.0`.
- The JSON is UTF-8 without BOM, LF-only, deterministic compact JSON, and contains no timestamps. The root
  metadata is `schemaVersion`, `dataVersion`, `source`, `sourceVersion`, `selection`, and `settings`.
- Settings are sorted by `(number, axis, origin)`. Each setting is the positional array
  `[number, hermannMauguin, hallSymbol, crystalSystem, axis, origin, operations]`; each operation is
  `[a11, a12, a13, a21, a22, a23, a31, a32, a33, tA, tB, tC]`. Translation numerators are exact over the
  fixed denominator 24 and normalized to `[0, 24)`. The checksum uses the canonical settings payload.
- Operations within a setting are sorted canonically by linear matrix row-major (`a11..a33`), then by the
  three normalized translation numerators (denominator 24). This fixes the checksum across spglib iteration
  order.
- The generator asserts 230 group numbers, 274 settings, the 186/48/26/14 category split, no duplicate
  `(number, axis, origin)`, determinant ±1, normalized translations, and group 47's 8-operation `Pmmm` set.
- The catalog DTOs are internal to `Mocassin.Symmetry`. Use a `System.Text.Json` source-generated
  `JsonSerializerContext` so trimming/AOT remains viable and the DTOs do not become public API.
- CI drift check: the existing Contracts and policy job installs the pinned requirements and runs
  `python tools/symmetry/generate_space_groups.py --check`. The check regenerates the canonical payload in
  memory and fails if the committed settings or checksum differ.
- The compiled package records `space_group_data_version`, `space_group_data_sha256`, and
  `symmetry_contract_version` in its metadata. The project envelope may also carry optional advisory metadata
  (`spaceGroupDataVersion`, `spaceGroupDataSha256`, `symmetryContractVersion`); a mismatch with the embedded
  values emits `PROJ034` warning. The compiled payload remains the authoritative build record.

### 2. Exact symmetry operations

```csharp
public readonly record struct SymmetryOperation(
    Int3x3 Linear,          // signed integer matrix
    Rational3 Translation); // numerator/denominator 24, normalized
```

- Use integer arithmetic for composition, inversion, and closure.
- Denominator 24 covers all crystallographic translations (1/2, 1/3, 1/4, 1/6).
- Apply operations to `GridFractional3`; because the grid denominator is a multiple of 24, every result
  remains on the grid.
- Conversion to `double` happens only at the geometry boundary; the discrete path stays on `GridFractional3`.
- Provide:
  - `Transform(Fractional3)`,
  - `Compose(other)`, `Inverse()`, `Determinant`, `FlipsOrientation`,
  - `NormalizeTranslation()`,
  - `ToAffineMatrix()`.
- Optionally parse `"x,y,z"` notation for diagnostics/tests, but the catalog stores numeric operations.

### 3. Crystal systems and cell parameters

- Seven crystal systems:
  - triclinic: no fixed lengths or angles;
  - monoclinic: unique axis B/C maps to β/γ; the other two angles are 90°. Unique axis A is reserved for a
    future catalog extension and is rejected in v1;
  - orthorhombic: α=β=γ=90°;
  - tetragonal: a=b; α=β=γ=90°;
  - trigonal, hexagonal axes: a=b; α=β=90°; γ=120°;
  - trigonal, rhombohedral axes: a=b=c; α=β=γ; 0<α<120°;
  - hexagonal: a=b; α=β=90°; γ=120°;
  - cubic: a=b=c; α=β=γ=90°.
- General angle validity:
  - 0 < α, β, γ < 180°;
  - the Gram determinant is strictly positive:
    `det G = 1 − cos²α − cos²β − cos²γ + 2 cosα cosβ cosγ > 0`;
  - the equivalent spherical triangle inequalities are
    `α+β+γ < 360°`, `α+β > γ`, `α+γ > β`, and `β+γ > α`;
  - near-degenerate cells below `CellDeterminantTolerance` are rejected.
- Length limits: 0.1–1000 Å. Values outside are `STR002` errors; there is no separate warning band in v1.
- **Correct, numerically stable cell-vector formula**:

```text
det G = 1 − cos²α − cos²β − cos²γ + 2 cosα cosβ cosγ

A = (a, 0, 0)
B = (b cos γ, b sin γ, 0)
C = (c cos β,
     c (cos α − cos β cos γ) / sin γ,
     c sqrt(det G) / sin γ)
```

- Validate `det G > 0` before taking the square root. The positive z-component makes the cell right-handed.
- Volume is `a b c sqrt(det G)`.
- The `Cy` term keeps the factor `c`; that is the original defect.
- Tolerances are named, unit-specific constants:
  - `CellLengthTolerance = 1e-3 Å` for a/b/c equality and length checks;
  - `CellAngleTolerance = 1e-3°` for 90°/120° checks and rhombohedral equality;
  - `CellDeterminantTolerance = 1e-12` for the near-degenerate cutoff;
  - `GeometricEqualityTolerance = 1e-6 Å` for Cartesian distance/vector equality;
  - `FractionalTrimTolerance = 1e-10` for boundary snapping before exact grid conversion.
- `CellConditionLimit = 1e12`; a cell whose condition number `||M||∞ · ||M⁻¹||∞` exceeds it is rejected with
  `STR003`, even when `det G` is above the determinant cutoff.
- Cell-parameter inconsistency is rejected with `STR003`; the API never silently overwrites user input. The
  UI may lock dependent fields as an editor convenience.
- Test all seven systems, both trigonal axis descriptions, unique-axis B/C, near-degenerate rejection, volume,
  right-handedness, and fractional ↔ Cartesian round-trips.
- Triclinic regression fixture: `a=5 Å`, `b=6 Å`, `c=7 Å`, `α=80°`, `β=85°`, `γ=95°`; compare the vectors
  and volume against an independent determinant calculation.

### 4. Coordinate types and encoding

```csharp
// Mocassin.Core.Geometry
public readonly record struct Fractional3(double A, double B, double C);
public readonly record struct Cartesian3(double X, double Y, double Z);
public readonly record struct Vector4I(int A, int B, int C, int P);
```

The primitive coordinate types live in **`Mocassin.Core.Geometry`** so the domain model can use them without
making Core depend on Symmetry. `AxisSetting` and `OriginChoice` are persisted vocabulary; `CrystalSystem` is
catalog-derived vocabulary. They also live in Core because `SpaceGroupRef` and `CellParameters` are part of the
project model.
`SpaceGroupSetting`, `SymmetryOperation`, the space-group catalog, the unit-cell encoder, and the symmetry
algorithms remain in `Mocassin.Symmetry`.

Core must never reference `Mocassin.Symmetry`. Catalog-dependent previews and validation are Symmetry
services that accept Core model types and return Symmetry or diagnostic results.

#### Generic math strategy

Modern C# generic math is useful for the arithmetic kernels, but a vector is not a scalar number. Do **not**
constrain `Fractional3`, `Cartesian3`, or `Vector4I` themselves to `INumber<T>`: `INumber<T>` implies scalar
comparison, parsing, formatting, and numeric semantics that do not apply to a coordinate vector.

Use this split instead:

- **Public types stay semantic and non-generic.** `Fractional3` and `Cartesian3` are distinct
  `readonly record struct` types with `double` components. They preserve coordinate-space type safety and
  make unit/coordinate mistakes visible at compile time.
- **Keep `Vector4I` separate.** Its `P` component is a position index, not a spatial coordinate. It is an
  encoding type, not a generic vector.
- **Share arithmetic through an internal generic kernel**, e.g.
  `internal readonly record struct Vector3<T>(T X, T Y, T Z) where T : unmanaged, INumber<T>`, or a static
  `VectorMath<T>` helper. Public wrappers delegate to it for add/subtract/scale/dot/cross/length.
- **Do not use `System.Numerics.Vector3` for geometry.** It is single-precision `float`; scientific
  coordinates, distances, and symmetry checks require `double`.
- **Generic math is an implementation detail, not a public format or API contract.** Persisted fields remain
  `double`/`int` arrays.

This avoids duplicating arithmetic while retaining readable, unit-aware public APIs. If the project later
needs `float` or `decimal` geometry variants, the internal kernel can be instantiated for them; the public
scientific model remains `double`-based.

- `Fractional3.TrimToUnitCell()` maps to `[0,1)` with a boundary policy that rounds values within `1e-10` of
  an integer to that integer. This boundary snap is a user-input convenience applied before grid conversion;
  it is not the deduplication criterion and may move a point by many grid steps. Near-boundary P-order and
  overlap tests are required.
- `Vector4I(cellA, cellB, cellC, P)` encodes an integer cell offset and the per-cell canonical P1 position
  index. `P` is in `[0, Pcount)`; base P1 positions use `(0, 0, 0, P)`.
- The linear index is row-major with `P` fastest:
  `index = P + cellC*Pcount + cellB*Pcount*Ccount + cellA*Pcount*Ccount*Bcount`.
- The linear index is a signed 32-bit `int`. The total lattice position count
  `Acount * Bcount * Ccount * Pcount` is computed in 64-bit arithmetic and must not exceed `int.MaxValue`
  (2,147,483,647). A larger lattice is rejected with a hard diagnostic; it is never wrapped or truncated.
- Negative cell offsets are relative-path coordinates. They are not linear indices; convert them to an
  explicit in-supercell offset before linearizing.
- `UnitCellVectorEncoder` maps fractional ↔ `Vector4I`, owns the canonical P1 position list, validates `P` and
  cell-offset bounds, and is immutable and thread-safe.
- **Canonical position order** (contract for compiled packages):
  1. expand every reference site to its P1 orbit;
  2. trim to `[0,1)`;
  3. deduplicate by exact grid coordinates;
  4. sort ascending by fractional `(fa, fb, fc)`, with `fa` most significant;
  5. assign `P = 0..Pcount-1` in that order.
- Exact ties should not occur after deduplication and overlap rejection. If one occurs, order by the reference
  site's stable ID, then by orbit member index; this is a defensive determinism rule, not a normal path.

#### Exact fractional grid

The discrete symmetry path does not use binary floating point. `Mocassin.Symmetry` converts each
`Fractional3` to an internal `GridFractional3` with three `long` components scaled by
`D = 24 × 10^12 = 24,000,000,000,000`.

- `24` is divisible by every crystallographic translation denominator in the catalog: 2, 3, 4, 6, 8, 12,
  and 24.
- `10^12` preserves any input decimal with up to 12 fractional digits exactly.
- An integer rotation matrix and a translation `m/24` applied to a grid point produce another grid point:
  `R·x + t` is exactly closed on the grid.
- Conversion uses the canonical shortest round-trip decimal representation of the `double`, parsed with
  invariant culture and `decimal`, scaled by `D`, and rounded with `MidpointRounding.AwayFromZero`. Do not
  multiply the binary `double` directly, because that is platform-sensitive near grid boundaries.
- Non-finite `Fractional3` components (`NaN`, `+Inf`, `-Inf`) are rejected with a structured validation
  diagnostic; they are never converted.
- A finite magnitude below half a grid step (`0.5 / D`) underflows to zero. Values below `decimal`'s
  representable range are therefore zero, not an error.
- `-0.0` is normalized to `0.0`.
- Values with more than 12 fractional digits are rounded to the nearest grid point. A warning diagnostic
  (assigned in Step 07) is emitted when the difference exceeds half a grid step.
- Orbit expansion, trimming, deduplication, sorting, `Vector4I` encoding, and site-overlap detection use the
  grid. Cartesian positions, distances, cell vectors, and radial search remain `double`.
- The public and persisted `Fractional3` remains `double`-based; `GridFractional3` is internal to
  `Mocassin.Symmetry`. The project format does not change.
- The grid denominator and conversion rule are part of the canonical position-order contract. Changing either
  bumps `SymmetryContractVersion` (not `catalog-N`) and updates golden P-index tests and signatures.
  `catalog-N` changes only when the catalog payload, selection, or encoding changes.

- The ordering is independent of input collection order and operation order. Golden tests freeze the P index
  for every reference site in the sample models.
- `Vector4I` decoding validates `P` against the position count; compiled data is bounds-checked on load.

### 5. Orbit, stabilizer, and reduction algorithms

#### Group terms

For a center site `c` and an ordered site sequence `S = (s0, ..., sN-1)`:

- `G0` is the **center stabilizer**: operations that map `c` to itself modulo lattice translation.
- `Gset` is the **setwise stabilizer**: operations in `G0` that map the unordered set `{S}` onto itself.
- `Gseq` is the **pointwise stabilizer**: operations in `G0` that fix every `si` individually.
- `Gseq` is normal in `Gset`, and `Gseq ⊆ Gset ⊆ G0`.
- `P ≅ Gset / Gseq` is the **induced permutation group** on the site indices. Its elements are the **induced
  site permutations**.

The old implementation names map as follows:

- `OrderPreservingUniqueSequenceOperations` = a transversal of `G0 / Gseq` (one operation per distinct ordered
  sequence).
- `OrderIgnoringUniqueSequenceOperations` = a transversal of `G0 / Gset` (one operation per distinct unordered
  set).
- `OrderIgnoringSelfProjectionOperations` = `Gset`.
- `SelfProjectionOrders` = the induced site permutations in `P`.

Use the standard terms in code and documentation. Keep the old names only in the glossary as parity aliases.

```csharp
public sealed record PointGroup(
    ImmutableArray<SymmetryOperation> CenterStabilizer,    // G0
    ImmutableArray<SymmetryOperation> SetwiseStabilizer,   // Gset
    ImmutableArray<SymmetryOperation> PointwiseStabilizer, // Gseq
    ImmutableArray<SitePermutation> InducedPermutations);  // P
```

`SitePermutation` is an immutable index mapping; the identity is `[0..N-1]`.

#### Orbit expansion

- `ExpandOrbit(SpaceGroupSetting, Fractional3) -> ImmutableArray<Fractional3>`:
  apply every operation, trim, deduplicate on the exact grid, sort canonically.
- `GetStabilizer(setting, site) -> ImmutableArray<SymmetryOperation>`:
  return `G0`.
- `GetPointGroup(setting, center, geometry) -> PointGroup`:
  return `G0`, `Gset`, `Gseq`, and `P` for the ordered geometry sequence. Expose:
  - the ordered orbit `G0·S`, with `|G0·S| = |G0| / |Gseq|`;
  - the set orbit `G0·{S}`, with `|G0·{S}| = |G0| / |Gset|`;
  - the induced site permutations `P`.

#### Pair reduction

- Enumerate periodic images within the cutoff; exclude zero-distance self-images.
- Compute the distance and convert it to an integer femtometre bucket.
- Sort candidates deterministically by `(distanceBucket, targetP, cellA, cellB, cellC)`.
- Take the first unassigned candidate as the shell representative.
- Apply `G0`; mark every candidate in the same distance bucket and the same `G0` orbit as assigned.
- The shell records its representative, distance, and equivalent-offset list.
- Chirality: compute the inverse vector `-d`. If the inverse is not in the same `G0` orbit, the shell is
  chiral; find the representative of the inverse orbit and link the two shells as chiral partners.
  - This is an orbit relationship, not physical chirality. In P1, every directed pair is its own chiral orbit
    and both directions must be represented.
- Pair shells are cached by `(catalogDataVersion, settingKey, centerP, cutoffBucket, cellParametersHash)`.
- The pair-shell order `(distanceBucket, targetP, cellA, cellB, cellC)`, then center/partner site indices, is
  the canonical pair-table order.

#### Cluster reduction

- Build `G0`, `Gset`, `Gseq`, and `P` for the center and ordered cluster sequence.
- Enumerate allowed occupation tuples from the per-site particle sets; prune empty or duplicate occupations
  according to the model rules.
- Compute each tuple's orbit under `P`; choose the lexicographically smallest tuple in site order as the
  canonical representative.
- Store the canonical tuple, its occupation code, and the expansion map to every equivalent tuple/permutation.
- Encode the tuple as a `ulong`: 6 bits per site, site 0 in the least significant bits, up to 9 sites (54 bits),
  unused high bits zero.
- Preserve chiral distinctions; do not merge mirror tuples unless the mirror permutation is in `P`.
- Cluster results are cached by
  `(catalogDataVersion, settingKey, centerP, clusterOffsetsHash, allowedOccupationHash)`.

- Use integer particle masks (`ulong`) for occupation sets; never `1 << id` on a 32-bit `int`.
- Complexity targets: orbit expansion `O(|G| log |G|)`; pair reduction `O(C · |G0|)` after distance bucketing;
  cluster reduction `O(T · |P|)` where `T` is the number of allowed occupation tuples, with early pruning.

### 6. Periodic geometry

#### Cell precomputation

- Precompute the cell matrix `M` with columns `a, b, c`, its inverse `M⁻¹`, the metric tensor `G = MᵀM`, and
  the volume `V = a·(b×c)`.
- Precompute only the base P1 Cartesian positions (`Pcount` values). Do not allocate a dense `A×B×C×P`
  Cartesian array.
- A neighbor at cell offset `(cellA, cellB, cellC)` is
  `baseCartesian[P] + cellA·a + cellB·b + cellC·c`.
- Fractional-to-Cartesian: `x = M·f`; Cartesian-to-fractional: `f = M⁻¹·x`.
- Distance for fractional difference `df`: `d² = dfᵀ G df`.

#### Minimum image and radial search

```csharp
public readonly record struct MinimumImageResult(
    Vector4I Offset,
    double DistanceAngstrom,
    int DistanceBucketFm);

public readonly record struct NeighborImage(
    Vector4I Offset,
    double DistanceAngstrom,
    int DistanceBucketFm);

public MinimumImageResult GetMinimumImage(Vector4I center, Vector4I target);
public ImmutableArray<NeighborImage> RadialSearch(Vector4I center, double cutoffAngstrom);
```

- `GetMinimumImage` returns exactly one shortest periodic image and always carries its offset.
- `RadialSearch` returns every image within the cutoff, including multiple images of the same `P` index.
- Neither API wraps implicitly; callers always receive the image offset.

#### Bounding box

Use the interplanar spacings:

```text
d_a = V / |b × c|
d_b = V / |c × a|
d_c = V / |a × b|
```

For search radius `r`, enumerate:

```text
|cellA| ≤ ceil(r / d_a) + 1
|cellB| ≤ ceil(r / d_b) + 1
|cellC| ≤ ceil(r / d_c) + 1
```

The extra cell covers the center’s position inside its cell and cutoff-boundary candidates. For minimum image,
use a safe covering-radius bound:

```text
R_cover ≤ 0.5 · (|a| + |b| + |c|)
```

Enumerate that box and choose the shortest image. Test against brute force for highly skewed triclinic cells; a
reduced/Niggli basis can be added later as an optimization.

#### Distance buckets and tolerances

- `PairDistanceBucket = 1 fm = 1e-5 Å`.
- `bucket = round(distance / 1 fm)` with `MidpointRounding.AwayFromZero`.
- Pair and cluster reduction compare integer buckets, not raw doubles.
- Enumerate with a `cutoff + 1 fm` margin and include `bucket <= cutoffBucket`.
- The exact `24 × 10^12` grid remains the fractional deduplication mechanism; no tolerance comparer is used in
  sorted dictionaries or hash sets.
- Named tolerances:

| Constant | Value | Use |
| --- | --- | --- |
| `PairDistanceBucket` | `1 fm = 1e-5 Å` | Discrete pair/cluster distance shells |
| `DistanceEqualityTolerance` | `1e-5 Å` | Continuous distance predicates outside the reduction |
| `GeometricEqualityTolerance` | `1e-6 Å` | Cartesian distance/vector equality |
| `FractionalTrimTolerance` | `1e-10` | Boundary snapping before exact grid conversion |
| `CellLengthTolerance` | `1e-3 Å` | Cell length equality |
| `CellAngleTolerance` | `1e-3°` | Cell angle equality |
| `CellDeterminantTolerance` | `1e-12` | Near-degenerate cell cutoff |

#### Tie-breaking and box queries

- If two minimum-image candidates share the shortest bucket, choose the smallest
  `(cellA, cellB, cellC)` lexicographically, then the smallest `P`.
- `GetPositionsInBox(min, max)` uses a half-open Cartesian box: a point equal to `min` on an axis is included;
  a point equal to `max` is excluded. Each periodic image is filtered independently; duplicates with different
  offsets remain distinct and results are sorted by `(P, cellA, cellB, cellC)`.
- `GetCellVolume`, `ToCartesian`, `ToFractional`, and `GetDistance` remain available; `GetDistance` is the
  direct, non-minimum-image distance unless the caller uses `GetMinimumImage`.

### 7. Caching, determinism, contracts, and performance

#### 7.1 Cache scope and keys

- `SpaceGroupCatalog` is immutable and may be shared process-wide.
- All other caches are scoped to a symmetry service or compilation instance; they are not static and are
  released with the compilation.
- Use immutable record keys with deterministic value equality for in-memory caches.
- Use SHA-256 of canonical bytes for persisted geometry IDs and cross-run hashes. Never use
  `string.GetHashCode()` for persisted or cross-run identity.
- Cache keys:

| Cache | Key |
| --- | --- |
| Cell geometry | `cellParametersHash` |
| Orbit / stabilizer | `(catalogDataVersion, settingKey, gridCenter)` |
| Point group | `(catalogDataVersion, settingKey, centerP, orderedSiteSequenceHash)` |
| Pair shells | `(catalogDataVersion, settingKey, centerP, cutoffBucket, cellParametersHash)` |
| Cluster reduction | `(catalogDataVersion, settingKey, centerP, clusterOffsetsHash, allowedOccupationHash)` |
| Canonical P1 order | `(catalogDataVersion, symmetryContractVersion, structureHash)` |

- Bound cache size per compilation. A limit breach is a diagnostic or a bounded failure, never an OOM.

#### 7.2 Determinism under parallelism

- Parallelize only independent orbit, pair, and cluster work; each task uses local buffers.
- Merge and sort results canonically before assigning IDs or storing them.
- Do not use parallel floating-point reductions.
- Cache insertion order must not affect results.
- Final outputs are sorted by the documented canonical keys.
- Golden tests run on Linux and Windows on step branches; macOS runs them on `main`.

#### 7.3 Symmetry contract version

- `SymmetryContractVersion = "symmetry-1"` covers:
  - the `24 × 10^12` fractional grid;
  - canonical P1 ordering and tie-break;
  - pair-shell ordering and the `1 fm` bucket;
  - cluster canonicalization and 6-bit occupation codes;
  - the stabilizer and reduction semantics.
- The compiled payload records:
  - `symmetry_contract_version`;
  - `space_group_data_version`;
  - `space_group_data_sha256`.
- Changing any covered rule bumps `SymmetryContractVersion` and updates golden fixtures and signatures.
- The project envelope may carry `symmetryContractVersion` as advisory metadata; the compiled payload records
  the authoritative build version. A mismatch emits `PROJ034` warning.

#### 7.4 Test oracles and tiers

- Brute-force oracles:
  - orbit membership and closure;
  - stabilizer and induced-permutation group laws;
  - pair shells from explicit image enumeration;
  - cluster canonicalization from explicit tuple permutations;
  - minimum image and radial search from explicit cell scans.
- Golden fixtures:
  - Step 02-local synthetic structures: P1 triclinic, P2 unique axis B, P2 unique axis C, Pmmm general and
    special positions, Fm-3m special positions, and R-3m hexagonal general position;
  - freeze P indices, pair shells, chiral links, cluster codes, and linear indices for those fixtures;
  - full ceria, barium zirconate, and vehicle/interstitialcy golden models belong to Step 03/08.
- PR-fast:
  - all 274 catalog group-law checks;
  - golden samples;
  - property subsets;
  - encoder and minimum-image tests.
  Target under 10 minutes.
- PR-medium:
  - more random property cases;
  - cross-language fixtures;
  - full reduction suites.
- Nightly/release:
  - full random property suites;
  - 20×20×20 stress;
  - benchmark regression;
  - macOS golden run;
  - memory profile.
- Property-test failures print the seed and canonical geometry hash.

#### 7.5 Performance and memory

- Benchmark fixture: 64 deterministic P1 positions in a cubic `a=5.41 Å` cell, expanded to `20×20×20` =
  512,000 `Vector4I` positions.
- Runner class: GitHub-hosted `ubuntu-latest`, 4 vCPU, .NET 10 Release.
- Benchmark: BenchmarkDotNet `P1Expansion20Cubed` in `Mocassin.Benchmarks`.
- Target: mean under 1 second; baseline in `tests/reference/bench/baselines.json` under the
  `linux-x64-github-ubuntu-latest-dotnet10` class.
- Command: `dotnet run -c Release --project src/dotnet/Mocassin.Benchmarks -- --filter "*P1Expansion20Cubed*"`.
- Pair and cluster reductions use the documented complexity bounds and early pruning.
- Only base P1 Cartesian positions are precomputed. Do not allocate a dense `A×B×C×P` Cartesian array.
- BenchmarkDotNet runs on release candidates; a 10% regression fails unless accepted by ADR.
- Record the benchmark environment and raw results in `tests/reference/bench/`.

#### 7.6 Public/internal API boundary

- Public: `SpaceGroupCatalog`, `SpaceGroupSetting`, `SymmetryOperation`, `PointGroup`, `SitePermutation`,
  `ExpandedSite`, `SiteOrbitInfo`, `CellGeometry`, `UnitCellVectorEncoder`, `MinimumImageResult`,
  `NeighborImage`, and the pair/cluster reduction result types.
- Internal: `GridFractional3`, JSON DTOs, cache keys, hashing helpers, and reduction scratch types.
- All public/protected members have XML documentation.
- ApiCompat baselines are updated for intentional public API changes.

#### 7.7 Error handling and limits

- A corrupt or missing catalog is a build/installation error, not a user diagnostic.
- Invalid user cell, setting, or site data produces structured diagnostics (`STR001`, `STR003`, etc.).
- Limits: 9 cluster sites, 63 particles, 10,000 P1 positions, and the signed 32-bit linear index.
- Exceeding a reduction or cache limit produces a diagnostic or a bounded failure, never an overflow or OOM.
- Diagnostics are sorted deterministically by severity, code, and path.

## Implementation tasks

- [x] Add the space-group data source and a build-time generator for the embedded catalog.
- [x] Implement `SymmetryOperation` with integer composition/inverse/closure.
- [x] Implement `SpaceGroupSetting`, `SpaceGroupCatalog`, and lookup APIs.
- [x] Implement crystal-system constraints and the corrected cell-vector transform.
- [x] Add `Fractional3`, `Cartesian3`, and `Vector4I` to `Mocassin.Core.Geometry`; implement the encoder in
  `Mocassin.Symmetry`.
- [x] Implement a shared generic `Vector3<T>`/`VectorMath<T>` arithmetic kernel constrained to
  `unmanaged, INumber<T>`; public semantic wrappers delegate to it.
- [x] Implement canonical P1 expansion and position ordering.
- [ ] Implement stabilizer/point-group/permutation reduction.
- [ ] Implement radial sampling and periodic geometry helpers.
- [x] Add golden tests for all 230 groups/274 settings.
- [ ] Document the space-group data version and index-order contract.
- [ ] Keep the assembly boundary: shared persisted vocabulary in `Mocassin.Core`, catalog and algorithms in
  `Mocassin.Symmetry`, and no Core reference to Symmetry.
- [x] Commit `Data/space-groups.json`, `Data/space-groups.sha256`, and `Data/README.md`; embed the data and wire
  the embedded-resource loader.
- [x] Add `tools/symmetry/generate_space_groups.py` and `requirements.txt`, and run the drift check in the
  Contracts and policy CI job.
- [x] Use a source-generated JSON serializer context for the internal catalog DTOs.
- [ ] Implement per-compilation caches, deterministic hashing, and `SymmetryContractVersion`.
- [ ] Add the 20×20×20 benchmark fixture and golden reduction/contract tests.
- [ ] Record the symmetry contract and catalog versions in the compiled payload.

## Acceptance criteria

- [x] All 230 international groups and 274 settings load.
- [x] Every setting passes identity, inverse, closure, determinant, and translation-normalization checks.
- [x] Group 47 is valid and matches spglib’s `Pmmm` setting.
- [x] General-position multiplicities match the International Tables for every setting.
- [x] P1 expansion of every Step 02 synthetic fixture site matches a brute-force expansion.
- [x] The canonical position order is stable across runs, OSes, and input ordering for the synthetic fixtures.
- [x] The triclinic cell test produces the correct C vector and angles.
- [ ] Pair and cluster reductions match brute-force orbit enumeration for random geometries.
- [ ] The `P1Expansion20Cubed` benchmark (64 P1 positions, 20×20×20) meets its target on the defined runner.
- [ ] The compiled payload records `symmetry_contract_version`, `space_group_data_version`, and
  `space_group_data_sha256`.
- [ ] Cross-OS golden P indices, pair shells, and cluster codes match.
- [ ] Cache scope and parallel-determinism tests pass.
- [ ] Documentation written

## Documentation

At this point, document the symmetry and geometry foundation:

- `docs/physics/symmetry.md` — space groups, exact operations, cell geometry, the `24 × 10^12` fractional
  grid, P1 expansion, canonical position order, minimum-image policy, and radial search.
- `docs/development/compilation-pipeline.md` — the symmetry stage, its exact grid, pair/cluster bucketing,
  cutoff margins, and inputs/outputs.
- `docs/glossary/notation.md` — fractional/Cartesian coordinates, the exact fractional grid, and `Vector4I`.
- `docs/reference/project-format.md` — space-group/cell/site fields, cell validity constraints, and the
  data-version contract.
- `docs/development/architecture.md` — catalog generation, embedding, drift checks, cache scope, and contract
  versioning.

## Tests

| Test | Assertion |
| --- | --- |
| Orbit property | Random start points stay in the computed orbit; orbit contents are independent of input order |
| Group-law property | Random operation triples compose associatively and every operation has an inverse |
| Catalog integrity | 230 groups, 274 settings, expected operation counts |
| Group laws | Identity, inverse, closure, determinant ±1 for all settings |
| Orbit sizes | General and special positions match known multiplicities |
| Canonical order | Shuffled sites/operations produce the same `P` assignment |
| P1 golden indices | Step 02 synthetic fixtures freeze the expected P order |
| Coordinate round-trip | Fractional ↔ `Vector4I` ↔ fractional for base positions and neighbor images, including negative offsets |
| Linear index | Small-supercell index round-trip; total exactly `int.MaxValue` is accepted by the arithmetic check |
| Index limit | A total of `int.MaxValue + 1` and out-of-range offsets are rejected; no wrap or truncation |
| Generic math | `Vector3<double>` arithmetic matches the semantic wrappers |
| Cell geometry | Seven systems, both trigonal axis descriptions, unique-axis B/C, volume, and right-handedness |
| Cell validity | Angles outside 0..180°, non-positive Gram determinant, and near-degenerate cells are rejected |
| Triclinic regression | `a=5`, `b=6`, `c=7`, `α=80°`, `β=85°`, `γ=95°` matches independent vectors and volume |
| Pair reduction | Reduced shells equal brute-force `G0` orbits for random pairs |
| Chiral pairs | Forward/inverse shell linking matches an independent path-orbit check |
| Cluster reduction | Canonical tuples are independent of input order and the expansion map covers every equivalent state once |
| Occupation codes | 6-bit codes round-trip for 63 particles and 9 sites |
| Minimum image | `GetMinimumImage` equals brute force over a large offset range for random triclinic cells |
| Radial search | `RadialSearch` equals brute-force enumeration and includes all images within the cutoff |
| Bounding box | The plane-distance box never misses an image at the cutoff |
| Image multiplicity | Multiple images of the same P index remain distinct and correctly offset |
| Distance buckets | Symmetry-equivalent pairs share a bucket; cutoff-boundary candidates are included with the 1 fm margin |
| Box queries | `GetPositionsInBox` half-open boundaries and periodic duplicates are deterministic |
| Grid invalid input | `NaN`, `+Inf`, and `-Inf` are rejected; subnormal values underflow to zero |
| Boundary trimming | Near-boundary inputs snap deterministically and do not merge distinct positions |
| Cell conditioning | Highly skewed cells above `CellConditionLimit` are rejected even with positive `det G` |
| Box boundaries | `min` is inclusive and `max` is exclusive; periodic duplicates remain distinct |
| Cache scope | Per-compilation caches do not leak across structures; keys include cell, cutoff, and contract version |
| Determinism | Parallel expansion and reduction equal sequential canonical output |
| Contract version | Changing a covered rule bumps `symmetry-1` and invalidates golden signatures |
| Benchmark | 64 P1 positions × 20×20×20 meets the `P1Expansion20Cubed` target on the defined runner class |
| Thread safety | Parallel expansion produces identical results |
| Assembly boundary | `Mocassin.Core` does not reference `Mocassin.Symmetry`; persisted symmetry vocabulary is in Core |
| Catalog checksum | Startup rejects a corrupted catalog or wrong schema version |
| Catalog drift | Generator `--check` reproduces the committed settings and checksum |
| Setting identity | `(number, axis, origin)` resolves to the same setting after a project round-trip; derived HM/Hall are not persisted |
| Grid conversion | 0, 0.25, 0.1, 0.123456789012, -0.0, and values outside [0,1) map to the expected grid integers |
| Grid closure | Applying every catalog operation to random grid points produces grid points |
| Grid dedup | Distinct grid points stay distinct; equivalent decimals dedup; no tolerance comparer is used |
| Grid precision | Inputs with more than 12 fractional digits round deterministically and warn |

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Space-group data errors | spglib source, committed checksum, group-law tests, and independent multiplicity checks |
| Catalog source/version drift | Pinned spglib, committed data, CI `--check`, and a provenance review for every pin change |
| Ambiguous setting identity | Persist structured `(number, axis, origin)`; derive HM/Hall from the catalog |
| Exact arithmetic performance | Use small fixed-size structs and integer ops; convert to double only at boundaries |
| Canonical order changes later | Treat as a format contract; version the catalog and add golden tests |
| Tolerance bugs | Use exact grid comparisons for discrete symmetry; use named, unit-specific tolerances for continuous geometry |
| Cell-parameter tolerance drift | Named constants, boundary tests, and no silent normalization |
| Near-degenerate cells | Positive Gram-determinant check and explicit `STR003` rejection |
| Large orbit/permutation counts | Bound counts, warn in validation, use pruning and parallelism |
| Grid precision or overflow | Fixed denominator `24×10^12`, `long` components, bounds tests, and a documented 12-digit limit |
| Grid/canonical-order drift | Version the grid with the catalog; golden P-index tests on all platforms |
| Linear-index overflow | `int` index, 64-bit total count, `int.MaxValue` hard cap, and boundary tests |
| Relative-offset misuse | Negative offsets are relative-path coordinates, not linear indices; explicit conversion and bounds checks |
| Chirality/orbit ambiguity | Standard group terms, exact orbit tests, and explicit orbit-vs-physical-chirality documentation |
| Skewed-cell minimum image | Plane-distance bounding box, covering-radius bound, and brute-force tests |
| Distance-bucket boundary drift | Fixed 1 fm bucket, symmetric rounding, and a 1 fm search margin |
| Dense Cartesian memory | Base P1 positions only; no `A×B×C×P` Cartesian allocation |
| Tolerance confusion | Named constants with units and documented scopes |
| Cache growth | Per-compilation scope, bounded entries, and explicit limits |
| Hash instability | SHA-256 for persisted IDs; no `string.GetHashCode()` |
| Contract drift | `SymmetryContractVersion`, golden fixtures, and versioned compiled payload |
| API leakage | Internal DTOs/cache types; public API baseline and XML docs |
| Cell conditioning | `CellConditionLimit` and a high-condition triclinic stress test |
| Grid invalid input | Non-finite rejection, underflow-to-zero rule, and explicit tests |
| Boundary trimming | Near-boundary P-order and overlap tests |
| Box boundary ambiguity | Half-open inclusion tests and distinct-offset duplicates |

## Done when

The symmetry engine is exact, validated for all 230 groups, produces deterministic P1 indices, and passes
brute-force equivalence tests for orbits, pairs, and clusters.
