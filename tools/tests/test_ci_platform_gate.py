# SPDX-License-Identifier: MIT
"""Tests for the platform-gate scope in .github/workflows/ci.yml.

The Windows/macOS platform gate is a focused .NET check. It must not start an
implicit Ubuntu job and must not re-run the full test suite, the coverage
collection, or the format check. These assertions guard that split.

The Ubuntu-only jobs (Rust, Python, contracts/policy, public API) are also
gated by a job condition, not only by `runs-on`. The condition is evaluated
here for each dispatch shape instead of being string-matched, so a flatter
expression that still starts those jobs on Windows fails the test.
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
JOB_RE = re.compile(r"(?m)^  (?P<name>[a-z][a-z-]*):\n")
JOB_IF_RE = re.compile(r"(?m)^    if: (?P<expression>\S.*)$")
TOKEN_RE = re.compile(
    r"\s*(?:(?P<string>'[^']*')|(?P<op>\|\||&&|==|\(|\))|(?P<name>[A-Za-z_][A-Za-z0-9_.]*))"
)

# Job -> the `changes` output that path-filters it.
UBUNTU_ONLY_JOBS = {
    "rust": "rust",
    "python": "python",
    "contracts": "contracts",
    "api-compat": "api",
}


def workflow_text() -> str:
    return WORKFLOW.read_text(encoding="utf-8")


def job_block(name: str) -> str:
    text = workflow_text()
    match = re.search(rf"(?m)^  {re.escape(name)}:\n", text)
    if match is None:
        raise AssertionError(f"job {name} is missing from ci.yml")
    following = JOB_RE.search(text, match.end())
    return text[match.start() : following.start() if following else len(text)]


def job_names() -> list[str]:
    jobs_section = workflow_text().split("\njobs:\n", 1)[1]
    return JOB_RE.findall(jobs_section)


def job_condition(name: str) -> str | None:
    """Return the job-level `if` expression, or None when the job has no condition."""
    match = JOB_IF_RE.search(job_block(name))
    return match.group("expression") if match else None


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


def tokenize(expression: str) -> list[tuple[str, str]]:
    tokens: list[tuple[str, str]] = []
    position = 0
    while position < len(expression):
        match = TOKEN_RE.match(expression, position)
        if match is None:
            raise AssertionError(f"cannot parse condition at {expression[position:]!r}")
        position = match.end()
        if match.group("string") is not None:
            tokens.append(("string", match.group("string")[1:-1]))
        elif match.group("op") is not None:
            tokens.append(("op", match.group("op")))
        else:
            tokens.append(("name", match.group("name")))
        while position < len(expression) and expression[position].isspace():
            position += 1
    return tokens


class Condition:
    """Evaluate the small GitHub-expression subset used by job-level `if`s.

    Supported: `==`, `&&`, `||`, parentheses, quoted strings, and dotted
    context references. An unknown reference evaluates to None, matching a
    context property that does not exist for the current event.
    """

    def __init__(self, expression: str, values: dict[str, str | None]) -> None:
        self.tokens = tokenize(expression)
        self.values = values
        self.position = 0

    def evaluate(self) -> bool:
        result = self._or()
        if self.position != len(self.tokens):
            raise AssertionError(f"unconsumed tokens: {self.tokens[self.position:]}")
        return bool(result)

    def _peek(self) -> tuple[str | None, str | None]:
        if self.position >= len(self.tokens):
            return None, None
        return self.tokens[self.position]

    def _take(self) -> tuple[str, str]:
        token = self._peek()
        if token[0] is None:
            raise AssertionError("unexpected end of condition")
        self.position += 1
        return token  # type: ignore[return-value]

    def _or(self) -> bool:
        result = self._and()
        while self._peek() == ("op", "||"):
            self._take()
            right = self._and()
            result = result or right
        return result

    def _and(self) -> bool:
        result = self._atom()
        while self._peek() == ("op", "&&"):
            self._take()
            right = self._atom()
            result = result and right
        return result

    def _atom(self) -> bool:
        if self._peek() == ("op", "("):
            self._take()
            result = self._or()
            if self._take() != ("op", ")"):
                raise AssertionError("unbalanced parentheses in job condition")
            return result
        return self._comparison()

    def _comparison(self) -> bool:
        left = self._value()
        operator = self._take()
        if operator != ("op", "=="):
            raise AssertionError(f"unsupported operator {operator!r} in job condition")
        return left == self._value()

    def _value(self) -> str | None:
        kind, value = self._take()
        if kind == "string":
            return value
        if kind == "name":
            return self.values.get(value)
        raise AssertionError(f"unexpected token {kind}:{value!r} in job condition")


def evaluate_job(
    name: str,
    *,
    event: str,
    platforms: str | None,
    changed: set[str],
) -> bool:
    expression = job_condition(name)
    if expression is None:
        raise AssertionError(f"job {name} has no job-level condition")
    values: dict[str, str | None] = {
        "github.event_name": event,
        "inputs.platforms": platforms,
        "needs.changes.outputs.dotnet": "true" if "dotnet" in changed else "false",
    }
    for job, output in UBUNTU_ONLY_JOBS.items():
        values[f"needs.changes.outputs.{output}"] = "true" if output in changed else "false"
    return Condition(expression, values).evaluate()


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
                "src/dotnet/Mocassin.Cli.Tests/Mocassin.Cli.Tests.csproj",
                "src/dotnet/Mocassin.App.Tests/Mocassin.App.Tests.csproj",
            ],
        )
        for relative in focused:
            with self.subTest(project=relative):
                self.assertTrue((REPO / relative).is_file(), f"{relative} does not exist")

    def test_job_set_is_explicit(self) -> None:
        # A new substantive job must make a deliberate Ubuntu-vs-platform
        # decision instead of inheriting the Ubuntu runner by default.
        self.assertEqual(
            set(job_names()),
            {"changes", "dotnet", "rust", "python", "contracts", "api-compat"},
        )

    def test_changes_job_is_unconditional_on_ubuntu(self) -> None:
        self.assertIsNone(job_condition("changes"))
        self.assertIn("runs-on: ubuntu-latest", job_block("changes"))

    def test_dotnet_job_runs_on_the_selected_runner(self) -> None:
        for platform in (None, "ubuntu", "windows", "macos", "all"):
            with self.subTest(platform=platform):
                self.assertTrue(
                    evaluate_job(
                        "dotnet",
                        event="workflow_dispatch",
                        platforms=platform,
                        changed={"dotnet"},
                    )
                )
                self.assertFalse(
                    evaluate_job(
                        "dotnet",
                        event="workflow_dispatch",
                        platforms=platform,
                        changed=set(),
                    )
                )

    def test_ubuntu_only_jobs_skip_windows_and_macos_dispatches(self) -> None:
        for job, output in UBUNTU_ONLY_JOBS.items():
            for platform in ("windows", "macos"):
                with self.subTest(job=job, platform=platform):
                    self.assertFalse(
                        evaluate_job(
                            job,
                            event="workflow_dispatch",
                            platforms=platform,
                            changed={output},
                        ),
                        f"{job} must not start for platforms={platform}",
                    )

    def test_ubuntu_only_jobs_run_on_main_push(self) -> None:
        for job, output in UBUNTU_ONLY_JOBS.items():
            with self.subTest(job=job):
                self.assertTrue(
                    evaluate_job(
                        job,
                        event="push",
                        platforms=None,
                        changed={output},
                    )
                )

    def test_ubuntu_only_jobs_run_for_ubuntu_and_all_dispatches(self) -> None:
        for job, output in UBUNTU_ONLY_JOBS.items():
            for platform in ("ubuntu", "all"):
                with self.subTest(job=job, platform=platform):
                    self.assertTrue(
                        evaluate_job(
                            job,
                            event="workflow_dispatch",
                            platforms=platform,
                            changed={output},
                        )
                    )

    def test_path_filters_still_govern_the_ubuntu_only_jobs(self) -> None:
        for job in UBUNTU_ONLY_JOBS:
            for event, platforms in (("push", None), ("workflow_dispatch", "ubuntu"), ("workflow_dispatch", "all")):
                with self.subTest(job=job, event=event, platform=platforms):
                    self.assertFalse(
                        evaluate_job(
                            job,
                            event=event,
                            platforms=platforms,
                            changed=set(),
                        ),
                        f"{job} must still respect its path filter",
                    )

    def test_ubuntu_only_jobs_pin_the_ubuntu_runner(self) -> None:
        for name in UBUNTU_ONLY_JOBS:
            with self.subTest(job=name):
                block = job_block(name)
                self.assertIn("runs-on: ubuntu-latest", block)
                self.assertNotIn("matrix.os", block)


if __name__ == "__main__":
    unittest.main()
