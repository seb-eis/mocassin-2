#!/usr/bin/env bash
# SPDX-License-Identifier: MIT
# Local validation for the MOCASSIN workspace.
#
# This mirrors the affected CI jobs without spending GitHub Actions minutes.
# Run the relevant mode on every feature branch, and `all --require` before a
# step closeout. The CI workflow only confirms what this script already checked.

set -euo pipefail

cd "$(dirname "$0")/.."

usage() {
  cat <<'EOF'
Usage: tools/verify.sh [mode] [--require|--closeout] [--list]

Modes:
  all        Run every check below (default; use before closeout)
  dotnet     Restore, build, test, and format the .NET solution
  api        NuGet licence/vulnerability audit and ApiCompat baseline check
  structure  Run only the code-structure policy test
  rust       Format, clippy, and test the Rust workspace
  python     Test and build the Python package
  docs       Strict documentation build, link check, DocFX, and Rust docs
  contracts  Contract/symmetry drift, process consistency, licence headers,
             secret scan, and cargo-deny
  policy     API and contracts/policy checks (api + contracts)

Options:
  --require   Fail when a required closeout check is unavailable
  --closeout  Alias for `all --require`
  --list      List the selected modes and checks
  --help      Show this help

The script is deliberately local-first. Do not push a step branch just to make
CI run these checks.
EOF
}

mode="all"
require_closeout=false
closeout_requested=false
list_modes=false

for arg in "$@"; do
  case "$arg" in
    all|dotnet|api|structure|rust|python|docs|contracts|policy)
      mode="$arg"
      ;;
    --require)
      require_closeout=true
      ;;
    --closeout)
      closeout_requested=true
      ;;
    --list)
      list_modes=true
      ;;
    -h|--help|help)
      usage
      exit 0
      ;;
    *)
      echo "Unknown argument: $arg" >&2
      usage >&2
      exit 2
      ;;
  esac
done

if [[ "$closeout_requested" == "true" ]]; then
  mode="all"
  require_closeout=true
fi

if [[ "$list_modes" == "true" ]]; then
  echo "Mode: $mode"
  case "$mode" in
    all) echo "  dotnet, api, rust, python, docs, contracts" ;;
    dotnet) echo "  restore, build, test, format" ;;
    api) echo "  licence/vulnerability audit, ApiCompat" ;;
    structure) echo "  architecture structure test" ;;
    rust) echo "  fmt, clippy, test" ;;
    python) echo "  pytest, package build" ;;
    docs) echo "  link check, strict site build, DocFX/rustdoc" ;;
    contracts) echo "  manifest/symmetry drift, process consistency, SPDX, secrets, cargo-deny" ;;
    policy) echo "  api + contracts" ;;
  esac
  exit 0
fi

# Catch whitespace errors before running expensive gates.
if git rev-parse --is-inside-work-tree >/dev/null 2>&1; then
  echo "== git diff --check =="
  if ! git diff --check; then
    echo "Working tree has whitespace errors. Fix them before continuing." >&2
    exit 1
  fi
  if ! git diff --cached --check; then
    echo "Staged files have whitespace errors. Fix them before continuing." >&2
    exit 1
  fi
fi

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
  python tools/check_docs_links.py
  ./tools/docs/build-docs.sh
}

run_contracts() {
  echo "== Contracts and policy =="
  python tools/contracts/generate.py --check
  if [[ -f tools/contracts/requirements.txt ]]; then
    python -m pip install -r tools/contracts/requirements.txt
  fi
  python tools/contracts/check_model_projection.py
  python tools/contracts/check_parameterization_projection.py
  if [[ -f tools/symmetry/requirements.txt ]]; then
    python -m pip install -r tools/symmetry/requirements.txt
  fi
  if [[ -f tools/symmetry/generate_space_groups.py ]]; then
    python tools/symmetry/generate_space_groups.py --check
  fi
  python tools/check_process_consistency.py
  python -m unittest discover -s tools/tests -t .
  python tools/check-license-headers.py
  python tools/check-no-secrets.py
  if command -v cargo-deny >/dev/null 2>&1; then
    cargo deny --manifest-path src/rust/Cargo.toml check
  elif [[ "$require_closeout" == "true" ]]; then
    echo "cargo-deny is required for a closeout gate but is not installed" >&2
    exit 1
  else
    echo "cargo-deny is not installed; skipping the dependency-policy check (not a closeout run)" >&2
  fi
}

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
  policy)
    run_api
    run_contracts
    ;;
  *)
    echo "Unknown mode: $mode" >&2
    usage >&2
    exit 2
    ;;
esac

if [[ "$require_closeout" == "true" ]]; then
  echo
  echo "Closeout gate completed with required checks enabled."
fi
