# Reference and parity assets

This directory holds reference data for statistical parity with the original solver:

- the original commit, compiler, flags, package pins, seeds, and hashes used to produce reference data;
- reference observables for ceria, barium zirconate, and a vehicle/interstitialcy model;
- reduced nightly datasets and checksums;
- benchmark baselines keyed by OS/CPU class/toolchain.

Follow [`docs/planning/07-reference-build.md`](../../docs/planning/07-reference-build.md) and keep
`/workspace/mocassin-original` read-only. The original is an oracle, not source to copy.
