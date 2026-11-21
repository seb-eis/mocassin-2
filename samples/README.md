# Samples

Canonical example projects used by tutorials, CLI checks, and parity tests.

| Directory | Purpose |
| --- | --- |
| [`blank/`](blank/README.md) | Minimal project skeleton for new users and tests |
| [`ceria/`](ceria/README.md) | Reduced-ceria KMC/MMC reference project |
| [`barium-zirconate/`](barium-zirconate/README.md) | Partially hydrated Y-doped BaZrO₃ reference structure with proton interstitial sites |
| [`transitions/`](transitions/README.md) | Ceria KMC/MMC, polaron hop, 5-site vehicle, and 5-site interstitialcy transition samples |
| [`lattice/`](lattice/README.md) | Ceria reduction/hydration and barium-zirconate doping samples |
| [`jobs/`](jobs/README.md) | Ceria temperature/doping sweep with a Step 10 job template |
| [`step-11-cluster-expansion/`](step-11-cluster-expansion/README.md) | P-1 origin-shifted ceria two-member cluster for the amendment-2 expansion-map fixture |
| [`simulations/`](simulations/README.md) | KMC/MMC samples with documented defaults and non-default configurations |

Transition samples are added with Step 04. Step 05 adds energy environments, clusters, and site energies to
the ceria and barium-zirconate samples. Step 06 adds lattice/doping and simulation samples. Sample projects
must use the current `.mocprj` schema; legacy formats are not supported.
