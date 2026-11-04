# Step 22 — Python Analysis API

**Phase:** E. Results  
**Depends on:** 18, 20
**Delivers:** A small, read-only Python package for `.mcs`/`.msr` files with basic multi-file evaluations, pytest
coverage, and mkdocstrings-generated API docs inside the Zensical site.

## Implementation status

- **Status:** Not started
- **Step branch:** —
- **Step CI:** —
- **Main merge:** —
- **Step tag:** —
- **Audit:** Not requested
- **Last updated:** —

| Feature | Branch | Status | Local gates | Step CI | Squash commit |
| --- | --- | --- | --- | --- | --- |
| — | — | — | — | — | — |

## Goal

Let scientists use Python for routine post-processing without the .NET API or GUI. The package must read the
MessagePack `.mcs` format, expose the raw arrays, and provide a small set of well-tested helpers for the most
common evaluations across one or many simulations.

This is deliberately **not** a second full evaluation framework. It is a minimal, stable reader plus a handful
of high-value helpers.

## Original reference

- The original analysis workflow was .NET/F#-centric; Python was used mainly for HPC job orchestration and
  result collection, not for reading `.mcs`.
- The original `.mcs` format was a raw native buffer with no magic/version/checksum, which made independent
  Python parsing impractical.
- The new MessagePack `.mcs` format is self-describing and already contains the cell vectors, particle
  charges/symbols/vacancy flags, temperature, and field metadata needed for basic analysis.

## Design

### 1. Package and repository layout

```text
src/python/
├── pyproject.toml
├── README.md
├── mocassin/
│   ├── __init__.py
│   ├── mcs.py            # McsFile, McsMetadata, read_mcs, read_mcs_many
│   ├── msr.py            # MsrFile, result rows, read_msr, read_msr_many
│   ├── evaluate.py       # single-file helpers
│   ├── aggregate.py      # McsSet, grouping, multi-file summaries
│   └── py.typed
└── tests/
    ├── conftest.py
    ├── test_mcs_reader.py
    ├── test_evaluate.py
    └── test_multi_file.py
```

- Import package: `mocassin`.
- Distribution name: `mocassin-analysis` (avoids a likely PyPI name collision; confirm availability at release).
- Python 3.11+.
- Runtime dependencies: `msgpack`, `numpy`.
- Dev/test dependencies: `pytest`, `pytest-cov`, `hypothesis`, `mkdocstrings[python]`.
- Type hints throughout; ship `py.typed`.
- No plotting dependency; return NumPy arrays so users can use matplotlib/plotly themselves.

### 2. Reader API

```python
from pathlib import Path
import numpy as np
from mocassin import read_mcs, read_mcs_many, read_msr, read_msr_many

mcs = read_mcs("Job00001/run.mcs")
mcs.metadata.model_signature
mcs.metadata.temperature_k
mcs.lattice           # np.ndarray, dtype=uint8, shape (A*B*C*P,)
mcs.counters          # structured/record array or dataclass array
mcs.global_trackers   # np.ndarray, float64, shape (N, 3), fractional
mcs.mobile_trackers
mcs.static_trackers
mcs.mobile_mapping
mcs.jump_statistics
mcs.energy_samples

files = read_mcs_many(sorted(Path("runs").glob("Job*/run.mcs")))

store = read_msr("ceria.msr")
store.results                 # status/exit code/timestamps per job
store.read_mcs(42)            # run.mcs bytes for job 42, parsed as McsFile
```

- `McsFile` is a frozen dataclass with:
  - `path: Path`
  - `metadata: McsMetadata`
  - `lattice: np.ndarray` (`uint8`)
  - `counters: tuple[McsCycleCounter, ...]` or a structured array
  - `global_trackers`, `mobile_trackers`, `static_trackers`: `np.ndarray` shape `(N, 3)`, fractional
  - `mobile_mapping`: `np.ndarray` (`uint32`)
  - `jump_statistics`: tuple of `McsJumpStatistics`
  - `energy_samples`: tuple of `McsEnergySample`
- `MsrFile` is a frozen dataclass with `path`, `schema_info`, `results` (job id, status, exit code, timestamps,
  blob sizes/hashes), and `read_mcs(job_id, phase="run")`.
- Read-only: no writer, no mutation helpers.
- Validate `format == "mocassin.mcs"`/`.msr`, version, metadata, lattice length, particle indices, tracker
  counts, histogram shapes, finite values, package binding, and the size cap.
- `.msr` support is read-only and validates `package_signature`/`package_format_version` when a package path is
  supplied.
