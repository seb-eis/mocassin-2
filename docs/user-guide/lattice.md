# Lattice and Doping

The lattice model describes how a supercell is doped. It is symbolic: fractions, supercell sizes, integer
truncation, and the applied-doping report belong to the Step 10 job builder, not to the project model.

## Occupation exchanges

An occupation exchange is a reusable rule that replaces one particle with another at a stable reference site:

```json
{
  "id": "…",
  "name": "Ce4+ -> Ce3+",
  "siteId": "…",
  "sourceParticleId": "…",
  "targetParticleId": "…"
}
```

- `siteId` must reference an existing stable site.
- Both particles must belong to the site's ordered particle set.
- Source and target must differ.
- The exchange has no effect until a doping references it.
- Its charge delta is derived as `target.charge - source.charge`; it is never persisted.
- Occupation-exchange names are unique case-insensitively within the collection.

The default building block is derived, not authored: a stable site uses the first particle of its particle set,
and an unstable site uses the void particle. Invalid or missing particle sets are reported by the particle and
structure validators; the lattice query does not throw.

## Dopings

A doping references one primary exchange and, optionally, a charge-compensating dependent exchange:

```json
{
  "id": "…",
  "name": "Reduce ceria",
  "priority": 0,
  "primaryExchangeId": "…",
  "dependentExchangeId": "…",
  "chargeCompensation": true
}
```

- `priority` is unique and in `0..9999`. Dopings are applied in ascending priority order, and this order is the
  deterministic application order.
- If `chargeCompensation` is `false`, `dependentExchangeId` must be absent. The primary exchange may have any
  charge delta, including zero for an isovalent substitution.
- If `chargeCompensation` is `true`, the dependent exchange is required, both charge deltas must be non-zero,
  and they must have opposite signs. The integer ratio and any downward truncation are Step 10 checks because
  they depend on the supercell and the requested fraction.
- Doping names are unique case-insensitively within the collection.
- The primary exchange defines the requested fraction; the dependent exchange is applied automatically to
  maintain charge neutrality.

Priority order is user-visible and is never inferred from dictionary order. To change the order, renumber every
priority deterministically; the editor's reorder operation assigns `0..n-1` in the requested order.

## Validation boundaries

Step 06 owns `LAT001`–`LAT011`: references, site stability, particle-set membership, source/target identity,
priority range and uniqueness, compensation shape and charge signs, and names.

Fractions, unknown doping references in a job template, same-site application order, requested-versus-applied
counts, truncation, charge-compensation ratios, supercell limits, and the lattice preview belong to Step 10
(`JOB004`–`JOB015`). The project stores no derived building block, applied counts, or preview data.

## Editing operations

`ProjectEditor` supports adding, updating, and removing occupation exchanges and dopings, deterministic priority
reordering, and dependent reporting when an exchange is still referenced. Removing an exchange with
`removeDependents: true` also removes the dopings that reference it.

## Samples

See the [lattice samples](https://github.com/seb-eis/mocassin-2/tree/main/samples/lattice) for ceria reduction,
ceria hydration-style dopings, and yttrium-doped barium zirconate. The diagnostic catalog is consolidated by
Step 07.
