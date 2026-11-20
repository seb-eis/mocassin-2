// SPDX-License-Identifier: MIT

//! The selectable `(position, particle)` direction pool container.
//!
//! Step 13 defines the layout and the invariants that Step 14 relies on; Step 14 populates the pool from the
//! selection mask, applies the selection optimizers, and performs the swap-remove updates. No Step 13 reader
//! constructs a non-empty pool.

use crate::ids::{ParticleId, PositionId};

/// A deterministic pool of selectable `(position, particle)` combinations with the same direction count.
///
/// Entries are inserted in ascending position/particle order. Step 14 removes an entry with `swap_remove`,
/// which moves the last member into the freed slot; `slot_of` supplies the O(1) lookup needed to keep that
/// member's slot index current. The layout is deliberately flat and contains no `HashMap`.
#[derive(Debug, Clone, Default, PartialEq, Eq)]
pub struct DirectionPool {
    /// The number of jump directions shared by every member of this pool.
    pub direction_count: u32,
    /// The selectable `(position, particle)` members in deterministic insertion order.
    pub members: Vec<(PositionId, ParticleId)>,
    /// Caller-indexed `(position, particle)` key to member-slot map; `u32::MAX` means "not in the pool".
    ///
    /// The key encoding is a Step 14 selection concern; the container only stores the map so a swap-remove can
    /// update it without a linear scan.
    pub slot_of: Vec<u32>,
}

impl DirectionPool {
    /// Returns the number of selectable members.
    #[must_use]
    pub fn len(&self) -> usize {
        self.members.len()
    }

    /// Returns `true` when the pool has no selectable members.
    #[must_use]
    pub fn is_empty(&self) -> bool {
        self.members.is_empty()
    }
}

#[cfg(test)]
mod tests {
    use super::DirectionPool;
    use crate::ids::{ParticleId, PositionId};

    #[test]
    fn an_empty_pool_is_empty() {
        let pool = DirectionPool::default();
        assert_eq!(pool.len(), 0);
        assert!(pool.is_empty());
        assert_eq!(pool.direction_count, 0);
    }

    #[test]
    fn the_member_count_tracks_the_member_vector() {
        let pool = DirectionPool {
            direction_count: 2,
            members: vec![
                (PositionId::new(0), ParticleId::new(1)),
                (PositionId::new(3), ParticleId::new(2)),
            ],
            slot_of: vec![0, u32::MAX, u32::MAX, 1],
        };
        assert_eq!(pool.len(), 2);
        assert!(!pool.is_empty());
        assert_eq!(pool.members[1].0.get(), 3);
    }
}
