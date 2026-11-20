#!/usr/bin/env python3
# SPDX-License-Identifier: MIT
"""Validate step closeout evidence before the audit-freeze merge.

The evidence file ties the audited commit, the closeout CI head SHA, the main
SHA at freeze, and the merge candidate together. It catches the common failure
where the audit record, the CI run, and the merge candidate drift apart.
"""

from __future__ import annotations

import argparse
import json
import re
import subprocess
import sys
from pathlib import Path

SHA_RE = re.compile(r"^[0-9a-f]{40}$")
# A step is `step-NN`, or `step-NN[a-z]` for a split step (for example `step-23a`).
STEP_RE = re.compile(r"^step-\d{2}[a-z]?$")
DOC_PREFIXES = ("docs/",)
DOC_FILES = ("AGENTS.md", "README.md", "CONTRIBUTING.md", "SECURITY.md")


def git(root: Path, *args: str) -> str:
    result = subprocess.run(
        ["git", "-C", str(root), *args],
        check=True,
        capture_output=True,
        text=True,
    )
    return result.stdout.strip()


def commit_exists(root: Path, sha: str) -> bool:
    try:
        git(root, "cat-file", "-e", f"{sha}^{{commit}}")
        return True
    except subprocess.CalledProcessError:
        return False


def is_ancestor(root: Path, ancestor: str, descendant: str) -> bool:
    result = subprocess.run(
        ["git", "-C", str(root), "merge-base", "--is-ancestor", ancestor, descendant],
        capture_output=True,
    )
    return result.returncode == 0


def check(root: Path, evidence_path: Path) -> list[str]:
    errors: list[str] = []
    try:
        data = json.loads(evidence_path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        return [f"cannot read evidence file: {exc}"]

    required = ("step", "audit_commit", "closeout_ci", "main_sha_at_freeze", "merge_candidate")
    for key in required:
        if key not in data:
            errors.append(f"missing field: {key}")
    if errors:
        return errors

    if not STEP_RE.match(str(data["step"])):
        errors.append(f"step is not a step-NN or step-NN[a-z] identifier: {data['step']!r}")

    audit = data["audit_commit"]
    closeout = data["closeout_ci"]
    closeout_sha = closeout.get("head_sha", "") if isinstance(closeout, dict) else ""
    main_sha = data["main_sha_at_freeze"]
    candidate = data["merge_candidate"]
    merge_tree = data.get("merge_tree_ci") or {}

    invalid_sha = False
    for name, sha in (
        ("audit_commit", audit),
        ("closeout_ci.head_sha", closeout_sha),
        ("main_sha_at_freeze", main_sha),
        ("merge_candidate", candidate),
    ):
        if not SHA_RE.match(sha or ""):
            errors.append(f"{name} is not a full 40-character commit SHA: {sha!r}")
            invalid_sha = True
        elif not commit_exists(root, sha):
            errors.append(f"{name} does not resolve to a commit: {sha}")
            invalid_sha = True

    if invalid_sha:
        return errors

    if audit != closeout_sha:
        errors.append("audit_commit and closeout_ci.head_sha differ")
    if not isinstance(closeout, dict) or not closeout.get("run_id"):
        errors.append("closeout_ci.run_id is required")

    if SHA_RE.match(audit or "") and SHA_RE.match(candidate or ""):
        if not is_ancestor(root, audit, candidate):
            errors.append("audit_commit is not an ancestor of merge_candidate")

    if SHA_RE.match(candidate or "") and SHA_RE.match(main_sha or ""):
        changed = git(root, "diff", "--name-only", f"{audit}..{candidate}").splitlines()
        code_changes = [
            path for path in changed if path not in DOC_FILES and not path.startswith(DOC_PREFIXES)
        ]
        if code_changes:
            merge_sha = merge_tree.get("head_sha", "") if isinstance(merge_tree, dict) else ""
            if merge_sha != candidate:
                errors.append(
                    "merge_candidate changes files outside docs/ but merge_tree_ci.head_sha does not match it"
                )
            if not isinstance(merge_tree, dict) or not merge_tree.get("run_id"):
                errors.append("merge_tree_ci.run_id is required when the merge candidate changes code")

        if not is_ancestor(root, main_sha, candidate):
            errors.append("main_sha_at_freeze is not an ancestor of merge_candidate")

    return errors


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("evidence", type=Path, help="closeout evidence JSON file")
    parser.add_argument("--root", type=Path, default=Path.cwd(), help="repository root")
    args = parser.parse_args(argv)

    root = args.root.resolve()
    try:
        git(root, "rev-parse", "--show-toplevel")
    except (OSError, subprocess.CalledProcessError):
        print(f"Not a git repository: {root}", file=sys.stderr)
        return 2

    errors = check(root, args.evidence.resolve())
    if errors:
        print("Closeout evidence problems:", file=sys.stderr)
        for error in errors:
            print(f"  {error}", file=sys.stderr)
        return 1

    print("Closeout evidence: OK")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
