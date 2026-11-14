# NaSICON reference sample: Na₃Zr₂Si₂PO₁₂

> This is a design scratch, not a decision. See [`README.md`](README.md).

| Field | Value |
| --- | --- |
| Status | Under discussion |
| Owner | Sample-data / validation research |
| Created | 2026-11-14 |
| Last updated | 2026-11-14 |
| Target step | Step 29 (validation, parity, and performance); `samples/`, `tests/reference/` |
| Related | Step 29, ADR 0005 (statistical parity), MOCASSIN paper (Eisele & Grieshammer 2021) |
| Summary | Research note for a third real-material KMC sample: Na₃Zr₂Si₂PO₁₂ (NASICON, `R-3c`), with published pair-interaction energies, migration barriers, a CIF structure, and a published MOCASSIN KMC precedent. |

## Context

The repository already plans ceria (oxygen vacancy + polaron) and Y-doped barium zirconate
(proton/vacancy transport) as real-material reference systems. Step 29 also calls for a
vehicle/interstitialcy fixture. A third real material is needed that:

- is not ceria or doped barium zirconate;
- has a published crystal structure suitable for direct model authoring;
- has published **pair interaction energies**, not only migration barriers;
- has published **migration barriers** for the relevant paths;
- has a documented transport mechanism that MOCASSIN can express;
- ideally has a published KMC study that used MOCASSIN, so the parameterization is already in
  the right modelling language and can serve as a cross-check.

Na₃Zr₂Si₂PO₁₂ (NASICON) satisfies all of these. The key source is the PhD thesis of Judith
Schütt (RWTH Aachen, 2024), whose KMC simulations were performed with MOCASSIN, plus the
associated PCCP/JMCA papers.

This draft records the research evidence and a proposed model blueprint. **It does not create
the project and does not authorize implementation.** The project should be built later, after
the remaining sample/testcase drafts are collected.

## Requirements and constraints

- Functional requirements: stable positions, unstable positions, particles, state changes,
  KMC transitions, pair interactions, site energies, initial configuration, and a runnable
  MOCASSIN job.
- Scientific correctness: published values must be traceable to a table or figure; every
  approximation must be recorded.
- Licensing: numeric data and short factual summaries with attribution are acceptable; do not
  copy thesis/paper text or database files into the repository.
- Offline/online: the sample must build and run offline once its JSON/`.mocprj` is authored.
- Cross-platform: no dependency on the Windows-only original GUI for the checked-in sample;
  the model should be authorable from project JSON or a small script.
- Reproducibility: record source commit/DOI, CIF identifiers, parameter values, phase choice,
  supercell, temperature, field, MCSP, seeds, and solver version with the fixture.
- Scope: this is a **sample-data** activity, not a solver-physics change. Any original-solver
  ambiguity follows the normal "no silent scientific corrections" rule.

## Recommendation

Use **Na₃Zr₂Si₂PO₁₂**, composition x = 2 of Na₁₊ₓZr₂SiₓP₃₋ₓO₁₂, in the rhombohedral `R-3c`
phase (high-temperature phase; the published MOCASSIN KMC used this phase).

Why this material:

- It is an alkali-ion conductor in a 3D polyanion framework, structurally and chemically
  distinct from fluorite ceria and perovskite barium zirconate.
- The published model is minimal: two pair-interaction terms plus one migration barrier per
  path, which is ideal for a readable sample.
- The published KMC was run with MOCASSIN, so the transition/energy concepts map directly.
- It provides a real-material **5-site interstitialcy** example, filling the Step 29
  interstitialcy slot with literature-backed data rather than a synthetic toy.

## Required-input checklist

