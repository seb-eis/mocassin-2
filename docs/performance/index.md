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

## HPC

The solver is a process-per-job design. SLURM/PBS templates and a runner are added in Step 19. Job folders
and `solver.json` are stable contracts, so external schedulers can launch `mocassin-sim` directly.
