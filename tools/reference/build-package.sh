#!/usr/bin/env bash
# SPDX-License-Identifier: MIT
set -euo pipefail

if [[ $# -lt 2 ]]; then
    echo "usage: $0 <project.mocprj> <output.msl> [build-name-filter]" >&2
    exit 2
fi

PROJECT="$1"
OUTPUT="$2"
BUILD_FILTER="${3:-KMC}"
WORK="${MOCASSIN_REFERENCE_WORK:-/tmp/opencode/mocassin-reference}"
SRC="$WORK/original"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
JSON="$WORK/project.json"

if [[ ! -f "$PROJECT" ]]; then
    echo "Project database not found: $PROJECT" >&2
    exit 1
fi

if [[ ! -f "$SRC/src/ModelBuilder/Mocassin.UI.Xml/bin/Release/netstandard2.0/Mocassin.UI.Data.dll" ]]; then
    "$SCRIPT_DIR/build-model-builder.sh"
fi

mkdir -p "$WORK" "$(dirname "$OUTPUT")"
sqlite3 -noheader "$PROJECT" "SELECT ProjectJson FROM MocassinProjectGraphs LIMIT 1;" > "$JSON"

if [[ ! -s "$JSON" ]]; then
    echo "Failed to extract ProjectJson from $PROJECT" >&2
    exit 1
fi

dotnet run --project "$SCRIPT_DIR/Mocassin.ReferenceHarness" -c Release \
    -p:ReferenceRoot="$SRC" -- "$JSON" "$OUTPUT" "$BUILD_FILTER"

test -s "$OUTPUT"
echo "Reference package built: $OUTPUT"
