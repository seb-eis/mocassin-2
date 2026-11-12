# XC-03 — Documentation Status Review

> Reviewer: write evidence, findings, and verdict directly into this record. You may edit only your own
> metadata, checklist completion marks and inline verification notes (not item text), `Evidence`, `Findings`,
> and `Verdict`, and commit only this file with `[skip ci]`. Do not edit the scope, checklist item text,
> resolutions, code, tests, configuration, step pages, status dashboard, or any other file.

## Request

| Field | Value |
| --- | --- |
| Review ID | XC-03 |
| Checkpoint | Phase B — Model definition (Steps 03–07) |
| Type | Repository-wide documentation status review |
| Target commit | `81fba457d6d4714b4fe56d9c7084a738815b365e` (Phase B checkpoint: drift remediation and record; parent `38a22d2`) |
| Request owner | Step agent / planner |
| Orchestrator (implementor) | OpenCode Phase B orchestrating agent |
| Review initiated by | Human owner |
| Reviewer | Fresh independent documentation reviewer (OpenCode subagent; read-only) |
| Review date | 2026-11-12 |

> Orchestration: an implementor agent requests the reviewer, fixes findings on a maintenance branch, adds
> resolutions without altering reviewer text, and requests re-review until the reviewer no longer identifies
> issues. The human owner approves the final verdict and the phase gate.

## Purpose

Verify that the published and developer documentation is complete, accurate, navigable, and consistent with
the completed phase. For the Phase B checkpoint, this covers the implemented Steps 00–07.

This is a documentation review, not a repository-policy audit or a design-correctness review.

## Non-goals

- Re-reviewing repository policy or planning records (XC-01).
- Re-reviewing the scientific/design correctness of the model (XC-02).
- Writing new pages or fixing stale text; the reviewer reports only.
- Auditing future Steps 08–31 pages that are explicitly placeholders.

## Scope

- Published docs: `docs/index.md`, `getting-started/`, `user-guide/`, `physics/`, `tutorials/`, `reference/`,
  `development/`, `performance/`, `troubleshooting/`, `migration/`, `examples/`, `glossary/`, `api/`.
- `zensical.toml` navigation, exclude rules, and docs build configuration.
- `tools/docs/` build scripts and requirements.
- Generated API documentation: DocFX, rustdoc, and mkdocstrings landing pages.
- Step pages 00–07 **Documentation** sections and delivered pages.
- Developer docs that are part of the published site: architecture, compilation pipeline, testing, agent
  workflow, contributing.
- Diagrams, images, screenshots, KaTeX assets, and external links.
- The documentation status reflected in the status dashboard and step pages.

## Evidence

### Repository state

- Reviewed commit `81fba457d6d4714b4fe56d9c7084a738815b365e` (parent `38a22d2`; subject
  `docs: record phase B drift check and fix stale status [skip ci]`), with `step-07` at `3ed558a`.
- Worktree HEAD is the request commit `2fe7b9180b0768147e9f6d81f29aa7f5bcef0aa1`. Its diff against the reviewed
  commit touches only the three XC request records, the cross-cutting README, and one `status.md` line; no
  published-doc content changed.
- `git status --short` was empty before this record was written.

### Files reviewed

- Rules and process: `AGENTS.md` §§1, 3.8, 3.9, 3.10, 3.14, 9, 11, 12, 13, 14;
  `docs/planning/reviews/cross-cutting/README.md`; `docs/planning/audits/README.md`.
- Published pages: `docs/index.md`; all of `getting-started/`, `user-guide/` (7 pages), `physics/` (6 pages),
  `tutorials/`, `reference/` (8 pages), `development/` (11 pages), `performance/`, `troubleshooting/` (2 pages),
  `migration/`, `examples/`, `glossary/` (2 pages), and `api/` (4 pages); `docs/javascripts/katex.js`.
