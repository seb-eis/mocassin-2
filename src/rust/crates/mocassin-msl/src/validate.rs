// SPDX-License-Identifier: MIT

//! Shared validation helpers and the scientific model invariants.

use std::collections::HashSet;

use mocassin_types::{ParameterizedModel, SimulationKind, SolverJobFlags};

use crate::error::PackageError;

/// Returns whether a string is exactly 64 lowercase hexadecimal characters.
#[must_use]
pub(crate) fn is_lower_hex_64(value: &str) -> bool {
    value.len() == 64
        && value
            .bytes()
            .all(|byte| byte.is_ascii_digit() || (b'a'..=b'f').contains(&byte))
}

/// Requires a 64-character lowercase hexadecimal identity.
pub(crate) fn require_hex64(what: &'static str, value: &str) -> Result<(), PackageError> {
    if is_lower_hex_64(value) {
        Ok(())
    } else {
        Err(PackageError::InvalidValue {
            what,
            detail: "expected 64 lowercase hexadecimal characters".to_owned(),
        })
    }
}

/// Requires a finite value.
pub(crate) fn require_finite(what: &'static str, value: f64) -> Result<(), PackageError> {
    if value.is_finite() {
        Ok(())
    } else {
        Err(PackageError::InvalidValue {
            what,
            detail: "the value is not finite".to_owned(),
        })
    }
}

/// Requires three finite values.
pub(crate) fn require_finite3(what: &'static str, value: [f64; 3]) -> Result<(), PackageError> {
    if value.iter().all(|component| component.is_finite()) {
        Ok(())
    } else {
        Err(PackageError::InvalidValue {
            what,
            detail: "a component is not finite".to_owned(),
        })
    }
}

/// Requires `mask` to have no bit at or above `particle_count`.
pub(crate) fn require_mask_bits(
    what: &'static str,
    mask: u64,
    particle_count: u32,
) -> Result<(), PackageError> {
    if particle_count >= 64 {
        return Ok(());
    }

    let allowed = (1_u64 << particle_count) - 1;
    if mask & !allowed == 0 {
        Ok(())
    } else {
        Err(PackageError::InvalidValue {
            what,
            detail: format!("the mask sets a bit at or above the {particle_count} particles"),
        })
    }
}

/// Requires a strictly ascending slice.
pub(crate) fn require_strictly_ascending<T: Ord>(
    what: &'static str,
    values: &[T],
) -> Result<(), PackageError> {
    if values.windows(2).all(|window| window[0] < window[1]) {
        Ok(())
    } else {
        Err(PackageError::InvalidValue {
            what,
            detail: "the values are not strictly ascending".to_owned(),
        })
    }
}

/// Requires every value to be unique.
pub(crate) fn require_unique<T: Eq + std::hash::Hash + Copy>(
    what: &'static str,
    values: &[T],
) -> Result<(), PackageError> {
    let mut seen = HashSet::with_capacity(values.len());
    for value in values {
        if !seen.insert(*value) {
            return Err(invalid(what, "the values must be unique"));
        }
    }
    Ok(())
}

/// Requires a strictly ascending slice of indices below `limit`.
pub(crate) fn require_ascending_below(
    what: &'static str,
    values: &[u8],
    limit: u32,
) -> Result<(), PackageError> {
    require_strictly_ascending(what, values)?;
    for value in values {
        if u32::from(*value) >= limit {
            return Err(PackageError::InvalidIndex {
                what,
                value: u64::from(*value),
                limit: u64::from(limit),
            });
        }
    }
    Ok(())
}

