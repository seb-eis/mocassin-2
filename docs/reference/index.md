# Reference

Reference material for the CLI, file formats, diagnostics, and repository tooling.

## Sections

- [Repository and file layout](file-layout.md) — source trees, generated artifacts, package immutability, and the planning exclusion.
- [Simulation package format](msl-format.md) — `.msl` v1 schema, content addressing, payloads, `package-summary-v1`, versioning, verification, and fixtures.
- [Project format](project-format.md) — `.mocprj` envelope, IDs, units, canonicalization, migrations, atomic save,
  including the Step 06 lattice/doping and simulation sections.
- [Transitions](transitions.md) — transition fields, semantic ordering, path matching, preview, and the `TRN` diagnostics.
- [Energy](energy.md) — environments, filters, clusters, site energies, ordering, limits, `GeometryId`/`EnergyId`.
- [CLI](cli.md) — the full `mocassin` command surface (`new`, `validate`, `compile`, `build`, `inspect`,
  `verify`, `doctor`, `clean --dry-run`), options, exit codes, JSON reports, and JSON-Lines events.
- **Formats** — the [immutable `.msl` package](msl-format.md) is defined here; the `.msr` results store, `.mcs`
  state, job folders, and `solver.json` are added with Steps 18 and 20.
- [Diagnostics](diagnostics.md) — every stable diagnostic code with its severity, owner, and help.
- [Troubleshooting validation errors](../troubleshooting/errors.md) — triage and common fixes.
- **Extension ABI** — the opt-in extension boundary and versioned host table (added with Step 19).

The machine-readable schema source is `contracts/manifest.json` in the repository. Generated API reference
pages are under [API](../api/index.md).
