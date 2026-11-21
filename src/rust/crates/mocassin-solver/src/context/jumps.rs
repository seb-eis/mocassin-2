// SPDX-License-Identifier: MIT

//! The KMC jump-status CSR.
//!
//! Feature 1 defines the container only; it is empty until feature 4 computes the receiver-major links. A status
//! is one `(P1 position, direction)` pair (`status = direction · P + position`), so the CSR is
//! translation-compact and independent of the supercell size. Feature 4 fills `links` with the explicit
//! sender/receiver path ids and reshapes `mocassin_types::JumpLink` accordingly.

use mocassin_types::JumpLink;

/// The jump-status offsets and links grouped by ascending status index.
#[derive(Clone, Debug, Default, PartialEq, Eq)]
pub struct JumpRuntime {
    /// Status offsets, length `S + 1`, with `S = P · D`.
    pub offsets: Vec<u32>,
    /// The receiver-major jump links grouped by status.
    pub links: Vec<JumpLink>,
}

impl JumpRuntime {
    /// Returns the empty jump status of a context that does not build it yet.
    #[must_use]
    pub fn empty() -> Self {
        Self::default()
    }

    /// Returns `true` when no offset or link is stored.
    #[must_use]
    pub fn is_empty(&self) -> bool {
        self.offsets.is_empty() && self.links.is_empty()
    }

    /// Returns `true` when the CSR has a consistent shape for `statuses` statuses.
    ///
    /// An empty container is accepted for any status count: MMC never builds the jump status, and a KMC context
    /// whose status build is a later feature stores no offset array yet.
    #[must_use]
    pub(crate) fn has_valid_shape(&self, statuses: u64) -> bool {
        if self.offsets.is_empty() {
            return self.links.is_empty();
        }

        self.offsets.len() as u64 == statuses + 1
            && self.offsets.first() == Some(&0)
            && self.offsets.last().copied() == Some(self.links.len() as u32)
            && self.offsets.windows(2).all(|pair| pair[0] <= pair[1])
    }
}

#[cfg(test)]
mod tests {
    use super::JumpRuntime;

    #[test]
    fn an_empty_status_is_valid_for_any_status_count() {
        let statuses = JumpRuntime::empty();
        assert!(statuses.is_empty());
        assert!(statuses.has_valid_shape(0));
        assert!(statuses.has_valid_shape(12));
    }

    #[test]
    fn an_inconsistent_csr_is_rejected() {
        let statuses = JumpRuntime {
            offsets: vec![0, 1],
            links: Vec::new(),
        };
        assert!(!statuses.has_valid_shape(1));
    }
}