- Configuration and tooling: `zensical.toml`; `tools/docs/{build-docs.sh,build-docs.ps1,serve.sh,serve.ps1,README.md,requirements.txt}`;
  `tools/check_docs_links.py`; `tools/docfx/docfx.json`; `.github/workflows/docs.yml`; `Directory.Build.props`;
  `src/rust/Cargo.toml`.
- Generated documentation: DocFX output in `artifacts/docfx/` and `site/api/dotnet/`; rustdoc output in
  `target/doc/` and `site/api/rust/`; Python placeholder in `site/api/python/`; `site/search.json`;
  `site/sitemap.xml`.
- Step evidence: `docs/planning/steps/step-00…07` (Documentation sections and acceptance criteria),
  `docs/planning/status.md`, `docs/planning/appendix-b-parity-matrix.md` (documentation rows),
  `docs/planning/drift-checks/2026-11-12-phase-b.md` (D2, D13).
- Formats and samples: `contracts/manifest.json`, `contracts/generated/schema/project-v1.schema.json`,
  `contracts/generated/schema/validation-v1.schema.json`, and `samples/{blank,ceria,barium-zirconate,lattice,simulations,transitions}`.
- Code cross-checks: `CoreDiagnosticCatalog.cs`, `SymmetryDiagnosticCatalog.cs`, `Step07Descriptors.cs`,
  `DiagnosticDocumentationTests.cs`, `ParticleModel.cs`, `StructureModel.cs`, `EnergyLimits.cs`,
  `Simulation.cs`, `SimulationModelValidator.cs`, `GeometryTolerances.cs`, `SymmetryContract.cs`.

### Commands and results

| Command | Result |
| --- | --- |
| `git status --short`; `git log --oneline --decorate -15` | Clean; HEAD `2fe7b91`, reviewed `81fba45`, `main`/`origin/main` `38a22d2`, `tag: step-07` `3ed558a`. |
| `zensical build --strict` | Exit 0; `No issues found`; built site has no `planning/` directory and a 45-URL `sitemap.xml`. |
| `./tools/docs/build-docs.sh` (before a Release build) | Exit 255: DocFX metadata found no `Mocassin.*.dll` Release inputs; `warning: No .NET API project detected`; `--warningsAsErrors` failed the build. Details in DOC-08. |
| `dotnet build Mocassin.slnx -c Release` | Exit 0; 0 warnings, 0 errors. |
| `./tools/docs/build-docs.sh` (after Release build) | Exit 0; Zensical, DocFX, and rustdoc stages completed; `Documentation built in …/site`. |
| `./tools/verify.sh docs --require` | Exit 0; `Documentation links OK (148 files scanned)`; `Closeout gate completed with required checks enabled.` |
| `python tools/check_docs_links.py` | Exit 0; `Documentation links OK (148 files scanned).` |
| `dotnet run --project src/dotnet/Mocassin.Cli -c Release -- validate samples/ceria/ceria.mocprj` | Exit 0; 0 errors, 0 warnings; `Result: valid`. |
| Same command with `--json --stats` | Exit 0; valid `validation-v1` report with non-null statistics; matches `docs/reference/cli.md`. |
| `python -m pytest src/python/tests -q` | 3 passed. |
| `cargo test --manifest-path src/rust/Cargo.toml --workspace -q` | Exit 0 (workspace currently has no tests). |
| `python tools/check-license-headers.py` | `SPDX headers present in 321 source files`. |
| `python tools/check-no-secrets.py` | `No secret patterns found in 661 tracked text files`. |
| `python tools/check_process_consistency.py` | `Process consistency: OK`. |
| Navigation audit (TOML nav parsed against `docs/**/*.md`, excluding `planning/`) | 45 published Markdown files; 45 nav entries (43 `.md` plus 2 generated API `.html`); no nav target missing on disk; only `api/dotnet/index.md` and `api/rust/index.md` are outside nav. |
| Generated API checks | `site/api/dotnet/api/Mocassin.Core.CoreModule.html`, `site/api/rust/mocassin_types/index.html`, and `site/api/python/index.html` all exist after the combined build. A Zensical-only rebuild exits 0 but leaves the two generated API targets absent (DOC-06). |
| Placeholder/stale-text greps | No `TODO`, `FIXME`, “not implemented”, or “coming soon” in published pages. Future-tense Step 07 references found in `reference/energy.md`, `user-guide/energy.md`, `user-guide/lattice.md`, `reference/transitions.md`, `reference/project-format.md`, `glossary/notation.md`, `user-guide/simulations.md`, and `user-guide/index.md` (DOC-04). |
| JSON field audit | Every quoted key in published JSON examples exists in `contracts/manifest.json` (the `validation-v1` report keys are in `reports.validation`, not `formats`). The envelope example itself is not schema-valid (DOC-03). |
| Diagnostics audit (scripted comparison with `CoreDiagnosticCatalog`, `SymmetryDiagnosticCatalog`, `Step07Descriptors`) | 140 codes on the page and 140 in the composed catalog; no missing/extra codes; all titles/severities/owners match except the truncated `SIM012` title (DOC-05). |
| Heading/notation audit | No skipped heading levels; KaTeX math uses `$$…$$` and `\(…\)` only; no single-dollar math outside code; 4 Mermaid/diagram blocks. |
| Search audit | `site/search.json` contains 452 entries and covers every published page; no `planning/` entry. |
| External-link spot check | `unpkg.com/katex@0.19.1` assets return 200; `mocassin.dev` schema host returned 000; unauthenticated `github.com/seb-eis/mocassin-2` returned 404 (private repository). |
| Drift-check D2/D13 verification | `README.md`, `docs/index.md`, and `docs/development/index.md` now say Steps 00–07 complete / Step 08 next; `zensical.toml` now contains `agent-security`, `drift-checks`, and `incident-and-rollback`; D2 and the navigable part of D13 are resolved. The two `api/*/index.md` placeholders remain intentionally unlisted. |

