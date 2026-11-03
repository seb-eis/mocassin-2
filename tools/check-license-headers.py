#!/usr/bin/env python3
# SPDX-License-Identifier: MIT

"""Verify that source files in the MIT implementation carry the SPDX header."""

from __future__ import annotations

import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
SOURCE_GLOBS = ("src/dotnet/**/*.cs", "src/rust/**/*.rs", "src/python/**/*.py")
SKIP_PARTS = {"bin", "obj", "target", "build", "dist", ".venv", "venv"}


def source_files() -> list[Path]:
    """Return the source files that must carry the MIT SPDX header."""
    files: list[Path] = []
    for pattern in SOURCE_GLOBS:
        files.extend(ROOT.glob(pattern))
    return sorted(
        path
        for path in files
        if not SKIP_PARTS.intersection(path.parts)
    )


def main() -> int:
    """Report source files with a missing or wrong SPDX identifier."""
    missing: list[Path] = []
    for path in source_files():
        first_line = path.read_text(encoding="utf-8").splitlines()[0] if path.stat().st_size else ""
        if "SPDX-License-Identifier: MIT" not in first_line:
            missing.append(path.relative_to(ROOT))

    if missing:
        print("Missing 'SPDX-License-Identifier: MIT' header:", file=sys.stderr)
        for path in missing:
            print(f"  {path}", file=sys.stderr)
        return 1

    print(f"SPDX headers present in {len(source_files())} source files")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
