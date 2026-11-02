# Step 29 — Standalone Documentation Completion and Publishing

**Phase:** G. Release and documentation  
**Depends on:** 19, 21, 27  
**Delivers:** The completed, audited, and published standalone documentation site built with Zensical, plus
generated .NET (DocFX) and Rust (rustdoc) API references under one site.

## Goal

Give users, scientists, and developers one coherent documentation site that explains how to install and use
MOCASSIN, the physics behind it, the architecture of the codebase, and how to extend and contribute. The site
must be buildable locally and in CI, with no hand-maintained duplication of API reference pages.

The site is **not written at the end**. Step 00 creates the skeleton, every feature step writes its pages, and
this step completes cross-cutting content, audits coverage, integrates the final APIs, and publishes the site.

## Original reference

- Original docs: `docs/userguide-md/` (user guide/API prose), `docs/old-guide/` (PDF/DOCX), build scripts
  using Pandoc/KaTeX, and a release-time HTML bundle.
- The original documentation mixed user guide, API examples, and build instructions; there was no separate
  developer architecture guide, tutorial track, CLI/format reference, or unified API reference.
- The API guide was prose with C#/F# snippets, not generated from source.
- The documentation build was part of a Windows PowerShell release pipeline.

## Design

### Documentation is written incrementally

| Stage | Responsibility |
| --- | --- |
| Step 00 | Create the Zensical skeleton, navigation, placeholder pages, build scripts, Docs CI job, and API stubs |
| Every feature step (01–28) | Write/update the user-guide, physics, developer, reference, or tutorial pages for that feature; keep the strict docs build green |
| Step 29 (this step) | Audit coverage, write cross-cutting pages, integrate final DocFX/rustdoc, polish, screenshot, validate tutorials, publish |
| Step 30 | Package the completed `site/` artifact with the release |

Step 29 therefore owns completion and quality, not first authoring. If a feature page is missing, fix it here
and add a note to the originating step.

### 1. Toolchain

| Content | Tool | Reason |
| --- | --- | --- |
| Narrative docs (home, user guide, physics, tutorials, developer guide, reference) | **Zensical** | Modern MkDocs-compatible static site generator, good authoring/navigation/search, Markdown-first |
| .NET API reference | **DocFX** | Native XML-doc/assembly metadata, .NET navigation, cross-references, modern .NET support |
| Rust API reference | **rustdoc** (`cargo doc`) | Canonical Rust documentation, doc tests, intra-doc links, no duplicate schema |
| Search/navigation | Zensical for narrative; DocFX/rustdoc retained for API sections | Avoids fragile reimplementation of language-specific API navigation |
| Diagrams | Mermaid or Zensical-compatible Markdown diagrams | Versionable in text |
| Math | Zensical/KaTeX-compatible math | Physics equations |

- **One published site, separate build outputs.** Zensical owns the root. DocFX is copied to
  `/api/dotnet/`, rustdoc to `/api/rust/`. `docs/api/index.md` is the API landing page.
- **Versioning/PDF/offline** are deferred; the first release publishes the current version only. The build
  layout leaves room for versioned subpaths later.
- **Planning is not published.** `docs/planning/` remains in the repository but is excluded from Zensical via
  `exclude_docs`.
- **Docs-as-code.** Every feature step writes or updates its documentation. This step assembles, edits, and
  validates; it does not invent feature docs at the end.

### 2. Repository layout