### Not run, and why

- Full `./tools/verify.sh all --require` was not run; this is a documentation review, and the docs, .NET build,
  Python, Rust test, licence, secret, and process subsets were run directly.
- Windows/macOS documentation builds were not run; `.github/workflows/docs.yml` runs the bash script on Ubuntu.
- Dark/light theme and mobile layout were not checked in a browser. There are no raster images or screenshots in
  the published pages, so image alt-text checks are not applicable.
- `mkdocstrings`/Python API rendering is not applicable at Phase B: `docs/api/python/index.md` is an explicit
  placeholder replaced by Step 22.
- External links to the private GitHub repository cannot be verified without credentials; the 404s above are an
  access effect, not proof of a missing target.

## Checklist

### A. Coverage

- [x] Every implemented feature in Steps 00–07 has a published or developer page. — Step 00–02 developer/reference pages and the Step 03–07 user-guide, physics, reference, and troubleshooting pages cover the implemented surface; no feature page is missing.
- [x] The user guide covers particles, particle sets, structure, transitions, energy, lattice/doping, and simulations at the implemented level. — All seven named pages exist and include the implemented fields, constraints, JSON examples, diagnostics, and editor operations.
- [x] Physics pages match the implemented symmetry, transition, energy, and simulation semantics. — Symmetry, energies, Monte Carlo, KMC, and MMC were cross-checked against the code constants and Step pages; solver-only behaviour is explicitly deferred.
- [x] Reference pages match the current project format and schema. — All JSON keys match `contracts/manifest.json`; one envelope example is not schema-valid (DOC-03), but the field documentation is current.
- [ ] Getting-started and tutorial pages work for the implemented surface. — Getting-started commands pass, but no tutorial page exists beyond the index; the index advertises two tutorials as available after completed steps (DOC-01).
- [x] Developer docs cover architecture, project format, testing, compilation boundary, and agent workflow. — All are present and linked; the architecture namespace table still uses pre-Step-04 wording (DOC-04).
- [x] No page claims a future feature is implemented. — Manual reading and grep found only explicit placeholders and “Step N”/“later phase” statements; no future capability is presented as delivered.

