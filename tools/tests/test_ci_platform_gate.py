# SPDX-License-Identifier: MIT
"""Tests for the platform-gate scope in .github/workflows/ci.yml.

The Windows/macOS platform gate is a focused .NET check. It must not start an
implicit Ubuntu job and must not re-run the full test suite, the coverage
collection, or the format check. These assertions guard that split.
"""

from __future__ import annotations

import json
import re
import subprocess
import unittest
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
WORKFLOW = REPO / ".github" / "workflows" / "ci.yml"

PLATFORM_CASE_RE = re.compile(r'(?ms)^\s*case "\$PLATFORMS" in\n(?P<body>.*?)^\s*esac$')
JOB_RE = re.compile(r"(?m)^  ([a-z][a-z-]*):\n")


def workflow_text() -> str:
    return WORKFLOW.read_text(encoding="utf-8")


def job_block(name: str) -> str:
    text = workflow_text()
    match = re.search(rf"(?m)^  {re.escape(name)}:\n", text)
    if match is None:
        raise AssertionError(f"job {name} is missing from ci.yml")
    following = JOB_RE.search(text, match.end())
    return text[match.start() : following.start() if following else len(text)]


def matrix_for(platform: str) -> subprocess.CompletedProcess[str]:
    """Run the real `changes` shell block so the matrix output is the shipped one."""
    match = PLATFORM_CASE_RE.search(workflow_text())
    if match is None:
        raise AssertionError("the platforms case block is missing from ci.yml")
    script = f'PLATFORMS="$1"\ncase "$PLATFORMS" in\n{match.group("body")}esac\necho "$os"\n'
    return subprocess.run(
        ["bash", "-c", script, "bash", platform],
        capture_output=True,
        text=True,
        check=False,
    )


class PlatformGateTests(unittest.TestCase):
    def test_platform_matrix_selects_only_the_requested_runner(self) -> None:
        expected = {
            "ubuntu": ["ubuntu-latest"],
            "windows": ["windows-latest"],
            "macos": ["macos-latest"],
            "all": ["ubuntu-latest", "windows-latest", "macos-latest"],
        }
        for platform, runners in expected.items():
            with self.subTest(platform=platform):
                result = matrix_for(platform)
                self.assertEqual(result.returncode, 0, result.stderr)
                self.assertEqual(json.loads(result.stdout), runners)

    def test_unknown_platform_is_rejected(self) -> None:
        result = matrix_for("solaris")
        self.assertEqual(result.returncode, 2)
        self.assertIn("Invalid platforms input", result.stderr)

    def test_windows_gate_does_not_imply_the_ubuntu_runner(self) -> None:
        result = matrix_for("windows")
        self.assertNotIn("ubuntu-latest", result.stdout)

    def test_full_suite_coverage_and_format_are_ubuntu_only(self) -> None:
        dotnet = job_block("dotnet")
        self.assertRegex(
            dotnet,
            r"if: matrix\.os == 'ubuntu-latest'\n\s+run: >\n\s+dotnet test Mocassin\.slnx",
            "the full .NET suite must be gated to Ubuntu",
        )
        self.assertRegex(
            dotnet,
            r"if: matrix\.os == 'ubuntu-latest'\n\s+run: dotnet format ",
            "the format check must be gated to Ubuntu",
        )
        self.assertNotRegex(
            dotnet,
            r"(?s)if: matrix\.os != 'ubuntu-latest'.{0,200}--collect",
            "a platform gate must not collect coverage",
        )

    def test_platform_gate_runs_only_the_platform_test_projects(self) -> None:
        dotnet = job_block("dotnet")
        focused = re.findall(
            r"if: matrix\.os != 'ubuntu-latest'\n\s+run: >\n\s+dotnet test (\S+\.csproj)",
            dotnet,
        )
        self.assertEqual(
            focused,
            [
                "src/dotnet/Mocassin.Packages.Tests/Mocassin.Packages.Tests.csproj",
                "src/dotnet/Mocassin.Results.Tests/Mocassin.Results.Tests.csproj",
            ],
        )
        for relative in focused:
            with self.subTest(project=relative):
                self.assertTrue((REPO / relative).is_file(), f"{relative} does not exist")

    def test_non_dotnet_jobs_are_ubuntu_only(self) -> None:
        for name in ("rust", "python", "contracts", "api-compat"):
            with self.subTest(job=name):
                block = job_block(name)
                self.assertIn("runs-on: ubuntu-latest", block)
                self.assertNotIn("matrix.os", block)


if __name__ == "__main__":
    unittest.main()
