# Step 09 Audit Request

> Reviewer: write the evidence, findings, and verdict directly into
> `docs/planning/audits/step-09.md`. You may edit only your own metadata (`Auditor`, `Audit date`,
> `Record state`), the checklist completion marks and inline verification notes (not item text), the
> `Evidence`, `Findings`, and `Verdict` sections, and commit only that file with a documentation-only message
> and `[skip ci]`. Do not edit the request scope, checklist item text, resolutions, code, tests, configuration,
> status dashboard, step page, or any other file.

## Request

| Field | Value |
| --- | --- |
| Step | 09 — Parameterization Templates |
| Request owner | Step agent |
| Orchestrator (step agent) | General planner/orchestrator |
| Audit initiated by | Human owner |
| Audited commit | `916bae1b66aeb0d3b8fb1e451072523a869b596e` |
| Step branch | `step/09-parameterization-templates` |
| Step tag | — |
| Closeout CI run | [`43634322838`](https://github.com/seb-eis/mocassin-2/actions/runs/43634322838) — green at `916bae1b66aeb0d3b8fb1e451072523a869b596e` |
| Closeout CI head SHA | `916bae1b66aeb0d3b8fb1e451072523a869b596e` |
| Main SHA at freeze | `b81355e00946f7abee5c4a56be23d8e1f777743e` |
| Merge-tree CI | Not required; `main` `b81355e` is an ancestor of the audited commit |
| Closeout evidence | `docs/planning/closeout/step-09.json` |
| Platform CI | Not run (Ubuntu-only policy; no platform-specific code) |
| Audit date | 2026-11-14 |
| Auditor | Fresh independent audit agent (OpenCode; no shared conversation with the planner, implementor, or orchestrator) |

Step page: [`Step 09 — Parameterization Templates`](../steps/step-09-parameterization-templates.md)

> Orchestration: the step agent (the orchestrator) freezes the step branch, requests the auditor, applies fixes
> autonomously on a new feature branch, reruns closeout CI, adds resolutions without altering reviewer text,
> and requests re-audit until the auditor no longer identifies issues. The human owner approves the final
> verdict, merge, and tag.

## Step summary

| Field | Value |
| --- | --- |
| Phase | C. Compilation |
| Depends on | 08 |
| Delivers | Project-resident, sparse parameterization templates with stable compiled identities, a values-only parameterization signature, deterministic apply/defaulting, site-energy overrides, `PRM` validation, diff/rebase reports, completeness checks, golden templates/vectors, and documentation. |

## Step-specific acceptance checklist

- [x] Creating a template from the ceria compiled model produces exactly the expected sets/rules. (audit: `ParameterizationFactoryTests.CeriaCreatesOneHeaderPerCompiledTableAndNoValues` compares every header/ID/descriptor to the compiled model and asserts no values; ceria-kmc and BaZrO₃ variants pass)
- [x] Applying the template to the same model succeeds and yields the same compiled values. (audit: `ParameterizationApplyTests` applies the checked-in ceria and BaZrO₃ templates, compares every pair/group/site value to the fixture and pins `cac3b231…`/`a7f6ebd9…`; `ParameterizationGoldenFixtureTests.GoldenAppliedValuesMatchTheCheckedInTemplates` rechecks the arrays; the implicit-default ceria-kmc golden is current)
- [x] A changed model produces a signature mismatch with a useful diff. (audit: `ParameterizationDiffSummaryTests.ChangedGeometryProducesPrm001WithAddedRemovedAndMatchedKeys` asserts `PRM001`, `Model == null`, non-empty Added/Removed/Matched and GeometryChanges; `ParameterizationValidationTests.StaleTemplateEmitsPrm001WithoutADiffFromValidateAndWithADiffFromApply` covers the surface difference)
- [x] Rebase copies unchanged values, defaults new ones, and reports removed ones without silent reuse. (audit: `ParameterizationRebaseTests` covers KeepMatching, NearestGeometry unique/ambiguous, Reset and PRM009; `ParameterizationRebasePropertyTests` proves every rebound matched value came from an exact or reported descriptor match; chiral link changes drop rather than copy)
- [x] Chiral partners share one `EnergyId` and one value; independent A→B/B→A values are unrepresentable. (audit: real BaZrO₃ has 24 chiral geometries / 12 reciprocal tables and one shared value per set; `ParameterizationChiralFixtureTests` proves the P4mm→P1 link change drops the value; `ValidatorRejectsConflictingMirroredChiralValues` rejects independent values. See F-06 for the mirrored-orientation persistence note)
- [x] Site energy overrides apply and can be reset to the authored model value. (audit: `ParameterizationSiteEnergyTests` covers every owned position, override/reset, invalid target, stale background and the signature change; `ParameterizationApplyTests.ImplicitDefault…` checks the authored ceria background)
- [x] Missing/zero values produce warnings, not silent success. (audit: implicit default suppresses `PRM003`/`PRM006`; explicit empty template emits both and still applies; explicit zero suppresses `PRM006` but not `PRM003`; see F-02 for stale-entry count accuracy)
- [x] Template JSON is canonical and round-trips, including culture, `-0.0`, and additive missing arrays. (audit: `ParameterizationTemplateTests`, `ParameterizationPersistenceTests`, and `ProjectSchemaTests` cover canonical field order/order independence, `-0.0`, comma-decimal culture, unknown-field rejection, missing arrays and store round-trip)
- [x] The parameterization signature is deterministic, finalized in the manifest, and reproduced by the
      checked-in projection vector. (audit: `signatures.parameterization` is `final`; `ParameterizationSignatureV1Tests` covers domain separation, quantization, negative zero and culture; `check_parameterization_projection.py` re-encodes all four `.bin` fixtures in Python; the C# golden test pins the same signatures. The overflow defect is F-01)
- [x] Untrusted-template limits reject oversized counts/strings/values with `PRM` errors before allocation. (audit: `ProjectValidator`/`ParameterizationTemplateValidator` reject over-limit counts, strings and keys; `MaxAppliedValueBytesIsEnforcedBeforeAllocation` and `ApplyLimitIsReportedByValidateAsAnError` cover the apply cap. Structural validation is not re-run by the service surface, see F-03)
- [x] Documentation written. (audit: user guide, project format, diagnostics, compilation pipeline, design decisions, troubleshooting, energy cross-links, performance page and nav are present; `verify.sh docs --require` is green. Stale status/feature-log and tool README text are F-04/F-05)

## Standard audit checklist

- [x] Required tests from the step page pass. (audit: full gate exit 0 — Core 639, Symmetry 223, Compilation 95, Packages 212, Contracts 14, Cli 7, App 1, Results 1, Architecture 1; parameterization-filtered runs: Packages 107, Core 69, Contracts 13; no failures/skips)
- [x] Public/protected XML documentation policy is followed. (audit: build warnings-as-errors; `PublicAPI.Unshipped.txt` additions in Core/Compilation/Contracts/Packages; ApiCompat clean; DocFX 340 models, 0 warnings/0 errors)
- [x] Documentation, status dashboard, feature log, and parity matrix are current. (audit: the dashboard row, step-page status/feature table and parity §6 rows are updated; the status "Active work" paragraph and the feature-7 outcome sentence are stale — F-04; the tool README regeneration text is stale — F-05)
- [x] Closeout CI is green at the audited commit. (audit: run `43630049214` is `CI`/`workflow_dispatch`, `completed/success`, `head_sha` `20c73bcb3b1643320ad5acbdd6ff5c0d18e2ad5b`; Detect changes, .NET, Contracts and policy and Public API compatibility green; Rust/Python skipped by path filters)
- [x] Closeout evidence matches the audited commit and the merge candidate. (audit: `check_closeout_evidence.py` prints `Closeout evidence: OK`; `audit_commit` = `closeout_ci.head_sha` = `20c73bc`, `main_sha_at_freeze` `b81355e` is an ancestor; `merge_candidate` is the audited code commit and the later `f96e7be` delta is documentation-only, so no merge-tree run is required)
- [x] If `main` moved after the freeze, a merge-tree CI run or a re-audit is recorded. (audit: `main` and `origin/main` are both still `b81355e00946f7abee5c4a56be23d8e1f777743e`; no move and no merge-tree run required)
- [x] Windows/macOS runs are recorded, or their absence is accepted because the step has no platform-specific
      code. (audit: not run; accepted under the manual platform policy. The new code is managed, path/case-neutral JSON/MessagePack and a `TimeProvider` clock; no P/Invoke, native or OS-specific branch was added. Cross-platform execution remains an accepted risk)
- [x] Format/version/fixture changes are documented and tested. (audit: project `schemaVersion` stays 1 as the pre-release additive extension; manifest project fields and generated schema are current; four parameterization projection fixtures and applied-value fixtures are generated by the real compiler/apply path and checked by C# and Python; no `.msl`/`.msr`/`.mcs` format was changed)
- [x] Security, dependency, licence, and secret-scan gates pass. (audit: NuGet licence policy 51 packages, no vulnerable packages; SPDX headers in 467 source files; no secret patterns in 811 tracked text files; cargo-deny advisories/bans/licenses/sources ok)
- [x] Code structure policy passes; no new root-level or catch-all folders. (audit: `Mocassin.Packages/Parameterization` has 19 files, the largest is 720 lines; no forbidden folder names; `structure`/Architecture.Tests green)
- [x] No unrelated changes or unapproved scope. (audit: diff `b81355e..20c73bc` is the parameterization model/contracts/service/editor, tests/fixtures, benchmarks, plugins/docs, manifest/schema and the Step 12 integration note; no CLI, GUI, `.msl` writer, solver or physics change)
- [x] Branch, tag, and status hygiene are correct. (audit: work is on `step/09-parameterization-templates`; no `step-09` tag exists locally or remotely; no merge to `main`; one closeout dispatch; no per-feature run. The status narrative is stale, F-04)

## Evidence

Commands run:

```text
$ git status --short
  (clean; branch step/09-parameterization-templates, up to date with origin)

$ git log --oneline -8
  f96e7be docs: record step 09 closeout evidence and request audit [skip ci]
  20c73bc step-09: add parameterization goldens, benchmarks, and closeout docs
  f6cfce3 step-09: add parameterization diff and rebase
  4063c4d step-09: add parameterization validation and diagnostics
  ce20049 step-09: add parameterization apply and site energies
  222c02e step-09: add parameterization factory and summary
  9a11a38 step-09: add parameterization signature and projection fixtures
  c7f9992 step-09: add parameterization template model and persistence

$ git diff --name-status 20c73bc..f96e7be
  A docs/planning/audits/step-09.md
  A docs/planning/closeout/step-09.json
  M docs/planning/status.md
  M docs/planning/steps/step-09-parameterization-templates.md
  (documentation only; verified)

$ git diff --stat b81355e..20c73bc
  111 files changed, 20316 insertions(+), 128 deletions(-)

$ git rev-parse main origin/main
  b81355e00946f7abee5c4a56be23d8e1f777743e
  b81355e00946f7abee5c4a56be23d8e1f777743e
$ git merge-base --is-ancestor b81355e 20c73bc; echo $?
  0
$ git tag -l 'step-09*'
  (empty)
$ git ls-remote --tags origin
  refs/tags/step-00 … refs/tags/step-08 only; no step-09
$ git ls-remote --heads origin
  refs/heads/main = b81355e…
  refs/heads/step/09-parameterization-templates = f96e7be…

$ PATH=/home/node/.venvs/mocassin/bin:$PATH ./tools/verify.sh all --require
  exit 0; "Closeout gate completed with required checks enabled."
  .NET: build 0 warnings / 0 errors; Core 639, Symmetry 223, Compilation 95, Packages 212,
        Contracts 14, Cli 7, App 1, Results 1, Architecture 1; dotnet format clean.
  API/dependencies: NuGet licence policy 51 packages; no vulnerable packages; ApiCompat
        compatible for all baselines.
  Rust: cargo fmt --check, clippy -D warnings and workspace tests passed.
  Python: 3 tests passed; wheel and sdist built.
  Documentation: links OK (155 files); Zensical strict "No issues found"; DocFX 340 models,
        0 warnings / 0 errors; rustdoc generated; site built.
  Contracts/policy: 18 generated artifacts current; model projections 5 fixtures;
        parameterization projections 4 fixtures; space-group catalog current (274 settings,
        sha256 1f3bf67d92b9258816ee27af2ef12f0035387fa8bad6527aff86f57a740e3395);
        process consistency OK; 20 tool tests passed; SPDX headers in 467 source files;
        no secret patterns in 811 tracked text files; cargo-deny advisories/bans/licenses/
        sources ok.

$ dotnet test src/dotnet/Mocassin.Packages.Tests/Mocassin.Packages.Tests.csproj -c Release --no-build \
    --filter "FullyQualifiedName~Parameterization"
  Passed! - Failed: 0, Passed: 107, Skipped: 0, Total: 107
$ dotnet test src/dotnet/Mocassin.Core.Tests/Mocassin.Core.Tests.csproj -c Release --no-build \
    --filter "FullyQualifiedName~Parameterization"
  Passed! - Failed: 0, Passed: 69, Skipped: 0, Total: 69
$ dotnet test src/dotnet/Mocassin.Contracts.Tests/Mocassin.Contracts.Tests.csproj -c Release --no-build \
    --filter "FullyQualifiedName~Parameterization"
  Passed! - Failed: 0, Passed: 13, Skipped: 0, Total: 13

$ PATH=/home/node/.venvs/mocassin/bin:$PATH python tools/check_closeout_evidence.py \
    docs/planning/closeout/step-09.json
  Closeout evidence: OK

$ PATH=/home/node/.venvs/mocassin/bin:$PATH python tools/contracts/check_model_projection.py
  Model projection check OK (5 fixtures).
$ PATH=/home/node/.venvs/mocassin/bin:$PATH python tools/contracts/check_parameterization_projection.py
  Parameterization projection check OK (4 fixtures).

$ GitHub API GET /repos/seb-eis/mocassin-2/actions/runs/43630049214
  id=43630049214 name=CI event=workflow_dispatch status=completed conclusion=success
  head_branch=step/09-parameterization-templates
  head_sha=20c73bcb3b1643320ad5acbdd6ff5c0d18e2ad5b
  created_at=2026-11-14T19:04:09Z updated_at=2026-11-14T19:06:59Z
  jobs: Detect changes success; .NET (ubuntu-latest) success; Contracts and policy success;
        Public API compatibility success; Python skipped; Rust skipped (path filters).
  Recent run query: no run exists at f96e7be; exactly one run at 20c73bc.
```

Repository reading and probes:

- Read `AGENTS.md` §3.8–§3.16, §4–§16; the step page; the audit request; `status.md`;
  `docs/planning/reviews/step-09.md`; `docs/planning/02-data-contracts.md`; ADRs 0009/0010/0011/0013;
  `docs/planning/appendix-b-parity-matrix.md`; `docs/planning/closeout/README.md`; the implementation diff
  `b81355e..20c73bc`; all new Core/Contracts/Packages code and tests; the benchmark; the manifest/generated
  contracts; `tools/contracts/**`; `tools/verify.sh`; and every published page the step changed.
- Project schema: `formats.project.fields.parameterizations.itemFields` now lists the five value arrays with
  nested fields and `itemAdditionalProperties: false`; `project-v1.schema.json` is regenerated; the new
  sample and `tests/contracts/` project fixtures are in `ProjectSchemaTests.fixturePaths`.
- Project JSON: `ProjectJson.Write` sorts `parameterizations` by ID and calls
  `ParameterizationTemplateCanonicalizer.Normalize`, which sorts every nested collection, pads default arrays
  and normalizes `-0.0`; `ProjectCanonicalizer` delegates to it.
- Signature: `ParameterizationSignatureV1` writes a six-field MessagePack array, `uint64` version, str32
  model signature, forced array headers, float64 values and the manifest's domain separator; the C# and
  Python checkers agree on all four vectors and the checked-in ceria/BaZrO₃ signatures.
- Apply/defaults/site background: `ParameterizationEntryWalker` resolves the authored `SiteEnergies` to every
  owned position, overlays overrides, maps pair/group/rule stable IDs, enforces masks/ranges and leaves
  ignored entries out of the applied/signed values; `ParameterizationApplyTests` and
  `ParameterizationSiteEnergyTests` cover the paths with real projects.
- Diff/rebase: `ParameterizationDiffBuilder` classifies by structured `ParameterizationValueKey`, orders
  deterministically and produces display-only geometry changes; `ParameterizationRebaseBuilder` copies only
  exact IDs or unique reported pair descriptors, excludes chiral nearest copying and never mutates inputs;
  property tests assert no unreported reuse.
- Limits and security: `ParameterizationLimits` and `ProjectStoreOptions.MaxDocumentBytes` bound structural
  input; `ParameterizationAnalyzer` checks the dense byte limit and int array bound before allocation. No
  path/code interpretation, no new dependency, no secret.
- Scratch probes under `/tmp/opencode/step09-audit` (not committed):
  - a 2,874,594-byte projection over large random tables: `Compute` (streaming) equals SHA-256 of the
    domain + `ComputeProjection`, so the `IncrementalHash` buffer path is correct for multi-buffer writes;
  - `CanonicalProjectionWriter.Quantize(1e300)` returns `Infinity`; `Compute` with a finite `1e300` rule value
    writes `float64 Infinity` (`0x7FF0000000000000`), and `Apply` with
    `ParameterizationTemplateEditor.SetRuleFrequency(..., new FrequencyHz(1e300))` throws
    `InvalidOperationException: The canonical projection contains a non-finite value.` while
    `ProjectValidator` returns no diagnostics and `Validate` returns only `PRM003`/`PRM016` warnings (F-01);
  - a template with one valid-but-stale 64-hex pair-set ID and one entry: `ProjectValidator` accepts it,
    `Apply` emits `PRM002`/`PRM003` ("125 of 126 required values unset (119 pair)") and does not emit
    `PRM006`, although no value is applied (F-02);
  - a directly supplied template with an overlong name or empty `ParameterizationId` gets no
    `PRM013`/`PRM014` from `Apply`/`Validate`; a malformed stable ID becomes `PRM002` (F-03).

## Known notes for the auditor

- The design review is `Ready with notes` at `26d6a0e` (round 4), accepted by the human owner on 2026-11-14;
  see `docs/planning/reviews/step-09.md`. Owner decisions: single-step 3.4–4.4 person-week exception, absolute
  `frequency_hz` model payload, and the implicit default build path.
- Feature-level deviations recorded in the Step 09 page:
  - `PRM005` with typed units: negative/non-finite frequencies are rejected by the JSON unit converters;
    structural `PRM005` covers zero.
  - Editor missing-set references use the existing `PROJ010` code; there is no Core `PRM` code for that
    editor-only case.
  - `PRM014` also rejects duplicate group headers and empty transition IDs.
  - `ParameterizationTemplateDefaults.CreateDefault` and `MaxAppliedValueBytes` enforcement landed in feature 4.
  - `PRM001/002/008/012` were registered in feature 4; feature 5 completed the catalog and `Validate`.
  - `Apply` returns no `ParameterizedModel` for error diagnostics (`PRM001/004/005/013`); warnings still produce
    a model.
  - The §4 pair-table invariant was clarified: every table has at least one geometry; achiral descriptors
    agree; a chiral table has exactly one `GeometryId == EnergyId` representative plus reciprocal links.
  - `NearestGeometry` is pair-only, requires the same stable/unstable kind and particle pair, and excludes
    chiral entries; ambiguous matches copy nothing and are reported.
  - The design's `100 fm` comment was corrected: 100 fm is 0.001 Å, not 0.1 Å.
  - Ceria-KMC golden values are the implicit-default apply (the sample has no template); ceria and BaZrO₃ use
    their checked-in populated templates.
  - Benchmark numbers were measured in the development container and are order-of-magnitude evidence.
- Closeout after the round-1 fixes: one replacement dispatch (`43634322838`); the earlier run `43630049214` on `20c73bc` is process history. No per-feature CI runs; Windows/macOS not run.

## Findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| **Blocker** | **F-01 — A finite but huge rule frequency overflows 12-decimal quantization, so `Apply` throws instead of returning `PRM` diagnostics and the public signature API can sign infinity.** `CanonicalProjectionWriter.Quantize` checks only the input for finiteness; `value * 1e12` overflows for finite values above ~1.8e296 and returns `Infinity`. `Apply` pre-quantizes the dense rule array, then `ParameterizationSignatureV1.Compute` calls `Quantize` again on the resulting infinity and throws. A template created with `SetRuleFrequency(..., new FrequencyHz(1e300))` passes `ProjectValidator` with no diagnostics and `Validate` with only `PRM003`/`PRM016` warnings, then `Apply` throws `InvalidOperationException` at `ParameterizationService.Apply.cs:49`; the exception escapes the public service. Calling `ParameterizationSignatureV1.Compute` directly with a finite `1e300` value instead succeeds and writes a `float64 Infinity` into the projection. This contradicts design §6.1 ("NaN/Inf are rejected"), §5/feature-4 ("user template data fails through `PRM` diagnostics", not exceptions), and the untrusted-input requirement. Add a post-quantization finiteness check (and a `PRM005` diagnostic or equivalent) plus regression tests for `1e300`/`double.MaxValue`. | `CanonicalProjectionWriter.cs:91-101`; `ParameterizationService.Apply.cs:39-49`; `ParameterizationSignatureV1.cs:102-109`; `ParameterizationEntryWalker.ValidateFrequency` (`ParameterizationEntryWalker.cs:530-559`) accepts any positive finite value and only warns `PRM016`; scratch probe output in Evidence (`Quantize(1e300)=Infinity`, projection value `0x7FF0000000000000`, `Apply` exception). No test covers a finite value above the quantization range. |
| **Minor** | **F-02 — Completeness metrics count stale or unknown template entries as set values, so `PRM003` under-reports unset slots and `PRM006` can be suppressed even when no value is applied.** `ParameterizationSummary.SetValueCount` is documented as values "inside the required slot space", but `PairMetrics`, `GroupMetrics` and `RuleMetrics` add `entries.Length` before resolving the set/ID to a compiled slot. `HasExplicitValues` therefore also becomes true for a template whose only entry is stale. The probe's template had one entry on an unknown-but-well-formed `EnergyId`: the value was ignored with `PRM002`, yet `PRM003` said 125/126 unset (119 pair) and `PRM006` was absent. This weakens the completeness warning that the step's acceptance criterion relies on. Count only entries that resolve to a required slot (and add a stale-entry summary test). | `ParameterizationSummary.cs:23`; `ParameterizationSummaryBuilder.cs:330`, `:414`, `:531`; `ParameterizationAnalyzer.cs:139-147`; scratch probe output in Evidence. Existing summary tests cover populated/dense templates only (`ParameterizationSummaryTests` lines 122–124, 176–177). |
| **Minor** | **F-03 — The service-level `Apply`/`Validate` surfaces do not run the structural template validator, so `PRM013`/`PRM014` are not emitted for a directly supplied template.** A template with an overlong name or an empty `ParameterizationId` is accepted by both methods with no `PRM013`/`PRM014`; a malformed stable ID is misreported as `PRM002` (unknown reference). The normal project path catches these in Core `ProjectValidator`, but the public API contract says a non-null template is validated as user data, and design §7.1 lists `PRM014` as emitted by "Core and Packages". The step page's feature-5 text says `Validate` is compiled-bound only, so the design and implementation need reconciliation (run the structural validator defensively or record the project-load-only boundary). | `ParameterizationAnalyzer.Analyze` (`ParameterizationAnalyzer.cs:33-113`) never calls `ParameterizationTemplateValidator`; `grep InvalidKey` finds no Packages emission; scratch probe output in Evidence. |
| **Minor** | **F-04 — The status dashboard and step-page feature log still contain stale closeout narrative.** `status.md`'s "Active work" paragraph says Step 09 is "in progress", features 1–3 are merged and "feature 4 is the current feature", while the dashboard row is `In audit` and all seven feature rows are `Done`. The step page's "Feature 7 outcome" still says the feature "is not squash-merged yet", while the feature table and "Feature 7 gate evidence" record the squash as `20c73bc`. These are documentation/status accuracy defects, not code defects; update them in the resolution/status commit. | `docs/planning/status.md:102-108`; `docs/planning/steps/step-09-parameterization-templates.md:352-353` and `:424`. |
| **Minor** | **F-05 — The contract-tools README's golden-regeneration instructions are stale.** It says "three parameterization projection fixtures" (there are four) and tells the reader to regenerate them with `FullyQualifiedName~ParameterizationProjectionFixtureTests` in `Mocassin.Compilation.Tests`; that test class was removed in feature 7. The real generator is `Mocassin.Packages.Tests.Parameterization.ParameterizationGoldenFixtureTests` (plus `ParameterizationChiralFixtureTests`), and `docs/development/compilation-pipeline.md` has the correct command. Following the README after a model change would write nothing. | `tools/contracts/README.md:28-36`; `grep -rn ParameterizationProjectionFixtureTests` finds only this README and the step page; `find tests/contracts/projections/parameterization -name '*.bin' | wc -l` = 4; `Mocassin.Packages.Tests/Parameterization/ParameterizationGoldenFixtureTests.cs:21`. |
| **Note** | **F-06 — Hand-authored mirrored chiral entries with the same value are accepted and not folded to the smaller orientation.** The design says the template stores the lexicographically smaller `(ParticleId, ParticleId)` orientation, and the canonical document hash should depend only on semantic content. `ParameterizationTemplateEditor.SetPairEnergy` normalizes, but `ParameterizationTemplateValidator` explicitly accepts a mirrored same-value entry and `ParameterizationTemplateCanonicalizer` only orders entries, so a hand-authored/`with`-built template can persist the larger orientation and produce different project bytes/hash for the same shared value. Applied values and the parameterization signature are unchanged. Fold the orientation in the canonicalizer or document the accepted representation. | `ParameterizationTemplateValidator.ValidatePairSets` mirrored-value branch; `ParameterizationTemplateCanonicalizer.NormalizePairEntries` (orders only); `ParameterizationValidationTests.ValidatorAcceptsMirroredChiralValuesWithTheSameEnergy`; `ParameterizationTemplateEditor.NormalizePairOrientation`. |
| **Note** | **F-07 — `ParameterizationLimits.MaxAppliedValueBytes` is dead public API and the step page's enforcement claim is inaccurate.** The constant is declared and appears in `PublicAPI.Unshipped.txt`, but nothing reads it; enforcement uses the configurable `ParameterizationOptions.MaxAppliedValueBytes` (same 2 GiB default). The step page's feature-1 deviation says the constant is "enforced only by the apply feature", which is not literally true. Use the constant as the option default/hard cap or remove it and correct the page. | `ParameterizationLimits.cs:74-77`; `grep MaxAppliedValueBytes` shows only the declaration, PublicAPI and `ParameterizationOptions`; `ParameterizationAnalyzer.cs:67-68`. |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. The auditor writes the findings and verdict
directly. The step agent adds resolutions in the separate `Resolution status` section without altering the
auditor's findings, evidence, or verdict.

## Record state

**State:** Re-audit required — round 1 returned `Fixes required` (F-01 Blocker; F-02–F-05 Minor; F-06–F-07
Notes). The step agent's resolutions are recorded in `## Resolution status`; after the fix feature is
squash-merged, a replacement closeout CI run and refreshed closeout evidence will follow.

`Re-audit required` is a record state, not a verdict.

## Reviewer independence

> I am a fresh context. I did not author or implement Step 09 and I have no shared conversation with the
> planner, implementor, or orchestrator. I received only the frozen commit/branch state, the audit request,
> repository links, and the required commands and scope. I read the repository myself; the findings and verdict
> above are mine. The audited code commit is `20c73bcb3b1643320ad5acbdd6ff5c0d18e2ad5b`; the request tip
> `f96e7be` is documentation-only, and this record is written against the frozen tree.

## Verdict

- [ ] Passed
- [ ] Passed with notes
- [x] Fixes required
- [ ] Waived

Follow-up owner: Step agent (implementor)
Follow-up issue/step: Fix F-01 on a new feature branch, add regression tests, update the closeout evidence/CI
run, append the resolutions, and request a re-audit. F-02/F-03 are diagnostic/API-consistency fixes; F-04/F-05
are documentation fixes. Do not merge or tag until a fresh re-audit passes.

## Resolution status

| Finding | Status | Owner | Commit |
| --- | --- | --- | --- |
| F-01 | Fixed — non-quantizable frequencies are `PRM005` errors, energies remain `PRM004`, and `Quantize` guards against overflow; regression tests added | Step agent (implementor) | `870e252` |
| F-02 | Fixed — completeness counts only entries that resolve to required slots; stale entries no longer suppress `PRM003`/`PRM006`; regression tests added | Step agent (implementor) | `870e252` |
| F-03 | Fixed — `Apply`/`Validate` run `ProjectValidator.ValidateTemplate`; `PRM013`/`PRM014` are returned for direct templates; regression tests added | Step agent (implementor) | `870e252` |
| F-04 | Fixed — status dashboard and feature-7 narrative now record the audit/re-audit state | Step agent (implementor) | `76c4cc7` |
| F-05 | Fixed — contract-tools README names four fixtures and the real Packages generators | Step agent (implementor) | `76c4cc7` |
| F-06 | Fixed — the write-time canonicalizer folds same-value mirrored chiral entries onto the smaller orientation and keeps conflicts for `PRM014` | Step agent (implementor) | `beccdc6` |
| F-07 | Fixed — `ParameterizationLimits.MaxAppliedValueBytes` is the default for `ParameterizationOptions.MaxAppliedValueBytes`; the step-page claim is corrected | Step agent (implementor) | `96e1d9f` |

The step agent fills this section after the verdict. Do not alter the reviewer-authored findings, evidence, or
verdict.

## Approval summary

The step agent completes this section before requesting human approval. It must cover the full history, not
only the latest round.

- [ ] Requested decision and frozen commit recorded
- [ ] Every review/audit round recorded (round, date, commit, reviewer, verdict)
- [ ] Every finding recorded (ID, severity, issue, evidence, resolution, fix commit, status)
- [ ] Accepted notes and waivers recorded with rationale
- [ ] Residual risks, deferred items, and follow-up owners recorded
- [ ] Explicit no-Blocker/no-Major statement, or the exact exceptions being waived
- [ ] Final CI and documentation-build results recorded

Round history:

| Round | Date | Commit | Reviewer | Verdict |
| --- | --- | --- | --- | --- |
| — | — | — | — | — |

Findings and resolutions:

| Finding | Severity | Issue | Resolution | Commit | Status |
| --- | --- | --- | --- | --- | --- |

## Human approval

- [ ] Accepted: Passed
- [ ] Accepted: Passed with notes
- [ ] Fixes required
- [ ] Waived

Approved by:
Date:

## Audit rounds

Append each new round below. Never overwrite an earlier round.

### Audit round 1

#### Independence and frozen state

Fresh independent audit context (no shared conversation with the planner, implementor, or orchestrator).
Audited code commit `20c73bcb3b1643320ad5acbdd6ff5c0d18e2ad5b` on `step/09-parameterization-templates`;
request tip `f96e7be` is documentation-only (`git diff --name-status 20c73bc..f96e7be` lists only the audit
record, closeout evidence, status and step page). `main`/`origin/main` are `b81355e0…` and an ancestor of the
audited commit. Worktree clean; no `step-09` tag; no per-feature CI run; exactly one closeout run at the audited
code commit.

#### Commands and results

`PATH=/home/node/.venvs/mocassin/bin:$PATH ./tools/verify.sh all --require` → exit 0, "Closeout gate completed
with required checks enabled" (Core 639 / Packages 212 / Compilation 95 / Contracts 14 etc.; Rust, Python,
Zensical/DocFX/rustdoc, contracts, SPDX 467, secret scan 811, cargo-deny all green). Parameterization-filtered
tests: Packages 107, Core 69, Contracts 13, all passed. `check_closeout_evidence.py` → OK; model projection
checker → 5 fixtures OK; parameterization projection checker → 4 fixtures OK. GitHub run `43630049214` is
`workflow_dispatch`/`completed`/`success` at `20c73bcb…`; the .NET, Contracts and policy, and Public API
compatibility jobs passed, Rust/Python were path-filtered. The exact outputs and the scratch-probe results are
in `## Evidence` above.

#### Findings

F-01 is a **Blocker**: finite rule frequencies above the 12-decimal scaling range quantize to infinity;
`Apply` then throws `InvalidOperationException` instead of returning `PRM` diagnostics, and
`ParameterizationSignatureV1.Compute` can sign an infinite float64. F-02 is a Minor completeness-metric defect
(stale entries suppress/corrupt `PRM003`/`PRM006` counts); F-03 is a Minor service structural-validation gap;
F-04/F-05 are Minor stale status/tool-README text; F-06/F-07 are Notes (mirrored chiral orientation and the
unused applied-byte constant). Because a Blocker remains, the step cannot be approved for merge.

#### Verdict

**Fixes required** — fix F-01 and the Minor findings, add regression tests, refresh the closeout evidence and
one replacement closeout CI run, then request a fresh re-audit. No merge or tag is authorized by this record.
