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

## Desktop validation, autosave, and recovery

The desktop Validation tab (internally the Problems channel) uses the same diagnostic codes and the published anchors as the CLI; it does not
invent codes or reword messages. The read-only edit refusal is an app-level message, not a diagnostic code.

| Symptom | Likely cause | Fix |
| --- | --- | --- |
| “Not validated” after opening | The session has no retained load result (standalone/tests) | Edit the project or use **Revalidate**; a normal opened project has a retained result. |
| A diagnostic has no help link | The code is not in the catalog or has no documented anchor | Check the code in the [diagnostics reference](../reference/diagnostics.md); unknown codes are intentionally unlinked. |
| Metadata name is refused with an inline error | Empty, over 128 characters, path separators, `..`, control characters, or reserved characters | Correct the name; the project and save point are unchanged. |
| “This project is open read-only; changes cannot be saved.” | Another session owns the lock, or the file is read-only | Edit in the owning session, or use Save As to a new path. |
| “Auto-saving…” then “Autosave failed” | The sidecar could not be written (permissions/disk) | Check the project directory and free space; a `recovery/` fallback may have been written. |
| “Recovery available” remains after open | You chose **Decide later** | Use **Recover** or **Discard**; a deferred `recovery/` candidate can expire after seven days. |
| Recovery prompt on startup | The previous session ended uncleanly (`cleanExit = false`) | Restore the remembered projects, or choose **Not now**; missing paths are reported and skipped. |
| The project file is missing but a recovery snapshot exists | The main file was moved/deleted | The restore/Problems flow reports the missing path; a `.autosave` sidecar is preserved, while a `recovery/` entry remains subject to retention. |
| Crash dialog shows recovery results | The crash action ran with its 2 s budget | Saved sessions are under `<app-data>/recovery/`; timed-out sessions were not claimed saved. If the UI thread is dead, no recovery files are written. |
| Closing the window does not close | A dirty-project Save/Discard/Cancel prompt was cancelled | Choose Save or Discard; Cancel keeps every session open with its sidecar and autosave running. |

