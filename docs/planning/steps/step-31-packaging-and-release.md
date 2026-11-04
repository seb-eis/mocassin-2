# Step 31 — Packaging and Release

**Phase:** G. Release and documentation  
**Depends on:** 22, 29, 30
**Delivers:** Reproducible cross-platform binaries, NuGet and Python packages, installable archives/packages,
the combined documentation artifact, samples, and a signed release process.

## Implementation status

- **Status:** Not started
- **Step branch:** —
- **Step CI:** —
- **Main merge:** —
- **Step tag:** —
- **Audit:** Not requested
- **Last updated:** —

| Feature | Branch | Status | Local gates | Step CI | Squash commit |
| --- | --- | --- | --- | --- | --- |
| — | — | — | — | — | — |

## Goal

Ship a usable product, not just a passing build. A new user must be able to download, install, create the
ceria sample, build a package, run a job, and analyze results using only the docs.

## Original reference

- Windows MSI installer (WiX) containing the WPF GUI and solver; file association for `.mocprj`.
- Linux solver binaries compiled per HPC host; manual `.so` deployment.
- NuGet packages for the evaluation API, built by PowerShell scripts.
- Documentation generated with Pandoc/KaTeX; build VM setup guide.
- No macOS support, no portable archives, no SBOM/provenance.

## Design

### 1. Versioning

- SemVer `MAJOR.MINOR.PATCH` for the product; independent schema versions for project/package/state.
- Version sources:
  - `Directory.Build.props` (`VersionPrefix`), `Cargo.toml` `workspace.package.version`;
  - CI injects `GIT_SHA`, build timestamp, and channel (`dev`, `rc`, `stable`).
- Every binary/package reports `--version` with:
  - product version, git SHA, build profile, .NET/Rust version, package format range.
- Compatibility:
  - project schema migrations within a major;
  - package format exact range;
  - state format current + previous version;
  - CLI JSON additive within a major.

### 2. Build outputs

| Artifact | Contents |
| --- | --- |
| `mocassin-<ver>-win-x64.zip` | Avalonia app (self-contained), CLI, `mocassin-sim.exe`, samples, docs |
| `mocassin-<ver>-linux-x64.tar.gz` | App, CLI, solver, samples, docs |
| `mocassin-<ver>-linux-arm64.tar.gz` | Same |
| `mocassin-<ver>-osx-arm64.tar.gz` | App bundle, CLI, solver, samples, docs |
| `mocassin-<ver>-osx-x64.tar.gz` | Same |
| Optional `.deb`/`.rpm`/AppImage | Desktop integration for Linux |
| Optional `.dmg`/`.pkg` | macOS app distribution |
| NuGet packages | `Mocassin.Core`, `.Symmetry`, `.Compilation`, `.Contracts`, `.Packages`, `.Results`, `.Cli` |
| Python package | `mocassin-analysis` wheel + sdist (import package `mocassin`) |
| `mocassin-sim` standalone | Solver only, for HPC nodes |
| Docker image (optional) | CLI + solver for CI/HPC containers |

- .NET publish: `-c Release -r <rid> --self-contained true -p:PublishSingleFile=true`
  (or framework-dependent for smaller packages; document the trade-off).
- Rust: `cargo build --release --target <triple>`; strip symbols; record `--version`.
- Bundle the space-group catalog and default samples.
- No runtime dependency on a system SQLite; `rusqlite` bundled.
- Build scripts:
  - `build.ps1` (Windows) and `build.sh` (Linux/macOS);
  - `dotnet build/test/publish`, `cargo build/test`, packaging, checksums.

### 3. Desktop packaging

**Windows**

- Zip archive with a `mocassin.exe` launcher; optional MSIX for Start-menu integration.
- File association for `.mocprj` via installer/MSIX manifest (optional).
- Code signing if a certificate is available; otherwise publish checksums.

**Linux**

- Tar.gz with a `mocassin` launcher script and `.desktop` file.
- Optional `.deb`/`.rpm` with dependencies (libssl, fontconfig, etc.).
- AppImage optional for distro-independent distribution.
- Test on Ubuntu LTS and Fedora.

**macOS**

- `.app` bundle inside a tar.gz or `.dmg`.
- Ad-hoc signing for local use; Developer ID signing + notarization for public releases.
- Universal binary or separate arm64/x64 builds.
- Document Gatekeeper steps if unsigned.

### 4. NuGet packages

- Public API packages with XML docs and source link.
- `Mocassin.Results` for evaluation scripts; `Mocassin.Packages` for headless building.
- Package the symmetry catalog as a content file or embedded resource (no path lookup).
- `dotnet add package Mocassin.Results` must work without manual DB paths.
- Symbols package (`.snupkg`) and deterministic builds.
- A `Mocassin.Csx`/scripting helper is optional; the CLI is preferred.

### 5. Documentation artifact

- The standalone documentation site is produced by Step 30 (`zensical build --strict`, DocFX, rustdoc,
  mkdocstrings).
- The release build must consume that exact `site/` artifact, not rebuild ad-hoc Markdown.
- Package the site as `mocassin-docs-<version>.zip` and attach it to the release; optionally deploy it to
  GitHub Pages or another static host.
