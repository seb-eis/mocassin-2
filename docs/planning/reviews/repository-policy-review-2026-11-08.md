# Repository Policy Review — 2026-11-08

## Metadata

| Field | Value |
| --- | --- |
| Review type | Whole-repository agent, workflow, and policy review |
| Reviewer | Independent out-of-context policy reviewer (fresh context) |
| Review date | 2026-11-08 |
| Reviewed commit | `29dc6ed06ce0593017af31b9096be039ca867a2e` (`29dc6ed`, `origin/main` at review start) |
| Branch | `docs/repository-policy-review` |
| Status | Complete — read-only review; no repository change except this record |
| Verdict | **Maintenance required** |

### Independence statement

This review was performed in a fresh context with no shared conversation with the planner or any implementor.
I did not author or implement any of the reviewed policies, workflows, scripts, or code. The repository at
`29dc6ed` was treated as the source of truth; this prompt was used only for scope, not as evidence.

## Scope

Reviewed the actual files, including:

- Root: `AGENTS.md`, `CONTRIBUTING.md`, `README.md`, `.editorconfig`, `Directory.Build.props`,
  `Directory.Packages.props`, `Mocassin.slnx`, `zensical.toml`, `global.json`.
- Workflows: `.github/workflows/ci.yml`, `docs.yml`, `benchmark.yml`, `nightly.yml`.
- Development docs: all `docs/development/*.md`, especially `agent-workflow.md`, `contributing.md`,
  `code-structure.md`, `index.md`, `architecture.md`, `testing-and-parity.md`, `compilation-pipeline.md`.
- Planning process: `docs/planning/reviews/README.md` and template; `docs/planning/audits/README.md` and
  template; `docs/planning/amendments/README.md` and template; `docs/planning/ci-policy.md`;
  `docs/planning/status.md`; Step 02 and Step 03 pages; Step 02 review/audit/amendment records; relevant ADRs.
- Tooling: `tools/verify.sh`, `tools/structure-policy.json`, `tools/check-api-compat.sh`,
  `tools/update-api-baseline.sh`, `tools/README.md`, `tools/contracts/`, `tools/docs/`, `tools/symmetry/`.
- Reference/published docs: `docs/index.md`, `docs/reference/file-layout.md`,
  `docs/reference/project-format.md`, `docs/glossary/notation.md`.
- Contract artifacts: `contracts/manifest.json`, `contracts/generated/schema/*.json`.

## Evidence

### Repository state

```text
git status --short
(clean before this record was created)

git rev-parse HEAD
29dc6ed06ce0593017af31b9096be039ca867a2e

git rev-parse origin/main
29dc6ed06ce0593017af31b9096be039ca867a2e

git log --oneline -3
29dc6ed chore: add code structure policy and align symmetry namespaces
3114d00 refactor: align Mocassin.Symmetry namespaces with folders
a3aab84 chore: add code structure policy and structure test

git tag --list
development-ready
planning-complete
step-00
step-01
step-02
```

### Commands run and results

| Check | Result |
| --- | --- |
| Parse all four workflow YAML files with `yaml.safe_load` | Pass; jobs parsed for `ci`, `docs`, `benchmark`, `nightly` |
| `bash -n` and `shellcheck -S warning` on `tools/*.sh`, `tools/docs/*.sh`, `tools/reference/*.sh` | Pass; no syntax errors, no shellcheck warnings |
| `./tools/verify.sh structure` | Pass; `Mocassin.Architecture.Tests` 1/1 |
| `zensical build --strict` | Pass; “No issues found” |
| `dotnet restore Mocassin.slnx`; `dotnet build Mocassin.slnx -c Release --no-restore` | Pass; 0 warnings, 0 errors |
| `dotnet format Mocassin.slnx --verify-no-changes --no-restore` on a pristine `git archive` of `29dc6ed` | Pass; exit 0. The review worktree’s pre-existing `obj` artifacts produced 15 false `IDE0005` reports; a clean archive built and formatted clean, matching CI. This is not a repository finding. |
| `./tools/check-api-compat.sh` | Pass; no breaking changes against the checked-in baselines |
| `python tools/contracts/generate.py --check` | Pass; 17 generated files current |
| `python tools/check-license-headers.py` | Pass; SPDX headers present in 128 source files |
| `python tools/check-no-secrets.py` | Pass; no secret patterns in 402 tracked text files |
| GitHub Actions API for `29dc6ed` | `CI` run `42482558290` completed/success at the commit |
| GitHub Actions API for `nightly.yml` | 3 scheduled runs recorded (2026-11-04/05/06), all completed/success |
| Full test suite | Not run; no finding required it |