Recovery files are untrusted input: manifests are size/count bounded (64 KiB/100 entries), snapshots go through
the normal project load path (64 MiB), and the seven-day sweep deletes only known recovery snapshot/manifest
patterns. Sidecars are never swept. See
[Repository and file layout](../reference/file-layout.md#application-data-layout).

## Model-editor inline errors versus diagnostic codes

The model editors show inline messages for the same checks the validation surface reports, but the two channels
have different jobs:

- A code-bearing inline message (`PAR001`, `PAR005`, `STR003`, `STR007`, `STR009`, `TRN012`, …) is a real
  diagnostic. The message is the validator's own text; the editor does not invent or reword codes. **Open
  Problems** shows the full instance, path, related paths, and help anchor.
- **Apply is disabled** while the candidate has an error. This includes the site form's `STR007` overlap: the
  candidate has no preview because `TryCreatePreview` returns null for an overlap, so the form lists the
  `STR007` diagnostics instead of claiming preview rows.
- A blocked delete is not an error code; it is the usage index's pre-check. The detail lists the dependents
  (MMC/KMC transitions, occupation exchanges/dopings, energy references) and offers navigation. The
  energy-reference-only action is available only when every dependent is an energy reference.
- Expression parse failures (for example `1e` or `2 pi`), an out-of-range numeric value, and the read-only
  “changes cannot be saved” message are app-level messages, not diagnostic codes. They keep the project, undo
  stack, save point, and dirty state unchanged.
- A stability change warning is advisory. The Problems channel reports the `TRN012`/`TRN020` diagnostics after
  the edit; the warning does not block the edit.

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

The full create/edit/apply/rebase flow is in
[Parameterization templates](../user-guide/parameterization.md). Worked examples are
`samples/parameterizations/ceria-parameters.mocprj` and
`samples/parameterizations/barium-zirconate-parameters.mocprj`; their applied values and signatures are the
checked-in golden fixtures.

| Symptom | Likely cause | Fix |
| --- | --- | --- |
| `PRM001` error | The model geometry, rules, or catalog changed after the template was created | Rebase the template, or recreate it from the current compiled model; never reuse values across changed geometry. |
| `PRM002` warning | A template entry references a pair/group/rule ID that the current model no longer has, or its descriptor disagrees | Remove the stale entry or rebase the template; the model value wins. |
| `PRM003` warning | Required pair, group, or kinetic-rule slots are unset; defaults are applied | Review the applied defaults for the reported counts, or set the missing values explicitly. |
| `PRM006` warning | The selected explicit template contains no explicit values at all | Set at least one value, or build without selecting an empty template so the implicit default is used. |
| `PRM008` warning | A site-energy override or authored background entry targets an unknown site/particle or a particle the site cannot occupy | Correct the site/particle key or remove the entry. |
| `PRM009` info | A template was rebased; the message carries the copied, defaulted, dropped, and ambiguous counts | Review the rebase report before saving the rebound template. |
| `PRM011` warning | The template contains a Metropolis rule set | MMC rules are not parameterizable; remove the set. The compiled default remains. |
| `PRM012` warning | A pair or group entry names a particle the reference site cannot occupy | Use a particle from the site's particle set, or remove the entry. |
| `PRM015` warning | At least 20 explicit pair/group values are dominated by one non-default value | Check for a bulk-fill or paste error and vary the values deliberately. |
| `PRM016` warning | An attempt frequency is outside the plausible `1e-3..1e18 Hz` band but still positive and finite | Check the frequency; use a project template and rebuild. |

### Rebase and stale values

A stale template is never silently reinterpreted. `Apply` rejects it with `PRM001` and a diff. Run `Diff` to see
the **Added**, **Removed**, **Matched**, and **Changed** (geometry-change) slots, then choose a strategy:

| Symptom | Likely cause | Fix |
| --- | --- | --- |
| Values disappeared after `KeepMatching` | The model geometry, particle order, or chiral link changed, so the stable IDs changed | This is intended. Use `NearestGeometry` only when the model offers a unique matching geometry, or re-enter the values. |
| `NearestGeometry` copied nothing and reported ambiguity | More than one added pair slot shares the logical site pair and distance tolerance | Reduce `NearestDistanceToleranceFm` so only one candidate qualifies, or keep the safe default behavior and re-enter the values. |
| Chiral values disappeared after a geometry change | Chiral entries are exact-ID only; a changed shared `EnergyId` or chiral link cannot be proven equivalent | Re-enter the shared value once. Independent A→B and B→A values do not exist by design. |
| A group or site override disappeared | Groups and site overrides have no nearest-geometry path | Re-enter the group value, or reset the site override to the authored background. |
| A rebase report says every value was dropped | The `Reset` strategy discards all values but keeps the template identity and headers | Apply the intended values again. Use `ParameterizationFactory.Create` instead if a genuinely new template is wanted. |
| `PRM009` appears after a successful rebase | The rebase report is informational | Inspect the copied, defaulted, dropped, and ambiguous counts before committing the template. |

## Build and CLI failures

A build runs load → select → compile → validate → parameterize → expand → generate/write → publish → verify.
Read the first error; later stages are skipped. Human progress goes to stderr, so redirect stdout only when
you want the final summary.

| Symptom | Likely cause | Fix |
| --- | --- | --- |
| Exit `3`, `CLI003` | The `.msl` target already exists | Choose a new path or pass `--force`; without it the existing package is untouched. |
| Exit `2`, `CLI004` | No build template/job template/parameterization selected, or several candidates | Pass `--template` (build template), `--job-template`, or `--parameterization` with one of the listed IDs. |
| Exit `2`, `CLI005` | Malformed or out-of-range `--jobs` | Use `all`, `N`, or `A-B`, with 1-based IDs within the expanded count; zero, descending, and empty selections are rejected. |
| Exit `1`, `JOB004`/`JOB008`/`JOB019` | Supercell or job-count limit | `JOB004` is the 500,000-position cap, `JOB008` is the 1–1000 per-configuration job count, and `JOB019` is the 99,999 total-job cap; reduce the cell size or job count. |
| Exit `1`, `JOB012` | A compensated doping pair cannot be realized | Change the doping fractions, the dependent exchange, or the cell size. |
| Exit `1`, `PRM001` | The selected parameterization targets a different model signature | Rebase the template against the current compiled model, or build with the implicit default. |
| Exit `4`, `PRM` warning promoted | `--warnings-as-errors` turned a parameterization warning into a failure | Fix the reported values/template, or drop the flag knowingly. |
| Exit `4`, `CMP` error | The compiler rejected a transition, geometry, conservation, or limit | Correct the model object named by the path; `CMP006` means a configured limit was exceeded. |
| Exit `1`, `PKG` after a build | The published package failed verification | Keep the package for diagnosis, run `mocassin verify <pkg>` for the checks, and fix the model/template before rebuilding. |
| Exit `6`, `CLI006` | The build was cancelled | A pre-publication cancel deletes the temp file and preserves an existing package; a post-publication cancel leaves the valid package. |
| Exit `2`, `CLI008` | `simulate`, `collect`, `evaluate`, `export`, full `clean`, or `--results` was used | Those commands arrive in later steps; `clean --dry-run` is the only cleanup available now. A refused `clean` still lists folders read-only and deletes nothing. |
| Exit `3`, `CLI002` | A directory was passed where a file is expected, or (`build -o`/`compile -o`) a parent directory is missing | Pass a file path. `build`/`compile` do not create a missing parent; `new` intentionally creates a missing output directory and exits 0. |
| Exit `1`, `CLI009` from `inspect` | The input cannot be inspected as a package (fatal structure failure) | Run `mocassin verify <pkg>` for package-specific findings, or rebuild. A non-fatal payload or signature defect is reported by `verify`, not by `inspect`. |
| `doctor` exits 1 | Runtime, SQLite, write permission, disk space, catalog, or template check failed | Fix the failing check; a missing solver is only a warning until the solver step, and free space below 100 MiB is only a warning. A check that throws is reported as `fail` with its exception type. |

`mocassin build --preview` uses the same selection and exit codes but never writes a package. `mocassin inspect`
shows a package’s recorded `jobSelection`, counts, and signatures; `mocassin verify` runs the ordered `PKG`
checks.

## Statistics

`--stats` adds advisory counts and estimates. Statistics are non-null whenever they can be computed, even when
the model is invalid only because of feasibility/budget errors or promoted warnings; `--stats` never changes
the diagnostics or the `valid` flag.

## Solver context initialization failures

The Rust solver reports context-construction failures with the canonical solver exit codes. They are distinct
from package-read errors (exit 11) and from a run-time limit abort (exit 14).

| Exit | Error | Typical cause | Fix |
| ---: | --- | --- | --- |
| 12 | `LimitExceeded` | The job's derived arrays exceed `ContextLimits` or the 1 GiB total cap before allocation | Raise the context limits knowingly, or reduce the supercell/model; this is a data error, not a run-time abort |
| 12 | `InvalidInput` | A package field the context reads is unusable (dimension/array/table count, jump path, goal overflow, zero total goal) | Rebuild the package with the current writer; check the job's MCSP targets |
| 12 | `NoMobileParticles` | A KMC job has no non-void environment | Add a mobile particle to the start lattice or change the mechanism |
| 12 | `NoSelectableJumps` | A KMC job has no selectable direction after the selection mask/optimizers | Check the selection masks, jump counts, and KMC selection optimizers |
| 12 | `ClusterGeometry` | A cluster member image is absent, ambiguous, or the slot set is inconsistent | The v1 cluster-link limitation (OD-1); rebuild the model/package as documented in the format reference |
| 12 | `MissingOccupationCode` | A raw cluster tuple is absent from a **present** expansion map | The package is internally inconsistent; rebuild it |
| 12 | `ExpansionMapAbsent` | A non-canonical raw tuple in a package with no `code_expansions` map | Rebuild the package with the current writer; never guess a neighbouring code |
| 12 | `ResumeMismatch` | The `.mcs` state signature/seed hash/job/dimensions/counters/mobile mapping does not match | Resume the matching job/package, or start fresh |
| 13 | `NonFiniteFactor` | `kT`, `K_fix`, `K_norm`, `nu_max`, `dt`, or the field factor is zero/NaN/infinite | Check the temperature, normalization energy, attempt-frequency cap, and field settings |
| 13 | `RngState` | The 64-byte `RngStateV1` blob cannot be restored | Resume from a valid `.mcs`, or start fresh |
| 13 | `Invariant` | A CSR, pool, tracker, or jump-status invariant failed | An internal/solver bug; report it with the package and job |
| 15 | `Cancelled` | The caller's cancellation flag was set during initialization | Retry without cancellation, or handle the cancellation as the caller intended |

A zero/NaN `nu_max` and a non-finite `dt` are numerical failures (exit 13), not data errors. A **void cluster
member** is not an error: it has no cluster row and contributes zero (the empty cluster); Step 15 must not call
the cluster lookup for such a tuple. A job that cannot be represented is exit 12, never the run-time `aborted by
a configured limit` exit 14.
