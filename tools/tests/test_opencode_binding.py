# SPDX-License-Identifier: MIT
"""Tests for the OpenCode role binding in .opencode/ and opencode.json."""

from __future__ import annotations

import fnmatch
import json
import re
import unittest
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
AGENTS = REPO / ".opencode" / "agents"
COMMANDS = REPO / ".opencode" / "commands"
CONFIG = REPO / "opencode.json"

PERMISSION_RE = re.compile(
    r"\{\s*action:\s*\"?([^\",}]+)\"?,\s*resource:\s*\"([^\"]*)\",\s*effect:\s*(\w+)\s*\}"
)


def frontmatter(path: Path) -> str:
    text = path.read_text(encoding="utf-8")
    if not text.startswith("---\n"):
        raise AssertionError(f"{path.name} has no frontmatter")
    end = text.index("\n---", 4)
    return text[4:end]


def body(path: Path) -> str:
    text = path.read_text(encoding="utf-8")
    end = text.index("\n---", 4)
    return text[end + 4 :]


def permission_rules(path: Path) -> list[tuple[str, str, str]]:
    return PERMISSION_RE.findall(frontmatter(path))


def effective(rules: list[tuple[str, str, str]], action: str, resource: str) -> str:
    """OpenCode's base policy allows; the agent rules come after it, last match wins."""
    result = "allow"
    for rule_action, rule_resource, effect in rules:
        if fnmatch.fnmatch(action, rule_action) and fnmatch.fnmatch(resource, rule_resource):
            result = effect
    return result


class BindingTests(unittest.TestCase):
    def test_default_agent_is_the_primary_orchestrator(self) -> None:
        config = json.loads(CONFIG.read_text(encoding="utf-8"))
        self.assertEqual(config["default_agent"], "orchestrator")
        self.assertRegex(frontmatter(AGENTS / "orchestrator.md"), r"(?m)^mode:\s*primary\s*$")

    def test_every_agent_has_a_description_and_mode(self) -> None:
        for name in ("orchestrator", "planner", "implementor", "reviewer"):
            with self.subTest(agent=name):
                fm = frontmatter(AGENTS / f"{name}.md")
                self.assertRegex(fm, r"(?m)^description:\s*\S")
                mode = re.search(r"(?m)^mode:\s*(\S+)\s*$", fm)
                expected = "primary" if name == "orchestrator" else "subagent"
                self.assertEqual(mode.group(1), expected)

    def test_orchestrator_cannot_edit_and_spawns_only_the_roles(self) -> None:
        rules = permission_rules(AGENTS / "orchestrator.md")
        for role in ("planner", "implementor", "reviewer"):
            with self.subTest(role=role):
                self.assertEqual(effective(rules, "subagent", role), "allow")
        self.assertEqual(effective(rules, "subagent", "general"), "deny")
        self.assertEqual(effective(rules, "edit", "src/App.cs"), "deny")
        self.assertEqual(effective(rules, "edit", "docs/process/workflow.md"), "deny")
        self.assertEqual(effective(rules, "edit", "docs/planning/audits/step-11.md"), "deny")

    def test_planner_cannot_edit_implementation_areas(self) -> None:
        rules = permission_rules(AGENTS / "planner.md")
        self.assertEqual(effective(rules, "edit", "src/App.cs"), "deny")
        self.assertEqual(effective(rules, "edit", "tests/UnitTests.cs"), "deny")
        self.assertEqual(effective(rules, "edit", "tools/verify.sh"), "deny")
        self.assertEqual(effective(rules, "edit", "contracts/manifest.json"), "deny")
        self.assertEqual(effective(rules, "edit", "docs/planning/steps/step-11-msl.md"), "allow")

    def test_reviewer_writes_only_step_and_cross_cutting_records(self) -> None:
        rules = permission_rules(AGENTS / "reviewer.md")
        self.assertEqual(effective(rules, "edit", "docs/planning/audits/step-11.md"), "allow")
        self.assertEqual(effective(rules, "edit", "docs/planning/reviews/step-11.md"), "allow")
        self.assertEqual(
            effective(rules, "edit", "docs/planning/reviews/cross-cutting/xc-04-combined.md"), "allow"
        )
        self.assertEqual(effective(rules, "edit", "docs/planning/reviews/README.md"), "deny")
        self.assertEqual(effective(rules, "edit", "src/App.cs"), "deny")
        self.assertEqual(effective(rules, "subagent", "explore"), "deny")

    def test_commands_use_the_orchestrator_and_keep_the_gate_order(self) -> None:
        for name in ("step-start", "step-closeout"):
            with self.subTest(command=name):
                fm = frontmatter(COMMANDS / f"{name}.md")
                self.assertRegex(fm, r"(?m)^agent:\s*orchestrator\s*$")

        start = body(COMMANDS / "step-start.md")
        self.assertIn("docs/planning/reviews/step-NN.md", start)
        self.assertIn("step/NN-slug", start)
        self.assertIn("spawn the `planner`", start)
        self.assertIn("spawn the `reviewer`", start)

        closeout = body(COMMANDS / "step-closeout.md")
        self.assertIn("docs/planning/audits/step-NN.md", closeout)
        self.assertIn("docs/planning/closeout/step-NN.json", closeout)
        self.assertLess(
            closeout.index("check_closeout_evidence"),
            closeout.index("spawn the `reviewer`"),
            "the evidence check must run before the audit request",
        )
        self.assertLess(
            closeout.index("spawn the `reviewer`"),
            closeout.index("present the cumulative approval report"),
            "the audit must precede the merge approval request",
        )


if __name__ == "__main__":
    unittest.main()
