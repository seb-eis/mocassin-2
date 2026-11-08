#!/usr/bin/env bash
# Local validation for the MOCASSIN workspace.
#
# This mirrors the affected CI jobs without spending GitHub Actions minutes.
# Run the relevant mode on every feature branch, and `all` before a step
# closeout. The CI workflow only confirms what this script already checked.
set -euo pipefail

cd "$(dirname "$0")/.."

usage() {
  cat <<'EOF'
Usage: tools/verify.sh [mode]

Modes:
  all        Run every check below (default; use before closeout)
  dotnet     Restore, build, test, and format the .NET solution
  api        NuGet licence/vulnerability audit and ApiCompat baseline check
  structure  Run only the code-structure policy test
  rust       Format, clippy, and test the Rust workspace
  python     Test and build the Python package
  docs       Strict documentation build, DocFX, and Rust docs
  contracts  Contract/symmetry drift, licence headers, secret scan, cargo-deny

The script is deliberately local-first. Do not push a step branch just to make
CI run these checks.
EOF
}

run_dotnet() {
  echo "== .NET =="
  dotnet restore Mocassin.slnx
  dotnet build Mocassin.slnx -c Release --no-restore
  dotnet test Mocassin.slnx -c Release --no-build
  dotnet format Mocassin.slnx --verify-no-changes --no-restore
}

run_api() {
  echo "== Public API and dependency gates =="
  dotnet build Mocassin.slnx -c Release
  dotnet tool restore
  python tools/contracts/check_nuget_licenses.py
  output="$(dotnet list Mocassin.slnx package --vulnerable --include-transitive)"
  echo "$output"
  if grep -q "has the following vulnerable packages" <<< "$output"; then
    echo "vulnerable NuGet packages found" >&2
    exit 1
  fi
  ./tools/check-api-compat.sh
}

run_structure() {
  echo "== Structure =="
  dotnet test src/dotnet/Mocassin.Architecture.Tests/Mocassin.Architecture.Tests.csproj -c Release
}

run_rust() {
  echo "== Rust =="
  cargo fmt --manifest-path src/rust/Cargo.toml --all --check
  cargo clippy --manifest-path src/rust/Cargo.toml --workspace --all-targets -- -D warnings
  cargo test --manifest-path src/rust/Cargo.toml --workspace
}

run_python() {
  echo "== Python =="
  python -m pytest src/python/tests
  python -m build src/python
}

run_docs() {
  echo "== Documentation =="
  ./tools/docs/build-docs.sh
}

run_contracts() {
  echo "== Contracts and policy =="
  python tools/contracts/generate.py --check
  if [[ -f tools/symmetry/requirements.txt ]]; then
    python -m pip install -r tools/symmetry/requirements.txt
  fi
  if [[ -f tools/symmetry/generate_space_groups.py ]]; then
    python tools/symmetry/generate_space_groups.py --check
  fi
  python tools/check-license-headers.py
  python tools/check-no-secrets.py
  if command -v cargo-deny >/dev/null 2>&1; then
    cargo deny --manifest-path src/rust/Cargo.toml check
  else
    echo "cargo-deny is not installed; skipping the local dependency-policy check" >&2
  fi
}

mode="${1:-all}"
case "$mode" in
  all)
    run_dotnet
    run_api
    run_rust
    run_python
    run_docs
    run_contracts
    ;;
  dotnet) run_dotnet ;;
  api) run_api ;;
  structure) run_structure ;;
  rust) run_rust ;;
  python) run_python ;;
  docs) run_docs ;;
  contracts) run_contracts ;;
  -h|--help|help) usage ;;
  *)
    echo "Unknown mode: $mode" >&2
    usage >&2
    exit 2
    ;;
esac
