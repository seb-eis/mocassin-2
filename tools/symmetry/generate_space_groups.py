#!/usr/bin/env python3
# SPDX-License-Identifier: MIT
"""Generate the committed 274-setting space-group catalog from spglib.

This is a development-only tool. Normal .NET builds consume the committed JSON;
they never run Python, spglib, or any network access.

The selection rule is the v1 compatibility subset described in Step 02:

* 186 single standard settings (choice ""),
* 48 origin-choice settings (choices "1" and "2", 24 groups),
* 26 monoclinic unique-axis B/C settings (choices "b"/"b1" and "c"/"c1"),
* 14 rhombohedral settings (choices "H" and "R", 7 groups).

The catalog uses a compact positional JSON encoding because it is machine-consumed and hashed, not
hand-reviewed:

    setting   = [number, hermannMauguin, hallSymbol, crystalSystem, axis, origin, operations]
    operation = [a11, a12, a13, a21, a22, a23, a31, a32, a33, tA, tB, tC]

Translation numerators are exact over the fixed denominator 24 and are normalized to [0, 24).

Run with ``--check`` in CI to fail if the committed settings or checksum drift.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import sys
from pathlib import Path

SPGLIB_VERSION = "2.8.0"
SPGLIB_COMMIT = "3ebd8ac3318793932baeee90b44f453a7c35b5b4"
CATALOG_SCHEMA_VERSION = 1
CATALOG_VERSION = f"spglib-{SPGLIB_VERSION}/catalog-1"
SELECTION_RULE = "274-setting-compatibility-v1"

MONOCLINIC_GROUPS = frozenset(range(3, 16))
RHOMHOHEDRAL_GROUPS = frozenset({146, 148, 155, 160, 161, 166, 167})

ROOT = Path(__file__).resolve().parents[2]
DATA_DIRECTORY = ROOT / "src" / "dotnet" / "Mocassin.Symmetry" / "Data"
CATALOG_PATH = DATA_DIRECTORY / "space-groups.json"
CHECKSUM_PATH = DATA_DIRECTORY / "space-groups.sha256"

IDENTITY_LINEAR = [1, 0, 0, 0, 1, 0, 0, 0, 1]


def crystal_system(number: int) -> str:
    """Return the crystal-system string for an international group number."""
    if number <= 2:
        return "triclinic"
    if number <= 15:
        return "monoclinic"
    if number <= 74:
        return "orthorhombic"
    if number <= 142:
        return "tetragonal"
    if number <= 167:
        return "trigonal"
    if number <= 194:
        return "hexagonal"
    return "cubic"


def operation_record(rotation, translation) -> list[int]:
    """Convert one spglib operation to a dense 12-integer record."""
    linear = [int(value) for row in rotation for value in row]
    numerators = []
    for value in translation:
        scaled = round(float(value) * 24.0)
        if abs((float(value) * 24.0) - scaled) > 1e-9:
            raise AssertionError(f"translation {value!r} is not a multiple of 1/24")
        numerators.append(int(scaled) % 24)
    return linear + numerators


def determinant(linear: list[int]) -> int:
    """Return the determinant of a flat row-major 3x3 matrix."""
    return (
        linear[0] * (linear[4] * linear[8] - linear[5] * linear[7])
        - linear[1] * (linear[3] * linear[8] - linear[5] * linear[6])
        + linear[2] * (linear[3] * linear[7] - linear[4] * linear[6])
    )


def hall_choices(spglib_module) -> dict[int, list[dict]]:
    """Group every Hall setting by international group number."""
    grouped: dict[int, list[dict]] = {}
    for hall_number in range(1, 531):
        setting_type = spglib_module.get_spacegroup_type(hall_number)
        number = int(setting_type.number)
        grouped.setdefault(number, []).append(
            {
                "hallNumber": hall_number,
                "choice": setting_type.choice,
                "hermannMauguin": setting_type.international_short,
                "hallSymbol": setting_type.hall_symbol,
            }
        )
    return grouped


def select_settings(spglib_module) -> list[list]:
    """Build the selected 274 settings with sorted dense operations."""
    grouped = hall_choices(spglib_module)
    selected: list[list] = []

    def add(number: int, choice: str, axis: str, origin: str) -> None:
        matches = [entry for entry in grouped[number] if entry["choice"] == choice]
        if len(matches) != 1:
            raise AssertionError(
                f"group {number} choice {choice!r} matched {len(matches)} settings"
            )
        entry = matches[0]
        symmetry = spglib_module.get_symmetry_from_database(entry["hallNumber"])
        operations = [
            operation_record(rotation, translation)
            for rotation, translation in zip(
                symmetry["rotations"], symmetry["translations"], strict=True
            )
        ]
        operations.sort(key=tuple)
        if len({tuple(operation) for operation in operations}) != len(operations):
            raise AssertionError(f"group {number} choice {choice!r} has duplicate operations")
        for operation in operations:
            if abs(determinant(operation[:9])) != 1:
                raise AssertionError(
                    f"group {number} choice {choice!r} has a non-unimodular operation"
                )
        selected.append(
            [
                number,
                entry["hermannMauguin"],
                entry["hallSymbol"],
                crystal_system(number),
                axis,
                origin,
                operations,
            ]
        )

    for number in range(1, 231):
        choices = {entry["choice"] for entry in grouped[number]}
        if {"1", "2"} <= choices:
            add(number, "1", "none", "originChoice1")
            add(number, "2", "none", "originChoice2")
        elif number in MONOCLINIC_GROUPS:
            add(number, "b" if "b" in choices else "b1", "uniqueAxisB", "none")
            add(number, "c" if "c" in choices else "c1", "uniqueAxisC", "none")
        elif number in RHOMHOHEDRAL_GROUPS:
            add(number, "H", "hexagonal", "none")
            add(number, "R", "rhombohedral", "none")
        else:
            add(number, "", "none", "none")

    selected.sort(key=lambda setting: (setting[0], setting[4], setting[5]))
    return selected


def build_catalog(spglib_module) -> dict:
    """Build the complete catalog document."""
    settings = select_settings(spglib_module)
    categories = {
        "standard": sum(
            1
            for setting in settings
            if setting[4] == "none"
            and setting[5] == "none"
            and setting[0] not in MONOCLINIC_GROUPS
        ),
        "originChoice": sum(1 for setting in settings if setting[5] != "none"),
        "monoclinicUniqueAxisBC": sum(
            1 for setting in settings if setting[0] in MONOCLINIC_GROUPS
        ),
        "rhombohedralAxes": sum(
            1 for setting in settings if setting[0] in RHOMHOHEDRAL_GROUPS
        ),
    }
    return {
        "schemaVersion": CATALOG_SCHEMA_VERSION,
        "dataVersion": CATALOG_VERSION,
        "source": "spglib",
        "sourceVersion": SPGLIB_VERSION,
        "selection": {
            "rule": SELECTION_RULE,
            "standard": categories["standard"],
            "originChoice": categories["originChoice"],
            "monoclinicUniqueAxisBC": categories["monoclinicUniqueAxisBC"],
            "rhombohedralAxes": categories["rhombohedralAxes"],
            "total": len(settings),
        },
        "settings": settings,
    }


def validate(catalog: dict, spglib_module) -> None:
    """Assert the catalog invariants required by the generator contract."""
    settings = catalog["settings"]
    numbers = {setting[0] for setting in settings}
    if numbers != set(range(1, 231)):
        raise AssertionError(f"expected all 230 group numbers, found {len(numbers)}")
    if len(settings) != 274:
        raise AssertionError(f"expected 274 settings, found {len(settings)}")
    keys = {(setting[0], setting[4], setting[5]) for setting in settings}
    if len(keys) != 274:
        raise AssertionError("duplicate (number, axis, origin) setting keys")
    if len({setting[2] for setting in settings}) != 274:
        raise AssertionError("duplicate Hall symbols")

    selection = catalog["selection"]
    expected_counts = {
        "standard": 186,
        "originChoice": 48,
        "monoclinicUniqueAxisBC": 26,
        "rhombohedralAxes": 14,
        "total": 274,
    }
    for key, expected in expected_counts.items():
        if selection[key] != expected:
            raise AssertionError(
                f"selection category {key} is {selection[key]}, expected {expected}"
            )

    monoclinic = [setting for setting in settings if setting[0] in MONOCLINIC_GROUPS]
    if len(monoclinic) != 26:
        raise AssertionError(f"expected 26 monoclinic settings, found {len(monoclinic)}")
    for number in MONOCLINIC_GROUPS:
        count = sum(1 for setting in monoclinic if setting[0] == number)
        if count != 2:
            raise AssertionError(f"group {number} has {count} monoclinic settings")

    rhombohedral = [
        setting for setting in settings if setting[0] in RHOMHOHEDRAL_GROUPS
    ]
    if len(rhombohedral) != 14:
        raise AssertionError(f"expected 14 rhombohedral settings, found {len(rhombohedral)}")

    for setting in settings:
        operations = setting[6]
        identity = any(
            operation[:9] == IDENTITY_LINEAR and operation[9:] == [0, 0, 0]
            for operation in operations
        )
        if not identity:
            raise AssertionError(
                f"group {setting[0]} {setting[4]} {setting[5]} has no identity"
            )
        if any(abs(determinant(operation[:9])) != 1 for operation in operations):
            raise AssertionError("non-unimodular operation in the catalog")
        if any(not 0 <= numerator < 24 for operation in operations for numerator in operation[9:]):
            raise AssertionError("unnormalized translation in the catalog")

    pmmm = [
        setting
        for setting in settings
        if setting[0] == 47 and setting[4] == "none" and setting[5] == "none"
    ]
    if len(pmmm) != 1:
        raise AssertionError("group 47 Pmmm setting is missing")
    pmmm_operations = pmmm[0][6]
    if len(pmmm_operations) != 8:
        raise AssertionError(f"group 47 has {len(pmmm_operations)} operations, expected 8")
    inversion = [-1, 0, 0, 0, -1, 0, 0, 0, -1]
    if not any(operation[:9] == inversion for operation in pmmm_operations):
        raise AssertionError("group 47 is missing inversion")


def canonical_settings_payload(settings: list[list]) -> bytes:
    """Return the canonical compact UTF-8 settings payload used for the checksum."""
    return json.dumps(
        settings, ensure_ascii=False, separators=(",", ":"), sort_keys=False
    ).encode("utf-8")


def checksum_of(settings: list[list]) -> str:
    """Return the SHA-256 checksum of the canonical settings payload."""
    return hashlib.sha256(canonical_settings_payload(settings)).hexdigest()


def write_outputs(catalog: dict) -> None:
    """Write the committed catalog and checksum."""
    DATA_DIRECTORY.mkdir(parents=True, exist_ok=True)
    CATALOG_PATH.write_text(
        json.dumps(catalog, ensure_ascii=False, separators=(",", ":")) + "\n",
        encoding="utf-8",
        newline="\n",
    )
    CHECKSUM_PATH.write_text(checksum_of(catalog["settings"]) + "\n", encoding="utf-8")


def check_outputs(catalog: dict) -> int:
    """Compare generated data with the committed files."""
    if not CATALOG_PATH.exists() or not CHECKSUM_PATH.exists():
        print("error: committed catalog files are missing", file=sys.stderr)
        return 1

    committed = json.loads(CATALOG_PATH.read_text(encoding="utf-8"))
    committed_checksum = CHECKSUM_PATH.read_text(encoding="utf-8").strip()
    generated_payload = canonical_settings_payload(catalog["settings"])
    committed_payload = canonical_settings_payload(committed["settings"])

    failures = []
    if generated_payload != committed_payload:
        failures.append("committed settings differ from the generated selection")
    expected_checksum = checksum_of(catalog["settings"])
    if committed_checksum != expected_checksum:
        failures.append(
            f"committed checksum is {committed_checksum}, expected {expected_checksum}"
        )
    if committed.get("dataVersion") != CATALOG_VERSION:
        failures.append("committed dataVersion differs from the pinned version")
    if committed.get("schemaVersion") != CATALOG_SCHEMA_VERSION:
        failures.append("committed schemaVersion differs from the pinned version")

    if failures:
        for failure in failures:
            print(f"error: {failure}", file=sys.stderr)
        return 1

    print(
        f"space-group catalog is current: {len(catalog['settings'])} settings, "
        f"sha256 {expected_checksum}"
    )
    return 0


def main() -> int:
    """Run the generator or the drift check."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--check",
        action="store_true",
        help="verify that the committed catalog and checksum match the pinned spglib data",
    )
    arguments = parser.parse_args()

    try:
        import spglib
    except ImportError:
        print(
            "error: spglib is required; install tools/symmetry/requirements.txt",
            file=sys.stderr,
        )
        return 1

    if spglib.__version__ != SPGLIB_VERSION:
        print(
            f"error: spglib {spglib.__version__} is installed, but {SPGLIB_VERSION} is pinned",
            file=sys.stderr,
        )
        return 1
    if SPGLIB_COMMIT != "3ebd8ac3318793932baeee90b44f453a7c35b5b4":
        print("error: the pinned spglib commit changed without review", file=sys.stderr)
        return 1

    catalog = build_catalog(spglib)
    validate(catalog, spglib)

    if arguments.check:
        return check_outputs(catalog)

    write_outputs(catalog)
    print(
        f"wrote {CATALOG_PATH.relative_to(ROOT)} with "
        f"{len(catalog['settings'])} settings, sha256 {checksum_of(catalog['settings'])}"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
