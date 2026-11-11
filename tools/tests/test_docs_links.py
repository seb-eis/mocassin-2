# SPDX-License-Identifier: MIT
"""Tests for tools.check_docs_links."""

from __future__ import annotations

import tempfile
import unittest
from pathlib import Path

from tools.check_docs_links import check_repository


class DocsLinksTests(unittest.TestCase):
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

    def test_valid_relative_link_and_anchor(self) -> None:
        self.write("docs/page.md", "# Page\n\n## A section\n")
        self.write("README.md", "[Page](docs/page.md)\n[Section](docs/page.md#a-section)\n")
        self.assertEqual(check_repository(self.root), [])

    def test_missing_file_is_reported(self) -> None:
        self.write("README.md", "[Missing](docs/missing.md)\n")
        errors = check_repository(self.root)
        self.assertEqual(len(errors), 1)
        self.assertIn("missing link target", errors[0])

    def test_missing_anchor_is_reported(self) -> None:
        self.write("docs/page.md", "# Page\n")
        self.write("README.md", "[Bad](docs/page.md#nope)\n")
        errors = check_repository(self.root)
        self.assertEqual(len(errors), 1)
        self.assertIn("missing anchor", errors[0])

    def test_code_fence_is_ignored(self) -> None:
        self.write("README.md", "```\n[Bad](docs/missing.md)\n```\n")
        self.assertEqual(check_repository(self.root), [])

    def test_external_and_placeholder_links_are_ignored(self) -> None:
        self.write("README.md", "[Web](https://example.com)\n[Placeholder]({{LINK}})\n[Step](step-NN.md)\n")
        self.assertEqual(check_repository(self.root), [])

    def test_directory_resolves_to_index_or_readme(self) -> None:
        self.write("docs/guide/README.md", "# Guide\n")
        self.write("README.md", "[Guide](docs/guide)\n")
        self.assertEqual(check_repository(self.root), [])


if __name__ == "__main__":
    unittest.main()
