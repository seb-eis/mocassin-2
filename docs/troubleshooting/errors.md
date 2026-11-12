# Validation errors and warnings

Most model problems are reported before compilation or package building. Every message carries a stable code;
look the code up in the [diagnostics reference](../reference/diagnostics.md) for its owner, default severity,
and general help. The per-instance message and `hint` usually name the exact object and property.

## Triage

1. Run `mocassin validate <project.mocprj>` for the human table, or add `--json` for the machine-readable
   `validation-v1` report.
2. Start with `error` diagnostics. Later stages are skipped after load or model errors, so a single root cause
   can hide follow-on messages.
3. Double-click or search using the reported path (`domain`, object ID, property) in the editor.
4. Re-run validation after each fix; do not rely on stale output.

## Common failures

| Symptom | Likely cause | Fix |
| --- | --- | --- |
| Exit `3`, `PROJ002`, null identity | Missing, unreadable, or oversized file | Check the path and file permissions; the file limit is 64 MiB by default. |
| Exit `1`, `PROJ002` | BOM, invalid UTF-8, malformed JSON, schema mismatch, or unsupported migration | Save as UTF-8 without a BOM and in the current schema version. |
| `PROJ034` warning | Project symmetry metadata differs from the embedded catalog | Re-resolve the space group or accept the warning knowingly. |
| `STR`/`SYM` errors | Invalid site, dummy position, or space-group reference | Correct the structure section. |
| `PAR` errors | Duplicate names, unknown set members, or reserved identifiers | Correct the particle and particle-set sections. |
| `TRN` errors | Invalid chain, path, or state change | Correct the transition chain and its reference path. |
| `LAT`/`SIM` errors | Invalid exchange, doping, or simulation settings | Correct the lattice/simulation section. |
| `ENM001`–`ENM021` | Invalid cutoff, filter, cluster, or site energy | Correct the energy environment; `ENM018` also warns when a KMC hop leaves the environment cutoff. |
| `SIM003` warning | A simulation has no transitions | Add transitions or remove the simulation. |
| `SIM038` warning | A KMC simulation has transitions but no mobile start particle | Make a particle mobile at the first stable position. |
| `SIM039` warning | No KMC simulation has a mobile start particle | Fix the start pools; this suppresses the per-simulation `SIM038`. |
| `SIM040`/`SIM041` error | Estimated rule count or memory exceeds the advisory budget | Reduce the model or raise the budget in `ValidationOptions` knowingly. |

## Statistics

`--stats` adds advisory counts and estimates. Statistics are non-null whenever they can be computed, even when
the model is invalid only because of feasibility/budget errors or promoted warnings; `--stats` never changes
the diagnostics or the `valid` flag.
