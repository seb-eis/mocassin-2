#!/usr/bin/env python3
# SPDX-License-Identifier: MIT

"""Scan tracked text files for high-confidence secret patterns."""

from __future__ import annotations

import re
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
SKIP_PARTS = {
    ".git",
    ".venv",
    "artifacts",
    "bin",
    "build",
    "dist",
    "node_modules",
    "obj",
    "site",
    "target",
    "venv",
}
PATTERNS = [
    ("private key", re.compile(r"-----BEGIN (?:RSA |EC |OPENSSH |DSA |PGP )?PRIVATE KEY-----")),
    ("AWS access key", re.compile(r"\bAKIA[0-9A-Z]{16}\b")),
    ("GitHub token", re.compile(r"\bgh[pousr]_[A-Za-z0-9]{36,}\b")),
    ("Slack token", re.compile(r"\bxox[baprs]-[A-Za-z0-9-]{10,}\b")),
    ("Google API key", re.compile(r"\bAIza[0-9A-Za-z_-]{35}\b")),
]


def tracked_files() -> list[Path]:
    """Return tracked files, falling back to a repository walk outside git."""
    try:
        output = subprocess.run(
            ["git", "ls-files", "-z"],
            cwd=ROOT,
            check=True,
            capture_output=True,
        ).stdout
        return [ROOT / item.decode("utf-8") for item in output.split(b"\0") if item]
    except (OSError, subprocess.CalledProcessError):
        return [path for path in ROOT.rglob("*") if path.is_file()]


def is_scannable(path: Path) -> bool:
    """Return whether a path is a non-generated text file worth scanning."""
    if SKIP_PARTS.intersection(path.parts):
        return False
    try:
        path.read_text(encoding="utf-8")
    except (OSError, UnicodeDecodeError):
        return False
    return True


def main() -> int:
    """Report high-confidence secret patterns and return a process status."""
    findings: list[str] = []
    scanned = 0
    for path in tracked_files():
        if not is_scannable(path):
            continue
        scanned += 1
        for line_number, line in enumerate(path.read_text(encoding="utf-8").splitlines(), start=1):
            for label, pattern in PATTERNS:
                if pattern.search(line):
                    findings.append(f"{path.relative_to(ROOT)}:{line_number}: possible {label}")

    if findings:
        print("Possible secrets found:", file=sys.stderr)
        for finding in findings:
            print(f"  {finding}", file=sys.stderr)
        return 1

    print(f"No secret patterns found in {scanned} tracked text files")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
