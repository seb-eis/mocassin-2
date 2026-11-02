# ADR 0012 — Transport and MSD Definitions

- **Status:** Accepted
- **Date:** 2026-11-02
- **Deciders:** Project owner

## Context

The evaluation API must produce one unambiguous value for diffusion, mobility, conductivity, the Haven ratio,
and correlated transport. The original implementation has known ambiguities:

- `N` sometimes means all lattice particles, sometimes mobile particles;
- the charge factor in `D_eff` was suspect;
- vacancy and polaron contributions were easy to mix;
- MSD was cumulative from the initial state, not time-lagged.

These are silent-factor-error risks. The Python API must match .NET exactly.

## Decision

### MSD and diffusion

Use **origin-based cumulative MSD**, matching the original tracker semantics:

```text
MSD_s(t) = (1 / N_s) * Σ_i |r_i(t) - r_i(0)|²
D_s      = MSD_s / (2 d t)        // d = 1, 2, 3
D_total  = MSD_total / (6 t)      // d = 3
```

- `N_s` is the number of particles in the tracked ensemble `s` (mobile species, global tracker, or selected
  group), **not** the total lattice particle count.
- Voids and immobile particles are excluded from `N_s` unless the caller explicitly requests them.
- `t` is the simulated KMC time; if `t = 0`, the result is `n/a`.
- A time-lagged MSD is not part of v1. It may be added later as a separately named evaluation.

### Per-species and total transport

For each charge-carrying species `s`:

```text
u_s      = (⟨R_s⟩ · ê) / (|E| t)
σ_s      = u_s * z_s * n_s * e
D_σ,s    = σ_s * k_B * T / (n_s * (z_s * e)²)
H_s      = D_tracer,s / D_σ,s
```

- `n_s` is the number density of species `s`; `z_s` is its charge number.
- The total conductivity is `σ_total = Σ_s σ_s` for independent migration.
- The total Nernst–Einstein coefficient is
  `D_σ,total = σ_total * k_B * T / Σ_s(n_s * (z_s * e)²)`.
- `H_total = D_tracer,total / D_σ,total`.
- Report per-species and total values explicitly. Do not silently collapse species.
- Vacancies and neutral particles are excluded from charge transport but may be reported separately.
- Polarons and other charge-carrying defects are treated as species with their own charge, density, and
  tracker ensemble.
- If `|E| = 0`, `t = 0`, or `z_s = 0`, the affected value is `n/a`, never `NaN` or `Infinity`.

### Correlation and Onsager

- Use the mobile trackers and the same per-species partitioning.
- `L_ij` is a full symmetric matrix; `L_ii` includes the auto and cross terms defined in Step 20.
- Units are `1/(J·m·s)`.
- Cross-species terms are reported separately; do not assume they are zero.

### Normalization and uncertainty

- `MigrationRate` uses the tracked ensemble count, not total particles.
- Aggregate uncertainties across independent jobs (SEM over job-level values), not across particles from one
  trajectory.
- Shared formula test vectors in .NET and Python must produce the same values within floating-point
  tolerance.

## Consequences

**Positive**

- One unambiguous definition for .NET, Python, and the solver diagnostics.
- Multi-species systems, polarons, and vacancies no longer depend on implicit conventions.
- The original origin-based behaviour is preserved.

**Negative**

- Time-lagged MSD requires a future, separately named API.
- Per-species output is larger than a single scalar.
- Users must understand which species is being reported.

## Alternatives considered

- **Time-lagged MSD for v1:** better for some analyses, but diverges from the original and needs trajectory
  windows.
- **Total transport only:** simpler, but ambiguous for mixed carriers and polarons.
