// SPDX-License-Identifier: MIT

//! The applied energy tables of the runtime model.

/// The applied energy values of a model.
#[derive(Debug, Clone, PartialEq)]
pub struct EnergyModel {
    /// The applied pair-energy tables in payload order.
    pub pair_tables: Vec<PairTable>,
    /// The applied cluster-energy tables in payload order.
    pub cluster_tables: Vec<ClusterTable>,
    /// The dense `[position][particle]` site-energy background in eV, row-major.
    pub defect_background: Vec<f64>,
}

/// One applied pair-energy table.
#[derive(Debug, Clone, PartialEq)]
pub struct PairTable {
    /// The shared stable pair-energy identity.
    pub object_id: String,
    /// The `particle_count × particle_count` row-major table indexed by `[center][partner]`.
    pub energies: Vec<f64>,
}

/// One applied cluster-energy table.
#[derive(Debug, Clone, PartialEq)]
pub struct ClusterTable {
    /// The stable cluster-energy identity.
    pub object_id: String,
    /// The canonical occupation codes, strictly ascending.
    pub occupation_codes: Vec<u64>,
    /// The applied energy per occupation code.
    pub energies: Vec<f64>,
    /// One row per allowed centre particle, ascending.
    pub particle_table_mapping: Vec<u8>,
}
