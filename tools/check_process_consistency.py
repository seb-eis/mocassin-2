#!/usr/bin/env python3
# SPDX-License-Identifier: MIT
"""Check process documentation and templates for common drift."""

from __future__ import annotations

import re
import sys
from pathlib import Path

REQUIRED_FILES = (
    "AGENTS.md",
    "docs/development/agent-workflow.md",
    "docs/development/orchestration.md",
    "docs/development/contributing.md",
    "docs/development/drift-checks.md",
    "docs/development/incident-and-rollback.md",
    "docs/development/agent-security.md",
    "docs/development/index.md",
    "docs/planning/README.md",
    "docs/planning/04-roadmap.md",
    "docs/planning/status.md",
    "docs/planning/reviews/README.md",
    "docs/planning/reviews/step-design-review-template.md",
    "docs/planning/reviews/cross-cutting/README.md",
    "docs/planning/reviews/cross-cutting/cross-cutting-review-template.md",
    "docs/planning/audits/README.md",
    "docs/planning/audits/step-audit-template.md",
    "docs/planning/closeout/README.md",
    "docs/planning/closeout/_template.json",
    "docs/planning/drift-checks/_template.md",
    "docs/planning/incidents/_template.md",
    "tools/check_closeout_evidence.py",
    "tools/check_docs_links.py",
    "tools/verify.sh",
    "opencode.json",
    ".opencode/agents/lead.md",
    ".opencode/agents/design.md",
    ".opencode/agents/dev-free.md",
    ".opencode/agents/dev.md",
    ".opencode/agents/review.md",
    ".opencode/agents/senior.md",
    ".opencode/commands/step-start.md",
    ".opencode/commands/step-closeout.md",
    ".opencode/commands/grill.md",
    ".opencode/commands/handoff.md",
    ".opencode/commands/retro.md",
    ".opencode/skills/codebase-design/SKILL.md",
    ".opencode/skills/diagnosing-bugs/SKILL.md",
    ".opencode/skills/domain-modeling/SKILL.md",
    ".opencode/skills/grilling/SKILL.md",
    ".opencode/skills/prototype/SKILL.md",
    ".opencode/skills/research/SKILL.md",
    ".opencode/skills/review-axes/SKILL.md",
    ".opencode/skills/tdd/SKILL.md",
    "GLOSSARY.md",
    "docs/development/codebase-design.md",
    "docs/development/diagnosing-bugs.md",
    "docs/development/domain-language.md",
    "docs/development/discovery.md",
    "docs/development/prototypes.md",
    "docs/development/research.md",
    "docs/development/review-axes.md",
    "docs/development/tdd.md",
    "docs/development/retrospectives.md",
    "docs/development/senior-consultation.md",
    "docs/planning/research/README.md",
    "docs/planning/research/_template.md",
    "docs/planning/prototypes/README.md",
    "docs/planning/prototypes/_template.md",
    "docs/planning/retrospectives/README.md",
    "docs/planning/retrospectives/_template.md",
    "docs/planning/reviews/senior/README.md",
    "docs/planning/reviews/senior/_template.md",
)

REQUIRED_INTEGRATION = (
    ("AGENTS.md", "closeout evidence"),
    ("AGENTS.md", "merge-tree"),
    ("AGENTS.md", "drift checks"),
    ("AGENTS.md", "agent security"),
    ("AGENTS.md", "incident and rollback"),
    ("AGENTS.md", "step sizing"),
    ("docs/planning/status.md", "In audit"),
    ("docs/planning/status.md", "Drift checks"),
    ("docs/planning/audits/step-audit-template.md", "closeout evidence file"),
    ("docs/planning/audits/step-audit-template.md", "merge candidate"),
    ("docs/planning/audits/step-audit-template.md", "Closeout evidence"),
    ("docs/planning/audits/step-audit-template.md", "Record state"),
    ("docs/planning/reviews/step-design-review-template.md", "Record state"),
    ("docs/planning/reviews/step-design-review-template.md", "Reviewer independence"),
    ("docs/planning/reviews/cross-cutting/cross-cutting-review-template.md", "Record state"),
    ("docs/planning/reviews/cross-cutting/README.md", "drift checks"),
    ("docs/planning/04-roadmap.md", "split trigger"),
    ("docs/development/agent-workflow.md", "Changing the process"),
    ("AGENTS.md", ".opencode/agents"),
    ("docs/development/orchestration.md", ".opencode/agents"),
    ("docs/development/orchestration.md", "default_agent"),
    ("AGENTS.md", "output contract"),
    ("AGENTS.md", "senior consultation"),
    ("AGENTS.md", "`policy-*.md`"),
    ("AGENTS.md", "Senior consultant"),
    ("docs/development/senior-consultation.md", "two completed standard rounds"),
    ("docs/development/senior-consultation.md", "policy-update review"),
    ("docs/development/review-axes.md", "Standards"),
    ("docs/development/review-axes.md", "Spec"),
    ("docs/planning/reviews/step-design-review-template.md", "| Axis |"),
    ("docs/planning/audits/step-audit-template.md", "| Axis |"),
    ("docs/planning/reviews/cross-cutting/cross-cutting-review-template.md", "| Axis |"),
    (".opencode/agents/review.md", "policy-*.md"),
    (".opencode/agents/lead.md", "senior"),
    ("docs/development/model-economics.md", "default"),
    ("docs/development/model-economics.md", "senior"),
    ("docs/development/orchestration.md", "`policy-*.md`"),
    ("docs/development/orchestration.md", "Senior consultant"),
    ("docs/development/agent-workflow.md", "senior consultation"),
    ("docs/planning/reviews/README.md", "policy-update"),
    ("docs/planning/reviews/README.md", "senior-consultation.md"),
)

