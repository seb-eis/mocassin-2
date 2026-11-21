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
    /// The optional raw-code expansion map.
    ///
    /// This is an implementation detail of [`ClusterTable::lookup`], not the supported semantic API. It is
    /// public because the table has no constructor and is built by struct literal in the workspace.
    #[doc(hidden)]
    pub expansions: Option<ClusterExpansions>,
}

/// The raw slot-tuple code resolution map of one cluster table.
#[derive(Debug, Clone, PartialEq)]
pub struct ClusterExpansions {
    /// Every raw slot-tuple code the writer enumerated, strictly ascending.
    pub expanded_codes: Vec<u64>,
    /// The canonical occupation index for the code at the same position.
    pub canonical_indices: Vec<u32>,
}

/// The result of resolving a raw cluster slot-tuple code.
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum ClusterCodeLookup {
    /// The raw code resolves to a canonical occupation index.
    Canonical(u32),
    /// A map is present but does not contain the raw code; the package is corrupt.
    MissingFromMap(u64),
    /// No map is present and the raw code is not canonical; the package predates the map and must be rebuilt.
    ExpansionMapAbsent(u64),
}

impl ClusterCodeLookup {
    /// Returns the canonical occupation index when the raw code resolves.
    #[must_use]
    pub const fn index(self) -> Option<u32> {
        match self {
            Self::Canonical(index) => Some(index),
            Self::MissingFromMap(_) | Self::ExpansionMapAbsent(_) => None,
        }
    }
}

impl ClusterTable {
    /// Resolves a raw slot-tuple code to its canonical occupation index.
    ///
    /// A present map is authoritative: a code absent from it is [`ClusterCodeLookup::MissingFromMap`]. Without
    /// a map the code must be canonical; otherwise the package predates the expansion map and the lookup
    /// returns [`ClusterCodeLookup::ExpansionMapAbsent`] rather than guessing a neighbour.
    #[must_use]
    pub fn lookup(&self, raw_code: u64) -> ClusterCodeLookup {
        if let Some(expansions) = &self.expansions {
            return match expansions.expanded_codes.binary_search(&raw_code) {
                Ok(position) => {
                    ClusterCodeLookup::Canonical(expansions.canonical_indices[position])
                }
                Err(_) => ClusterCodeLookup::MissingFromMap(raw_code),
            };
        }

        match self.occupation_codes.binary_search(&raw_code) {
            Ok(position) => {
                ClusterCodeLookup::Canonical(u32::try_from(position).unwrap_or(u32::MAX))
            }
            Err(_) => ClusterCodeLookup::ExpansionMapAbsent(raw_code),
        }
    }
}
