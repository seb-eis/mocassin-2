# Step 13 populated package sample

This sample builds the Step 13 populated `.msl` fixture in
[`tests/contracts/packages/step-13-populated/`](../../tests/contracts/packages/step-13-populated/). It is a small
P1 ceria-derived mechanism with:

- a stable and an unstable oxygen environment, so the compiled model has non-empty pair tables;
- a three-site migration-edge cluster, so the compiled model has a non-empty cluster table;
- an extended `{O, Vc, H}` particle set on the O2 site, so one direction in each collection admits `H` at a
  node where the other direction does not (the non-trivial per-node mask difference);
- one KMC simulation (the three-site oxygen migration) and one MMC simulation (the two-site O/V exchange);
- two jobs (one KMC, one MMC) at 2×2×2 supercells, built with the real `.NET` writer and the default package
  options.

The project is derived from `tests/contracts/project-energy-populated.mocprj`; it removes that fixture's second
metropolis transition, whose unstable `U1` site is rejected by the model rules (`TRN016`). It also adds explicit
simulations and a job template so the package contains jobs.

## Fixture properties

The extended O2 set provides the **non-trivial per-node mask difference** the design asks for. The
**translation-equivalent start mapping** is carried by the separate real-writer
[`step-13-centered`](../step-13-centered/README.md) fixture, derived from the Step 08 C-centered sample after
the compiler fix `1f5b473`. That package has cross-bucket `jump_mapping` entries and also carries a per-node
mask difference, so Step 13 has a real package for both properties without weakening this sample's KMC/MMC and
populated pair/cluster coverage.

The reader acceptance for a cross-bucket direction is also covered by the synthetic mapping tests in
`src/rust/crates/mocassin-msl/src/map/tests`; Step 16 consumes the centered fixture for its exact
per-direction check.
