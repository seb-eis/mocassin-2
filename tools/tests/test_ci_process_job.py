# SPDX-License-Identifier: MIT
"""Tests for the process/binding job and its path filter in `.github/workflows/ci.yml`.

The 2026-11-27 workflow adoption added `tools/check_process_consistency.py`,
`tools/check_docs_links.py`, `tools/check_doc_math.py`, and `tools/tests/**`,
but no CI job ran them. These assertions execute the shipped change-detection
snippet over sample file lists and guard the lightweight job that consumes it,
so the guardrails cannot silently drop out of CI again.

Coverage guards sit on top of the behaviour checks: every path exported by the
process checker and every Markdown file in the documentation checker's scan must
match the workflow's case arm, the normative filter lists in
`docs/planning/ci-policy.md` and `AGENTS.md` must match that arm, and the job's
test command must be the one `tools/verify.sh` runs.
"""

from __future__ import annotations

import re
import subprocess
import tempfile
import unittest
from fnmatch import fnmatchcase
from pathlib import Path

from tools.check_docs_links import iter_markdown
from tools.check_process_consistency import NORMATIVE_FILES, REQUIRED_FILES, VERDICT_FILES

REPO = Path(__file__).resolve().parents[2]
WORKFLOW = REPO / ".github" / "workflows" / "ci.yml"
CI_POLICY = REPO / "docs" / "planning" / "ci-policy.md"
AGENTS = REPO / "AGENTS.md"
VERIFY = REPO / "tools" / "verify.sh"
CONTRACTS_REQUIREMENTS = REPO / "tools" / "contracts" / "requirements.txt"

JOB_RE = re.compile(r"(?m)^  (?P<name>[a-z][a-z-]*):\n")
JOB_IF_RE = re.compile(r"(?m)^    if: (?P<expression>\S.*)$")

AREA_FLAGS = ("dotnet", "rust", "python", "contracts", "api", "process")

# Every path family that must enable the process/binding job.
PROCESS_PATHS = (
    ".opencode/agents/dev-free.md",
    "README.md",
    "CONTRIBUTING.md",
    "SECURITY.md",
    "AGENTS.md",
    "GLOSSARY.md",
    "opencode.json",
    "docs/development/index.md",
    "docs/user-guide/index.md",
    "docs/planning/reviews/step-15.md",
    "docs/planning/audits/step-15.md",
    "docs/planning/steps/step-14-simulation-context-initialization.md",
    "docs/planning/adr/0011-immutable-package-and-results-store.md",
    "docs/planning/ci-policy.md",
    "docs/planning/status.md",
    "docs/planning/maintenance/2026-11-27-temporary-free-dev-binding.md",
    "docs/planning/drift-checks/2026-11-18-phase-c.md",
    "docs/planning/README.md",
    "docs/planning/04-roadmap.md",
    "docs/planning/closeout/_template.json",
    "docs/planning/incidents/_template.md",
    "docs/planning/research/_template.md",
    "docs/planning/prototypes/_template.md",
    "docs/planning/retrospectives/_template.md",
    "zensical.toml",
    "tools/check_process_consistency.py",
    "tools/check_closeout_evidence.py",
    "tools/check_docs_links.py",
    "tools/check_doc_math.py",
    "tools/verify.sh",
    "tools/tests/test_ci_process_job.py",
)

# Every checker-owned input plus the gate scripts the process job invokes or mirrors.
CHECKER_INPUT_PATHS = tuple(
    sorted(
        set(REQUIRED_FILES)
        | set(NORMATIVE_FILES)
        | set(VERDICT_FILES)
        | {"tools/check_closeout_evidence.py", "tools/verify.sh"}
    )
)

# The tool-test discovery command shared by the job and `tools/verify.sh`.
DISCOVERY_COMMAND = "python -m unittest discover -s tools/tests -t ."

# The only dependency install permitted in the lightweight process job.
CONTRACTS_INSTALL_COMMAND = "python -m pip install -r tools/contracts/requirements.txt"

# Single-file filter tokens. A documented token is a filter token when it ends
# with `*` or is one of these; that keeps unrelated backticked paths in the same
# prose (for example `tools/contracts/manifest.json`) out of the comparison.
SINGLE_FILE_FILTERS = frozenset(
    {
        "README.md",
        "CONTRIBUTING.md",
        "SECURITY.md",
        "AGENTS.md",
        "GLOSSARY.md",
        "opencode.json",
        "zensical.toml",
        "docs/planning/README.md",
        "docs/planning/04-roadmap.md",
        "docs/planning/ci-policy.md",
        "docs/planning/status.md",
        "tools/check_process_consistency.py",
        "tools/check_closeout_evidence.py",
        "tools/check_docs_links.py",
        "tools/check_doc_math.py",
        "tools/verify.sh",
    }
)


