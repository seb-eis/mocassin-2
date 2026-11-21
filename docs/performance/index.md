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

## Solver package load (Step 13)

The solver reader is benchmarked with Criterion in `src/rust/crates/mocassin-msl/benches/load.rs`. The command is:

```sh
cargo bench --manifest-path src/rust/Cargo.toml -p mocassin-msl --bench load
```

It opens `tests/contracts/packages/msl-v1/ceria-temperature-sweep.msl` read-only, loads job 1 with an empty model
cache (cold), and loads job 1 again with the model cached (warm). Measured on the local container class
(AMD Ryzen 7 7840U, Debian GNU/Linux 13, Rust 1.99.1, Criterion 0.8.2, release, single-threaded):

| Measurement | Median |
| --- | ---: |
| Open the package (connection, pragmas, identity/schema checks) | 171.1 µs |
| Open + cold `load_job(1)` (model, lattice, job, both signatures) | 343.1 µs |
| Warm `load_job(1)` (model cached) | 71.1 µs |

All three are far below the `< 100 ms` load target. That target is a **ceria-fixture budget**, not a promise for
a 512 MiB package: the reader's `ReaderLimits` bound the large-package path, and a large payload may take
proportionally longer. A same-class rerun on the busy container measured open 271.0 µs / cold 351.4 µs / warm
68.0 µs: the open variance (+58 %) is environment noise, and the baseline is flagged
**boundary-calibrated for Step 29** (the auditor's run saw +10.4 %). The 10 % same-class regression rule is not
relaxed. The raw Criterion logs are
[`step-13-load-2026-11-20.txt`](../../tests/reference/bench/step-13-load-2026-11-20.txt) and
[`step-13-load-2026-11-20-rerun.txt`](../../tests/reference/bench/step-13-load-2026-11-20-rerun.txt); the
structured evidence and the same-runner-class baseline are
[`step-13-load.json`](../../tests/reference/bench/step-13-load.json) and the `rustLoad` entry in
[`baselines.json`](../../tests/reference/bench/baselines.json). The RNG draw/refill/state numbers and their
baseline are in [`step-13-rng.json`](../../tests/reference/bench/step-13-rng.json) and the `rustRng` entry.

## Solver context initialization (Step 14)

The context benchmark is `src/rust/crates/mocassin-solver/benches/context.rs`:

```sh
cargo bench --manifest-path src/rust/Cargo.toml -p mocassin-solver --bench context
```

It measures `SimulationContext::initialize` for the real-writer
`tests/contracts/packages/step-14-ceria-10x10x10/step-14-ceria-10x10x10.msl` package (table-free
ceria, 4,000 environments) and for the `step-13-populated` model cloned onto a hand-built 10×10×10 lattice
(3,000 environments, six pair tables and one cluster table). Setup/cloning is outside the measured region.
Measured on the local container class (AMD Ryzen 7 7840U, Debian GNU/Linux 13, Rust 1.99.1, Criterion 0.8.2,
release, single-threaded):

| Scenario | Median | Memory plan / capacity |
| --- | ---: | ---: |
| Ceria 10×10×10 (table-free) | 75.6 µs | 308,632 B / 308,632 B |
| Populated clone 10×10×10 | 315.3 µs | 234,832 B / 234,864 B |

Both are far below the `< 100 ms` and `≤ 64 MiB` budgets. The same-runner-class baseline is the `rustContext`
entry in [`baselines.json`](../../tests/reference/bench/baselines.json); the structured evidence is
[`step-14-context.json`](../../tests/reference/bench/step-14-context.json) and the raw Criterion log is
[`step-14-context-2026-11-21.txt`](../../tests/reference/bench/step-14-context-2026-11-21.txt). The 10 % rule is
applied only within that class. The table-free ceria fixture is not a pair/cluster-scale measurement; the
populated clone is.

The allocation proof is `src/rust/crates/mocassin-solver/tests/allocation.rs`, its own test binary with a
counting global allocator. The ceria 10×10×10 build makes 98 allocations / 316,872 B against a 308,632 B plan
(capacity equal to the plan); the populated 2×2×2 build makes 103 allocations / 11,861 B against a 2,672 B plan.
After construction, the hot accessors (`node_energy`, `site_energy`, `cluster_energy`, `cluster_code`,
`link_range`, `incoming`, `jump_links`, `mobile_positions`, the tracker mappings, `dt`, goals, normalization)
and 200 pool `set_selectable` particle-change cycles perform **zero** allocations; the pool capacities/pointers
are unchanged. The allocation-test process peak RSS was 11,060 KiB (Linux `VmHWM`, non-gating context next to
the memory report); the context plan itself is far below the 64 MiB budget. The build-time temporary allocations
are the plan entries and the link/cluster/pool builders; the per-array capacities are fixed by the plan.

## HPC

The solver is a process-per-job design. SLURM/PBS templates and a runner are added in Step 19. Job folders
and `solver.json` are stable contracts, so external schedulers can launch `mocassin-sim` directly.
