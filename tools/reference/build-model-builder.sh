#!/usr/bin/env bash
set -euo pipefail

ORIGINAL="${MOCASSIN_ORIGINAL:-/workspace/mocassin-original}"
WORK="${MOCASSIN_REFERENCE_WORK:-/tmp/opencode/mocassin-reference}"
SRC="$WORK/original"

if [[ ! -d "$ORIGINAL" ]]; then
    echo "Original MOCASSIN clone not found: $ORIGINAL" >&2
    exit 1
fi

mkdir -p "$WORK"
if [[ ! -d "$SRC" ]]; then
    echo "Copying original MOCASSIN to $SRC"
    rsync -a --exclude '.git' "$ORIGINAL/" "$SRC/"
fi

cd "$SRC/src/ModelBuilder"

echo "Applying EF Core 3.1.32 / SQLitePCLRaw 2.1.2 compatibility pins to work copy"
find . -name '*.csproj' -print0 | xargs -0 sed -i \
    -e 's/Version="3.1.6"/Version="3.1.32"/g' \
    -e 's/SQLitePCLRaw.bundle_e_sqlite3" Version="2.0.4"/SQLitePCLRaw.bundle_e_sqlite3" Version="2.1.2"/g'

find . -type d \( -name bin -o -name obj \) -prune -exec rm -rf {} +

dotnet build Mocassin.Tools.Evaluation/Mocassin.Tools.Evaluation.csproj -c Release --nologo

echo "Model builder build complete: $PWD/Mocassin.Tools.Evaluation/bin/Release/netstandard2.0"
