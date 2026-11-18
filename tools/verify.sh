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
  aot        Native AOT smoke host, positive control, and CLI evidence
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
    all|dotnet|api|structure|rust|python|docs|contracts|aot|policy)
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
    all) echo "  dotnet, api, rust, python, docs, contracts, aot" ;;
    dotnet) echo "  restore, build, test, format" ;;
    api) echo "  licence/vulnerability audit, ApiCompat" ;;
    structure) echo "  architecture structure test" ;;
    rust) echo "  fmt, clippy, test" ;;
    python) echo "  pytest, package build" ;;
    docs) echo "  math check, link check, strict site build, DocFX/rustdoc" ;;
    contracts) echo "  manifest/symmetry drift, process consistency, SPDX, secrets, cargo-deny" ;;
    aot) echo "  zero-warning AOT smoke host, IL2026/IL3050 positive control, native smoke run, CLI evidence" ;;
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
  python tools/check_doc_math.py
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
  python tools/contracts/check_lattice_projection.py
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

run_aot() {
  echo "== AOT =="
  # Fail-not-skip: clang/lld are Tier 1 tools and every closeout from Step 12 needs them.
  for tool in clang ld.lld; do
    if ! command -v "$tool" >/dev/null 2>&1; then
      echo "aot: required tool '$tool' not found; install clang/lld 19.1.7 (Tier 1 image)" >&2
      exit 1
    fi
  done

  local aot_root="artifacts/aot"
  rm -rf "$aot_root"
  mkdir -p "$aot_root"
  local smoke_project="tests/aot/SystemCommandLineSmoke/SystemCommandLineSmoke.csproj"
  local control_project="tests/aot/PositiveControl/PositiveControl.csproj"
  local smoke_out="$aot_root/smoke"
  local control_out="$aot_root/control"
  local smoke_log="$aot_root/system-commandline-smoke.log"
  local control_log="$aot_root/positive-control.log"

  # A clean build is required: an incremental publish would suppress the warnings the positive control
  # must emit and could reuse a stale native image.
  dotnet clean "$smoke_project" -c Release -r linux-x64 >/dev/null 2>&1 || true
  dotnet clean "$control_project" -c Release -r linux-x64 >/dev/null 2>&1 || true

  if ! dotnet publish "$smoke_project" -c Release -r linux-x64 \
      -p:PublishAot=true -p:TrimMode=full -p:TrimmerSingleWarn=false \
      -p:SuppressTrimAnalysisWarnings=false -p:SelfContained=true \
      -o "$smoke_out" >"$smoke_log" 2>&1; then
    if grep -q "ILCompiler" "$smoke_log"; then
      echo "aot: Microsoft.DotNet.ILCompiler 11.0.0 could not be restored; check the NuGet feed/cache" >&2
    fi
    tail -40 "$smoke_log" >&2
    echo "aot: the zero-warning smoke host failed to publish" >&2
    exit 1
  fi

  if grep -Eq "warning IL[23][0-9]{3}" "$smoke_log"; then
    echo "aot: the smoke host emitted trim/AOT warnings:" >&2
    grep -E "warning IL[23][0-9]{3}" "$smoke_log" >&2
    exit 1
  fi

  local smoke_binary="$smoke_out/SystemCommandLineSmoke"
  if [[ ! -x "$smoke_binary" ]]; then
    echo "aot: the native smoke binary was not produced at $smoke_binary" >&2
    exit 1
  fi

  if ! dotnet publish "$control_project" -c Release -r linux-x64 \
      -p:PublishAot=true -p:TrimMode=full -p:TrimmerSingleWarn=false \
      -p:SuppressTrimAnalysisWarnings=false -p:SelfContained=true \
      -o "$control_out" >"$control_log" 2>&1; then
    if grep -q "ILCompiler" "$control_log"; then
      echo "aot: Microsoft.DotNet.ILCompiler 11.0.0 could not be restored; check the NuGet feed/cache" >&2
    fi
    tail -40 "$control_log" >&2
    echo "aot: the positive-control project failed to publish" >&2
    exit 1
  fi

  if ! grep -q "IL2026" "$control_log" || ! grep -q "IL3050" "$control_log"; then
    echo "aot: the positive control did not emit both IL2026 and IL3050; the trim/AOT analyzers are not active" >&2
    exit 1
  fi

  local help_log="$aot_root/smoke-help.txt"
  local version_log="$aot_root/smoke-version.txt"
  local parse_log="$aot_root/smoke-parse-error.txt"
  if ! "$smoke_binary" --help >"$help_log" 2>&1; then
    echo "aot: the native smoke host failed '--help'" >&2
    exit 1
  fi
  if ! grep -q "Usage:" "$help_log"; then
    echo "aot: the native smoke host did not print usage for '--help'" >&2
    exit 1
  fi
  if ! "$smoke_binary" --version >"$version_log" 2>&1; then
    echo "aot: the native smoke host failed '--version'" >&2
    exit 1
  fi

  local parse_exit=0
  "$smoke_binary" validate x --bogus >"$parse_log" 2>&1 || parse_exit=$?
  if [[ "$parse_exit" -ne 2 ]]; then
    echo "aot: the native smoke parse-error case exited $parse_exit; expected 2 (CLI001)" >&2
    exit 1
  fi
  if ! grep -q "CLI001" "$parse_log"; then
    echo "aot: the native smoke parse-error case did not emit CLI001" >&2
    exit 1
  fi

  # The real CLI publish is evidence, not a gate: Microsoft.Data.Sqlite and MessagePack are not verified for
  # Native AOT. Step 31 owns the platform matrix and the AOT/single-file decision.
  local cli_log="$aot_root/mocassin-cli.log"
  if dotnet publish src/dotnet/Mocassin.Cli/Mocassin.Cli.csproj -c Release -r linux-x64 \
      -p:PublishAot=true -p:TrimMode=full -p:TrimmerSingleWarn=false \
      -p:SuppressTrimAnalysisWarnings=false -p:SelfContained=true \
      -o "$aot_root/mocassin-cli" >"$cli_log" 2>&1; then
    local cli_warnings
    cli_warnings="$(grep -cE "warning IL[23][0-9]{3}" "$cli_log" || true)"
    echo "aot evidence: Mocassin.Cli published with $cli_warnings IL2xxx/IL3xxx warning(s); not a gate" >&2
    echo "aot evidence: log $cli_log"
  else
    if grep -q "ILCompiler" "$cli_log"; then
      echo "aot: Microsoft.DotNet.ILCompiler 11.0.0 could not be restored; check the NuGet feed/cache" >&2
      exit 1
    fi
    local cli_diagnostics
    cli_diagnostics="$(grep -cE "(warning|error) IL[23][0-9]{3}" "$cli_log" || true)"
    echo "aot evidence: Mocassin.Cli publish failed with $cli_diagnostics IL2xxx/IL3xxx diagnostic(s); recorded as evidence, not a gate (log $cli_log)" >&2
  fi

  echo "== AOT passed =="
}

case "$mode" in
  all)
    run_dotnet
    run_api
    run_rust
    run_python
    run_docs
    run_contracts
    run_aot
    ;;
  dotnet) run_dotnet ;;
  api) run_api ;;
  structure) run_structure ;;
  rust) run_rust ;;
  python) run_python ;;
  docs) run_docs ;;
  contracts) run_contracts ;;
  aot) run_aot ;;
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
