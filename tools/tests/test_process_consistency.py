# SPDX-License-Identifier: MIT
"""Tests for tools.check_process_consistency."""

from __future__ import annotations

import tempfile
import unittest
from pathlib import Path

from tools.check_process_consistency import (
    check,
    check_forbidden_terms,
    check_status_vocabulary,
    check_unused_audit_records,
    check_verdict_lists,
)

ROOT = Path(__file__).resolve().parents[2]


class ProcessConsistencyTests(unittest.TestCase):
    def test_repository_process_is_consistent(self) -> None:
        self.assertEqual(check(ROOT), [])

    def test_forbidden_term_is_reported(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            (root / "AGENTS.md").write_text("Use the design foundations review.\n", encoding="utf-8")
            errors = check_forbidden_terms(root)
            self.assertTrue(any("design foundations" in error for error in errors))

    def test_state_in_verdict_list_is_reported(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            path = root / "docs/planning/reviews/step-design-review-template.md"
            path.parent.mkdir(parents=True)
            path.write_text("## Verdict\n\n- [ ] Re-review required\n", encoding="utf-8")
            errors = check_verdict_lists(root)

        self.assertTrue(any("Re-review required" in error for error in errors))

    def test_pre_seeded_audit_for_not_started_step_is_reported(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            status = root / "docs/planning/status.md"
            status.parent.mkdir(parents=True)
            status.write_text(
                "| 07 | Step | Phase | Not started | — | — | — | — | — | Not requested | Not requested |\n",
                encoding="utf-8",
            )
            audit = root / "docs/planning/audits/step-07.md"
            audit.parent.mkdir(parents=True)
            audit.write_text("# Audit\n", encoding="utf-8")
            errors = check_unused_audit_records(root)

        self.assertTrue(any("step-07.md" in error for error in errors))

    def test_pre_seeded_audit_for_not_started_split_step_is_reported(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            status = root / "docs/planning/status.md"
            status.parent.mkdir(parents=True)
            status.write_text(
                "| 23a | Step | Phase | Not started | — | — | — | — | — | Not requested | Not requested |\n",
                encoding="utf-8",
            )
            audit = root / "docs/planning/audits/step-23a.md"
            audit.parent.mkdir(parents=True)
            audit.write_text("# Audit\n", encoding="utf-8")
            errors = check_unused_audit_records(root)

        self.assertTrue(any("step-23a.md" in error for error in errors))

    def test_missing_in_audit_status_is_reported(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            status = root / "docs/planning/status.md"
            status.parent.mkdir(parents=True)
            status.write_text("Not started, In progress, Done\n", encoding="utf-8")
            errors = check_status_vocabulary(root)

        self.assertTrue(any("In audit" in error for error in errors))


if __name__ == "__main__":
    unittest.main()
