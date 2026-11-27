# Author the ceria model in the GUI

This tutorial authors the particle and structure half of the ceria sample entirely in the desktop editors. The
transitions and the transition editor are the 24B half; add those in the transition-editor tutorial.

The reference project is `samples/ceria/ceria.mocprj`; the empty starting point is `samples/blank/blank.mocprj`.

## 1. Particles and sets

Open **Model → Particles**.

1. Add `Ce4+` with symbol `Ce`, charge `+4`, not a vacancy.
2. Add `O2-` with symbol `O`, charge `-2`, not a vacancy.
3. Add `Vacancy` with symbol `Vc`, charge `0`, vacancy enabled.
4. Add the sets in **Model → Particle sets**:
   - `Cerium site`: `Ce4+`.
   - `Oxygen site`: `O2-`, `Vacancy` (first member is the default occupation).
   - `Oxygen transition site`: `Vacancy`, `O2-`.

Use the set detail's **Project particles** list with drag or the **Add** button; the first member shows the
default-occupation badge. A referenced particle cannot be deleted until its set or transition dependency is
removed.

## 2. Space group and cell

Open **Model → Structure**.

1. Search `225`, select the cubic setting, and inspect the operation list and the candidate orbit preview.
   Apply the setting.
2. In **Cell parameters**, set `a = 5.41`; the cubic locks set `b = c = 5.41` and all angles to `90°`. Apply.
3. Confirm the live vectors and volume and the summary's reference-site count, volume, and density.

## 3. Sites and the P1 preview

Open **Model → Sites** and add:

| Name | Fractional | Stability | Particle set |
| --- | --- | --- | --- |
| `Ce` | `0, 0, 0` | stable | `Cerium site` |
| `O` | `0.25, 0.25, 0.25` | stable | `Oxygen site` |
| `O transition` | `0.5, 0.25, 0.25` | unstable | `Oxygen transition site` |

Select `O` and open the P1 preview. The orbit rows show the fractional and Cartesian positions, the canonical
`P` index, and the orbit-member index; the whole-structure list is in `P` order. The plane selector projects the
positions onto a–b, a–c, or b–c. No generating-operation label is shown.

If an edit would overlap another site, the form shows `STR007` and disables Apply; correct the position or the
particle set before applying.

## 4. Dummies and summary

- Open **Model → Dummy positions** to add visual-only markers. They use the **◇** glyph and are excluded from
  simulation compilation.
- The structure summary derives every value from the structure value and preview: reference sites, P1
  positions, stable/unstable positions (labelled positions), volume, reference-site density in Å⁻³, the orbit
  table, and the `STR009` large-P1 warning. It does not run the on-demand statistics service.

## 5. Save and validate

Save the project and run:

```bash
mocassin validate samples/ceria/ceria.mocprj
```

The particle, structure, and site diagnostics should be clean; `SIM003` and the transition-related messages are
expected until the transition half is authored in 24B.