| MOCASSIN input | NaSICON data | Status |
| --- | --- | --- |
| Space group and lattice | `R-3c` (167), a = b = 9.0535 Å, c = 23.0677 Å (CIF, 320 °C) | Available |
| Reference sites and occupancies | Na1 6b, Na2 18e, Na3 36f, Zr 12c, P/Si 18e, O1/O2 36f | Available (Na3 approximate) |
| Particles and charges | Na⁺, V_Na, Zr⁴⁺, P⁵⁺, Si⁴⁺, O²⁻ | Available |
| Stable pair interactions | Na–Na 4.92 Å = +0.08 eV; Na–Si 3.11 Å = −0.04 eV; Na–Si 3.71 Å = −0.05 eV | Available |
| Unstable-site energy / barrier | 80° = 0.30 eV; 90° = 0.25 eV; 180° = 0.23 eV at x = 2 | Available |
| Mechanism | Interstitialcy-like 5-site push chain; interstitial-like 3-site alternative | Documented |
| Initial configuration | Na1 full, Na2 2/3, P:Si = 1:2 | Derived from CIF/composition |
| Validation observables | E_A ≈ 0.30 eV; $\sigma(T)$ from published KMC | Available |
| Transition-state coordinates | Na3 36f split site; exact DFT path positions not tabulated in the thesis | Approximate |
| Attempt frequency | Fixed; value not stated in the NaSICON thesis excerpt | Open |

## Crystal structure

### Source

- COD entry **1529809**: J. J. Didisheim, E. Prince, B. J. Wuensch, "Neutron Rietveld
  analysis of structural changes in NASICON solid solutions Na₁₊ₓZr₂SiₓP₃₋ₓO₁₂ at elevated
  temperatures: x = 1.6 and 2.0 at 320 °C", *Solid State Ionics* **1986**, 18–19, 944–958.
- CIF: <https://www.crystallography.net/cod/1529809.html>
- Composition in the CIF: Na₃Zr₂Si₂PO₁₂ (x = 2), rhombohedral `R-3c:H`, Z = 6.

### Lattice and sites

Hexagonal setting, a = b = 9.0535 Å, c = 23.0677 Å, α = β = 90°, γ = 120°.

| Site | Wyckoff | x | y | z | Occupancy | Role |
| --- | --- | ---: | ---: | ---: | ---: | --- |
| Na1 | 6b | 0 | 0 | 0 | 1.01 | stable, regular Na site |
| Na2 | 18e | 0.6292 | 0 | 0.25 | 0.66 | stable, interstitialcy site |
| Na3 | 36f | see note | see note | see note | — | unstable transition site |
| Zr | 12c | 0 | 0 | 0.1469 | 1 | framework |
| P/Si | 18e | 0.2883 | 0 | 0.25 | P 0.33 / Si 0.67 | framework / substituent |
| O1 | 36f | 0.1762 | −0.036 | 0.1944 | 1 | framework |
| O2 | 36f | 0.1939 | 0.1707 | 0.0929 | 1 | framework |

The DFT structure used in the KMC study is close: a ≈ 9.10 Å, c ≈ 22.60 Å (thesis Tables
A.1.3/A.1.4, x = 2 averages). Either the CIF or the DFT values may be used, but the choice
must be recorded. The CIF is convenient because it is a citable, complete structure.

### Cell content and charge balance

Per conventional cell (Z = 6):

| Species | Count | Charge contribution |
| --- | ---: | ---: |
| Na⁺ on Na1 | 6 | +6 |
| Na⁺ on Na2 | 12 | +12 |
| V_Na on Na2 | 6 | 0 |
| Zr⁴⁺ | 12 | +48 |
| P⁵⁺ | 6 | +30 |
| Si⁴⁺ | 12 | +48 |
| O²⁻ | 72 | −144 |
| **Total** | | **0** |

So the sample is charge-neutral with Na1 fully occupied and Na2 two-thirds occupied.

### Na3 (unstable) site note

The thesis describes Na3 (36f) as a metastable split position between Na1 and Na2 and uses it
for the interstitialcy transient. The accessible thesis does not tabulate a Na3 coordinate for
the Zr composition. Options:

1. Use the Na3 36f position from the DFT NEB path if the authors' data can be obtained.
2. Start from the isostructural Na₃₊ₓSc₂₋ₓ… refinements in the Jülich thesis (M. Guin, 2017,
   Table 4.5), e.g. Na3 ≈ (0.97, 0.71, 0.94), then relax/adjust it so the path is
   geometrically plausible.
3. Use the monoclinic `C2/c` structure instead, where the CIF explicitly gives Na3 at
   (0.836, 0.079, 0.842); see the phase alternative below.

Because the barrier is set by the Na3 site energy, an approximate Na3 position still produces
a runnable sample; it mainly affects the path geometry and any unstable–stable pair terms.
Record the approximation.

## Pair-interaction model

### Published values

From the thesis (Table 7.1), which underlies the PCCP 2022 KMC model:

