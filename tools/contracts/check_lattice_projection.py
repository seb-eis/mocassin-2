#!/usr/bin/env python3
# SPDX-License-Identifier: MIT

"""Verify the canonical lattice projection fixtures (ADR 0010).

The five-field lattice layout is defined by ADR 0010 and finalized in Step 11. Until the generated
``signatures/lattice-v1.json`` descriptor is marked ``final`` by the manifest feature, this checker re-encodes
against the documented fixed layout. Once the descriptor is final, it must match that layout and is used as the
single source.
"""

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
DESCRIPTOR = ROOT / "contracts" / "generated" / "signatures" / "lattice-v1.json"
FIXTURES = ROOT / "tests" / "contracts" / "projections" / "lattice"
DOMAIN = b"mocassin/lattice-signature/v1"

FIXED_FIELDS = [
    {"name": "formatVersion", "type": "uint64", "required": True},
    {"name": "modelSignature", "type": "string", "required": True},
    {"name": "latticeSize", "type": "array", "items": "uint64", "length": 3, "required": True},
    {"name": "positionCount", "type": "uint64", "required": True},
    {"name": "particles", "type": "bin", "required": True},
]


def check_final_descriptor(descriptor: dict) -> list[str]:
    """Return problems when a final generated descriptor differs from the documented five fields."""
    problems: list[str] = []
    if descriptor.get("projectionType") != "LatticeSignatureV1":
        problems.append("lattice descriptor has the wrong projectionType")
    if descriptor.get("domainPrefix") != DOMAIN.decode("utf-8"):
        problems.append("lattice descriptor has the wrong domainPrefix")
    fields = descriptor.get("fields", [])
    if len(fields) != len(FIXED_FIELDS):
        problems.append(f"lattice descriptor has {len(fields)} fields, expected {len(FIXED_FIELDS)}")
        return problems
    for expected, actual in zip(FIXED_FIELDS, fields, strict=True):
        for key in ("name", "type", "required", "items", "length"):
            if key in expected and actual.get(key) != expected[key]:
                problems.append(
                    f"lattice field {expected['name']!r} has {key}={actual.get(key)!r}, "
                    f"expected {expected[key]!r}"
                )
    return problems


def check() -> int:
    """Re-encode every fixture and compare its signature and canonical bytes."""
    descriptor = json.loads(DESCRIPTOR.read_text(encoding="utf-8"))
    problems: list[str] = []
    if descriptor.get("status") == "final":
        problems.extend(check_final_descriptor(descriptor))

    fixture_count = 0
    for binary_path in sorted(FIXTURES.glob("*.bin")):
        fixture_count += 1
        metadata_path = binary_path.with_suffix(".json")
        if not metadata_path.exists():
            problems.append(f"missing metadata {metadata_path}")
            continue

        raw = binary_path.read_bytes()
        metadata = json.loads(metadata_path.read_text(encoding="utf-8"))
        try:
            tree = msgpack.unpackb(raw, raw=False, strict_map_key=False)
            if not isinstance(tree, list) or len(tree) != 5:
                raise ValueError("projection must be a five-field array")
            if not isinstance(tree[2], list) or len(tree[2]) != 3:
                raise ValueError("latticeSize must contain exactly three values")
            if not isinstance(tree[4], (bytes, bytearray)):
                raise ValueError("particles must be a MessagePack bin value")
            reencoded = encode_projection({"fields": FIXED_FIELDS}, tree)
        except Exception as exc:  # noqa: BLE001 - report every fixture failure
            problems.append(f"{binary_path.name}: {exc}")
            continue

        signature = hashlib.sha256(DOMAIN + b"\x00" + reencoded).hexdigest()
        projection_sha = hashlib.sha256(reencoded).hexdigest()
        expected = {
            "fixtureFormat": "canonical-projection-v1",
            "descriptor": "lattice-v1",
            "modelSignature": tree[1],
            "signatureHex": signature,
            "projectionSha256": projection_sha,
        }
        for key, value in expected.items():
            if metadata.get(key) != value:
                problems.append(
                    f"{binary_path.name}: {key} mismatch ({metadata.get(key)!r} != {value!r})"
                )
        if reencoded != raw:
            problems.append(f"{binary_path.name}: canonical bytes differ")

    if fixture_count == 0:
        problems.append("no lattice projection fixtures found")
    if problems:
        for problem in problems:
            print(problem, file=sys.stderr)
        print(f"Lattice projection check failed with {len(problems)} error(s).", file=sys.stderr)
        return 1

    print(f"Lattice projection check OK ({fixture_count} fixtures).")
    return 0


if __name__ == "__main__":
    raise SystemExit(check())
