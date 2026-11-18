#!/usr/bin/env bash
# SPDX-License-Identifier: MIT
# Build the combined MOCASSIN documentation site.
#
# Zensical owns the narrative site. DocFX and rustdoc output is copied into
# site/api/dotnet and site/api/rust when the corresponding projects/crates
# exist. Generated API HTML and site/ are not committed.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT"

if ! command -v zensical >/dev/null 2>&1; then
  echo "error: zensical is not installed; see tools/docs/requirements.txt" >&2
  exit 1
fi

echo "==> Zensical (strict)"
zensical build --strict

if [[ -f Mocassin.slnx && -f tools/docfx/docfx.json ]] && command -v dotnet >/dev/null 2>&1; then
  # DocFX loads the Release assemblies. Build the solution when they are absent; an existing Release build
  # is reused, so the `all` gate does not repeat the .NET stage's work.
  docfx_inputs=(
    "src/dotnet/Mocassin.Core/bin/Release/net11.0/Mocassin.Core.dll"
    "src/dotnet/Mocassin.Symmetry/bin/Release/net11.0/Mocassin.Symmetry.dll"
    "src/dotnet/Mocassin.Compilation/bin/Release/net11.0/Mocassin.Compilation.dll"
    "src/dotnet/Mocassin.Contracts/bin/Release/net11.0/Mocassin.Contracts.dll"
    "src/dotnet/Mocassin.Packages/bin/Release/net11.0/Mocassin.Packages.dll"
    "src/dotnet/Mocassin.Results/bin/Release/net11.0/Mocassin.Results.dll"
    "src/dotnet/Mocassin.App.ViewModels/bin/Release/net11.0/Mocassin.App.ViewModels.dll"
    "src/dotnet/Mocassin.App.Views/bin/Release/net11.0/Mocassin.App.Views.dll"
    "src/dotnet/Mocassin.Rendering/bin/Release/net11.0/Mocassin.Rendering.dll"
  )
  build_required=false
  for input in "${docfx_inputs[@]}"; do
    if [[ ! -f "$input" ]]; then
      build_required=true
      break
    fi
  done
  if [[ "$build_required" == "true" ]]; then
    echo "==> .NET Release build (DocFX input)"
    dotnet build Mocassin.slnx -c Release
  fi

  echo "==> DocFX"
  dotnet tool restore
  rm -rf artifacts/docfx
  dotnet docfx tools/docfx/docfx.json -o artifacts/docfx --warningsAsErrors
  mkdir -p site/api
  rm -rf site/api/dotnet
  cp -R artifacts/docfx site/api/dotnet

  # DocFX has no root index page. Redirect the directory URL and its internal
  # "../index.html" links to the first content page.
  cat > site/api/dotnet/index.html <<'HTML'
<!doctype html>
<html lang="en"><head><meta charset="utf-8">
<meta http-equiv="refresh" content="0; url=api/Mocassin.Core.CoreModule.html">
<title>MOCASSIN .NET API</title></head>
<body><p><a href="api/Mocassin.Core.CoreModule.html">Open the MOCASSIN .NET API</a></p></body></html>
HTML
else
  echo "==> DocFX skipped (solution or tools/docfx/docfx.json not present yet)"
fi

if [[ -f src/rust/Cargo.toml ]] && command -v cargo >/dev/null 2>&1; then
  echo "==> rustdoc"
  rm -rf target/doc
  cargo doc --manifest-path src/rust/Cargo.toml --workspace --no-deps --target-dir target
  mkdir -p site/api
  rm -rf site/api/rust
  cp -R target/doc site/api/rust

  # cargo doc has no workspace landing page. Redirect the directory URL and
  # rustdoc's internal "../index.html" links to the first crate page.
  cat > site/api/rust/index.html <<'HTML'
<!doctype html>
<html lang="en"><head><meta charset="utf-8">
<meta http-equiv="refresh" content="0; url=mocassin_types/index.html">
<title>MOCASSIN Rust API</title></head>
<body><p><a href="mocassin_types/index.html">Open the MOCASSIN Rust API</a></p></body></html>
HTML
else
  echo "==> rustdoc skipped (Rust workspace not present yet)"
fi

echo "Documentation built in $ROOT/site"
