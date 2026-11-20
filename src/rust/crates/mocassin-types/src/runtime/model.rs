// SPDX-License-Identifier: MIT

//! The validated compiled/parameterized model.

use crate::ids::PositionId;

use crate::runtime::energy::EnergyModel;
use crate::runtime::simulation::SimulationModel;
use crate::runtime::transition::TransitionModel;

/// The fully validated parameterized model shared by every job of a package.
///
/// `signature` is the Step 09 parameterization signature and `model_signature` the Step 08 compiled-model
/// identity string. Both were recomputed from the payload during mapping.
#[derive(Debug, Clone, PartialEq)]
pub struct ParameterizedModel {
    /// The payload-encoding version; always 1 in v1.
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
    pub particle: ParticleTable,
    /// The compiled structure.
    pub structure: StructureModel,
    /// The applied energy values.
    pub energy: EnergyModel,
    /// The compiled transitions.
    pub transitions: TransitionModel,
    /// The compiled simulations.
    pub simulations: Vec<SimulationModel>,
    /// Derived counts used by the dry-run summary.
    pub counts: ModelCounts,
}

/// The compiled particle table. Index zero is void.
#[derive(Debug, Clone, PartialEq)]
pub struct ParticleTable {
    /// Particle charges in elementary-charge units, indexed by compiled index.
    pub charges: Vec<f64>,
    /// Particle symbols, indexed by compiled index.
    pub symbols: Vec<String>,
    /// Whether each compiled particle is a vacancy.
    pub is_vacancy: Vec<bool>,
}

/// The compiled structure, including the per-image environment links.
#[derive(Debug, Clone, PartialEq)]
pub struct StructureModel {
    /// The Cartesian `a`, `b`, and `c` cell vectors in ångström.
    pub cell_vectors_ang: [[f64; 3]; 3],
    /// The canonical P1 positions in ascending `P` order.
    pub positions: Vec<Position>,
    /// The number of user reference sites.
    pub site_count: u32,
    /// The interaction environments, indexed by `PositionId`.
    pub environments: Vec<EnvironmentModel>,
    /// The MMC local-update cube `[maxCellA, maxCellB, maxCellC, 0]`.
    pub interaction_range: [i32; 4],
    /// `position_count × (particle_count − 1)`.
    pub static_trackers_per_cell: u32,
    /// The number of KMC global tracker definitions.
    pub global_tracker_count: u32,
}

/// One canonical P1 position.
#[derive(Debug, Clone, PartialEq, Eq)]
pub struct Position {
    /// The encoded position `[cellA, cellB, cellC, P]`; base positions are `[0, 0, 0, P]`.
    pub vector4: [i32; 4],
    /// The owning reference-site index.
    pub site_index: u32,
    /// Whether the position belongs to a stable reference site.
    pub stable: bool,
    /// The allowed-particle mask reconstructed from the payload's ascending particle ids.
    pub allowed_mask: u64,
}

/// The interaction environment of one position.
#[derive(Debug, Clone, PartialEq, Eq)]
pub struct EnvironmentModel {
    /// The selectable compiled-particle mask.
    pub selection_particle_mask: u64,
    /// The periodic pair images linked from this position.
    pub pairs: Vec<PairLink>,
    /// The cluster links from this position.
    pub clusters: Vec<ClusterLink>,
    /// The compiled particle indices updated through this environment, ascending.
    pub update_particle_ids: Vec<u8>,
}

/// One periodic partner image linked from a position.
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub struct PairLink {
    /// The partner canonical P1 position.
    pub target_position: PositionId,
    /// The relative cell offset `[cellA, cellB, cellC]`.
    pub offset: [i32; 3],
    /// The shared pair-table index (payload key `energy_id`).
    pub pair_id: u32,
}

/// One cluster-table link from a position.
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub struct ClusterLink {
    /// The shared cluster-table index.
    pub cluster_id: u32,
    /// The position's site slot in the occupation code; zero is the centre.
    pub byte_position: u8,
}

/// Derived model counts for the dry-run summary.
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub struct ModelCounts {
    /// Number of compiled particles, including void.
    pub particle_count: u32,
    /// Number of canonical P1 positions.
    pub position_count: u32,
    /// Number of user reference sites.
    pub site_count: u32,
    /// Number of environments (one per position).
    pub environment_count: u32,
    /// Number of pair-energy tables.
    pub pair_table_count: u32,
    /// Number of cluster-energy tables.
    pub cluster_table_count: u32,
    /// Number of jump collections.
    pub collection_count: u32,
    /// Number of jump directions.
    pub direction_count: u32,
    /// Number of rules.
    pub rule_count: u32,
    /// Number of simulations.
    pub simulation_count: u32,
    /// Number of global trackers.
    pub global_tracker_count: u32,
    /// Number of static trackers per cell.
    pub static_trackers_per_cell: u32,
}
