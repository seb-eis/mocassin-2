# SPDX-License-Identifier: MIT
"""Tests for tools.check_closeout_evidence."""

from __future__ import annotations

import json
import subprocess
import tempfile
import unittest
from pathlib import Path

from tools.check_closeout_evidence import check


class CloseoutEvidenceTests(unittest.TestCase):
    def setUp(self) -> None:
        self._tmp = tempfile.TemporaryDirectory()
        self.root = Path(self._tmp.name)
        subprocess.run(["git", "init", "-q"], cwd=self.root, check=True)
        subprocess.run(["git", "config", "user.name", "Test"], cwd=self.root, check=True)
        subprocess.run(["git", "config", "user.email", "test@example.com"], cwd=self.root, check=True)

        self.base = self.commit("base.txt", "base\n", "base")
        self.docs = self.commit("docs/planning/note.md", "note\n", "docs")
        self.root_docs = self.commit("AGENTS.md", "# Guide\n", "root docs")
        self.code = self.commit("code.py", "print('x')\n", "code")

    def tearDown(self) -> None:
        self._tmp.cleanup()

    def commit(self, rel: str, text: str, message: str) -> str:
        path = self.root / rel
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(text, encoding="utf-8")
        subprocess.run(["git", "add", rel], cwd=self.root, check=True)
        subprocess.run(["git", "commit", "-qm", message], cwd=self.root, check=True)
        return subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=self.root, text=True).strip()

    def evidence(self, **overrides: object) -> Path:
        data = {
            "step": "step-03",
            "audit_commit": self.base,
            "closeout_ci": {"run_id": "123", "head_sha": self.base},
            "main_sha_at_freeze": self.base,
            "merge_candidate": self.docs,
            "merge_tree_ci": {"run_id": "", "head_sha": ""},
        }
        data.update(overrides)
        path = self.root / "evidence.json"
        path.write_text(json.dumps(data), encoding="utf-8")
        return path

    def test_valid_docs_only_candidate(self) -> None:
        self.assertEqual(check(self.root, self.evidence()), [])

    def test_root_docs_candidate_is_documentation_only(self) -> None:
        self.assertEqual(check(self.root, self.evidence(merge_candidate=self.root_docs)), [])

    def test_unresolved_sha_reports_without_traceback(self) -> None:
        missing = "f" * 40
        path = self.evidence(audit_commit=missing, closeout_ci={"run_id": "123", "head_sha": missing})
        errors = check(self.root, path)
        self.assertTrue(any("does not resolve" in error for error in errors))

    def test_split_step_identifiers(self) -> None:
        for step in ("step-23a", "step-23b", "step-00z"):
            with self.subTest(step=step):
                self.assertEqual(check(self.root, self.evidence(step=step)), [])

    def test_invalid_step_identifier(self) -> None:
        for step in ("wp-03", "step-23aa", "step-023", "step-23A", "step-23x?", "step-3a"):
            with self.subTest(step=step):
                errors = check(self.root, self.evidence(step=step))
                self.assertTrue(any("step-NN" in error for error in errors))

    def test_mismatched_closeout_sha(self) -> None:
        path = self.evidence(closeout_ci={"run_id": "123", "head_sha": self.docs})
        errors = check(self.root, path)
        self.assertTrue(any("differ" in error for error in errors))

    def test_code_change_requires_merge_tree_ci(self) -> None:
        path = self.evidence(merge_candidate=self.code)
        errors = check(self.root, path)
        self.assertTrue(any("merge_tree_ci.head_sha" in error for error in errors))

    def test_code_change_with_merge_tree_ci_passes(self) -> None:
        path = self.evidence(
            merge_candidate=self.code,
            merge_tree_ci={"run_id": "456", "head_sha": self.code},
        )
        self.assertEqual(check(self.root, path), [])


if __name__ == "__main__":
    unittest.main()
