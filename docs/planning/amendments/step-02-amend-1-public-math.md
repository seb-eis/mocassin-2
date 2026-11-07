# Step 02 Amendment 1 — Public Generic Math Types

## Request

| Field | Value |
| --- | --- |
| Amendment ID | `step-02-amend-1` |
| Parent step | 02 — Symmetry and Geometry |
| Status | Ready with notes |
| Branch | `feature/step-02-symmetry-and-geometry/amend-1-public-math` |
| Design review | `docs/planning/reviews/step-02-amend-1.md` |
| Parent audit | `docs/planning/audits/step-02.md` |
| Integration commit | — |
| Combined audit commit | — |
| Tag | `step-02` |
| Date | 2026-11-07 |
| Owner | Planner |

Parent step page: [`Step 02 — Symmetry and Geometry`](../steps/step-02-symmetry-and-geometry.md)

## Rationale

The Step 02 cell geometry currently carries a temporary `double[] inverse` and duplicate determinant/inverse
code. The implementation is correct, but the representation is weak: the 3×3 layout is implicit, indexing is
error-prone, and the inverse is allocated separately from the matrix.

At the same time, public generic math is a high-value reusable layer. Later steps need matrix/vector math for:

- cell geometry and compilation;
- homogeneous/authoring transforms;
- rendering and UI geometry;
- extension and analysis code.

A local `Matrix3d` inside `CellGeometry` would fix the array but not provide the reusable public layer. This
amendment introduces the public generic math types, keeps `Int3x3` as the exact integer specialization, and
migrates `CellGeometry` to `Matrix3<double>`.

## Scope

- Make `Vector3<T>` public in `Mocassin.Core.Geometry`.
- Add public `Vector4<T>`.
- Add public row-based `Matrix3<T>` and `Matrix4<T>`.
- Migrate `CellGeometry` from `double[]` to `Matrix3<double>`.
- Add XML documentation and public-API baseline coverage.
- Add matrix/vector tests and a small performance/inlining check.
- Record the design decision and update the architecture/open-decisions pages.

## Non-goals

- No homogeneous path semantics in this amendment. `Vector4<T>`/`Matrix4<T>` are general math primitives;
  the UX `PathCoordinate` decision remains separate.
- No change to `Int3x3` or the exact integer symmetry operations.
- No integer or `decimal` matrix inverse. Floating-point matrices use `IFloatingPointIeee754<T>`, which excludes
  `decimal`; exact integer matrices remain `Int3x3`.
- No rendering, Avalonia, or UX integration.
- No changes to the project, package, results, or state formats.

## Design

### Type constraints

| Type | Constraint | Why |
| --- | --- | --- |
| `Vector3<T>` | `T : unmanaged, INumber<T>` | General numeric vector arithmetic; no division-by-`T` requirement beyond `INumber<T>` |
| `Vector4<T>` | `T : unmanaged, INumber<T>` | General numeric vector arithmetic and matrix rows |
| `Matrix3<T>` | `T : unmanaged, IFloatingPointIeee754<T>` | Determinant/inverse require floating-point division |
| `Matrix4<T>` | `T : unmanaged, IFloatingPointIeee754<T>` | Determinant/inverse require floating-point division |
| `Int3x3` | exact `int` specialization in `Mocassin.Symmetry` | Exact crystallographic group laws and `±1` inverse |

`double` is the scientific geometry type. `float`/`Half` may be used by future rendering code; `decimal` and
integer matrices are not part of this API.

### `Vector3<T>`

Make the existing type public. Preserve:

- `X`, `Y`, `Z`, `Zero`;
- add/subtract/negate/scale/divide;
- `Dot`, `Cross`, `LengthSquared`;
- invariant `ToString`.

Add XML documentation and API baseline entries. The current operators already use `AggressiveInlining`.

### `Vector4<T>`

Add:

