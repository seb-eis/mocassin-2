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
| `tests/contracts/api-baseline/net10.0/` | `tools/update-api-baseline.sh` | Yes; refresh after an intentional public API change |
| `tests/contracts/**/*.mcs`, `*.msl`, `*.msr`, `*.log`, `Job*/` | Real writers | Yes; explicit `.gitignore` exceptions |

`contracts/generated/schema/` holds one schema per format or report. `package-v1.schema.json` is the SQLite row
schema; `package-payloads-v1.schema.json` separately describes the top-level model, job, and lattice payloads and
uses the manifest's payload unknown-key policy.

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
