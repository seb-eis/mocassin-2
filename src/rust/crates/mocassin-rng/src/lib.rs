// SPDX-License-Identifier: MIT

//! Deterministic random-number generation for the MOCASSIN solver.
//!
//! ADR 0005 fixes the algorithm: ChaCha12 (`rand_chacha`), SHA-256 seed derivation, exactly resumable 64-byte
//! state, and job-level parallelism only. This crate is the only place that names `rand_chacha` state APIs; the
//! [`RngStateV1`] bytes are a first-party format, not a third-party memory layout.
//!
//! - [`derive_job_seed`] and [`derive_lattice_seed`] reproduce the Step 10 build-time derivation for
//!   verification; a normal run uses the package's `jobs.seed` and never re-derives it silently.
//! - [`Rng`] provides the fixed draw semantics (`next_u32`, `next_u64`, `next_f64`, `next_bounded`, `fork`).
//! - [`RngStateV1`] is the `"chacha12-v1"` resume blob written to `.mcs` `rngState`.
//! - [`seed_hash`] is the only seed value that may appear in a summary or log.

#![forbid(unsafe_op_in_unsafe_fn)]

pub mod seed;
pub mod state;
pub mod stream;

pub use seed::{
    JOB_SEED_DOMAIN, LATTICE_SEED_DOMAIN, SEED_BYTES, derive_job_seed, derive_lattice_seed,
    seed_hash,
};
pub use state::{RNG_STATE_BYTES, RNG_STATE_MAGIC, RNG_STATE_VERSION, RngStateError, RngStateV1};
pub use stream::{InvalidForkDomain, MAX_FORK_DOMAIN_BYTES, RNG_ALGORITHM, RNG_FORK_DOMAIN, Rng};
