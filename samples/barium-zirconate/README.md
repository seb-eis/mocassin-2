# Barium zirconate sample

`barium-zirconate.mocprj` is a partially hydrated, yttrium-doped BaZrO₃ reference structure. Step 03 defines
the host particles Ba²⁺, Zr⁴⁺, and O²⁻, the Y³⁺ substitution on the zirconium site, one shared uncharged
vacancy particle, and the proton interstitial H⁺. The perovskite structure uses space group `Pm-3m` (221) with
`a = 4.19 Å`.

The structural model contains:

- stable Ba, Zr (with Y substitution), O, and proton interstitial sites;
- a stable proton reference site at `(0.734, 0.5, 0)`. The offset `0.234` is approximately
  `0.98 Å / 4.19 Å`, placing the proton on the O–O bisector toward a neighbouring oxygen. `Pm-3m` expands it
  to the four proton positions around each of the three oxygen sites, 12 positions in total. This is a stable
  defect position, not a KMC transition-only position; proton-hopping transitions between the symmetry-
  equivalent positions are added by Step 04;
- particle sets `[Ba²⁺]`, `[Zr⁴⁺, Y³⁺]`, `[O²⁻, V]`, and `[V, H⁺]`. The oxygen and proton sets share the single
  uncharged vacancy particle `V`; additional vacancy particles are only needed for distinct charged vacancy
  states, for example polaron hopping along vacancies. The proton site defaults to `V`, so the dry host is
  empty and hydration fills it with `H⁺`.

The sample is structural only. Doping levels, hydration levels, transitions, parameterization, and job
templates are added by later steps.

The file is produced by the real project writer and is mirrored as
`tests/contracts/project-barium-zirconate.mocprj`.
