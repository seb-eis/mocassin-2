# Energy Model

The energy model describes which interactions are sampled, which clusters exist, and which site energies
apply. It is geometric and symbolic: parameter values are supplied later by parameterization templates.

## Stable environment

The stable environment defines one cutoff and optional filters for every stable site.

- The default cutoff is `1.0 Å` with no filters when a project has no `stableEnvironment` section.
- The cutoff must be positive and finite. A cutoff above half the smallest cell dimension is a periodic-
  ambiguity warning (`ENM002`), not an error.
- The environment defines the maximum distance for stable–stable pair sampling.

## Unstable environments

Each unstable site may define its own environment with its own cutoff and filters. If an unstable site has no
environment, pair sampling inherits the stable cutoff but samples unstable–stable pairs only. Clusters centred
on an unstable site still require an explicit unstable environment (`ENM007`).

## Filters

A filter removes pair interactions whose centre-to-partner distance lies in the inclusive range
`[start, end]`:

- stable filters are symmetric: `(A, B)` removes both `A → B` and `B → A`;
- unstable filters are one-directional: `siteAId` must equal the environment site, and `siteBId` is the
  partner;
- filter boundaries use the `1e-5 Å` distance-equality tolerance;
- `start` must not exceed `end`, and `end` must stay inside the environment cutoff (`ENM003`);
- a duplicate or mirrored stable filter is a redundant-filter warning (`ENM005`).

## Clusters

A cluster is a multi-body interaction centred on one reference site.

- `offsetVectors` contains only the surrounding positions, in definition order. The centre is implicit.
- A cluster must have 2–8 surrounding offsets (3–9 positions including the centre); one or more than eight is
  `ENM008`.
- Offsets may lie outside `[0, 1)`. They are resolved with the same nearest-image rule as transition paths.
- Every surrounding position must resolve to a distinct stable site, must not be removed by a filter of the
  centre environment, and must stay inside the centre environment cutoff (`ENM009`–`ENM011`).
- A cluster contributes to the energy of its centre only. To model the same geometry from another site,
  create another cluster with that site as centre.
- Raw permutations are `centreParticleCount × Π(surroundingParticleSetSize)`. More than 500 raw permutations
  warns (`ENM012`); more than 5000 is an error (`ENM013`).
- More than four clusters on one centre warns (`ENM020`); more than ten is an error (`ENM021`).

To fix an oversized cluster, reduce the number or size of the surrounding occupation sets, split the cluster,
or use a shorter-range environment. The cost preview reports raw and symmetry-reduced permutations before
compilation.

## Site energies

Site energies are sparse per site and particle:

- missing entries mean `0 eV`;
- a `(siteId, particleId)` pair may appear once (`ENM014`);
- values must be finite and within `-100..100 eV` (`ENM015`);
- an entry for a particle the site cannot occupy is a warning (`ENM016`).

Site energies act as local offsets and can represent fixed migration barriers on unstable sites. A
parameterization template may override the model value; the authored value remains the default.

## Costs and limits

| Quantity | Warning | Hard error |
| --- | ---: | ---: |
| Stable-environment pair positions | 500 | 5000 |
| Unstable-environment pair positions | 100 | 1000 |
| Cluster raw permutations | 500 | 5000 |
| Clusters per centre site | 4 | 10 |

The preview also reports the stable P1 position count, pair-image count, unique directed pair geometries,
per-cluster permutation counts, and estimated energy-table memory.

See the [ceria sample](https://github.com/seb-eis/mocassin-2/blob/main/samples/ceria/README.md) for a stable
environment plus an oxygen migration-edge cluster. The complete diagnostic catalog and consolidated
troubleshooting guidance are added with Step 07.
