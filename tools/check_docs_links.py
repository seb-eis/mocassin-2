#!/usr/bin/env python3
# SPDX-License-Identifier: MIT
"""Check relative Markdown links and anchors across the repository.

The checker is dependency-free and intended to run locally and in CI. It scans
the root guides and every Markdown file under docs/, ignores external URLs,
fenced code, inline code, and template placeholders, and verifies that relative
files and heading anchors exist.
"""

from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path
from urllib.parse import unquote

ROOT_FILES = ("README.md", "CONTRIBUTING.md", "AGENTS.md", "SECURITY.md", "GLOSSARY.md")
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
EXTERNAL_SCHEMES = ("http://", "https://", "mailto:", "tel:", "data:", "javascript:")
SCHEME_RE = re.compile(r"^[A-Za-z][A-Za-z0-9+.-]*:")
LINK_RE = re.compile(r"!?\[[^\]]*\]\(([^)]+)\)")
FENCE_RE = re.compile(r"^```.*?^```", re.MULTILINE | re.DOTALL)
INLINE_CODE_RE = re.compile(r"`[^`\n]*`")
HEADING_RE = re.compile(r"^(#{1,6})\s+(.+?)\s*#*\s*$", re.MULTILINE)
HTML_ID_RE = re.compile(r"""(?:id|name)=["']([^"']+)["']""")


def _blank_fences(text: str) -> str:
    return FENCE_RE.sub(lambda m: "\n" * m.group(0).count("\n"), text)


def strip_code(text: str) -> str:
    """Remove fenced and inline code while preserving line numbers."""
    text = _blank_fences(text)
    return INLINE_CODE_RE.sub(lambda m: " " * len(m.group(0)), text)


def slugify(heading: str) -> str:
    text = re.sub(r"`([^`]*)`", r"\1", heading)
    text = re.sub(r"<[^>]+>", "", text)
    text = text.strip().lower()
    text = re.sub(r"[^\w\s-]", "", text, flags=re.UNICODE)
    text = re.sub(r"\s+", "-", text)
    return text


def anchors_for(text: str) -> set[str]:
    anchors: set[str] = set()
    seen: dict[str, int] = {}
    for match in HEADING_RE.finditer(_blank_fences(text)):
        slug = slugify(match.group(2))
        count = seen.get(slug, 0)
        anchors.add(slug if count == 0 else f"{slug}-{count}")
        seen[slug] = count + 1
    anchors.update(HTML_ID_RE.findall(text))
    return anchors


def parse_target(raw: str) -> str:
    raw = raw.strip()
    if raw.startswith("<") and ">" in raw:
        return raw[1 : raw.index(">")]
    return raw.split()[0]


def should_skip(target: str) -> bool:
    if not target or target == "#":
        return True
    if "{{" in target or "}}" in target:
        return True
    if "step-NN" in target or "wp-NN" in target or "XC-NN" in target:
        return True
    # Generated API output is copied into the site at build time.
    if target.startswith(("dotnet/", "rust/", "python/")):
        return True
    if target.startswith(EXTERNAL_SCHEMES):
        return True
    if SCHEME_RE.match(target):
        return True
    # Site-root links are generator-specific; do not guess the site root.
    if target.startswith("/"):
        return True
    return False


def iter_markdown(root: Path) -> list[Path]:
    files: list[Path] = []
    for name in ROOT_FILES:
        path = root / name
        if path.is_file():
            files.append(path)
    docs = root / "docs"
    if docs.is_dir():
        for path in docs.rglob("*.md"):
            if any(part in SKIP_DIRS for part in path.relative_to(root).parts):
                continue
            files.append(path)
    return sorted(set(files))


def resolve_target(source: Path, target: str, root: Path) -> Path | None:
    path_part = target.split("?", 1)[0].split("#", 1)[0]
    path_part = unquote(path_part)
    if not path_part:
        return source
    candidate = (source.parent / path_part).resolve()
    if candidate.is_dir():
        for index in ("index.md", "README.md"):
            indexed = candidate / index
            if indexed.is_file():
                return indexed
    return candidate


def check_file(path: Path, root: Path, check_anchors: bool = True) -> list[str]:
    text = path.read_text(encoding="utf-8")
    stripped = strip_code(text)
    errors: list[str] = []
    rel = path.relative_to(root)

    for match in LINK_RE.finditer(stripped):
        raw = match.group(1)
        target = parse_target(raw)
        if should_skip(target):
            continue

        line = stripped.count("\n", 0, match.start()) + 1
        resolved = resolve_target(path, target, root)
        if resolved is None:
            continue
        if not resolved.exists():
            errors.append(f"{rel}:{line}: missing link target: {target}")
            continue
        if not check_anchors or "#" not in target or resolved.suffix.lower() != ".md":
            continue

        anchor = unquote(target.split("#", 1)[1])
        if not anchor:
            continue
        anchors = anchors_for(resolved.read_text(encoding="utf-8"))
        if anchor not in anchors:
            errors.append(f"{rel}:{line}: missing anchor '#{anchor}' in {target}")

    return errors


def check_repository(root: Path, check_anchors: bool = True) -> list[str]:
    errors: list[str] = []
    for path in iter_markdown(root):
        errors.extend(check_file(path, root, check_anchors))
    return errors


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=Path.cwd(), help="Repository root (default: current directory)")
    parser.add_argument("--no-anchors", action="store_true", help="Skip heading-anchor validation")
    args = parser.parse_args(argv)

    root = args.root.resolve()
    errors = check_repository(root, check_anchors=not args.no_anchors)
    if errors:
        print("Broken documentation links:", file=sys.stderr)
        for error in errors:
            print(f"  {error}", file=sys.stderr)
        return 1

    print(f"Documentation links OK ({len(iter_markdown(root))} files scanned).")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
