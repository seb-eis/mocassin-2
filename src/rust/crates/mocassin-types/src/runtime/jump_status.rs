// SPDX-License-Identifier: MIT

//! The KMC jump-status CSR container.
//!
//! Step 13 defines the container and its invariants; Step 14 computes the per-`[cell][direction]` links and
//! retains the static-correction decision. The layout mirrors the original solver's `JumpLink` fields so the
//! sender path node and the affected environment link stay explicit.

/// One precomputed sender/receiver link of a KMC jump.
#[derive(Debug, Clone, Copy, Default, PartialEq, Eq)]
pub struct JumpLink {
    /// The path node whose particle changes (the sender).
    pub sender_path_id: i32,
    /// The relative environment-link id whose energy changes (the receiver).
    pub link_id: i32,
}

/// A CSR jump-status table: `offsets` has one entry per status plus a final end offset into `links`.
#[derive(Debug, Clone, Default, PartialEq, Eq)]
pub struct JumpStatus {
    /// Status offsets; the first entry is zero and the last entry equals `links.len()`.
    pub offsets: Vec<u32>,
    /// The sender/receiver links, grouped by status.
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

    fn link(sender_path_id: i32, link_id: i32) -> JumpLink {
        JumpLink {
            sender_path_id,
            link_id,
        }
    }

    #[test]
    fn an_empty_status_is_valid() {
        assert!(JumpStatus::default().is_valid());
    }

    #[test]
    fn a_nonempty_csr_is_valid() {
        let status = JumpStatus {
            offsets: vec![0, 2, 3],
            links: vec![link(1, 0), link(0, 2), link(2, 1)],
        };
        assert!(status.is_valid());
    }

    #[test]
    fn offsets_must_start_at_zero_and_end_at_the_link_count() {
        assert!(
            !JumpStatus {
                offsets: vec![1, 2],
                links: vec![link(0, 0)],
            }
            .is_valid()
        );
        assert!(
            !JumpStatus {
                offsets: vec![0, 2],
                links: vec![link(0, 0)],
            }
            .is_valid()
        );
    }

    #[test]
    fn offsets_must_be_nondecreasing() {
        assert!(
            !JumpStatus {
                offsets: vec![0, 2, 1, 2],
                links: vec![link(0, 0), link(0, 1)],
            }
            .is_valid()
        );
    }
}
