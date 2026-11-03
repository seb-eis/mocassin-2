#!/usr/bin/env python3
# SPDX-License-Identifier: MIT

"""Validate the contract manifest and generate checked-in contract artifacts."""

from __future__ import annotations

import json
import os
import re
import sys
from pathlib import Path
from typing import Any

ROOT = Path(__file__).resolve().parents[2]
MANIFEST_PATH = ROOT / "contracts" / "manifest.json"
GENERATED_DIR = ROOT / "contracts" / "generated"
SIGNATURE_VECTORS_PATH = ROOT / "tests" / "contracts" / "signature-vectors.json"

REQUIRED_FORMATS = {
    "project",
    "package",
    "results",
    "state",
    "solver",
    "jobFolder",
    "events",
    "mmcfe",
}
REQUIRED_SIGNATURES = {"model", "parameterization", "lattice"}
VALID_FIELD_TYPES = {
    "array",
    "bin",
    "bool",
    "float64",
    "int64",
    "map",
    "nil",
    "object",
    "string",
    "uint64",
}
FORMAT_CONSTANTS = {
    "project": "ProjectSchemaVersion",
    "package": "PackageFormatVersion",
    "results": "ResultsFormatVersion",
    "state": "StateFormatVersion",
    "solver": "SolverFormatVersion",
    "jobFolder": "JobFolderFormatVersion",
    "events": "EventsSchemaVersion",
    "mmcfe": "MmcfeFormatVersion",
}
FORMAT_BASE_NAMES = {
    "project": "Project",
    "package": "Package",
    "results": "Results",
    "state": "State",
    "solver": "Solver",
    "jobFolder": "JobFolder",
    "events": "Events",
    "mmcfe": "Mmcfe",
}
STRING_SUFFIXES = {
    "format": "Format",
    "fileName": "FileName",
    "fileExtension": "FileExtension",
    "encoding": "Encoding",
}


def fail(message: str) -> None:
    """Raise a manifest validation error."""
    raise ValueError(message)


def require(condition: bool, message: str) -> None:
    """Raise a manifest validation error when a condition is false."""
    if not condition:
        fail(message)


def load_manifest() -> dict[str, Any]:
    """Load and parse the contract manifest."""
    try:
        with MANIFEST_PATH.open(encoding="utf-8") as stream:
            manifest = json.load(stream)
    except (OSError, json.JSONDecodeError) as error:
        fail(f"cannot read {MANIFEST_PATH.relative_to(ROOT)}: {error}")
    require(isinstance(manifest, dict), "manifest root must be an object")
    return manifest


def validate_field(field: dict[str, Any], context: str) -> None:
    """Validate one field descriptor."""
    require(isinstance(field, dict), f"{context}: field must be an object")
    name = field.get("name")
    require(isinstance(name, str) and name, f"{context}: field name must be a non-empty string")
    field_type = field.get("type")
    require(field_type in VALID_FIELD_TYPES, f"{context}.{name}: unsupported type {field_type!r}")
    require(isinstance(field.get("required"), bool), f"{context}.{name}: required must be a boolean")
    if field_type == "array":
        item_type = field.get("items", "object")
        require(item_type in VALID_FIELD_TYPES, f"{context}.{name}: unsupported item type {item_type!r}")
        if "length" in field:
            length = field["length"]
            require(isinstance(length, int) and length > 0, f"{context}.{name}: length must be positive")


def validate_fields(fields: list[dict[str, Any]], context: str) -> None:
    """Validate a field list and reject duplicate names."""
    require(isinstance(fields, list) and fields, f"{context}: fields must be a non-empty array")
    names: set[str] = set()
    for field in fields:
        validate_field(field, context)
        name = field["name"]
        require(name not in names, f"{context}: duplicate field {name!r}")
        names.add(name)


