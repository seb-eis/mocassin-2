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

## Parameterization templates

`mocassin validate` runs only the structural `PRM` checks that live in Core `ProjectValidator`; it never
compiles. Stale or incomplete templates that need a compiled model are reported by `mocassin build` after
compilation. A build may proceed with parameterization warnings; `--warnings-as-errors` turns them into a build
failure with exit `4`. Validation-stage warnings promoted by the same flag exit `1`.

| Symptom | Likely cause | Fix |
| --- | --- | --- |
| `PRM001` error | The model geometry, rules, or catalog changed after the template was created | Rebase the template, or recreate it from the current compiled model; never reuse values across changed geometry. |
| `PRM002` warning | A template entry references a pair/group/rule ID that the current model no longer has, or its descriptor disagrees | Remove the stale entry or rebase the template; the model value wins. |
| `PRM003` warning | Required pair, group, or kinetic-rule slots are unset; defaults are applied | Review the applied defaults for the reported counts, or set the missing values explicitly. |
| `PRM006` warning | The selected explicit template contains no explicit values at all | Set at least one value, or build without selecting an empty template so the implicit default is used. |
| `PRM008` warning | A site-energy override or authored background entry targets an unknown site/particle or a particle the site cannot occupy | Correct the site/particle key or remove the entry. |
| `PRM011` warning | The template contains a Metropolis rule set | MMC rules are not parameterizable; remove the set. The compiled default remains. |
| `PRM012` warning | A pair or group entry names a particle the reference site cannot occupy | Use a particle from the site's particle set, or remove the entry. |
| `PRM015` warning | At least 20 explicit pair/group values are dominated by one non-default value | Check for a bulk-fill or paste error and vary the values deliberately. |
| `PRM016` warning | An attempt frequency is outside the plausible `1e-3..1e18 Hz` band but still positive and finite | Check the frequency; use a project template and rebuild. |

## Statistics

`--stats` adds advisory counts and estimates. Statistics are non-null whenever they can be computed, even when
the model is invalid only because of feasibility/budget errors or promoted warnings; `--stats` never changes
the diagnostics or the `valid` flag.
