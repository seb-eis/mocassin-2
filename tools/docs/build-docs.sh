#!/usr/bin/env bash
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
  echo "==> DocFX"
  dotnet tool restore
  rm -rf artifacts/docfx
  dotnet docfx tools/docfx/docfx.json -o artifacts/docfx
  mkdir -p site/api
  rm -rf site/api/dotnet
  cp -R artifacts/docfx site/api/dotnet
else
  echo "==> DocFX skipped (solution or tools/docfx/docfx.json not present yet)"
fi

if [[ -f src/rust/Cargo.toml ]] && command -v cargo >/dev/null 2>&1; then
  echo "==> rustdoc"
  cargo doc --manifest-path src/rust/Cargo.toml --workspace --no-deps --target-dir target/doc
  mkdir -p site/api
  rm -rf site/api/rust
  cp -R target/doc site/api/rust
else
  echo "==> rustdoc skipped (Rust workspace not present yet)"
fi

echo "Documentation built in $ROOT/site"
