#!/usr/bin/env python3
# SPDX-License-Identifier: MIT

"""Verify the canonical parameterization projection fixtures (ADR 0010)."""

from __future__ import annotations

import hashlib
import json
import sys
from pathlib import Path

try:
    import msgpack
except ImportError as exc:  # pragma: no cover - toolchain guard
    raise SystemExit("msgpack is required; install tools/contracts/requirements.txt") from exc

from check_model_projection import encode_projection

ROOT = Path(__file__).resolve().parents[2]
DESCRIPTOR = ROOT / "contracts" / "generated" / "signatures" / "parameterization-v1.json"
FIXTURES = ROOT / "tests" / "contracts" / "projections" / "parameterization"
DOMAIN = b"mocassin/parameterization-signature/v1"


def check() -> int:
    """Re-encode every fixture from the generated descriptor and compare hashes."""
    descriptor = json.loads(DESCRIPTOR.read_text(encoding="utf-8"))
    if descriptor.get("status") != "final":
        print("parameterization descriptor is not final", file=sys.stderr)
        return 1
    if descriptor.get("projectionType") != "ParameterizationSignatureV1":
        print("parameterization descriptor has the wrong projectionType", file=sys.stderr)
        return 1
    if descriptor.get("domainPrefix") != DOMAIN.decode("utf-8"):
        print("parameterization descriptor has the wrong domainPrefix", file=sys.stderr)
        return 1

    failures = 0
    fixture_count = 0
    for binary_path in sorted(FIXTURES.glob("*.bin")):
        fixture_count += 1
        metadata_path = binary_path.with_suffix(".json")
        if not metadata_path.exists():
            print(f"missing metadata {metadata_path}", file=sys.stderr)
            failures += 1
            continue

        raw = binary_path.read_bytes()
        metadata = json.loads(metadata_path.read_text(encoding="utf-8"))
        try:
            tree = msgpack.unpackb(raw, raw=False, strict_map_key=False)
            if not isinstance(tree, list) or len(tree) != 6:
                raise ValueError("projection must be a six-field array")
            reencoded = encode_projection(descriptor, tree)
        except Exception as exc:  # noqa: BLE001 - report every fixture failure
            print(f"{binary_path.name}: {exc}", file=sys.stderr)
            failures += 1
            continue

        signature = hashlib.sha256(DOMAIN + b"\x00" + reencoded).hexdigest()
        projection_sha = hashlib.sha256(reencoded).hexdigest()
        expected = {
            "fixtureFormat": "canonical-projection-v1",
            "descriptor": "parameterization-v1",
            "modelSignature": tree[1],
            "signatureHex": signature,
            "projectionSha256": projection_sha,
        }
        for key, value in expected.items():
            if metadata.get(key) != value:
                print(
                    f"{binary_path.name}: {key} mismatch ({metadata.get(key)!r} != {value!r})",
                    file=sys.stderr,
                )
                failures += 1
        if reencoded != raw:
            print(f"{binary_path.name}: canonical bytes differ", file=sys.stderr)
            failures += 1

    if fixture_count == 0:
        print("no parameterization projection fixtures found", file=sys.stderr)
        return 1
    if failures:
        print(
            f"Parameterization projection check failed with {failures} error(s).",
            file=sys.stderr,
        )
        return 1

    print(f"Parameterization projection check OK ({fixture_count} fixtures).")
    return 0


if __name__ == "__main__":
    raise SystemExit(check())
