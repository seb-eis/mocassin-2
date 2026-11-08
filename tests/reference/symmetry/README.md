# Symmetry Reference Fixtures

## `ase-general-multiplicities.json`

General-position multiplicity for each international group number `1..230`, generated independently with
ASE 3.29.0 (`ase.spacegroup.Spacegroup.equivalent_sites`). ASE is a development-only oracle; it is not a
runtime dependency and is not installed by the .NET build or CI.

The .NET test `EverySettingMatchesTheFrozenGeneralMultiplicityTable` maps each catalog setting to its group
number and applies the one setting-dependent rule: rhombohedral-axis settings use one third of the ASE
hexagonal-axis general multiplicity. All other settings (standard, origin choices, monoclinic unique-axis
B/C) share the group multiplicity.

`GenericOrbitHasFullMultiplicityForEverySetting` independently confirms that a generic point produces the
full operation count for every catalog setting.
