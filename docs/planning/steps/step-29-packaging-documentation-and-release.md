# Step 29 — Packaging, Documentation, and Release

**Phase:** G. Release  
**Depends on:** 28  
**Delivers:** Reproducible cross-platform binaries, NuGet packages, installable archives/packages, complete
documentation and samples, and a signed release process.

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
| NuGet packages | `Mocassin.Core`, `.Symmetry`, `.Compilation`, `.Packages`, `.Results`, `.Cli` |
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

### 5. Documentation

- **README** — what it is, install, 5-minute ceria tutorial, links.
- **User guide** — rewritten chapters:
  - installation and first project;
  - model concepts (particles, structure, transitions, energy, lattice, simulations);
  - parameterization and job templates;
  - building/running/collecting;
  - evaluation and plots;
  - 3D viewer;
  - MMCFE;
  - HPC workflows;
  - troubleshooting.
- **Reference**:
  - CLI commands/exit codes;
  - project/package/state formats;
  - diagnostics catalog;
  - physics formulas/units/conventions;
  - extension ABI.
- **Migration note** — clean break: no legacy import; how to recreate a project from the old UI; what changes
  in results.
- **Samples** — ceria (KMC/MMC), barium zirconate, vehicle, interstitialcy; each with a README and expected
  outputs.
- **Developer docs** — architecture, build, testing, release, ADRs.
- **Citation/licence** — MIT, NOTICE with the original GPL project attribution, and the original paper citation.

### 6. Release process

1. Freeze features; update versions/changelog.
2. Run the full release checklist (step 28 gates).
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
- Supported package format: current major; older formats rejected with a clear message.
- Security policy and issue templates.
- A `doctor` command and crash-log instructions.
- Deprecation policy: one minor release of warning before removal.
- A roadmap for deferred items (remote deploy, Parquet, advanced 3D effects, localization, append builds).

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
- [ ] Write the user guide, reference, migration note, and samples.
- [ ] Add checksums, SBOM, and provenance generation.
- [ ] Implement signing/notarization where credentials exist.
- [ ] Create the release workflow and checklist.
- [ ] Run the public-artifact smoke test.

## Acceptance criteria

- [ ] A clean machine can extract an archive and run the ceria tutorial.
- [ ] All artifacts report the correct version/SHA and pass the smoke test.
- [ ] `dotnet add package Mocassin.Results` works without manual path setup.
- [ ] `mocassin-sim` runs standalone on an HPC node.
- [ ] Checksums/SBOM/provenance are attached to the release.
- [ ] Docs cover install, workflow, formats, diagnostics, and physics.
- [ ] The migration note clearly states the clean-break policy.
- [ ] Licence/attribution/citation are present in the app and artifacts.

## Tests

| Test | Assertion |
| --- | --- |
| Version | `--version` fields correct |
| Archive | Extract/run on each OS |
| NuGet | Clean-project install and evaluation |
| Smoke | Ceria tutorial end-to-end |
| Checksums | Match published values |
| Docs | Link checker/sample validation |
| Signing | Signature/notarization if enabled |
| SBOM | Valid CycloneDX |

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Platform packaging quirks | Test artifacts on clean VMs; document prerequisites |
| macOS notarization delays | Start early; provide unsigned fallback instructions |
| NuGet path/resource issues | Embedded catalog; clean-project test |
| Documentation drift | Docs built/tested in CI; samples validated |
| Supply-chain risk | Checksums, SBOM, pinned dependencies, signed tags |

## Done when

A new user can install the application on any supported OS, follow the docs to reproduce the ceria workflow,
and evaluate results; the release is signed/checksummed and fully documented.
