#!/usr/bin/env bash
# Serve the narrative documentation locally with live reload.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT"

if ! command -v zensical >/dev/null 2>&1; then
  echo "error: zensical is not installed; see tools/docs/requirements.txt" >&2
  exit 1
fi

exec zensical serve "$@"
