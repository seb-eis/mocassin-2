# Simulation samples

These projects exercise the Step 06 simulation model with the current `.mocprj` schema.

| File | Configuration |
| --- | --- |
| `ceria-kmc.mocprj` | KMC at 773 K, 200 target MCSP, 250 pre-run MCSP, a fixed RNG seed, and `noJumpLogging` |
| `ceria-mmc.mocprj` | MMC at 1200 K with a non-default `0.05` break tolerance |

The KMC sample demonstrates a non-default configuration and an explicit seed; the MMC sample keeps the
documented defaults except for temperature and break tolerance. Both use the ceria transition set from
`samples/transitions/`. Job expansion, effective job counts, and package writing remain Steps 10 and 11 work.

The populated sample model is also mirrored as `tests/contracts/project-simulations-populated.mocprj`.
