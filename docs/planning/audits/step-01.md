# Step 01 Audit Request

## Request

| Field | Value |
| --- | --- |
| Step | 01 — Domain Kernel and Project Format |
| Request owner | Step agent |
| Audit status | **Re-audit required** |
| Audited commit | `05c41ab` |
| Step branch | `step/01-domain-kernel` |
| Step tag | — |
| CI run | [41927992251 — green](https://github.com/seb-eis/mocassin-2/actions/runs/41927992251) |
| Audit date | 2026-11-04 |
| Auditor | Pending fresh read-only re-audit context |

Step page: [`Step 01 — Domain Kernel and Project Format`](../steps/step-01-domain-kernel-and-project-format.md)

The original audit returned **Fixes required** on 2026-11-04. The main fixes were squash-merged as `45d5a4b`;
the two re-audit minors were fixed and squash-merged as `05c41ab`. This request is now **Re-audit required**.
The commit that updates this request is documentation-only and carries `[skip ci]`; it does not change the
audited commit.

## Step summary

| Field | Value |
| --- | --- |
| Phase | A. Foundations |
| Depends on | 00 |
| Delivers | The immutable domain kernel, stable IDs, units, diagnostics, and the versioned JSON project file with canonical serialization and migrations. |

## Audit scope

- [x] Domain kernel, immutable project aggregate, IDs, units, diagnostics, and editing primitives
- [x] Versioned project JSON, canonical serialization, and canonical hash rules
- [x] Schema migrations and future-version rejection
- [x] Atomic save, `.bak`, lock file, stale-lock recovery, and autosave behavior
- [x] Reference validation and diagnostic codes
- [x] Project store behavior and round-trip tests
- [x] Property tests for units, canonicalization, and edit invariants
- [x] Public/protected XML documentation policy
- [x] Documentation, status dashboard, feature log, and parity matrix
- [x] CI result, scope hygiene, and no unapproved format changes

## Step-specific acceptance checklist

- [x] A `Project` round-trips through JSON with no loss for all sample projects.
- [x] Canonical serialization is byte-identical after reordering collections.
- [x] Invalid references (unknown IDs, wrong domain IDs) produce diagnostics with paths.
- [x] A future `schemaVersion` is rejected with a clear message.
- [x] A v1 fixture loads and saves without changes.
- [x] A failed save (simulated I/O error) leaves the original file intact.
- [x] A lock file prevents concurrent edits and recovers from a stale lock.
- [x] The JSON Schema validates all fixtures.
- [x] Documentation written

## Standard audit checklist

- [x] Required tests from the step page pass.
- [x] Public/protected XML documentation policy is followed.
- [x] Documentation, status dashboard, feature log, and parity matrix are current.
- [x] CI is green at the audited commit.
- [x] Format/version/fixture changes are documented and tested.
- [x] Security, dependency, licence, and secret-scan gates pass.
- [x] No unrelated changes or unapproved scope.
- [x] Branch, tag, and status hygiene are correct.

## Evidence

Original audit at `1d1ad57`:

```text
dotnet test Mocassin.slnx -c Release ................ pass (147 total)
dotnet format Mocassin.slnx --verify-no-changes ..... pass
zensical build --strict ............................. pass
tools/docs/build-docs.sh ............................ pass
python tools/contracts/generate.py --check .......... current (17 files)
```

CI: [41915254710](https://github.com/seb-eis/mocassin-2/actions/runs/41915254710) — green on Ubuntu + Windows.

Re-audit at `05c41ab`:

```text
dotnet test Mocassin.slnx -c Release ................ pass (171 total)
dotnet format Mocassin.slnx --verify-no-changes ..... pass
tools/check-api-compat.sh ........................... pass (all assemblies)
zensical build --strict ............................. pass
tools/docs/build-docs.sh ............................ pass (DocFX 0 warnings, 0 errors)
python tools/contracts/generate.py --check .......... current (17 files)
python tools/contracts/check_nuget_licenses.py ...... pass (21 packages; OSMFEULA exceptions recorded)
dotnet list ... package --vulnerable ................ no vulnerable packages
python tools/check-license-headers.py ............... pass (71 source files)
python tools/check-no-secrets.py .................... pass (315 tracked text files)
```

CI: [41927992251](https://github.com/seb-eis/mocassin-2/actions/runs/41927992251) — green on Ubuntu + Windows.
The previous fix squash `45d5a4b` was verified by CI `41924977588`.

## Known notes for the auditor

- Step 01 was rebased onto `main` after the independent audit policy landed. The audited commit contains the
  rebased feature commits plus the audit-structure integration.
- Feature squash hashes in the step page are the rebased hashes. Their per-feature CI run IDs refer to the
  original pre-rebase squashes; the final rebased branch is verified by CI run `41927992251`.
- `docs/development/agent-workflow.md` was changed to link to the audit policy on GitHub instead of the
  excluded `docs/planning/` tree. This keeps `zensical build --strict` green; it is a documentation-only
  policy-integration fix.
- Step 01 intentionally leaves `ModelDefinition` empty and templates minimal; Steps 03–10 extend them.
- `ProjectStore.SaveAsync` does not validate scientific content; validated editing is performed by
  `ProjectEditor`/`ProjectDocument`.
- Autosave scheduling is exposed by `ProjectDocument`; the UI timer belongs to Step 23.
- The JSON Schema currently describes only the Step 01 project shape.
- DocFX reports duplicate-member warnings but zero errors. Strict Zensical and rustdoc builds pass.
- No main merge or `step-01` tag has been created.
- Fixes applied on `feature/step-01-domain-kernel/fix-audit-findings`, squash-merged as `45d5a4b`. All
  blockers/majors and actionable minors are addressed; parent-directory fsync and the size-cap TOCTOU window
  remain accepted notes.
- `ProjectStore.SaveAsync` expects a validated project. Callers must check `ProjectLoadResult.IsSuccess` or run
  `ProjectValidator` before saving; a structural save-time guard remains a documented follow-up.
- Migration-cycle and non-monotonic `ToVersion` guards remain future hardening.
- The M3 OSMF EULA acceptance is recorded in both `NOTICE.md` and this audit request; it reflects the owner
  decision for free, non-revenue, test-only use.

## Findings

| Severity | Finding | Evidence | Resolution |
| --- | --- | --- | --- |
| **Blocker** | B1 — `LoadAsync` threw on malformed but structurally valid JSON (duplicate keys, missing camera/render box, null array elements, non-finite style values). | Audit adversarial checks against `samples/blank/blank.mocprj`. | Fixed: `LoadBytes` guards the full parse/migrate/deserialize/validate pipeline; validator reports null elements and non-finite values; regression matrix and structured fuzz test added. |
| **Blocker** | B2 — non-finite `ObjectStyle.Scale` loaded and crashed on save; save/autosave serialization could throw. | `scale: 1e400` reproduction. | Fixed: converter rejects non-finite scale; validator checks scale; save/autosave convert serialization failures to `PROJ030`/`PROJ033`; tests added. |
| **Major** | M1 — canonical JSON did not normalize `-0.0` in vector arrays. | Camera/render-box vectors produced different canonical bytes and hashes. | Fixed: canonical double-array converter and unit serialization normalization; canonical/hash equality tests added. |
| **Major** | M2 — schema and loader disagreed on required nested view fields and numeric enum strings. | Missing `objectStyles`, `projection`, `renderBox.visible`, and object-style defaults; `"projection": "1"` accepted. | Fixed: optional fields/defaults aligned in manifest, schema, loader, and docs; numeric enum strings rejected; schema/loader tests added. |
| **Major** | M3 — `JsonSchema.Net`/`JsonPointer.Net`/`Json.More.Net` carry OSMFEULA binary-release terms. | NuGet nuspec `requireLicenseAcceptance`/`OSMFEULA.txt`. | Accepted by owner for this free, non-revenue, test-only use; recorded in `NOTICE.md`; exact-version NuGet licence gate and vulnerability audit added to CI. |
| **Minor** | Blank README claimed the placeholder CLI validates projects. | `samples/blank/README.md`. | Fixed: README now states validation arrives with Step 12. |
| **Minor** | Hash documentation disagreed about dirty tracking. | `docs/reference/project-format.md`, `docs/development/design-decisions.md`. | Fixed: `ComputeDocument` is documented as the dirty-tracking hash; `Compute` is the model hash. |
| **Minor** | Root README and manifest still described Step 00/skeleton status. | `README.md`, `contracts/manifest.json`. | Fixed: status/notes now describe Step 01. |
| **Minor** | DocFX emitted duplicate-member warnings from copied dependency DLLs. | `tools/docfx/docfx.json`. | Fixed: explicit project assembly list; build now has zero warnings and enforces `--warningsAsErrors`. |
| **Minor** | Migration pipeline did not write the resulting `schemaVersion` back. | `ProjectMigrations.Apply`. | Fixed: migrated documents receive the new version; synthetic migration test added. |
| **Minor** | Audit request standard checklist was unchecked. | This file. | Fixed: standard checklist completed in this record. |
| **Minor** | Duplicate IDs within one domain produced two diagnostics. | `ProjectValidator.CollectIds` + `AddDomain`. | Fixed: same-domain duplicates are reported once; regression test added. |
| **Minor** | `.gitignore` omitted optional `.mocprj.bak` files. | `.gitignore`. | Fixed: `*.mocprj.bak` added. |
| **Note** | Lock timeout could reclaim a live same-host lock. | `ProjectStore.IsStale`. | Fixed: live same-host processes are never reclaimed by timeout alone; regression test added. |
| **Note** | `AtomicProjectFileWriter` does not fsync the parent directory. | `AtomicProjectFileWriter`. | Accepted: target-file fsync and atomic rename are implemented; parent-directory fsync is a documented follow-up. |
| **Note** | `LoadFromPathAsync` has a size-cap TOCTOU window. | `ProjectStore.LoadFromPathAsync`. | Accepted: bounded by the configured cap and platform file-size checks; a streaming cap remains a follow-up. |
| **Minor** | Public `ProjectJson.Deserialize` overloads leaked `ArgumentNullException`/`ArgumentOutOfRangeException` for malformed project data. | Re-audit report. | Fixed: converter wraps malformed-data exceptions as `JsonException`; style-number conversion also throws `JsonException`; both overloads tested. |
| **Minor** | Re-audit evidence heading referred to the original audited commit while listing fix-branch evidence. | Re-audit report. | Fixed: evidence split into original audit at `1d1ad57` (147 tests) and re-audit at `45d5a4b` (169 tests), with their respective CI runs. |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. The auditor fills this section only.

## Verdict

- [ ] Passed
- [ ] Passed with notes
- [x] Fixes required
- [ ] Re-audit required
- [ ] Waived

Follow-up owner: Step 01 agent
Follow-up issue/step: re-audit at `05c41ab`