- Unknown fields are ignored; unsupported `format`/version raises a typed exception
  (`McsFormatError`, `McsVersionError`, `McsDataError`).
- Use `msgpack.unpackb(..., raw=False, strict_map_key=False)` and convert to NumPy arrays once. Do not parse
  values lazily in hot loops.
- Field names, format versions, and required fields come from generated constants derived from
  `contracts/manifest.json`; do not hand-copy them into the Python package.

### 3. Single-file helpers

```python
from mocassin import evaluate

counts = evaluate.particle_counts(mcs)             # np.ndarray[int], indexed by particle id
occupation = evaluate.lattice_occupation(mcs)      # per-position particle ids
displacements = evaluate.cartesian_displacements(mcs)  # (N,3) float64, metres
msd = evaluate.mean_squared_displacement(mcs)     # per particle / radial
diffusion = evaluate.diffusion_coefficients(mcs)  # D_x,D_y,D_z,D_r
mobility = evaluate.mobility(mcs)                 # field direction, m²/(V s)
conductivity = evaluate.conductivity(mcs)         # S/m, per particle
hist = evaluate.jump_barrier_histogram(mcs, tracker=0)
```

Rules:

- Convert fractional tracker displacements to Cartesian using `metadata.cell_vectors_angstrom`, then to metres.
- Diffusion uses origin-based cumulative MSD: `D_x = MSD_x/(2t)`, `D_total = MSD_total/(6t)`. `N_s` is the
  tracked ensemble size, not the total particle count.
- Mobility/conductivity: use `temperature_k`, `electric_field_vm`, `electric_field_direction`, particle
  charge, and species density. Return per-species and total values (ADR 0012). Return `None`/`nan` for zero
  field, zero time, or `z == 0`; exclude vacancies and neutral particles from charge transport.
- Particle counts include void (index 0); helpers that need charge carriers filter void/vacancies.
- Jump histograms return bin edges/centres and counts; under/overflow are separately exposed.
- No hidden unit conversions: all outputs are SI except energies in eV and lattice coordinates in Å, matching
  the data contract.

### 4. Multi-file helpers

```python
from mocassin import McsSet

runs = McsSet.from_paths(paths)
runs.filter(kind="kmc", temperature_k=(700, 1100))
runs.group_by("temperature_k")
runs.mean_particle_counts()
runs.mean_msd()
runs.mean_diffusion()
runs.mean_conductivity()
runs.jump_histogram_sum()
runs.to_records()  # list[dict] for pandas/CSV
```

- `McsSet` is a lightweight collection of `McsFile` objects.
- Filtering and grouping use metadata fields only; no model loading.
- Aggregations return NumPy arrays plus counts and standard errors where meaningful.
- No silent dropping: functions return the number of included/excluded files and the reasons (e.g. zero field,
  missing trackers, wrong kind).
- A basic `arrhenius_fit` helper may be included if it stays small; otherwise users can fit the returned
  arrays themselves. The first release focuses on read/aggregate, not a full statistics package.

### 5. Tests

- **pytest** is the test framework.
- Fixtures:
  - Rust-generated `.mcs` files for KMC and MMC, small and medium lattices;
  - a malformed/truncated file;
  - a future `formatVersion` fixture;
  - multi-file set with different temperatures/fields.
- Tests:
  - reader metadata/lattice/counters/tracker shapes and values match a canonical JSON summary;
  - cross-language comparison with the .NET reader on the same fixture;
  - single-file formulas against hand calculations;
  - zero field / `z == 0` handling;
  - multi-file filtering/grouping/aggregation;
  - malformed/version/size rejection;
  - dtype/shape/units;
  - optional `pytest --cov` coverage threshold (e.g. 85 % for the package).
- Run on Python 3.11, 3.12, and 3.13 in CI.

### 6. Documentation

- Zensical uses native `mkdocstrings` for the Python API:
  - `docs/api/python/index.md` contains `::: mocassin` and selected submodules;
  - `docs/api/index.md` links to `/api/python/` alongside `/api/dotnet/` and `/api/rust/`.
- `mkdocstrings` is configured in `zensical.toml`; the docs build installs the Python package in editable mode
  so docstrings resolve.
- User-facing examples:
  - `docs/user-guide/results.md` — reading a job and computing basic quantities;
  - `docs/tutorials/result-analysis.md` — multi-file Python workflow;
  - `docs/reference/mcs-format.md` — the schema the reader consumes.
- Docstrings use Google or NumPy style consistently and include units and shapes.
- API docs are generated, not hand-maintained.

