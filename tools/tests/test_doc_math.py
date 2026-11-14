# SPDX-License-Identifier: MIT
"""Tests for tools.check_doc_math."""

from __future__ import annotations

import tempfile
import unittest
from pathlib import Path

from tools.check_doc_math import check_repository


class DocMathTests(unittest.TestCase):
    def setUp(self) -> None:
        self._tmp = tempfile.TemporaryDirectory()
        self.root = Path(self._tmp.name)

    def tearDown(self) -> None:
        self._tmp.cleanup()

    def write(self, rel: str, text: str) -> Path:
        path = self.root / rel
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(text, encoding="utf-8")
        return path

    def test_planning_accepts_dollar_math(self) -> None:
        self.write("docs/planning/draft.md", "Inline $x_v$ and display $$E = mc^2$$ math.\n")
        self.assertEqual(check_repository(self.root), [])

    def test_planning_warns_on_backslash_inline(self) -> None:
        self.write("docs/planning/draft.md", "Inline \\(x\\) math.\n")
        warnings = check_repository(self.root)
        self.assertEqual(len(warnings), 1)
        self.assertIn("planning docs", warnings[0].message)
        self.assertEqual(warnings[0].line, 1)

    def test_planning_warns_on_bracket_display(self) -> None:
        self.write("docs/planning/draft.md", "\\[x = 1\\]\n")
        warnings = check_repository(self.root)
        self.assertEqual(len(warnings), 1)
        self.assertIn("planning docs", warnings[0].message)

    def test_published_accepts_katex_delimiters(self) -> None:
        self.write("docs/page.md", "Inline \\(x\\) and display $$E = mc^2$$ math.\n")
        self.assertEqual(check_repository(self.root), [])

    def test_published_warns_on_single_dollar(self) -> None:
        self.write("docs/page.md", "Inline $x_v$ math.\n")
        warnings = check_repository(self.root)
        self.assertEqual(len(warnings), 1)
        self.assertIn("published docs", warnings[0].message)
        self.assertEqual(warnings[0].line, 1)

    def test_published_warns_on_bracket_display(self) -> None:
        self.write("docs/page.md", "\\[x = 1\\]\n")
        warnings = check_repository(self.root)
        self.assertEqual(len(warnings), 1)
        self.assertIn("$$...$$", warnings[0].message)

    def test_published_ignores_currency(self) -> None:
        self.write("docs/page.md", "A cost of $5 and $10, or $1,000.00.\n")
        self.assertEqual(check_repository(self.root), [])

    def test_code_fences_and_inline_code_are_ignored(self) -> None:
        self.write(
            "docs/page.md",
            "```text\n$x_v$ and \\(x\\)\n```\n\nInline `$x_v$` and `\\(x\\)` code.\n",
        )
        self.assertEqual(check_repository(self.root), [])

    def test_planning_classification_is_exact(self) -> None:
        self.write("docs/planning-notes/page.md", "Inline $x_v$ math.\n")
        warnings = check_repository(self.root)
        self.assertEqual(len(warnings), 1)
        self.assertIn("published docs", warnings[0].message)

    def test_line_numbers_are_preserved(self) -> None:
        self.write("docs/page.md", "First line.\n\nInline $x_v$ math.\n")
        warnings = check_repository(self.root)
        self.assertEqual(len(warnings), 1)
        self.assertEqual(warnings[0].line, 3)

    def test_missing_docs_directory_is_empty(self) -> None:
        self.assertEqual(check_repository(self.root), [])


if __name__ == "__main__":
    unittest.main()