### B. Accuracy

- [x] Commands, paths, project names, versions, and settings match the current repository. — Tool versions match `global.json`, the tool manifest, and `requirements.txt`; the validate/build/test commands were executed successfully; docs-build ergonomics are DOC-08/DOC-09.
- [ ] JSON examples match the generated schema and contract field names. — Field names match, but the envelope’s `"viewSettings": {}` contradicts the schema and the page’s own required-fields rule (DOC-03).
- [x] Formulas, units, notation, and diagnostic codes are consistent with the model pages. — KaTeX/unit notation is consistent; 140/140 diagnostic codes, severities, owners, and titles match except the truncated `SIM012` title (DOC-05).
- [x] Screenshots and diagrams are current; no stale UI or old workflow. — There are no screenshots or raster images; the four Mermaid/ASCII diagrams describe the current phase and contain no stale UI.
- [x] Cross-links and anchors resolve. — `zensical build --strict` and `check_docs_links.py` both pass, including anchors.
- [x] No stale “Step 01/02” status, old CI matrix, or old package/namespace names remain in published pages. — The Step 01/02 status, CI matrix, and package names are current; the architecture table omits the new Step 04–06 namespaces and is recorded separately (DOC-04).

### C. Navigation and structure

- [x] `zensical.toml` navigation covers all published pages; no orphan pages. — 45 published Markdown files, 43 in nav plus 2 generated API targets; no unintended orphan remains, and the two unlisted API placeholders are recorded in DOC-06.
- [x] `docs/planning/` remains excluded from the published site. — No `planning/` path appears in `site/`, `site/search.json`, or `site/sitemap.xml`.
- [x] Heading hierarchy, code blocks, tables, and callouts are consistent. — Script check found no skipped heading levels; spot-checked pages use consistent fenced blocks, tables, admonitions, and Mermaid fences.
- [x] Images have alt text; dark/light theme and mobile layout are acceptable. — There are no images, so alt text is not applicable; the light/dark/system palette configuration is present, but no visual browser check was performed.
- [x] Search index includes the published pages. — `site/search.json` has 452 entries and covers every published page; no `planning/` entry.
- [ ] External links are appropriate; runtime CDN dependencies are identified. — The KaTeX unpkg runtime dependency works but is not identified in published pages (DOC-07); private-repository and schema-host links are noted in DOC-11.

### D. API documentation

- [x] DocFX builds with no missing public API docs. — DocFX metadata/build runs with `--warningsAsErrors` and exits 0; XML documentation is enforced by `CS1591` plus warnings-as-errors.
- [x] rustdoc builds with no missing public docs/warnings. — `cargo doc --workspace --no-deps` exits 0; the workspace denies missing docs.
- [ ] mkdocstrings renders the Python API correctly. — Not applicable at Phase B: `docs/api/python/index.md` is an explicit placeholder replaced by Step 22 (DOC-12).
- [ ] API landing pages link to the correct generated paths. — The combined build creates and links `api/dotnet/api/Mocassin.Core.CoreModule.html` and `api/rust/mocassin_types/index.html`; a Zensical-only build leaves both nav targets missing (DOC-06).
- [x] Public API examples and XML docs are current. — Public XML documentation is build-enforced for .NET and Rust; there are no hand-written API examples that could drift.

### E. Build and publication

- [x] `zensical build --strict` passes. — Exit 0, “No issues found”.
- [ ] Combined docs build (Zensical + DocFX + rustdoc) passes. — It passes after a Release .NET build; on a clean checkout it exits 255 because DocFX finds no Release assemblies (DOC-08). Bash/PowerShell warning policy also differs (DOC-09).
- [x] Licence/attribution and secret scans are clean. — 321 source files have SPDX headers; 661 tracked text files contain no secret patterns; `NOTICE.md` attribution is present.
- [x] The manual docs workflow or local build is documented and current. — `docs/development/contributing.md` and `.github/workflows/docs.yml` agree on the manual Documentation workflow; `tools/docs/README.md` lists the scripts.
- [ ] Packaged/offline docs assumptions are recorded, including KaTeX/CDN assets. — The published docs do not identify the unpkg KaTeX dependency or the offline deferral recorded in the Step 30 plan (DOC-07).

