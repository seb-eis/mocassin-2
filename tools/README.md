# Tools

Repository tooling. None of these directories contain production runtime code.

## Local gates and scripts

| File | Purpose |
| --- | --- |
| [`verify.sh`](verify.sh) | Local validation: `dotnet`, `api`, `structure`, `rust`, `python`, `docs`, `contracts`, `policy`, or `all`; use `--require` or `--closeout` before closeout |
| [`structure-policy.json`](structure-policy.json) | Folder/file limits, forbidden folders, and concept suffixes for the structure test |
| [`api-projects.txt`](api-projects.txt) | Shared list of public library assemblies checked by ApiCompat |
| [`check-api-compat.sh`](check-api-compat.sh) | ApiCompat check against the checked-in baseline assemblies |
| [`update-api-baseline.sh`](update-api-baseline.sh) | Refresh the baseline after an intentional public API change |
| [`check-license-headers.py`](check-license-headers.py) | SPDX header check |
| [`check-no-secrets.py`](check-no-secrets.py) | Secret-pattern scan |
| [`check_docs_links.py`](check_docs_links.py) | Relative-link and anchor check across the root guides and `docs/` |
| [`check_process_consistency.py`](check_process_consistency.py) | Status/verdict vocabulary, template fields, gate text, and unused-record drift |
| [`check_closeout_evidence.py`](check_closeout_evidence.py) | Audit, closeout CI, main SHA, and merge-candidate consistency |

## Directories

| Directory | Purpose |
| --- | --- |
| [`contracts/`](contracts/README.md) | Validate `contracts/manifest.json` and generate schema/constants/fixture scaffolding |
| [`docs/`](docs/README.md) | Zensical, DocFX, and rustdoc build/serve scripts and pinned requirements |
| [`docfx/`](docfx/README.md) | DocFX configuration for the .NET API reference |
| [`hpc/`](hpc/README.md) | SLURM/PBS templates and process-per-job helpers (later step) |
| [`reference/`](reference/README.md) | Read-only reference build/generation scripts for the original implementation |
| [`symmetry/`](symmetry/) | Space-group catalog generator and pinned requirements |

## Closeout checks

Before a step closeout:

```sh
./tools/verify.sh all --require
python tools/check_closeout_evidence.py docs/planning/closeout/step-NN.json
```

`--require` makes a skipped required check a failure. `cargo-deny` must be installed for a closeout run. The
closeout evidence file is described in [`docs/planning/closeout/README.md`](../docs/planning/closeout/README.md).

## Tool tests

The checkers have unit tests:

```sh
python -m unittest discover -s tools/tests -t .
```
