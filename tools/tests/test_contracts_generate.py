# SPDX-License-Identifier: MIT

"""Tests for the package payload manifest descriptors and generated schemas."""

from __future__ import annotations

import copy
import json
import unittest

from tools.contracts import generate


class PackageManifestTests(unittest.TestCase):
    """Pins the Step 11 package-shape finalization in the manifest and generator."""

    def setUp(self) -> None:
        self.manifest = generate.load_manifest()
        self.package = self.manifest["formats"]["package"]

    def test_lattice_signature_is_final_and_uses_the_five_field_projection(self) -> None:
        lattice = self.manifest["signatures"]["lattice"]
        self.assertEqual("final", lattice["status"])
        self.assertEqual("LatticeSignatureV1", lattice["projectionType"])
        self.assertEqual(
            ["formatVersion", "modelSignature", "latticeSize", "positionCount", "particles"],
            [field["name"] for field in lattice["fields"]],
        )
        self.assertEqual(
            ["uint64", "string", "array", "uint64", "bin"],
            [field["type"] for field in lattice["fields"]],
        )
        self.assertNotIn("dopingInfo", [field["name"] for field in lattice["fields"]])

    def test_payload_group_defines_model_job_and_lattice(self) -> None:
        payloads = self.package["payloads"]
        self.assertEqual("ignore", payloads["unknownFields"])
        self.assertEqual(1, payloads["formatVersion"])
        self.assertEqual(
            {"model", "job", "lattice"},
            {key for key in payloads if key not in {"formatVersion", "unknownFields", "notes"}},
        )
        for kind in ("model", "job", "lattice"):
            version_fields = [
                field for field in payloads[kind]["fields"] if field["name"] == "format_version"
            ]
            self.assertEqual(1, len(version_fields))
            self.assertEqual([1], version_fields[0]["enum"])

    def test_job_table_drops_undefined_columns_and_keeps_write_out_count(self) -> None:
        fields = [field["name"] for field in self.package["tables"]["jobs"]["fields"]]
        self.assertNotIn("norm_factor", fields)
        self.assertNotIn("base_frequency_hz", fields)
        self.assertIn("write_out_count", fields)
        self.assertLess(fields.index("target_mcsp"), fields.index("write_out_count"))
        self.assertLess(fields.index("write_out_count"), fields.index("pre_run_mcsp"))

    def test_payload_schema_is_separate_and_manifest_driven(self) -> None:
        artifacts = generate.build_artifacts(self.manifest)
        row = json.loads(
            artifacts[generate.GENERATED_DIR / "schema" / "package-v1.schema.json"]
        )
        payloads = json.loads(
            artifacts[generate.GENERATED_DIR / "schema" / "package-payloads-v1.schema.json"]
        )

        self.assertFalse(row["additionalProperties"])
        self.assertIn("schema_info", row["properties"])
        self.assertNotIn("model", row["properties"])
        self.assertEqual(
            "https://mocassin.dev/schemas/package-payloads-v1.json",
            payloads["$id"],
        )
        self.assertFalse(payloads["additionalProperties"])
        for kind in ("model", "job", "lattice"):
            self.assertTrue(payloads["properties"][kind]["additionalProperties"])
            self.assertIn("format_version", payloads["properties"][kind]["properties"])

    def test_payload_unknown_field_policy_comes_from_the_manifest(self) -> None:
        manifest = copy.deepcopy(self.manifest)
        manifest["formats"]["package"]["payloads"]["unknownFields"] = "reject"
        artifacts = generate.build_artifacts(manifest)
        payloads = json.loads(
            artifacts[generate.GENERATED_DIR / "schema" / "package-payloads-v1.schema.json"]
        )
        for kind in ("model", "job", "lattice"):
            self.assertFalse(payloads["properties"][kind]["additionalProperties"])

    def test_unknown_payload_kind_is_rejected(self) -> None:
        manifest = copy.deepcopy(self.manifest)
        manifest["formats"]["package"]["payloads"]["state"] = {
            "fields": [
                {"name": "format_version", "type": "uint64", "required": True, "enum": [1]}
            ]
        }
        with self.assertRaises(ValueError):
            generate.validate_manifest(manifest)

    def test_payload_format_version_must_match_the_group_version(self) -> None:
        manifest = copy.deepcopy(self.manifest)
        manifest["formats"]["package"]["payloads"]["lattice"]["fields"][0]["enum"] = [2]
        with self.assertRaises(ValueError):
            generate.validate_manifest(manifest)

    def test_payloads_are_only_allowed_on_the_package_format(self) -> None:
        manifest = copy.deepcopy(self.manifest)
        manifest["formats"]["state"]["payloads"] = manifest["formats"]["package"]["payloads"]
        with self.assertRaises(ValueError):
            generate.validate_manifest(manifest)


if __name__ == "__main__":
    unittest.main()
