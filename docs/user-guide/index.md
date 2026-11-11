# User Guide

The user guide covers the complete workflow. Pages are completed as the corresponding implementation steps
land.

## Workflow

1. **Create or open a project.** A project is versioned UTF-8 JSON (`.mocprj`) containing the model,
   parameterizations, job templates, and build templates.
2. **Define the model.** Particles, crystal structure, transitions, energy environments, lattice/doping
   definitions, and simulation definitions.
3. **Validate.** Fix errors and review warnings before compiling.
4. **Parameterize.** Enter energies and attempt frequencies against a stable model signature.
5. **Build.** Compile the model, expand jobs, generate supercells, and write an immutable `.msl` package.
6. **Run.** Execute `mocassin-sim` jobs locally or on an HPC scheduler.
7. **Collect.** Read job folders and write a separate `.msr` results store.
8. **Analyse.** Evaluate particle statistics, transport, tracking, and energy data in the GUI, CLI, or Python.

## Planned sections

- [Particles and particle sets](particles.md)
- [Crystal structure](structure.md)
- [Transitions](transitions.md)
- [Energy model](energy.md)
- [Lattice and doping](lattice.md)
- [Simulations](simulations.md)
- Project lifecycle and model editors
- Validation and diagnostics
- Parameterization templates
- Job templates and supercells
- Building and running simulations
- Results, plots, and reports
- 3D visualization and MMCFE

Until those pages are written, see the [tutorials](../tutorials/index.md) and [reference](../reference/index.md)
sections.