FORBIDDEN_TERMS = (
    "design foundations",
    "repository policy",
    "Each feature squash",
    "normal feature approval",
    "Feature approval:",
)

NORMATIVE_FILES = (
    "AGENTS.md",
    "docs/development/agent-workflow.md",
    "docs/development/orchestration.md",
    "docs/development/contributing.md",
    "docs/development/code-structure.md",
    "docs/planning/README.md",
    "docs/planning/04-roadmap.md",
    "docs/planning/status.md",
    "docs/planning/reviews/README.md",
    "docs/planning/reviews/step-design-review-template.md",
    "docs/planning/reviews/cross-cutting/README.md",
    "docs/planning/reviews/cross-cutting/cross-cutting-review-template.md",
    "docs/planning/audits/README.md",
    "docs/planning/audits/step-audit-template.md",
    "docs/planning/closeout/README.md",
    "GLOSSARY.md",
    "docs/development/codebase-design.md",
    "docs/development/diagnosing-bugs.md",
    "docs/development/domain-language.md",
    "docs/development/discovery.md",
    "docs/development/prototypes.md",
    "docs/development/research.md",
    "docs/development/review-axes.md",
    "docs/development/tdd.md",
    "docs/development/retrospectives.md",
    "docs/development/senior-consultation.md",
    "docs/planning/research/README.md",
    "docs/planning/research/_template.md",
    "docs/planning/prototypes/README.md",
    "docs/planning/prototypes/_template.md",
    "docs/planning/retrospectives/README.md",
    "docs/planning/retrospectives/_template.md",
    "docs/planning/reviews/senior/README.md",
    "docs/planning/reviews/senior/_template.md",
)

VERDICT_FILES = (
    "docs/planning/reviews/step-design-review-template.md",
    "docs/planning/audits/step-audit-template.md",
    "docs/planning/reviews/cross-cutting/cross-cutting-review-template.md",
)

VERDICT_STATES = ("Re-review required", "Re-audit required")
STEP_ID_RE = re.compile(r"\d{2}[a-z]?")
AUDIT_RE = re.compile(r"^docs/planning/audits/step-(\d{2}[a-z]?)\.md$")
GATE_RE = re.compile(r"GATE_[A-Z_]+")
TIER_VOCABULARY_PAGES = (
    "docs/development/model-economics.md",
    "docs/development/orchestration.md",
    "docs/development/design-proportionality.md",
    "docs/development/agent-workflow.md",
)
AGENT_DIR = ".opencode/agents"
RETIRED_TIER_RE = re.compile(r"\b(?:workhorse|strong(?:[ -]tier)?)\b", re.IGNORECASE)
FINDINGS_COLUMNS = (
    (
        "docs/planning/reviews/step-design-review-template.md",
        "| ID | Axis | Severity | Origin | Finding | Evidence | Recommendation |",
    ),
    (
        "docs/planning/audits/step-audit-template.md",
        "| ID | Axis | Severity | Origin | Finding | Evidence | Recommendation |",
    ),
    (
        "docs/planning/reviews/cross-cutting/cross-cutting-review-template.md",
        "| ID | Axis | Severity | Area | Finding | Evidence | Recommendation |",
    ),
)


def iter_markdown(root: Path) -> list[Path]:
    files = [root / "AGENTS.md", root / "README.md", root / "CONTRIBUTING.md", root / "SECURITY.md"]
    docs = root / "docs"
    if docs.is_dir():
        files.extend(sorted(docs.rglob("*.md")))
    return [path for path in files if path.is_file()]


def check_required_files(root: Path) -> list[str]:
    return [f"missing required file: {rel}" for rel in REQUIRED_FILES if not (root / rel).is_file()]


def check_integration(root: Path) -> list[str]:
    errors: list[str] = []
    for rel, needle in REQUIRED_INTEGRATION:
        path = root / rel
        if not path.is_file():
            errors.append(f"missing required file: {rel}")
            continue
        if needle.lower() not in path.read_text(encoding="utf-8").lower():
            errors.append(f"{rel} does not mention required integration text: {needle}")
    return errors


def check_verdict_lists(root: Path) -> list[str]:
    errors: list[str] = []
    for rel in VERDICT_FILES:
        path = root / rel
        if not path.is_file():
            continue
        in_verdict = False
        for number, line in enumerate(path.read_text(encoding="utf-8").splitlines(), start=1):
            if line.startswith("## Verdict"):
                in_verdict = True
                continue
            if in_verdict and line.startswith("## "):
                in_verdict = False
            if in_verdict:
                for state in VERDICT_STATES:
                    if line.strip().startswith("- [ ]") and state in line:
                        errors.append(f"{rel}:{number}: verdict list contains state '{state}'")
    return errors


