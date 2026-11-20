# Step 13 populated package sample

This sample builds the Step 13 populated `.msl` fixture in
[`tests/contracts/packages/step-13-populated/`](../../tests/contracts/packages/step-13-populated/). It is a small
P1 ceria-derived mechanism with:

- a stable and an unstable oxygen environment, so the compiled model has non-empty pair tables;
- a three-site migration-edge cluster, so the compiled model has a non-empty cluster table;
- one KMC simulation (the three-site oxygen migration) and one MMC simulation (the two-site O/V exchange);
- two jobs (one KMC, one MMC) at 2×2×2 supercells, built with the real `.NET` writer and the default package
  options.

The project is derived from `tests/contracts/project-energy-populated.mocprj`; it removes that fixture's second
metropolis transition, whose unstable `U1` site is rejected by the model rules (`TRN016`). It also adds explicit
simulations and a job template so the package contains jobs.

## Intentional limits

The model builder rejects the two extra properties the Step 13 design originally asked this fixture to carry:

- A **non-trivial per-node mask difference** needs a particle set on a transition node that differs from the
  other direction's node. The compiler rejects a mixed set on a migration endpoint with `CMP004` (the same
  start state then has two outcomes), and a mixed MMC exchange fails the same rule. The union check's positive
  and negative cases are covered by the synthetic payload mutations in
  `src/rust/crates/mocassin-msl/src/map/tests` instead.
- An **equivalent-start mapping** needs a symmetry operation that maps a path to itself with the same relative
  encoding. The checked-in P1 and P-1 variants do not produce one (the P-1 build also fails the package
  verifier's cluster-link check, `PKG010`). The Rust reader accepts a direction mapped into another bucket
  because it never requires `direction.position_id == bucket position`; that acceptance is covered by the
  same synthetic mapping tests and remains a Step 16 interface check.

The fixture still covers the populated pair/cluster tables, both simulation kinds, the real-writer payload and
signature paths, and the MMC validation rules.
