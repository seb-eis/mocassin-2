# Step 21 — Analysis, Plots, and Reports

**Phase:** E. Results  
**Depends on:** 20  
**Delivers:** Aggregation across job sets, Arrhenius and transport fits, plot-ready series, headless
PNG/SVG/CSV/JSON/Parquet exports, and reproducible analysis reports.

## Goal

Move from raw per-job evaluations to scientific insight: compare temperatures/dopings, fit activation
energies, inspect barrier distributions, and produce publication-ready tables and figures without a GUI.

## Original reference

- Evaluation API produced per-job lists; users wrote F#/C# scripts and `.dat` files via `PlotData` helpers.
- No built-in fitting, aggregation, or report generation in the core application.
- The GUI had no general plotting/charting beyond the 3D viewer and MMCFE import tool.
- The original user guide shows conductivity/diffusion analysis as external scripting.

This step intentionally adds a first-class analysis layer; it is an improvement, not a parity requirement.

## Design

### 1. Analysis model

```csharp
public sealed record AnalysisSeries(
    string Name,
    string XLabel,
    string YLabel,
    string XUnit,
    string YUnit,
    IReadOnlyList<AnalysisPoint> Points);

public sealed record AnalysisPoint(
    double X,
    double Y,
    double? YError,
    JobInfo Job,
    IReadOnlyDictionary<string, string> Tags);
```

- Series are produced by queries over a filtered job set.
- Points retain the source job for traceability.
- Errors are standard errors over duplicates/configurations; `null` if not estimable.
- Tags include collection, config, particle, transition, direction.

### 2. Built-in analyses

| Analysis | X | Y | Notes |
| --- | --- | --- | --- |
| Arrhenius conductivity | 1000/T (1/K) | log10(σT) (S/m·K) | Linear fit; activation energy `Ea = -slope * k_B * ln(10) * 1000` depending on axis convention |
| Arrhenius diffusion | 1000/T | log10(D) | Tracer or σ; activation energy |
| Conductivity vs doping | doping fraction | σ | At fixed T |
| Mobility vs T | T | u | Per particle/transition |
| MSD summary | particle | MSD | Final-state only; no time series |
| Jump barrier histogram | E (eV) | count/rate | Per particle/transition |
| Correlation/Haven | T | H_R | `D_tracer/D_sigma` |
| Transition contributions | transition | σ contribution | Stacked/grouped |
| MMC energy | T | mean energy | Equilibrium |

- Each analysis defines its filters, grouping, aggregation, and fit model.
- Log-scale fits (Arrhenius) exclude non-positive conductivity/diffusion values and report how many points
  were excluded and why.
- Grouping keys are explicit and stable (collection, config, particle, transition).
- Duplicate jobs are averaged; standard error = `std/√n`.
- Missing/zero-field data are excluded with a reason, not silently dropped.

### 3. Fitting

```csharp
public sealed record LinearFit(
    double Slope,
    double Intercept,
    double SlopeStandardError,
    double InterceptStandardError,
    double RSquared,
    int PointCount);

public sealed record ArrheniusFit(
    LinearFit Fit,
    double ActivationEnergyEv,
    double Prefactor,
    double TemperatureRangeK);
```

- Ordinary least squares with standard errors and R².
- Weighted fit when point errors are available (`1/σ²`), with a flag.
- Report the temperature range and warn on extrapolation.
- Activation energy formula documented for the chosen x-axis (`1000/T` vs `1/T`).
- No hidden unit conversions; inputs are SI, labels state units.

### 4. Plot rendering

- Use LiveCharts2 (`LiveChartsCore.SkiaSharpView.Avalonia`) for interactive charts and its SkiaSharp rendering for headless PNG/SVG export (falling back to PNG + CSV if a vector backend is unavailable).
- `IPlotRenderer` abstraction so tests can assert on data without pixels.
- Default style:
  - white background, black axes, colorblind-friendly palette;
  - axis labels with units;
  - legend, grid, fit line with equation/R²;
  - deterministic size/DPI.
- Supported exports: PNG, SVG, PDF (optional), CSV of the underlying series.
- Plot definitions are data-driven (`PlotSpec`) so the GUI can edit them and the CLI can render the same spec.

### 5. Reports

```text
mocassin report <project.msl> --template <name> -o report/
```

- A report is a directory containing:
  - `report.md` (or HTML) with metadata, method, tables, figures;
  - `figures/*.png|svg`;
  - `data/*.csv`;
  - `manifest.json` with versions, filters, package hash, solver version, seeds.
