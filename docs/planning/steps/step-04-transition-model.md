# Step 04 — Transition Model

**Phase:** B. Model definition  
**Depends on:** 03
**Delivers:** State changes, state-change groups, abstract chains, and KMC/MMC transitions bound to reference
geometry, with validation of transition semantics.

## Implementation status

- **Status:** Not started
- **Step branch:** —
- **Step CI:** —
- **Main merge:** —
- **Step tag:** —
- **Audit:** Not requested
- **Design review:** Not requested
- **Last updated:** —

| Feature | Branch | Status | Local gates | Step CI | Squash commit |
| --- | --- | --- | --- | --- | --- |
| — | — | — | — | — | — |

## Goal

Let a user describe every supported transport mechanism abstractly, bind it to symmetry-reduced reference
geometry, and validate that the definition is physically meaningful before rule generation.

## Original reference

- `IStateExchangePair` — donor and acceptor particles; exactly one vacancy for a vacancy pair; acceptor is void
  for an unstable-position pair.
- `IStateExchangeGroup` — a set of pairs with the same role in a chain; may not mix physical exchange and
  charge-state exchange.
- `IAbstractTransition` — a chain of state-change groups with `StateCount` and `StateCount − 1` connectors of
  type `Dynamic` or `Static`; `IsMetropolis` when `StateCount == 2`; `IsAssociation` flag.
- `IKineticTransition` — an abstract chain plus a 3D reference path; geometry count equals state count;
  endpoints stable, intermediates unstable, no ring, no consecutive unstable positions.
- `IMetropolisTransition` — an abstract chain (2-site) plus two reference cell sites; selecting the same site
  twice means an intra-sublattice exchange.
- Original connector patterns:
  - `D` — 2-site Metropolis;
  - `D-D+` — basic kinetic migration (3 sites);
  - `D+ SS D+` — normal vehicle (5 sites);
  - `DD (S DD)+` — split vehicle (6 sites).
- Original supported mechanisms: 2-site MMC, 3-site migration, 5-site interstitialcy, 7-site interstitialcy,
  5-site 2-species vehicle, 6-site 2-species vehicle, charge transfer/polaron hop.
- Original limits: 100 transitions, 2–8 positions per chain.
- Original defects: empty-list indexing in the normal-vehicle validation, disabled self-exchange check,
  ambiguous end-state selection, conservation enforced only during rule generation.

## Design

### 1. State changes

```csharp
public sealed record StateChange(
    StateChangeId Id,
    string Name,
    ParticleId DonorParticleId,
    ParticleId AcceptorParticleId);

public sealed record StateChangeGroup(
    StateChangeGroupId Id,
    string Name,
    ImmutableArray<StateChangeId> StateChangeIds);
```

- `[D, A]` means donor `D` is replaced by acceptor `A` at a position.
- The void particle is always a valid acceptor. It is never a donor for stable positions.
- A group is a set of state changes with the same role. It may contain one or many pairs (e.g. all halides
  migrating into a vacancy).
- A group must not mix a physical exchange (both particles non-void, same/different symbols) with a
  charge-state exchange (same symbol, different charge) unless the chain is explicitly a charge-transfer
  chain. This prevents physically invalid rule combinations.
- A state change with donor == acceptor is rejected (no-op).
- A state change where the acceptor is void and the donor is void is rejected.

### 2. Chains

```csharp
public enum TransitionPattern
{
    Metropolis2,
    Migration3,
    Interstitialcy5,
    Interstitialcy7,
    Vehicle5,
    Vehicle6,
    Custom
}

public enum TransitionLinker { Dynamic, Static }

public sealed record TransitionChain(
    TransitionChainId Id,
    string Name,
    TransitionPattern Pattern,
    ImmutableArray<StateChangeGroupId> GroupIds,
    ImmutableArray<TransitionLinker> Linkers,
    bool GeneratesReverse);
```

Pattern rules:

| Pattern | Positions | Linkers | Stability sequence | Reverse generated |
| --- | ---: | --- | --- | --- |
| `Metropolis2` | 2 | `D` | S, S | Yes |
| `Migration3` | 3 | `D,D` | S, U, S | Yes |
| `Interstitialcy5` | 5 | `D,D,D,D` | S, U, S, U, S | No |
| `Interstitialcy7` | 7 | `D,D,D,D,D,D` | S, U, S, U, S, U, S | No |
| `Vehicle5` | 5 | `D,S,S,D` | S, S, U, S, S | No |
| `Vehicle6` | 6 | `D,D,S,D,D` | S, U, S, S, U, S | No |
| `Custom` | 2–8 | valid `D/S` sequence | inferred from reference path | Explicit |

(S = stable, U = unstable.)

- `Custom` exists to preserve the original's regex-validated connector strings without hard-coding only the
  six common patterns. Its stability roles are inferred from the reference path and must satisfy:
  - length = positions − 1;
  - a 2-position chain is Metropolis and uses `D`;
  - endpoints are stable;
  - a kinetic chain has at least one unstable position and no two consecutive unstable positions;
  - every unstable position uses a `[D,∅]` chain group; every stable position uses a `[D,A]` group;
  - static linkers must connect positions that exist in the reference geometry.
- `GeneratesReverse` is true only for `Metropolis2` and `Migration3`. Complex mechanisms never generate an
  inverse automatically; the user must define the reverse mechanism explicitly if needed.
- Chain groups are ordered and semantically meaningful.
- A chain may reference a group more than once (e.g. the same `[A,V]` group at both ends).

### 3. KMC transitions

```csharp
public sealed record KineticTransition(
    KineticTransitionId Id,
    string Name,
    TransitionChainId ChainId,
    ImmutableArray<Fractional3> ReferencePath);

public sealed record MetropolisTransition(
    MetropolisTransitionId Id,
    string Name,
    TransitionChainId ChainId,
    CellSiteId SiteAId,
    CellSiteId SiteBId);
```

- `ReferencePath` length equals the chain position count.
- The path is expressed in unit-cell fractional coordinates and may leave `[0,1)`.
- The path's stability sequence must match the pattern’s **Stability sequence** in the table above.
  In particular, vehicles are not simple `S,U,S` migrations: `Vehicle5` is `S,S,U,S,S` and `Vehicle6` is
  `S,U,S,S,U,S`.
- The path must not form a ring (first point != last point) and must not contain consecutive unstable
  positions for simple mechanisms.
- Metropolis transitions use exactly two reference sites. `SiteA == SiteB` means an exchange within one
  sublattice.
- A transition name is unique within its kind.
- A transition may be referenced by multiple simulations.

### 4. Symmetry extension

- The model stores one reference path/site pair; step 08 generates all symmetry-equivalent paths and rules.
- The preview API (`TransitionPreview`) shows:
  - the reference path in fractional and Cartesian coordinates;
  - the symmetry-expanded paths (count and endpoints);
  - the state-change matrices for the chain;
  - the number of expected rules before conservation filtering.
- Preview is advisory; the compiled model is authoritative.

### 5. Validation rules

| Code | Severity | Rule |
| --- | --- | --- |
| `TRN001` | Error | State change references unknown particle |
| `TRN002` | Error | Donor equals acceptor |
| `TRN003` | Error | Both donor and acceptor are void |
| `TRN004` | Warning | Acceptor is a charged particle but donor is void |
| `TRN005` | Error | Group is empty or references unknown state change |
| `TRN006` | Error | Group mixes physical and charge-state exchanges |
| `TRN007` | Error | Chain group count/linker count mismatch |
| `TRN008` | Error | Chain pattern has the wrong number of positions |
| `TRN009` | Error | Linker sequence invalid for the pattern |
| `TRN010` | Error | Kinetic path length ≠ chain position count |
| `TRN011` | Error | Kinetic path endpoints are not stable |
| `TRN012` | Error | Kinetic path intermediate is not unstable |
| `TRN013` | Error | Consecutive unstable positions |
| `TRN014` | Error | Path is a ring |
| `TRN015` | Error | Metropolis transition does not use a 2-position chain |
| `TRN016` | Error | Metropolis site reference invalid |
| `TRN017` | Error | Duplicate transition name |
| `TRN018` | Warning | Complex transition without an explicitly defined reverse |
| `TRN019` | Error | More than 100 transitions |
| `TRN020` | Warning | Transition path leaves the interaction range |

