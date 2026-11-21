// SPDX-License-Identifier: MIT

//! The owner-major neighbour-link CSR and the inverse routing index.
//!
//! Feature 1 defines the layout only: every arena is empty and the phase pipeline does not build links yet.
//! Feature 2 fills the CSRs from the payload and validates them.
//!
//! Orientation (Step 14 design §6.1): a payload pair link on P1 position `O` is stored at its **owner** `O`
//! (the centre whose energy vector contains the pair sum) and points at its partner. The pair table is row-major
//! `[owner particle][partner particle]`, and the inverse index answers "which owners point at this partner".
//! Both CSRs are per P1 position, so neither grows with the supercell.

/// The compact owner-major link CSR plus the inverse routing index.
#[derive(Clone, Debug, Default, PartialEq, Eq)]
pub struct LinkArena {
    /// Owner offsets, length `P + 1`; the owner's forward links are
    /// `links[owner_offsets[owner]..owner_offsets[owner + 1]]`.
    pub owner_offsets: Vec<u32>,
    /// The owner's forward links grouped by ascending owner position.
    pub links: Vec<Link>,
    /// Cluster associations referenced by [`Link::cluster_start`]/[`Link::cluster_len`].
    pub cluster_assoc: Vec<ClusterAssoc>,
    /// Partner offsets, length `P + 1`; the partner's incoming links are
    /// `inverse[inverse_offsets[partner]..inverse_offsets[partner + 1]]`.
    pub inverse_offsets: Vec<u32>,
    /// Owners whose forward link points at this partner position.
    pub inverse: Vec<IncomingLink>,
}

impl LinkArena {
    /// Returns the empty arena of a context whose links are not built yet.
    #[must_use]
    pub fn empty() -> Self {
        Self::default()
    }

    /// Returns `true` when no link or association is stored.
    #[must_use]
    pub fn is_empty(&self) -> bool {
        self.owner_offsets.is_empty()
            && self.links.is_empty()
            && self.cluster_assoc.is_empty()
            && self.inverse_offsets.is_empty()
            && self.inverse.is_empty()
    }

    /// Returns `true` when both CSRs have a consistent shape for `positions` P1 positions.
    #[must_use]
    pub(crate) fn has_valid_shape(&self, positions: u32) -> bool {
        if self.is_empty() {
            return true;
        }

        let expected = positions as usize + 1;
        if self.owner_offsets.len() != expected
            || self.inverse_offsets.len() != expected
            || self.owner_offsets.first() != Some(&0)
            || self.inverse_offsets.first() != Some(&0)
            || self.owner_offsets.last().copied() != Some(self.links.len() as u32)
            || self.inverse_offsets.last().copied() != Some(self.inverse.len() as u32)
            || !self.owner_offsets.windows(2).all(|pair| pair[0] <= pair[1])
            || !self
                .inverse_offsets
                .windows(2)
                .all(|pair| pair[0] <= pair[1])
        {
            return false;
        }

        self.links.iter().all(|link| {
            (link.partner_position as usize) < positions as usize
                && u64::from(link.cluster_start) + u64::from(link.cluster_len)
                    <= self.cluster_assoc.len() as u64
        }) && self
            .inverse
            .iter()
            .all(|incoming| (incoming.owner_position as usize) < positions as usize)
    }
}

/// One owner's forward pair link.
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub struct Link {
    /// The partner P1 position (`< P`).
    pub partner_position: u32,
    /// The partner image relative to the owner's cell.
    pub offset: [i32; 3],
    /// The shared pair-table index.
    pub pair_id: u32,
    /// The first cluster association of this link.
    pub cluster_start: u32,
    /// The number of cluster associations of this link.
    pub cluster_len: u16,
}

/// One owner whose forward link points at a partner position.
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub struct IncomingLink {
    /// The owning P1 position.
    pub owner_position: u32,
    /// The link's index inside the owner's forward list.
    pub link_id: u32,
}

/// One cluster association carried by a forward link.
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub struct ClusterAssoc {
    /// The cluster instance whose centre owns the link.
    pub instance: u32,
    /// The slot the partner occupies in that instance.
    pub slot: u8,
}

#[cfg(test)]
mod tests {
    use super::{ClusterAssoc, IncomingLink, Link, LinkArena};

    #[test]
    fn the_memory_plan_sizes_match_the_documented_layout() {
        assert_eq!(core::mem::size_of::<Link>(), 28);
        assert_eq!(core::mem::size_of::<IncomingLink>(), 8);
        assert_eq!(core::mem::size_of::<ClusterAssoc>(), 8);
    }

    #[test]
    fn an_empty_arena_is_valid() {
        let arena = LinkArena::empty();
        assert!(arena.is_empty());
        assert!(arena.has_valid_shape(4));
    }

    #[test]
    fn an_inconsistent_csr_is_rejected() {
        let arena = LinkArena {
            owner_offsets: vec![0, 1],
            links: vec![Link {
                partner_position: 0,
                offset: [0, 0, 0],
                pair_id: 0,
                cluster_start: 0,
                cluster_len: 0,
            }],
            ..LinkArena::empty()
        };

        assert!(!arena.has_valid_shape(4));
    }
}