/// Validates one settings block's value ranges, normalization, and KMC-only fields.
pub(crate) struct SettingsCheck {
    /// A display prefix such as `"simulations.default_settings"`.
    pub what: &'static str,
    /// The owning simulation kind.
    pub kind: SimulationKind,
    /// The temperature in kelvin.
    pub temperature_k: f64,
    /// The target MCSP.
    pub target_mcsp: u64,
    /// The KMC pre-run MCSP.
    pub pre_run_mcsp: u64,
    /// The wall-clock limit.
    pub time_limit_s: Option<u64>,
    /// The minimum success rate.
    pub min_success_rate: Option<f64>,
    /// The electric-field modulus in V/m.
    pub electric_field_vm: f64,
    /// The normalized field direction.
    pub electric_field_direction: [f64; 3],
    /// The normalization energy.
    pub normalization_energy_ev: Option<f64>,
    /// The maximum attempt frequency.
    pub max_attempt_frequency_hz: Option<f64>,
    /// The MMC break tolerance.
    pub break_tolerance: Option<f64>,
    /// The MMC break sample length.
    pub break_sample_length_mcsp: Option<u64>,
    /// The MMC break sample interval.
    pub break_sample_interval_mcsp: Option<u64>,
    /// The result sampling interval.
    pub result_sample_mcsp: Option<u64>,
    /// The authored flag bits, when the block is a simulation default.
    pub authored_flags: Option<u32>,
}

impl SettingsCheck {
    /// Runs every documented settings check.
    pub(crate) fn validate(&self) -> Result<(), PackageError> {
        let what = self.what;

        require_finite(what, self.temperature_k)?;
        if self.temperature_k <= 0.0 {
            return Err(invalid(what, "temperature_k must be positive"));
        }
        if self.target_mcsp == 0 {
            return Err(invalid(what, "target_mcsp must be positive"));
        }
        if self.time_limit_s == Some(0) {
            return Err(invalid(what, "time_limit_s must be positive when present"));
        }
        if let Some(rate) = self.min_success_rate {
            require_finite(what, rate)?;
            if rate < 0.0 {
                return Err(invalid(what, "min_success_rate must be non-negative"));
            }
        }
        require_finite(what, self.electric_field_vm)?;
        if self.electric_field_vm < 0.0 {
            return Err(invalid(what, "electric_field_vm must be non-negative"));
        }
        require_finite3(what, self.electric_field_direction)?;
        if self.electric_field_vm > 0.0 {
            let norm = norm(self.electric_field_direction);
            if (norm - 1.0).abs() > 1e-9 {
                return Err(invalid(
                    what,
                    "the electric-field direction is not a unit vector",
                ));
            }
        }
        for (name, value) in [
            ("normalization_energy_ev", self.normalization_energy_ev),
            ("break_tolerance", self.break_tolerance),
        ] {
            if let Some(value) = value {
                require_finite(name, value)?;
            }
        }
        if let Some(frequency) = self.max_attempt_frequency_hz {
            require_finite(what, frequency)?;
            if frequency <= 0.0 {
                return Err(invalid(
                    what,
                    "max_attempt_frequency_hz must be positive when present",
                ));
            }
        }
        if let Some(tolerance) = self.break_tolerance
            && tolerance < 0.0
        {
            return Err(invalid(what, "break_tolerance must be non-negative"));
        }

        if matches!(self.kind, SimulationKind::Mmc) && self.pre_run_mcsp != 0 {
            return Err(invalid(what, "pre_run_mcsp must be zero for MMC"));
        }

        if matches!(self.kind, SimulationKind::Kmc) {
            if self.break_tolerance.is_some_and(|value| value != 0.0) {
                return Err(invalid(what, "break fields are MMC-only"));
            }
            for value in [
                self.break_sample_length_mcsp,
                self.break_sample_interval_mcsp,
                self.result_sample_mcsp,
            ] {
                if value.is_some_and(|value| value != 0) {
                    return Err(invalid(what, "break fields are MMC-only"));
                }
            }
        }

        if let Some(flags) = self.authored_flags
            && flags & !SolverJobFlags::AUTHORED_BITS_MASK != 0
        {
            return Err(invalid(
                what,
                "simulation default flags may contain authored bits 0-3 only",
            ));
        }

        Ok(())
    }
}

fn invalid(what: &'static str, detail: &str) -> PackageError {
    PackageError::InvalidValue {
        what,
        detail: detail.to_owned(),
    }
}

/// Returns the Euclidean norm of a 3-vector.
#[must_use]
pub(crate) fn norm(value: [f64; 3]) -> f64 {
    (value[0] * value[0] + value[1] * value[1] + value[2] * value[2]).sqrt()
}

