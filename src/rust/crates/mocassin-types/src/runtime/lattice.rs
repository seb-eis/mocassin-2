// SPDX-License-Identifier: MIT

//! The validated initial lattice.

use crate::ids::ParticleId;

/// The initial lattice of one job.
#[derive(Debug, Clone, PartialEq, Eq)]
pub struct InitialLattice {
    /// The `lattices.signature` recomputed and verified during mapping.
    pub signature: String,
    /// The supercell size `[a, b, c]`.
    pub lattice_size: [u32; 3],
    /// The number of canonical P1 positions per unit cell.
    pub position_count: u32,
    /// The row-major particle array, length `a × b × c × position_count`.
    pub particles: Vec<ParticleId>,
}

impl InitialLattice {
    /// Returns the number of lattice sites.
    #[must_use]
    pub fn len(&self) -> usize {
        self.particles.len()
    }

    /// Returns `true` when the lattice has no sites.
    #[must_use]
    pub fn is_empty(&self) -> bool {
        self.particles.is_empty()
    }
}
