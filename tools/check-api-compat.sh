#!/usr/bin/env bash
# SPDX-License-Identifier: MIT
#
# Compare the current public .NET API against the checked-in baseline assemblies.
# The PublicAPI analyzers also enforce PublicAPI.Shipped.txt at build time; this
# check is the release-facing ApiCompat gate.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
BASELINE_DIR="$ROOT/tests/contracts/api-baseline/net10.0"
DOTNET_EXE="$(command -v dotnet)"
DOTNET_ROOT="$(dirname "$(readlink -f "$DOTNET_EXE")")"
REF_PACK="$(find "$DOTNET_ROOT/packs/Microsoft.NETCore.App.Ref" -path '*/ref/net10.0' -type d | sort -V | tail -1)"

if [[ -z "$REF_PACK" ]]; then
  echo "error: could not locate the .NET 10 reference pack under $DOTNET_ROOT/packs" >&2
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
