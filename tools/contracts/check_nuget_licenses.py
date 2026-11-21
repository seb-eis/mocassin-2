#!/usr/bin/env python3
# SPDX-License-Identifier: MIT

"""Check the licences of restored NuGet packages against the project policy."""

from __future__ import annotations

import json
import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path
from typing import Any

ROOT = Path(__file__).resolve().parents[2]

ALLOWED_EXPRESSIONS = {
    "MIT",
    "Apache-2.0",
    "BSD-2-Clause",
    "BSD-3-Clause",
    "ISC",
    "MS-PL",
    "Unlicense",
    "CC0-1.0",
    "0BSD",
}

# Source is MIT; the NuGet binary release carries the Open Source Maintenance
# Fee EULA. Accepted for this free, non-revenue, test-only use. Exact versions
# force a review when the dependency changes.
OSMF_EULA_EXCEPTIONS = {
    "JsonSchema.Net": "9.6.0",
    "JsonPointer.Net": "7.0.4",
    "Json.More.Net": "3.0.1",
}

KNOWN_PACKAGE_LICENSES = {
    ("xunit.abstractions", "2.0.3"): "Apache-2.0",
}

# Some MIT packages ship the licence as a file rather than an SPDX expression.
# Exact versions force a review when the dependency changes.
KNOWN_FILE_LICENSES = {
    ("CommandLineParser", "2.9.1"): "MIT",
    ("Microsoft.DotNet.PlatformAbstractions", "3.1.6"): "MIT",
    # Avalonia.Desktop pulls the ANGLE native runtime on Windows. Its LICENSE file
    # is the BSD-3-Clause ANGLE licence (Google/TransGaming/3DLabs). Pinned to the
    # exact version so a dependency change forces a new review.
    ("Avalonia.Angle.Windows.Natives", "2.1.27548.20260419"): "BSD-3-Clause",
}

KNOWN_LICENSE_URLS = {
    "https://licenses.nuget.org/MIT": "MIT",
    "https://licenses.nuget.org/Apache-2.0": "Apache-2.0",
    "https://licenses.nuget.org/BSD-3-Clause": "BSD-3-Clause",
    "https://raw.githubusercontent.com/xunit/xunit/master/license.txt": "Apache-2.0",
}


def fail(message: str) -> None:
    """Raise a policy error."""
    raise ValueError(message)


def load_assets() -> list[Path]:
    """Return all restored project.assets.json files."""
    paths = sorted(ROOT.glob("src/dotnet/**/obj/project.assets.json"))
    if not paths:
        fail("no project.assets.json files found; run dotnet restore first")
    return paths


def package_folders(assets: list[Path]) -> list[Path]:
    """Return the distinct NuGet package folders."""
    folders: list[Path] = []
    for path in assets:
        data = json.loads(path.read_text(encoding="utf-8"))
        for folder in data.get("packageFolders", {}):
            candidate = Path(folder)
            if candidate not in folders:
                folders.append(candidate)
    return folders


def package_libraries(assets: list[Path]) -> dict[str, dict[str, Any]]:
    """Return package libraries keyed by id/version."""
    libraries: dict[str, dict[str, Any]] = {}
    for path in assets:
        data = json.loads(path.read_text(encoding="utf-8"))
        for key, library in data.get("libraries", {}).items():
            if library.get("type") == "package":
                libraries[key] = library
    return libraries


def find_nuspec(folders: list[Path], library: dict[str, Any]) -> Path | None:
    """Locate the nuspec for one library."""
    relative = library.get("path")
    if not isinstance(relative, str):
        return None
    for folder in folders:
        directory = folder / relative
        if directory.is_dir():
            nuspecs = sorted(directory.glob("*.nuspec"))
            if nuspecs:
                return nuspecs[0]
    return None


def read_text(element: ET.Element | None) -> str | None:
    """Read an optional element's text."""
    if element is None:
        return None
    text = (element.text or "").strip()
    return text or None


def expression_is_allowed(expression: str) -> bool:
    """Check a simple SPDX expression against the allow-list."""
    tokens = [
        token.strip()
        for token in re.split(r"\s+(?:OR|AND)\s+|[()]", expression)
        if token.strip()
    ]
    return bool(tokens) and all(token in ALLOWED_EXPRESSIONS for token in tokens)


def resolve_license(package_id: str, version: str, nuspec: Path) -> tuple[str, str]:
    """Return (licence label, source) for a package."""
    root = ET.parse(nuspec).getroot()
    license_element = root.find(".//{*}license")
    license_url_element = root.find(".//{*}licenseUrl")
    license_url = read_text(license_url_element)

    if license_element is not None:
        license_type = (license_element.get("type") or "").strip()
        license_text = read_text(license_element)
        if license_type == "expression" and license_text:
            return license_text, "nuspec expression"
        if license_type == "file" and license_text:
            if license_text == "OSMFEULA.txt":
                expected = OSMF_EULA_EXCEPTIONS.get(package_id)
                if expected is None:
                    fail(f"{package_id} {version}: OSMFEULA is not an approved exception")
                if expected != version:
                    fail(
                        f"{package_id} {version}: OSMFEULA exception is pinned to {expected}; review the new version"
                    )
                return "OSMFEULA (accepted exception)", "nuspec file"

            known_file = KNOWN_FILE_LICENSES.get((package_id, version))
            if known_file:
                return known_file, f"known file licence '{license_text}'"
            fail(f"{package_id} {version}: file licence '{license_text}' requires explicit review")

    if license_url and license_url in KNOWN_LICENSE_URLS:
        return KNOWN_LICENSE_URLS[license_url], "known licenseUrl"
    known = KNOWN_PACKAGE_LICENSES.get((package_id, version))
    if known:
        return known, "known package mapping"
    fail(f"{package_id} {version}: no recognised licence metadata")


def main() -> int:
    """Validate all restored NuGet package licences."""
    try:
        assets = load_assets()
        folders = package_folders(assets)
        libraries = package_libraries(assets)
        problems: list[str] = []
        checked = 0

        for key, library in sorted(libraries.items()):
            package_id, version = key.split("/", 1)
            nuspec = find_nuspec(folders, library)
            if nuspec is None:
                problems.append(f"{key}: nuspec not found")
                continue

            try:
                license_label, source = resolve_license(package_id, version, nuspec)
            except ValueError as error:
                problems.append(str(error))
                continue

            checked += 1
            print(f"{key}: {license_label} ({source})")
            if license_label != "OSMFEULA (accepted exception)" and not expression_is_allowed(license_label):
                problems.append(f"{key}: licence '{license_label}' is not on the allow-list")

        if problems:
            print("\nNuGet licence policy failures:", file=sys.stderr)
            for problem in problems:
                print(f"  {problem}", file=sys.stderr)
            return 1

        print(f"\nNuGet licence policy OK ({checked} packages).")
        return 0
    except ValueError as error:
        print(f"NuGet licence policy error: {error}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