| Pair | Distance | Energy | Meaning |
| --- | ---: | ---: | --- |
| Na⁺–Na⁺ | 4.92 Å | **+0.08 eV** | repulsive Na–Na |
| Na⁺–Si⁴⁺ | 3.11 Å | **−0.04 eV** | attractive Na–Si (1NN) |
| Na⁺–Si⁴⁺ | 3.71 Å | **−0.05 eV** | attractive Na–Si (2NN) |
| Na⁺–Na⁺ | 3.46 Å | 0 (omitted) | Na1 assumed fully occupied |

The model is written as

$$
E_{\mathrm{conf}}=\sum_i \frac{N_{\mathrm{Na},i}E_{\mathrm{Na},i}}{2}
+\sum_i N_{\mathrm{Si},i}E_{\mathrm{Si},i},
$$

where $i$ labels the Na⁺–Na⁺ and Na⁺–Si⁴⁺ neighbour shells around Na2/Na4 sites. The
published fit considered Na⁺–cation pairs up to 5.1 Å and 754 DFT configurations.

Interpretation:

- Na⁺–Na⁺ repulsion raises the site energy; this is the main reason conductivity drops at
  high Na content.
- Na⁺–Si⁴⁺ attraction lowers the Na⁺ site energy near silicon; this is the aliovalent
  substituent effect.
- Na–O, Na–Zr, Na–P and framework–framework terms are not part of the published model and are
  set to 0.
- Na1–Na2 is a constant because Na1 is assumed full; in the 5-site interstitialcy the Na1
  occupation never changes, so omitting it is exact for that mechanism.

### Mapping to the CIF structure

The published distances are averages over monoclinic/rhombohedral configurations. In the
`R-3c` CIF the nearest shells are:

| Pair | CIF distances (Å) | Suggested assignment |
| --- | --- | --- |
| Na1–Na2 | 3.449 | 0 eV (constant; filter or leave zero) |
| Na2–Na2 | 4.79, 4.96, 5.14, 5.81 | +0.08 eV for 4.79/4.96 (published 4.92); 5.14/5.81 outside 5.1 Å |
| Na2–Si | 3.05, 3.09, 3.86, 5.97, 5.99 | −0.04 eV for 3.05/3.09 (published 3.11); −0.05 eV for 3.86 (published 3.71); rest outside cutoff |

The multiplicities above are from the CIF symmetry expansion and should be re-checked after
the model builder expands the reference sites. The published energies are **per pair**, so
the same value applies to every symmetry-equivalent pair in the shell; do not multiply by the
coordination number when authoring the parameterization.

### Cutoff and filters

- Stable environment cutoff: **5.1 Å**, matching the published model.
- Recommended filters to keep the parameterization small:
  - Na1–Na2, Na1–O, Na1–Zr, Na1–P/Si;
  - Na2–O, Na2–Zr;
  - O–O, O–Zr, O–P/Si, Zr–P/Si.
- **Do not** filter Na2–P/Si: that site carries both P and Si, and the Na–Si term is the
  physically relevant one. Set ε(Na,P) = 0 and ε(Na,Si) = −0.04/−0.05 eV.
- If filters are omitted, leave the non-published pair energies at 0. The template will be
  larger but semantically identical.

## Migration barriers

### Published values

From the thesis (Table A.3.4), rhombohedral phase, x = 2:

| Mechanism | Path angle | $E_{\mathrm{mig},0}$ |
| --- | ---: | ---: |
| interstitial-like | 80° | **0.30 eV** |
| interstitialcy-like | 90° | **0.25 eV** |
| interstitialcy-like | 180° | **0.23 eV** |

For context (Table A.3.4):

| x | 80° | 90° | 180° |
| ---: | ---: | ---: | ---: |
| 0 | 0.52 | 0.34 | 0.19 |
| 1 | 0.42 | 0.30 | 0.23 |
| 2 | 0.30 | 0.25 | 0.23 |
| 3 | 0.20 | 0.16 | 0.15 |

The thesis also tabulates the raw per-configuration values (Tables A.3.3, A.3.5) and their
spread. The thesis's migration energy is a **directionally independent** barrier extracted
from the energy profile; the actual barrier in a local environment follows the usual
interpolation between initial and final state energies.

### Mapping to MOCASSIN

