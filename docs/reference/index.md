# Reference

Reference material for the CLI, file formats, diagnostics, and repository tooling.

## Sections

- [Repository and file layout](file-layout.md) — source trees, generated artifacts, and the planning exclusion.
- [Project format](project-format.md) — `.mocprj` envelope, IDs, units, canonicalization, migrations, atomic save.
- **CLI** — `mocassin` and `mocassin-sim` commands, options, exit codes, and JSON events (added with Steps
  12 and 19).
- **Formats** — immutable `.msl` package, `.msr` results store, `.mcs` state, job folders, and `solver.json`
  (added with Steps 11, 18, and 20).
- **Diagnostics** — stable diagnostic codes and help links (added with Step 07).
- **Extension ABI** — the opt-in extension boundary and versioned host table (added with Step 19).

The machine-readable schema source is `contracts/manifest.json` in the repository. Generated API reference
pages are under [API](../api/index.md).