```csharp
public readonly record struct Vector4<T>(T X, T Y, T Z, T W)
    where T : unmanaged, INumber<T>
{
    public static Vector4<T> Zero { get; }
    public T LengthSquared { get; }
    public T Dot(Vector4<T> other);
    public static Vector4<T> operator +(Vector4<T> left, Vector4<T> right);
    public static Vector4<T> operator -(Vector4<T> left, Vector4<T> right);
    public static Vector4<T> operator -(Vector4<T> value);
    public static Vector4<T> operator *(Vector4<T> value, T factor);
    public static Vector4<T> operator *(T factor, Vector4<T> value);
    public static Vector4<T> operator /(Vector4<T> value, T divisor);
}
```

Do not add a 4D cross product.

### `Matrix3<T>`

Row-based storage:

```csharp
public readonly record struct Matrix3<T>(
    Vector3<T> Row0,
    Vector3<T> Row1,
    Vector3<T> Row2)
    where T : unmanaged, IFloatingPointIeee754<T>
```

Required API:

- `M11..M33` component accessors;
- `Column0..Column2`;
- `Identity`;
- `FromRows`, `FromColumns`;
- `Transpose`;
- `Determinant`;
- `Inverse`;
- `InfinityNorm`;
- `ConditionNumber`;
- `Transform(Vector3<T>)`;
- `operator *(Matrix3<T>, Matrix3<T>)`;
- `ToString`.

Convention:

- row-major storage;
- `M * v` treats `v` as a column vector;
- `Row0` is the first row;
- `FromColumns(a, b, c)` builds the matrix whose columns are `a`, `b`, `c`.

`Inverse` throws `InvalidOperationException` when the determinant is zero. The implementation may use the
adjugate for 3×3.

### `Matrix4<T>`

Row-based storage:

```csharp
public readonly record struct Matrix4<T>(
    Vector4<T> Row0,
    Vector4<T> Row1,
    Vector4<T> Row2,
    Vector4<T> Row3)
    where T : unmanaged, IFloatingPointIeee754<T>
```

Required API mirrors `Matrix3<T>`:

- `M11..M44`;
- `Column0..Column3`;
- `Identity`, `FromRows`, `FromColumns`;
- `Transpose`, `Determinant`, `Inverse`;
- `InfinityNorm`, `ConditionNumber`;
- `Transform(Vector4<T>)`;
- `operator *(Matrix4<T>, Matrix4<T>)`;
- `ToString`.

Use cofactor expansion or a documented Gaussian-elimination implementation. The general inverse is required;
an affine-specialized inverse can be added later if profiling justifies it. Homogeneous point/direction
semantics are not part of this type.

#### Consumer justification for 4×4

`Vector4<T>`/`Matrix4<T>` are retained because the planned homogeneous/authoring layer (Step 23/24) and the
rendering transforms (Step 27) need a public 4×4 math type. Adding them now keeps the public math API coherent
and avoids a second public-math amendment. They are independently tested for construction, multiplication, determinant, inverse, transform, and
condition number; they are not required by `CellGeometry`.

These consumer references are provisional. The Step 23/24/27 design reviews must confirm the 4×4 consumer or
defer the 4D types; if they do not need it, `Vector4<T>`/`Matrix4<T>` can be removed in a later amendment without
changing the 3×3/`CellGeometry` contract.

#### Singular and conditioning contract

- `Matrix3<T>.Inverse` and `Matrix4<T>.Inverse` throw `InvalidOperationException` when the determinant is zero.
- `ConditionNumber` is defined only for an invertible matrix; it throws for a singular matrix.
- `Inverse` does not apply a tolerance. `CellGeometry` owns the near-zero determinant and condition-number
  validation before it computes or relies on the inverse.
- `AggressiveInlining` is limited to small hot accessors and transforms. Do not force-inline `Inverse` or the
  4×4 paths without benchmark evidence.

### `CellGeometry` migration

Replace the `double[] inverse` field with:

```csharp
public Matrix3<double> Matrix { get; }
public Matrix3<double> Inverse { get; }
```

