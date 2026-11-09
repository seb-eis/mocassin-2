# Monte Carlo Methods

MOCASSIN uses two Monte Carlo methods on the same fixed lattice and transition model:

- **Metropolis Monte Carlo (MMC)** samples equilibrium configurations without advancing physical time.
- **Kinetic Monte Carlo (KMC)** selects transitions by their rates and advances simulated time.

Both methods consume the same authored transition definitions. The difference is which transition kind is
active and how acceptance or selection is performed.

## State triplets

Each compiled transition records the occupations before and after the move. For a position with a state-change
group, a rule contains:

- **S0** — the donor occupation before the move;
- **S1** — the intermediate or unstable occupation;
- **S2** — the acceptor occupation after the move.

The Step 04 `StateChangeMatrices` expose the authored donor/acceptor sets per position. Step 08 turns them into
compiled rules and checks mass and charge conservation.

## Shared transition model

The authored model separates:

- **state changes** — one donor/acceptor pair;
- **state-change groups** — the allowed pairs at one position;
- **chains** — ordered groups and dynamic/static linkers;
- **concrete transitions** — a kinetic reference path or a Metropolis site pair.

This separation lets one mechanism be reused by multiple simulations and lets compilation expand one reference
geometry into all symmetry-equivalent paths.

## Metropolis Monte Carlo

MMC accepts a proposed exchange with probability `min(1, exp(-ΔE / k_B T))`. It does not advance physical time
and does not use attempt frequencies. A two-site transition may select the same reference site twice; that is
an intra-sublattice exchange, not a no-op.

See [MMC](mmc.md) for the two-site exchange semantics.

## Kinetic Monte Carlo

KMC assigns each compiled rule a rate, selects a rule in proportion to its rate, and advances time by
`Δt = -ln(u) / R` for a total rate `R`. It uses the kinetic transition's reference path and may include
pre-runs, normalization, and field-driven transport.

See [KMC](kmc.md) for paths, stability, and reverse links.

## Step ownership

Step 04 defines and validates the authored transition model and its preview. Step 05 adds energies, Step 06
adds simulations and attempt frequencies, and Step 08 compiles rules, generates automatic reverse rules for
two- and three-site mechanisms, and performs conservation filtering.