### Documentation link check

A local Markdown link scan found only intentional template placeholders
(`docs/planning/{audits,reviews,amendments}/*` naming `step-NN-short-title.md`) and generated API paths
(`docs/api/index.md`). No broken planning-doc link was found.

## Findings

| ID | Severity | Category | Area | Finding | Evidence | Recommendation |
| --- | --- | --- | --- | --- | --- | --- |
| R-01 | **Major** | Stale / Inconsistency | `AGENTS.md` | The normative agent guide still says the project is “planning only”, that there is no production .NET solution or Rust workspace, and that the build/test commands are not yet valid. The repository contains a 20-project .NET solution, a Rust workspace, Step 02 code, and completed Step 02 audits. An agent reading the first section can conclude that implementation must not start. | `AGENTS.md:11-13` (“Current state: planning only… no production .NET solution or Rust workspace”); `AGENTS.md:576-577` (“commands become valid… Do not run them expecting success today”); actual solution in `Mocassin.slnx`; `src/rust/Cargo.toml`; `docs/planning/status.md:49`. | Replace the planning-only state with the current state (Step 02 complete, Step 03 next) and remove the “not valid today” caveat, or scope it only to genuinely future commands. |
| R-02 | **Major** | Stale | Status surfaces | Public and planning status surfaces still report Step 00/01. They disagree with the status dashboard, Step 02 page, and tags. | `README.md:7-9` (“Step 01 complete… Step 02 is the next unblocked step”); `docs/index.md:6-9` (“completed Step 00 and is closing out Step 01”); `docs/planning/README.md:120-122` (“Repository and toolchain Complete (Step 00)”, “.NET model builder Not started”); `docs/development/index.md:39` (only Step 00/01); actual state `docs/planning/status.md:49-50,82-84`. | Update all four surfaces in the Step 02 closeout/next maintenance change so they say Step 02 complete and Step 03 is the next step. |
| R-03 | **Major** | Inconsistency | CI policy / Nightly workflow | Policy and guides say Nightly is disabled and must stay disabled until real suites exist, but `nightly.yml` has an active weekly `schedule` trigger with a placeholder job, and GitHub has recorded scheduled runs. This is both a policy contradiction and an avoidable CI cost. | `.github/workflows/nightly.yml:3-9` (comment says disabled, but `schedule: cron "0 2 * * 1"` is present); `AGENTS.md:448,459` (“Nightly disabled”, “Keep Nightly disabled”); `docs/planning/ci-policy.md:29`; `docs/development/contributing.md:109`; `docs/planning/status.md:93-94` (“CI runs only at closeout and on main”); GitHub API: 3 scheduled successful runs. | Decide one policy: remove the schedule trigger/delete the placeholder until suites exist, or change the policy and docs to record an explicitly accepted weekly placeholder run. Do not leave both stories in place. |
| R-04 | **Major** | Stale / Inconsistency | CI documentation | Several policy/developer documents still describe the pre-minimisation CI matrix and triggers: Ubuntu + Windows on step-branch pushes and macOS on `main`. Current automatic CI is `main`-only Ubuntu, with one manual closeout dispatch and manual Windows/macOS gates. Related benchmark/command text is also stale. | `docs/planning/03-quality-strategy.md:359-360` (“ubuntu-latest and windows-latest on step branches; macOS on main…”); `docs/development/compilation-pipeline.md:65` (“CI repeats… on Linux and Windows”); `docs/planning/steps/step-00-repository-toolchain-and-ci.md:167-169,231` (step/* push CI task checked); `docs/planning/steps/step-02-symmetry-and-geometry.md:586`; `docs/planning/06-toolchain-requirements.md:118` (“CI covers 3.11–3.13”, actual 3.13 only); `docs/planning/03-quality-strategy.md:438` (“CI builds the combined site on every PR”); `docs/planning/ci-policy.md:28` (benchmark “once the project lands”, but `Mocassin.Benchmarks` exists); `.github/workflows/benchmark.yml:3-5` (“project lands with a later step; workflow will fail until…”); `docs/planning/ci-policy.md:82,88` (`--ref step/02-symmetry-and-geometry`, merged/deleted); `.github/workflows/ci.yml:315-316` (symmetry generator still described as landing with Step 02). | Align every CI matrix/trigger statement with `AGENTS.md` §3.13 and the actual workflows; update benchmark, Python, and branch-example text; remove stale workflow comments. |
| R-05 | **Major** | Inconsistency | Local quality gate | `tools/verify.sh all` is presented as the closeout gate and “mirrors the affected CI jobs”, but it does not run the public-API compatibility check, NuGet licence policy, or vulnerability audit, all of which CI enforces. An agent can pass `verify.sh all` and still fail the closeout CI, especially when Step 03 adds public API. | `tools/verify.sh:4-6,60-76,80-86` (no `check-api-compat.sh`, no `check_nuget_licenses.py`, no vulnerability audit); `AGENTS.md:159` (closeout requires `./tools/verify.sh all`); `.github/workflows/ci.yml:356-398` (api-compat job runs ApiCompat, NuGet licence policy, vulnerability audit); `CONTRIBUTING.md:49-54` and `docs/development/contributing.md:76-80` list API compatibility separately, but not as part of `verify.sh all`. | Either add API compatibility, NuGet licence, and vulnerability checks to `verify.sh all`/a new mode, or narrow the “mirrors CI”/closeout claim and add the missing commands to the AGENTS/CONTRIBUTING gate lists. |
| R-06 | **Major** | Stale | Step 03 audit readiness | The pre-seeded Step 03 audit record does not match the current audit template. It lacks the closeout-CI field, the platform-CI field, the known-notes section, and the code-structure checklist item, yet it still has a `Resolution` column that the reviewer must not edit. Step 03 is the next step, so this is the record an auditor would start from. | `docs/planning/audits/step-03.md:10-13,37-46,58-60`; current template `docs/planning/audits/step-audit-template.md:21-22,48-55,65-72`; policy requires code structure and platform status `docs/planning/audits/README.md:51-59,95-105`. | Regenerate the 29 unused pre-seeded audit records (Steps 03–31) from the current template before Step 03 starts, or replace pre-seeding with on-demand creation. Keep the reviewer-only fields separate from the step-agent resolution section. |
| R-07 | **Major** | Inconsistency | Step 03 design vs project contract | Four deferred Step 02 review items (`m9`) are still unresolved in the Step 03 design and the persisted contract: (a) `particleIds` order is semantic but is not in the contract’s semantic-array list; (b) the contract stores `"stable": true` while Step 03 uses a `SiteStability` enum; (c) `CellDummyPosition.Id` is a raw `Guid` while the project convention is typed IDs; (d) the preview rejects `A*B*C*P` above the index cap but `StructureModel` has no supercell dimensions. `STR003` also says “outside 0..180°” although the contract requires strictly `0 < angle < 180`. | `docs/planning/02-data-contracts.md:14-19,187-197`; `docs/planning/steps/step-03-particle-and-structure-models.md:90-107,141,157-159`; `docs/glossary/notation.md:10`; `AGENTS.md:700`; deferred finding `docs/planning/reviews/step-02.md:162,213` (m9). | Resolve these in the Step 03 design review and record the chosen JSON/ID semantics before implementation. Add the `STR003` range correction to the Step 03 diagnostic table. |
| R-08 | **Major** | Inconsistency | Step 03 contract/schema readiness | The data contract and published reference already promise a project `structure` section and an optional envelope `symmetry` field, but `contracts/manifest.json`/`generated/schema/project-v1.schema.json` contain neither; the generated schema rejects unknown properties. Step 03’s task list does not include updating the manifest/schema. | `docs/planning/02-data-contracts.md:213-216` (“generated schema/manifest marks it reserved”); `docs/reference/project-format.md:94-95,118-120` (“structure section ships with Step 03”; optional `symmetry`); `contracts/generated/schema/project-v1.schema.json:7` (`additionalProperties: false`); `grep` shows no `spaceGroup`/`symmetry` in the generated project schema; Step 03 tasks `docs/planning/steps/step-03-particle-and-structure-models.md:232-243` omit manifest/schema work. | Add a Step 03 task and acceptance item for updating `contracts/manifest.json`, regenerating the project schema, and adding fixtures with and without `symmetry`. State explicitly whether the project `schemaVersion` stays 1. |
| R-09 | **Major** | Inconsistency | Step 03 public-API gate | Step 03 adds many public Core/Symmetry types, but its task list and acceptance criteria do not mention `PublicAPI.Shipped.txt`/`PublicAPI.Unshipped.txt` or the checked-in ApiCompat baseline. The build uses `PublicApiAnalyzers` and ADR 0013 requires the baseline; a Step 03 implementation following the page alone can fail the build or leave the API gate stale. | `docs/planning/steps/step-03-particle-and-structure-models.md:230-243,245-254`; `src/dotnet/Mocassin.Core/Mocassin.Core.csproj:10-12` and every other library project; `docs/planning/adr/0013-format-compatibility-and-api-versioning.md:45-46`; `AGENTS.md:610-619,912-930` omit the API baseline/PublicAPI files from the general gates. | Add an explicit Step 03 task and acceptance item for XML docs, `PublicAPI.Unshipped.txt`, and refreshing the ApiCompat baseline after intentional public API changes; add API compatibility to the general definition of done. |
| R-10 | Minor | Stale | Amendment records | Both Step 02 amendment pages still say the audit verdict is `pending` and the merge commit is `—`, while the amendment index and page headers say `Integrated` and Step 02 is merged/tagged. | `docs/planning/amendments/step-02-amend-1-public-math.md:333-335`; `docs/planning/amendments/step-02-amend-2-docs-math.md:278-280`; `docs/planning/amendments/README.md:110-111`; `docs/planning/status.md:49`. | Update the amendment audit sections to the passed parent audit and the actual merge/tag, or mark the section historical with the date. |
| R-11 | Minor | Inconsistency | Status vocabularies | The review and audit status tables do not include the values actually used by the dashboard and records (`Ready with notes`, `Passed with notes`), while their verdict lists include state transitions (`Re-review required`, `Re-audit required`). The audit template also labels its spawner field “Review initiated by” instead of “Audit initiated by”. | `docs/planning/reviews/README.md:40-50,64-70`; `docs/planning/audits/README.md:35-45,60-70`; `docs/planning/status.md:47-49`; `docs/planning/audits/step-audit-template.md:16`; Step 02 records use `Passed with notes` / `Ready with notes`. | Define one status vocabulary and one verdict vocabulary, use them in tables, records, and the dashboard, and correct the audit template field label. |
| R-12 | Minor | Inconsistency | Architecture diagrams | The development architecture diagrams draw `Core --> Symmetry --> Compilation --> Contracts`, implying Core depends on Symmetry, while the normative AGENTS target uses `Core <- Symmetry <- Compilation <- Contracts` and the projects actually have Symmetry -> Core. The arrow legend is missing. | `docs/development/architecture.md:8-17`; `docs/development/index.md:9-18`; `AGENTS.md:531-538`; actual references in `src/dotnet/Mocassin.Symmetry/Mocassin.Symmetry.csproj` and `Mocassin.Compilation.csproj`. | Reverse the diagram arrows or add an explicit “arrow points to dependent” legend and verify both architecture pages against the project references. |
| R-13 | Minor | Inconsistency | Code-structure policy | AGENTS’ forbidden-folder list omits `Utilities/`, which the structure policy JSON and code-structure page forbid. AGENTS’ root-file list omits `Usings.cs` and `README.md` that the full policy permits. | `AGENTS.md:679-683`; `docs/development/code-structure.md:40-48,74-83`; `tools/structure-policy.json:31-33`. | Make AGENTS a short summary that points to the single policy file, or align the list exactly. |
| R-14 | Minor | Stale | Tooling and navigation indexes | `tools/README.md` only lists directories; it omits `verify.sh`, `structure-policy.json`, `check-api-compat.sh`, `update-api-baseline.sh`, and `symmetry/`. Step 03’s documentation plan adds `docs/user-guide/particles.md` and `structure.md`, but `zensical.toml` has no navigation entries for them. | `tools/README.md:5-11`; `docs/planning/steps/step-03-particle-and-structure-models.md:260-261`; `zensical.toml:28` (user-guide only) and `zensical.toml:39-48` (development nav). | Add the missing tool entries and make a documentation-nav update part of the Step 03 task list (or generate nav from the docs tree). |
| R-15 | Note | Stale / historical | Step 02 review record | The durable Step 02 review record says its findings were transcribed verbatim from `.tmp/review/*` handoffs that were deleted, and lists those deleted paths as reports. Current policy says a reviewer writes directly into the durable record and no handoff/scribe is used. The record is historical and must not be rewritten, but it is a misleading precedent for new agents. | `docs/planning/reviews/step-02.md:3-6,19,31-32,118,247,318`; current rule `AGENTS.md:276-277`; `docs/planning/reviews/README.md:25`; `docs/planning/audits/README.md:20`. | Add a short note to the reviews README that pre-policy records may reference retired `.tmp` handoffs; leave the historical record text unchanged. |

## Clean areas

The following areas were checked and are consistent; they are not findings.

- **Branch, merge, squash, tag, and approval rules.** `AGENTS.md` §3.1–§3.7, `CONTRIBUTING.md` §2,
  `docs/development/contributing.md` §1–§3, and `docs/development/agent-workflow.md` §9/§15 agree on
  `main`/step/feature branches, squash merges, `--no-ff` step merges, annotated tags, and explicit approval.
- **Reviewer/auditor independence and write scope.** `AGENTS.md` §3.8–§3.12, the reviews and audits READMEs,
  and the amendment process agree that a fresh context reviews, writes only the record, and does not fix,
  merge, tag, or push `main`.
- **Code-structure limits and enforcement.** The 20-file folder limit, depth 4, 800-line limit, 20-file test
  root, forbidden folders, and allowed suffixes match `tools/structure-policy.json` and
  `Mocassin.Architecture.Tests`; `./tools/verify.sh structure` passes.
- **Toolchain pins.** `global.json` (10.0.403), `README.md`, `docs/opencode/mocassin.containerfile`,
  `tools/docs/requirements.txt` (Zensical 0.0.74), `.config/dotnet-tools.json` (DocFX 2.81.0), CI
  (`cargo-deny` 0.20.2, Rust 1.99.1), and spglib 2.8.0 provenance agree.
- **Workflow validity and basic gates.** All four workflow YAML files parse; all shell scripts pass
  `bash -n`/`shellcheck`; strict Zensical, ApiCompat, contract drift, licence-header, and secret-scan checks
  pass at the frozen commit.
- **Step 02 records.** The design-review and audit rounds are append-only, retain earlier rounds, and their
  final verdicts (`Ready with notes`, `Passed with notes`) match the Step 02 page and status dashboard; the
  only stale part is the amendment audit section (R-10).
- **Step 03 dependency/status.** Step 03 depends on completed Step 02, is marked `Not started`, and has no
  design-review or audit request yet, which is consistent with the process; the gaps are the stale audit
  template and the unresolved design/contract/API tasks (R-06–R-09), not the dependency state.
- **Documentation publication boundary.** `docs/planning/` is excluded from Zensical, and the published docs
  build strictly.

## Simplification opportunities

These are not all defects, but they are concrete opportunities to reduce duplicated rules and future drift.

1. **One normative process document.** Workflow rules are duplicated in `AGENTS.md` §3.8–§3.13,
   `docs/planning/reviews/README.md`, `docs/planning/audits/README.md`,
   `docs/planning/amendments/README.md`, and `docs/development/agent-workflow.md` §13–§17. The duplication has
   already produced drift (R-04, R-11). Keep `AGENTS.md` as a short normative summary and move detailed
   sequence/status text into one process document that the others link to.
2. **Stop pre-seeding 32 audit records.** The pre-seeded files rot faster than they are used (R-06). Create an
   audit record from the current template when the step reaches `Ready for audit`, as the review process already
   does, or regenerate all unused files from the template as a generated check.
3. **Remove or explicitly accept placeholder workflows.** `nightly.yml` currently spends scheduled runs on a
   placeholder (R-03), and `benchmark.yml` contains false “project will not exist yet” comments (R-04). Keep
   manual benchmark/workflow dispatch, but remove the nightly schedule until real suites exist.
4. **Consolidate contributor guidance.** `CONTRIBUTING.md`, `docs/development/contributing.md`,
   `AGENTS.md` §3/§6/§16, and `docs/development/agent-workflow.md` repeat branch, gate, and evidence rules.
   A single “how to contribute” page plus short links would reduce the number of places that can disagree.
5. **Share the API project list.** `tools/check-api-compat.sh` and `tools/update-api-baseline.sh` duplicate
   the list of public assemblies. Derive the list from the solution or a small shared file so a new library
   cannot be omitted from one side.
6. **Unify status names.** Reviews, audits, amendments, and the dashboard use overlapping status/verdict names
   (R-11). Use one enum for “state” and one for “verdict”, and render the dashboard from the record metadata
   where practical.
7. **Generate or check CI documentation.** The CI matrix is restated in AGENTS, `ci-policy.md`,
   `contributing.md`, `03-quality-strategy.md`, and step pages. One table in `ci-policy.md` with a consistency
   check (or a generated excerpt) would prevent R-04.
8. **Drop unused path-filter entries.** `ci.yml` filters `tools/ci/*`, but no such directory exists. Remove it
   or add the directory/contents it is meant to cover.

## Verdict

**Maintenance required.**

The core branch model, review/audit separation, code-structure enforcement, and toolchain pins are sound, and
the repository passes the implemented structural, documentation, contract, API-compatibility, licence, and
secret checks at `29dc6ed`. However, the repository is not ready to hand to a Step 03 implementor without
cleanup:

- The normative `AGENTS.md` entry state is factually wrong (R-01), and the main status surfaces still point at
  Step 01 (R-02).
- The CI policy says Nightly is disabled while scheduled placeholder runs exist (R-03), and several policy
  pages still describe the retired step-branch CI matrix (R-04).
- The local closeout gate does not cover the API and dependency checks that CI enforces (R-05).
- Step 03’s audit record is stale (R-06), its design still contains unresolved contract contradictions (R-07),
  the generated project schema does not contain the promised structure/symmetry fields (R-08), and the step
  omits the public-API baseline task (R-09).

These are process and documentation maintenance issues, not evidence of a scientific or architectural defect.
They should be closed before the Step 03 design review starts, because several of them define the inputs to
that review.

## Prioritized cleanup list

1. **Before any agent starts Step 03:** update `AGENTS.md` §1/§6 and the four stale status surfaces
   (R-01, R-02).
2. **Before the Step 03 design review:** resolve the four deferred contract items and the `STR003` wording
   (R-07), add the manifest/schema update (R-08), and add the PublicAPI/ApiCompat tasks (R-09).
3. **Before the Step 03 audit request:** regenerate the Step 03 audit record from the current template (R-06).
4. **Before the next closeout:** decide the Nightly policy and remove or accept the scheduled placeholder
   (R-03); align the CI documentation and benchmark text (R-04).
5. **Before the next feature approval:** add the missing API/NuGet/vulnerability checks to the local gate or
   narrow its claim (R-05).
6. **Maintenance:** update the Step 02 amendment audit sections (R-10), unify status vocabularies (R-11),
   correct architecture arrows (R-12), align the code-structure list (R-13), and refresh tooling/nav indexes
   (R-14).
7. **Optional:** add the historical-record note for the retired `.tmp` handoffs (R-15), and pursue the
   simplification list above.

## Resolution status

Date: 2026-11-08
Owner: Implementor / maintenance update
Maintenance commit: `6f5b06a` (follow-up resolution record commit follows)

The reviewer verdict above is retained unchanged. This section records the follow-up maintenance update.

| Finding | Status | Resolution |
| --- | --- | --- |
| R-01 | Fixed | `AGENTS.md` now states that Steps 00–02 are complete, Step 03 is next, and the build/test commands are current. |
| R-02 | Fixed | `README.md`, `docs/index.md`, `docs/planning/README.md`, and `docs/development/index.md` now report Step 02 complete and Step 03 next. |
| R-03 | Fixed | `nightly.yml` is a manual placeholder with no schedule; CI policy, AGENTS, and contributing docs agree. |
| R-04 | Fixed | CI matrix/trigger text, benchmark comments, branch examples, Python coverage, and the Step 00 historical note were aligned with the current policy. |
| R-05 | Fixed | `tools/verify.sh api` runs the NuGet licence policy, vulnerability audit, and ApiCompat; `all` includes it. |
| R-06 | Fixed | Audit records 03–31 were regenerated from the current template; the audit README requires regeneration when the template changes. |
| R-07 | Addressed in Step 03 design | `particleIds` is marked semantic; the contract uses `stability` strings; `CellDummyPositionId` is typed; the preview takes cell counts; `STR003` uses the open angle range. The Step 03 design review should confirm the final choices. |
| R-08 | Step 03 task added | Step 03 now owns the manifest/schema update and fixtures; the reference docs no longer claim the generated schema already contains the fields. The actual schema update remains a Step 03 deliverable. |
| R-09 | Fixed | Step 03 tasks/acceptance and the general definition of done now require XML docs, `PublicAPI.Unshipped.txt`, and the ApiCompat baseline. |
| R-10 | Fixed | Amendment audit sections and the amendment index now record the passed parent audit, merge, tag, and post-development maintenance. |
| R-11 | Fixed | Review/audit status tables include `Ready with notes` and `Passed with notes`; state transitions are separated from verdicts; the audit template uses `Audit initiated by`. |
| R-12 | Fixed | Architecture diagrams now point to dependencies and include an arrow legend. |
| R-13 | Fixed | AGENTS now includes `Utilities/`, `Usings.cs`, and `README.md`, matching the full structure policy. |
| R-14 | Fixed | `tools/README.md` lists the new scripts and `symmetry/`; Step 03 owns the navigation update for the new user-guide pages. |
| R-15 | Fixed | The reviews README records that pre-policy `.tmp/review` references are historical and not to be rewritten. |

Simplification opportunities: the highest-value items were adopted in this cleanup (CI documentation alignment,
audit-record regeneration, local API gate, status-surface consolidation). The remaining suggestions — one
normative process document, on-demand audit creation, a shared API project list, and generated CI docs — are
recorded as follow-up opportunities, not blockers for Step 03.
