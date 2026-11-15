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

## Supercell generation

A job configuration requests a supercell of \(A \times B \times C\) unit cells and a list of doping fractions.
The build generates the lattice from the compiled model; the project never stores an applied lattice.

The base lattice replicates the unit cell in row-major order:

```text
index = ((a * B + b) * C + c) * position_count + P
```

with \(a\) outermost and \(P\) in canonical P1 order. A stable site starts with the first particle of its
authored particle set; an unstable site starts with the implicit void particle. The same layout is used by the
preview, the `.msl` lattice payload, and the solver reader.

Each doping is then applied to the **current** lattice in ascending priority:

- `requested = floor(fraction × available)`, where `available` is the number of source particles on the
  exchange site after all earlier applications. A non-finite fraction or one outside `0..1` is `JOB005`.
- A positive fraction with no source particle applies zero and reports `JOB010`.
- Whenever the effective fraction differs from the requested fraction, the build reports `JOB011`. This is the
  truncation result: the applied count is rounded down, never up.
- If charge compensation is enabled, the primary and dependent applications form an integer pair with opposite
  charge deltas. The search starts at the requested primary count and reduces it until the dependent count is an
  integer within \(10^{-6}\) and fits the available dependent population. If no pair is realizable, the lattice
  is not built and `JOB012` is an error.
- The dependent exchange is always applied after the primary, and its source positions are recomputed on the
  current lattice. Same-site dopings therefore behave as ordinary sequential dopings: a later `A → C` applies to
  the particles that an earlier `A → B` left behind.
- Positions are sampled uniformly without replacement from the candidate positions, sorted by ascending linear
  index, using the job's deterministic lattice stream. The same job coordinates and seed always produce the same
  lattice.

Requested, available, applied primary, applied dependent, and effective counts are reported for every doping.
The preview exposes the same report as the build.

## Charge neutrality

- A compensated pair is accepted only when both applications are charge-balanced within
  \(10^{-6} \times \max(1, n)\) elementary charges.
- An applied **uncompensated** doping with a non-zero charge delta is allowed. If the final lattice is not neutral,
  the build reports `JOB016` as a warning and marks the lattice non-neutral. This includes a mixed configuration
  with one compensated and one intentionally uncompensated charged doping.
- A non-neutral **base** lattice is a model property, not a doping failure. It is reported through the total
  charge before and after doping and through the neutrality flag, but it does not by itself produce `JOB012` or
  `JOB016`.
- `JOB012` is reserved for a compensated pair that cannot be realized or is not balanced. It is never used for a
  pre-existing base charge.

## Validation boundaries

Step 06 owns `LAT001`–`LAT011`: references, site stability, particle-set membership, source/target identity,
priority range and uniqueness, compensation shape and charge signs, and names.

Fractions, unknown doping references in a job template, same-site application order, requested-versus-applied
counts, truncation, charge-compensation ratios, supercell/job limits, extensions, and the lattice preview are
Step 10 job-builder checks (`JOB001`–`JOB020`). The project stores no derived building block, applied counts, or
preview data.

## Previewing a lattice

The same generator backs the build and the preview, so a preview cannot disagree with the lattice a build
produces for the same request and seed. Preview a template without writing a package with:

```text
mocassin build <project.mocprj> --preview [--template <jobTemplateId>] [--json]
```

The preview echoes the requested cell size and doping list, reports requested versus applied primary counts,
dependent counts, effective fractions, total charge before and after, neutrality, and the per-site remaining
particle populations. It returns no partial preview: an unknown doping, a non-finite or out-of-range fraction,
an invalid supercell, or an impossible compensation returns `null` with diagnostics. A job-count preview reports
the default, effective, and whole-template job totals without generating a lattice.

The preview cache is bounded to eight entries and 256 MiB and keyed by the project model hash, the lattice
request hash, the canonical doping list, and the catalog/symmetry provenance, so an edit cannot reuse stale
data. The cache stores the report and populations, not the generated particle bytes. See the
[CLI reference](../reference/cli.md) and the [job-template guide](job-templates.md).

## Editing operations

`ProjectEditor` supports adding, updating, and removing occupation exchanges and dopings, deterministic priority
reordering, and dependent reporting when an exchange is still referenced. Removing an exchange with
`removeDependents: true` also removes the dopings that reference it.

## Samples

See the [lattice samples](https://github.com/seb-eis/mocassin-2/tree/main/samples/lattice) for ceria reduction,
ceria hydration-style dopings, and yttrium-doped barium zirconate. The diagnostic catalog is consolidated by
Step 07.
