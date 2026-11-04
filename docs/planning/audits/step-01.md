# Step 01 Audit Request

## Request

| Field | Value |
| --- | --- |
| Step | 01 — Domain Kernel and Project Format |
| Request owner | Step agent |
| Audit status | **Ready for audit** |
| Audited commit | `1d1ad57` |
| Step branch | `step/01-domain-kernel` |
| Step tag | — |
| CI run | [41915254710 — green](https://github.com/seb-eis/mocassin-2/actions/runs/41915254710) |
| Audit date | 2026-11-04 |
| Auditor | Pending fresh read-only audit context |

Step page: [`Step 01 — Domain Kernel and Project Format`](../steps/step-01-domain-kernel-and-project-format.md)

The step branch is frozen at `1d1ad57` for audit. The commit that sets this request to **Ready for audit** is
documentation-only and carries `[skip ci]`; it does not change code or the audited commit.

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

- [ ] Required tests from the step page pass.
- [ ] Public/protected XML documentation policy is followed.
- [ ] Documentation, status dashboard, feature log, and parity matrix are current.
- [ ] CI is green at the audited commit.
- [ ] Format/version/fixture changes are documented and tested.
- [ ] Security, dependency, licence, and secret-scan gates pass.
- [ ] No unrelated changes or unapproved scope.
- [ ] Branch, tag, and status hygiene are correct.

## Evidence

Commands run on the rebased audited commit (`1d1ad57`):

```text
dotnet test Mocassin.slnx -c Release ................ pass (147 total)
dotnet format Mocassin.slnx --verify-no-changes ..... pass
zensical build --strict ............................. pass
tools/docs/build-docs.sh ............................ pass (0 errors; DocFX duplicate-member warnings)
python tools/contracts/generate.py --check .......... current (17 files)
```

CI: [41915254710](https://github.com/seb-eis/mocassin-2/actions/runs/41915254710) — green on Ubuntu + Windows.

## Known notes for the auditor

- Step 01 was rebased onto `main` after the independent audit policy landed. The audited commit contains the
  rebased feature commits plus the audit-structure integration.
- Feature squash hashes in the step page are the rebased hashes. Their per-feature CI run IDs refer to the
  original pre-rebase squashes; the final rebased branch is verified by CI run `41915254710`.
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

## Findings

| Severity | Finding | Evidence | Resolution |
| --- | --- | --- | --- |
| — | — | — | — |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. The auditor fills this section only.

## Verdict

- [ ] Passed
- [ ] Passed with notes
- [ ] Fixes required
- [ ] Re-audit required
- [ ] Waived

Follow-up owner:
Follow-up issue/step:
