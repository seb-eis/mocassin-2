# SPDX-License-Identifier: MIT
"""Tests for the process/binding job and its path filter in `.github/workflows/ci.yml`.

The 2026-11-27 workflow adoption added `tools/check_process_consistency.py`,
`tools/check_docs_links.py`, `tools/check_doc_math.py`, and `tools/tests/**`,
but no CI job ran them. These assertions execute the shipped change-detection
snippet over sample file lists and guard the lightweight job that consumes it,
so the guardrails cannot silently drop out of CI again.
"""

from __future__ import annotations

import re
import subprocess
import tempfile
import unittest
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
WORKFLOW = REPO / ".github" / "workflows" / "ci.yml"

JOB_RE = re.compile(r"(?m)^  (?P<name>[a-z][a-z-]*):\n")
JOB_IF_RE = re.compile(r"(?m)^    if: (?P<expression>\S.*)$")

AREA_FLAGS = ("dotnet", "rust", "python", "contracts", "api", "process")

# Every path family that must enable the process/binding job.
PROCESS_PATHS = (
    ".opencode/agents/dev-free.md",
    "AGENTS.md",
    "GLOSSARY.md",
    "opencode.json",
    "docs/development/index.md",
    "docs/planning/reviews/step-15.md",
    "docs/planning/audits/step-15.md",
    "zensical.toml",
    "tools/check_process_consistency.py",
    "tools/check_docs_links.py",
    "tools/check_doc_math.py",
    "tools/tests/test_ci_process_job.py",
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
            "python -m unittest discover -s tools/tests",
        ):
            with self.subTest(command=command):
                self.assertIn(command, block)
        for absent in ("dotnet", "cargo", "pip install"):
            with self.subTest(absent=absent):
                self.assertNotIn(absent, block, "the process job must stay cheap")

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
