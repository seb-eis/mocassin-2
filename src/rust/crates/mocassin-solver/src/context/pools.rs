// SPDX-License-Identifier: MIT

//! The KMC selection pools.
//!
//! Feature 1 defines the layout only; the pools are empty until feature 4 builds them. A pool member is an
//! **absolute supercell environment** plus a particle (`EnvId`), not a P1 position: the KMC loop selects a
//! supercell environment, while the direction count and the selection mask are P1-level tables. The Step 13
//! `mocassin_types::DirectionPool` placeholder is not used by the context and is retired in feature 4.

use super::dimensions::EnvId;
use mocassin_types::ParticleId;

/// The KMC selection pools of one context.
#[derive(Clone, Debug, Default, PartialEq, Eq)]
pub struct SelectionPools {
    /// One pool per distinct selectable direction count, ascending.
    pub pools: Vec<SelectionPool>,
    /// `E·pc` global slot map; `slot[env·pc + particle]` is the member slot, or `u32::MAX`.
    pub slot: Vec<u32>,
    /// `P·pc` P1 table; `pool_of[P·pc + particle]` is the pool id, or `u32::MAX`.
    pub pool_of: Vec<u32>,
    /// `Σ pool.members.len() × pool.direction_count`.
    pub selectable_jump_count: u64,
}

impl SelectionPools {
    /// Returns the empty pools of a context whose selection space is not built yet.
    #[must_use]
    pub fn empty() -> Self {
        Self::default()
    }

    /// Returns `true` when no pool member or slot map is stored.
    #[must_use]
    pub fn is_empty(&self) -> bool {
        self.pools.is_empty()
            && self.slot.is_empty()
            && self.pool_of.is_empty()
            && self.selectable_jump_count == 0
    }

    /// Returns `true` when the pools and slot maps have a consistent shape.
    #[must_use]
    pub(crate) fn has_valid_shape(&self, dimensions: &super::Dimensions) -> bool {
        if self.pools.is_empty() {
            return self.slot.is_empty()
                && self.pool_of.is_empty()
                && self.selectable_jump_count == 0;
        }

        let environments = dimensions.environment_count() as usize;
        let particles = dimensions.pc() as usize;
        let positions = dimensions.p() as usize;

        self.slot.len() == environments * particles
            && self.pool_of.len() == positions * particles
            && self
                .pools
                .windows(2)
                .all(|pair| pair[0].direction_count <= pair[1].direction_count)
            && self.pools.iter().all(|pool| {
                pool.direction_count != 0
                    && pool.members.iter().all(|member| {
                        (member.env.get() as usize) < environments
                            && usize::from(member.particle.get()) < particles
                    })
            })
    }
}

/// One direction-count bucket.
#[derive(Clone, Debug, Default, PartialEq, Eq)]
pub struct SelectionPool {
    /// The number of selectable jump directions shared by every member.
    pub direction_count: u32,
    /// The selectable `(environment, particle)` members in deterministic order.
    pub members: Vec<PoolMember>,
}

/// One selectable environment/particle combination.
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub struct PoolMember {
    /// The absolute supercell environment.
    pub env: EnvId,
    /// The selectable particle.
    pub particle: ParticleId,
}

#[cfg(test)]
mod tests {
    use mocassin_types::ParticleId;

    use super::{PoolMember, SelectionPools};
    use crate::context::Dimensions;

    fn dimensions() -> Dimensions {
        Dimensions::new([2, 2, 2], 4, 5).expect("valid test dimensions")
    }

    #[test]
    fn a_pool_member_is_eight_bytes() {
        assert_eq!(core::mem::size_of::<PoolMember>(), 8);
    }

    #[test]
    fn empty_pools_are_valid() {
        let pools = SelectionPools::empty();
        assert!(pools.is_empty());
        assert!(pools.has_valid_shape(&dimensions()));
    }

    #[test]
    fn a_member_without_a_slot_map_is_rejected() {
        let pools = SelectionPools {
            pools: vec![super::SelectionPool {
                direction_count: 2,
                members: vec![PoolMember {
                    env: crate::context::EnvId::new(0),
                    particle: ParticleId::new(1),
                }],
            }],
            ..SelectionPools::empty()
        };
        assert!(!pools.has_valid_shape(&dimensions()));
    }
}
