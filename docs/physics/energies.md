# Energy Contributions

The energy model separates three contributions:

- **pair interactions** sampled from stable–stable and unstable–stable environments;
- **cluster interactions** defined by a centre site and 2–8 surrounding positions;
- **site energies** attached to a reference site and particle.

## Environments and cutoffs

A stable environment defines the sampling radius for all stable sites. Each unstable site may define its own
radius and filters. A pair is included when its minimum-image distance is inside the environment cutoff and
outside every matching filter.

For a pair image at distance \(d\), a filter with range \([s, e]\) removes the interaction when

$$
s - \tau \le d \le e + \tau,
$$

where \(\tau = 10^{-5}\,\text{Å}\). Stable filters apply to both directions; unstable filters apply only from
the environment site to its partner.

## Pair contributions

Pair sampling applies the centre stabilizer \(G_0\) to group symmetry-equivalent images. Each directed
geometry has a stable `GeometryId`; the shared energy-table identity is the `EnergyId`.

The pair contribution for a sampled pair is the table value for its particle pair. The 0.5 factor and the
total-lattice-energy convention belong to the solver energy engine in Step 15; the project model defines the
interaction geometry, not the summation convention.

## Cluster contributions

A cluster is an ordered surrounding geometry around one centre. Its raw state count is

$$
N_\text{raw} = N_\text{centre} \prod_{i=1}^{n} N_i,
$$

where \(N_\text{centre}\) is the centre occupation-set size and \(N_i\) are the surrounding set sizes. The
induced permutation group \(P\) reduces equivalent occupation tuples to one energy per orbit. The unique
count is computed with Burnside's lemma rather than by enumerating the full product.

A cluster contributes only at its centre. The same physical geometry viewed from another position is a
different cluster definition.

## Site energies

Site energies are local offsets. A missing entry is zero. They are also the mechanism for fixed migration
barriers on unstable sites. The model value is the default; a parameterization template may override it.

## Chirality and shared energy tables

Two directed pair geometries can be chiral partners when the inverse direction is a distinct orbit of the
centre stabilizer. The compiled model keeps both `GeometryId` values for display and migration, but both
directions share one `EnergyId` and one energy table. Independent `A → B` and `B → A` parameter values are
therefore not representable. This deliberately makes the orbit relationship explicit instead of silently
forcing symmetry.

In a P1 model every directed pair is its own orbit; the shared identity still applies where an inverse
geometry exists. Unstable–stable pairs are asymmetric by nature and do not have a chiral partner.

## Units

Model and parameter tables use electronvolts (eV). The solver converts to thermal units once per job.
Transport output uses SI units.
