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
