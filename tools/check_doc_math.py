#!/usr/bin/env python3
# SPDX-License-Identifier: MIT
"""Check documentation math delimiters.

Published documentation under ``docs/`` (except ``docs/planning/``) renders
math with KaTeX through ``docs/javascripts/katex.js``. That initializer
supports ``$$...$$`` for display math and ``\\(...\\)`` for inline math; the
single-dollar ``$...$`` delimiter is intentionally disabled.

Planning records under ``docs/planning/`` are read outside the Zensical build.
They use ``$...$`` for inline math and ``$$...$$`` for display math.

The checker warns about convention violations. Warnings are not fatal by
default because a wrong delimiter is an inconvenience, not a build failure.
Pass ``--strict`` to turn warnings into a non-zero exit code.
"""

from __future__ import annotations

import argparse
import re
import sys
from dataclasses import dataclass
from pathlib import Path

SKIP_DIRS = {
    ".git",
    ".tmp",
    "node_modules",
    "site",
    "dist",
    "build",
    "target",
    "bin",
    "obj",
    "artifacts",
    ".venv",
    "__pycache__",
}

FENCE_START_RE = re.compile(r"^\s*(`{3,}|~{3,})")
FENCE_END_RE = re.compile(r"^\s*(`{3,}|~{3,})\s*$")
INLINE_CODE_RE = re.compile(r"(`+)(.*?)\1")
HTML_COMMENT_RE = re.compile(r"<!--.*?-->", re.DOTALL)
BACKSLASH_DELIMITER_RE = re.compile(r"(?<!\\)\\[()\[\]]")
SINGLE_DOLLAR_RE = re.compile(r"(?<!\\)(?<!\$)\$(?!\$)([^$\n]+?)(?<!\\)\$(?!\$)")
MATH_HINT_RE = re.compile(r"[\\_^{}=+\-*/<>()\[\]]")
GREEK_RE = re.compile(r"[\u0370-\u03ff\u1f00-\u1fff]")


@dataclass(frozen=True)
class MathWarning:
    """A single delimiter-convention warning."""

    path: str
    line: int
    message: str


def _blank_line(line: str) -> str:
    return "\n" if line.endswith("\n") else ""


def strip_fences(text: str) -> str:
    """Blank fenced code blocks while preserving line numbers."""
    lines = text.splitlines(keepends=True)
    result: list[str] = []
    fence_marker: str | None = None

    for line in lines:
        if fence_marker is None:
            match = FENCE_START_RE.match(line)
            if match is None:
                result.append(line)
                continue
            fence_marker = match.group(1)
            result.append(_blank_line(line))
            continue

        result.append(_blank_line(line))
        end = FENCE_END_RE.match(line)
        if end is not None and end.group(1)[0] == fence_marker[0] and len(end.group(1)) >= len(fence_marker):
            fence_marker = None

    return "".join(result)


def strip_html_comments(text: str) -> str:
    """Blank HTML comments while preserving line numbers."""
    return HTML_COMMENT_RE.sub(lambda match: "\n" * match.group(0).count("\n"), text)


def strip_inline_code(text: str) -> str:
    """Blank inline code spans while preserving line numbers."""
    return INLINE_CODE_RE.sub(lambda match: " " * len(match.group(0)), text)


def strip_code(text: str) -> str:
    """Remove fenced code, HTML comments, and inline code."""
    return strip_inline_code(strip_html_comments(strip_fences(text)))


def iter_markdown(root: Path) -> list[Path]:
    """Return Markdown files under ``docs/`` in stable order."""
    docs = root / "docs"
    if not docs.is_dir():
        return []
    files: list[Path] = []
    for path in docs.rglob("*.md"):
        if any(part in SKIP_DIRS for part in path.relative_to(root).parts):
            continue
        files.append(path)
    return sorted(files)


def is_planning(path: Path, root: Path) -> bool:
    """Return True when the file is a planning record."""
    parts = path.relative_to(root).parts
    return len(parts) >= 2 and parts[0] == "docs" and parts[1] == "planning"


def looks_like_math(content: str) -> bool:
    """Return True when a ``$...$`` span is likely math rather than currency."""
    text = content.strip()
    if not text:
        return False
    if MATH_HINT_RE.search(text) or GREEK_RE.search(text):
        return True
    if len(text) == 1 and text.isalpha():
        return True
    tokens = text.split()
    return len(tokens) > 1 and all(len(token) == 1 and token.isalpha() for token in tokens)


def _line_number(text: str, offset: int) -> int:
    return text.count("\n", 0, offset) + 1


def check_file(path: Path, root: Path) -> list[MathWarning]:
    """Check one Markdown file and return its warnings."""
    text = path.read_text(encoding="utf-8")
    clean = strip_code(text)
    relative = path.relative_to(root).as_posix()
    planning = is_planning(path, root)
    warnings: dict[tuple[int, str], MathWarning] = {}

    for match in BACKSLASH_DELIMITER_RE.finditer(clean):
        delimiter = match.group(0)[1]
        if planning:
            message = "planning docs use $...$ / $$...$$; found a backslash delimiter"
        elif delimiter in "[]":
            message = "published docs use $$...$$ for display; found \\[...\\]"
        else:
            continue
        line = _line_number(clean, match.start())
        warnings.setdefault((line, message), MathWarning(relative, line, message))

    if not planning:
        for match in SINGLE_DOLLAR_RE.finditer(clean):
            if not looks_like_math(match.group(1)):
                continue
            message = "published docs use \\(...\\) for inline math; found $...$"
            line = _line_number(clean, match.start())
            warnings.setdefault((line, message), MathWarning(relative, line, message))

    return list(warnings.values())


def check_repository(root: Path) -> list[MathWarning]:
    """Check every documentation file under ``docs/``."""
    warnings: list[MathWarning] = []
    for path in iter_markdown(root):
        warnings.extend(check_file(path, root))
    return warnings


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=Path.cwd(), help="Repository root (default: current directory)")
    parser.add_argument(
        "--strict",
        action="store_true",
        help="Return a non-zero exit code when warnings are found (not used by verify.sh).",
    )
    args = parser.parse_args(argv)

    root = args.root.resolve()
    files = iter_markdown(root)
    warnings = check_repository(root)

    if not warnings:
        print(f"Documentation math conventions OK ({len(files)} files scanned).")
        return 0

    print("Documentation math convention warnings (not fatal by default):", file=sys.stderr)
    for warning in warnings:
        print(f"  {warning.path}:{warning.line}: {warning.message}", file=sys.stderr)
    print(f"{len(warnings)} warning(s) in {len({warning.path for warning in warnings})} file(s).", file=sys.stderr)
    return 1 if args.strict else 0


if __name__ == "__main__":
    raise SystemExit(main())
