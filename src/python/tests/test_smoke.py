# SPDX-License-Identifier: MIT

"""Smoke tests for the Step 00 ``mocassin-analysis`` placeholder package."""

from mocassin import __all__, __version__


def test_package_exposes_version() -> None:
    """The installed package exposes its distribution version."""
    assert __version__ == "0.1.0"


def test_version_has_three_numeric_parts() -> None:
    """The package version is a simple three-part numeric version."""
    parts = __version__.split(".")
    assert len(parts) == 3
    assert all(part.isdigit() for part in parts)


def test_public_surface_is_explicit() -> None:
    """The placeholder package does not accidentally export implementation details."""
    assert __all__ == ["__version__"]
