# Step 07 — Validation, Diagnostics, and Model Statistics

**Phase:** B. Model definition  
**Depends on:** 03, 04, 05, 06
**Delivers:** A cross-domain validation pipeline, a stable diagnostic catalog, model statistics, and the
headless `mocassin validate` command.

## Implementation status

- **Status:** Not started
- **Step branch:** —
- **Step CI:** —
- **Main merge:** —
- **Step tag:** —
- **Audit:** Not requested
- **Design review:** Not requested
- **Last updated:** —

| Feature | Branch | Status | Local gates | Step CI | Squash commit |
| --- | --- | --- | --- | --- | --- |
| — | — | — | — | — | — |

## Goal

Give users a single, trustworthy answer to "is this model valid, and if not, what exactly is wrong?" before
compilation or package building. Validation must be fast, deterministic, navigable, and testable.

## Original reference

- Validation is a reflection-discovered pipeline of `DataValidator` operations per domain, with
  `ValueSetting<T>` bounds (warning/hard limits) and `WarningMessage.IsCritical`.
- Conflict handling is a second post-validation pipeline that mutates dependent data; critical conflicts can
  leave the model mutated without rollback.
- The GUI validator re-pushes the entire project into the model, inspects `IOperationReport.IsGood`, and runs a
  customization-template feasibility test.
- There is no single diagnostic catalog; messages are produced by `ModelMessageSource` and string formatting.
- Known defects: post-mutation conflicts, off-by-one particle-set count, empty-list indexing, disabled
  self-exchange check, ambiguous end-state selection.

## Design

### 1. Validation stages

Validation runs as an ordered pipeline over an immutable `Project` snapshot:

| Stage | Responsibility | Examples |
| --- | --- | --- |
| 1. Structural | JSON/schema, IDs, references, duplicates | Unknown particle ID, duplicate name |
| 2. Domain | Per-domain invariants | Cell angles, path stability, cluster geometry |
| 3. Cross-domain | Relationships between domains | Transition particle not in site set, doping source not default |
| 4. Feasibility | Cost and compilation preconditions | Permutation explosion, no transitions, zero mobiles |
| 5. Build | Template compatibility, job count, package limits | Parameterization signature mismatch |

- Stages 1–4 run for `validate`; stage 5 runs for `build`.
- Each stage returns a `DiagnosticBag`; later stages may be skipped if earlier stages have errors.
- Validation never mutates the project. Conflict resolution is explicit editing, not a hidden mutation.

### 2. Diagnostic catalog

- Every diagnostic has:
  - stable code (`PAR001`, `STR002`, `TRN010`, `ENM013`, `LAT009`, `SIM007`, `JOB003`, …);
  - severity (`Info`, `Warning`, `Error`);
  - human-readable message;
  - optional `ModelPath` (domain, object ID, property);
  - optional hint/help anchor;
  - optional related objects.
- Codes are grouped by domain and documented in `docs/reference/diagnostics.md`.
- Messages use invariant formatting and no localized text concatenation; localization resources are keyed by
  code.
- Validation results are sorted deterministically: severity (Error first), code, domain, object ID, property.
- A summary lists counts by severity and the first N errors for console output.

### 3. Cross-domain rules

Examples of rules that cannot live in a single domain:

- Every particle used in a particle set exists and is not void.
- Every site's particle set exists and contains at least one particle.
- Every transition's donor/acceptor particles are valid for the positions they occupy.
- Every kinetic path's stability sequence matches the chain group roles.
- Every kinetic path point resolves to exactly one reference site; unmatched and ambiguous points are
  `TRN020` (Step 04).
- Every kinetic path lies within the relevant stable or unstable environment cutoff; a path outside it is
  `ENM018` (Warning, owned by this cross-domain stage). Step 04 does not read energy data. Step 05 defines the
  `ENM018` constant and help anchor; Step 07 owns its emission and consolidated documentation.

Step 05 owns the energy diagnostics `ENM001`–`ENM017` and `ENM019`–`ENM021`; Step 07 aggregates them and
documents them in `docs/reference/diagnostics.md`. The Step 05 Core/Symmetry split is composed by
`ProjectValidationService`, so Step 07's catalog sees one stable, sorted result.
- Every unstable environment belongs to an unstable site.
- Every cluster center is a site with an environment.
- Every doping's source particle is the default occupation at the point the doping is applied.
- Every simulation's transitions exist and match the simulation kind.
- Every parameterization's model signature matches the current compiled model.
- Every job template's simulation/doping/optimizer references exist.
- The lattice size and doping fractions can produce a neutral supercell.

### 4. Model statistics

Compute and expose:

```csharp
public sealed record ModelStatistics(
    int ParticleCount,
    int ParticleSetCount,
    int ReferenceSiteCount,
    long P1PositionCount,
    long StablePositionCount,
    long UnstablePositionCount,
    int KineticTransitionCount,
    int MetropolisTransitionCount,
    int ClusterCount,
    long EstimatedPairInteractionCount,
    long EstimatedUniqueClusterPermutations,
    int EstimatedRuleCount,
    long EstimatedPositionsForDefaultSupercell,
    long EstimatedMemoryBytes);
```

