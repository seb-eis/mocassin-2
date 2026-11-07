#!/usr/bin/env bash
# SPDX-License-Identifier: MIT
#
# Refresh the checked-in ApiCompat baseline assemblies after an intentional
# public API change. Review the generated diff before committing it.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
DEST="$ROOT/tests/contracts/api-baseline/net10.0"
PROJECTS=(
  Mocassin.Core
  Mocassin.Symmetry
  Mocassin.Compilation
  Mocassin.Contracts
  Mocassin.Packages
  Mocassin.Results
  Mocassin.App.ViewModels
  Mocassin.App.Views
  Mocassin.Rendering
)

dotnet build "$ROOT/Mocassin.slnx" -c Release
rm -rf "$DEST"
mkdir -p "$DEST"

for project in "${PROJECTS[@]}"; do
  # Prefer the reference assembly: it carries the public surface without implementation
  # code or embedded data resources, keeping the checked-in baselines small.
  reference="$ROOT/src/dotnet/$project/obj/Release/net10.0/ref/$project.dll"
  if [[ -f "$reference" ]]; then
    cp "$reference" "$DEST/$project.dll"
  else
    cp "$ROOT/src/dotnet/$project/bin/Release/net10.0/$project.dll" "$DEST/$project.dll"
  fi
done

echo "Updated ApiCompat baseline in $DEST"