def validate_manifest(manifest: dict[str, Any]) -> None:
    """Validate the manifest envelope, formats, signatures, events, and fixtures."""
    require(manifest.get("manifestVersion") == 1, "manifestVersion must be 1")
    require(isinstance(manifest.get("project"), str), "project must be a string")

    formats = manifest.get("formats")
    require(isinstance(formats, dict), "formats must be an object")
    require(set(formats) == REQUIRED_FORMATS, "formats must define exactly the required format ids")

    for format_id, descriptor in formats.items():
        require(isinstance(descriptor, dict), f"formats.{format_id}: descriptor must be an object")
        require(descriptor.get("id") == format_id, f"formats.{format_id}: id must match the key")
        require(isinstance(descriptor.get("title"), str), f"formats.{format_id}: title must be a string")
        require(
            descriptor.get("unknownFields") in {"reject", "ignore"},
            f"formats.{format_id}: unknownFields must be 'reject' or 'ignore'",
        )
        version = descriptor.get("schemaVersion", descriptor.get("formatVersion"))
        require(isinstance(version, int) and version >= 1, f"formats.{format_id}: version must be positive")

        has_content = any(key in descriptor for key in ("fields", "tables", "events"))
        require(has_content, f"formats.{format_id}: must define fields, tables, or events")

        if "fields" in descriptor:
            validate_fields(descriptor["fields"], f"formats.{format_id}.fields")
        if "tables" in descriptor:
            tables = descriptor["tables"]
            require(isinstance(tables, dict) and tables, f"formats.{format_id}.tables must be a non-empty object")
            for table_name, table in tables.items():
                require(isinstance(table, dict), f"formats.{format_id}.tables.{table_name}: must be an object")
                validate_fields(table.get("fields", []), f"formats.{format_id}.tables.{table_name}")
        if "events" in descriptor:
            events = descriptor["events"]
            require(isinstance(events, list) and events, f"formats.{format_id}.events must be a non-empty array")
            event_names: set[str] = set()
            for event in events:
                require(isinstance(event, dict), f"formats.{format_id}.events: entry must be an object")
                name = event.get("name")
                require(isinstance(name, str) and name, f"formats.{format_id}.events: name must be a string")
                require(name not in event_names, f"formats.{format_id}.events: duplicate event {name!r}")
                event_names.add(name)
                required_fields = event.get("requiredFields")
                require(
                    isinstance(required_fields, list)
                    and all(isinstance(item, str) and item for item in required_fields),
                    f"formats.{format_id}.events.{name}: requiredFields must be strings",
                )

    signatures = manifest.get("signatures")
    require(isinstance(signatures, dict), "signatures must be an object")
    require(set(signatures) == REQUIRED_SIGNATURES, "signatures must define exactly the required signature ids")
    for signature_id, descriptor in signatures.items():
        require(isinstance(descriptor, dict), f"signatures.{signature_id}: descriptor must be an object")
        require(descriptor.get("id") == signature_id, f"signatures.{signature_id}: id must match the key")
        require(
            isinstance(descriptor.get("domainPrefix"), str) and descriptor["domainPrefix"].startswith("mocassin/"),
            f"signatures.{signature_id}: domainPrefix must start with 'mocassin/'",
        )
        require(
            isinstance(descriptor.get("projectionType"), str) and descriptor["projectionType"],
            f"signatures.{signature_id}: projectionType must be a string",
        )
        validate_fields(descriptor.get("fields", []), f"signatures.{signature_id}.fields")

    fixtures = manifest.get("fixtures")
    require(isinstance(fixtures, dict), "fixtures must be an object")
    vectors = fixtures.get("signatureVectors")
    require(isinstance(vectors, dict), "fixtures.signatureVectors must be an object")
    require(
        vectors.get("path") == "tests/contracts/signature-vectors.json",
        "fixtures.signatureVectors.path must be tests/contracts/signature-vectors.json",
    )
    items = fixtures.get("items")
    require(isinstance(items, list) and items, "fixtures.items must be a non-empty array")
    fixture_ids: set[str] = set()
    for item in items:
        require(isinstance(item, dict), "fixtures.items: entry must be an object")
        fixture_id = item.get("id")
        require(isinstance(fixture_id, str) and fixture_id, "fixtures.items: id must be a string")
        require(fixture_id not in fixture_ids, f"fixtures.items: duplicate id {fixture_id!r}")
        fixture_ids.add(fixture_id)
        require(isinstance(item.get("path"), str) and item["path"], f"fixtures.items.{fixture_id}: path required")


def json_schema_type(field: dict[str, Any]) -> dict[str, Any]:
    """Map a manifest field descriptor to a JSON Schema fragment."""
    field_type = field["type"]
    if field_type == "uint64":
        return {"type": "integer", "minimum": 0}
    if field_type == "int64":
        return {"type": "integer"}
    if field_type == "float64":
        return {"type": "number"}
    if field_type == "bool":
        return {"type": "boolean"}
    if field_type == "bin":
        return {"type": "string", "contentEncoding": "base64"}
    if field_type == "map":
        return {"type": "object"}
    if field_type == "object":
        return {"type": "object"}
    if field_type == "nil":
        return {"type": "null"}
    if field_type == "array":
        schema: dict[str, Any] = {
            "type": "array",
            "items": json_schema_type({"type": field.get("items", "object"), "required": False}),
        }
        if "length" in field:
            schema["minItems"] = field["length"]
            schema["maxItems"] = field["length"]
        return schema
    return {"type": "string"}


def field_properties(fields: list[dict[str, Any]]) -> tuple[dict[str, Any], list[str]]:
    """Build JSON Schema properties and required names from a field list."""
    properties = {field["name"]: json_schema_type(field) for field in fields}
    required = [field["name"] for field in fields if field["required"]]
    return properties, required


