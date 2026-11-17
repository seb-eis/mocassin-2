# Performance and HPC

Performance targets and benchmark procedure are defined in the internal quality strategy. Public results are
published here once the solver and build pipeline exist.

> **Toolchain note.** The measurements recorded on this page were taken on .NET 10 and are kept as recorded.
> Measurements taken after the Step 11 amendment use .NET 11 (`net11.0`). Historical numbers are not rewritten
> across the toolchain change; compare only within the same runner/toolchain class.

## Planned targets

| Scenario | Target |
| --- | --- |
| Build a compiled model for 100 jobs | < 5 s on a 4-core laptop |
| Build 10⁴ small jobs | < 10 min, < 2 GB peak RSS |
| Load `.msl` and start a job | < 100 ms |
| KMC/MMC throughput | At least 80 % of the original C solver per core |
| 20×20×20 supercell memory | < 2 GB |

Benchmarks run on every release candidate; regressions above 10 % fail unless an ADR accepts them. Absolute
timings are compared only within the same runner/CPU class.

## Parameterization (Step 09)

The parameterization benchmark suite is `ParameterizationBenchmarks` in `src/dotnet/Mocassin.Benchmarks` and
is dispatched manually through the `Benchmark` workflow. It measures template creation, apply and signature,
canonical template JSON round-trip, and the implicit-default KMC apply path over the checked-in samples.

Measured on an AMD Ryzen 7 7840U (8 physical / 16 logical cores), Debian GNU/Linux 13, .NET SDK 10.0.403 /
host runtime 10.0.14 Release, BenchmarkDotNet 0.15.8:

| Benchmark | Mean | Allocated |
| --- | ---: | ---: |
| Create ceria template + summary | 7.6 µs | 21.1 KB |
| Apply ceria template + signature | 21.9 µs | 124.8 KB |
| Apply ceria KMC implicit default + signature | 3.4 µs | 70.7 KB |
| Sign an applied ceria value set | 6.9 µs | 65.9 KB |
| Canonical ceria template JSON round-trip | 138.9 µs | 204.7 KB |

All measured values are far below the design targets (< 20 ms create, < 50 ms apply, < 100 ms round-trip).
The synthetic large-model case is a separate PR-medium/nightly guard; these numbers describe the checked-in
samples only.

## Package build (Step 12)

The Step 12 acceptance workload is a synthetic 10,000-job project derived from
`samples/jobs/ceria-temperature-sweep.mocprj`: two collections, ten configurations each, `500` jobs per
configuration, mixed `6×6×6`/`8×8×8` ceria cells, no GUI. The command was:

```sh
dotnet src/dotnet/Mocassin.Cli/bin/Release/net11.0/Mocassin.Cli.dll \
  build /tmp/opencode/10k.mocprj -o /tmp/opencode/10k.msl --json
```

Measured on the AMD Ryzen 7 7840U (16 logical cores), Debian GNU/Linux 13, Linux 6.12, .NET 11.0.100 / runtime
11.0.0, Release build. Wall clock and peak RSS were captured around the child process with
`resource.getrusage(RUSAGE_CHILDREN).ru_maxrss`; the number is the whole `dotnet` process, not an isolated
allocation measurement.

| Metric | Result |
| --- | ---: |
| Jobs / configs | 10,000 / 20 |
| Wall time, full build + built-in verification | 3.76 s |
| Peak RSS | 151,820 KB (~148 MiB) |
| Package size | 9,625,600 bytes (~9.2 MiB) |
| Job selection | `all`, expanded and selected 10,000 |
| Lattice count / reuse factor | 9,998 / 1.0002 |
| Post-build verification | passed, 10 checks, 0 diagnostics |
| Standalone `mocassin verify` | 1.21 s, 135,624 KB peak RSS, valid |

Stage timings from the report were load `2.6 ms`, select `0.5 ms`, validate `204.4 ms`, compile `89.6 ms`,
build-stage validate `21.7 ms`, parameterize `13.4 ms`, expand `22.2 ms`, generate `2113.9 ms`, publish
`16.8 ms`, and verify `796.0 ms`. The full-build budget (< 10 min, < 2 GB) is met with wide margin; the
acceptance evidence and exact command are also recorded in the Step 12 page.

## HPC

The solver is a process-per-job design. SLURM/PBS templates and a runner are added in Step 19. Job folders
and `solver.json` are stable contracts, so external schedulers can launch `mocassin-sim` directly.
