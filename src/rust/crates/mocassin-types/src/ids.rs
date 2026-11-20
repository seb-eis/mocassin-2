// SPDX-License-Identifier: MIT

//! Dense typed identities used by the validated runtime model.
//!
//! The compiled package stores plain integers; the runtime model wraps them so particle, position, cell,
//! collection, direction, and rule indices cannot be confused. Construction is explicit through [`new`], which
//! keeps the payload → runtime mapping the single place where an untrusted integer becomes a typed identity.
//!
//! [`new`]: ParticleId::new

use core::fmt;

/// Dense particle index, `0..=63`.
///
/// Particle `0` is void. The six-bit state-code slots and the `u64` particle masks bound the table to 64
/// particles, so a `u8` payload value is narrowed by the mapping layer and never widened back into a 32-bit
/// shift.
#[derive(Clone, Copy, Debug, Default, PartialEq, Eq, PartialOrd, Ord, Hash)]
#[repr(transparent)]
pub struct ParticleId(u8);

impl ParticleId {
    /// Wraps a validated dense particle index.
    #[must_use]
    pub const fn new(value: u8) -> Self {
        Self(value)
    }

    /// Returns the dense index.
    #[must_use]
    pub const fn get(self) -> u8 {
        self.0
    }
}

impl fmt::Display for ParticleId {
    fn fmt(&self, formatter: &mut fmt::Formatter<'_>) -> fmt::Result {
        fmt::Display::fmt(&self.0, formatter)
    }
}

impl From<ParticleId> for u8 {
    fn from(id: ParticleId) -> Self {
        id.0
    }
}

/// Dense position index inside the unit cell (the compiled `P1` position).
#[derive(Clone, Copy, Debug, Default, PartialEq, Eq, PartialOrd, Ord, Hash)]
#[repr(transparent)]
pub struct PositionId(u32);

impl PositionId {
    /// Wraps a validated dense position index.
    #[must_use]
    pub const fn new(value: u32) -> Self {
        Self(value)
    }

    /// Returns the dense index.
    #[must_use]
    pub const fn get(self) -> u32 {
        self.0
    }
}

impl fmt::Display for PositionId {
    fn fmt(&self, formatter: &mut fmt::Formatter<'_>) -> fmt::Result {
        fmt::Display::fmt(&self.0, formatter)
    }
}

impl From<PositionId> for u32 {
    fn from(id: PositionId) -> Self {
        id.0
    }
}

/// Dense supercell index in the documented row-major lattice order (`((a * B + b) * C + c)`).
#[derive(Clone, Copy, Debug, Default, PartialEq, Eq, PartialOrd, Ord, Hash)]
#[repr(transparent)]
pub struct CellIndex(u32);

impl CellIndex {
    /// Wraps a validated linear cell index.
    #[must_use]
    pub const fn new(value: u32) -> Self {
        Self(value)
    }

    /// Returns the linear cell index.
    #[must_use]
    pub const fn get(self) -> u32 {
        self.0
    }
}

impl fmt::Display for CellIndex {
    fn fmt(&self, formatter: &mut fmt::Formatter<'_>) -> fmt::Result {
        fmt::Display::fmt(&self.0, formatter)
    }
}

impl From<CellIndex> for u32 {
    fn from(index: CellIndex) -> Self {
        index.0
    }
}

/// Dense jump-collection index.
#[derive(Clone, Copy, Debug, Default, PartialEq, Eq, PartialOrd, Ord, Hash)]
#[repr(transparent)]
pub struct JumpCollectionId(u32);

impl JumpCollectionId {
    /// Wraps a validated collection index.
    #[must_use]
    pub const fn new(value: u32) -> Self {
        Self(value)
    }

    /// Returns the dense index.
    #[must_use]
    pub const fn get(self) -> u32 {
        self.0
    }
}

impl fmt::Display for JumpCollectionId {
    fn fmt(&self, formatter: &mut fmt::Formatter<'_>) -> fmt::Result {
        fmt::Display::fmt(&self.0, formatter)
    }
}

impl From<JumpCollectionId> for u32 {
    fn from(id: JumpCollectionId) -> Self {
        id.0
    }
}

