# ADR 0005 — Statistical Parity and RNG Policy

- **Status:** Accepted
- **Date:** 2026-11-02
- **Deciders:** Project owner

## Context

The original solver uses PCG32 with a 64-bit state and increment, seeded directly from the job database. Its
trajectories depend on:

- the exact PCG32 draw order,
- the order of direction pools and environment links,
- floating-point operation order and `libm` implementation,
- x86-64 struct layout and compiler flags.

Bit-exact reproduction would force the Rust solver to preserve all of these, including unsafe layouts and
platform-specific floating-point behavior. The project owner requires **statistical parity**, not identical
trajectories.

## Decision

- Use **ChaCha12** (RustCrypto `rand_chacha`) as the solver PRNG, seeded from a 256-bit value.
- Derive each job seed deterministically with **BLAKE3** over
  `(master_seed, collection_index, config_index, job_index, purpose)`.
- Use separate streams for lattice generation and simulation, so changing simulation settings does not
  change the generated lattice.
- Parallelise **across jobs**, never within a Monte Carlo trajectory.
- Validate against the original solver by comparing observables within statistical error, not by comparing
  individual states.

## Consequences

**Positive**

- Idiomatic, portable, high-quality RNG.
- Reproducible results on all platforms for the same solver version and package.
- Freedom to choose data layouts and iteration orders for performance.
- Job-level parallelism without changing results.

**Negative**

- Cannot diff a new trajectory against an old one to find a physics bug; validation must be statistical and
  test-based.
- Existing seeds do not reproduce old results.
- Statistical tests require more compute and careful tolerance design.

## Validation policy

For reference systems, compare:

- mean energy, acceptance/rejection/skip/blocked fractions;
- conductivity, mobility, diffusion, correlation factors;
- jump-barrier histograms and occupation statistics.

Acceptance criterion: difference within `max(3σ, 2 %)` for intensive quantities and `max(3σ, 5 %)` for
transport coefficients, plus a distribution test (KS with an effect-size threshold). Exact reference values,
seeds, and sample sizes are stored in `tests/reference/README.md`.

## Determinism guarantees

- Same package + same solver binary + same platform + same seed ⇒ identical `.mcs`.
- Cross-platform: same seed ⇒ identical trajectory unless the platform's floating-point behavior differs;
  the CI checks statistical equivalence rather than byte equality.
- Package builds are deterministic: same project + same builder version ⇒ byte-identical `.msl` model
  payloads (SQLite page layout may differ; payload hashes must match).