Use the user-guide "fixed migration barrier" recipe: place the mobile Na⁺ on the unstable Na3
site with a site energy equal to $E_{\mathrm{mig},0}$. The stable Na1/Na2 site energies
remain 0; the pair interactions provide the configuration dependence. The barrier is then

$$
E_{\mathrm{mig}} \approx E_{\mathrm{mig},0} + \frac{E_f - E_i}{2},
$$

which is the same interpolation used in the companion barium-cerate paper (Eq. 3 there).

If all interstitialcy paths share one Na3 reference site, they share one site energy. For a
first sample, use one of:

- **0.24–0.25 eV** (average of 90°/180°) for a single interstitialcy barrier;
- **0.23 eV** (180°, the fastest path) if only one path is modelled;
- separate 90° and 180° transitions with 0.25/0.23 eV only if the model builder can bind
  distinct unstable sites or distinct site-energy overrides per path.

The 0.02 eV difference is small compared with the other approximations; do not over-engineer
the first sample.

## Transport mechanism

### Interstitialcy-like (recommended, 5-site)

The published MOCASSIN simulations implemented the interstitialcy-like mechanism:

$$
\text{Na2} \rightarrow \text{Na3} \rightarrow \text{Na1} \rightarrow \text{Na3}' \rightarrow \text{Na2}'.
$$

A Na⁺ on Na2 moves toward the occupied Na1 site and pushes the Na1 ion onto a neighbouring
vacant Na2 site. The two Na3 positions are the unstable transient split-pair positions.

MOCASSIN mapping:

- Position chain: `[stable Na2][unstable Na3][stable Na1][unstable Na3'][stable Na2']`
- Connectors: `Dynamic Dynamic Dynamic Dynamic` (5-site interstitialcy-like)
- State-change groups, in order:
  1. `[Na⁺, V_Na]`
  2. `[Na⁺, empty]`
  3. `[Na⁺, Na⁺]` — central self-exchange/no-op
  4. `[Na⁺, empty]`
  5. `[Na⁺, V_Na]`

The central `[Na⁺, Na⁺]` group keeps the Na1 site occupied while the two neighbouring Na⁺
ions exchange. MOCASSIN's rule generator processes the dynamic pairs across the unstable
positions and recognises the push chain. The original self-exchange validation is disabled,
which is consistent with this use; still, verify the generated rules and the mass/charge
conservation in the built model.

**Important:** MOCASSIN does not auto-generate inverse transitions for chains with more than
three positions. The 5-site transition must define its own reverse direction or be paired with
an explicitly authored inverse. Check the rule set for both directions before running KMC.

### Interstitial-like (simpler alternative, 3-site)

$$
\text{Na1} \rightarrow \text{Na3} \rightarrow \text{Na2}
$$

- Position chain: `[stable Na1][unstable Na3][stable Na2]`
- Connectors: `Dynamic Dynamic`
- Groups: `[Na⁺, V_Na]`, `[Na⁺, empty]`, `[Na⁺, V_Na]`
- Barrier: 0.30 eV (the 80° value) or another documented choice.
- MOCASSIN auto-generates the inverse for 3-site migrations.

This model creates transient Na1 vacancies, for which the published pair model has no
Na1–Na2 term. Set Na1–Na2 = 0 and record the approximation. It is easier to author but less
faithful to the published correlated mechanism.

## Proposed MOCASSIN model blueprint

### Particles

| Particle | Charge | Role |
| --- | ---: | --- |
| Na⁺ | +1 | mobile |
| V_Na | 0 | vacancy on Na2 |
| Zr⁴⁺ | +4 | framework |
| P⁵⁺ | +5 | framework |
| Si⁴⁺ | +4 | substituent on the P/Si site |
| O²⁻ | −2 | framework |

The void/empty particle is the model's implicit null particle and is used only in the
`[Na, empty]` unstable-site groups.

### Sites

| Site | Stability | Occupant(s) |
| --- | --- | --- |
| Na1 (6b) | stable | Na⁺ (full) |
| Na2 (18e) | stable | Na⁺ / V_Na (2/3, 1/3) |
| Na3 (36f) | unstable | Na⁺ / empty (transient only) |
| Zr (12c) | stable | Zr⁴⁺ |
| P/Si (18e) | stable | P⁵⁺ / Si⁴⁺ (1/3, 2/3) |
| O1 (36f) | stable | O²⁻ |
| O2 (36f) | stable | O²⁻ |

### Energy model

