// SPDX-License-Identifier: MIT

//! Monte Carlo solver core for MOCASSIN simulation packages.
//!
//! The crate is built around one [`context::SimulationContext`] per job: a deterministic, allocation-bounded
//! runtime state built once from a validated [`mocassin_types::JobPackage`]. The context owns the lattice and
//! environment arenas, the neighbour-link and cluster runtimes, the energy tables, the selection pools, the KMC
//! jump status, the tracker index space, the counters, and the goals/normalization state. Steps 15–18 consume it;
//! this crate never reads a package, never writes a package, and never opens SQLite.
//!
//! Step 14 builds the context in six features. Feature 1 (this commit) establishes the module layout, the
//! dimension/index contract, the context limits and memory plan, the lattice/flag/mobile-tracker arenas, the
//! counter allocation, the phase pipeline with cancellation, and the documented error/exit mapping. The
//! link/cluster, energy, pool/jump-status, goal/normalization, and fixture work lands in features 2–6.

#![forbid(unsafe_op_in_unsafe_fn)]

pub mod context;