- Counts that can exceed `int` — P1/stable/unstable positions, pair interactions, cluster permutations, and
  memory — are `long`, matching the Step 05 cost-preview fields. The 63⁹ cluster case would overflow `int`.
- Statistics use the same sampling/preview code as steps 02, 05, and 08.
- They are advisory but must be accurate enough to warn before an expensive build.
- The CLI prints them as a table or JSON.
- The GUI shows them in a "Model overview" panel and updates after edits (debounced).

### 5. Feasibility checks

- At least one mobile particle must exist for MMC/KMC.
- At least one KMC transition with a possible mobile start particle.
- Unstable sites exist for KMC; stable-only models are valid for MMC.
- A simulation's transition set is non-empty.
- The default supercell has at least one of each required species.
- Cluster/pair counts are within hard limits.
- Estimated rule count is within the configured limit (e.g. 1,000,000 rules) and memory estimate is below a
  configurable budget.
- Parameterization completeness: every compiled interaction/rule has a value (defaults may be 0/1e13, but the
  build warns if a template is incomplete).

### 6. CLI contract

```text
mocassin validate <project.mocprj> [--json] [--warnings-as-errors] [--stats]
```

- Exit `0` if no errors (warnings allowed unless `--warnings-as-errors`).
- Exit `1` if errors.
- `--json` prints:

```json
{
  "schemaVersion": 1,
  "projectId": "…",
  "valid": false,
  "summary": { "errors": 2, "warnings": 1, "infos": 0 },
  "diagnostics": [
    {
      "code": "TRN012",
      "severity": "error",
      "message": "Kinetic path position 1 must be unstable.",
      "path": { "domain": "transitions", "objectId": "…", "property": "referencePath[1]" },
      "hint": "Mark the site as unstable or choose a different reference site."
    }
  ],
  "statistics": { "...": "..." }
}
```

- Human output is a readable table with file/object context and a final summary.
- Validation of the ceria sample must complete in < 200 ms on a laptop.

### 7. GUI integration (contract for steps 24–25)

- The validator panel shows a filterable table: severity, code, message, object, property.
- Double-click navigates to the object editor and selects the property.
- A "revalidate" command runs the pipeline with cancellation; results are timestamped.
- The status bar shows error/warning counts and a "model valid" indicator.
- Statistics are shown alongside diagnostics; clicking a count opens the relevant editor.

## Implementation tasks

- [ ] Implement `ValidationPipeline` and stage ordering.
- [ ] Implement the diagnostic catalog and resource keys.
- [ ] Implement cross-domain validators.
- [ ] Implement `ModelStatistics` and cost estimation.
- [ ] Implement `mocassin validate` with human/JSON output and exit codes.
- [ ] Implement deterministic sorting and deduplication.
- [ ] Add a diagnostics reference page.
- [ ] Add one test per diagnostic code (valid and invalid fixture).
- [ ] Add the ceria "known good" and "known bad" fixtures.

## Acceptance criteria

- [ ] The ceria sample validates with zero errors and a documented warning set.
- [ ] Each diagnostic code has a test that triggers it and asserts severity, path, and hint.
- [ ] Validation never mutates the project or throws for user data.
- [ ] `mocassin validate --json` output validates against a JSON schema.
- [ ] Exit codes match the contract.
- [ ] Validation of the ceria sample completes in < 200 ms.
- [ ] The GUI can navigate from every diagnostic to its object (verified in steps 24–25).
- [ ] Documentation written

## Documentation

At this point, document validation and diagnostics:

- `docs/reference/diagnostics.md` — complete diagnostic catalog, severities, paths, hints.
- `docs/troubleshooting/errors.md` — common validation failures and fixes.
- `docs/development/testing-and-parity.md` — validation pipeline, per-code tests, determinism.
- `docs/user-guide/` — inline validation/help behavior for the affected editors.

## Tests

| Test | Assertion |
| --- | --- |
| Diagnostic catalog | Every code is unique and documented |
| Per-code tests | Trigger each code; assert severity/path |
| Cross-domain | Transition/site, doping/default, template/signature |
| Determinism | Same project yields the same ordered diagnostics |
| No mutation | Project hash unchanged after validation |
| CLI | Exit codes and JSON schema |
| Performance | Ceria validation under the budget |

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Diagnostic codes become unstable | Treat them as API; document and test |
| Duplicate/conflicting messages | Deduplicate by code + path; one message per issue |
| Expensive feasibility checks | Cache previews; run heavy checks only with `--stats` or before build |
| Validation diverges from compilation | Share the same sampling/rule code; add parity tests |

## Done when

A user can validate any project headlessly or in the GUI, see precise and navigable diagnostics, and trust
that a valid project will compile without hidden model mutations.