def format_version(descriptor: dict[str, Any]) -> int:
    """Return the schema or format version for a format descriptor."""
    return int(descriptor.get("schemaVersion", descriptor.get("formatVersion")))


def schema_for_format(descriptor: dict[str, Any], manifest_version: int) -> dict[str, Any]:
    """Build a JSON Schema fragment for one format."""
    version = format_version(descriptor)
    additional = descriptor["unknownFields"] == "ignore"
    schema: dict[str, Any] = {
        "$schema": "https://json-schema.org/draft/2020-12/schema",
        "$id": f"https://mocassin.dev/schemas/{descriptor['id']}-v{version}.json",
        "title": descriptor["title"],
        "description": f"MOCASSIN contract manifest v{manifest_version}; generated from contracts/manifest.json.",
        "type": "object",
        "additionalProperties": additional,
    }

    if "fields" in descriptor:
        properties, required = field_properties(descriptor["fields"])
        schema["properties"] = properties
        schema["required"] = required
    elif "tables" in descriptor:
        properties = {}
        for table_name, table in descriptor["tables"].items():
            table_properties, table_required = field_properties(table["fields"])
            properties[table_name] = {
                "type": "object",
                "properties": table_properties,
                "required": table_required,
                "additionalProperties": additional,
            }
        schema["properties"] = properties
        schema["required"] = list(properties)
    elif "events" in descriptor:
        event_names = [event["name"] for event in descriptor["events"]]
        schema["properties"] = {
            "schemaVersion": {"const": version},
            "event": {"type": "string", "enum": event_names},
        }
        schema["required"] = ["schemaVersion", "event"]
        schema["x-event-required-fields"] = {
            event["name"]: event["requiredFields"] for event in descriptor["events"]
        }
    return schema


def json_text(value: Any) -> str:
    """Serialize a value deterministically with a trailing newline."""
    return json.dumps(value, indent=2, ensure_ascii=False) + "\n"


def to_screaming_snake(name: str) -> str:
    """Convert a PascalCase constant name to SCREAMING_SNAKE_CASE."""
    return re.sub(r"(?<!^)(?=[A-Z])", "_", name).upper()


def constants_for(manifest: dict[str, Any]) -> tuple[str, str, str]:
    """Generate C#, Rust, and Python version constants."""
    formats = manifest["formats"]
    lines_cs = [
        "// SPDX-License-Identifier: MIT",
        "// <auto-generated>",
        "// Generated by tools/contracts/generate.py from contracts/manifest.json. Do not edit.",
        "// </auto-generated>",
        "",
        "namespace Mocassin.Contracts;",
        "",
        "/// <summary>Version constants generated from the contract manifest.</summary>",
        "public static class ContractVersions",
        "{",
        "    /// <summary>Contract manifest version.</summary>",
        f"    public const int ManifestVersion = {manifest['manifestVersion']};",
    ]
    lines_rs = [
        "// SPDX-License-Identifier: MIT",
        "// @generated by tools/contracts/generate.py from contracts/manifest.json. Do not edit.",
        "",
        f"pub const MANIFEST_VERSION: u64 = {manifest['manifestVersion']};",
    ]
    lines_py = [
        "# SPDX-License-Identifier: MIT",
        "# @generated by tools/contracts/generate.py from contracts/manifest.json. Do not edit.",
        "",
        f"MANIFEST_VERSION = {manifest['manifestVersion']}",
    ]

    for format_id in sorted(formats):
        descriptor = formats[format_id]
        version = format_version(descriptor)
        constant = FORMAT_CONSTANTS[format_id]
        lines_cs.extend(
            [
                "",
                f"    /// <summary>{descriptor['title']} version.</summary>",
                f"    public const int {constant} = {version};",
            ]
        )
        rust_constant = to_screaming_snake(constant)
        lines_rs.append(f"pub const {rust_constant}: u64 = {version};")
        lines_py.append(f"{to_screaming_snake(constant)} = {version}")

        for key in ("format", "fileName", "fileExtension", "encoding"):
            if key not in descriptor:
                continue
            csharp_name = f"{FORMAT_BASE_NAMES[format_id]}{STRING_SUFFIXES[key]}"
            value = descriptor[key]
            lines_cs.extend(
                [
                    "",
                    f"    /// <summary>{descriptor['title']} {key}.</summary>",
                    f'    public const string {csharp_name} = "{value}";',
                ]
            )
            rust_name = to_screaming_snake(csharp_name)
            lines_rs.append(f'pub const {rust_name}: &str = "{value}";')
            lines_py.append(f'{to_screaming_snake(csharp_name)} = "{value}"')

    lines_cs.extend(["}", ""])
    lines_rs.append("")
    lines_py.append("")
    return "\n".join(lines_cs), "\n".join(lines_rs), "\n".join(lines_py)


