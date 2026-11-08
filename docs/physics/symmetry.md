# Symmetry and Geometry

The symmetry foundation turns a crystallographic setting and a list of reference sites into a deterministic
set of unit-cell positions and symmetry-reduced interaction environments. It is exact where crystallography
is discrete and continuous only for Cartesian geometry.

## Space-group catalog

The embedded catalog contains the 230 international space groups in 274 compatibility settings:

| Category | Settings |
| --- | ---: |
| Single standard setting | 186 |
| Origin choice 1 and 2 | 48 |
| Monoclinic unique axis B and C | 26 |
| Rhombohedral and hexagonal axes | 14 |

The catalog is generated from spglib `2.8.0` (commit `3ebd8ac3318793932baeee90b44f453a7c35b5b4`) and committed
as a compact positional JSON resource. Its SHA-256 covers the canonical settings payload, not the file
wrapping or any timestamp. The .NET loader verifies the schema version, data version, checksum, 230/274
counts, identity presence, determinant ±1, and normalized translations at startup. CI regenerates the
selection and fails on drift.

The original MOCASSIN SQLite database is not a source. It is GPL-3.0-or-later and is never copied. The
independent data source and licensing boundary are recorded in `NOTICE.md`.

## Exact operations

A symmetry operation is

$$
x \mapsto R x + t
$$

where `R` is a signed 3×3 integer matrix and `t` is an exact rational translation with denominator 24.
Composition, inversion, determinant, and group closure use integer arithmetic. Every catalog operation has
determinant `+1` or `-1`. Group 47 is the corrected `Pmmm` setting with eight operations, including identity
and inversion.

The public coordinate types remain `double`-based:

- `Fractional3` — fractional coordinates;
- `Cartesian3` — Cartesian coordinates in ångström;
- `Vector4I` — an integer cell offset plus the per-cell P index.

## Exact fractional grid

Discrete symmetry work uses an internal `GridFractional3` scaled by

$$
D = 24 \times 10^{12} = 24{,}000{,}000{,}000{,}000
$$

The factor 24 is divisible by every crystallographic translation denominator in the catalog (2, 3, 4, 6, 8,
12, and 24). `10^12` preserves any decimal input with up to 12 fractional digits exactly. An integer rotation
plus a translation `m/24` maps grid points to grid points exactly.

Conversion rules:

- use the canonical shortest round-trip decimal representation and parse it as `decimal`;
- round to the nearest grid point with `MidpointRounding.AwayFromZero`;
- reject `NaN` and infinities with a structured diagnostic;
- underflow finite magnitudes below half a grid step to zero;
- normalize `-0.0` to `0.0`;
- signal a warning when the input required rounding (more than 12 fractional digits).

`Fractional3.TrimToUnitCell()` snaps components within `1e-10` of an integer and maps into \([0,1)\). That snap
is a user-input convenience applied before grid conversion; it is not the deduplication criterion.

## Cell geometry

Cell lengths must be in `0.1..1000 Å`; angles must be strictly between `0°` and `180°`. The Gram determinant

$$
\det G = 1 - \cos^2\alpha - \cos^2\beta - \cos^2\gamma
         + 2\cos\alpha\cos\beta\cos\gamma
$$

must be positive and above `1e-12`. The cell matrix condition number `||M||∞ · ||M⁻¹||∞` is bounded by
`1e12` as a defensive guard.

The standard right-handed cell vectors are

$$
\begin{aligned}
A &= (a, 0, 0) \\
B &= (b\cos\gamma,\; b\sin\gamma,\; 0) \\
C &= \left(
       c\cos\beta,\;
       \frac{c(\cos\alpha - \cos\beta\cos\gamma)}{\sin\gamma},\;
       \frac{c\sqrt{\det G}}{\sin\gamma}
     \right)
\end{aligned}
$$

The `c` factor in the C y-component is required; omitting it is the legacy bug. Volume is
`a b c sqrt(det G)`, and `C` always has a positive z-component.

Crystal-system constraints are checked when the resolved setting is supplied: monoclinic unique-axis B/C,
orthorhombic and tetragonal right angles, trigonal/hexagonal `a=b` with `γ=120°`, rhombohedral `a=b=c` with
`α=β=γ<120°`, and cubic equal lengths. Inconsistent parameters produce `STR002`/`STR003` diagnostics; the
model never silently normalizes user input.