```text
zensical.toml                  # project config: docs_dir = "docs", site_dir = "site"
docs/
├── index.md                   # landing page
├── getting-started/
│   ├── installation.md
│   ├── quickstart.md
│   └── first-project.md
├── user-guide/
│   ├── projects.md
│   ├── particles.md
│   ├── structure.md
│   ├── transitions.md
│   ├── energy.md
│   ├── lattice.md
│   ├── simulations.md
│   ├── parameterization.md
│   ├── job-templates.md
│   ├── building.md
│   ├── running.md
│   ├── results.md
│   ├── visualization.md
│   └── mmcfe.md
├── physics/
│   ├── overview.md
│   ├── monte-carlo.md
│   ├── kmc.md
│   ├── mmc.md
│   ├── energies.md
│   ├── symmetry.md
│   ├── transport.md
│   ├── tracking.md
│   └── units-constants.md
├── tutorials/
│   ├── ceria-kmc.md
│   ├── ceria-mmc.md
│   ├── barium-zirconate.md
│   ├── vehicle-mechanism.md
│   ├── parameter-sweep.md
│   ├── hpc-campaign.md
│   └── result-analysis.md
├── reference/
│   ├── cli.md
│   ├── project-format.md
│   ├── msl-format.md
│   ├── mcs-format.md
│   ├── diagnostics.md
│   ├── file-layout.md
│   └── extension-abi.md
├── development/
│   ├── architecture.md
│   ├── code-structure.md
│   ├── design-decisions.md
│   ├── compilation-pipeline.md
│   ├── solver-architecture.md
│   ├── ui-architecture.md
│   ├── testing-and-parity.md
│   ├── contributing.md
│   └── release.md
├── performance/
│   ├── benchmarks.md
│   ├── hpc.md
│   ├── profiling.md
│   └── large-campaigns.md
├── troubleshooting/
│   ├── faq.md
│   ├── errors.md
│   ├── installation.md
│   └── performance.md
├── migration/
│   ├── clean-break.md
│   ├── recreating-projects.md
│   └── result-differences.md
├── examples/
│   ├── index.md
│   ├── ceria.md
│   ├── barium-zirconate.md
│   ├── vehicle.md
│   └── publications.md
├── glossary/
│   ├── index.md
│   ├── notation.md
│   └── units.md
├── api/
│   ├── index.md               # links to /api/dotnet and /api/rust
│   ├── dotnet/index.md        # placeholder; replaced by DocFX output in site/api/dotnet
│   └── rust/index.md          # placeholder; replaced by rustdoc output in site/api/rust
└── planning/                  # excluded from the published site

tools/docfx/docfx.json         # DocFX configuration for the .NET API
```

### 3. Required content

These sections are mandatory for the first release:

1. **Getting started**
   - installation for Windows/Linux/macOS;
   - prerequisites (.NET, solver, GPU);
   - quickstart with the ceria sample;
   - first project, first package, first run, first result.
2. **User guide**
   - project lifecycle;
   - particles/sets, structure/sites, transitions, energy, lattice/doping, simulations;
   - parameterization and job templates;
   - building, running, collecting, visualizing, MMCFE;
   - every screen has a task-oriented page and a screenshot.
3. **Physics**
   - fixed-lattice Monte Carlo and the MMC/KMC distinction;
   - transition-state theory, detailed balance, acceptance;
   - KMC normalization, time step, selection;
   - pair/cluster/site energy model and the transition-state energy;
   - space-group symmetry, P1 expansion, and why positions are fixed;
   - transport: mobility, conductivity, Nernst–Einstein, Haven ratio;
   - tracking: global/mobile/static, MSD, diffusion, correlations;
   - units, constants, sign conventions, and known approximations.
4. **Tutorials**
   - ceria KMC;
   - ceria MMC;
   - Y-doped BaZrO₃ hydration/transport;
   - vehicle/interstitialcy mechanism;
   - temperature/doping parameter sweep;
   - HPC campaign;
   - result analysis and plots.
5. **CLI + formats reference**
   - `mocassin` and `mocassin-sim` commands, options, exit codes, JSON events;
   - project JSON, `.msl`, `.mcs` MessagePack schema, extension ABI;
   - diagnostics catalog and file-layout contract.
6. **Developer architecture guide**
   - repository/module structure;
   - dependency rules and data flow;
   - design decisions (link to ADRs);
   - compiler pipeline, solver architecture, Avalonia MVVM/rendering, result evaluation;
   - testing, parity, fuzzing, and release process;
   - how to add a model domain, a transition mechanism, an evaluation, or a UI editor.
7. **Performance + HPC guide**
   - benchmark methodology;
   - memory/parallelism model;
   - SLURM/PBS workflows;
   - large-campaign layout and collection;
   - profiling and troubleshooting slow jobs.
8. **Troubleshooting + FAQ**
   - installation, GPU/OpenGL, solver aborts, validation errors, package corruption, broken links.
