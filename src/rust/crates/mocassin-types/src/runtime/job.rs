// SPDX-License-Identifier: MIT

//! The validated job package passed to the solver engine.

use crate::ids::{ParticleId, PositionId};
use crate::runtime::lattice::InitialLattice;
use crate::runtime::model::ParameterizedModel;
use crate::settings::{JobSettings, SimulationKind};

/// One validated job: the shared model, the initial lattice, and the resolved settings.
#[derive(Debug, Clone, PartialEq)]
pub struct JobPackage {
    /// The 1-based job id.
    pub job_id: u32,
    /// The job kind.
    pub kind: SimulationKind,
    /// The shared validated model.
    pub model: std::sync::Arc<ParameterizedModel>,
    /// The initial lattice.
    pub lattice: InitialLattice,
    /// The resolved job settings.
    pub settings: JobSettings,
    /// The effective seed; `--seed` overrides are process-local and applied by the CLI.
    pub seed: [u8; 32],
    /// The KMC start options removed from the selection pool.
    pub optimizers: Vec<SelectionOptimizer>,
    /// The effective extension entries, sorted by id.
    pub extensions: Vec<ExtensionConfig>,
    /// The optional advanced instruction.
    pub instruction: Option<String>,
    /// The canonical doping summary.
    pub doping_info: String,
}

/// One compiled KMC selection optimizer entry.
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub struct SelectionOptimizer {
    /// The canonical P1 position index.
    pub position_id: PositionId,
    /// The compiled particle index removed from the start pool.
    pub particle_id: ParticleId,
}

/// One opaque solver-extension configuration.
#[derive(Debug, Clone, PartialEq, Eq)]
pub struct ExtensionConfig {
    /// The stable extension identifier.
    pub id: String,
    /// The canonical JSON text of the authored configuration.
    pub config_json: String,
}