def build_artifacts(manifest: dict[str, Any]) -> dict[Path, str]:
    """Build all generated artifacts in memory."""
    artifacts: dict[Path, str] = {}
    formats = manifest["formats"]

    for format_id, descriptor in formats.items():
        version = format_version(descriptor)
        artifacts[
            GENERATED_DIR / "schema" / f"{format_id}-v{version}.schema.json"
        ] = json_text(schema_for_format(descriptor, manifest["manifestVersion"]))

    for signature_id, descriptor in manifest["signatures"].items():
        artifacts[GENERATED_DIR / "signatures" / f"{signature_id}-v1.json"] = json_text(
            {
                "manifestVersion": manifest["manifestVersion"],
                "id": descriptor["id"],
                "domainPrefix": descriptor["domainPrefix"],
                "domainSeparator": "0x00",
                "hash": "sha256",
                "projectionType": descriptor["projectionType"],
                "status": descriptor.get("status", "skeleton"),
                "notes": descriptor.get("notes", ""),
                "fields": descriptor["fields"],
            }
        )

    csharp, rust, python = constants_for(manifest)
    artifacts[GENERATED_DIR / "constants" / "csharp" / "ContractVersions.g.cs"] = csharp
    artifacts[GENERATED_DIR / "constants" / "rust" / "contract_versions.rs"] = rust
    artifacts[GENERATED_DIR / "constants" / "python" / "contract_versions.py"] = python

    artifacts[GENERATED_DIR / "fixtures" / "descriptors.json"] = json_text(
        {
            "manifestVersion": manifest["manifestVersion"],
            "signatureVectors": manifest["fixtures"]["signatureVectors"],
            "fixtures": manifest["fixtures"]["items"],
        }
    )

    readme = [
        "# Generated contract artifacts",
        "",
        "This directory is generated by `tools/contracts/generate.py` from `contracts/manifest.json`.",
        "Do not edit these files by hand. Run the generator instead.",
        "",
        "| Path | Contents |",
        "| --- | --- |",
    ]
    for path in sorted(artifacts):
        readme.append(f"| `{path.relative_to(GENERATED_DIR).as_posix()}` | Generated contract artifact |")
    artifacts[GENERATED_DIR / "README.md"] = "\n".join(readme) + "\n"

    vectors = manifest["fixtures"]["signatureVectors"]
    artifacts[SIGNATURE_VECTORS_PATH] = json_text(
        {
            "format": vectors["format"],
            "formatVersion": vectors["formatVersion"],
            "status": vectors.get("status", "scaffold"),
            "notes": vectors.get("notes", ""),
            "vectors": [],
        }
    )
    return artifacts


def write_artifacts(artifacts: dict[Path, str]) -> None:
    """Write generated artifacts atomically."""
    for path, content in artifacts.items():
        path.parent.mkdir(parents=True, exist_ok=True)
        temporary = path.with_name(f"{path.name}.tmp")
        temporary.write_text(content, encoding="utf-8")
        os.replace(temporary, path)


def check_artifacts(artifacts: dict[Path, str]) -> int:
    """Return non-zero when generated artifacts are missing or stale."""
    problems: list[str] = []
    expected = set(artifacts)

    for path, content in artifacts.items():
        if not path.exists():
            problems.append(f"missing {path.relative_to(ROOT)}")
            continue
        if path.read_text(encoding="utf-8") != content:
            problems.append(f"stale {path.relative_to(ROOT)}")

    if GENERATED_DIR.exists():
        existing = {path for path in GENERATED_DIR.rglob("*") if path.is_file()}
        for path in sorted(existing - expected):
            problems.append(f"unexpected {path.relative_to(ROOT)}")

    if problems:
        print("Contract artifacts are not current:", file=sys.stderr)
        for problem in problems:
            print(f"  {problem}", file=sys.stderr)
        print("Run: python tools/contracts/generate.py", file=sys.stderr)
        return 1

    print(f"Contract artifacts are current ({len(artifacts)} files)")
    return 0


def main(argv: list[str]) -> int:
    """Validate the manifest and write or check generated artifacts."""
    if any(argument not in {"--check"} for argument in argv[1:]):
        print("usage: python tools/contracts/generate.py [--check]", file=sys.stderr)
        return 2

    try:
        manifest = load_manifest()
        validate_manifest(manifest)
        artifacts = build_artifacts(manifest)
    except ValueError as error:
        print(f"contract manifest error: {error}", file=sys.stderr)
        return 1

    if "--check" in argv[1:]:
        return check_artifacts(artifacts)

    write_artifacts(artifacts)
    print(f"Wrote {len(artifacts)} contract artifacts from {MANIFEST_PATH.relative_to(ROOT)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv))
