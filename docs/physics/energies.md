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
total-lattice-energy convention are defined in [Total lattice energy](#total-lattice-energy-solver-runtime)
and implemented by the context initialization (Step 14) and the energy engine (Step 15); the project model
defines the interaction geometry, not the summation convention.

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

Two directed pair geometries can be chiral partners when the inverse direction remains a distinct orbit from
both directed centres. If either centre's stabilizer maps the inverse onto the forward direction, the pair is
symmetric and has no chiral partner. A chiral link is reciprocal: if two forward directions share one inverse
orbit, the relation is not one-to-one and those directions stay achiral. The compiled model keeps both
`GeometryId` values for display and migration, but both directions share one `EnergyId` and one energy table.
Independent `A → B` and `B → A` parameter values are therefore not representable. This deliberately makes the
orbit relationship explicit instead of silently forcing symmetry.

In a P1 model every directed pair is its own orbit; the shared identity still applies where an inverse
geometry exists. Unstable–stable pairs are asymmetric by nature and do not have a chiral partner.

## Total lattice energy (solver runtime)

The solver counts each contribution exactly once:

$$
U = \sum_{\text{stable } e} E_\text{site}(P(e), p(e))
  + \tfrac{1}{2} \sum_{\text{stable } e} E_\text{pair}(e, p(e))
  + \sum_{\text{instances } i} \sum_{\text{cells } c}
      E_\text{cluster}\bigl(i, c, p(c, P(\text{centre}_i))\bigr).
$$

The site background is counted once per environment; a pair is counted from both ends, hence the \(1/2\); a
cluster instance is counted once per supercell cell, at its centre, because the cluster energy is a function of
the full occupation tuple whose slot 0 is the centre particle and the same instance geometry exists in every
cell. The environment energy vector a transition path sums over therefore holds
the site and pair parts only; the cluster part is a separate per-centre term and must be added for every centre
whose code changes.

The pair vector is **owner-major**: a payload pair link is stored on the interaction's centre, the table row is
the centre's particle and the column its partner's particle, so the centre's vector is the sum over the centre's
own links. When a partner changes, the centre's vector is updated with the centre's table and row; a solver must
not apply the changing environment's table to the other side, and must not assume that a reverse link exists.
Step 14 supplies the inverse index that finds the affected centres; Step 15 applies the deltas.

**Cluster images and canonical codes.** The v1 cluster link does not persist a member's cell offset. A
multi-centre cluster table may centre on several P1 positions of its site; Step 14 resolves each instance's member
image from the centre's own pair links (the rotated multi-centre image). The soundness argument is the full space
group plus the complete tuple product, not the centre-site stabilizer alone: the cluster reduction enumerates
every allowed occupation tuple and canonicalises it, the applied energy is invariant under the full space group,
so a transported centre's tuple belongs to the same energy orbit; the approved `code_expansions` map resolves the
raw tuple to that orbit's canonical code. The solver never re-derives symmetry. A cross-image member or an
ambiguous image remains a v1 limitation (Step 14 OD-1).

The original solver used `0.5 * Σ_stable E[environment]`, where the environment energy already contained the
site background and a cluster term replicated at every member. That halved the site and cluster contributions.
The new solver keeps the documented per-centre convention instead; the deviation is recorded in the Step 14
design (D-3) and covered by a hand-computed test and the Step 29 parity work. Energies are in kT inside the
solver and converted to eV only at output boundaries; the initial total is computed once at context
initialization (Step 14) and maintained incrementally by the energy engine (Step 15).

A cluster cell whose centre or member slot is **void** has no canonical occupation row: the `.msl` table
enumerates only the site's allowed non-void particles. Its cluster contribution is zero (the empty cluster), not a
missing-code error. The energy engine must not call the cluster lookup for such a tuple; the context exposes an
absent `cluster_code_id` and a zero energy row for that cell. The multi-centre instance fixture and the
resumed-lattice test cover the rule.

## Transition energies (Step 15 feature 1)

For a resolved path \(p_0,\ldots,p_{L-1}\) and rule states \(S0,S1,S2\), the solver evaluates

$$
\begin{aligned}
s_0 &= \sum_i E[p_i][S0_i] + \sum_c C[c][S0_c], \\
\mathrm{raw}\,s_1 &= \sum_{i=1}^{L-1} E[p_i][S1_i], \\
\mathrm{conf} &= s_2 - s_0, \\
s_1 &= s_0 + \mathrm{raw}\,s_1 + \tfrac12\mathrm{conf}, \\
\mathrm{field} &= -\tfrac12\,\texttt{field\_scale}\,
  (\texttt{charge\_transport\_vector}\cdot\hat e), \\
b_{01} &= (s_1-s_0)+\mathrm{field}, \qquad
b_{10} &= (s_1-s_2)-\mathrm{field}.
\end{aligned}
$$

\(E\) is the site+pair owner vector and \(C\) is the separate cluster term at each path centre. Raw S1 reads
only \(E\): node 0 contributes zero, and cluster terms enter S1 through \(s_0\) and the midpoint correction.
All internal values are in \(kT\); a direct model-table read is multiplied by `ev_to_kt` exactly once.

S2 is a read-only direct sum, not a backup/apply/restore. For **every** path node, including an unchanged
spectator receiver, the evaluator walks that node's own forward links. A link whose partner is another path
node contributes the owner's table and owner's row at the final receiver particle. The partner-change test is
only a zero-delta skip; there is no receiver-change filter and no structural-stability filter. Duplicate
periodic images with distinct pair tables all contribute. The jump-status table is not the source for this
sum: it keeps one sorted receiver link per ordered path pair and filters receivers by structural stability.
The original additionally excluded initially-void receivers; Step 14 corrected that filter.

Each affected cluster code is built once from the final occupation tuple, with path-node S2 values as slot
overrides, and resolved through the package's `code_expansions` map. A void centre or member contributes the
zero row without a lookup. A non-void tuple absent from a present map, or a non-canonical tuple in a pre-map
package, fails closed. A present v1 `static_correction` also fails closed because its non-null semantics are
undefined; every v1 rule uses the dynamic path.

The acceptance kernel returns \(\exp(-b_{01})\). Libm is the default; the job's `useFastExp` flag opts into
the bounded binary-exponent approximation.

## State application and the incremental lattice energy (Step 15 feature 2)

When a caller accepts a transition, `apply_state_change` commits the rule's final (\(S2\)) occupation at the
resolved path. The write order is fixed: affected cluster tuples are resolved before the first mutation;
particles and derived flags follow; the mobile tracker ids are permuted so each identity follows the moved ion
(an unchanged node keeps its identity, and a void destination has none); each changed environment leaves its old
selection pool and joins its new one; every owner in the inverse link index receives the owner-major pair delta
for all particles; and each affected cluster is rebuilt once from the final state. The affected cluster set is
the union of the member-side `ClusterAssoc` route and the centre-side
`centres(position(V)) × cell(V)` route, so a centre-only change cannot leave a stale row. Counters, tracker
values, and time are not touched.

The lattice energy is updated on the fly from the exact decomposition:

$$
\Delta U = \sum_V (\sigma'_V s'_V - \sigma_V s_V)
 + \tfrac12\Bigl[\sum_V (\sigma'_V p'_V - \sigma_V p_V)
 + \sum_{V}\ \sum_{\substack{O \in \mathrm{incoming}(V)\\ O\ \text{unchanged}}}
   \sigma_O\,\delta_{(O,V)}\Bigr]
 + \sum_{(i,c)} (C'_{i,c} - C_{i,c}),
$$

where \(s\) is the site background, \(p\) the pair part of the environment vector, \(C\) the per-centre
cluster row, and \(\sigma\) the `STABLE` flag. The 0.5 pair factor appears exactly once; an unchanged unstable
owner contributes no pair row even though its vector entry is updated. A changed environment's after-value is
read after the owner pass, so the intra-path deltas are already included. The cluster rows \(C\) are read
immediately before they are overwritten. In debug builds the stored total is checked against the independent
full recomputation after every accepted step.

## Units

Model and parameter tables use electronvolts (eV). The solver computes \(kT_eV = k_B T\) once per job and
stores vectors, cluster rows, field scale, and lattice totals in \(kT\). Direct pair/cluster table reads are
scaled by `ev_to_kt` at the read; eV appears only at output boundaries. Transport output uses SI units.
