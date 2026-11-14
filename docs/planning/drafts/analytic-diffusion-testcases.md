# Analytically solvable diffusion test cases

> This is a design scratch, not a decision. See [`README.md`](README.md).

| Field | Value |
| --- | --- |
| Status | Under discussion |
| Owner | Research / validation design |
| Created | 2026-11-14 |
| Last updated | 2026-11-14 |
| Target step | Step 29 (validation, parity, performance); later `tests/reference/` |
| Related | Step 29, ADR 0005 (statistical parity), `docs/planning/07-reference-build.md` |
| Summary | Seven fixed-barrier, non-interacting vacancy-diffusion test cases on high-symmetry lattices, with published analytical tracer diffusion coefficients and correlation factors. |

## Context

The real-material reference systems (ceria, barium zirconate, and the proposed NaSICON sample) validate the
full model stack, but they are not analytically solvable. A small set of trivial structures is needed to
validate the solver kernel itself:

- one mobile species and one vacancy;
- a single 1NN migration with a constant barrier;
- no pair interactions, no clusters, no electric field;
- a lattice for which the tracer diffusion coefficient $D^{*}$ and the tracer correlation factor $f^{*}$
  are known analytically.

The dissertation of Sebastian Eisele contains exactly such a set in §4.3.3.1 (“Tracer correlation and
conductivity in model structures”), Tables 4.3 and 4.4. The structures are the classical 2D and 3D lattices
whose correlation factors were computed by Compaan and Haven.

This draft records the test cases, the analytical formulas, the published reference values, and the
implementation details needed to reproduce them. **It does not create the test fixtures and does not authorize
implementation.**

## Source

- S. Eisele, *Development and application of a generic Monte Carlo simulation framework for defect transport
  in crystalline solid electrolytes*, PhD thesis, RWTH Aachen University, 2021/2022.