## Canonical P1 order

The canonical order is a format contract:

1. expand every reference site to its P1 orbit;
2. trim to \([0,1)\);
3. deduplicate by exact grid coordinates;
4. sort ascending by fractional `(a, b, c)`, with `a` most significant;
5. assign `P = 0..Pcount-1` in that order.

`Vector4I` encodes `(cellA, cellB, cellC, P)`. The linear index is row-major with P fastest:

$$
\text{index} = P
  + \text{cellC}\cdot P_{\text{count}}
  + \text{cellB}\cdot P_{\text{count}}C_{\text{count}}
  + \text{cellA}\cdot P_{\text{count}}C_{\text{count}}B_{\text{count}}
$$

The total lattice count is computed in 64-bit arithmetic, capped at `int.MaxValue`, and never wrapped.
Negative cell offsets are relative-path coordinates; convert them with `ToSupercellOffset` before
linearization.

## Stabilizers and point groups

For a center site `c` and ordered site sequence `S = (s0, …, sN−1)`:

- \(G_0\) — center stabilizer: operations that map `c` to itself modulo lattice translation;
- \(G_{\text{set}}\) — setwise stabilizer: operations in `G0` that map the unordered set onto itself;
- \(G_{\text{seq}}\) — pointwise stabilizer: operations in `G0` that fix every `si` individually;
- \(P \cong G_{\text{set}} / G_{\text{seq}}\) — induced site permutations.

\(G_{\text{seq}} \subseteq G_{\text{set}} \subseteq G_0\). The ordered orbit has \(|G_0|/|G_{\text{seq}}|\) sequences; the set orbit has \(|G_0|/|G_{\text{set}}|\) sets.

## Pair shells

Periodic images within a cutoff are grouped into distance shells:

- distance bucket \(1\ \text{fm} = 10^{-5}\ \text{Å}\), rounded symmetrically;
- candidates sorted by `(distanceBucket, targetP, cellA, cellB, cellC)`;
- the first unassigned candidate is the shell representative;
- applying `G0` marks every candidate in the same bucket and orbit as equivalent;
- the inverse direction is linked as a chiral partner when it is not in the same `G0` orbit.

Chiral here means an orbit relationship, not physical chirality. In P1 every directed pair is its own orbit.
Chiral partners are matched within the same distance bucket so that distinct lattice images are not confused
with the reverse direction of one shell.

## Cluster reduction

A cluster is a center plus an ordered sequence of up to nine sites. Allowed occupations are enumerated and
canonicalized under the induced permutation group; the lexicographically smallest tuple in site order is the
representative. Occupation tuples are encoded as `ulong` with six bits per site, site 0 in the least
significant bits:

$$
\text{code} = \sum_{i=0}^{N-1} \text{particleIndex}(s_i)\,2^{6i}
$$

The expansion map stores every equivalent tuple code and the permutation that produced it. Mirror tuples are
merged only when the mirror permutation is in `P`.

## Periodic geometry

The cell matrix has columns `A`, `B`, `C`; the metric tensor is \(G = M^{T}M\). Distances use \(d^2 = df^{T}G\,df\).

- `GetMinimumImage` returns exactly one shortest image and its offset; ties choose the smallest
  `(cellA, cellB, cellC)`, then P.
- `RadialSearch` returns every image within the cutoff, excluding only the zero-offset self image.
- `GetPositionsInBox` is half-open: `min` is inclusive and `max` is exclusive; distinct offsets remain
  distinct.
- Search boxes use the interplanar spacings \(d_a = V/|b\times c|\), \(d_b = V/|c\times a|\), and \(d_c = V/|a\times b|\), plus one
  extra cell.

## Reproducibility contract

`SymmetryContract.Version = "symmetry-1"` covers the grid denominator and conversion, canonical P1 order,
pair-shell order and the 1 fm bucket, cluster canonicalization and occupation codes, and the stabilizer and
reduction semantics. Changing any covered rule requires a new contract version and updated golden fixtures.

Compiled packages record:

- `symmetry_contract_version`;
- `space_group_data_version`;
- `space_group_data_sha256`.

The project may carry advisory symmetry metadata; a mismatch with the embedded catalog warns `PROJ034`. The
compiled payload remains authoritative. Caches are scoped to one compilation and keyed by catalog version,
contract version, setting identity, and operation inputs.
