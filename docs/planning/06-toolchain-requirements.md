# 06 — Development Toolchain Requirements

This page lists the host/container tooling needed to implement, test, document, and release MOCASSIN. It is
the source of truth for the dedicated MOCASSIN development image defined in
[`docs/opencode/mocassin.containerfile`](../opencode/mocassin.containerfile).

That image depends **only on the `opencode:base` image from
[`seb-eis/opencode-container`](https://github.com/seb-eis/opencode-container), built from its
`variants/base.containerfile`. The opencode-container repository is not modified.

The containerfile is intended as the reproducible base for a MOCASSIN coding agent running under the opencode
container system. The same tools can of course be installed on a normal workstation or container for direct
development without opencode; the image is just one packaged way to provide them.

Versions are candidate pins as of **2026-11-03**. Keep the image pins and the project tool manifests
(`global.json`, `Directory.Packages.props`, `Cargo.toml`, `tools/docs/requirements.txt`) aligned.

## 1. Status vocabulary

| Status | Meaning |
| --- | --- |
| **Required** | Blocks Step 00 or the named step/CI job; add to the image before that work starts. |
| **Recommended** | Needed by a named later step; can be added when that step starts. |
| **Optional** | Nice-to-have; no acceptance gate depends on it. |
| **Present** | Already provided by `opencode:base`; no action in the MOCASSIN image. |
| **Project-local** | Restored by NuGet/Cargo/pip; not installed in the image. |
| **Deferred** | Out of scope until the named release/feature. |

## 2. Provided by `opencode:base` (opencode-container)

| Tool | Status | Needed for | Notes |
| --- | --- | --- | --- |
| Node 26 / npm, OpenCode CLI | Present | Tooling only | Version follows `opencode:base`; not a MOCASSIN runtime dependency. |
| git, git-lfs, curl, ca-certificates | Present | Steps 00+ | Repository and downloads. |
| sqlite3, zstd | Present | Steps 11, 18, 20 | Package/state inspection. |
| jq, rg, fd, tree, file, unzip/zip, xxd | Present | Steps 00+ | General inspection and scripts. |
| shellcheck, shfmt | Present | Steps 00+ | Shell script gates. |
| openssl | Present | Steps 11/31 | Hashing/tooling. |

Except for the explicitly optional release tools in section 7, everything else below is installed by
`docs/opencode/mocassin.containerfile`.

## 3. System and platform packages

### 3.1 GUI, Skia, and headless graphics

Required for Avalonia, screenshot tests, and the 3D viewer. Included in
`docs/opencode/mocassin.containerfile`.

| Package group | Status | Needed for | Notes |
| --- | --- | --- | --- |
| `fontconfig`, `fonts-dejavu-core`, `fonts-liberation` | Required | Steps 23–28 | Deterministic text and screenshots. Run `fc-cache -f`. |
| `libfontconfig1`, `libfreetype6`, `libharfbuzz0b`, `libpng16-16`, `libglib2.0-0` | Required | Steps 23–28 | SkiaSharp/Avalonia native text and image stack. |
| `libx11-6`, `libx11-xcb1`, `libxcb1`, `libxext6`, `libxrender1`, `libxfixes3`, `libxrandr2`, `libxi6`, `libxcursor1`, `libxinerama1`, `libxcomposite1`, `libxdamage1`, `libxtst6`, `libxss1`, `libxkbcommon0`, `libxkbcommon-x11-0`, `libxxf86vm1`, `libxshmfence1`, `libice6`, `libsm6` | Required | Steps 23–28 | Avalonia X11 backend and input. |
| `libgl1`, `libegl1`, `libgles2`, `libglx-mesa0`, `libegl-mesa0`, `libgl1-mesa-dri`, `libgbm1`, `libdrm2` | Required | Steps 23–28 | OpenGL/EGL; software rendering for CI. |
| `mesa-utils` | Required | Steps 23–28 | `glxinfo` verification. |
| `xvfb`, `xauth`, `x11-utils` | Required | Steps 23–28 | Headless GUI/screenshot runs. Use `LIBGL_ALWAYS_SOFTWARE=1`. |
| `libwayland-client0`, `libwayland-cursor0`, `libwayland-egl1` | Optional | Step 27 | Only if the Wayland backend is exercised. |
| `libvulkan1`, `vulkan-tools`, `mesa-vulkan-drivers` | Optional | Step 27 | Renderer-backend spike only. |
| `libdbus-1-3`, `libgtk-3-0`, `xdg-utils` | Optional | Steps 23–28 | Native dialogs/portals; tests use fake pickers. |

### 3.2 Native diagnostics and compilation

| Tool | Status | Needed for | Notes |
| --- | --- | --- | --- |
| `linux-perf` | Required | Step 29 | Solver/CPU profiling and flamegraphs. In the default opencode container, `perf` is installed but blocked by `perf_event_paranoid=3` and `cap-drop=ALL`; use Valgrind/Callgrind or grant `PERFMON` explicitly. |
| `valgrind` | Optional | Steps 13–19 | Native memory diagnostics; Miri is preferred for Rust. |
| `clang`, `lld`, `llvm` | Optional | Steps 19, 29, 31 | Fuzzing/AOT/NativeAOT only; defer until the AOT decision or fuzz setup needs it. |
| `libssl-dev`, `zlib1g-dev` | Recommended | Steps 29, 31 | Cargo tool builds and NativeAOT prerequisites. |

## 4. .NET / C#

| Tool | Status | Needed for | Notes |
| --- | --- | --- | --- |
| .NET SDK 10.0.x | Required | Steps 00+ | Installed by the MOCASSIN image; pin `10.0.403` in `global.json`. |
| `dotnet format` | Required | Steps 00+ | Built into the installed SDK. |
| DocFX 2.81.0 | Required | Step 00 docs job, Step 30 | **~300 MB installed.** Install as a global tool; add `~/.dotnet/tools` to `PATH`. |
| `dotnet-reportgenerator-globaltool` 5.5.12 | Recommended | Step 29 | Coverage report aggregation. |
| `dotnet-counters`, `dotnet-trace`, `dotnet-dump` | Optional | Step 29 | .NET runtime diagnostics; `perf` remains the primary profiler. |
| Avalonia, CommunityToolkit.Mvvm, LiveCharts2, Silk.NET | Project-local | Steps 23–28 | NuGet packages, restored per repo. |
| MessagePack, ZstdSharp, Microsoft.Data.Sqlite | Project-local | Steps 11, 18, 20 | NuGet packages. |
| xUnit, FsCheck, FluentAssertions | Project-local | Steps 00+ | Test packages. |
| `System.Numerics.Tensors` | Project-local | Step 20 | Vectorized evaluations. |
| `Microsoft.CodeAnalysis.PublicApiAnalyzers` | Project-local | Steps 00, 31 | Public API analyzer package. |
| `Microsoft.DotNet.ApiCompat.Tool` | Required | Steps 00, 31 | Dotnet tool for the public-API compatibility gate; declare it in `.config/dotnet-tools.json`. |
| Serilog | Project-local | Steps 23–28 | Application logging. |

**Note:** `dotnet tool install --global` writes shims to `~/.dotnet/tools`. The MOCASSIN image adds that
directory to `PATH`.

## 5. Rust

| Tool / component | Status | Needed for | Notes |
| --- | --- | --- | --- |
| Rust stable 1.99.1, Cargo, rustfmt, clippy | Required | Steps 00+ | Installed by the MOCASSIN image; keep warnings/format gates. |
| `cargo-binstall` 1.26.2 | Recommended | Image build | Speeds up installing the other Cargo tools. |
| `cargo-deny` 0.20.2 | Required | Steps 00, 29 | License/advisory/dependency policy. |
| `cargo-llvm-cov` 0.9.1 | Recommended | Step 29 | Rust coverage; needs `llvm-tools-preview`. |
| `llvm-tools-preview` | Recommended | Step 29 | Install via `rustup component add`. |
| `cargo-fuzz` 0.13.2 | Required | Steps 13, 29 | Fuzz `.msl`/`.mcs`/extension-config readers. On Linux, use `--target x86_64-unknown-linux-gnu`; the default musl target is incompatible with ASAN. |
| Nightly Rust toolchain | Required | Steps 13, 29 | `cargo-fuzz` needs nightly; use `--profile minimal`. |
| `miri`, `rust-src` | Recommended | Step 29 | Unsafe-code validation; nightly components. |
| `cargo-insta` 1.49.0 | Optional | Steps 18–20 | Snapshot review; `insta` crate tests run without the CLI. |
| `cargo-nextest` 0.9.150 | Optional | All test tiers | Faster test execution. |
| `cargo-flamegraph` | Optional | Step 29 | Flamegraphs; uses `perf`. |
| `cargo-audit` 0.22.2 | Optional | Step 29 | Alternative to `cargo-deny`; do not run both as gates. |
| `clang`/`lld`/`llvm` | Optional | Steps 19, 29, 31 | Only if fuzzing/AOT needs them. |
| serde, rmp-serde, rusqlite, zstd, clap, thiserror, tracing, rand_chacha, proptest, criterion, insta | Project-local | Steps 13+ | Cargo dependencies; no image install. |

## 6. Python

Debian marks Python as externally managed. `pip install --user` fails; use a dedicated venv or `pipx`. Do not
use `--break-system-packages`. The MOCASSIN image uses one venv at `/home/node/.venvs/mocassin` for docs,
tests, and packaging.

| Tool / package | Status | Needed for | Notes |
| --- | --- | --- | --- |
| Python 3.11–3.13, pip, venv | Required | Steps 00, 22 | Installed by the MOCASSIN image (3.13); CI runs 3.13 automatically, 3.11/3.12 manual/nightly. |
| `pipx` | Optional | Step 00 | Alternative to the dedicated venv; the image uses the venv. |
| `zensical` 0.1.1 | Required | Steps 00, 30 | Narrative docs build; 0.1.x beta (semi-stable), pin exactly. |
| `mkdocstrings[python]` 1.0.8 | Required | Steps 22, 30 | Pulls `mkdocs`, `mkdocstrings-python` 2.0.10, `mkdocs-autorefs`, `griffe`. |
| `pytest` 9.2.0 | Required | Steps 00, 22 | Python tests. |
| `pytest-cov` 7.1.0 | Recommended | Steps 22, 29 | Coverage report. |
| `hypothesis` 6.170.14 | Required | Step 22 | Property-based reader/aggregation tests. |
| `numpy` 2.5.4 | Required | Step 22 | Array API. |
| `msgpack` 1.2.4 | Required | Steps 08/22 | Step 08 projection fixture checker (dev-only); `.mcs` reader. |
| `build` 1.6.1 | Required | Steps 22, 31 | Wheel/sdist build. |
| `twine` 7.0.0 | Optional | Step 31 | PyPI upload/check. |
| `mypy` 2.4.1 | Optional | Step 22 | Static type checking; `ruff` is already present. |
| `ruff` 0.17.3 | Required | Step 22 | Installed in the MOCASSIN Python venv. |

Project-local Python dependencies for `mocassin-analysis`: `msgpack`, `numpy`, `pytest`, `pytest-cov`,
`hypothesis`, `mkdocstrings[python]`, `build`. Keep the exact pins in `src/python/pyproject.toml` and
`tools/docs/requirements.txt`.

## 7. Documentation, CI, and release

| Tool | Status | Needed for | Notes |
| --- | --- | --- | --- |
| Zensical + mkdocstrings | Required | Steps 00, 22, 30 | Covered in the Python section. |
| DocFX | Required | Steps 00, 30 | Covered in the .NET section. |
| rustdoc | Required | Steps 00, 30 | Ships with the Rust toolchain; `cargo doc`. |
| `actionlint` | Optional | Step 00 | Validates `.github/workflows/ci.yml`. |
| `gh` | Optional | Step 31 | Release/PR automation. Do not use `gh auth login`; derive `GH_TOKEN` per command. |
| `syft` | Optional | Step 31 | CycloneDX/SBOM generation. |
| `grype` | Optional | Step 31 | Artifact vulnerability scanning. |
| `cosign` | Optional | Step 31 | Signing/provenance if enabled. |
| `pandoc` | Deferred | Step 30/31 | PDF/offline docs only. |
| `mermaid-cli` | Deferred | Step 30 | Zensical renders Mermaid client-side; no static CLI needed for v1. |

## 8. Image strategy

The MOCASSIN toolchain lives in one dedicated image:

```text
docs/opencode/mocassin.containerfile
```

It builds **only** from the `opencode:base` image produced by
[`seb-eis/opencode-container`](https://github.com/seb-eis/opencode-container):

```dockerfile
ARG BASE_IMAGE=opencode:base
FROM ${BASE_IMAGE}
```

The opencode-container repository is not modified. The base image must be built separately before this image
is built:

```sh
# In a checkout of https://github.com/seb-eis/opencode-container:
git clone https://github.com/seb-eis/opencode-container
cd opencode-container
make base

# In the MOCASSIN repository:
cd /path/to/mocassin
podman build -f docs/opencode/mocassin.containerfile -t opencode:mocassin .
```

The resulting `opencode:mocassin` image is the base for the MOCASSIN coding agent. The same requirements can be
installed directly on a workstation or another container if opencode is not used.

| Tier | Contents | Approx. addition over `opencode:base` (opencode-container) |
| --- | --- | --- |
| **Tier 1 — implementation** | .NET SDK, stable Rust, Python/venv, GUI/headless libs, Zensical/mkdocstrings, DocFX, Python test tools, `cargo-deny`, `cargo-llvm-cov`, `linux-perf` | ~3.2–3.7 GB installed |
| **Tier 2 — fuzzing/AOT** | Nightly + Miri, `cargo-fuzz`, `clang`/`lld`/`llvm`, `cargo-insta`/`nextest`/`flamegraph` | ~0.6–1.0 GB installed |
| **Release-only (optional)** | `syft`, `grype`, `cosign`, `gh`, `actionlint` | ~0.1–0.2 GB installed |

Approximate total for the full development image over `opencode:base`: **~4.0–4.7 GB installed**,
**~1.5–2.0 GB compressed**. DocFX alone is about 300 MB installed; the .NET and Rust toolchains are the
largest contributors.

The committed `mocassin.containerfile` includes Tier 1 and Tier 2. Release-only tools stay commented until
Step 31 so the image does not carry unreviewed signing/SBOM dependencies during core development.

## 9. Verification checklist

Run these inside the built `opencode:mocassin` image:

```sh
dotnet --version
cargo --version
cargo deny --version
cargo llvm-cov --version
docfx --version
zensical --version
python -c 'import mkdocstrings, mkdocstrings_handlers.python'
pytest --version
xvfb-run -a glxinfo -B
```

Plus:

- `dotnet tool list --global` shows DocFX and ReportGenerator.
- `~/.dotnet/tools` is on `PATH`.
- The venv at `/home/node/.venvs/mocassin` imports `zensical`, `mkdocstrings`, and the Python handler.
- `fc-cache -f` succeeds.
- `LIBGL_ALWAYS_SOFTWARE=1 xvfb-run -a glxinfo -B` reports a renderer.

## 10. Related decisions

- [`docs/opencode/mocassin.containerfile`](../opencode/mocassin.containerfile) — the dedicated MOCASSIN image.

- [ADR 0006](adr/0006-3d-rendering.md) — renderer spike and fallback; drives Vulkan/Wayland optional tools.
- [ADR 0008](adr/0008-standalone-documentation-toolchain.md) — Zensical/DocFX/rustdoc/mkdocstrings.
- [ADR 0010](adr/0010-canonical-messagepack-signatures.md) — canonical projection test vectors.
- [ADR 0011](adr/0011-immutable-package-and-results-store.md) — `.msl`/`.msr` split; affects fuzz targets.
- [ADR 0013](adr/0013-format-compatibility-and-api-versioning.md) — previous-major fixtures.
- [ADR 0014](adr/0014-extension-abi-and-trust-boundary.md) — extension ABI/fuzz targets.
- [Step 00](steps/step-00-repository-toolchain-and-ci.md) — image/CI setup.
- [Step 13](steps/step-13-rust-solver-foundation-and-package-reader.md) — fuzzing starts.
- [Step 19](steps/step-19-solver-cli-hpc-and-extensions.md) — extension ABI.
- [Step 22](steps/step-22-python-analysis-api.md) — Python package/docs.
- [Steps 23–28](steps/step-23-avalonia-shell-and-project-lifecycle.md) — GUI, 3D, screenshots.
- [Step 29](steps/step-29-validation-parity-and-performance.md) — fuzzing, coverage, profiling.
- [Step 30](steps/step-30-standalone-documentation.md) — combined docs site.
- [Step 31](steps/step-31-packaging-and-release.md) — packaging, SBOM, signing.
- [`03-quality-strategy.md`](03-quality-strategy.md) — test tiers and gates.
- [`05-open-decisions.md`](05-open-decisions.md) — remaining owner decisions.
