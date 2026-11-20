// SPDX-License-Identifier: MIT

//! The validated simulation encodings of the runtime model.

use crate::settings::{SimulationKind, SimulationSettings};

/// One compiled simulation.
#[derive(Debug, Clone, PartialEq)]
pub struct SimulationModel {
    /// The stable project simulation identifier.
    pub id: String,
    /// The simulation kind.
    pub kind: SimulationKind,
    /// The active compiled collection indices.
    pub jump_collection_ids: Vec<u32>,
    /// The compiled runtime-default settings.
    pub default_settings: SimulationSettings,
    /// The tracker index lists used by this simulation.
    pub tracker_models: TrackerModels,
}

/// The global and static tracker index lists of one simulation.
#[derive(Debug, Clone, PartialEq, Eq)]
pub struct TrackerModels {
    /// The global tracker indices referenced by the simulation.
    pub global_tracker_indices: Vec<u32>,
    /// The static tracker indices referenced by the simulation.
    pub static_tracker_indices: Vec<u32>,
}
