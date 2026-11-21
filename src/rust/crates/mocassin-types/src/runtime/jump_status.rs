// SPDX-License-Identifier: MIT

//! The KMC jump-status CSR container.
//!
//! Step 13 defines the container and its invariants; Step 14 computes the per-`(P1 position, direction)` links.
//! Each link names the **sender** and **receiver** path nodes explicitly and indexes the receiver's
//! owner-major forward link list, so the energy engine never reverse-searches the path nodes and always uses the
//! receiver's pair table and row (Step 14 design §9.2).

/// One precomputed sender/receiver link of a KMC jump.
#[derive(Debug, Clone, Copy, Default, PartialEq, Eq)]
pub struct JumpLink {
    /// The path node whose particle changes (the sender).
    pub sender_path_id: u8,
    /// The path node whose energy vector changes (the receiver).
    pub receiver_path_id: u8,
    /// The link's index inside the receiver's forward link list.
    pub link_id: u32,
}

/// A CSR jump-status table: `offsets` has one entry per status plus a final end offset into `links`.
#[derive(Debug, Clone, Default, PartialEq, Eq)]
pub struct JumpStatus {
    /// Status offsets; the first entry is zero and the last entry equals `links.len()`.
    pub offsets: Vec<u32>,
    /// The receiver-major sender/receiver links, grouped by status.
    pub links: Vec<JumpLink>,
}

impl JumpStatus {
    /// Returns `true` when the CSR offsets are nondecreasing, start at zero, and end at `links.len()`.
    #[must_use]
    pub fn is_valid(&self) -> bool {
        if self.offsets.is_empty() {
            return self.links.is_empty();
        }
        let Ok(length) = u32::try_from(self.links.len()) else {
            return false;
        };
        self.offsets.first() == Some(&0)
            && self.offsets.windows(2).all(|pair| pair[0] <= pair[1])
            && self.offsets.last().copied() == Some(length)
    }
}

#[cfg(test)]
mod tests {
    use super::{JumpLink, JumpStatus};

    fn link(sender_path_id: u8, receiver_path_id: u8, link_id: u32) -> JumpLink {
        JumpLink {
            sender_path_id,
            receiver_path_id,
            link_id,
        }
    }

    #[test]
    fn a_jump_link_is_eight_bytes() {
        assert_eq!(core::mem::size_of::<JumpLink>(), 8);
    }

    #[test]
    fn an_empty_status_is_valid() {
        assert!(JumpStatus::default().is_valid());
    }

    #[test]
    fn a_nonempty_csr_is_valid() {
        let status = JumpStatus {
            offsets: vec![0, 2, 3],
            links: vec![link(1, 2, 0), link(0, 1, 2), link(2, 0, 1)],
        };
        assert!(status.is_valid());
    }

    #[test]
    fn offsets_must_start_at_zero_and_end_at_the_link_count() {
        assert!(
            !JumpStatus {
                offsets: vec![1, 2],
                links: vec![link(0, 1, 0)],
            }
            .is_valid()
        );
        assert!(
            !JumpStatus {
                offsets: vec![0, 2],
                links: vec![link(0, 1, 0)],
            }
            .is_valid()
        );
    }

    #[test]
    fn offsets_must_be_nondecreasing() {
        assert!(
            !JumpStatus {
                offsets: vec![0, 2, 1, 2],
                links: vec![link(0, 1, 0), link(0, 1, 1)],
            }
            .is_valid()
        );
    }
}