/// Dense jump-direction index.
#[derive(Clone, Copy, Debug, Default, PartialEq, Eq, PartialOrd, Ord, Hash)]
#[repr(transparent)]
pub struct JumpDirectionId(u32);

impl JumpDirectionId {
    /// Wraps a validated direction index.
    #[must_use]
    pub const fn new(value: u32) -> Self {
        Self(value)
    }

    /// Returns the dense index.
    #[must_use]
    pub const fn get(self) -> u32 {
        self.0
    }
}

impl fmt::Display for JumpDirectionId {
    fn fmt(&self, formatter: &mut fmt::Formatter<'_>) -> fmt::Result {
        fmt::Display::fmt(&self.0, formatter)
    }
}

impl From<JumpDirectionId> for u32 {
    fn from(id: JumpDirectionId) -> Self {
        id.0
    }
}

/// Dense transition-rule index.
///
/// The reader requires `rule_id == index` in the rule vector, so the typed value and the payload value are the
/// same integer.
#[derive(Clone, Copy, Debug, Default, PartialEq, Eq, PartialOrd, Ord, Hash)]
#[repr(transparent)]
pub struct RuleId(u32);

impl RuleId {
    /// Wraps a validated rule index.
    #[must_use]
    pub const fn new(value: u32) -> Self {
        Self(value)
    }

    /// Returns the dense index.
    #[must_use]
    pub const fn get(self) -> u32 {
        self.0
    }
}

impl fmt::Display for RuleId {
    fn fmt(&self, formatter: &mut fmt::Formatter<'_>) -> fmt::Result {
        fmt::Display::fmt(&self.0, formatter)
    }
}

impl From<RuleId> for u32 {
    fn from(id: RuleId) -> Self {
        id.0
    }
}

/// Packed occupation state of one rule endpoint or start bucket.
///
/// The code is always an unsigned `u64`. It stores up to [`StateCode::MAX_NODES`] path nodes of
/// [`StateCode::BITS_PER_NODE`] bits each, node `i` in bits `6*i .. 6*i + 6`. Node `0` is the start
/// occupation. A signed `i64` is never used, so the high node slots can never acquire a sign extension.
#[derive(Clone, Copy, Debug, Default, PartialEq, Eq, PartialOrd, Ord, Hash)]
#[repr(transparent)]
pub struct StateCode(u64);

impl StateCode {
    /// Maximum number of six-bit path nodes a state code can carry (a `u64` holds ten).
    pub const MAX_NODES: usize = 10;

    /// Width of one packed node in bits.
    pub const BITS_PER_NODE: u32 = 6;

    /// Mask of one packed node.
    pub const NODE_MASK: u64 = (1_u64 << Self::BITS_PER_NODE) - 1;

    /// Wraps a raw packed state code.
    #[must_use]
    pub const fn new(value: u64) -> Self {
        Self(value)
    }

    /// Returns the raw packed code.
    #[must_use]
    pub const fn get(self) -> u64 {
        self.0
    }

    /// Returns the particle index stored in node `index`.
    ///
    /// # Panics
    ///
    /// Panics when `index >= StateCode::MAX_NODES`. The runtime path length is validated to be at most
    /// [`StateCode::MAX_NODES`] before a node is read.
    #[must_use]
    pub const fn node(self, index: usize) -> u8 {
        assert!(index < Self::MAX_NODES, "state node index is out of range");
        ((self.0 >> (Self::BITS_PER_NODE * index as u32)) & Self::NODE_MASK) as u8
    }

    /// Returns the particle index stored in node `index`, or `None` when the index is out of range.
    #[must_use]
    pub const fn try_node(self, index: usize) -> Option<u8> {
        if index < Self::MAX_NODES {
            Some(self.node(index))
        } else {
            None
        }
    }

    /// Returns the mask of the bits that `node_count` packed nodes occupy.
    ///
    /// Validation uses this to reject a state code whose bits above the rule's path length are set. A count of
    /// zero yields a zero mask.
    ///
    /// # Panics
    ///
    /// Panics when `node_count > StateCode::MAX_NODES`.
    #[must_use]
    pub const fn mask(node_count: usize) -> u64 {
        assert!(
            node_count <= Self::MAX_NODES,
            "state node count is out of range"
        );
        if node_count == 0 {
            0
        } else {
            (1_u64 << (Self::BITS_PER_NODE * node_count as u32)) - 1
        }
    }

