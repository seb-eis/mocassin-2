# Repository and File Layout

```text
mocassin/
├── docs/                 Published narrative site (docs/planning/ is excluded)
├── src/dotnet/           .NET solution and tests
├── src/rust/             Rust workspace and crates
├── src/python/           `mocassin-analysis` package and tests
├── contracts/            manifest.json + generated contract artifacts
├── samples/              Example projects
├── tests/contracts/      Cross-language format fixtures
├── tests/reference/      Statistical parity and benchmark assets
├── tools/contracts/      Manifest validator/generator
├── tools/docs/           Documentation build and serve scripts
├── tools/docfx/          DocFX configuration
└── tools/hpc/            SLURM/PBS helpers
```

## Generated artifacts

| Artifact | Produced by | Committed? |
| --- | --- | --- |
| `site/` | Zensical and combined docs build | No |
| `artifacts/docfx/` | DocFX | No |
| `target/doc/` | `cargo doc` | No |
| `contracts/generated/` | `tools/contracts/generate.py` | Yes; regenerate, never hand-edit |
| `tests/contracts/api-baseline/net11.0/` | `tools/update-api-baseline.sh` | Yes; refresh after an intentional public API change |
| `tests/contracts/packages/msl-v1/` | The amended `.NET 11` writer (feature-7 regeneration command) | Yes; `.msl` + `package-summary-v1` + extracted payloads |
| `tests/contracts/packages/amend-1-net10/` | Captured with the pre-amendment `net10.0` writer | Yes; compatibility fixture, never regenerated |
| `tests/contracts/packages/amend-1/` | Captured with the pre-amendment `ZstdSharp` codec | Yes; raw payloads and golden frames, bytes may be re-captured with a note |
| `tests/contracts/**/*.mcs`, `*.msl`, `*.msr`, `*.log`, `Job*/` | Real writers | Yes; explicit `.gitignore` exceptions |

`contracts/generated/schema/` holds one schema per format or report. `package-v1.schema.json` is the SQLite row
schema; `package-payloads-v1.schema.json` separately describes the top-level model, job, and lattice payloads and
uses the manifest's payload unknown-key policy.


## Application data layout

The desktop application owns an application-local data root; it is separate from project, package, results, and
job-folder paths.

| Platform | Default root |
| --- | --- |
| Windows | `%LOCALAPPDATA%\Mocassin` |
| Linux | `~/.local/share/Mocassin` |
| macOS | `~/Library/Application Support/Mocassin` |

`--data-dir <path>` overrides the root for one run. `MOCASSIN_DATA_DIR` provides an environment override; the
command-line value wins. The directory contains:

```text
<app-data>/
├── Mocassin.Settings.json  # versioned application settings
├── logs/                   # rolling Serilog files
├── cache/                  # disposable application cache
└── recovery/               # crash/fallback snapshots and manifests
```

A saved project may also have an `<file>.autosave` sidecar next to it. The sidecar is written by autosave and
removed by a successful save, a return to the save point, a clean close, or an explicit discard; it is never
swept by the recovery retention policy. A `recovery/` entry is a `<projectId>.mocprj.autosave` snapshot plus a
`<projectId>.json` manifest (`format: "mocassin.recovery"`, `version: 1`, project id/path, `savedUtc`, source).
Recovery files older than seven days are swept at startup; sidecars are exempt. The accepted limitation is that a
deferred `recovery/` candidate can expire (m-14). When candidates have the same timestamp, the adjacent
`.autosave` sidecar is offered before a `recovery/` manifest, and the ordinal snapshot path breaks any remaining
tie.

The settings document is application-local. It is **not** part of `contracts/manifest.json`, has no
cross-language fixtures, and has no compatibility guarantee with any project/package/results format. Unknown
properties are ignored and dropped on the next write. A missing key takes its documented default.

## `mocassin.settings` v1

```json
{
  "format": "mocassin.settings",
  "schemaVersion": 1,
  "theme": "system",
  "accent": null,
  "density": "comfortable",
  "fontScale": 1.0,
  "highContrast": false,
  "reduceMotion": false,
  "culture": "en",
  "logging": { "level": "information" },
  "ui": { "confirmOnExternalChange": true, "showWelcomeOnStart": true },
  "paths": {
    "lastOpenDirectory": null,
    "lastSaveDirectory": null,
    "lastFolderDirectory": null
  },
  "recentFiles": [],
  "layout": {
    "leftFraction": 0.2167,
    "rightFraction": 0.2667,
    "consoleFraction": 0.225,
    "leftVisible": true,
    "rightVisible": true,
    "consoleVisible": true,
    "activeConsoleChannel": "application"
  },
  "autosave": { "enabled": true, "intervalMinutes": 5 },
  "session": {
    "openProjects": [],
    "activeProject": null,
    "cleanExit": true,
    "savedUtc": null
  }
}
```

