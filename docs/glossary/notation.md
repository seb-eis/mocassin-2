# Notation, Identifiers, and Units

This page records the conventions introduced by the project format. The physics notation and simulation
symbols are expanded as later steps land.

## Identifiers

| Convention | Meaning |
| --- | --- |
| `ParticleId`, `ParticleSetId`, `CellSiteId`, … | Strongly typed wrappers over `Guid`. |
| UUIDv7 | Default generator for new project objects; sortable by creation time. |
| lowercase hyphenated UUID | JSON representation of every project identifier. |
| reserved ID | Fixed GUID for an implicit object such as the void particle or empty particle set. |
| `ParticleIndex`, `PositionIndex`, `RuleIndex` | Dense compiled indices; valid only inside one compiled package and never stored in `.mocprj`. |

References use typed IDs, not array positions. Non-semantic arrays are sorted by ID when serialized.

## Units

| Quantity | Type | Input unit | Transport/output |
| --- | --- | --- | --- |
| Length | `LengthAngstrom` | ångström (Å) | Å where shown; SI for transport calculations |
| Energy | `EnergyEv` | electronvolt (eV) | eV in the physics core |
| Temperature | `TemperatureKelvin` | kelvin (K) | K |
| Charge | `ChargeElementary` | elementary charge | elementary charge in the model; SI where required |
| Time | `TimeSeconds` | second (s) | s |
| Frequency | `FrequencyHz` | hertz (Hz) | Hz |
| Electric field | `ElectricFieldVm` | volt per metre (V/m) | V/m |
| Angle | `AngleDegrees` | degree | degrees in the model |

Rules:

- Unit structs validate finiteness and their allowed range.
- There are no implicit conversions between unit types; wrapping a bare `double` is explicit.
- JSON stores a plain number; the field name or schema description carries the unit.
- Unit-cell counts are `A*B*C`, never `A+B+C`.

## Fractional coordinates

- The project stores fractional coordinates as decimal numbers.
- Symmetry expands and orders positions on an internal exact grid with denominator `24 × 10^12`.
- The grid exactly represents any input decimal with up to 12 fractional digits and is closed under all
  crystallographic operations.
- Inputs with more than 12 fractional digits are rounded to the nearest grid point and may produce a warning.
- Fractional coordinates are converted to Cartesian coordinates only at the geometry boundary.

## Cell geometry

- Cell lengths are in ångström and must be in `0.1..1000 Å`.
- Cell angles are in degrees and must satisfy `0 < α, β, γ < 180°` and a positive Gram determinant.
- Crystal-system constraints fix equal lengths/angles, 90°/120° values, the monoclinic unique axis, and the
  rhombohedral angle range.
- Cell vectors use the standard right-handed convention: a along x, b in the xy-plane, and c with a positive
  z-component.
- Inconsistent parameters are rejected; the model never silently normalizes user input.

## P1 positions

- P1 positions are sorted by exact fractional-grid coordinates; `P` is the per-cell index in `[0, Pcount)`.
- `Vector4I(cellA, cellB, cellC, P)` combines a cell offset with the per-cell P index.
- The linear lattice index is
  `P + cellC*Pcount + cellB*Pcount*Ccount + cellA*Pcount*Ccount*Bcount`.
- The index is a signed 32-bit integer; the total lattice position count is checked in 64-bit and must not
  exceed `2,147,483,647`.
- Negative cell offsets are relative-path coordinates, not linear indices.

## Symmetry reduction

For a center site `c` and ordered site sequence `S = (s0, ..., sN-1)`:

- `G0` — center stabilizer: operations that map `c` to itself modulo lattice translation.
- `Gset` — setwise stabilizer: operations in `G0` that map the unordered set `{S}` onto itself.
- `Gseq` — pointwise stabilizer: operations in `G0` that fix every `si` individually.
- `P ≅ Gset / Gseq` — induced permutation group on the site indices.

`Gseq` is normal in `Gset`, and `Gseq ⊆ Gset ⊆ G0`.

Legacy names from the original implementation map as follows:

- `OrderPreservingUniqueSequenceOperations` = a transversal of `G0 / Gseq`.
- `OrderIgnoringUniqueSequenceOperations` = a transversal of `G0 / Gset`.
- `OrderIgnoringSelfProjectionOperations` = `Gset`.
- `SelfProjectionOrders` = the induced site permutations in `P`.

A **chiral partner** is the representative of the inverse orbit when the inverse direction is not in the same
`G0` orbit. This is an orbit relationship, not physical chirality; in P1 every directed pair is its own orbit.

## Periodic geometry

- The cell matrix `M` has columns a, b, c; `G = MᵀM` is the metric tensor.
- `GetMinimumImage` returns the shortest periodic image and its offset; `RadialSearch` returns every image
  within the cutoff.
- The search box uses the interplanar spacings `d_a = V/|b×c|`, `d_b = V/|c×a|`, and `d_c = V/|a×b|`.
- Distances use `d² = dfᵀGdf`.
- Pair and cluster distance buckets are integer femtometres (`1 fm = 1e-5 Å`); the cutoff includes candidates
  up to the cutoff bucket with a 1 fm margin.
- Negative cell offsets remain explicit; no implicit wrapping.

## Time and seeds

- `createdUtc` and `modifiedUtc` are ISO-8601 UTC timestamps.
- Canonical output uses seven fractional digits and a trailing `Z`.
- `masterSeed` is exactly 32 bytes encoded as 64 lowercase hexadecimal characters.
- SHA-256 is used for content hashes and deterministic seed derivation. `string.GetHashCode()` is never used
  for persisted identity.

## Diagnostics

Diagnostic codes are stable uppercase strings with a domain prefix and three digits:

| Prefix | Area |
| --- | --- |
| `PROJ` | Project format, migration, persistence, locking |
| `PAR` | Particles |
| `STR` | Structure |
| `SYM` | Symmetry |
| `JOB` | Job templates and supercells |
| `PRM` | Parameterization |

Step 01 defines `PROJ001`–`PROJ033`; the complete catalogue is finalized in Step 07.

## JSON notation

- Property names use camelCase.
- Enums use camelCase strings (`"perspective"`, `"orthographic"`).
- Unknown properties are rejected at the project envelope and known nested objects; the per-object style map
  explicitly permits additional primitive keys.
- JSON is UTF-8, LF-only, without a byte-order mark.
