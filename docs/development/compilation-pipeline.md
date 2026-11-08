# Compilation Pipeline

Compilation is a pure, staged pipeline. Each stage has explicit inputs, outputs, and cache boundaries; no stage
performs file I/O or UI work. This page describes the symmetry stage introduced in Step 02 and how later stages
consume its results.

## Stage order

```text
validated project
  → structure / P1 expansion
  → symmetry reduction (environments, pair and cluster representatives)
  → transition and rule generation
  → encoding and ledger construction
  → immutable package
```

Later stages add transition, energy, lattice, and simulation compilation. Step 02 owns the first two stages and
the exact ordering contract they establish.

## Symmetry stage

### Inputs

- A validated cell (`CellParameters`) and a resolved `SpaceGroupSetting`.
- Reference sites as `Fractional3` coordinates.
- Optional interaction cutoffs for pair and cluster environments.

### Outputs

- `UnitCellVectorEncoder` with canonical P1 positions, `ExpandedSite` records, and `SiteOrbitInfo` records.
- `PointGroup` for each center/geometry combination: `G0`, `Gset`, `Gseq`, and induced permutations.
- `PairReductionResult` with canonical shells, equivalent images, and chiral links.
- `ClusterReductionResult` with canonical occupation tuples, 6-bit codes, and expansion maps.
- `SymmetryVersionInfo` containing the contract, catalog data version, and catalog checksum.

### Exact grid

All discrete work uses `GridFractional3` with denominator `24 × 10^12`. Cartesian geometry uses
`Cartesian3`/`CellGeometry` and `double`. The pipeline converts to Cartesian only for distances, cell vectors,
and radial search; it never uses a tolerance comparer to deduplicate or order sites.

### P1 expansion and ordering

The encoder expands each reference site to its orbit, trims to `[0, 1)`, deduplicates exact grid points,
sorts by fractional `(a, b, c)`, and assigns `P` in ascending order. Two reference sites that expand to the
same grid position produce `STR007`; positions above the 10,000 budget produce `STR009`.

The linear index is `P + cellC*Pcount + cellB*Pcount*Ccount + cellA*Pcount*Ccount*Bcount`, computed in
64-bit arithmetic with a hard `int.MaxValue` cap. Negative offsets are relative-path coordinates and must be
normalized with `ToSupercellOffset` before linearization.

### Symmetry reduction

Pair reduction enumerates periodic images inside the cutoff, buckets distances to whole femtometres, sorts by
`(distanceBucket, targetP, cellA, cellB, cellC)`, and groups candidates under `G0`. Cluster reduction
canonicalizes allowed occupation tuples under the induced permutation group. The exact semantics are in
[Symmetry and Geometry](../physics/symmetry.md).

### Determinism and parallelism

Orbit, pair, and cluster work is independent per center/site. Parallel tasks must use local buffers and merge
results with the documented canonical keys before assigning IDs or storing values. Floating-point reductions
are not parallelized. Deterministic tests freeze P indices, verify pair-shell and cluster representatives
against brute-force oracles, and CI repeats the golden P-index and catalog checks on Linux and Windows.

### Caches

Caches are owned by one compilation; they are not static. Keys include the catalog data version and symmetry
contract version, plus:

| Cache | Additional key inputs |
| --- | --- |
| Cell geometry | cell-parameter SHA-256 |
| Orbit / stabilizer | setting identity, exact center grid |
| Point group | setting identity, center grid, ordered geometry hash |
| Pair shells | setting identity, center P, cutoff bucket, cell hash |
| Cluster reduction | setting identity, center P, offset hash, allowed-occupation hash |
| Canonical P1 order | structure hash |

A capacity breach is a bounded failure, never an unbounded allocation. Cache insertion order never affects
results. Step 02 provides the scoped cache and key builders; Step 08 compilation wires them into the orbit,
point-group, pair, and cluster stages.

### Versioning

`SymmetryContract.Version = "symmetry-1"` covers the grid, canonical P1 order, pair ordering and bucket,
cluster canonicalization and codes, and reduction semantics. The catalog version
(`spglib-2.8.0/catalog-1`) is independent and changes only with the catalog payload or selection rule.

Changing a covered rule bumps the contract version, updates golden fixtures and signatures, and is called out
in the step page. The compiled payload records the contract version, catalog data version, and catalog SHA-256.

## Test tiers

- **PR-fast:** catalog integrity, group laws, synthetic P1/pair/cluster cases, cell geometry, minimum
  image, encoder round-trips, and the 274-setting contract checks.
- **PR-medium:** randomized property tests for orbits, pairs, clusters, and geometry.
- **Nightly/release:** larger random suites, the 20×20×20 expansion benchmark, and cross-platform test runs.