- Built-in templates:
  - `conductivity` — Arrhenius, doping series, transition contributions;
  - `diffusion` — D vs T, Haven ratio, MSD summary;
  - `mmc` — energy convergence, equilibrium;
  - `jump-statistics` — barrier histograms.
- The report includes a "reproducibility" section: package signature, job selection, solver version, analysis
  version.
- No hidden state; running the same command on the same package produces byte-identical data (figures may
  differ by renderer version; record it).

### 6. CLI

```text
mocassin analyze <project.msl> --analysis arrhenius-conductivity
                  [--where ...] [--group-by ...] [-o series.csv] [--json]
mocassin plot <project.msl> --analysis ... --spec plot.json -o figure.png|svg
mocassin report <project.msl> --template conductivity -o report/
mocassin export <project.msl> --query ... --format csv|json|parquet -o file
```

- `--where` supports a safe filter expression (kind, collection, config, temperature range, field range,
  status, particle, transition).
- `--group-by` accepts a comma-separated list of stable keys.
- `--json` emits the series/fit objects.
- `--format parquet` is optional; if the dependency is too heavy, ship CSV/JSON only and add Parquet later.
- All commands support `--jobs`, `--parallel`, and cancellation.

### 7. Performance and streaming

- Analyses stream job results; they do not materialize all `.mcs` blobs at once.
- Aggregations use bounded memory: per-group accumulators (count, sum, sum of squares).
- Fits operate on aggregated points, not raw cycles.
- Plot rendering happens after aggregation.
- 10,000 jobs with 10 series should analyze in < 1 minute on a laptop (excluding blob I/O).

### 8. Validation

- Analytic two-state/two-temperature datasets produce known Arrhenius slopes.
- Weighted vs unweighted fits agree when errors are equal.
- Missing data handling is explicit.
- Report manifest validates against a schema.
- Plot data equals CSV data exactly (no rounding before export).
- Reproducibility: same input/versions → same CSV/JSON bytes.

## Implementation tasks

- [ ] Implement `AnalysisSeries`, `AnalysisPoint`, grouping/aggregation.
- [ ] Implement the built-in analyses and filters.
- [ ] Implement linear/weighted fits and Arrhenius conversion.
- [ ] Implement `IPlotRenderer` with LiveCharts2/SkiaSharp PNG/SVG output.
- [ ] Implement CSV/JSON (and optional Parquet) exporters.
- [ ] Implement the Markdown/HTML report builder and templates.
- [ ] Implement `mocassin analyze`, `plot`, `report`, `export`.
- [ ] Add analytic datasets and golden outputs.
- [ ] Document units, formulas, and report templates.

## Acceptance criteria

- [ ] Arrhenius analysis of a synthetic dataset recovers the input activation energy within 1e-6 eV.
- [ ] Grouping by collection/config/temperature is deterministic.
- [ ] Plots render headlessly on all three OSes with labels/units.
- [ ] A report contains manifest, tables, figures, and data with matching values.
- [ ] Same package/command produces identical CSV/JSON.
- [ ] Large job sets stream within memory budget.
- [ ] Zero-field/empty groups are excluded with a reported reason.
- [ ] Documentation written

## Documentation

At this point, document analysis and reporting:

- `docs/user-guide/results.md` — analyses, grouping, fits, plots, reports.
- `docs/physics/transport.md` — Arrhenius/Nernst–Einstein/Haven definitions used by the fits.
- `docs/examples/` — plot/report examples with downloadable data.
- `docs/reference/cli.md` — `analyze`, `plot`, `report`, `export`.

## Tests

| Test | Assertion |
| --- | --- |
| Aggregation | Mean/SEM for known samples |
| Arrhenius | Slope/activation energy |
| Weighted fit | Matches analytic weighted LS |
| Filters | Inclusive/exclusive ranges |
| Plot spec | Data → SVG/PNG without exceptions |
| Report | Manifest/schema and file set |
| Reproducibility | Byte-identical CSV/JSON |
| Streaming | Memory bound for many jobs |

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Unit/formula mistakes | One physics module, dimensional tests |
| Fit misuse/extrapolation | Report range, warnings, R²/errors |
| Plot dependency churn | Abstraction + golden SVG/data tests |
| Large exports | Streaming writers, optional Parquet |
| Report drift | Manifest with versions/hashes |

## Done when

Users can aggregate, fit, plot, and report simulation results reproducibly from the CLI, with units and
provenance recorded.
