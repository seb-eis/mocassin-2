# Transition Model

Transitions describe how particles move or exchange on the fixed crystal structure. A transition is authored as
a reusable abstract **chain** plus one or more concrete kinetic (KMC) or Metropolis (MMC) transitions bound to
reference geometry.

## Overview

```text
StateChange [donor, acceptor]
        ↓
StateChangeGroup (ordered state changes with one role)
        ↓
TransitionChain (ordered groups + D/S linkers)
        ↓
KineticTransition (reference path)  or  MetropolisTransition (two sites)
```

The model stores one reference path or site pair. Compilation expands symmetry-equivalent paths later; the
Step 04 preview is advisory and the compiled model is authoritative.

## State changes

A state change is one donor/acceptor pair:

```json
{ "id": "…", "name": "O2- -> VO", "donorParticleId": "…", "acceptorParticleId": "…" }
```

- `[D, A]` replaces donor `D` with acceptor `A` at a position.
- The implicit void particle (`ParticleId.Void`) is a valid acceptor. A void donor is allowed only for an
  explicit reverse vacancy pair.
- Donor and acceptor must differ (`TRN002`); both must not be void (`TRN003`).
- A known donor/acceptor is required (`TRN001`). A charged acceptor with a void donor warns `TRN004`.
- State-change names are unique case-insensitively within the state-change collection (`TRN017`).

## State-change groups

A group is an ordered set of state changes with the same role at one chain position:

```json
{ "id": "…", "name": "O vacancy", "stateChangeIds": ["…"] }
```

- A group must not be empty and must reference known state changes (`TRN005`).
- A group must not mix physical exchanges (different particle symbols) with charge-state exchanges (same
  symbol, different charge) (`TRN006`).
- A group must not mix stable-role pairs (acceptor not void) with unstable-role pairs (acceptor void)
  (`TRN025`).
- An unstable position's group must contain exactly one `[donor, void]` pair (`TRN026`).
- Group names are unique case-insensitively (`TRN017`).

## Chains and patterns

A chain stores the ordered group list, the connectors between adjacent positions, and the pattern:

```json
{
  "id": "…",
  "name": "3-site migration",
  "pattern": "migration3",
  "groupIds": ["…", "…", "…"],
  "linkers": ["dynamic", "dynamic"],
  "generatesReverse": true
}
```

`linkers` always has one fewer entry than `groupIds` (`TRN007`). `dynamic` performs a state exchange;
`static` is a geometric offset without an exchange.

| Pattern | Positions | Linkers | Stability sequence | Automatic reverse |
| --- | ---: | --- | --- | --- |
| `metropolis2` | 2 | `D` | S, S | Yes |
| `migration3` | 3 | `D,D` | S, U, S | Yes |
| `interstitialcy5` | 5 | `D,D,D,D` | S, U, S, U, S | No |
| `interstitialcy7` | 7 | `D,D,D,D,D,D` | S, U, S, U, S, U, S | No |
| `vehicle5` | 5 | `D,S,S,D` | S, S, U, S, S | No |
| `vehicle6` | 6 | `D,D,S,D,D` | S, U, S, S, U, S | No |
| `custom` | 2–8 | validated `D/S` sequence | inferred from the reference path | No |

- A built-in pattern has a fixed position count (`TRN008`) and linker sequence (`TRN009`).
- A 2-position custom chain is a Metropolis chain: one dynamic linker.
- A 3–8-position custom chain is kinetic. It accepts either all-dynamic `D{2,7}` or the vehicle-shaped
  `D+ S+ D+` sequence, with at least one dynamic linker on each side of the static run.
- The single-static `D S D` sequence is explicitly invalid (`TRN009`).
- `generatesReverse` must be `true` for `metropolis2` and `migration3`, and `false` for every other pattern
  (`TRN024`).
- Chain names are unique case-insensitively (`TRN017`), and every group reference must exist (`TRN021`).

## Kinetic transitions

```json
{
  "id": "…",
  "name": "O migration",
  "chainId": "…",
  "referencePath": [[0.25, 0.25, 0.25], [0.0, 0.25, 0.25], [0.25, 0.25, 0.75]],
  "reverseTransitionId": null
}
```

- `referencePath` length equals the chain position count (`TRN010`).
- Points use unit-cell fractional coordinates and may lie outside `[0,1)`. Persisted points are never wrapped.
- Endpoints must be stable (`TRN011`). Consecutive unstable positions are rejected (`TRN013`). A kinetic chain
  must have at least one unstable position (`TRN022`).
- The resolved path must match exactly one `(P, cell-offset)` pair and owning reference site per point within
  `PathMatchTolerance = 1e-6` fractional units (`TRN020`). The cell offset is the nearest integer shift with
  ties toward zero.
- Every pair of resolved points must have a distinct full encoding (`P` plus cell offset); an interior return
  or literal endpoint repeat is `TRN014`. A different periodic cell is a distinct position.