- RWTH Publications record 836291: <https://publications.rwth-aachen.de/record/836291>
- DOI: [10.18154/RWTH-2021-11223](https://doi.org/10.18154/RWTH-2021-11223)
- Open PDF: <https://publications.rwth-aachen.de/record/836291/files/836291.pdf>
- Relevant section: §4.3.3.1, Tables 4.3 and 4.4; additional consistency checks in the same section,
  Figures 4.21–4.23.
- Correlation factors: K. Compaan, Y. Haven, “Correlation factors for diffusion in solids,”
  *Trans. Faraday Soc.* **1956**, 52, 786.

## Scope and non-goals

In scope:

- infinite-dilution vacancy diffusion with a single vacancy;
- constant 1NN migration barrier;
- no defect–defect interactions;
- tracer diffusion coefficient $D^{*}$;
- tracer correlation factor $f^{*}$;
- additional known-limit checks: vacancy-fraction dependence, conductivity symmetry, field-strength
  linearity.

Not in scope:

- interacting defects;
- multiple mobile species;
- interstitialcy or vehicle mechanisms;
- finite-temperature energy landscapes;
- quantitative agreement with any real material.

## Theory

### Hopping model

For a cubic crystal with strongly diluted, non-interacting vacancies, the tracer diffusion coefficient is

$$
D^{*}(T) \approx f^{*} x_{\mathrm{v}} D_{\mathrm{v}}
= f^{*} x_{\mathrm{v}}
  \frac{\nu_{0}^{*} N_{\mathrm{dir}}^{*} l^{2}}{6}
  \exp\!\left(-\frac{E_{\mathrm{mig}}^{*}}{k_{\mathrm{B}}T}\right),
\tag{2.28}
$$

where

- $f^{*}$ is the tracer correlation factor;
- $x_{\mathrm{v}}$ is the vacancy molar fraction;
- $\nu_{0}^{*}$ is the attempt frequency per direction;
- $N_{\mathrm{dir}}^{*}$ is the number of possible jump directions per tracer site;
- $l$ is the jump distance;
- $E_{\mathrm{mig}}^{*}$ is the fixed migration barrier.

The thesis applies this expression to all seven test cases, including the 2D lattices.

### Simulated diffusion coefficient

The simulated tracer diffusion coefficient follows from the mean square displacement (MSD):

$$
D_{\mathrm{sim}}^{*} = \frac{\left\langle |\Delta \vec{x}|^{2} \right\rangle}{2dt},
\tag{2.13}
$$

with $d=2$ for the 2D cases and $d=3$ for the 3D cases.

### Simulated correlation factor

The simulated tracer correlation factor is

$$
f_{\mathrm{sim}}^{*}
= \left\langle
  \frac{\Delta \vec{r}(t_{\mathrm{sim}}) \cdot \Delta \vec{r}(t_{\mathrm{sim}})}
       {\Gamma\, t_{\mathrm{sim}}\, l_{\mathrm{jump}}^{2}}
\right\rangle,
\tag{4.16}
$$

where $\Gamma$ is the migration rate and the angle brackets denote the thermal average.

## Test-case set

From Table 4.3. All models use $a=b=c=1$ Å and the angles of the affiliated crystal system.

| # | Structure | Space group | Wyckoff position | $l_{\mathrm{jump}}$ / Å | Dimension |
| ---: | --- | --- | --- | ---: | :---: |
| 1 | square lattice | $P\bar{4}$ (81) | $0,0,0$ | 1.0 | 2D |
| 2 | hexagonal lattice | $P6$ (168) | $0,0,0$ | 1.0 | 2D |
| 3 | honeycomb lattice | $P6$ (168) | $1/3,2/3,0$ | $\cos(30^{\circ})\cdot 0.5 \approx 0.433013$ | 2D |
| 4 | cubic lattice | $P23$ (195) | $0,0,0$ | 1.0 | 3D |
| 5 | body-centred cubic (bcc) | $I23$ (197) | $0,0,0$ | $\sqrt{3}\cdot 0.5 \approx 0.866025$ | 3D |
| 6 | face-centred cubic (fcc) | $F23$ (196) | $0,0,0$ | $\sqrt{2}\cdot 0.5 \approx 0.707107$ | 3D |
| 7 | diamond lattice | $Fd\bar{3}m$ (227) | $0,0,0$ | $\sqrt{3}\cdot 0.25 \approx 0.433013$ | 3D |

### Simulation settings

From §4.3.3.1:

| Setting | Value |
| --- | --- |
| Lattice parameters | $a=b=c=1$ Å, crystal-system angles |
| 1NN migration barrier | 1 eV, constant |
| Attempt frequency | $10^{13}$ Hz |
| Electric field | none |
| Temperature | 1000 K |
| Supercell, 2D | $100 \times 100 \times 1$ |
| Supercell, 3D | $25 \times 25 \times 16$ |
| Vacancies | exactly one per supercell |
| Other particles | tracers |
| MCSP | 10,000 |
| Independent samples | 48, random starting conditions |
| Averaging | thermal average of the MSD |

## Published reference values

From Table 4.4. The theoretical values use Eq. (2.28); the simulated values use Eq. (2.13); the simulated
correlation factors use Eq. (4.16).

| Structure | $D_{\mathrm{theo}}^{*}$ / m² s⁻¹ | $D_{\mathrm{sim}}^{*}$ / m² s⁻¹ | $f_{\mathrm{theo}}^{*}$ | $f_{\mathrm{sim}}^{*}$ |
| --- | ---: | ---: | ---: | ---: |
| 2D square | $2.8414\cdot10^{-17}$ | $2.836(4)\cdot10^{-17}$ | 0.46705 | 0.4662(7) |
| 2D hexagonal | $5.1109\cdot10^{-17}$ | $5.108(8)\cdot10^{-17}$ | 0.56006 | 0.5597(8) |
| 2D honeycomb | $2.5348\cdot10^{-18}$ | $2.535(3)\cdot10^{-18}$ | $1/3$ | 0.3336(4) |
| 3D cubic | $5.9818\cdot10^{-17}$ | $5.969(7)\cdot10^{-17}$ | 0.65549 | 0.6541(8) |
| 3D bcc | $3.2919\cdot10^{-17}$ | $3.322(3)\cdot10^{-17}$ | 0.72149 | 0.7280(7) |
| 3D fcc | $1.7827\cdot10^{-17}$ | $1.783(1)\cdot10^{-17}$ | 0.78146 | 0.7816(5) |
| 3D diamond | $7.1288\cdot10^{-19}$ | $7.133(3)\cdot10^{-19}$ | $1/2$ | 0.5003(3) |

The thesis notes that a few simulated values differ from the analytical values by slightly more than the
statistical error, and that literature values for $f^{*}$ also differ within a similar margin. The bcc case
is the largest visible deviation (about 0.9 % in $D^{*}$ and $f^{*}$).

## Reconstructing the model parameters

Eq. (2.28) can be inverted to check the parameters behind each theoretical value:

$$
x_{\mathrm{v}} N_{\mathrm{dir}}^{*}
= \frac{D_{\mathrm{theo}}^{*}}
       {f^{*}\, \frac{\nu_{0} l^{2}}{6}\,
        \exp(-E_{\mathrm{mig}}/(k_{\mathrm{B}}T))}.
$$

Using $\nu_{0}=10^{13}$ Hz, $E_{\mathrm{mig}}=1$ eV, and $T=1000$ K:

| Structure | $l$ / Å | $f_{\mathrm{theo}}^{*}$ | $D_{\mathrm{theo}}^{*}$ / m² s⁻¹ | $x_{\mathrm{v}}N_{\mathrm{dir}}^{*}$ | Plausible $N_{\mathrm{dir}}^{*}$ | Implied $x_{\mathrm{v}}$ |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| 2D square | 1.0 | 0.46705 | $2.8414\cdot10^{-17}$ | $4.000\cdot10^{-4}$ | 4 | $1.0\cdot10^{-4}$ |
| 2D hexagonal | 1.0 | 0.56006 | $5.1109\cdot10^{-17}$ | $6.001\cdot10^{-4}$ | 6 | $1.0\cdot10^{-4}$ |
| 2D honeycomb | 0.433013 | $1/3$ | $2.5348\cdot10^{-18}$ | $2.667\cdot10^{-4}$ | 3 | $8.89\cdot10^{-5}$ (see note) |
| 3D cubic | 1.0 | 0.65549 | $5.9818\cdot10^{-17}$ | $6.001\cdot10^{-4}$ | 6 | $1.0\cdot10^{-4}$ |
| 3D bcc | 0.866025 | 0.72149 | $3.2919\cdot10^{-17}$ | $4.000\cdot10^{-4}$ | 8 | $5.0\cdot10^{-5}$ |
| 3D fcc | 0.707107 | 0.78146 | $1.7827\cdot10^{-17}$ | $3.000\cdot10^{-4}$ | 12 | $2.5\cdot10^{-5}$ |
| 3D diamond | 0.433013 | $1/2$ | $7.1288\cdot10^{-19}$ | $5.000\cdot10^{-5}$ | 4 | $1.25\cdot10^{-5}$ |

The 3D vacancy fractions follow from the supercell site counts:

- cubic: $25^{2}\cdot16 = 10{,}000$ sites → $x_{\mathrm{v}}=10^{-4}$;
- bcc: 2 atoms per cell → 20,000 sites → $x_{\mathrm{v}}=5\cdot10^{-5}$;
- fcc: 4 atoms per cell → 40,000 sites → $x_{\mathrm{v}}=2.5\cdot10^{-5}$;
- diamond: 8 atoms per cell → 80,000 sites → $x_{\mathrm{v}}=1.25\cdot10^{-5}$.

The 2D square and hexagonal cases give one site per cell and $x_{\mathrm{v}}=10^{-4}$. The honeycomb is the
only ambiguous case: a 3-neighbour honeycomb with $x_{\mathrm{v}}N_{\mathrm{dir}}^{*}=2.667\cdot10^{-4}$
implies $x_{\mathrm{v}}\approx8.89\cdot10^{-5}$, which does not follow from a simple
$100\times100\times1$ two-site cell. Recover the exact honeycomb model from the MOCASSIN example structures
or reconstruct it and match $D_{\mathrm{theo}}^{*}$ directly.

## Secondary consistency checks

These are not closed-form analytical cases, but they have known limits and symmetries and are useful kernel
tests.

### Vacancy-fraction dependence of $f^{*}$

From Figure 4.21. Using the same structures, 1 eV barrier, and 1000 K:

- $f^{*}(x_{\mathrm{v}}\to0)$ approaches the infinite-dilution values in Table 4.4;
- $f^{*}(x_{\mathrm{v}}\to1)$ approaches 1;
- $f^{*}(0.01)$ is almost identical to the infinite-dilution value.

The vacancy fraction was varied from 0.01 to 0.95.

### Conductivity vs vacancy fraction

From Figure 4.22. Mobile species A⁺, 1 eV barrier, attempt frequency 10 THz, electric field 10 MV/m in the
x-direction, square, honeycomb, fcc, and diamond structures:

- $\sigma_{\mathrm{A}}(x_{\mathrm{v}})=\sigma_{\mathrm{A}}(1-x_{\mathrm{v}})$;
- $\sigma_{\mathrm{A}}$ is maximal near $x_{\mathrm{v}}\approx0.5$;
- the maximum is caused by vacancy–vacancy site blocking, not by a change in the jump rate.

### Field-strength linearity

From Figure 4.23. 1NN vacancy migration of A⁺ in a $10\times10\times10$ fcc supercell with
$x_{\mathrm{v}}=0.1$, $10^{13}$ Hz, field in the x-direction:

- linear response for fields from about 2 MV/m to about 1 GV/m;
- increasing noise below 1 MV/m;
- nonlinear conductivity above about 1 GV/m;
- barrier-dependent tipping above 10 GV/m.

This is a useful regression test for the field term, even though the absolute conductivity is not
analytically known for the concentrated case.

## Implementation blueprint for the new project

One small project per lattice is cleaner than one project with seven structures.

### Structure

- Use the published space group and Wyckoff position.
- $a=b=c=1$ Å with the crystal-system angles.
- One stable reference site for the lattice.
- One unstable reference site at the midpoint of the 1NN bond (or a symmetry-consistent saddle point).
- For 2D cases, either support a 2D supercell mode or emulate it with a one-cell-thick 3D supercell and only
  in-plane transitions.

### Particles

| Particle | Charge | Role |
| --- | ---: | --- |
| A⁺ | +1 | mobile tracer / conducting species |
| V_A | 0 (vacancy) | single vacancy |

For pure tracer-diffusion tests the charge is not used, but using A⁺ keeps the conductivity tests consistent.

### Transition

- 3-site vacancy migration: `[stable][unstable][stable]`.
- State-change groups: `[A⁺, V_A]`, `[A⁺, empty]`, `[A⁺, V_A]`.
- Inverse transition generated automatically for the 3-site case.
- Jump length set by the lattice; verify it matches Table 4.3.

### Energy model

- Stable site energy: 0 for A⁺.
- Unstable site energy: 1 eV for A⁺.
- No pair interactions, no clusters, no filters needed.
- No electric field for the diffusion tests.

### Initial configuration

- Exactly one vacancy per supercell.
- All other lattice sites occupied by A⁺.
- Random starting position of the vacancy for each of the 48 samples.

### Job settings

- KMC, null-event algorithm.
- 1000 K.
- 10,000 MCSP.
- 48 independent samples.
- 2D: $100\times100\times1$; 3D: $25\times25\times16$.
- Attempt frequency $10^{13}$ Hz.

### Evaluation

- Compute $D_{\mathrm{sim}}^{*}$ from the MSD with Eq. (2.13); use $d=2$ for 2D and $d=3$ for 3D.
- Compute $f_{\mathrm{sim}}^{*}$ with Eq. (4.16).
- Compare against Table 4.4.
- Report the sample mean and the standard error over the 48 samples.

## Acceptance criteria

A test case passes if:

- $D_{\mathrm{sim}}^{*}$ agrees with $D_{\mathrm{theo}}^{*}$ within the combined statistical uncertainty,
  or within a documented tolerance if the sample count is reduced;
- $f_{\mathrm{sim}}^{*}$ agrees with $f_{\mathrm{theo}}^{*}$ within the same criterion;
- the simulated values reproduce the published ordering and magnitudes across all seven lattices;
- mass and charge are conserved;
- the vacancy performs the expected 1NN random walk with no spurious long-range jumps;
- the 2D cases use $d=2$ in the MSD and the 3D cases use $d=3$.

The published deviations are small enough that a tolerance of about 1 % relative, plus the statistical error,
is a reasonable first target. The thesis itself shows a few deviations slightly above the pure statistical
error, so do not over-tighten the tolerance before the sample count is matched.

## Caveats and open questions

- **Honeycomb reconstruction.** The published $D_{\mathrm{theo}}^{*}$ implies
  $x_{\mathrm{v}}N_{\mathrm{dir}}^{*}=2.667\cdot10^{-4}$. The exact basis/supercell used to obtain it is not
  stated in the table. Recover it from the MOCASSIN example or fit the model to the published $D^{*}$.
- **2D representation.** The thesis lists 3D space groups (`P4̄`, `P6`) but calls the cases 2D. Confirm whether
  the original models use a one-cell-thick supercell with only in-plane transitions, and how the new solver
  should represent that.
- **Vacancy fraction.** The analytical $D^{*}$ scales with $x_{\mathrm{v}}$. If the new sample uses a
  different supercell, either keep $x_{\mathrm{v}}$ fixed or scale the target accordingly.
- **Attempt frequency.** The tests use $10^{13}$ Hz. Do not change it without rescaling the target.
- **Correlation-factor definition.** Eq. (4.16) uses the migration rate $\Gamma$; confirm whether the new
  tracker reports the same quantity or whether $f^{*}$ must be computed from the jump count.
- **Random starting conditions.** The published values are thermal averages over 48 samples. A single sample
  will not reproduce them.
- **GPL boundary.** The MOCASSIN example structures and source are GPL-3.0-or-later. Use them as reference
  only; do not copy files or code into the MIT repository.
- **Temperature.** Only 1000 K is published for the main table. Other temperatures can be added later if the
  analytical scaling is used.

## Promotion criteria

Promote this draft when:

- the seven structure models are defined unambiguously, including the honeycomb and the 2D representation;
- the analytical targets and tolerances are accepted;
- at least one fast kernel test and one full seven-case parity run exist;
- the results are recorded in `tests/reference/` with the usual manifest (source DOI, parameters, supercells,
  seeds, sample counts, solver hashes).

The likely owner is a Step 29 update or a dedicated validation-fixture step. The draft itself is not an
implementation authorization.

## References

1. S. Eisele, *Development and application of a generic Monte Carlo simulation framework for defect transport
   in crystalline solid electrolytes*, PhD thesis, RWTH Aachen University, 2021/2022.
   RWTH record 836291: <https://publications.rwth-aachen.de/record/836291>.
   DOI: [10.18154/RWTH-2021-11223](https://doi.org/10.18154/RWTH-2021-11223).
   Open PDF: <https://publications.rwth-aachen.de/record/836291/files/836291.pdf>.
   - §2.1.2 for Eqs. (2.13) and (2.28);
   - §4.3.3.1 for Tables 4.3 and 4.4 and Figures 4.21–4.23.
2. K. Compaan, Y. Haven, “Correlation factors for diffusion in solids,” *Trans. Faraday Soc.* **1956**, 52,
   786.
3. G. Murch, “The Haven ratio in fast ionic conductors,” *Solid State Ionics* **1982**, 7, 177–198.
4. S. Eisele, S. Grieshammer, “MOCASSIN: Metropolis and kinetic Monte Carlo for solid electrolytes,”
   *J. Comput. Chem.* **2021**, 42, 737–748.
   DOI: [10.1002/jcc.26418](https://doi.org/10.1002/jcc.26418).

## Appendix A — source map

| Item | Source |
| --- | --- |
| Test-case structures and jump lengths | Eisele 2021/2022, §4.3.3.1, Table 4.3 |
| Simulation settings | Eisele 2021/2022, §4.3.3.1, text before Table 4.3 |
| Analytical $D^{*}$ | Eisele 2021/2022, Eq. (2.28) |
| Simulated $D^{*}$ | Eisele 2021/2022, Eq. (2.13) |
| Simulated $f^{*}$ | Eisele 2021/2022, Eq. (4.16) |
| Reference values | Eisele 2021/2022, Table 4.4 |
| $f^{*}(x_{\mathrm{v}})$ | Eisele 2021/2022, Figure 4.21 |
| $\sigma(x_{\mathrm{v}})$ | Eisele 2021/2022, Figure 4.22 |
| Field-strength linearity | Eisele 2021/2022, Figure 4.23 |
| Correlation factors | Compaan & Haven 1956 |

## Appendix B — formula summary

$$
D^{*}(T) \approx f^{*} x_{\mathrm{v}}
\frac{\nu_{0}^{*} N_{\mathrm{dir}}^{*} l^{2}}{6}
\exp\!\left(-\frac{E_{\mathrm{mig}}^{*}}{k_{\mathrm{B}}T}\right)
$$

$$
D_{\mathrm{sim}}^{*} = \frac{\langle |\Delta \vec{x}|^{2}\rangle}{2dt}
$$

$$
f_{\mathrm{sim}}^{*}
= \left\langle
\frac{\Delta \vec{r}(t_{\mathrm{sim}})\cdot\Delta \vec{r}(t_{\mathrm{sim}})}
     {\Gamma\,t_{\mathrm{sim}}\,l_{\mathrm{jump}}^{2}}
\right\rangle
$$