- Stable environment cutoff: 5.1 Å.
- Pair energies as in the table above; all other pairs 0.
- Na⁺ on Na3 site energy: 0.23–0.25 eV (choose one value and document it).
- No cluster/multi-body terms are required for the published model.
- Consider filters from the previous section to keep the parameterization readable.

### Transition model

- One 5-site interstitialcy chain for 90° and one for 180°, **or** one chain with a single
  averaged barrier.
- Optional 3-site interstitial-like chain for 80° if a simpler/second mechanism is wanted.
- Verify:
  - the reference paths do not form rings;
  - the two Na3 positions are symmetry-consistent with the chosen Na1/Na2 sites;
  - the generated rules preserve mass and charge;
  - both forward and reverse directions exist.

### Initial configuration

- Na1: 6 Na⁺ per conventional cell (full).
- Na2: 12 Na⁺ + 6 V_Na (2/3 occupancy).
- P/Si: 6 P⁵⁺ + 12 Si⁴⁺ (1/3, 2/3), randomly distributed.
- Zr, O: fully occupied framework.
- Charge neutrality is exact as shown above.
- For KMC, only Na⁺ is mobile; P/Si and framework remain fixed.

### Job settings for a first run

- Supercell: start small (e.g. 2×2×2 or 3×3×3) for a fast sample; the published study used
  10×10×10.
- Temperature: 450–700 K (published range).
- Electric field: 10⁷ V m⁻¹ (published field-independent value).
- MCSP: 10,000 after 100 equilibration steps for parity; reduce for a smoke run.
- Independent runs: 12 for parity; 1–3 for a smoke run.
- Attempt frequency: fixed; the NaSICON thesis excerpt does not state the value. The companion
  barium-cerate paper uses 10¹³ Hz. Record the value used and calibrate only if matching the
  published conductivity is required.

## Validation targets

Published KMC values for x = 2, rhombohedral, model 4 (pair interactions + direction- and
composition-dependent barriers):

| Observable | Value | Source |
| --- | ---: | --- |
| Activation energy $E_A$ | ≈ 0.30 eV | thesis Table A.3.15 |
| $\sigma$ at 300 K | ≈ 1.8 × 10⁻² S cm⁻¹ | thesis Table A.3.10 |
| $\sigma$ at 573 K | ≈ 2.2 S cm⁻¹ | thesis Table A.3.10 |
| Partial $E_A$, 80° / 90° / 180° | 0.37 / 0.30 / 0.29 eV | thesis Table A.3.16 |

The published KMC conductivities are high compared with experiment; the thesis discusses this.
Use them as a **model-consistency check**, not as experimental truth. A reasonable acceptance
test for the sample:

- the Arrhenius fit of the simulated $\sigma(T)$ gives $E_A$ within ~0.03–0.05 eV of 0.30 eV;
- the qualitative trend σ(573 K) > σ(300 K) holds;
- mass and charge are conserved;
- the Na2 occupancy remains near 2/3 in the absence of a driving force;
- no ring transitions, invalid rules, or unstable-position leakage appear in the build
  diagnostics.

## Caveats and approximations

1. **Na1–Na2 interactions are not tabulated.** The published pair model assumes Na1 is full.
   The 5-site interstitialcy keeps Na1 occupied, so the omission is consistent. The 3-site
   alternative creates Na1 vacancies; set Na1–Na2 = 0 and document it.
2. **Na3 coordinates are approximate.** Use a DFT-relaxed path position if available;
   otherwise use the published split-site analogue and record the choice.
3. **Published distances are averages.** Map the 4.92/3.11/3.71 Å values onto the nearest CIF
   shells; do not expect a one-to-one shell match.
4. **Barrier is an average over local environments.** The full dependence requires the DFT
   NEB data or the authors' MOCASSIN project; the thesis tabulates averages and ranges.
5. **Phase choice matters.** The CIF is the 320 °C `R-3c` phase; the room-temperature phase is
   monoclinic `C2/c` (COD 1529608). The thesis gives monoclinic barriers too (x = 2:
   0.33/0.21/0.19 eV) and notes the pair interactions are similar. The `R-3c` phase is
   simpler and matches the published KMC; use it first.
6. **Attempt frequency is not fixed by the source excerpt.** Record the chosen value and do
   not tune it silently to match experiment.
