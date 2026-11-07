# Space-Group Catalog Data

This directory contains the committed space-group catalog consumed by `Mocassin.Symmetry`. Normal builds
embed these files as resources; they never run Python, spglib, or network access.

## Provenance

| Field | Value |
| --- | --- |
| Source | [spglib](https://github.com/spglib/spglib) |
| Release | `2.8.0` |
| Commit | `3ebd8ac3318793932baeee90b44f453a7c35b5b4` |
| Licence | BSD-3-Clause |
| Generator | `tools/symmetry/generate_space_groups.py` |
| Pinned dependency | `tools/symmetry/requirements.txt` (`spglib==2.8.0`) |
| Generated version | `spglib-2.8.0/catalog-1` |

The generator asserts the installed `spglib` version and the pinned commit constant before producing data.
The original MOCASSIN SQLite database is **not** a source: it is GPL-3.0-or-later and is never copied. It may
be used locally only as a cross-check. Attribution and citation requirements are recorded in `NOTICE.md`.

## Selection rule (`274-setting-compatibility-v1`)

Exactly one compatibility category is applied per international group number:

| Category | Selection | Groups | Settings |
| --- | --- | --- | ---: |
| Standard | `choice = ""` | 186 groups with one standard setting | 186 |
| Origin choice | `choice = "1"` and `"2"` | 24 groups with two origins | 48 |
| Monoclinic unique axis B/C | `choice = "b"` or `"b1"`, and `"c"` or `"c1"` | 13 monoclinic groups | 26 |
| Rhombohedral axes | `choice = "H"` and `"R"` | 7 rhombohedral groups | 14 |
| **Total** | | | **274** |

Primitive monoclinic groups 3, 4, 6, 10, and 11 use `b`/`c`; cell-choice groups 5, 7–9, and 12–15 use
`b1`/`c1`. Unique axis A, monoclinic cell choices 2/3, orthorhombic axis permutations, and other non-standard
settings are excluded in v1. `AxisSetting.UniqueAxisA` is reserved and rejected by the v1 project validator.

## File layout

- `space-groups.json` — UTF-8 without BOM, LF-only, deterministic compact JSON, no timestamps. The catalog is
  machine-consumed and hashed, not hand-reviewed, so it uses a dense positional encoding:
  - root keys are `schemaVersion`, `dataVersion`, `source`, `sourceVersion`, `selection`, and `settings`;
  - each setting is an array
    `[number, hermannMauguin, hallSymbol, crystalSystem, axis, origin, operations]`;
  - each operation is a 12-integer array
    `[a11, a12, a13, a21, a22, a23, a31, a32, a33, tA, tB, tC]`.
- Settings are sorted by `(number, axis, origin)`. Operations are sorted by linear matrix row-major
  `(a11..a33)`, then by the three normalized translation numerators.
- Translation numerators are exact over the fixed denominator 24 and are normalized to `[0, 24)`.
- `space-groups.sha256` — lowercase SHA-256 of the **canonical settings payload**, a compact UTF-8 JSON array
  containing only the `settings` value. It does not cover the root metadata or any timestamp.

## Regeneration

```sh
python -m pip install -r tools/symmetry/requirements.txt
python tools/symmetry/generate_space_groups.py
python tools/symmetry/generate_space_groups.py --check
```

CI runs the `--check` command in the Contracts and policy job. It fails if the committed settings payload or
checksum differs from the pinned spglib selection.

## Validation

The generator asserts:

- exactly 230 international group numbers and 274 settings;
- the 186 / 48 / 26 / 14 category split;
- two monoclinic settings for every monoclinic group;
- no duplicate `(number, axis, origin)` key and no duplicate Hall symbol;
- every operation has determinant `±1`;
- every translation is normalized with denominator 24;
- identity is present in every setting;
- group 47 (`Pmmm`) has the full 8-operation set including identity and inversion, with no degenerate row.

The .NET loader recomputes the checksum at startup and repeats the cheap fail-fast checks (schema version,
230/274 counts, identity, determinant, normalized translations). Exhaustive closure, inverse, and
general-position-multiplicity checks run in the test suite.

## Known deviations from the original implementation

- Group 47 is the corrected spglib `Pmmm` operation set, not the original 4-operation invalid set.
- Translations are normalized modulo 1.
- Origin choice and axis setting are structured fields, not free-text specifiers.
- Position order is the canonical exact-grid order; it does not depend on database order or tolerance
  deduplication.
- The legacy triclinic/rhombohedral cell-vector `c`-factor bug does not exist here.
