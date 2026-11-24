# Tutorials

Tutorials are added with the features they exercise. Each tutorial is executable against the checked-in
samples and is validated in CI.

## Planned tutorials

| Tutorial | Exercises | Available after |
| --- | --- | --- |
| Create a blank project | Project lifecycle and validation | Step 01 |
| [Author the ceria model in the GUI](ceria-model.md) | Particles, sets, space group, cell, sites, P1 preview, dummies, summary | Step 24A |
| Build and validate the ceria model | Particles, structure, transitions, energy, simulations | Steps 03–07 |
| Parameterize and build a KMC package | Templates, supercells, `.msl` writer | Steps 09–11 |
| Run and collect a KMC job | Solver, job folders, `.msr` collection | Steps 13–20 |
| Analyse transport from Python | `.mcs`/`.msr` reader and NumPy evaluations | Step 22 |
| Run a parameter sweep on SLURM | HPC helpers | Step 19 |

The canonical sample directories are `samples/ceria/`, `samples/blank/`, and the additional models added by
later steps.