- Verify that the combined site contains narrative pages at `/`, .NET API at `/api/dotnet/`, Rust API at
  `/api/rust/`, and Python API at `/api/python/`.
- Verify the release notes link to the installed docs, the migration guide, and the citation block.
- **README** remains the short entry point: what it is, install, five-minute ceria tutorial, links to the site.
- **Citation/licence** — MIT, NOTICE with the original GPL project attribution, and the original paper citation.

### 6. Release process

1. Freeze features; update versions/changelog.
2. Run the full release checklist (steps 29–30 gates).
3. Build artifacts on each OS in a clean environment.
4. Generate checksums (SHA-256), SBOM (CycloneDX), and provenance (SLSA-style).
5. Sign binaries/packages where possible.
6. Smoke test each artifact:
   - install/extract;
   - create/open ceria;
   - build a 1-job package;
   - run KMC/MMC;
   - collect/evaluate;
   - open the 3D viewer.
7. Publish GitHub release with notes, checksums, SBOM, and docs.
8. Publish NuGet packages.
9. Tag the repository; archive build logs.
10. Announce with a short “what’s new / breaking changes” note.

### 7. Support and maintenance

- Supported platforms: Windows 10/11 x64, Ubuntu 22.04+/Fedora, macOS 13+ (arm64/x64).
- Supported formats: `.msl`/`.msr` current and previous major; `.mcs` current major (ADR 0013).
- Security policy and issue templates.
- A `doctor` command and crash-log instructions.
- Deprecation policy: one minor release of warning before removal.
- A roadmap for deferred items in [`05-open-decisions.md`](../05-open-decisions.md) (remote deploy, Parquet,
  advanced 3D effects, localization, append builds).

### 8. Post-release validation

- Download each artifact and run the smoke test from the public URL.
- Verify NuGet install in a clean project.
- Check documentation links and sample projects.
- Confirm the parity report and checksums are attached.
- Monitor issue tracker for installation problems.

## Implementation tasks

- [ ] Add version/SHA/channel stamping to .NET and Rust builds.
- [ ] Implement `build.ps1`/`build.sh` for all targets.
- [ ] Produce self-contained app/CLI/solver archives.
- [ ] Add optional MSIX/desktop/dmg packaging.
- [ ] Configure NuGet packaging, symbols, and SourceLink.
- [ ] Package the Step 30 documentation site and include it in the release artifacts.
- [ ] Build and publish the `mocassin-analysis` wheel/sdist and verify a clean install.
- [ ] Add checksums, SBOM, and provenance generation.
- [ ] Implement signing/notarization where credentials exist.
- [ ] Create the release workflow and checklist.
- [ ] Run the public-artifact smoke test.

## Acceptance criteria

- [ ] A clean machine can extract an archive and run the ceria tutorial.
- [ ] All artifacts report the correct version/SHA and pass the smoke test.
- [ ] `dotnet add package Mocassin.Results` works without manual path setup.
- [ ] `pip install mocassin-analysis` works and can read a release fixture `.mcs`.
- [ ] `mocassin-sim` runs standalone on an HPC node.
- [ ] Checksums/SBOM/provenance are attached to the release.
- [ ] The combined documentation site artifact is attached and its links/API subpaths verify.
- [ ] The release links to the migration guide and citation block.
- [ ] Licence/attribution/citation are present in the app and artifacts.
- [ ] Documentation written

## Documentation

At this point, document release and installation:

- `docs/development/release.md` — versioning, signing, SBOM/provenance, release checklist.
- `docs/getting-started/installation.md` — platform archives, first run, uninstall/upgrade.
- `docs/reference/file-layout.md` — installed layout, solver binary, docs artifact.
- Release notes and artifact manifest — version/SHA, checksums, docs site archive, citation.

## Tests

| Test | Assertion |
| --- | --- |
| Version | `--version` fields correct |
| Archive | Extract/run on each OS |
| NuGet | Clean-project install and evaluation |
| Python package | Wheel/sdist build, clean install, `.mcs`/`.msr` fixture read |
| Smoke | Ceria tutorial end-to-end, including `.msr` collection and evaluation |
| Checksums | Match published values |
| Docs | Combined site artifact, link check, API subpaths |
| Signing | Signature/notarization if enabled |
| SBOM | Valid CycloneDX |

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Platform packaging quirks | Test artifacts on clean VMs; document prerequisites |
| macOS notarization delays | Start early; provide unsigned fallback instructions |
| NuGet path/resource issues | Embedded catalog; clean-project test |
| PyPI/name/distribution issues | Use `mocassin-analysis`; attach wheel/sdist if unavailable |
| Documentation drift | Docs built/tested in Step 30 and consumed as an artifact |
| Supply-chain risk | Checksums, SBOM, pinned dependencies, signed tags |

## Done when

A new user can install the application on any supported OS, follow the published docs to reproduce the ceria
workflow, and evaluate results from .NET or Python; the release is signed/checksummed and ships with the
combined documentation site and Python package.