| Key | Type | Default | Rule |
| --- | --- | --- | --- |
| `format` | string | — (required) | Must equal `"mocassin.settings"`; any other value makes the document invalid and it is not loaded or overwritten |
| `schemaVersion` | integer | `1` | Current major is `1`; a value above the current major is rejected without overwriting; a lower value is migrated |
| `theme` | string enum | `"system"` | `system` / `light` / `dark` |
| `accent` | string or null | `null` | `blue`, `teal`, `green`, `amber`, `violet`, `rose`, or null for the Fluent default |
| `density` | string enum | `"comfortable"` | `comfortable` / `compact` |
| `fontScale` | number | `1.0` | 0.85–1.5 inclusive; outside the range falls back to `1.0` with a warning |
| `highContrast` | boolean | `false` | High-contrast selection |
| `reduceMotion` | boolean | `false` | Disables non-essential transitions |
| `culture` | string | `"en"` | BCP-47 tag for number/date formatting; UI text remains invariant English |
| `logging.level` | string enum | `"information"` | `trace` / `debug` / `information` / `warning` / `error` / `fatal` |
| `ui.confirmOnExternalChange` | boolean | `true` | Confirm before reloading an externally modified project |
| `ui.showWelcomeOnStart` | boolean | `true` | Open the welcome document on a fresh start |
| `paths.lastOpenDirectory` | string or null | `null` | Last accepted open-picker directory |
| `paths.lastSaveDirectory` | string or null | `null` | Last accepted save-picker directory |
| `paths.lastFolderDirectory` | string or null | `null` | Last accepted folder-picker directory |
| `recentFiles` | array | `[]` | At most 10 `{ "path", "lastOpenedUtc" }` entries, de-duplicated by path |
| `layout.leftFraction` / `rightFraction` / `consoleFraction` | number | `260/1200` / `320/1200` / `180/800` | Side panels 0.08–0.45 and console 0.08–0.60; missing, non-finite, or out-of-range values fall back to the default. A pre-fix document with `leftWidth`/`rightWidth`/`consoleHeight` is migrated once with the 1200 × 800 reference; the writer emits only fractions |
| `layout.leftVisible` / `rightVisible` / `consoleVisible` | boolean | `true` | Panel visibility |
| `layout.activeConsoleChannel` | string | `"application"` | Channel id (`application`/`problems`); an unknown id falls back to the first available channel |
| `autosave.enabled` | boolean | `true` | Periodic sidecar autosave on/off |
| `autosave.intervalMinutes` | integer | `5` | 1–60 inclusive; out of range falls back to `5` with a warning |
| `session.openProjects` | array of string | `[]` | Paths open at the last exit, capped at 20 and de-duplicated |
| `session.activeProject` | string or null | `null` | Must be one of `openProjects` after normalization |
| `session.cleanExit` | boolean | `true` | False while running; true after a clean window close |
| `session.savedUtc` | string or null | `null` | ISO-8601 UTC time the block was written |

A wrong type, unknown enum, or out-of-range value falls back to that field's default and records a warning. A
malformed or unreadable document loads all defaults, reports a banner/console warning, and is not overwritten.
Migrations are pure functions keyed by `schemaVersion`; version 1 is the first version, so its migration is the
identity. The added `autosave` and `session` objects are additive: no schema bump, and a 23A document without
them loads with the defaults above. They remain application-local and are not contract-manifest or
cross-language fixtures.

Writes are serialized through the source-generated `AppSettingsJsonContext` to UTF-8 without a BOM, written to a
sibling temporary file, flushed with `Flush(flushToDisk: true)`, and atomically replaced. The document is capped
at 4 MiB; a failed or oversized write leaves the previous file intact.

## Simulation packages

A `.msl` package is a single immutable SQLite file, written wherever the user chooses (there is no reserved
directory). The format, schema, payloads, versioning, and verification checks are in
[Simulation package format](msl-format.md).

Immutable-package rules:

- Publication writes a new file and atomically replaces the target; it never appends to or updates a published
  package. Rebuild to change anything.
- A published package has no `results` table and no `-wal`/`-shm` sidecar. Results are written to a separate
  `.msr` store (Step 20), bound to the package signature; it is not a sidecar and must not be placed inside the
  package.
- Readers open the package read-only (`Mode=ReadOnly`, `Cache=Private`, `query_only=ON`) and never mutate it.
- The package SHA-256 is recorded by the build; the writer's result also reports the job/lattice counts, the
  lattice reuse factor, and any advisory size warning.
- `tests/contracts/packages/msl-v1/` holds the checked-in fixture and its canonical summary; the two amendment
  fixture directories preserve the pre-switch compatibility evidence.

## Atomic package publication

`mocassin build` (Step 12) writes only the immutable `.msl`; it never appends to an existing package. The writer
builds in `<target>.tmp-<pid>-<random>` beside the target (ignored as `*.msl.tmp-*`), uses WAL and batch commits
during the build, checkpoints and switches to `journal_mode=DELETE`, then hashes, fsyncs the file, fsyncs the
containing directory on Unix, and atomically replaces the target. Windows has no equivalent directory fsync in
this path; the file fsync plus atomic replace is its guarantee. A cancelled or failed build deletes the temp file
and leaves an existing package untouched. There is no `.bak` file. Orphaned `*.msl.tmp-*` files are inert and may
be removed by `mocassin clean` (Step 28).

## Planning docs

`docs/planning/` contains the internal roadmap, step pages, ADRs, contracts, and parity matrix. Zensical
excludes it with `exclude_docs = "planning/"`; it must never appear in the published site.
