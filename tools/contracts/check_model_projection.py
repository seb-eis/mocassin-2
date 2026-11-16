#!/usr/bin/env python3
# SPDX-License-Identifier: MIT
"""Verify the canonical compiled-model projection fixtures (ADR 0010)."""

from __future__ import annotations

import hashlib
import json
import math
import struct
import sys
from pathlib import Path

try:
    import msgpack
except ImportError as exc:  # pragma: no cover - toolchain guard
    raise SystemExit("msgpack is required; install tools/contracts/requirements.txt") from exc

ROOT = Path(__file__).resolve().parents[2]
DESCRIPTOR = ROOT / "contracts" / "generated" / "signatures" / "model-v1.json"
FIXTURES = ROOT / "tests" / "contracts" / "projections"
DOMAIN = b"mocassin/model-signature/v1"


def array_header(count: int) -> bytes:
    return b"\xdd" + struct.pack(">I", count)


def string(value: str) -> bytes:
    data = value.encode("utf-8")
    return b"\xdb" + struct.pack(">I", len(data)) + data


def quantize(value: float) -> float:
    """Quantize a projected double to 12 decimals, round half away from zero."""
    scaled = value * 1e12
    rounded = math.floor(scaled + 0.5) if scaled >= 0.0 else math.ceil(scaled - 0.5)
    result = rounded / 1e12
    return 0.0 if result == 0.0 else result


def encode_field(field: dict, value) -> bytes:
    kind = field["type"]
    if kind == "uint64":
        return b"\xcf" + struct.pack(">Q", int(value))
    if kind == "int64":
        return b"\xd3" + struct.pack(">q", int(value))
    if kind == "float64":
        return b"\xcb" + struct.pack(">d", quantize(float(value)))
    if kind == "bool":
        return b"\xc3" if value else b"\xc2"
    if kind == "nil":
        return b"\xc0"
    if kind == "string":
        if value is None:
            return b"\xc0"
        return string(value)
    if kind == "bin":
        data = bytes(value)
        return b"\xc6" + struct.pack(">I", len(data)) + data
    if kind == "array":
        items = field.get("items", "object")
        return array_header(len(value)) + b"".join(encode_item(field, items, item) for item in value)
    if kind == "object":
        fields = field.get("fields", [])
        if len(fields) != len(value):
            raise ValueError(f"object {field['name']} has {len(value)} values, expected {len(fields)}")
        return array_header(len(fields)) + b"".join(encode_field(f, v) for f, v in zip(fields, value, strict=True))
    raise ValueError(f"unsupported projection type {kind!r}")


def encode_item(field: dict, item_type: str, value) -> bytes:
    if field.get("itemFields") is not None:
        fields = field["itemFields"]
        if len(fields) != len(value):
            raise ValueError(f"{field['name']} item has {len(value)} values, expected {len(fields)}")
        return array_header(len(fields)) + b"".join(encode_field(f, v) for f, v in zip(fields, value, strict=True))
    if field.get("itemItems") is not None:
        inner = field["itemItems"]
        descriptor = {"name": "item", "required": False, **inner}
        return encode_field(descriptor, value)
    return encode_field({"name": "item", "type": item_type, "required": False}, value)


def encode_projection(descriptor: dict, tree: list) -> bytes:
    fields = descriptor["fields"]
    if len(fields) != len(tree):
        raise ValueError(f"projection has {len(tree)} fields, expected {len(fields)}")
    return array_header(len(fields)) + b"".join(encode_field(f, v) for f, v in zip(fields, tree, strict=True))


def check() -> int:
    descriptor = json.loads(DESCRIPTOR.read_text(encoding="utf-8"))
    failures = 0
    for binary_path in sorted(FIXTURES.glob("*.bin")):
        metadata_path = binary_path.with_suffix(".json")
        if not metadata_path.exists():
            print(f"missing metadata {metadata_path}", file=sys.stderr)
            failures += 1
            continue
        raw = binary_path.read_bytes()
        metadata = json.loads(metadata_path.read_text(encoding="utf-8"))
        try:
            tree = msgpack.unpackb(raw, raw=False, strict_map_key=False)
            reencoded = encode_projection(descriptor, tree)
        except Exception as exc:  # noqa: BLE001 - report every fixture failure
            print(f"{binary_path.name}: {exc}", file=sys.stderr)
            failures += 1
            continue
        signature = hashlib.sha256(DOMAIN + b"\x00" + reencoded).hexdigest()
        projection_sha = hashlib.sha256(reencoded).hexdigest()
        expected = {
            "fixtureFormat": "canonical-projection-v1",
            "descriptor": "model-v1",
            "signatureHex": signature,
            "projectionSha256": projection_sha,
        }
        for key, value in expected.items():
            if metadata.get(key) != value:
                print(f"{binary_path.name}: {key} mismatch ({metadata.get(key)!r} != {value!r})", file=sys.stderr)
                failures += 1
        if reencoded != raw:
            print(f"{binary_path.name}: canonical bytes differ", file=sys.stderr)
            failures += 1
    if failures:
        print(f"Model projection check failed with {failures} error(s).", file=sys.stderr)
        return 1
    print(f"Model projection check OK ({len(list(FIXTURES.glob('*.bin')))} fixtures).")
    return 0


if __name__ == "__main__":
    raise SystemExit(check())