def workflow_text() -> str:
    return WORKFLOW.read_text(encoding="utf-8")


def job_block(name: str) -> str:
    text = workflow_text()
    match = re.search(rf"(?m)^  {re.escape(name)}:\n", text)
    if match is None:
        raise AssertionError(f"job {name} is missing from ci.yml")
    following = JOB_RE.search(text, match.end())
    return text[match.start() : following.start() if following else len(text)]


def filter_run() -> str:
    """Return the dedented `run:` body of the change-detection step."""
    text = workflow_text()
    try:
        step = text.index("      - id: filter\n")
        start = text.index("        run: |\n", step) + len("        run: |\n")
    except ValueError as error:
        raise AssertionError("the change-detection filter step is missing from ci.yml") from error
    lines: list[str] = []
    for line in text[start:].splitlines():
        if line.startswith("          "):
            lines.append(line[10:])
        elif not line.strip():
            lines.append("")
        else:
            break
    return "\n".join(lines)


def workflow_filter_patterns() -> set[str]:
    """Return the process case-arm alternatives, with `**` folded to `*`."""
    lines = filter_run().splitlines()
    for index, line in enumerate(lines):
        if line.strip() != "process=true ;;":
            continue
        pattern_line = lines[index - 1].strip()
        if not pattern_line.endswith(")"):
            raise AssertionError(f"unexpected process case arm: {pattern_line}")
        return {part.replace("**", "*") for part in pattern_line[:-1].split("|")}
    raise AssertionError("the filter step has no `process=true` case arm")


def documented_filter_patterns(text: str) -> set[str]:
    """Return the filter tokens documented in one prose block."""
    return {
        token.replace("**", "*")
        for token in re.findall(r"`([^`]+)`", text)
        if token.endswith("*") or token in SINGLE_FILE_FILTERS
    }


def ci_policy_filter_row() -> str:
    for line in CI_POLICY.read_text(encoding="utf-8").splitlines():
        if line.startswith("| Process and binding |"):
            cells = line.split("|")
            if len(cells) < 3:
                break
            return cells[2]
    raise AssertionError("ci-policy.md has no 'Process and binding' path-filter row")


def agents_filter_bullet() -> str:
    lines = AGENTS.read_text(encoding="utf-8").splitlines()
    for index, line in enumerate(lines):
        if line.startswith("- The `Process and binding checks` job runs"):
            block = [line]
            for following in lines[index + 1 :]:
                if following.startswith("- ") or not following.strip():
                    break
                block.append(following)
            return "\n".join(block)
    raise AssertionError("AGENTS.md has no process/binding filter bullet")


def detect(changed: list[str]) -> dict[str, str]:
    """Run the shipped path-classification and derivation logic for one file list."""
    body = filter_run()
    marker = "dotnet=false"
    if marker not in body:
        raise AssertionError("the filter step no longer initialises the area flags")
    snippet = body[body.index(marker) :]
    with tempfile.NamedTemporaryFile("w", delete=False, encoding="utf-8") as changed_file:
        changed_file.write("\n".join(changed) + "\n")
        changed_path = changed_file.name
    with tempfile.NamedTemporaryFile("w", delete=False, encoding="utf-8") as output:
        output_path = Path(output.name)
    script = (
        "set -uo pipefail\n"
        "all=false\n"
        "PLATFORMS=ubuntu\n"
        f'changed="$(cat {changed_path})"\n' + snippet
    )
    result = subprocess.run(
        ["bash", "-c", script],
        capture_output=True,
        text=True,
        check=False,
        env={"GITHUB_OUTPUT": str(output_path), "PATH": "/usr/local/bin:/usr/bin:/bin"},
    )
    if result.returncode != 0:
        raise AssertionError(f"the change-detection snippet failed: {result.stderr}")
    outputs = dict(
        line.split("=", 1)
        for line in output_path.read_text(encoding="utf-8").splitlines()
        if "=" in line
    )
    output_path.unlink(missing_ok=True)
    Path(changed_path).unlink(missing_ok=True)
    return outputs


