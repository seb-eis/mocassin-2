// SPDX-License-Identifier: MIT

//! Shared vocabulary for the MOCASSIN solver and package reader.
//!
//! This crate owns the typed identities, read limits, job setting/flag types, and the solver exit-code model
//! that `mocassin-msl`, `mocassin-rng`, the future simulation engine, and `mocassin-sim` agree on. It has no
//! I/O, SQLite, Zstd, tracing, or CLI dependency (Step 13 design §1); the reader and CLI map their errors onto
//! the [`ExitCode`] values defined here.
//!
//! The Step 13 feature 1 slice provides the dense typed IDs, [`ReaderLimits`], the settings and composed job
//! flag types, and the canonical solver exit codes 0/5/10–16. Payload DTOs and the validated runtime model are
//! added by the reader/mapping features that decode and check them.

#![forbid(unsafe_op_in_unsafe_fn)]

pub mod exit;
pub mod ids;
pub mod limits;
pub mod settings;

pub use exit::{ErrorClass, ExitCode, UnknownExitCode};
pub use ids::{
    CellIndex, JumpCollectionId, JumpDirectionId, ParticleId, PositionId, RuleId, StateCode,
};
pub use limits::ReaderLimits;
pub use settings::{
    JobSettings, SimulationKind, SimulationSettings, SolverJobFlags, SolverJobFlagsError,
    UnknownSimulationKind,
};
