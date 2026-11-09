# Transitions Reference

Transition sections are flat members of `model`. Missing sections load as empty arrays. The project
`schemaVersion` remains `1` because the sections are additive under the pre-release v1 format.

## JSON fields

### `stateChanges`

| Field | Type | Required | Meaning |
| --- | --- | --- | --- |
| `id` | UUID string | yes | Stable `StateChangeId`. |
| `name` | string | yes | Unique case-insensitive name. |
| `donorParticleId` | UUID string | yes | Replaced particle; may be the implicit void. |
| `acceptorParticleId` | UUID string | yes | Replacement particle; may be the implicit void. |

### `stateChangeGroups`

| Field | Type | Required | Meaning |
| --- | --- | --- | --- |
| `id` | UUID string | yes | Stable `StateChangeGroupId`. |
| `name` | string | yes | Unique case-insensitive name. |
| `stateChangeIds` | UUID string array | yes | Semantic group role order. |

### `chains`

| Field | Type | Required | Meaning |
| --- | --- | --- | --- |
| `id` | UUID string | yes | Stable `TransitionChainId`. |
| `name` | string | yes | Unique case-insensitive name. |
| `pattern` | camelCase string | yes | `metropolis2`, `migration3`, `interstitialcy5`, `interstitialcy7`, `vehicle5`, `vehicle6`, or `custom`. |
| `groupIds` | UUID string array | yes | Semantic position order. |
| `linkers` | string array | yes | `dynamic` or `static`; length is `groupIds.length - 1`. |
| `generatesReverse` | bool | yes | True only for `metropolis2` and `migration3`. |

### `kineticTransitions`

| Field | Type | Required | Meaning |
| --- | --- | --- | --- |
| `id` | UUID string | yes | Stable `KineticTransitionId`. |
| `name` | string | yes | Unique within the kinetic collection. |
| `chainId` | UUID string | yes | Referenced chain. |
| `referencePath` | array of `[a,b,c]` arrays | yes | Semantic position order; fractional coordinates may leave `[0,1)`. |
| `reverseTransitionId` | UUID string | no | Optional symmetric same-kind reverse link. |

### `metropolisTransitions`

| Field | Type | Required | Meaning |
| --- | --- | --- | --- |
| `id` | UUID string | yes | Stable `MetropolisTransitionId`. |
| `name` | string | yes | Unique within the Metropolis collection. |
| `chainId` | UUID string | yes | Referenced chain. |
| `siteAId` | UUID string | yes | First stable reference site. |
| `siteBId` | UUID string | yes | Second stable reference site; may equal `siteAId`. |
| `reverseTransitionId` | UUID string | no | Optional symmetric same-kind reverse link. |

## Semantic order

Canonical JSON sorts `stateChanges`, `stateChangeGroups`, `chains`, `kineticTransitions`, and
`metropolisTransitions` by `id`. It never re-sorts:

- `stateChangeGroups[].stateChangeIds`;
- `chains[].groupIds`;
- `chains[].linkers`;
- `kineticTransitions[].referencePath`.

`linkers` and `groupIds` therefore keep their authored position order through every save/load and hash.

## Canonical hashing

A transition edit changes both `ProjectHasher.Compute` (canonical model) and `ProjectHasher.ComputeDocument`
(canonical document). Non-semantic collection order is ignored; semantic array order changes the hash.
`ProjectHasher.ComputeTransition(project, transitionRef)` hashes the transition closure: the transition, its
chain, referenced groups and state changes, and the full particle order including the implicit void. It is the
preview cache identity.

Empty transition sections are omitted from the written model so transition-free projects keep their Step 01/03
canonical hashes. Missing sections are always accepted as empty.

## Path matching

- `referencePath` points use unit-cell fractional coordinates.
- For every component, the nearest integer cell shift is used; ties resolve toward zero.
- All component differences must be at most `PathMatchTolerance = 1e-6`.
- A point must match exactly one `(P, cell offset)` pair and owning reference site.
- The persisted point is not wrapped. A different periodic cell is a distinct physical position.
- Ring detection compares every pair of full encodings.

## Diagnostics

| Code | Severity | Rule |
| --- | --- | --- |
| `TRN001` | Error | State change references an unknown particle. |
| `TRN002` | Error | Donor equals acceptor. |
| `TRN003` | Error | Both donor and acceptor are void. |
| `TRN004` | Warning | Charged acceptor from a void donor. |
| `TRN005` | Error | Group is empty or references an unknown state change. |
| `TRN006` | Error | Group mixes physical and charge-state exchanges. |
| `TRN007` | Error | Group count/linker count mismatch. |
| `TRN008` | Error | Pattern has the wrong position count. |
| `TRN009` | Error | Invalid built-in or custom linker sequence. |
| `TRN010` | Error | Kinetic path length differs from chain position count. |
| `TRN011` | Error | Kinetic path endpoint is not stable. |
| `TRN012` | Error | Group stability disagrees with the resolved site. |
| `TRN013` | Error | Consecutive unstable positions. |
| `TRN014` | Error | Repeated full encoded path position. |
| `TRN015` | Error | Metropolis transition uses an incompatible chain. |
| `TRN016` | Error | Metropolis site is unknown, unstable, or invalid. |
| `TRN017` | Error | Name is empty or duplicated within its collection. |
| `TRN018` | Warning | Complex transition has no explicit reverse link. |
| `TRN019` | Error | More than 100 transitions in total. |
| `TRN020` | Error | Path point is non-finite or does not match exactly one P1 position. |
| `TRN021` | Error | Chain references an unknown group. |
| `TRN022` | Error | Kinetic chain has no unstable position. |
| `TRN023` | Error | Kinetic transition uses a Metropolis chain. |
| `TRN024` | Error | `generatesReverse` disagrees with the pattern. |
| `TRN025` | Error | Group mixes stable-role and unstable-role pairs. |
| `TRN026` | Error | Unstable position group does not contain exactly one `[donor, void]`. |
| `TRN027` | Error | Reverse link is unknown, cross-kind, asymmetric, or self-referential. |
| `TRN028` | Error | Resolved path stability sequence disagrees with the built-in pattern. |

Definition-level codes come from `Mocassin.Core.Model.Transitions.TransitionModelValidator`. Resolved-path and
site codes come from `Mocassin.Symmetry.Transitions.TransitionPathValidator`. The diagnostics page with help
anchors is consolidated in Step 07.

## Preview contract

`TransitionPreview` is derived data and is never persisted. Its `StateChangeMatrices` are dense row-major
matrices of `ParticleCount²` integers. `ParticleOrder` is authored particles sorted by ID followed by the
implicit void particle. The entry at `donorIndex * ParticleCount + acceptorIndex` is `1` when the group contains
that pair. An unstable position has exactly one `1` at `[donor, void]`; a stable position has at least one `1`.
An invalid transition returns `null` and diagnostics, never a partial preview.
