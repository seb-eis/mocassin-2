#!/usr/bin/env bash
# SPDX-License-Identifier: MIT
#
# Compare the current public .NET API against the checked-in baseline assemblies.
# The PublicAPI analyzers also enforce PublicAPI.Shipped.txt at build time; this
# check is the release-facing ApiCompat gate.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
BASELINE_DIR="$ROOT/tests/contracts/api-baseline/net10.0"
PROJECTS_FILE="$ROOT/tools/api-projects.txt"
DOTNET_EXE="$(command -v dotnet)"
DOTNET_ROOT="$(dirname "$(readlink -f "$DOTNET_EXE")")"
REF_PACK="$(find "$DOTNET_ROOT/packs/Microsoft.NETCore.App.Ref" -path '*/ref/net10.0' -type d | sort -V | tail -1)"

if [[ -z "$REF_PACK" ]]; then
  echo "error: could not locate the .NET 10 reference pack under $DOTNET_ROOT/packs" >&2
  exit 1
fi

if [[ ! -f "$PROJECTS_FILE" ]]; then
  echo "error: missing project list $PROJECTS_FILE" >&2
  exit 1
fi

mapfile -t projects < <(grep -vE '^[[:space:]]*(#|$)' "$PROJECTS_FILE")
if [[ ${#projects[@]} -eq 0 ]]; then
  echo "error: no projects listed in $PROJECTS_FILE" >&2
  exit 1
fi

shopt -s nullglob
baselines=("$BASELINE_DIR"/*.dll)
if [[ ${#baselines[@]} -eq 0 ]]; then
  echo "error: no API baseline assemblies in $BASELINE_DIR" >&2
  exit 1
fi

for baseline in "${baselines[@]}"; do
  name="$(basename "$baseline" .dll)"
  if ! printf '%s\n' "${projects[@]}" | grep -Fxq "$name"; then
    echo "error: baseline $baseline is not listed in $PROJECTS_FILE" >&2
    exit 1
  fi
done

for name in "${projects[@]}"; do
  baseline="$BASELINE_DIR/$name.dll"
  if [[ ! -f "$baseline" ]]; then
    echo "error: missing baseline $baseline; run tools/update-api-baseline.sh" >&2
    exit 1
  fi

  current="$ROOT/src/dotnet/$name/bin/Release/net10.0/$name.dll"
  if [[ ! -f "$current" ]]; then
    echo "error: missing current assembly $current; run dotnet build -c Release first" >&2
    exit 1
  fi

  echo "==> ApiCompat $name"
  dotnet apicompat \
    --left "$baseline" \
    --right "$current" \
    --left-assembly-references "$BASELINE_DIR,$REF_PACK" \
    --right-assembly-references "$(dirname "$current"),$REF_PACK"
done

echo "Public API is compatible with the checked-in baseline."