- Conservation of mass and charge is checked during rule generation (step 08), where the full state triplet is
  known. Step 04 only checks definition-level consistency.
- The validator reports the exact position index and group index for path errors.

### 6. Editing operations

- Add/update/remove state change, group, chain, kinetic transition, metropolis transition.
- Removing an object that is referenced by another object produces a diagnostic and a list of dependents;
  the editor offers "remove dependents" as an explicit operation.
- Duplicating a chain/transition creates new IDs and keeps references consistent.
- "Make group from selected state changes" is a UI convenience backed by a single editor operation.

## Implementation tasks

- [ ] Implement `StateChange`, `StateChangeGroup`, `TransitionChain`, `KineticTransition`,
  `MetropolisTransition`.
- [ ] Implement pattern/linker validation, including `Custom`.
- [ ] Implement path/stability validation.
- [ ] Implement transition preview using the symmetry engine.
- [ ] Implement reference and duplicate-name validation.
- [ ] Implement editor operations and dependent-removal diagnostics.
- [ ] Add ceria KMC (oxygen migration), ceria MMC (O/V exchange), and polaron-hop samples.
- [ ] Add a 5-site vehicle and a 5-site interstitialcy sample.
- [ ] Add diagnostics/help text.

## Acceptance criteria

- [ ] Ceria oxygen migration can be defined as a 3-site chain with `[O,V_O]`, `[O,∅]`, `[O,V_O]` and a
  stable-unstable-stable path.
- [ ] A 2-site MMC exchange `[A,B]` can be defined and previewed.
- [ ] A 5-site vehicle with five chain groups (`[A,V_A]`, `[A,V_A]`, `[AB,∅]`, `[B,V_B]`, `[B,V_B]`),
  a `D,S,S,D` chain, and an `S,S,U,S,S` reference path validates.
- [ ] Invalid paths (unstable endpoint, consecutive unstable, ring) are rejected with precise diagnostics.
- [ ] Complex transitions warn when no reverse is defined.
- [ ] The preview enumerates the expected symmetry-equivalent paths.
- [ ] Documentation written

## Documentation

At this point, document the transition model:

- `docs/user-guide/transitions.md` — state changes, groups, chains, pattern/linker rules, KMC/MMC paths.
- `docs/physics/monte-carlo.md` and `docs/physics/kmc.md` — transition mechanisms and state triplets.
- `docs/physics/mmc.md` — two-site exchange semantics.
- `docs/development/compilation-pipeline.md` — transition inputs and the deferred rule-generation stage.

## Tests

| Test | Assertion |
| --- | --- |
| State changes | Donor/acceptor/void validation |
| Groups | Mixed physical/charge exchange rejected |
| Patterns | Every built-in pattern accepts only its valid linker sequence |
| Custom | Valid/invalid custom chains |
| Paths | Stability, length, ring, consecutive-unstable checks |
| Metropolis | Same-site and different-site exchanges |
| Preview | Symmetry-expanded path count matches brute force |
| Editing | Remove/duplicate keeps references valid |
| Samples | Ceria and vehicle fixtures validate |

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Ambiguous vehicle semantics | Encode the documented unidirectional behavior; add a golden vehicle model |
| Custom chains create unsupported mechanisms | Restrict to the documented linker grammar; rule generation rejects unsupported cases |
| Reverse-generation confusion | Explicit flag and warning; never silently generate for complex mechanisms |
| Path stability mismatch with energy model | Validate against the structure and revalidate in step 08 |

## Done when

All original transition mechanisms can be defined, previewed, and validated, and invalid definitions produce
actionable diagnostics before compilation.