9. **Migration + clean-break guide**
   - no legacy import;
   - how to recreate a legacy `.mocprj` in the new format;
   - expected result differences (MessagePack state, attempt histograms, corrected formulas);
   - citation/attribution.
10. **Glossary, notation, units**
    - symbols, indices, unit conventions, Kröger–Vink notation, acronyms.
11. **Examples + publications**
    - downloadable sample projects;
    - command transcripts;
    - reference publications and citation guidance.
12. **API reference**
    - .NET via DocFX;
    - Rust via rustdoc;
    - landing page, cross-links to guides, and a "how to read the API docs" note.

### 4. API reference build

**.NET**

- `tools/docfx/docfx.json` configures metadata from `src/dotnet/`.
- Build with the pinned `docfx` .NET tool (`dotnet tool restore` + `dotnet docfx`).
- Enable XML documentation generation for all public libraries (already required by the build).
- Treat missing XML docs on public APIs as warnings/errors in CI.
- Copy the DocFX output to `site/api/dotnet/`.

**Rust**

- `cargo doc --workspace --no-deps --target-dir target/doc`.
- Enable `#![deny(missing_docs)]` on public crates.
- Copy `target/doc/` to `site/api/rust/`.
- Keep crate-level docs linking to the narrative architecture/physics pages.

**Cross-links**

- `docs/api/index.md` links to both API trees.
- Narrative pages link to stable API paths (`/api/dotnet/...`, `/api/rust/...`).
- API landing pages include a "back to guide" link.
- Cross-links are checked by the site link checker; API-internal links are checked by DocFX/rustdoc.
- Unified search across narrative + API is deferred; each generated API keeps its own search.

### 5. Build and publication

Scripts:

```bash
# narrative + combined site
zensical build --strict

# .NET API
dotnet tool restore
dotnet docfx tools/docfx/docfx.json -o artifacts/docfx

# Rust API
cargo doc --workspace --no-deps --target-dir target/doc

# combine (placeholders live at site/api/dotnet/index.html and site/api/rust/index.html)
mkdir -p site/api/dotnet site/api/rust
cp -r artifacts/docfx/. site/api/dotnet/
cp -r target/doc/. site/api/rust/
```

- The Step 00 `tools/docs/build-docs.sh`/`.ps1` scripts are extended here to build and copy DocFX/rustdoc.
- `tools/docs/serve.sh` (from Step 00) runs `zensical serve` for authoring.
- The Step 00 Docs CI job already builds Zensical strictly; this step adds the DocFX/rustdoc jobs and final
  combined artifact checks.
- Publish the `site/` artifact on release tags. If GitHub Pages is available, deploy there; otherwise attach
  the site archive to the release.
- Do not commit generated API HTML or `site/`. The API placeholders keep the narrative build strict-clean;
  run the final link check again after copying generated API output over them.
- A `tools/docs/requirements.txt` (or a root `pyproject.toml`) pins Zensical and any Markdown extensions.

### 6. Content ownership

Documentation is written alongside the feature, not at the end:

| Content | Written by | Finalized by |
| --- | --- | --- |
| Installation/quickstart/first project | 22, 25 | 29 |
| User guide (model domains) | 03–06, 23–24 | 29 |
| User guide (build/run/results/MMCFE) | 12, 25, 27 | 29 |
| CLI reference | 12, 19 | 29 |
| Project/.msl/.mcs/extension formats | 01, 11, 18, 19 | 29 |
| Diagnostics | 07 | 29 |
| Physics/units/formulas | 15–18, 20 | 29 |
| Developer architecture | 00–02, 08, 13–21, 28 | 29 |
| Tutorials | 25, 27, 28 | 29 |
| Performance/HPC | 19, 28 | 29 |
| Migration/clean-break | 00, 29 | 29 |
| Examples/publications | 28, 29 | 29 |
| API reference landing/links | 29 | 29 |

Each step’s “Done when” includes updating its docs page. This step verifies completeness, consistency, and
buildability.

### 7. Documentation quality rules