Construction:

```csharp
Matrix = Matrix3<double>.FromColumns(A, B, C);
Inverse = Matrix.Inverse;
```

Use:

- `ToCartesian(f) => new Cartesian3(...)` from `Matrix.Transform(new Vector3<double>(f.A, f.B, f.C))`;
- `ToFractional(x) => new Fractional3(...)` from `Inverse.Transform(new Vector3<double>(x.X, x.Y, x.Z))`;
- `VolumeAngstrom3 = Math.Abs(Matrix.Determinant)`;
- `ConditionNumber = Matrix.ConditionNumber`;
- metric tensor `G = Matrix.Transpose() * Matrix` for distance calculations, or the equivalent cached scalar
  components.

No behavior change is intended. Existing cell-geometry tests and golden values must remain identical within
the documented tolerances.

### Performance and inlining

- Mark small public vector/matrix operators, accessors, and transforms `AggressiveInlining`; do not force-inline
  `Inverse` or the 4×4 paths without benchmark evidence.
- Avoid arrays inside matrix types.
- Use value types; `Matrix3<double>` is 72 bytes and `Matrix4<double>` is 128 bytes.
- Add `MatrixMathBenchmarks` in `Mocassin.Benchmarks` with `MemoryDiagnoser` and `DisassemblyDiagnoser` for
  `Matrix3<double>.Transform` and `Matrix3<double>.Inverse`.
- Acceptance: the transform benchmark reports `Allocated = 0`, and the disassembly contains no unexpected call
  to `Matrix3.Transform`, `Vector3<T>` operators, or the generic kernel.
- Record the benchmark class, environment, and raw result in `tests/reference/bench/baselines.json` and link
  the result from this amendment page.
- Do not use `Unsafe.As` or layout punning.

## API and contract impact

- New public types and members in `Mocassin.Core.Geometry`.
- `Vector3<T>` changes from internal to public: additive, no breaking change.
- `CellGeometry.Matrix` and `CellGeometry.Inverse` become public read-only properties.
- `src/dotnet/Mocassin.Core/PublicAPI.Unshipped.txt`,
  `src/dotnet/Mocassin.Symmetry/PublicAPI.Unshipped.txt`, XML documentation, and both API baseline assemblies
  (`tests/contracts/api-baseline/net10.0/Mocassin.Core.dll` and
  `tests/contracts/api-baseline/net10.0/Mocassin.Symmetry.dll`) must be updated.
- No project/package/results/state format changes.

## Temporary implementation and migration

- The current `CellGeometry` `double[] inverse` is temporary.
- This amendment replaces it with `Matrix3<double>`.
- `Int3x3` remains unchanged.
- No compatibility shim is required.

## Acceptance criteria

- [ ] `Vector3<T>`, `Vector4<T>`, `Matrix3<T>`, and `Matrix4<T>` are public in `Mocassin.Core.Geometry`.
- [ ] Every public member has XML documentation and appears in both public API baselines.
- [ ] `Matrix3<T>` and `Matrix4<T>` are row-based and document the `M * v` convention.
- [ ] `Matrix3<T>.FromColumns` builds the cell matrix correctly.
- [ ] `Matrix3<T>.Inverse` and `Matrix4<T>.Inverse` satisfy `M * M⁻¹ = I` for random invertible matrices.
- [ ] `CellGeometry` contains no `double[]` matrix/inverse field.
- [ ] Existing cell-geometry behavior and golden values are unchanged.
- [ ] Matrix/vector property tests pass for `double` and `float`.
- [ ] `Matrix4<T>` construction, `FromRows`, `FromColumns`, `Transform`, and `ConditionNumber` are tested.
- [ ] The matrix benchmark records `Allocated = 0` and the expected inlining result.
- [ ] `Int3x3` and exact symmetry arithmetic are unchanged.
- [ ] No project, package, results, or state format changes.

## Tests