    /// Returns `true` when every bit above `node_count` packed nodes is clear.
    ///
    /// This is the rule-state check from the Step 13 design §3.5: a rule with a path of `node_count` nodes may
    /// not carry occupation data in the unused high slots.
    #[must_use]
    pub const fn has_clear_bits_above(self, node_count: usize) -> bool {
        self.0 & !Self::mask(node_count) == 0
    }
}

impl fmt::Display for StateCode {
    fn fmt(&self, formatter: &mut fmt::Formatter<'_>) -> fmt::Result {
        write!(formatter, "0x{:016x}", self.0)
    }
}

impl From<StateCode> for u64 {
    fn from(code: StateCode) -> Self {
        code.0
    }
}

#[cfg(test)]
mod tests {
    use super::{
        CellIndex, JumpCollectionId, JumpDirectionId, ParticleId, PositionId, RuleId, StateCode,
    };

    #[test]
    fn ids_round_trip_their_dense_index() {
        assert_eq!(ParticleId::new(3).get(), 3);
        assert_eq!(u8::from(ParticleId::new(3)), 3);
        assert_eq!(PositionId::new(4).get(), 4);
        assert_eq!(u32::from(PositionId::new(4)), 4);
        assert_eq!(CellIndex::new(5).get(), 5);
        assert_eq!(u32::from(CellIndex::new(5)), 5);
        assert_eq!(JumpCollectionId::new(6).get(), 6);
        assert_eq!(u32::from(JumpCollectionId::new(6)), 6);
        assert_eq!(JumpDirectionId::new(7).get(), 7);
        assert_eq!(u32::from(JumpDirectionId::new(7)), 7);
        assert_eq!(RuleId::new(8).get(), 8);
        assert_eq!(u32::from(RuleId::new(8)), 8);
    }

    #[test]
    fn ids_default_to_zero_and_order_numerically() {
        assert_eq!(ParticleId::default(), ParticleId::new(0));
        assert_eq!(PositionId::default(), PositionId::new(0));
        assert!(ParticleId::new(1) < ParticleId::new(2));
        assert!(PositionId::new(2) > PositionId::new(1));
    }

    #[test]
    fn ids_display_their_dense_index() {
        assert_eq!(ParticleId::new(3).to_string(), "3");
        assert_eq!(PositionId::new(42).to_string(), "42");
    }

    #[test]
    fn state_code_reads_each_six_bit_node() {
        // Node 0 = 1, node 1 = 2, node 9 = 63.
        let code = StateCode::new(1 | (2 << 6) | (63 << 54));
        assert_eq!(code.node(0), 1);
        assert_eq!(code.node(1), 2);
        assert_eq!(code.node(9), 63);
        assert_eq!(code.get(), 0x0fc0_0000_0000_0081);
    }

    #[test]
    fn state_code_try_node_rejects_out_of_range_indices() {
        let code = StateCode::new(1);
        assert_eq!(code.try_node(0), Some(1));
        assert_eq!(code.try_node(StateCode::MAX_NODES), None);
    }

    #[test]
    #[should_panic(expected = "state node index is out of range")]
    fn state_code_node_panics_past_the_last_node() {
        let _ = StateCode::new(0).node(StateCode::MAX_NODES);
    }

    #[test]
    fn state_code_mask_covers_only_the_used_nodes() {
        assert_eq!(StateCode::mask(0), 0);
        assert_eq!(StateCode::mask(1), 0b11_1111);
        assert_eq!(StateCode::mask(2), 0b1111_1111_1111);
        assert_eq!(StateCode::mask(StateCode::MAX_NODES), (1_u64 << 60) - 1);
    }

    #[test]
    fn state_code_high_bits_are_detected() {
        let two_nodes = StateCode::new(0b11_1111 | (0b11_1111 << 6));
        assert!(two_nodes.has_clear_bits_above(2));
        assert!(!two_nodes.has_clear_bits_above(1));
        assert!(!StateCode::new(1 << 12).has_clear_bits_above(2));
        assert!(StateCode::new(0).has_clear_bits_above(0));
    }

    #[test]
    fn state_code_displays_as_hex() {
        assert_eq!(StateCode::new(0x0f).to_string(), "0x000000000000000f");
    }
}
