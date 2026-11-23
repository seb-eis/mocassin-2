# Particles and Particle Sets

Particles describe the species and pseudo-particles that can occupy fixed crystal sites. Positions themselves
belong to the [crystal structure](structure.md); a particle is the state that can be assigned to a site.

## Particle fields

| JSON field | Meaning |
| --- | --- |
| `id` | Stable UUID identifier. References use the typed `ParticleId`, never an array index. |
| `name` | User-visible name. Names are unique within the project, case-insensitively. |
| `symbol` | Mass-conservation group. Species with the same symbol may exchange without violating mass conservation. |
| `charge` | Charge in units of the elementary charge, restricted to the finite range −1000..1000. |
| `isVacancy` | Marks a pseudo-particle that represents an empty state. |

`isVoid` is computed from the reserved `ParticleId.Void` value and is never serialized as a project field.

The recommended symbol pattern is `^[A-Z][A-Za-z0-9+\-()]{0,10}$`. A symbol that does not match emits the
`PAR008` warning rather than an error, so existing scientific labels remain usable.

Use a distinct symbol for the vacancy pseudo-particle, for example `Vc` for the ceria oxygen vacancy or `V`
for a generic vacancy. The symbol is a mass-conservation tag: a vacancy that shares the host symbol makes a
vacancy exchange look like a same-symbol/no-movement event and defeats vacancy tracking and mass-conservation
analysis.

One vacancy particle can represent all uncharged vacancy types in a model, including oxygen vacancies and
empty interstitial positions. Define additional vacancy particles only when you need distinct charged vacancy
states, for example polaron hopping along vacancies.

## The implicit void particle

The void particle always exists and is not stored in `model.particles`:

- reserved identifier `ParticleId.Void`;
- symbol `∅`;
- charge `0`;
- not a vacancy.

`IParticleQuery.Find(ParticleId.Void)` returns it, and `IParticleQuery.VoidParticle` exposes the same object.
An authored particle must not reuse the reserved identifier (`PAR010`). The compiled particle index `0` is
assigned later by the model compiler; it is not part of the Step 03 model.

## Vacancies and charged defaults

A vacancy is a pseudo-particle that represents an empty state. It can be used for several mechanisms or
sites so the solver can track them individually. A charged vacancy is valid, but a particle set must not have
both a charged vacancy and a charged default occupation: charge transport would cancel for a vacancy
migration. That case emits the `PAR004` warning; set either the vacancy or the default occupation to charge
zero.

## Particle sets

A particle set is an ordered list of particle identifiers. The order is semantic: the first particle is the
default occupation of a stable site. Unstable sites always default to the implicit void.

- The set must not be empty and must not contain the void (`PAR005`).
- Every member must resolve to an authored particle (`PAR006`).
- Names are unique case-insensitively and must not be empty (`PAR011`).
- A repeated member identifier is `PAR012`.
- Two sets with the same ordered member list are allowed but emit the `PAR009` warning; they are not merged
  automatically.
- `ParticleSetId.Empty` is reserved for the implicit empty set and must not be authored (`PAR010`).

The limits are 63 custom particles (the 64th slot is the void) and 100 particle sets. Exceeding a limit emits
`PAR002` or `PAR007`.

## JSON example

```json
{
  "particles": [
    {
      "id": "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5f01",
      "name": "O2-",
      "symbol": "O",
      "charge": -2.0,
      "isVacancy": false
    },
    {
      "id": "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5f02",
      "name": "Vacancy",
      "symbol": "V",
      "charge": 0.0,
      "isVacancy": true
    }
  ],
  "particleSets": [
    {
      "id": "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5f04",
      "name": "Oxygen site",
      "particleIds": [
        "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5f01",
        "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5f02"
      ]
    }
  ]
}
```

Canonical project JSON sorts `particles` and `particleSets` by ID. The `particleIds` order is preserved
because it is semantic. See the [project format](../reference/project-format.md) for the complete envelope.

## Validation

| Code | Severity | Meaning |
| --- | --- | --- |
| `PAR001` | Error | Particle name is empty or duplicated case-insensitively. |
| `PAR002` | Error | More than 63 custom particles. |
| `PAR004` | Warning | A set contains a charged vacancy and a charged default occupation. |
| `PAR005` | Error | Set is empty or contains the void particle. |
| `PAR006` | Error | Set references an unknown particle. |
| `PAR007` | Error | More than 100 particle sets. |
| `PAR008` | Warning | Symbol is empty, too long, or does not match the recommended pattern. |
| `PAR009` | Warning | Two sets have the same ordered member list. |
| `PAR010` | Error | Authored object reuses `ParticleId.Void` or `ParticleSetId.Empty`. |
| `PAR011` | Error | Set name is empty or duplicated case-insensitively. |
| `PAR012` | Error | Set contains the same particle more than once. |

`PAR003` is not used in v1. An out-of-range charge is rejected by the typed value or JSON reader as
`PROJ002`.

## Editing particles and sets in the GUI

Open **Particles** or **Particle sets** from the **Model** menu or the project explorer's **Model** node. Both
editors use the shared model-table surface: search, a severity filter, typed column sorting, and multi-select.

### Particles

- The table shows name, symbol, charge, vacancy, authored index, usage count, and a status column. The implicit
  **Void** particle is pinned first, marked read-only, and explained in the detail pane; it is never persisted and
  is not counted as an authored particle.
- **Add** creates `Particle 1`, `Particle 2`, … (case-insensitive unique) with charge `0`.
- Name, symbol, charge, and the vacancy toggle each commit on `Enter` or focus loss as one undo entry. **Apply**
  commits the whole form as one entry; **Revert** restores the last committed values.
- **Duplicate** copies every field under a new id and a unique name.
- Select several authored particles and use the bulk **Set charge** action to change them all in **one** undo
  entry; a mid-batch rejection changes nothing.
- Inline messages: `PAR001` for an empty or duplicate name, `PAR008` when the symbol does not match the
  recommended pattern, and `PAR004` when a charged vacancy could pair with a charged default occupation.
- **Delete** is blocked while the particle is referenced (by a set, a state change, or a transition). The
  editor lists the dependents and **Show usages**; delete the dependents in their own domain first. The Core
  structural validator rejects the same dangling reference, so the pre-check and the rejection agree.

### Particle sets

- The table shows name, ordered member count, usage count, and status. The detail carries its own
  **Project particles** list as the drag source, so membership works inside one document.
- Drag a particle onto the member list, or select it and use **Add**. **Remove**, **Move up**, and **Move down**
  are the keyboard equivalents of every drag action; both paths produce the same order.
- The first member carries the **default occupation** badge and appears first in the occupation-chip preview.
- The void cannot be a member (`PAR005`), a repeated member is refused (`PAR012`), and an empty set is invalid
  (`PAR005`): the error is inline and **Apply** is disabled.
- **Duplicate** copies the ordered members under a new set id and a unique name.
- Deleting a set used by a reference site is blocked with the dependent site list; the Core reference check also
  rejects the dangling site reference.

Read-only sessions disable Add, Remove, Duplicate, Apply, bulk charge, and the set membership controls, and show
the shell's read-only banner.

## Queries and editing

The immutable `Project` snapshot implements `IParticleQuery`: `Particles`, `VoidParticle`,
`Find(ParticleId)`, and `Find(ParticleSetId)`. `ProjectEditor` provides `AddParticle`, `UpdateParticle`,
`RemoveParticle`, `AddParticleSet`, `UpdateParticleSet`, and `RemoveParticleSet`. The default editor uses Core
validation; pass `ProjectValidationService.Validate` to include catalog-dependent and cross-domain checks.
See the [architecture guide](../development/architecture.md) for the composition boundary.
