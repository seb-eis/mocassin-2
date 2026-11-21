// SPDX-License-Identifier: MIT

//! The cluster runtime: instances, geometry, occupation codes, and per-centre energies.
//!
//! Feature 1 defines the flat layout only; every array is empty until feature 2 validates the geometry and
//! feature 3 fills the energies.
//!
//! A cluster table can have more than one centre: the writer emits slot `0` for every P1 position of the centre
//! site, so the runtime keeps **one instance per slot-0 link** (Step 14 design §6.2/§6.3). Geometry is a flat
//! CSR (`slot_start` + `slot_positions`/`slot_cells`), never a `Vec` per instance; the centre index is a CSR
//! over P1 positions so Step 15 can read every cluster term of a path node without a supercell scan.

/// The cluster instances and their derived state.
#[derive(Clone, Debug, Default, PartialEq)]
pub struct ClusterArena {
    /// One instance per slot-0 link.
    pub instances: Vec<ClusterInstance>,
    /// Centre offsets, length `P + 1`; the instances centred on a position are
    /// `centres[centre_offsets[position]..centre_offsets[position + 1]]`.
    pub centre_offsets: Vec<u32>,
    /// Instance ids grouped by ascending centre P1 position.
    pub centres: Vec<u32>,
    /// Slot offsets, length `K + 1`; the instance's slots are
    /// `slot_start[instance]..slot_start[instance + 1]`.
    pub slot_start: Vec<u32>,
    /// P1 position of each instance slot; slot 0 is the centre.
    pub slot_positions: Vec<u32>,
    /// Relative cell offset of each instance slot; slot 0 is `[0, 0, 0]`.
    pub slot_cells: Vec<[i32; 3]>,
    /// Raw occupation code per `instance * A·B·C + cell`.
    pub codes: Vec<u64>,
    /// Canonical occupation index per `instance * A·B·C + cell`.
    pub code_ids: Vec<u32>,
    /// Energy-row prefix per instance, length `K + 1`.
    pub energy_start: Vec<u32>,
    /// Per-instance cluster energy in kT, indexed `cell * Σ mapping_len + energy_start[i] + row`.
    pub energy: Vec<f64>,
}

impl ClusterArena {
    /// Returns the empty arena of a context whose clusters are not built yet.
    #[must_use]
    pub fn empty() -> Self {
        Self::default()
    }

    /// Returns `true` when no instance or cluster-derived array is stored.
    #[must_use]
    pub fn is_empty(&self) -> bool {
        self.instances.is_empty()
            && self.centre_offsets.is_empty()
            && self.centres.is_empty()
            && self.slot_start.is_empty()
            && self.slot_positions.is_empty()
            && self.slot_cells.is_empty()
            && self.codes.is_empty()
            && self.code_ids.is_empty()
            && self.energy_start.is_empty()
            && self.energy.is_empty()
    }

    /// Returns `true` when the flat cluster arrays have a consistent shape.
    #[must_use]
    pub(crate) fn has_valid_shape(&self, dimensions: &super::Dimensions) -> bool {
        if self.is_empty() {
            return true;
        }

        let instances = self.instances.len();
        let cells = u64::from(dimensions.cell_count());
        let slots = self.slot_positions.len();
        let energy_rows = u64::from(self.energy_start.last().copied().unwrap_or(0));

        self.centre_offsets.len() == dimensions.p() as usize + 1
            && self.centre_offsets.first() == Some(&0)
            && self.centre_offsets.last().copied() == Some(self.centres.len() as u32)
            && self
                .centre_offsets
                .windows(2)
                .all(|pair| pair[0] <= pair[1])
            && self.centres.iter().all(|id| (*id as usize) < instances)
            && self.slot_start.len() == instances + 1
            && self.slot_start.first() == Some(&0)
            && self.slot_start.last().copied() == Some(slots as u32)
            && self.slot_start.windows(2).all(|pair| pair[0] <= pair[1])
            && self.slot_cells.len() == slots
            && self.codes.len() as u64 == instances as u64 * cells
            && self.code_ids.len() == self.codes.len()
            && self.energy_start.len() == instances + 1
            && self.energy_start.first() == Some(&0)
            && self.energy_start.windows(2).all(|pair| pair[0] <= pair[1])
            && self.energy.len() as u64 == cells * energy_rows
            && self.instances.iter().enumerate().all(|(index, instance)| {
                let start = self.slot_start[index] as u64;
                let end = self.slot_start[index + 1] as u64;
                u64::from(instance.centre_position) < u64::from(dimensions.p())
                    && instance.slot_count != 0
                    && start < slots as u64
                    && end <= slots as u64
                    && end - start == u64::from(instance.slot_count)
                    && u64::from(instance.slot_start) == start
            })
            && self
                .slot_positions
                .iter()
                .all(|position| *position < dimensions.p())
    }
}

/// One cluster instance, one per slot-0 link.
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub struct ClusterInstance {
    /// The shared cluster-table index.
    pub table: u32,
    /// The centre P1 position.
    pub centre_position: u32,
    /// The first slot of this instance in the flat slot arrays.
    pub slot_start: u32,
    /// The number of slots, including the centre slot 0.
    pub slot_count: u8,
}

#[cfg(test)]
mod tests {
    use super::{ClusterArena, ClusterInstance};
    use crate::context::Dimensions;

    fn dimensions() -> Dimensions {
        Dimensions::new([2, 2, 2], 4, 5).expect("valid test dimensions")
    }

    #[test]
    fn an_instance_is_sixteen_bytes() {
        assert_eq!(core::mem::size_of::<ClusterInstance>(), 16);
    }

    #[test]
    fn an_empty_arena_is_valid() {
        let arena = ClusterArena::empty();
        assert!(arena.is_empty());
        assert!(arena.has_valid_shape(&dimensions()));
    }

    #[test]
    fn a_half_built_arena_is_rejected() {
        let arena = ClusterArena {
            instances: vec![ClusterInstance {
                table: 0,
                centre_position: 0,
                slot_start: 0,
                slot_count: 2,
            }],
            ..ClusterArena::empty()
        };
        assert!(!arena.has_valid_shape(&dimensions()));
    }
}
