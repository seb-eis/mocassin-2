# .NET solution

The .NET 11 solution is rooted at [`Mocassin.slnx`](../../Mocassin.slnx). Projects live directly below this
directory:

| Project | Responsibility |
| --- | --- |
| `Mocassin.Core` | IDs, units, primitive geometry, model objects, diagnostics, validation contracts |
| `Mocassin.Symmetry` | Space-group catalog, exact operations, cell geometry, orbit/permutation reduction |
| `Mocassin.Compilation` | P1 extension, environments, interactions, transition rules, compiled model |
| `Mocassin.Contracts` | Versioned package/results/state DTOs and schema constants |
| `Mocassin.Packages` | Project persistence, templates, supercells, package encoding, `.msl` writing |
| `Mocassin.Results` | `.msl`/`.msr`/`.mcs` reading, collection, evaluation, exporters |
| `Mocassin.Cli` | `mocassin` headless CLI |
| `Mocassin.App` | Avalonia composition root |
| `Mocassin.App.ViewModels` | View models and commands |
| `Mocassin.App.Views` | Avalonia views and controls |
| `Mocassin.Rendering` | 3D abstraction and Avalonia host |

Test projects use the `Mocassin.*.Tests` naming pattern. The allowed dependency direction is defined in
[`docs/planning/01-target-architecture.md`](../../docs/planning/01-target-architecture.md) §3.