### F. Documentation status and exit criteria

- [x] Step pages 00–07 document their delivered pages and mark “Documentation written”. — All eight Step pages have the checked documentation criterion and name pages that exist in the site.
- [ ] Status dashboard and parity matrix documentation entries are current. — `status.md` is current, but most documentation rows in the parity matrix still say `Planned` despite delivered pages and generated API output (DOC-02).
- [x] Known documentation gaps, placeholders, and follow-ups have owners and target steps. — Python API → Step 22; tutorials, full CLI/formats, standalone docs completion, and offline → Step 30; package/state/format pages → Steps 11/18/20; extension ABI → Step 19.
- [x] No Blocker or Major finding remains, or every exception is explicitly waived by the human owner. — No Blocker or Major finding was identified; only Minor/Note findings DOC-01…DOC-12 remain.
- [x] Minor/Note findings have owners and follow-ups. — Every finding in `Findings` names an owner/target step; remediation status is left for the orchestrator, which owns that table.
- [ ] The human owner approves the verdict and remediation plan. — This is a human-owner action and is intentionally left unchecked.

## Findings

| ID | Severity | Area | Finding | Evidence | Recommendation |
| --- | --- | --- | --- | --- | --- |
| DOC-01 | Minor | Coverage | No tutorial pages exist beyond the index, although the index says two tutorials are available after completed steps and that every tutorial is executable and CI-validated. | `docs/tutorials/index.md:3-15` says “Each tutorial is executable … and is validated in CI” and marks “Create a blank project” available after Step 01 and “Build and validate the ceria model” after Steps 03–07; `docs/tutorials/` contains only `index.md`; the parity matrix plans tutorials for Step 30 (`appendix-b-parity-matrix.md:239`). | Add the two Phase B tutorials, or relabel them as Step 30 content and remove the current CI/availability claim. |
| DOC-02 | Minor | Status | The parity matrix documentation block still marks delivered pages and generated API references as `Planned`. | `docs/planning/appendix-b-parity-matrix.md:235-249`: “Docs skeleton + build/CI” is `Planned` for Step 00; “User guide + physics”, “Developer architecture guide”, “Contributing + dev workflow”, “Migration + clean-break guide”, “Glossary, notation, units”, and the .NET/Rust API rows are `Planned` for Step 30, although the pages and generated references already exist. `status.md` is current. | Mark delivered rows `Implemented` or `Partial` with the evidence step, and keep only genuinely missing content (tutorials, full CLI/formats, Python API, final publish) as `Planned`. |
| DOC-03 | Minor | Accuracy | The project-format envelope example is invalid under the generated schema and the page’s own required-fields rule. | `docs/reference/project-format.md:28` uses `"viewSettings": {}`; `:391-394` says `camera` and `renderBox` are required and missing ones are rejected; `contracts/generated/schema/project-v1.schema.json` has `viewSettings.required = ["camera","renderBox"]`. The seed at `:19` is not 64 hex characters, and the `$schema` URI at `:16` (`https://mocassin.dev/schemas/project-v1.json`) returned HTTP 000. | Use a schema-valid view-settings block (or label the example as a partial envelope), a valid/illustrative seed placeholder, and a resolvable schema URI. |
| DOC-04 | Minor | Accuracy | Published pages still describe Step 04–07 work in future tense, and the architecture namespace table is stale. | `docs/development/architecture.md:35` says later steps add transition, energy, lattice, and simulation sections although `src/dotnet/Mocassin.Core/Model/{Transitions,Energy,Lattice,Simulations}` exists; `docs/reference/energy.md:4`, `docs/user-guide/energy.md:79-80`, `docs/user-guide/lattice.md:78-79`, `docs/reference/transitions.md:124`, `docs/reference/project-format.md:448`, `docs/glossary/notation.md:149`, and `docs/user-guide/simulations.md:108` still defer content to Step 07; `docs/user-guide/index.md:28` lists validation/diagnostics as planned. | Rewrite these sentences in present tense, link the delivered diagnostics/troubleshooting pages, add the missing `ENM`/`LAT`/`TRN`/`SIM` prefixes to the glossary table, and update the architecture namespaces. |
| DOC-05 | Minor | Accuracy | The diagnostics reference does not fully match its own contract: `SIM012` has a truncated title, the promised path/hint detail is absent, and the intro overstates the automated drift check. | `docs/reference/diagnostics.md:76,419` end with “above ” (the source `CoreDiagnosticCatalog.cs:83` has the same truncation); the table has code/severity/owner/title/help only, while `docs/planning/steps/step-07-validation-and-diagnostics.md:524-525` promises “paths, hints”; `docs/reference/diagnostics.md:3-6` says the page is generated and drift-tested, but `DiagnosticDocumentationTests.cs:11-36` compares only the code set and headings. | Complete the `SIM012` title in the catalog and page; add path/hint columns or record the deviation; either generate the table or narrow the claim and extend the test to titles/severities. |
| DOC-06 | Minor | Navigation | The API navigation targets generated HTML that is absent in a Zensical-only build; the two API placeholder pages are unreachable. | `zensical.toml:81-82` targets `api/dotnet/api/Mocassin.Core.CoreModule.html` and `api/rust/mocassin_types/index.html`. A Zensical-only rebuild exits 0 but leaves both files absent while `site/api/dotnet/index.html` and `site/api/rust/index.html` placeholders exist; the combined build creates the targets. | Point the API nav at the placeholder index pages (which redirect after the combined build), or make the API nav conditional; record the two placeholders as intentionally unlisted if kept. |
| DOC-07 | Minor | Publication | The runtime KaTeX CDN dependency and the offline/packaged-docs deferral are not identified in published pages. | `zensical.toml:87-94` loads three `unpkg.com` KaTeX URLs at runtime; grep for `unpkg`, `CDN`, or `offline` in the published pages finds no explanation; `docs/planning/steps/step-30-standalone-documentation.md:70` defers versioning/PDF/offline. | Document the CDN dependency and the offline deferral in getting-started or development docs, with Step 30 as the owner. |
| DOC-08 | Minor | Build | The documented combined docs build fails on a checkout without a prior Release .NET build. | `./tools/docs/build-docs.sh` before `dotnet build Mocassin.slnx -c Release` exits 255: DocFX reports “No files are found with glob pattern …/bin/Release/net10.0/Mocassin.*.dll”, “No .NET API project detected”, and `--warningsAsErrors` fails the build. The same script exits 0 after the Release build. `tools/docs/README.md:5-13` lists the script without the prerequisite; `docs/getting-started/index.md:18-33` does build first. | Build the required assemblies or detect missing Release outputs in the script and print an actionable prerequisite; document it in `tools/docs/README.md`. |
| DOC-09 | Minor | Build | The PowerShell combined build omits the DocFX warnings-as-errors gate used by bash and CI. | `tools/docs/build-docs.sh:24` passes `--warningsAsErrors`; `tools/docs/build-docs.ps1:26` does not. | Add the same warning policy to the PowerShell script so a Windows local build cannot pass with DocFX warnings. |
| DOC-10 | Note | Publication boundary | The development Containerfile is published as a raw site file outside navigation. | Zensical copies `docs/opencode/mocassin.containerfile` to `site/opencode/mocassin.containerfile`; `zensical.toml:21-23` excludes only `planning/`; the file is not in nav and is not linked from the published site. | Confirm this is intentional, or exclude `opencode/` and link the file from a developer page if it should remain downloadable. |
| DOC-11 | Note | External links | Published links target the private GitHub repository or a non-resolving schema host and cannot be used by unauthenticated readers. | `curl` returned 404 for `https://github.com/seb-eis/mocassin-2` and 000 for `https://mocassin.dev/schemas/project-v1.json`; the KaTeX unpkg URLs returned 200. | Verify the publication access model before launch, mark repository-internal links clearly, and use a resolvable schema URI. |
| DOC-12 | Note | API docs | The Python/mkdocstrings reference is a placeholder, so the `mkdocstrings` checklist item cannot be exercised at Phase B. | `docs/api/python/index.md:3-6`; `docs/api/index.md:11`; no Python API rendering exists in `site/api/python/`. | Keep the placeholder and re-check at Step 22 (or Step 30 for final publication). |

