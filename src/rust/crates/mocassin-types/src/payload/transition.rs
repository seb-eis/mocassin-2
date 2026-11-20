// SPDX-License-Identifier: MIT

//! The compiled transition section of the model payload.

use serde::{Deserialize, Serialize};

/// The compiled transitions.
///
/// `jump_count` is in `[position][particle]` order and `jump_mapping` is the flat concatenation in
/// `[position][particle][relative]` order.
#[derive(Debug, Clone, PartialEq, Deserialize, Serialize)]
#[serde(rename_all = "snake_case")]
pub struct TransitionPayload {
    /// The compiled jump collections in dense index order.
    pub jump_collections: Vec<JumpCollectionPayload>,
    /// The compiled direction representatives in dense index order.
    pub jump_directions: Vec<JumpDirectionPayload>,
    /// The compiled rules in dense rule order.
    pub jump_rules: Vec<JumpRulePayload>,
    /// The selectable direction count per `(position, particle)`.
    pub jump_count: Vec<u32>,
    /// The selectable direction indices in relative-slot order.
    pub jump_mapping: Vec<u32>,
    /// The dense `[position][particle] → tracker offset` array with `-1` for "none".
    pub static_tracker_mapping: Vec<i32>,
    /// The dense `[collection][particle] → tracker index` array with `-1` for "none".
    pub global_tracker_mapping: Vec<i32>,
}

/// One compiled jump collection.
#[derive(Debug, Clone, PartialEq, Deserialize, Serialize)]
#[serde(rename_all = "snake_case")]
pub struct JumpCollectionPayload {
    /// The simulation kind that owns the collection (`"kmc"` or `"mmc"`).
    pub kind: String,
    /// The authored transition identifier.
    pub transition_id: String,
    /// The compiled mobile-particle mask.
    pub mobile_particle_mask: u64,
    /// The collection rule indices.
    pub rule_ids: Vec<u32>,
    /// The collection direction indices.
    pub direction_ids: Vec<u32>,
}

/// One compiled jump-direction representative.
#[derive(Debug, Clone, PartialEq, Deserialize, Serialize)]
#[serde(rename_all = "snake_case")]
pub struct JumpDirectionPayload {
    /// The stable compiled direction identity.
    pub object_id: String,
    /// The representative start canonical P1 index.
    pub position_id: u32,
    /// The owning collection index.
    pub collection_id: u32,
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
#[derive(Debug, Clone, PartialEq, Deserialize, Serialize)]
#[serde(rename_all = "snake_case")]
pub struct JumpRulePayload {
    /// The dense persisted rule index.
    pub rule_id: u32,
    /// The owning collection index.
    pub collection_id: u32,
    /// The state-0 code, six bits per path node.
    pub state0: u64,
    /// The state-1 code; zero and ignored for MMC.
    pub state1: u64,
    /// The state-2 code.
    pub state2: u64,
    /// The absolute applied attempt frequency in hertz.
    pub frequency_hz: f64,
    /// The particle-identity charge transport vector in e·Å.
    pub charge_transport_vector: [f64; 3],
    /// The optional static S2 correction; `None` in v1.
    pub static_correction: Option<[f64; 3]>,
    /// One entry per path node: the global tracker index, or `0xFF` for a non-mobile node.
    pub tracker_order: Vec<u8>,
}
