# SPDX-License-Identifier: MIT

"""Unit vectors for the parameterization projection checker's quantization and encoding."""

from __future__ import annotations

import unittest

from tools.contracts.check_model_projection import encode_projection, quantize


class ParameterizationProjectionQuantizationTests(unittest.TestCase):
    """Pins the 12-decimal midpoint behaviour shared by both projection checkers."""

    def test_midpoint_vectors(self) -> None:
        cases = [
            (5e-13, 1e-12),
            (-5e-13, -1e-12),
            (-4e-13, 0.0),
            (0.30000000000000004, 0.3),
            (2.0, 2.0),
            (-0.0, 0.0),
        ]
        for value, expected in cases:
            with self.subTest(value=value):
                self.assertEqual(quantize(value), expected)

    def test_projection_encoding_quantizes_nested_values(self) -> None:
        descriptor = {
            "fields": [
                {"name": "formatVersion", "type": "uint64", "required": True},
                {"name": "modelSignature", "type": "string", "required": True},
                {
                    "name": "pairTables",
                    "type": "array",
                    "required": True,
                    "items": "array",
                    "itemItems": {"type": "array", "items": "float64"},
                },
                {
                    "name": "groupSets",
                    "type": "array",
                    "required": True,
                    "items": "array",
                    "itemItems": {"type": "array", "items": "float64"},
                },
                {"name": "siteEnergies", "type": "array", "required": True, "items": "float64"},
                {"name": "transitionRules", "type": "array", "required": True, "items": "float64"},
            ]
        }
        raw = [1, "model", [[5e-13, -5e-13]], [[]], [-0.0], [1.0e13]]
        quantized = [1, "model", [[1e-12, -1e-12]], [[]], [0.0], [1.0e13]]

        self.assertEqual(
            encode_projection(descriptor, raw),
            encode_projection(descriptor, quantized),
        )


if __name__ == "__main__":
    unittest.main()
