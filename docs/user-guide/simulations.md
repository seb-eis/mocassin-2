# Simulations

A simulation defines the temperature, run length, sampling, and transition set for one KMC or MMC run. Job
expansion, effective job counts, supercell generation, and package writing belong to Steps 10 and 11.

## Kinds

Two concrete simulation records exist:

- **KMC** (`SimulationKind.Kmc`, persisted as `kmc`) references kinetic transitions.
- **MMC** (`SimulationKind.Mmc`, persisted as `mmc`) references Metropolis transitions.

The kind is derived from the concrete record and is not user-settable. The project JSON carries `kind` so the
reader can choose the record type; a kind that disagrees with the transition array or kind-specific settings is
`SIM023`. Switching kind is a delete-and-create editor operation that requires the transition references to be
removed first.

The persisted simulation is a flat object with the shared identifier and name, the matching transition array,
and a `settings` object:

```json
{
  "id": "…",
  "name": "O migration 773 K",
  "kind": "kmc",
  "kineticTransitionIds": ["…"],
  "settings": {
    "temperatureK": 773.0,
    "targetMcsp": 200,
    "writeOutCount": 100,
    "timeLimitSeconds": 31536000,
    "minSuccessRate": null,
    "jobCount": 12,
    "rngSeed": null,
    "executionFlags": [],
    "instruction": null,
    "preRunMcsp": 250,
    "electricFieldModulusVm": 1.0e7,
    "electricFieldDirection": [1.0, 0.0, 0.0],
    "normalizationEnergyEv": null,
    "maxAttemptFrequencyHz": null
  }
}
```

`kineticTransitionIds` and `metropolisTransitionIds` preserve authored order. Duplicates of the same
`(kind, id)` reference are rejected; a simulation may reference transitions that other simulations also use.

## Common settings

| Setting | Default | Hard range | Recommended |
| --- | --- | --- | --- |
| `temperatureK` | 1000 K | 0.1–10,000 K | 100–5000 K |
| `targetMcsp` | 1000 | 1–1,000,000 | up to 1000 |
| `writeOutCount` | 100 | 0–1000 | 0 or 5–100 |
| `timeLimitSeconds` | 31,536,000 s (P1Y) | positive or `null` | — |
| `minSuccessRate` | `null` (disabled) | `null`, 0, or 0–`int.MaxValue` MCS/s | at least 10 MCS/s |
| `jobCount` | 12 | 1–1000 | 5–100 |
| `rngSeed` | `null` | at most 100 characters | — |
| `executionFlags` | `[]` (`None`) | sorted authored flags | — |

`targetMcsp` is the number of successful steps per mobile particle. `writeOutCount` is the number of
intermediate data-out calls; `0` writes only the final state. `timeLimitSeconds` is the wall-clock limit;
`null` means no limit. `minSuccessRate` is in MCS/s, not a fraction; `0` and `null` both disable the abort and
`0` canonicalizes to `null`. An empty `rngSeed` canonicalizes to `null` and derives the seed from the
project/job seed (ADR 0005).

## KMC settings

| Setting | Default | Hard range | Recommended |
| --- | --- | --- | --- |
| `preRunMcsp` | 100 | 0–1,000,000 | up to 1000 |
| `electricFieldModulusVm` | 1.0e7 V/m | non-negative, finite | 1e4–1e9 V/m |
| `electricFieldDirection` | `(1, 0, 0)` | three finite components; non-zero for a non-zero field | — |
| `normalizationEnergyEv` | `null` (none) | `null`, 0, or positive finite | up to 10 eV |
| `maxAttemptFrequencyHz` | `null` (from rules) | `null` or positive finite | — |

`preRunMcsp = 0` disables the relaxation pre-run. `normalizationEnergyEv` `0.0` is accepted and canonicalizes
to `null`. A positive `maxAttemptFrequencyHz` rescales all frequencies while preserving ratios; it is the
simulation-level default for Step 10 job templates.

## MMC settings

| Setting | Default | Hard range | Recommended |
| --- | --- | --- | --- |
| `breakTolerance` | 0 (disabled) | 0–1 | up to 0.1 |
| `breakSampleLengthMcsp` | 1000 | 0–100,000 | up to 10,000 |
| `breakSampleIntervalMcsp` | 100 | 1–1000 | up to 100 |
| `resultSampleMcsp` | 200 | 0–1,000,000 | up to 10,000 |

## Execution flags

`executionFlags` is a sorted array of camelCase names:

- `noSaving` skips state and checkpoint writes;
- `noJumpLogging` disables the KMC jump histogram;
- `useFastExp` selects the documented fast exponential approximation;
- `deterministic` selects the canonical RNG-consumption path by disabling the Step 16 frequency pre-check and
  optional shortcuts.

The `.msl` job payload uses the separate `SolverJobFlags` word. Kind, pre-run, and dual-DOF bits are derived by
the compiler/job builder and must not appear in the project array. `LAT`/`SIM` validation rejects duplicate,
derived, or unknown flags.

## Validation and ownership

Step 06 owns `SIM001`–`SIM037`: references, kind compatibility, names, ranges, recommended-band warnings,
transition-count limits, duplicate references, seed length, flags, and canonical values. Step 07/10 remains
responsible for build-time feasibility when a simulation has no transitions. Step 10 owns job expansion,
effective job counts, and total-job limits; Step 11 writes the package.

See the [simulation samples](https://github.com/seb-eis/mocassin-2/tree/main/samples/simulations) for a KMC
project with a non-default 773 K configuration and an MMC project with a non-default break tolerance.
