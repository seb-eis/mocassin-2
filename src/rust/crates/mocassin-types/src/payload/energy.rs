// SPDX-License-Identifier: MIT

//! The applied energy section of the model payload.

use serde::{Deserialize, Serialize};

/// The applied energy values of a model payload.
#[derive(Debug, Clone, PartialEq, Deserialize, Serialize)]
#[serde(rename_all = "snake_case")]
pub struct EnergyPayload {
    /// The applied pair-energy tables in ascending compiled table order.
    pub pair_tables: Vec<PairTablePayload>,
    /// The applied cluster-energy tables in ascending compiled table order.
    pub cluster_tables: Vec<ClusterTablePayload>,
    /// The dense `[position][particle]` site-energy background in eV, row-major.
    pub defect_background: Vec<f64>,
}

/// One applied pair-energy table.
#[derive(Debug, Clone, PartialEq, Deserialize, Serialize)]
#[serde(rename_all = "snake_case")]
pub struct PairTablePayload {
    /// The shared stable pair-energy identity.
    pub object_id: String,
    /// The `particle_count × particle_count` row-major table indexed by `[center][partner]`.
    pub energies: Vec<f64>,
}

/// One applied cluster-energy table.
#[derive(Debug, Clone, PartialEq, Deserialize, Serialize)]
#[serde(rename_all = "snake_case")]
pub struct ClusterTablePayload {
    /// The stable cluster-energy identity.
    pub object_id: String,
    /// The canonical occupation codes, sorted ascending.
    pub occupation_codes: Vec<u64>,
    /// The optional raw slot-tuple code resolution map.
    #[serde(default)]
    pub code_expansions: Option<CodeExpansionPayload>,
    /// The applied energy per occupation code.
    pub energies: Vec<f64>,
    /// One row per allowed centre particle in ascending compiled particle index.
    pub particle_table_mapping: Vec<u8>,
}

/// The raw slot-tuple code resolution map of one cluster table.
#[derive(Debug, Clone, PartialEq, Deserialize, Serialize)]
#[serde(rename_all = "snake_case")]
pub struct CodeExpansionPayload {
    /// Every raw slot-tuple code the writer enumerated, strictly ascending.
    pub expanded_codes: Vec<u64>,
    /// The canonical occupation index for the code at the same position.
    pub canonical_indices: Vec<u32>,
}