- Every page starts with a one-sentence purpose and intended audience.
- Every command is copy-pasteable and uses placeholders consistently.
- Every tutorial has prerequisites, steps, expected output, and troubleshooting.
- Physics pages state assumptions, units, and sign conventions; equations are testable against the code.
- Architecture pages link to ADRs and step pages rather than duplicating decisions.
- Screenshots use the current UI and include alt text.
- No dead links, no orphan pages, no placeholder "TODO" in a release build.
- API docs are generated from source; do not hand-maintain duplicate API pages.
- `docs/planning/` is internal and excluded from the site.
- English first; localization is deferred.

## Implementation tasks

- [ ] Audit every required page against the Step 00 skeleton and the feature-step docs; fill gaps and remove
  placeholders.
- [ ] Write/complete cross-cutting pages: physics overview and conventions, architecture synthesis, migration
  guide, glossary/notation/units, examples/publications, and tutorial introductions.
- [ ] Integrate the final DocFX and rustdoc outputs into `/api/dotnet/` and `/api/rust/`; complete the API
  landing page and cross-links.
- [ ] Add/refresh screenshots, diagrams, and alt text for every GUI screen.
- [ ] Run every tutorial against `samples/` and fix commands, expected output, and troubleshooting notes.
- [ ] Validate the full site with `zensical build --strict`, DocFX, and `cargo doc`.
- [ ] Add a docs review checklist and ownership/coverage map.
- [ ] Publish the `site/` artifact on release; optionally deploy to Pages.
- [ ] Record any deferred docs items in the parity matrix.

## Acceptance criteria

- [ ] `zensical build --strict` succeeds with no broken links/anchors.
- [ ] DocFX builds the .NET API with no missing-doc errors for public APIs.
- [ ] `cargo doc --workspace --no-deps` builds the Rust API with no warnings.
- [ ] The combined site serves narrative pages at `/` and API docs at `/api/dotnet/` and `/api/rust/`.
- [ ] All required and selected sections exist and are reachable from the navigation.
- [ ] No placeholder/TODO pages remain except the API landing placeholders, which are replaced by generated
  output at build time; every narrative navigation entry has real content.
- [ ] The ceria tutorial can be completed using only the docs and samples.
- [ ] Every public GUI screen has a user-guide page and screenshot.
- [ ] Physics pages match the implemented formulas and units tests.
- [ ] Migration/clean-break and glossary pages are present.
- [ ] `docs/planning/` is not published.
- [ ] The Docs CI job has remained green throughout implementation and the release artifact contains the
  complete site.
- [ ] Documentation written

## Documentation

At this point, complete the cross-cutting and final documentation:

- `docs/index.md` and `docs/api/index.md` — final landing/navigation/API entry points.
- `docs/physics/overview.md` and `docs/physics/units-constants.md` — physics synthesis and conventions.
- `docs/development/architecture.md`, `design-decisions.md` — final architecture synthesis and ADR links.
- `docs/migration/`, `docs/glossary/`, `docs/examples/publications.md` — migration, notation, citations.
- Audit every page written by Steps 01–28; fill gaps, refresh screenshots, run tutorials, publish the site.

## Tests

| Test | Assertion |
| --- | --- |
| Zensical build | Strict mode, no broken links/anchors |
| DocFX | API metadata/build; public docs coverage |
| rustdoc | `cargo doc`; missing-docs deny |
| Navigation | Every required page reachable |
| Tutorials | Commands run against samples |
| Cross-links | Narrative ↔ API links resolve |
| Screenshots | Present and current |
| Search | Narrative search returns key pages |
| Exclusions | Planning pages absent from `site/` |
| Artifact | Release contains the combined site |

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Docs drift from code | Docs-as-code per step; CI build/link checks; tutorial smoke tests |
| Zensical is 0.x | Pin the version; keep content standard Markdown/MkDocs-compatible |
| DocFX/rustdoc navigation mismatch | Keep narrative navigation authoritative; API landing page; cross-links |
| API docs missing comments | Treat missing XML docs/rustdoc as errors in CI |
| Large site/build time | Cache dependencies; build API docs only when source changes |
| Screenshot churn | Automate screenshots where possible; review per release |

## Done when

A new user can install MOCASSIN, complete the ceria tutorial, understand the physics well enough to trust the
results, and a developer can navigate the architecture and API references from one published site.
