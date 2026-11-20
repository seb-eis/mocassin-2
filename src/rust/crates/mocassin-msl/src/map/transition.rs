// SPDX-License-Identifier: MIT

//! The transition-payload mapping.

use mocassin_types::payload::TransitionPayload;
use mocassin_types::{
    JumpCollection, JumpCollectionId, JumpDirection, JumpDirectionId, JumpRule, ReaderLimits,
    RuleId, StateCode, TransitionModel,
};

use crate::error::PackageError;
use crate::validate::{
    require_finite, require_finite3, require_hex64, require_mask_bits, require_unique,
};

use super::{parse_kind, u32_len};

pub(super) fn map_transitions(
    payload: &TransitionPayload,
    particle_count: u32,
    position_count: u32,
    static_trackers_per_cell: u32,
    global_tracker_count: u32,
    limits: &ReaderLimits,
) -> Result<TransitionModel, PackageError> {
    let collection_count = u32_len(payload.jump_collections.len())?;
    let direction_count = u32_len(payload.jump_directions.len())?;
    let rule_count = u32_len(payload.jump_rules.len())?;

    let mut collections = Vec::with_capacity(payload.jump_collections.len());
    for collection in &payload.jump_collections {
        let kind = parse_kind("transitions.jump_collections.kind", &collection.kind)?;
        require_mask_bits(
            "transitions.jump_collections.mobile_particle_mask",
            collection.mobile_particle_mask,
            particle_count,
        )?;

        for rule in &collection.rule_ids {
            if *rule >= rule_count {
                return Err(PackageError::InvalidIndex {
                    what: "transitions.jump_collections.rule_ids",
                    value: u64::from(*rule),
                    limit: u64::from(rule_count),
                });
            }
        }
        require_unique(
            "transitions.jump_collections.rule_ids",
            &collection.rule_ids,
        )?;
        for direction in &collection.direction_ids {
            if *direction >= direction_count {
                return Err(PackageError::InvalidIndex {
                    what: "transitions.jump_collections.direction_ids",
                    value: u64::from(*direction),
                    limit: u64::from(direction_count),
                });
            }
        }
        require_unique(
            "transitions.jump_collections.direction_ids",
            &collection.direction_ids,
        )?;

        collections.push(JumpCollection {
            kind,
            transition_id: collection.transition_id.clone(),
            mobile_particle_mask: collection.mobile_particle_mask,
            rule_ids: collection
                .rule_ids
                .iter()
                .map(|rule| RuleId::new(*rule))
                .collect(),
            direction_ids: collection
                .direction_ids
                .iter()
                .map(|direction| JumpDirectionId::new(*direction))
                .collect(),
        });
    }

    let mut directions = Vec::with_capacity(payload.jump_directions.len());
    for direction in &payload.jump_directions {
        require_hex64(
            "transitions.jump_directions.object_id",
            &direction.object_id,
        )?;
        if direction.position_id >= position_count {
            return Err(PackageError::InvalidIndex {
                what: "transitions.jump_directions.position_id",
                value: u64::from(direction.position_id),
                limit: u64::from(position_count),
            });
        }
        if direction.collection_id >= collection_count {
            return Err(PackageError::InvalidIndex {
                what: "transitions.jump_directions.collection_id",
                value: u64::from(direction.collection_id),
                limit: u64::from(collection_count),
            });
        }
        if direction.path.len() != direction.movement.len() {
            return Err(PackageError::LengthMismatch {
                what: "transitions.jump_directions.movement",
                expected: direction.path.len() as u64,
                found: direction.movement.len() as u64,
            });
        }
        if direction.path.len() < 2 || direction.path.len() > limits.max_path_nodes as usize {
            return Err(PackageError::InvalidValue {
                what: "transitions.jump_directions.path",
                detail: format!(
                    "a path must have 2..={} nodes, found {}",
                    limits.max_path_nodes,
                    direction.path.len()
                ),
            });
        }
        for node in &direction.path {
            let position = node[3];
            if position < 0 || position as u32 >= position_count {
                return Err(PackageError::InvalidIndex {
                    what: "transitions.jump_directions.path",
                    value: position.unsigned_abs() as u64,
                    limit: u64::from(position_count),
                });
            }
        }
        for movement in &direction.movement {
            require_finite3("transitions.jump_directions.movement", *movement)?;
        }
        require_finite3(
            "transitions.jump_directions.path_vector",
            direction.path_vector,
        )?;
        require_finite("transitions.jump_directions.length", direction.length)?;
        if direction.length < 0.0 {
            return Err(PackageError::InvalidValue {
                what: "transitions.jump_directions.length",
                detail: "the length must be non-negative".to_owned(),
            });
        }

        directions.push(JumpDirection {
            object_id: direction.object_id.clone(),
            position_id: direction.position_id,
            collection_id: JumpCollectionId::new(direction.collection_id),
            path: direction.path.clone(),
            movement: direction.movement.clone(),
            path_vector: direction.path_vector,
            length: direction.length,
            is_inverse: direction.is_inverse,
        });
    }

    let mut rules = Vec::with_capacity(payload.jump_rules.len());
    for (index, rule) in payload.jump_rules.iter().enumerate() {
        if rule.rule_id != index as u32 {
            return Err(PackageError::InvalidValue {
                what: "transitions.jump_rules.rule_id",
                detail: "rule_id must equal the dense rule index".to_owned(),
            });
        }
        if rule.collection_id >= collection_count {
            return Err(PackageError::InvalidIndex {
                what: "transitions.jump_rules.collection_id",
                value: u64::from(rule.collection_id),
                limit: u64::from(collection_count),
            });
        }
        for (name, state) in [
            ("state0", rule.state0),
            ("state1", rule.state1),
            ("state2", rule.state2),
        ] {
            let code = StateCode::new(state);
            for node in 0..StateCode::MAX_NODES {
                if u32::from(code.node(node)) >= particle_count {
                    return Err(PackageError::InvalidIndex {
                        what: name,
                        value: u64::from(code.node(node)),
                        limit: u64::from(particle_count),
                    });
                }
            }
        }
        require_finite("transitions.jump_rules.frequency_hz", rule.frequency_hz)?;
        if rule.frequency_hz < 0.0 {
            return Err(PackageError::InvalidValue {
                what: "transitions.jump_rules.frequency_hz",
                detail: "a frequency must be non-negative".to_owned(),
            });
        }
        require_finite3(
            "transitions.jump_rules.charge_transport_vector",
            rule.charge_transport_vector,
        )?;
        if let Some(correction) = rule.static_correction {
            require_finite3("transitions.jump_rules.static_correction", correction)?;
        }
        if rule.tracker_order.len() > limits.max_path_nodes as usize {
            return Err(PackageError::InvalidValue {
                what: "transitions.jump_rules.tracker_order",
                detail: "the tracker order exceeds the path-node limit".to_owned(),
            });
        }

        rules.push(JumpRule {
            rule_id: RuleId::new(rule.rule_id),
            collection_id: JumpCollectionId::new(rule.collection_id),
            state0: StateCode::new(rule.state0),
            state1: StateCode::new(rule.state1),
            state2: StateCode::new(rule.state2),
            frequency_hz: rule.frequency_hz,
            charge_transport_vector: rule.charge_transport_vector,
            static_correction: rule.static_correction,
            tracker_order: rule.tracker_order.clone(),
        });
    }

    let selection_cells = u64::from(position_count)
        .checked_mul(u64::from(particle_count))
        .ok_or_else(|| PackageError::InvalidValue {
            what: "transitions",
            detail: "position_count × particle_count overflows".to_owned(),
        })?;

    if payload.jump_count.len() as u64 != selection_cells {
        return Err(PackageError::LengthMismatch {
            what: "transitions.jump_count",
            expected: selection_cells,
            found: payload.jump_count.len() as u64,
        });
    }
    if payload.static_tracker_mapping.len() as u64 != selection_cells {
        return Err(PackageError::LengthMismatch {
            what: "transitions.static_tracker_mapping",
            expected: selection_cells,
            found: payload.static_tracker_mapping.len() as u64,
        });
    }

    let mut jump_offsets = Vec::with_capacity(payload.jump_count.len() + 1);
    let mut offset = 0_u64;
    jump_offsets.push(0_u32);
    for count in &payload.jump_count {
        offset += u64::from(*count);
        if offset > u64::from(u32::MAX) {
            return Err(PackageError::InvalidValue {
                what: "transitions.jump_offsets",
                detail: "the prefix sum exceeds the 32-bit offset space".to_owned(),
            });
        }
        jump_offsets.push(offset as u32);
    }
    if offset != payload.jump_mapping.len() as u64 {
        return Err(PackageError::LengthMismatch {
            what: "transitions.jump_mapping",
            expected: offset,
            found: payload.jump_mapping.len() as u64,
        });
    }
    for direction in &payload.jump_mapping {
        if *direction >= direction_count {
            return Err(PackageError::InvalidIndex {
                what: "transitions.jump_mapping",
                value: u64::from(*direction),
                limit: u64::from(direction_count),
            });
        }
    }

    for value in &payload.static_tracker_mapping {
        if *value != -1
            && !u32::try_from(*value).is_ok_and(|value| value < static_trackers_per_cell)
        {
            return Err(PackageError::InvalidIndex {
                what: "transitions.static_tracker_mapping",
                value: value.unsigned_abs() as u64,
                limit: u64::from(static_trackers_per_cell),
            });
        }
    }
    let collection_particles = u64::from(collection_count)
        .checked_mul(u64::from(particle_count))
        .ok_or_else(|| PackageError::InvalidValue {
            what: "transitions.global_tracker_mapping",
            detail: "collection_count × particle_count overflows".to_owned(),
        })?;
    if payload.global_tracker_mapping.len() as u64 != collection_particles {
        return Err(PackageError::LengthMismatch {
            what: "transitions.global_tracker_mapping",
            expected: collection_particles,
            found: payload.global_tracker_mapping.len() as u64,
        });
    }
    for value in &payload.global_tracker_mapping {
        if *value != -1 && !u32::try_from(*value).is_ok_and(|value| value < global_tracker_count) {
            return Err(PackageError::InvalidIndex {
                what: "transitions.global_tracker_mapping",
                value: value.unsigned_abs() as u64,
                limit: u64::from(global_tracker_count),
            });
        }
    }

    Ok(TransitionModel {
        collections,
        directions,
        rules,
        jump_count: payload.jump_count.clone(),
        jump_offsets,
        jump_mapping: payload.jump_mapping.clone(),
        static_tracker_mapping: payload.static_tracker_mapping.clone(),
        global_tracker_mapping: payload.global_tracker_mapping.clone(),
    })
}
