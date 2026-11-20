// SPDX-License-Identifier: MIT

//! The compiled-model payload and its particle/structure sections.

use serde::{Deserialize, Serialize};

use super::energy::EnergyPayload;
use super::simulation::SimulationPayload;
use super::transition::TransitionPayload;

/// The `.msl` compiled-model payload.
///
/// `signature` is the Step 09 parameterization signature and the `models.signature` key; `model_signature` is
/// the Step 08 compiled-model identity string. `format_version` is the payload-encoding version, independent of
/// the package schema version.
#[derive(Debug, Clone, PartialEq, Deserialize, Serialize)]
#[serde(rename_all = "snake_case")]
pub struct ModelPayload {
    /// The payload-encoding version.
    pub format_version: u32,
    /// The Step 09 parameterization signature.
    pub signature: String,
    /// The Step 08 compiled-model signature (an identity string).
    pub model_signature: String,
    /// The symmetry contract version.
    pub symmetry_contract_version: String,
    /// The space-group catalog data version.
    pub space_group_data_version: String,
    /// The SHA-256 of the canonical space-group catalog payload.
    pub space_group_data_sha256: String,
    /// The compiled particle table.
    pub particle: ParticlePayload,
    /// The compiled structure.
    pub structure: StructurePayload,
    /// The applied energy values.
    pub energy: EnergyPayload,
    /// The compiled transitions.
    pub transitions: TransitionPayload,
    /// The compiled simulation encodings, in compiled order.
    pub simulations: Vec<SimulationPayload>,
}

/// The compiled particle table. Index zero is void.
#[derive(Debug, Clone, PartialEq, Deserialize, Serialize)]
#[serde(rename_all = "snake_case")]
pub struct ParticlePayload {
    /// Particle charges in elementary-charge units, indexed by compiled particle index.
    pub charges: Vec<f64>,
    /// Particle symbols, indexed by compiled particle index.
    pub symbols: Vec<String>,
    /// Whether each compiled particle is a vacancy.
    pub is_vacancy: Vec<bool>,
    /// The compiled particle count, including void.
    pub count: u32,
}

/// The compiled structure section.
#[derive(Debug, Clone, PartialEq, Deserialize, Serialize)]
#[serde(rename_all = "snake_case")]
pub struct StructurePayload {
    /// The Cartesian `a`, `b`, and `c` cell vectors in ångström.
    pub cell_vectors_ang: [[f64; 3]; 3],
    /// The canonical P1 positions in ascending `P` order.
    pub positions: Vec<PositionPayload>,
    /// The number of canonical P1 positions per unit cell.
    pub position_count: u32,
    /// The number of user reference sites.
    pub site_count: u32,
    /// The interaction environments indexed by compiled environment index.
    pub environments: Vec<EnvironmentPayload>,
    /// The MMC local-update cube `[maxCellA, maxCellB, maxCellC, 0]`.
    pub interaction_range: [i32; 4],
    /// `position_count × (particle_count − 1)`; void has no static tracker.
    pub static_trackers_per_cell: u32,
    /// The number of KMC global tracker definitions.
    pub global_tracker_count: u32,
}

/// One canonical P1 position.
#[derive(Debug, Clone, PartialEq, Deserialize, Serialize)]
#[serde(rename_all = "snake_case")]
pub struct PositionPayload {
    /// The full encoded position `[cellA, cellB, cellC, P]`; base positions are `[0, 0, 0, P]`.
    pub vector4: [i32; 4],
    /// The owning reference-site index; not the `P` index.
    pub site_index: u32,
    /// Whether the position belongs to a stable reference site.
    pub stable: bool,
    /// The ascending compiled particle indices in the allowed-particle mask.
    pub particle_ids: Vec<u8>,
}

/// The interaction environment of one compiled environment index.
#[derive(Debug, Clone, PartialEq, Deserialize, Serialize)]
#[serde(rename_all = "snake_case")]
pub struct EnvironmentPayload {
    /// The selectable compiled-particle mask.
    pub selection_particle_mask: u64,
    /// The periodic pair images linked from positions in this environment.
    pub pairs: Vec<PairLinkPayload>,
    /// The cluster links used for incremental occupation-code updates.
    pub clusters: Vec<ClusterLinkPayload>,
    /// The compiled particle indices updated through this environment.
    pub update_particle_ids: Vec<u8>,
}

/// One periodic partner image linked from a position.
#[derive(Debug, Clone, PartialEq, Deserialize, Serialize)]
#[serde(rename_all = "snake_case")]
pub struct PairLinkPayload {
    /// The partner canonical P1 index.
    pub target_position: u32,
    /// The relative cell offset `[cellA, cellB, cellC]` from the centre position.
    pub offset: [i32; 3],
    /// The shared pair-table index.
    pub energy_id: u32,
}

/// One cluster-table link from a position.
#[derive(Debug, Clone, PartialEq, Deserialize, Serialize)]
#[serde(rename_all = "snake_case")]
pub struct ClusterLinkPayload {
    /// The shared cluster-table index.
    pub cluster_id: u32,
    /// The position's site slot in the occupation code; zero is the centre.
    pub byte_position: u8,
}