/// Validates the scientific invariants of the mapped model (Step 13 design §3.5, M-2).
pub(crate) fn validate_scientific(model: &ParameterizedModel) -> Result<(), PackageError> {
    let particle_count = model.counts.particle_count;
    let collection_count = model.counts.collection_count as usize;

    // Consistency of the collection lists and the per-collection path length.
    let mut listed_directions: Vec<Vec<u32>> = vec![Vec::new(); collection_count];
    for (index, direction) in model.transitions.directions.iter().enumerate() {
        let collection = direction.collection_id.get() as usize;
        if collection >= collection_count {
            return Err(PackageError::InvalidIndex {
                what: "jump_directions.collection_id",
                value: u64::from(direction.collection_id.get()),
                limit: collection_count as u64,
            });
        }
        listed_directions[collection].push(index as u32);
    }

    let mut listed_rules: Vec<Vec<u32>> = vec![Vec::new(); collection_count];
    for (index, rule) in model.transitions.rules.iter().enumerate() {
        let collection = rule.collection_id.get() as usize;
        if collection >= collection_count {
            return Err(PackageError::InvalidIndex {
                what: "jump_rules.collection_id",
                value: u64::from(rule.collection_id.get()),
                limit: collection_count as u64,
            });
        }
        listed_rules[collection].push(index as u32);
    }

    let mut collection_path_lengths: Vec<usize> = vec![0; collection_count];
    for (index, collection) in model.transitions.collections.iter().enumerate() {
        let mut expected_directions: Vec<u32> = collection
            .direction_ids
            .iter()
            .map(|direction| direction.get())
            .collect();
        expected_directions.sort_unstable();
        let mut actual_directions = listed_directions[index].clone();
        actual_directions.sort_unstable();
        if expected_directions != actual_directions || actual_directions.is_empty() {
            return Err(invalid(
                "transitions.jump_collections",
                "a collection must own exactly its non-empty direction list",
            ));
        }

        let mut expected_rules: Vec<u32> =
            collection.rule_ids.iter().map(|rule| rule.get()).collect();
        expected_rules.sort_unstable();
        let mut actual_rules = listed_rules[index].clone();
        actual_rules.sort_unstable();
        if expected_rules != actual_rules {
            return Err(invalid(
                "transitions.jump_collections",
                "a collection must own exactly its rule list",
            ));
        }

        let path_length = model.transitions.directions[expected_directions[0] as usize]
            .path
            .len();
        for direction_id in &expected_directions {
            let direction = &model.transitions.directions[*direction_id as usize];
            if direction.path.len() != path_length {
                return Err(invalid(
                    "transitions.jump_directions",
                    "directions in one collection must have the same path length",
                ));
            }
        }
        collection_path_lengths[index] = path_length;
    }

    // Per-node union of the direction masks, per collection.
    let union_masks: Vec<Vec<u64>> = (0..collection_count)
        .map(|collection| {
            let path_length = collection_path_lengths[collection];
            let mut unions = vec![0_u64; path_length];
            for direction_id in &model.transitions.collections[collection].direction_ids {
                let direction = &model.transitions.directions[direction_id.get() as usize];
                for (node, cell) in direction.path.iter().enumerate() {
                    let position = cell[3] as usize;
                    unions[node] |= model.structure.positions[position].allowed_mask;
                }
            }
            unions
        })
        .collect();

    for (index, rule) in model.transitions.rules.iter().enumerate() {
        let collection = rule.collection_id.get() as usize;
        let path_length = collection_path_lengths[collection];
        let kind = model.transitions.collections[collection].kind;

        if rule.rule_id.get() != index as u32 {
            return Err(invalid(
                "transitions.jump_rules.rule_id",
                "rule_id must equal the dense rule index",
            ));
        }

        for (_name, state) in [
            ("state0", rule.state0),
            ("state1", rule.state1),
            ("state2", rule.state2),
        ] {
            for node in 0..path_length {
                if u32::from(state.node(node)) >= particle_count {
                    return Err(PackageError::InvalidIndex {
                        what: "transitions.jump_rules state node",
                        value: u64::from(state.node(node)),
                        limit: u64::from(particle_count),
                    });
                }
            }
            if !state.has_clear_bits_above(path_length) {
                return Err(invalid(
                    "transitions.jump_rules state",
                    "state bits above the collection path length must be zero",
                ));
            }
        }

        if matches!(kind, SimulationKind::Mmc) && rule.state1.get() != 0 {
            return Err(invalid(
                "transitions.jump_rules.state1",
                "state1 must be zero for MMC",
            ));
        }

        if rule.tracker_order.len() != path_length {
            return Err(PackageError::LengthMismatch {
                what: "transitions.jump_rules.tracker_order",
                expected: path_length as u64,
                found: rule.tracker_order.len() as u64,
            });
        }
        for tracker in &rule.tracker_order {
            if *tracker != 0xFF && u32::from(*tracker) >= model.counts.global_tracker_count {
                return Err(PackageError::InvalidIndex {
                    what: "transitions.jump_rules.tracker_order",
                    value: u64::from(*tracker),
                    limit: u64::from(model.counts.global_tracker_count),
                });
            }
        }

        // M-3: void is exempt; every non-void state particle must be in the collection union at the same node.
        for (name, state) in [
            ("state0", rule.state0),
            ("state1", rule.state1),
            ("state2", rule.state2),
        ] {
            for (node, union) in union_masks[collection].iter().enumerate() {
                let particle = u32::from(state.node(node));
                if particle == 0 {
                    continue;
                }
                if union & (1_u64 << particle) == 0 {
                    return Err(invalid(
                        "transitions.jump_rules state",
                        &format!(
                            "{name} node {node} particle {particle} is not admitted by any direction of the collection"
                        ),
                    ));
                }
            }
        }

        // Mass conservation: the non-void symbol multiset is unchanged.
        let mut start_symbols: Vec<&str> = Vec::new();
        let mut end_symbols: Vec<&str> = Vec::new();
        for node in 0..path_length {
            let start = u32::from(rule.state0.node(node)) as usize;
            let end = u32::from(rule.state2.node(node)) as usize;
            if start != 0 {
                start_symbols.push(model.particle.symbols[start].as_str());
            }
            if end != 0 {
                end_symbols.push(model.particle.symbols[end].as_str());
            }
        }
        start_symbols.sort_unstable();
        end_symbols.sort_unstable();
        if start_symbols != end_symbols {
            return Err(invalid(
                "transitions.jump_rules",
                "the state0/state2 non-void particle symbols are not conserved",
            ));
        }

        let start_charge: f64 = (0..path_length)
            .map(|node| model.particle.charges[u32::from(rule.state0.node(node)) as usize])
            .sum();
        let end_charge: f64 = (0..path_length)
            .map(|node| model.particle.charges[u32::from(rule.state2.node(node)) as usize])
            .sum();
        if (start_charge - end_charge).abs() > 1e-9 {
            return Err(invalid(
                "transitions.jump_rules",
                "the state0/state2 charges are not conserved within 1e-9",
            ));
        }

        let mut any_charge_change = false;
        for node in 0..path_length {
            let start = u32::from(rule.state0.node(node)) as usize;
            let end = u32::from(rule.state2.node(node)) as usize;
            if model.particle.charges[start] != model.particle.charges[end] {
                any_charge_change = true;
                break;
            }
        }
        require_finite3(
            "transitions.jump_rules.charge_transport_vector",
            rule.charge_transport_vector,
        )?;
        if !any_charge_change && norm(rule.charge_transport_vector) > 1e-9 {
            return Err(invalid(
                "transitions.jump_rules.charge_transport_vector",
                "a rule without a charge change must have a zero transport vector",
            ));
        }
    }

    // Movement, path vector, and length.
    for direction in &model.transitions.directions {
        if direction.path.len() != direction.movement.len() {
            return Err(PackageError::LengthMismatch {
                what: "jump_directions.movement",
                expected: direction.path.len() as u64,
                found: direction.movement.len() as u64,
            });
        }
        let start = direction.path[0];
        if start != [0, 0, 0, direction.position_id as i32] {
            return Err(invalid(
                "jump_directions.path",
                "node zero must be [0, 0, 0, position_id]",
            ));
        }

        require_finite3("jump_directions.path_vector", direction.path_vector)?;
        require_finite("jump_directions.length", direction.length)?;
        if direction.length < 0.0 {
            return Err(invalid(
                "jump_directions.length",
                "the length must be non-negative",
            ));
        }

        let mut expected = [0.0_f64; 3];
        for movement in &direction.movement {
            require_finite3("jump_directions.movement", *movement)?;
            for (axis, value) in expected.iter_mut().enumerate() {
                *value += movement[0] * model.structure.cell_vectors_ang[0][axis]
                    + movement[1] * model.structure.cell_vectors_ang[1][axis]
                    + movement[2] * model.structure.cell_vectors_ang[2][axis];
            }
        }
        for (axis, (actual, declared)) in expected.iter().zip(&direction.path_vector).enumerate() {
            let tolerance = 1e-9 * declared.abs().max(1.0);
            if (actual - declared).abs() > tolerance {
                return Err(invalid(
                    "jump_directions.path_vector",
                    &format!("component {axis} does not match the movement sum"),
                ));
            }
        }
        let expected_length = norm(direction.path_vector);
        let tolerance = 1e-9 * expected_length.max(1.0);
        if (direction.length - expected_length).abs() > tolerance {
            return Err(invalid(
                "jump_directions.length",
                "the length does not match the path-vector norm",
            ));
        }
    }

    // Jump tables: sums, ranges, and per-bucket duplicates.
    let directions = model.transitions.directions.len() as u32;
    if model.transitions.jump_offsets.len() != model.transitions.jump_count.len() + 1 {
        return Err(PackageError::LengthMismatch {
            what: "transitions.jump_offsets",
            expected: model.transitions.jump_count.len() as u64 + 1,
            found: model.transitions.jump_offsets.len() as u64,
        });
    }

    let mut expected_offset = 0_u64;
    for (bucket, count) in model.transitions.jump_count.iter().enumerate() {
        let start = model.transitions.jump_offsets[bucket] as usize;
        let end = start + *count as usize;
        if end > model.transitions.jump_mapping.len() {
            return Err(invalid(
                "transitions.jump_mapping",
                "a jump bucket extends beyond the mapping array",
            ));
        }

        let mut seen = HashSet::new();
        for direction in &model.transitions.jump_mapping[start..end] {
            if *direction >= directions {
                return Err(PackageError::InvalidIndex {
                    what: "transitions.jump_mapping",
                    value: u64::from(*direction),
                    limit: u64::from(directions),
                });
            }
            if !seen.insert(*direction) {
                return Err(invalid(
                    "transitions.jump_mapping",
                    "a jump bucket contains the same direction twice",
                ));
            }
        }

        expected_offset += u64::from(*count);
        if expected_offset > u64::from(u32::MAX) {
            return Err(invalid(
                "transitions.jump_count",
                "the jump-count prefix sum exceeds the 32-bit offset space",
            ));
        }
        if model.transitions.jump_offsets[bucket + 1] as u64 != expected_offset {
            return Err(invalid(
                "transitions.jump_offsets",
                "the offsets are not the prefix sums of jump_count",
            ));
        }
    }
    if expected_offset != model.transitions.jump_mapping.len() as u64 {
        return Err(PackageError::LengthMismatch {
            what: "transitions.jump_mapping",
            expected: expected_offset,
            found: model.transitions.jump_mapping.len() as u64,
        });
    }

    for value in &model.transitions.static_tracker_mapping {
        if *value != -1
            && !u32::try_from(*value)
                .is_ok_and(|value| value < model.counts.static_trackers_per_cell)
        {
            return Err(PackageError::InvalidIndex {
                what: "transitions.static_tracker_mapping",
                value: value.unsigned_abs() as u64,
                limit: u64::from(model.counts.static_trackers_per_cell),
            });
        }
    }
    for value in &model.transitions.global_tracker_mapping {
        if *value != -1
            && !u32::try_from(*value).is_ok_and(|value| value < model.counts.global_tracker_count)
        {
            return Err(PackageError::InvalidIndex {
                what: "transitions.global_tracker_mapping",
                value: value.unsigned_abs() as u64,
                limit: u64::from(model.counts.global_tracker_count),
            });
        }
    }

    Ok(())
}
