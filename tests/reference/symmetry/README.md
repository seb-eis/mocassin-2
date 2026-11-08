# Symmetry Reference Fixtures

## `general-multiplicities.json`

Frozen general-position multiplicity per catalog setting: 274 entries keyed by `(number, axis, origin)`.

Provenance:

- generated from the pinned `spglib-2.8.0/catalog-1` selection;
- representative values are cross-checked against the International Tables for Crystallography, Volume A
  (P1, P-1, P2, Pmmm, R3, R-3m, Fm-3m);
- the .NET test `EverySettingMatchesTheFrozenGeneralMultiplicityTable` compares every setting, and
  `GenericOrbitHasFullMultiplicityForEverySetting` independently confirms that a generic point produces the
  full operation count for every setting.

This file is a golden regression fixture, not a runtime dependency.
