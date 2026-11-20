// SPDX-License-Identifier: MIT

//! Validated runtime model, job, and lattice types.
//!
//! These types are the Step 14 input. They are not a second persisted schema: `mocassin-msl` maps the decoded
//! `.msl` payloads onto them, validates every index, length, finite value, kind, flag, and scientific
//! invariant, and verifies both stored signatures before it hands a [`JobPackage`] to the engine.

mod direction_pool;
mod energy;
mod job;
mod jump_status;
mod lattice;
mod model;
mod simulation;
mod transition;

pub use direction_pool::DirectionPool;
pub use energy::{ClusterTable, EnergyModel, PairTable};
pub use job::{ExtensionConfig, JobPackage, SelectionOptimizer};
pub use jump_status::{JumpLink, JumpStatus};
pub use lattice::InitialLattice;
pub use model::{
    ClusterLink, EnvironmentModel, ModelCounts, PairLink, ParameterizedModel, ParticleTable,
    Position, StructureModel,
};
pub use simulation::{SimulationModel, TrackerModels};
pub use transition::{JumpCollection, JumpDirection, JumpRule, TransitionModel};
