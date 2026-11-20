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

## Fixture properties and limits

The extended O2 set provides the **non-trivial per-node mask difference** the design asks for. The sample
cannot provide an **equivalent-start mapping**: `TransitionCompilationStage.DirectionKey` includes the start
`P`, so `EquivalentStartPositions` can only ever repeat the representative start and `BuildMappings` never
places a direction in a different bucket. The checked-in Fm-3m `msl-v1` package and a plain P-1 variant of
this sample both show no cross-bucket mapping; the P-1 variant builds and verifies, so the earlier P-1
`PKG010` note is not reproducible. The Step 13 amendment
[`step-13-amend-1`](../../docs/planning/amendments/step-13-amend-1-fixture-properties.md) records the
requested acceptance of the synthetic coverage and the Step 08 compiler defect owner. The reader acceptance
is covered by the synthetic mapping tests in `src/rust/crates/mocassin-msl/src/map/tests`; Step 16 re-plans
its exact per-direction fixture against the compiler fix.

The fixture still covers the populated pair/cluster tables, both simulation kinds, the real-writer payload and
signature paths, and the MMC validation rules.
