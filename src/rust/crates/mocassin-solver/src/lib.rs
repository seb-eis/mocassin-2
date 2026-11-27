// SPDX-License-Identifier: MIT

//! Monte Carlo solver core for MOCASSIN simulation packages.
//!
//! The crate is built around one [`context::SimulationContext`] per job: a deterministic, allocation-bounded
//! runtime state built once from a validated [`mocassin_types::JobPackage`]. The context owns the lattice and
//! environment arenas, the neighbour-link and cluster runtimes, the energy tables, the selection pools, the KMC
//! jump status, the tracker index space, the counters, and the goals/normalization state. Steps 15–18 consume it;
//! this crate never reads a package, never writes a package, and never opens SQLite.
//!
//! Step 14 builds the context. Step 15 feature 1 adds the read-only transition-energy evaluator and the
//! acceptance-exponential kernel; state application, MMC exchange, benchmarks, and closeout follow in later
//! Step 15 features.

#![forbid(unsafe_op_in_unsafe_fn)]

pub mod context;
pub mod energy;