## Record state

**State:** Passed with notes

`Re-review required` is a record state, not a verdict.

## Reviewer independence

> I am a fresh context. I did not author or implement the reviewed work and have no shared conversation with the
> request owner, the orchestrator, the XC-01/XC-02 reviewers, or any Phase B implementor. I read the repository
> myself at `81fba457d6d4714b4fe56d9c7084a738815b365e`; the request commit `2fe7b91` only fills request metadata.
> I did not fix any finding.

## Verdict

- [ ] Passed
- [x] Passed with notes
- [ ] Fixes required
- [ ] Waived

Follow-up owner: Phase B orchestrator and documentation owner
Follow-up step: Step 30 (tutorials, CDN/offline assumptions, and final publication); Step 08 (stale step-status text); Step 22 (Python API)

## Remediation status

| Finding | Status | Owner | Commit |
| --- | --- | --- | --- |
| DOC-01 | Accepted — human owner 2026-11-12 | Documentation owner | Follow-up Step 30: add the planned Phase B tutorials or relabel the index |
| DOC-02 | Fixed | Orchestrator | `62e1072` — parity-matrix documentation/CI/test rows updated to `Implemented`/`Partial` with evidence steps |
| DOC-03 | Accepted — human owner 2026-11-12 | Documentation owner | Follow-up Step 30: replace the invalid `viewSettings` envelope example and schema/seed placeholders |
| DOC-04 | Accepted — human owner 2026-11-12 | Documentation owner | `62e1072` partly refreshed; remaining future-tense pages are a Step 30 follow-up |
| DOC-05 | Accepted — human owner 2026-11-12 | Documentation owner | Follow-up Step 30/07: complete the `SIM012` title, add path/hint columns or record the deviation, and align the drift-check claim |
| DOC-06 | Accepted — human owner 2026-11-12 | Documentation owner | Follow-up Step 30: point the API nav at the placeholder indexes or make it conditional |
| DOC-07 | Accepted — human owner 2026-11-12 | Documentation owner | Follow-up Step 30: document the KaTeX CDN dependency and offline deferral |
| DOC-08 | Fixed (documented prerequisite) | Tooling | `62e1072` — `tools/docs/README.md` now requires a prior Release build; automatic detection remains optional |
| DOC-09 | Fixed | Tooling | `62e1072` — PowerShell DocFX now passes `--warningsAsErrors` |
| DOC-10 | Accepted note — human owner 2026-11-12 | Documentation owner | Confirm or exclude `docs/opencode/` from the published site before publication |
| DOC-11 | Accepted note — human owner 2026-11-12 | Documentation owner | Verify public repository/schema links before launch |
| DOC-12 | Accepted note — human owner 2026-11-12 | Documentation owner | Keep the Python API placeholder until Step 22 |

Final substantive checkpoint after the XC-02 round-3 remediation: `84781f8a4ea3ef64fedaee59d513e26de604ee2c` (this record’s scope is unchanged). XC-03 found no Blocker or Major; the verdict remains **Passed with notes** and no re-review is required. The human owner accepted the documentation Minor/Note follow-ups on 2026-11-12; the open items above target Step 30/22.

## Human approval

- [ ] Accepted: Passed
- [x] Accepted: Passed with notes
- [ ] Fixes required
- [ ] Waived

Approved by: Sebastian Eisele
Date: 2026-11-12