### 7. Packaging and release

- Build wheel/sdist with `python -m build`.
- Publish `mocassin-analysis` to PyPI (or attach the wheel to the release if the name is unavailable).
- Version aligned with the package/state format; `__version__` exposed.
- CI builds the wheel and runs `pip install` + pytest in a clean environment.
- Step 31 includes the wheel/sdist in release artifacts.

## Implementation tasks

- [ ] Create `src/python/pyproject.toml`, package skeleton, `py.typed`, and README.
- [ ] Implement `McsFile`, metadata, counters, and `read_mcs`/`read_mcs_many`.
- [ ] Implement `MsrFile`, result metadata, and `read_msr`/`read_msr_many` (read-only).
- [ ] Implement schema validation and typed exceptions.
- [ ] Implement single-file helpers (`particle_counts`, displacement/MSD/diffusion, mobility/conductivity,
  jump histograms).
- [ ] Implement `McsSet` filtering/grouping/aggregation and `to_records`.
- [ ] Add pytest fixtures for `.mcs`/`.msr` and cross-language reader tests using manifest-generated constants.
- [ ] Add CI jobs for Python 3.11–3.13, wheel build, and pytest.
- [ ] Add `docs/api/python/index.md` with mkdocstrings directives and Zensical config.
- [ ] Add user-guide/tutorial examples.
- [ ] Include the wheel/sdist in release artifacts.

## Acceptance criteria

- [ ] `read_mcs` reads KMC and MMC fixtures and returns the expected metadata, lattice, counters, trackers, and
  histograms.
- [ ] `read_msr` reads results/status metadata and extracts `.mcs` blobs with package binding checks.
- [ ] The Python reader matches the .NET reader on the same fixture.
- [ ] Single-file helpers match hand calculations and .NET evaluation values within tolerance, including
  per-species/total transport and origin-based MSD.
- [ ] Multi-file filtering/grouping/aggregation works and reports excluded files.
- [ ] Zero-field and `z == 0` cases return `None`/`nan`, never `Infinity`/`NaN` silently.
- [ ] Malformed/unsupported files raise typed errors with path and field context.
- [ ] `pytest` passes on Python 3.11–3.13 with coverage ≥ 85 %.
- [ ] `mkdocstrings` renders the API under `/api/python/` in the Zensical site.
- [ ] The wheel/sdist builds and installs in a clean environment.
- [ ] Documentation written

## Documentation

At this point, document the Python reader and basic evaluations:

- `docs/api/python/index.md` — mkdocstrings-generated API reference.
- `docs/user-guide/results.md` — Python quickstart, reading a job, units, examples.
- `docs/tutorials/result-analysis.md` — multi-file workflow with NumPy/matplotlib.
- `docs/reference/mcs-format.md` — Python-visible schema, metadata, and size limits.
- `docs/reference/msr-format.md` — Python-visible results store and package binding.
- `docs/development/testing-and-parity.md` — cross-language fixture tests and Python CI.
- `docs/api/index.md` — link the Python API alongside .NET and Rust.

## Tests

Tests use real `.mcs` files produced by the Rust solver and generated toy documents; the reader is never
mocked.

| Test | Assertion |
| --- | --- |
| Reader | Metadata/lattice/counters/trackers/histograms |
| Cross-language | Python vs .NET canonical summary |
| Single-file | Counts, displacement, MSD, diffusion, per-species/total mobility |
| Results store | `.msr` metadata, blob extraction, package binding |
| Multi-file | Filter/group/aggregate; excluded counts |
| Edge cases | Zero field, z=0, empty set, MMC |
| Errors | Bad type/version/size/index |
| Packaging | Wheel/sdist clean install |
| Docs | mkdocstrings build under /api/python/ |

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Schema drift between Rust/.NET/Python | One fixture set; cross-language tests in CI; manifest checks for `.msr` |
| NumPy memory for many files | Read-only arrays, bounded multi-file APIs, document memory use |
| Scope creep into a full stats package | Keep first release to read + basic aggregation; defer advanced fits |
| PyPI name collision | Use `mocassin-analysis` distribution; confirm at release |
| mkdocstrings docstring parsing | Consistent docstring style and CI docs build |

## Done when

A scientist can `pip install mocassin-analysis`, read one or many `.mcs` files and `.msr` stores, compute
particle counts, origin-based displacement/MSD/diffusion, per-species and total mobility/conductivity, and jump
histograms, and browse generated Python API docs in the same Zensical site as the rest of MOCASSIN.