| Test | Assertion |
| --- | --- |
| Vector arithmetic | Add/subtract/scale/dot/length match scalar reference for `double` and `float` |
| Matrix identity | `M * I = I * M = M` |
| Matrix multiplication | Associative and matches component reference |
| Determinant | Multiplicative and matches the 3×3/4×4 reference |
| Inverse 3×3 | `M * M⁻¹ = I` for random invertible matrices; singular throws |
| Inverse 4×4 | `M * M⁻¹ = I` for random invertible matrices; singular throws |
| Transform | `M * v` matches row-dot reference |
| Row/column convention | `FromColumns(a,b,c)` transforms the standard basis to the columns |
| Matrix4 construction | `FromRows`/`FromColumns` and `Transform(Vector4<T>)` match a component reference |
| Singular/condition | Singular inverse and condition-number usage throw; `CellGeometry` rejects before use |
| Cell matrix | `CellGeometry.Matrix` columns equal `A`, `B`, `C` |
| Cell round-trip | `ToCartesian`/`ToFractional` round-trip within tolerance |
| Cell condition | `Matrix.ConditionNumber` equals the existing condition number |
| Cell volume | `abs(det M)` equals the existing volume |
| Inlining | Matrix-vector transform has no allocation or unexpected call in the disassembly/benchmark |

## Documentation

- [ ] `docs/development/design-decisions.md` — public generic math and `IFloatingPointIeee754<T>` matrix constraint.
- [ ] `docs/development/architecture.md` — `Core.Geometry` owns the generic math primitives.
- [ ] `docs/planning/05-open-decisions.md` — resolved decision row.
- [ ] `docs/planning/steps/step-02-symmetry-and-geometry.md` — supersede the internal-kernel rule and update the
  public API boundary and implementation tasks.
- [ ] `docs/planning/01-target-architecture.md` — public vector/matrix math in `Core.Geometry`.
- [ ] `src/dotnet/Mocassin.Core/PublicAPI.Unshipped.txt` and
  `src/dotnet/Mocassin.Symmetry/PublicAPI.Unshipped.txt`.
- [ ] Both API baseline assemblies.
- [ ] `docs/planning/reviews/step-02.md` links to this amendment review.
- [ ] `docs/planning/audits/step-02.md` lists the amendment ID and combined commit.
- [ ] Amendment page/index and parent audit request.

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Public API surface expansion | Design review, XML docs, API baseline, and a focused public API list |
| Generic constraint confusion | Document `IFloatingPointIeee754<T>` for matrices (excludes `decimal`) and `INumber<T>` for vectors; keep `Int3x3` for exact integers |
| Row/column convention errors | Explicit convention, `FromRows`/`FromColumns` tests, cell matrix test |
| 4×4 inverse complexity | Property tests against an independent reference; singular-matrix tests; keep the planned Step 23/24/27 consumers explicit |
| Struct copy/performance | Benchmark and disassembly check; limit `AggressiveInlining` to measured hot paths; keep matrix types small |
| Behavior change in cell geometry | Reuse the existing golden tests and compare within documented tolerances |

## Dependencies

- Step 02 cell geometry is implemented and its design is frozen.
- The Step 02 audit is not yet frozen.
- `Mocassin.Core.Geometry` currently contains the internal `Vector3<T>` and the public coordinate wrappers.
- `Int3x3` remains in `Mocassin.Symmetry`.

## Integration

- Branch: `feature/step-02-symmetry-and-geometry/amend-1-public-math`
- Parent step branch: `step/02-symmetry-and-geometry`
- Parent branch synchronized with `main` before implementation: —
- Parent review linked: —
- Approval: normal feature approval; design review before implementation
- Squash commit: —
- Parent audit request updated: —
- Combined frozen commit: —

## Audit

- Parent audit record: `docs/planning/audits/step-02.md`
- Audit verdict: —
- Merge commit: —
- Tag: `step-02`
- Post-merge fallback required: no
