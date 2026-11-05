# Physics

MOCASSIN simulates defect thermodynamics and transport on a fixed lattice using two Monte Carlo methods with
a shared model:

- **Metropolis Monte Carlo (MMC)** samples equilibrium configurations. It accepts moves with the Metropolis
  criterion and does not advance physical time.
- **Kinetic Monte Carlo (KMC)** selects and accepts transitions according to their rates. It advances
  simulated time and supports pre-runs, normalization, and field-driven transport.

## Energy model

The model separates:

- pair interactions between stable or unstable positions;
- user-defined multi-body clusters (up to nine positions);
- site energies;
- an optional external electric field.

Model and parameter tables use electronvolts (eV). The solver converts energies to thermal units once per job.
Transport output uses SI units.

## Symmetry

The user defines the model in a space-group setting. Compilation expands it to a deterministic P1 position
order, generates symmetry-reduced interaction and rule sets, and assigns dense indices for the solver. The
discrete symmetry path uses exact integer operations on a `24 × 10^12` fractional grid; tolerance-based
deduplication is never used to order positions.

## Tracking and transport

KMC tracking records global per-transition/particle displacements, mobile per-particle displacements, and
static per-cell/position/particle quantities. Fractional displacements are converted to Cartesian output only
at the reporting boundary. Transport definitions use tracked-ensemble counts and per-species contributions.

Detailed formulas and conventions are recorded in the internal plan and encoded in the test suite. The public
physics pages are completed as the corresponding steps land.
