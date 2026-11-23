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
MODEL_RE = re.compile(r"(?m)^model:\s*(\S+)\s*$")


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


def model(path: Path) -> str:
    match = MODEL_RE.search(frontmatter(path))
    if not match:
        raise AssertionError(f"{path.name} has no model line")
    return match.group(1)


def effective(rules: list[tuple[str, str, str]], action: str, resource: str) -> str:
    """OpenCode's base policy allows; the agent rules come after it, last match wins."""
    result = "allow"
    for rule_action, rule_resource, effect in rules:
        if fnmatch.fnmatch(action, rule_action) and fnmatch.fnmatch(resource, rule_resource):
            result = effect
    return result


class BindingTests(unittest.TestCase):
    def test_default_agent_is_the_primary_lead(self) -> None:
        config = json.loads(CONFIG.read_text(encoding="utf-8"))
        self.assertEqual(config["default_agent"], "lead")
        self.assertRegex(frontmatter(AGENTS / "lead.md"), r"(?m)^mode:\s*primary\s*$")

    def test_every_agent_has_a_description_and_mode(self) -> None:
        for name in ("lead", "design", "dev-free", "dev", "review"):
            with self.subTest(agent=name):
                fm = frontmatter(AGENTS / f"{name}.md")
                self.assertRegex(fm, r"(?m)^description:\s*\S")
                mode = re.search(r"(?m)^mode:\s*(\S+)\s*$", fm)
                expected = "primary" if name == "lead" else "subagent"
                self.assertEqual(mode.group(1), expected)

    def test_agents_use_the_assigned_model_tiers(self) -> None:
        self.assertEqual(model(AGENTS / "lead.md"), "opencode-go/deepseek-v4.1-flash")
        self.assertEqual(model(AGENTS / "design.md"), "opencode-go/deepseek-v4.1-flash")
        self.assertEqual(model(AGENTS / "review.md"), "opencode-go/deepseek-v4.1-flash")
        self.assertEqual(model(AGENTS / "dev-free.md"), "opencode/deepseek-v4.1-flash-free")
        self.assertEqual(model(AGENTS / "dev.md"), "opencode-go/deepseek-v4.1-flash")

    def test_lead_cannot_edit_and_spawns_only_the_roles(self) -> None:
        rules = permission_rules(AGENTS / "lead.md")
        for role in ("design", "dev-free", "dev", "review"):
            with self.subTest(role=role):
                self.assertEqual(effective(rules, "subagent", role), "allow")
        self.assertEqual(effective(rules, "subagent", "planner"), "deny")
        self.assertEqual(effective(rules, "subagent", "general"), "deny")
        self.assertEqual(effective(rules, "edit", "src/App.cs"), "deny")
        self.assertEqual(effective(rules, "edit", "docs/process/workflow.md"), "deny")
        self.assertEqual(effective(rules, "edit", "docs/planning/audits/step-11.md"), "deny")

    def test_design_cannot_edit_implementation_areas(self) -> None:
        rules = permission_rules(AGENTS / "design.md")
        self.assertEqual(effective(rules, "edit", "src/App.cs"), "deny")
        self.assertEqual(effective(rules, "edit", "tests/UnitTests.cs"), "deny")
        self.assertEqual(effective(rules, "edit", "tools/verify.sh"), "deny")
        self.assertEqual(effective(rules, "edit", "contracts/manifest.json"), "deny")
        self.assertEqual(effective(rules, "edit", "docs/planning/steps/step-11-msl.md"), "allow")

    def test_dev_free_delegates_to_dev_and_never_switches_models(self) -> None:
        text = body(AGENTS / "dev-free.md")
        self.assertIn(".opencode/agents/dev.md", text)
        self.assertIn("Do not switch", text)
        self.assertIn("report it to the lead", text)

    def test_review_writes_only_step_and_cross_cutting_records(self) -> None:
        rules = permission_rules(AGENTS / "review.md")
        self.assertEqual(effective(rules, "edit", "docs/planning/audits/step-11.md"), "allow")
        self.assertEqual(effective(rules, "edit", "docs/planning/reviews/step-11.md"), "allow")
        self.assertEqual(
            effective(rules, "edit", "docs/planning/reviews/cross-cutting/xc-04-combined.md"), "allow"
        )
        self.assertEqual(effective(rules, "edit", "docs/planning/reviews/README.md"), "deny")
        self.assertEqual(effective(rules, "edit", "src/App.cs"), "deny")
        self.assertEqual(effective(rules, "subagent", "explore"), "deny")

    def test_commands_use_the_lead_and_keep_the_gate_order(self) -> None:
        for name in ("step-start", "step-closeout"):
            with self.subTest(command=name):
                fm = frontmatter(COMMANDS / f"{name}.md")
                self.assertRegex(fm, r"(?m)^agent:\s*lead\s*$")

        start = body(COMMANDS / "step-start.md")
        self.assertIn("docs/planning/reviews/step-NN.md", start)
        self.assertIn("step/NN-slug", start)
        self.assertIn("spawn `design`", start)
        self.assertIn("spawn `review`", start)

        closeout = body(COMMANDS / "step-closeout.md")
        self.assertIn("docs/planning/audits/step-NN.md", closeout)
        self.assertIn("docs/planning/closeout/step-NN.json", closeout)
        self.assertIn("spawn `dev-free`", closeout)
        self.assertIn("pre-audit gate", closeout)
        self.assertLess(
            closeout.index("check_closeout_evidence"),
            closeout.index("spawn `review`"),
            "the evidence check must run before the audit request",
        )
        self.assertLess(
            closeout.index("spawn `review`"),
            closeout.index("present the cumulative approval report"),
            "the audit must precede the merge approval request",
        )


if __name__ == "__main__":
    unittest.main()
