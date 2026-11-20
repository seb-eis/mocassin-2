// SPDX-License-Identifier: MIT

//! The validated transitions of the runtime model.

use crate::ids::{JumpCollectionId, JumpDirectionId, RuleId, StateCode};
use crate::settings::SimulationKind;

/// The validated transition state of a model.
#[derive(Debug, Clone, PartialEq)]
pub struct TransitionModel {
    /// The compiled jump collections in dense index order.
    pub collections: Vec<JumpCollection>,
    /// The compiled direction representatives in dense index order.
    pub directions: Vec<JumpDirection>,
    /// The compiled rules in dense rule order.
    pub rules: Vec<JumpRule>,
    /// The selectable direction count per `(position, particle)`.
    pub jump_count: Vec<u32>,
    /// The prefix sums of `jump_count`; length is `jump_count.len() + 1`.
    pub jump_offsets: Vec<u32>,
    /// The selectable direction indices in relative-slot order.
    pub jump_mapping: Vec<u32>,
    /// The dense `[position][particle] → tracker offset` array with `-1` for "none".
    pub static_tracker_mapping: Vec<i32>,
    /// The dense `[collection][particle] → tracker index` array with `-1` for "none".
    pub global_tracker_mapping: Vec<i32>,
}

/// One compiled jump collection.
#[derive(Debug, Clone, PartialEq, Eq)]
pub struct JumpCollection {
    /// The simulation kind that owns the collection.
    pub kind: SimulationKind,
    /// The authored transition identifier.
    pub transition_id: String,
    /// The compiled mobile-particle mask.
    pub mobile_particle_mask: u64,
    /// The collection rule indices.
    pub rule_ids: Vec<RuleId>,
    /// The collection direction indices.
    pub direction_ids: Vec<JumpDirectionId>,
}

/// One compiled jump-direction representative.
#[derive(Debug, Clone, PartialEq)]
pub struct JumpDirection {
    /// The stable compiled direction identity.
    pub object_id: String,
    /// The representative start canonical P1 index.
    pub position_id: u32,
    /// The owning collection index.
    pub collection_id: JumpCollectionId,
    /// The per-node relative encodings `[cellA, cellB, cellC, P]`; node zero is the start.
    pub path: Vec<[i32; 4]>,
    /// The per-node fractional movement vectors.
    pub movement: Vec<[f64; 3]>,
    /// The Cartesian net path vector in ångström.
    pub path_vector: [f64; 3],
    /// The magnitude of `path_vector` in ångström.
    pub length: f64,
    /// Whether the direction is an automatically generated inverse.
    pub is_inverse: bool,
}

/// One compiled jump rule.
#[derive(Debug, Clone, PartialEq)]
pub struct JumpRule {
    /// The dense rule index.
    pub rule_id: RuleId,
    /// The owning collection index.
    pub collection_id: JumpCollectionId,
    /// The state-0 code, six bits per path node.
    pub state0: StateCode,
    /// The state-1 code; zero and ignored for MMC.
    pub state1: StateCode,
    /// The state-2 code.
    pub state2: StateCode,
    /// The absolute applied attempt frequency in hertz.
    pub frequency_hz: f64,
    /// The particle-identity charge transport vector in e·Å.
    pub charge_transport_vector: [f64; 3],
    /// The optional static S2 correction; `None` in v1.
    pub static_correction: Option<[f64; 3]>,
    /// One entry per path node: the global tracker index, or `0xFF`.
    pub tracker_order: Vec<u8>,
}