def check_forbidden_terms(root: Path) -> list[str]:
    errors: list[str] = []
    for rel in NORMATIVE_FILES:
        path = root / rel
        if not path.is_file():
            continue
        text = path.read_text(encoding="utf-8")
        for term in FORBIDDEN_TERMS:
            if term in text:
                errors.append(f"{rel}: contains stale process term '{term}'")
    return errors


def check_status_vocabulary(root: Path) -> list[str]:
    path = root / "docs/planning/status.md"
    text = path.read_text(encoding="utf-8")
    missing = [value for value in ("Not started", "In progress", "Blocked", "Awaiting approval", "In audit", "Done", "Deferred") if value not in text]
    return [f"status.md is missing status value: {value}" for value in missing]


def check_unused_audit_records(root: Path) -> list[str]:
    status = (root / "docs/planning/status.md").read_text(encoding="utf-8")
    step_status: dict[str, str] = {}
    for line in status.splitlines():
        cells = [cell.strip() for cell in line.strip().strip("|").split("|")]
        if len(cells) >= 4 and STEP_ID_RE.fullmatch(cells[0]):
            step_status[cells[0]] = cells[3]

    errors: list[str] = []
    audits = root / "docs/planning/audits"
    if not audits.is_dir():
        return errors
    for path in sorted(audits.glob("step-*.md")):
        match = AUDIT_RE.match(str(path.relative_to(root)))
        if not match:
            continue
        step = match.group(1)
        if step_status.get(step) == "Not started":
            errors.append(f"{path.relative_to(root)} is pre-seeded for a step that is still Not started")
    return errors


def check_verify_script(root: Path) -> list[str]:
    path = root / "tools/verify.sh"
    if not path.is_file():
        return ["missing tools/verify.sh"]
    text = path.read_text(encoding="utf-8")
    errors: list[str] = []
    for needle in ("--require", "--closeout", "check_process_consistency.py", "check_docs_links.py", "git diff --check"):
        if needle not in text:
            errors.append(f"tools/verify.sh does not contain required gate text: {needle}")
    return errors


def check_closeout_template(root: Path) -> list[str]:
    path = root / "docs/planning/closeout/_template.json"
    if not path.is_file():
        return ["missing closeout template"]
    text = path.read_text(encoding="utf-8")
    required = ("\"step\"", "\"audit_commit\"", "\"closeout_ci\"", "\"main_sha_at_freeze\"", "\"merge_candidate\"", "\"merge_tree_ci\"")
    return [f"closeout template is missing field: {field}" for field in required if field not in text]


def findings_header(path: Path) -> str | None:
    """Return the first table header after a '## Findings' heading."""
    in_findings = False
    for line in path.read_text(encoding="utf-8").splitlines():
        stripped = line.strip()
        if stripped == "## Findings":
            in_findings = True
            continue
        if not in_findings:
            continue
        if stripped.startswith("|"):
            return stripped
        if stripped.startswith("## "):
            return None
    return None


def check_findings_columns(root: Path) -> list[str]:
    errors: list[str] = []
    for rel, expected in FINDINGS_COLUMNS:
        path = root / rel
        if not path.is_file():
            errors.append(f"missing required file: {rel}")
            continue
        header = findings_header(path)
        if header is None:
            errors.append(f"{rel}: no findings table under '## Findings'")
        elif header != expected:
            errors.append(f"{rel}: findings table header is '{header}', expected '{expected}'")
    return errors


def check_tier_vocabulary(root: Path) -> list[str]:
    errors: list[str] = []
    files = list(TIER_VOCABULARY_PAGES)
    agents = root / AGENT_DIR
    if agents.is_dir():
        files.extend(sorted(str(path.relative_to(root)) for path in agents.glob("*.md")))

    for rel in files:
        path = root / rel
        if not path.is_file():
            continue
        for number, line in enumerate(path.read_text(encoding="utf-8").splitlines(), start=1):
            match = RETIRED_TIER_RE.search(line)
            if match:
                errors.append(f"{rel}:{number}: retired tier vocabulary '{match.group(0)}'")
    return errors


def check(root: Path) -> list[str]:
    errors: list[str] = []
    errors.extend(check_required_files(root))
    errors.extend(check_integration(root))
    errors.extend(check_verdict_lists(root))
    errors.extend(check_forbidden_terms(root))
    errors.extend(check_status_vocabulary(root))
    errors.extend(check_unused_audit_records(root))
    errors.extend(check_verify_script(root))
    errors.extend(check_closeout_template(root))
    errors.extend(check_findings_columns(root))
    errors.extend(check_tier_vocabulary(root))
    return errors


def main() -> int:
    root = Path.cwd()
    errors = check(root)
    if errors:
        print("Process consistency problems:", file=sys.stderr)
        for error in errors:
            print(f"  {error}", file=sys.stderr)
        return 1

    print("Process consistency: OK")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