7. **The published KMC used a 10×10×10 supercell.** A small sample supercell will have
   finite-size and statistics limitations; label smoke runs as such.
8. **P/Si disorder is random in the published simulations.** The initial configuration must
   be seeded deterministically for reproducibility.

## Alternatives considered

| Material | Structure | Why attractive | Why not first choice |
| --- | --- | --- | --- |
| **LaSrGa₃O₇ melilite** | `P-421m`, a ≈ 8.056 Å, c ≈ 5.333 Å | O²⁻ interstitial conductor; same group's DFT + KMC pair-interaction model; very distinct structure | Paper is closed access; data availability depends on institutional access |
| **Y-doped BaCeO₃** | `Pnma` (low T), `R3c`/`Pm3m` at high T | Open-access 2025 paper; explicitly uses MOCASSIN; literature-compiled proton–dopant interactions and barriers | Still a perovskite proton conductor, chemically close to the BaZrO₃ case we want to avoid |
| **YSZ** | cubic fluorite `Fm-3m` | Simple structure; huge literature; well-known barriers | Published interaction model is a cluster expansion with many-body ECIs, not a clean pair table |
| **UO₂ / UO₂₊ₓ** | cubic fluorite | Simple structure; rich KMC literature; oxygen-interstitial clusters | Radioactive material; defect interactions are cluster/binding energies rather than a simple pair table |
| **Li-argyrodite / LGPS / LLZO** | cubic / tetragonal | Important Li-ion conductors; KMC literature exists | Pair-interaction parameter sets are scattered and less consistently tabulated |

The melilite is the best fallback if an oxygen-interstitial sample is preferred; the barium
cerate paper is the best fallback if an open, directly MOCASSIN-based parameter set is
preferred despite the perovskite similarity.

## Open questions

- Which phase should the checked-in sample use: `R-3c` (published KMC) or `C2/c`
  (room-temperature, explicit Na3 coordinates)?
- Should the sample implement the 5-site interstitialcy only, or also the 3-site
  interstitial-like path?
- Should the sample use one averaged barrier or separate 90°/180° transitions?
- Can the authors' DFT Na3 path coordinates or MOCASSIN project be obtained? If not, which
  approximation is acceptable?
- Which supercell size and MCSP are appropriate for a checked-in sample versus a nightly
  parity run?
- Where should the sample live: `samples/nasicon/` for the project, `tests/reference/` for
  the parity fixture, or both?
- Does the sample use the new JSON project format, a legacy `.mocprj`, or both? The
  repository's clean-break rule forbids a legacy reader/writer, so the checked-in sample
  should be new-format JSON unless it is a generated reference artifact outside the product
  path.

## Next steps / promotion criteria

1. Collect the remaining sample/testcase drafts (the user plans a second draft) before
   starting work.
2. Decide the phase, mechanism, barrier treatment, and sample location.
3. Author the project JSON / `.mocprj` with the particles, sites, pair energies, site energy,
   transitions, initial configuration, and job settings above.
4. Build with the original model builder and run a smoke KMC job with the original solver.
5. Run the validation checks in the "Validation targets" section and record the results.
6. Add the sample to `samples/` and, if it becomes a parity fixture, to `tests/reference/`
   with a manifest (source DOIs, CIF IDs, parameter values, supercell, temperature, field,
   MCSP, seeds, solver/build hashes).
7. Promote this draft through the normal process if it changes Step 29 scope, the sample
   layout, or the parity matrix. The likely owner is a Step 29 update or a dedicated
   sample-data step; the draft itself is not an implementation authorization.

## References

### Primary data source

- J. Schütt, *The Origins of High Sodium Ion Conductivity in NaSICON-Type Materials: A
  Computational Investigation of Structural and Transport Properties*, PhD thesis, RWTH
  Aachen University, 2024. RWTH Publications record 995864:
  <https://publications.rwth-aachen.de/record/995864>
  - Open PDF: <https://publications.rwth-aachen.de/record/995864/files/995864.pdf>
  - Structure: Chapters 3 and 5, Appendix A.1.
  - Pair model: Section 7.1.1, Table 7.1.
  - Migration energies: Section 7.1.2, Tables A.3.3–A.3.6.
  - KMC setup: Section 4.3.3.
  - KMC results: Section 7.2, Tables A.3.7–A.3.17.

### Peer-reviewed papers

