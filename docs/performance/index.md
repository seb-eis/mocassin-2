# Performance and HPC

Performance targets and benchmark procedure are defined in the internal quality strategy. Public results are
published here once the solver and build pipeline exist.

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

## HPC

The solver is a process-per-job design. SLURM/PBS templates and a runner are added in Step 19. Job folders
and `solver.json` are stable contracts, so external schedulers can launch `mocassin-sim` directly.