- A position's group stability must agree with the resolved site stability (`TRN012`). Built-in patterns must
  also match their full stability sequence (`TRN028`).
- A kinetic transition must not use `metropolis2` or a two-position custom chain (`TRN023`).

## Metropolis transitions

```json
{
  "id": "…",
  "name": "O exchange",
  "chainId": "…",
  "siteAId": "…",
  "siteBId": "…",
  "reverseTransitionId": null
}
```

- The chain must be `metropolis2` or a two-position custom chain (`TRN015`).
- Both sites must exist and be stable (`TRN016`). `SiteA == SiteB` is a valid intra-sublattice exchange.
- MMC transitions do not use `referencePath`; their preview path is derived from the two sites.

## Reverse links

`reverseTransitionId` is an optional same-kind link to an explicitly defined reverse transition.

- The link is symmetric: if A points to B, B must point to A.
- Self-links and cross-kind links are rejected (`TRN027`).
- Complex patterns (`interstitialcy5`, `interstitialcy7`, `vehicle5`, `vehicle6`, `custom`) never generate an
  automatic inverse; a missing explicit link warns `TRN018`.
- Kinetic and Metropolis transition names are unique within their own kind (`TRN017`); the same name may be
  used once in each kind. At most 100 transitions are allowed in total (`TRN019`).

## Preview

`Mocassin.Symmetry.Transitions.TransitionPreviewService` returns a non-persisted preview for a transition and
requested `(A, B, C)` counts:

- the transition reference and chain;
- the authored path or Metropolis site positions;
- resolved P1 positions and owning sites;
- symmetry-expanded paths or site pairs;
- dense donor/acceptor matrices for each chain position;
- the expected rule count before conservation filtering.

`ParticleOrder` is the authored particles sorted by ID followed by the implicit void particle. Each matrix is
row-major with `ParticleCount²` entries; the entry at `donorIndex * ParticleCount + acceptorIndex` is `1` when
the group contains that pair. An unstable position has exactly one `1` at `[donor, void]`; a stable position has
at least one `1`.

An invalid transition returns `null` with diagnostics and never a partial preview. Non-positive `(A, B, C)`
counts are programmer errors and throw `ArgumentOutOfRangeException`. The preview is never written to the
project.

## Editing

`ProjectEditor` exposes add/update/remove operations for state changes, groups, chains, and both transition
kinds. Removing a referenced object fails with an `InvalidDocument` diagnostic whose `related` paths list the
dependents. Pass `removeDependents: true` to cascade the removal through groups, chains, and transitions.
Duplicating a chain or transition creates a new ID, a unique name, and no copied reverse link.

## Validation codes

| Code | Severity | Meaning |
| --- | --- | --- |
| `TRN001` | Error | Unknown particle reference. |
| `TRN002` | Error | Donor equals acceptor. |
| `TRN003` | Error | Both particles are void. |
| `TRN004` | Warning | Charged acceptor from a void donor. |
| `TRN005` | Error | Group is empty or references an unknown state change. |
| `TRN006` | Error | Group mixes physical and charge-state exchanges. |
| `TRN007` | Error | Group/linker count mismatch. |
| `TRN008` | Error | Wrong pattern position count. |
| `TRN009` | Error | Invalid linker sequence for the pattern or custom grammar. |
| `TRN010` | Error | Kinetic path length differs from the chain position count. |
| `TRN011` | Error | Kinetic path endpoint is not stable. |
| `TRN012` | Error | Group stability disagrees with the resolved site. |
| `TRN013` | Error | Consecutive unstable positions. |
| `TRN014` | Error | Repeated full encoded path position. |
| `TRN015` | Error | Metropolis transition uses an incompatible chain. |
| `TRN016` | Error | Metropolis site is unknown, unstable, or invalid. |
| `TRN017` | Error | Name is empty or duplicated within its collection. |
| `TRN018` | Warning | Complex transition has no explicit reverse link. |
| `TRN019` | Error | More than 100 transitions. |
| `TRN020` | Error | Path point is non-finite or does not match exactly one P1 position. |
| `TRN021` | Error | Chain references an unknown group. |
| `TRN022` | Error | Kinetic chain has no unstable position. |
| `TRN023` | Error | Kinetic transition uses a Metropolis chain. |
| `TRN024` | Error | `generatesReverse` disagrees with the pattern. |
| `TRN025` | Error | Group mixes stable-role and unstable-role pairs. |
| `TRN026` | Error | Unstable position group does not contain exactly one `[donor, void]`. |
| `TRN027` | Error | Reverse link is unknown, cross-kind, asymmetric, or self-referential. |
| `TRN028` | Error | Resolved path stability sequence disagrees with the built-in pattern. |

See [Transitions reference](../reference/transitions.md) for the JSON fields and semantic ordering.
