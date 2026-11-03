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

cd "$SRC/src/McSolver"
cmake -S . -B build -DCMAKE_BUILD_TYPE=Release \
    -DCMAKE_C_FLAGS="-Wno-error=incompatible-pointer-types"
cmake --build ./build --config Release -j"${JOBS:-$(nproc)}"

echo "Solver build complete: $PWD/build/Mocassin.Simulator"
