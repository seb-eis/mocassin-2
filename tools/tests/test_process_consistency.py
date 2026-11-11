# SPDX-License-Identifier: MIT
"""Tests for tools.check_process_consistency."""

from __future__ import annotations

import unittest
from pathlib import Path

from tools.check_process_consistency import check

ROOT = Path(__file__).resolve().parents[2]


class ProcessConsistencyTests(unittest.TestCase):
    def test_repository_process_is_consistent(self) -> None:
        self.assertEqual(check(ROOT), [])


if __name__ == "__main__":
    unittest.main()