class ProcessJobTests(unittest.TestCase):
    def test_process_paths_enable_only_the_process_job(self) -> None:
        for path in PROCESS_PATHS:
            with self.subTest(path=path):
                outputs = detect([path])
                self.assertEqual(outputs["process"], "true")
                for flag in AREA_FLAGS:
                    if flag != "process":
                        self.assertEqual(
                            outputs[flag],
                            "false",
                            f"{path} must not enable the {flag} job",
                        )

    def test_every_process_checker_input_matches_the_workflow_filter(self) -> None:
        """PR-fast regression: checker inputs must never bypass the process job."""
        patterns = workflow_filter_patterns()
        for path in CHECKER_INPUT_PATHS:
            with self.subTest(path=path):
                self.assertTrue(
                    any(fnmatchcase(path, pattern) for pattern in patterns),
                    f"{path} is validated by the process checks but no process filter matches it",
                )

    def test_every_documentation_markdown_file_matches_the_process_filter(self) -> None:
        """PR-fast regression: the link checker's whole scan must enable the process job."""
        paths = sorted(path.relative_to(REPO).as_posix() for path in iter_markdown(REPO))
        self.assertTrue(paths, "the documentation checker must discover Markdown files")
        patterns = workflow_filter_patterns()
        uncovered = [
            path for path in paths if not any(fnmatchcase(path, pattern) for pattern in patterns)
        ]
        self.assertEqual(
            uncovered,
            [],
            "documentation Markdown files bypass the process job:\n" + "\n".join(uncovered),
        )

    def test_contract_tooling_does_not_enable_the_process_job(self) -> None:
        for path in ("tools/contracts/manifest.json", "tools/check-license-headers.py"):
            with self.subTest(path=path):
                outputs = detect([path])
                self.assertEqual(outputs["contracts"], "true")
                self.assertEqual(outputs["process"], "false")

    def test_code_changes_do_not_enable_the_process_job(self) -> None:
        for path in (
            "src/dotnet/Mocassin.Core/Particle.cs",
            "src/rust/mocassin-solver/src/lib.rs",
            "src/python/mocassin/__init__.py",
        ):
            with self.subTest(path=path):
                self.assertEqual(detect([path])["process"], "false")

    def test_ci_workflow_change_enables_every_job_including_process(self) -> None:
        outputs = detect([".github/workflows/ci.yml"])
        for flag in AREA_FLAGS:
            with self.subTest(flag=flag):
                self.assertEqual(outputs[flag], "true")

    def test_changes_job_exports_the_process_output(self) -> None:
        self.assertIn(
            "process: ${{ steps.filter.outputs.process }}",
            job_block("changes"),
        )

    def test_process_job_runs_the_guardrails_without_building_code(self) -> None:
        block = job_block("process")
        self.assertIn("runs-on: ubuntu-latest", block)
        self.assertIn('python-version: "3.13"', block)
        for command in (
            "python tools/check_process_consistency.py",
            "python tools/check_docs_links.py",
            "python tools/check_doc_math.py",
            DISCOVERY_COMMAND,
        ):
            with self.subTest(command=command):
                self.assertIn(command, block)
        install_commands = [
            line.strip().removeprefix("run: ")
            for line in block.splitlines()
            if line.strip().startswith("run: ") and "install" in line.lower()
        ]
        self.assertEqual(
            install_commands,
            [CONTRACTS_INSTALL_COMMAND],
            "the process job must install only the pinned contracts requirements",
        )
        self.assertEqual(
            CONTRACTS_REQUIREMENTS.read_text(encoding="utf-8").splitlines(),
            ["msgpack==1.2.4"],
            "the contracts test requirement must stay pinned",
        )
        for absent in ("dotnet", "cargo"):
            with self.subTest(absent=absent):
                self.assertNotIn(absent, block, "the process job must stay cheap")

    def test_tool_test_discovery_matches_the_verify_script(self) -> None:
        self.assertIn(
            DISCOVERY_COMMAND,
            VERIFY.read_text(encoding="utf-8"),
            "tools/verify.sh must keep the discovery command the job mirrors",
        )
        self.assertIn(DISCOVERY_COMMAND, job_block("process"))

    def test_documented_filter_lists_match_the_workflow_arm(self) -> None:
        expected = workflow_filter_patterns()
        self.assertIn(
            ".opencode/*",
            expected,
            "the drift guard expects the workflow arm, not the documented list",
        )
        for label, documented in (
            ("docs/planning/ci-policy.md", ci_policy_filter_row()),
            ("AGENTS.md §3.13", agents_filter_bullet()),
        ):
            with self.subTest(home=label):
                self.assertEqual(
                    documented_filter_patterns(documented),
                    expected,
                    f"{label} and the workflow filter disagree",
                )

    def test_process_job_is_path_filtered_and_ubuntu_only(self) -> None:
        match = JOB_IF_RE.search(job_block("process"))
        self.assertIsNotNone(match, "the process job needs an explicit condition")
        expression = match.group("expression")
        self.assertIn("needs.changes.outputs.process == 'true'", expression)
        self.assertIn(
            "(github.event_name == 'push' || inputs.platforms == 'ubuntu' || inputs.platforms == 'all')",
            expression,
        )


if __name__ == "__main__":
    unittest.main()
