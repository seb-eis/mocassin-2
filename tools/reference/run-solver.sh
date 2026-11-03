#!/usr/bin/env bash
set -euo pipefail

if [[ $# -lt 3 ]]; then
    echo "usage: $0 <package.msl> <job-id> <io-directory>" >&2
    exit 2
fi

MSL="$1"
JOB="$2"
IO="$3"
WORK="${MOCASSIN_REFERENCE_WORK:-/tmp/opencode/mocassin-reference}"
SOLVER="$WORK/original/src/McSolver/build/Mocassin.Simulator"

if [[ ! -x "$SOLVER" ]]; then
    echo "Solver not built: $SOLVER (run build-solver.sh first)" >&2
    exit 1
fi

mkdir -p "$IO"
"$SOLVER" -dbPath "$MSL" -jobId "$JOB" -ioPath "$IO"

echo "Reference state files:"
ls -lh "$IO/run.mcs" "$IO/prerun.mcs" 2>/dev/null || true
