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
  cp "$ROOT/src/dotnet/$project/bin/Release/net10.0/$project.dll" "$DEST/$project.dll"
done

echo "Updated ApiCompat baseline in $DEST"