- J. Schuett, F. Pescher, S. Neitzel-Grieshammer, "The origin of high Na⁺ ion conductivity in
  Na₁₊ₓZr₂SiₓP₃₋ₓO₁₂ NASICON materials", *Phys. Chem. Chem. Phys.* **2022**, 24,
  22154–22167. DOI: [10.1039/D2CP03621E](https://doi.org/10.1039/D2CP03621E).
- J. Schuett, J. Schillings, S. Neitzel-Grieshammer, "Interstitial or interstitialcy: effect
  of the cation size on the migration mechanism in NaSICON materials", *Phys. Chem. Chem.
  Phys.* **2024**, 26, 2190–2204. DOI: [10.1039/D3CP05089K](https://doi.org/10.1039/D3CP05089K).
- J. Schuett, A. S. Kuhn, S. Neitzel-Grieshammer, "Predicting the Na⁺ ion transport properties
  of NaSICON materials using density functional theory and kinetic Monte Carlo", *J. Mater.
  Chem. A* **2023**, 11, 9160–9177. DOI: [10.1039/D3TA00440F](https://doi.org/10.1039/D3TA00440F).
- S. Eisele, S. Grieshammer, "MOCASSIN: Metropolis and kinetic Monte Carlo for solid
  electrolytes", *J. Comput. Chem.* **2021**, 42, 737–748.
  DOI: [10.1002/jcc.26418](https://doi.org/10.1002/jcc.26418).

### Structure

- COD 1529809, Na₃Zr₂Si₂PO₁₂, `R-3c`, 320 °C:
  <https://www.crystallography.net/cod/1529809.html>
- COD 1529608, Na₃Zr₂Si₂PO₁₂, `C2/c`, room temperature:
  <https://www.crystallography.net/cod/1529608.html>
  - W. H. Baur, J. R. Dygas, D. H. Whitmore, J. Faber, *Solid State Ionics* **1986**, 18–19,
    935–943.

### Alternatives

- J. Schütt, T. K. Schultze, S. Neitzel-Grieshammer, "Oxygen Ion Migration and Conductivity in
  LaSrGa₃O₇ Melilites from First Principles", *Chem. Mater.* **2020**, 32, 4442–4450.
  DOI: [10.1021/acs.chemmater.9b04599](https://doi.org/10.1021/acs.chemmater.9b04599).
- G. Winterhoff, S. Neitzel-Grieshammer, "Kinetic Monte Carlo simulation of proton conductivity
  in Y-doped barium cerate", *Solid State Ionics* **2025**, 428, 116953.
  DOI: [10.1016/j.ssi.2025.116953](https://doi.org/10.1016/j.ssi.2025.116953).
  - Open PDF: <https://publications.rwth-aachen.de/record/1015918/files/1015918.pdf>
- M. Guin, *Chemical and physical properties of sodium ionic conductors for solid-state
  batteries*, PhD thesis, RWTH Aachen / Forschungszentrum Jülich, 2017 (Jülich report
  Energie & Umwelt 373). Used only for the Na3 split-site analogue and general NASICON
  structure discussion.

## Appendix A: CIF coordinate excerpt used above

The following is the site list from COD 1529809 used to derive the table in this draft
(fractional coordinates, hexagonal setting):

```text
Zr1  0     0     0.1469
P1   0.2883 0    0.25
Si1  0.2883 0    0.25
O1   0.1762 -0.036 0.1944
Na1  0     0     0
O2   0.1939 0.1707 0.0929
Na2  0.6292 0    0.25
```

Occupancies: Na1 1.01, Na2 0.66, P/Si site P 0.33 / Si 0.67, others 1.

## Appendix B: Computed neighbour distances from the CIF

Computed with the hexagonal lattice a = 9.0535 Å, c = 23.0677 Å and the CIF symmetry
operations (periodic images included):

| Pair | Distances (Å) | Pair count per 18 Na2 (approx.) |
| --- | --- | ---: |
| Na1–Na2 | 3.449 | 2 per Na2 (6 per Na1) |
| Na2–Na2 | 4.79, 4.96, 5.14, 5.81 | 2, 2, 1, 1 |
| Na2–Si | 3.05, 3.09, 3.86, 5.97, 5.99 | 2, 1, 2, —, — |

These are geometry checks only. Recompute after the model builder expands the reference sites
and before assigning the published per-pair energies.
