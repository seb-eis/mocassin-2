// SPDX-License-Identifier: MIT

//! The owner-major neighbour-link CSR and the inverse routing index.
//!
//! A payload pair link on canonical P1 position `O` is stored at its **owner** `O` (the centre whose energy
//! vector contains the pair sum) and points at its partner. The pair table is row-major
//! `[owner particle][partner particle]`, and the inverse index answers "which owners point at this partner".
//! Both CSRs are per P1 position, so neither grows with the supercell (Step 14 design §6.1).
//!
//! The forward CSR is sorted by `pair_id`, then `partner_position`, then `offset` lexicographically; the sort
//! is stable, so exact duplicate images keep payload order. The inverse index follows the forward order within
//! each partner position. Both orders are deterministic and fix the f64 summation order of Step 15.
//!
//! A forward link can carry `ClusterAssoc` entries: "the owner is the centre of cluster instance `instance`,
//! and this link's partner occupies slot `slot`". The cluster phase (see [`super::clusters`]) appends them and
//! sets [`Link::cluster_start`]/[`Link::cluster_len`]; the pair phase leaves those fields zero.

use mocassin_types::{JobPackage, PositionId};

use super::CancelProbe;
use super::dimensions::{Dimensions, EnvId};
use super::error::ContextError;
use super::limits::ContextLimits;

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

    /// Builds the owner-major CSR and the inverse routing index from the payload pair links.
    ///
    /// # Errors
    ///
    /// Returns [`ContextError::InvalidInput`] for an out-of-range partner position, pair table, or per-owner
    /// link count, and [`ContextError::Cancelled`] when the caller's flag is set.
    pub(crate) fn build(
        job: &JobPackage,
        dimensions: &Dimensions,
        limits: &ContextLimits,
        cancel: &CancelProbe<'_>,
    ) -> Result<Self, ContextError> {
        let positions = dimensions.p() as usize;
        let pair_tables = job.model.energy.pair_tables.len();

        let mut owner_offsets = vec![0_u32; positions + 1];
        for (position, environment) in job.model.structure.environments.iter().enumerate() {
            if position % super::CANCEL_STRIDE == 0 {
                cancel.check()?;
            }

            let count = environment.pairs.len();
            if count > limits.max_links_per_position as usize {
                return Err(ContextError::LimitExceeded {
                    what: "max_links_per_position",
                    value: count as u64,
                    limit: u64::from(limits.max_links_per_position),
                });
            }

            for pair in &environment.pairs {
                if pair.target_position.get() as usize >= positions {
                    return Err(ContextError::InvalidInput {
                        what: "pair link target",
                        detail: format!(
                            "owner P{position} targets P{} but the unit cell has {positions} positions",
                            pair.target_position.get()
                        ),
                    });
                }

                if pair.pair_id as usize >= pair_tables {
                    return Err(ContextError::InvalidInput {
                        what: "pair link table",
                        detail: format!(
                            "owner P{position} references pair table {} but the model has {pair_tables}",
                            pair.pair_id
                        ),
                    });
                }
            }

            owner_offsets[position + 1] = owner_offsets[position]
                .checked_add(u32::try_from(count).map_err(|_| ContextError::Invariant {
                    what: "pair link count",
                })?)
                .ok_or(ContextError::Invariant {
                    what: "pair link count overflow",
                })?;
        }

        let mut links = Vec::with_capacity(owner_offsets[positions] as usize);
        for position in 0..positions {
            let start = links.len();
            for pair in &job.model.structure.environments[position].pairs {
                links.push(Link {
                    partner_position: pair.target_position.get(),
                    offset: pair.offset,
                    pair_id: pair.pair_id,
                    cluster_start: 0,
                    cluster_len: 0,
                });
            }

            links[start..].sort_by(|left, right| {
                (left.pair_id, left.partner_position, left.offset).cmp(&(
                    right.pair_id,
                    right.partner_position,
                    right.offset,
                ))
            });
        }

        let mut inverse_offsets = vec![0_u32; positions + 1];
        for link in &links {
            let partner = link.partner_position as usize;
            inverse_offsets[partner + 1] =
                inverse_offsets[partner + 1]
                    .checked_add(1)
                    .ok_or(ContextError::Invariant {
                        what: "inverse link count overflow",
                    })?;
        }

        for partner in 0..positions {
            inverse_offsets[partner + 1] = inverse_offsets[partner]
                .checked_add(inverse_offsets[partner + 1])
                .ok_or(ContextError::Invariant {
                    what: "inverse link count overflow",
                })?;
        }

        let mut inverse = vec![
            IncomingLink {
                owner_position: 0,
                link_id: 0,
            };
            links.len()
        ];
        let mut cursor = inverse_offsets.clone();
        for owner in 0..positions {
            let start = owner_offsets[owner] as usize;
            let end = owner_offsets[owner + 1] as usize;
            for (link_id, link) in links[start..end].iter().enumerate() {
                let partner = link.partner_position as usize;
                let slot = cursor[partner] as usize;
                inverse[slot] = IncomingLink {
                    owner_position: owner as u32,
                    link_id: link_id as u32,
                };
                cursor[partner] += 1;
            }
        }

        debug_assert_eq!(inverse.len(), links.len());
        Ok(Self {
            owner_offsets,
            links,
            cluster_assoc: Vec::new(),
            inverse_offsets,
            inverse,
        })
    }

    /// Returns `true` when no forward link, inverse entry, or cluster association is stored.
    ///
    /// The CSR offset arrays are always allocated for a built arena; emptiness describes the link entries.
    #[must_use]
    pub fn is_empty(&self) -> bool {
        self.links.is_empty() && self.cluster_assoc.is_empty() && self.inverse.is_empty()
    }

    /// Returns the forward links of one P1 position.
    #[must_use]
    pub fn owner_links(&self, position: PositionId) -> &[Link] {
        let index = position.get() as usize;
        if self.owner_offsets.is_empty() {
            return &[];
        }
        let start = self.owner_offsets[index] as usize;
        let end = self.owner_offsets[index + 1] as usize;
        &self.links[start..end]
    }

    /// Returns the owner's link at `link_id`, relative to the owner's forward list.
    #[must_use]
    pub fn owner_link(&self, position: PositionId, link_id: u32) -> Option<&Link> {
        self.owner_links(position).get(link_id as usize)
    }

    /// Returns the incoming links of one partner P1 position.
    #[must_use]
    pub fn incoming_links(&self, position: PositionId) -> &[IncomingLink] {
        let index = position.get() as usize;
        if self.inverse_offsets.is_empty() {
            return &[];
        }
        let start = self.inverse_offsets[index] as usize;
        let end = self.inverse_offsets[index + 1] as usize;
        &self.inverse[start..end]
    }

    /// Appends one cluster association to a forward link.
    pub(crate) fn attach_assoc(
        &mut self,
        link_id: usize,
        assoc: ClusterAssoc,
    ) -> Result<(), ContextError> {
        let assoc_count = self.cluster_assoc.len();
        let link = self.links.get_mut(link_id).ok_or(ContextError::Invariant {
            what: "cluster association link",
        })?;

        if link.cluster_len == 0 {
            link.cluster_start =
                u32::try_from(assoc_count).map_err(|_| ContextError::LimitExceeded {
                    what: "cluster associations",
                    value: assoc_count as u64,
                    limit: u64::from(u32::MAX),
                })?;
        }

        link.cluster_len = link
            .cluster_len
            .checked_add(1)
            .ok_or(ContextError::LimitExceeded {
                what: "cluster associations per link",
                value: u64::from(link.cluster_len) + 1,
                limit: u64::from(u16::MAX),
            })?;
        self.cluster_assoc.push(assoc);
        Ok(())
    }

    /// Returns the summed allocator capacity of the arena arrays.
    #[must_use]
    pub(crate) fn capacity_bytes(&self) -> u64 {
        let owner_offsets = self.owner_offsets.capacity() as u64 * 4;
        let links = self.links.capacity() as u64 * core::mem::size_of::<Link>() as u64;
        let assoc =
            self.cluster_assoc.capacity() as u64 * core::mem::size_of::<ClusterAssoc>() as u64;
        let inverse_offsets = self.inverse_offsets.capacity() as u64 * 4;
        let inverse = self.inverse.capacity() as u64 * core::mem::size_of::<IncomingLink>() as u64;
        owner_offsets + links + assoc + inverse_offsets + inverse
    }

    /// Returns `true` when both CSRs have a consistent shape for `positions` P1 positions.
    #[must_use]
    pub(crate) fn has_valid_shape(&self, positions: u32) -> bool {
        if self.owner_offsets.is_empty() {
            return self.links.is_empty()
                && self.inverse_offsets.is_empty()
                && self.inverse.is_empty()
                && self.cluster_assoc.is_empty();
        }

        let expected = positions as usize + 1;
        if self.owner_offsets.len() != expected
            || self.inverse_offsets.len() != expected
            || self.owner_offsets.first() != Some(&0)
            || self.inverse_offsets.first() != Some(&0)
            || self.owner_offsets.last().copied() != Some(self.links.len() as u32)
            || self.inverse_offsets.last().copied() != Some(self.inverse.len() as u32)
            || self.links.len() != self.inverse.len()
            || !self.owner_offsets.windows(2).all(|pair| pair[0] <= pair[1])
            || !self
                .inverse_offsets
                .windows(2)
                .all(|pair| pair[0] <= pair[1])
        {
            return false;
        }

        if !self.links.iter().all(|link| {
            (link.partner_position as usize) < positions as usize
                && u64::from(link.cluster_start) + u64::from(link.cluster_len)
                    <= self.cluster_assoc.len() as u64
        }) {
            return false;
        }

        if !self
            .inverse
            .iter()
            .all(|incoming| (incoming.owner_position as usize) < positions as usize)
        {
            return false;
        }

        // Every inverse entry must point back at a forward link whose resolved partner is the entry's bucket.
        for partner in 0..positions as usize {
            let start = self.inverse_offsets[partner] as usize;
            let end = self.inverse_offsets[partner + 1] as usize;
            for incoming in &self.inverse[start..end] {
                let owner = incoming.owner_position as usize;
                let Some(link) = self
                    .links
                    .get(self.owner_offsets[owner] as usize + incoming.link_id as usize)
                else {
                    return false;
                };
                if link.partner_position as usize != partner
                    || self.owner_offsets[owner] as usize + incoming.link_id as usize
                        >= self.owner_offsets[owner + 1] as usize
                {
                    return false;
                }
            }
        }

        true
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

impl LinkArena {
    /// Resolves the partner environment of `owner` through `link` (Step 14 design §6.1).
    #[must_use]
    pub fn partner_of(&self, dimensions: &Dimensions, owner: EnvId, link: &Link) -> EnvId {
        dimensions.wrapped_env(
            dimensions.cell_of(owner),
            link.offset,
            link.partner_position,
        )
    }

    /// Resolves the owner environment of `partner` through an inverse entry.
    #[must_use]
    pub fn owner_of(
        &self,
        dimensions: &Dimensions,
        partner: EnvId,
        incoming: IncomingLink,
    ) -> Option<EnvId> {
        let link = self.owner_link(PositionId::new(incoming.owner_position), incoming.link_id)?;
        let owner_cell = dimensions.wrapped_cell_index_i64(
            dimensions.cell_of(partner),
            [
                -i64::from(link.offset[0]),
                -i64::from(link.offset[1]),
                -i64::from(link.offset[2]),
            ],
        );
        Some(dimensions.cell_env(owner_cell, incoming.owner_position))
    }
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

    #[test]
    fn a_dangling_inverse_entry_is_rejected() {
        let arena = LinkArena {
            owner_offsets: vec![0, 1, 1],
            links: vec![Link {
                partner_position: 1,
                offset: [0, 0, 0],
                pair_id: 0,
                cluster_start: 0,
                cluster_len: 0,
            }],
            inverse_offsets: vec![0, 1, 1],
            inverse: vec![IncomingLink {
                owner_position: 0,
                link_id: 0,
            }],
            ..LinkArena::empty()
        };

        assert!(!arena.has_valid_shape(2));
    }
}
