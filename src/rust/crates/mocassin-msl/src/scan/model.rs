// SPDX-License-Identifier: MIT

//! The model-payload schema walk.

use mocassin_types::ReaderLimits;

use super::facts::{ClusterFacts, ScanFacts, validate_model};
use super::{
    Cursor, check_cap, kind_name, scan_fixed_matrix, scan_map, scan_map_optional, skip_array,
};
use crate::error::PackageError;

const MODEL_KEYS: &[&str] = &[
    "format_version",
    "signature",
    "model_signature",
    "symmetry_contract_version",
    "space_group_data_version",
    "space_group_data_sha256",
    "particle",
    "structure",
    "energy",
    "transitions",
    "simulations",
];

const PARTICLE_KEYS: &[&str] = &["charges", "symbols", "is_vacancy", "count"];

const STRUCTURE_KEYS: &[&str] = &[
    "cell_vectors_ang",
    "positions",
    "position_count",
    "site_count",
    "environments",
    "interaction_range",
    "static_trackers_per_cell",
    "global_tracker_count",
];

const POSITION_KEYS: &[&str] = &["vector4", "site_index", "stable", "particle_ids"];

const ENVIRONMENT_KEYS: &[&str] = &[
    "selection_particle_mask",
    "pairs",
    "clusters",
    "update_particle_ids",
];

const PAIR_LINK_KEYS: &[&str] = &["target_position", "offset", "energy_id"];

const CLUSTER_LINK_KEYS: &[&str] = &["cluster_id", "byte_position"];

const ENERGY_KEYS: &[&str] = &["pair_tables", "cluster_tables", "defect_background"];

const PAIR_TABLE_KEYS: &[&str] = &["object_id", "energies"];

const CLUSTER_TABLE_KEYS: &[&str] = &[
    "object_id",
    "occupation_codes",
    "energies",
    "particle_table_mapping",
];

const CLUSTER_TABLE_OPTIONAL_KEYS: &[&str] = &["code_expansions"];

const CODE_EXPANSION_KEYS: &[&str] = &["expanded_codes", "canonical_indices"];

const TRANSITION_KEYS: &[&str] = &[
    "jump_collections",
    "jump_directions",
    "jump_rules",
    "jump_count",
    "jump_mapping",
    "static_tracker_mapping",
    "global_tracker_mapping",
];

const COLLECTION_KEYS: &[&str] = &[
    "kind",
    "transition_id",
    "mobile_particle_mask",
    "rule_ids",
    "direction_ids",
];

const DIRECTION_KEYS: &[&str] = &[
    "object_id",
    "position_id",
    "collection_id",
    "path",
    "movement",
    "path_vector",
    "length",
    "is_inverse",
];

const RULE_KEYS: &[&str] = &[
    "rule_id",
    "collection_id",
    "state0",
    "state1",
    "state2",
    "frequency_hz",
    "charge_transport_vector",
    "static_correction",
    "tracker_order",
];

const SIMULATION_KEYS: &[&str] = &[
    "id",
    "kind",
    "jump_collection_ids",
    "default_settings",
    "tracker_models",
];

const SETTINGS_KEYS: &[&str] = &[
    "temperature_k",
    "target_mcsp",
    "write_out_count",
    "pre_run_mcsp",
    "time_limit_s",
    "min_success_rate",
    "electric_field_vm",
    "electric_field_direction",
    "normalization_energy_ev",
    "max_attempt_frequency_hz",
    "break_tolerance",
    "break_sample_length_mcsp",
    "break_sample_interval_mcsp",
    "result_sample_mcsp",
    "flags",
];

const TRACKER_KEYS: &[&str] = &["global_tracker_indices", "static_tracker_indices"];

/// Scans a decompressed model payload and cross-checks its section lengths.
pub(crate) fn scan_model(payload: &[u8], limits: &ReaderLimits) -> Result<ScanFacts, PackageError> {
    let mut cursor = Cursor::new(
        payload,
        limits.max_model_elements,
        limits.max_messagepack_depth,
        "model",
    );
    let mut facts = ScanFacts::default();

    scan_map(&mut cursor, MODEL_KEYS, |cursor, key| match key {
        "format_version" => {
            facts.format_version = Some(cursor.read_u64()?);
            Ok(())
        }
        "signature"
        | "model_signature"
        | "symmetry_contract_version"
        | "space_group_data_version"
        | "space_group_data_sha256" => {
            let _ = cursor.read_str()?;
            Ok(())
        }
        "particle" => scan_particle(cursor, &mut facts, limits),
        "structure" => scan_structure(cursor, &mut facts, limits),
        "energy" => scan_energy(cursor, &mut facts, limits),
        "transitions" => scan_transitions(cursor, &mut facts, limits),
        "simulations" => scan_simulations(cursor, &mut facts, limits),
        _ => unreachable!("the schema map only yields known keys"),
    })?;

    validate_model(&facts)?;
    cursor.finish()?;
    Ok(facts)
}

fn scan_particle(
    cursor: &mut Cursor<'_>,
    facts: &mut ScanFacts,
    limits: &ReaderLimits,
) -> Result<(), PackageError> {
    scan_map(cursor, PARTICLE_KEYS, |cursor, key| match key {
        "charges" => {
            facts.particle_lengths[0] = Some(skip_array(cursor)?);
            Ok(())
        }
        "symbols" => {
            facts.particle_lengths[1] = Some(skip_array(cursor)?);
            Ok(())
        }
        "is_vacancy" => {
            facts.particle_lengths[2] = Some(skip_array(cursor)?);
            Ok(())
        }
        "count" => {
            let count = u64::from(cursor.read_u32()?);
            check_cap(
                cursor,
                "particle_count",
                count,
                u64::from(limits.max_particles),
            )?;
            facts.particle_count = Some(count);
            Ok(())
        }
        _ => unreachable!(),
    })
}

fn scan_structure(
    cursor: &mut Cursor<'_>,
    facts: &mut ScanFacts,
    limits: &ReaderLimits,
) -> Result<(), PackageError> {
    scan_map(cursor, STRUCTURE_KEYS, |cursor, key| match key {
        "cell_vectors_ang" => scan_fixed_matrix(cursor, 3, 3),
        "positions" => {
            let length = cursor.read_array_len()?;
            check_cap(cursor, "positions", length, u64::from(limits.max_positions))?;
            for _ in 0..length {
                scan_position(cursor)?;
            }
            cursor.leave();
            facts.positions_len = Some(length);
            Ok(())
        }
        "position_count" => {
            let count = u64::from(cursor.read_u32()?);
            check_cap(
                cursor,
                "position_count",
                count,
                u64::from(limits.max_position_count),
            )?;
            facts.position_count = Some(count);
            Ok(())
        }
        "site_count" => {
            facts.site_count = Some(u64::from(cursor.read_u32()?));
            Ok(())
        }
        "environments" => {
            let length = cursor.read_array_len()?;
            check_cap(
                cursor,
                "environments",
                length,
                u64::from(limits.max_positions),
            )?;
            for _ in 0..length {
                scan_environment(cursor, facts, limits)?;
            }
            cursor.leave();
            facts.environments_len = Some(length);
            Ok(())
        }
        "interaction_range" => scan_fixed_value_array(cursor, 4),
        "static_trackers_per_cell" | "global_tracker_count" => {
            let _ = cursor.read_u32()?;
            Ok(())
        }
        _ => unreachable!(),
    })
}

fn scan_position(cursor: &mut Cursor<'_>) -> Result<(), PackageError> {
    scan_map(cursor, POSITION_KEYS, |cursor, key| match key {
        "vector4" => scan_fixed_value_array(cursor, 4),
        "site_index" => {
            let _ = cursor.read_u32()?;
            Ok(())
        }
        "stable" => cursor.skip_value(),
        "particle_ids" => {
            let _ = skip_array(cursor)?;
            Ok(())
        }
        _ => unreachable!(),
    })
}

fn scan_environment(
    cursor: &mut Cursor<'_>,
    facts: &mut ScanFacts,
    limits: &ReaderLimits,
) -> Result<(), PackageError> {
    scan_map(cursor, ENVIRONMENT_KEYS, |cursor, key| match key {
        "selection_particle_mask" => {
            let _ = cursor.read_u64()?;
            Ok(())
        }
        "pairs" => {
            let length = cursor.read_array_len()?;
            check_cap(
                cursor,
                "pair links per position",
                length,
                u64::from(limits.max_pair_links_per_position),
            )?;
            facts.pair_links_total = facts
                .pair_links_total
                .checked_add(length)
                .ok_or_else(|| cursor.scan_error("pair-link total overflows"))?;
            check_cap(
                cursor,
                "pair-link total",
                facts.pair_links_total,
                u64::from(limits.max_pair_links),
            )?;
            for _ in 0..length {
                scan_pair_link(cursor)?;
            }
            cursor.leave();
            Ok(())
        }
        "clusters" => {
            let length = cursor.read_array_len()?;
            check_cap(
                cursor,
                "cluster links per position",
                length,
                u64::from(limits.max_cluster_links_per_position),
            )?;
            for _ in 0..length {
                scan_cluster_link(cursor)?;
            }
            cursor.leave();
            Ok(())
        }
        "update_particle_ids" => {
            let _ = skip_array(cursor)?;
            Ok(())
        }
        _ => unreachable!(),
    })
}

fn scan_pair_link(cursor: &mut Cursor<'_>) -> Result<(), PackageError> {
    scan_map(cursor, PAIR_LINK_KEYS, |cursor, key| match key {
        "target_position" | "energy_id" => {
            let _ = cursor.read_u32()?;
            Ok(())
        }
        "offset" => scan_fixed_value_array(cursor, 3),
        _ => unreachable!(),
    })
}

fn scan_cluster_link(cursor: &mut Cursor<'_>) -> Result<(), PackageError> {
    scan_map(cursor, CLUSTER_LINK_KEYS, |cursor, key| match key {
        "cluster_id" => {
            let _ = cursor.read_u32()?;
            Ok(())
        }
        "byte_position" => cursor.skip_value(),
        _ => unreachable!(),
    })
}

fn scan_energy(
    cursor: &mut Cursor<'_>,
    facts: &mut ScanFacts,
    limits: &ReaderLimits,
) -> Result<(), PackageError> {
    scan_map(cursor, ENERGY_KEYS, |cursor, key| match key {
        "pair_tables" => {
            let length = cursor.read_array_len()?;
            check_cap(
                cursor,
                "pair tables",
                length,
                u64::from(limits.max_pair_tables),
            )?;
            for _ in 0..length {
                let mut energies = 0;
                scan_map(cursor, PAIR_TABLE_KEYS, |cursor, key| match key {
                    "object_id" => {
                        let _ = cursor.read_str()?;
                        Ok(())
                    }
                    "energies" => {
                        energies = skip_array(cursor)?;
                        Ok(())
                    }
                    _ => unreachable!(),
                })?;
                facts.pair_table_energy_lengths.push(energies);
            }
            cursor.leave();
            facts.pair_table_count = Some(length);
            Ok(())
        }
        "cluster_tables" => {
            let length = cursor.read_array_len()?;
            check_cap(
                cursor,
                "cluster tables",
                length,
                u64::from(limits.max_cluster_tables),
            )?;
            for table_index in 0..length {
                let mut table = ClusterFacts::default();
                let result = scan_map_optional(
                    cursor,
                    CLUSTER_TABLE_KEYS,
                    CLUSTER_TABLE_OPTIONAL_KEYS,
                    |cursor, key| match key {
                        "object_id" => {
                            let _ = cursor.read_str()?;
                            Ok(())
                        }
                        "occupation_codes" => {
                            table.codes = skip_array(cursor)?;
                            Ok(())
                        }
                        "energies" => {
                            table.energies = skip_array(cursor)?;
                            Ok(())
                        }
                        "particle_table_mapping" => {
                            table.mapping = skip_array(cursor)?;
                            Ok(())
                        }
                        "code_expansions" => scan_code_expansions(
                            cursor,
                            table_index,
                            &mut table,
                            &mut facts.cluster_expansion_bytes,
                            limits,
                        ),
                        _ => unreachable!(),
                    },
                );
                result?;
                facts.cluster_table_lengths.push(table);
            }
            cursor.leave();
            facts.cluster_table_count = Some(length);
            Ok(())
        }
        "defect_background" => {
            facts.defect_background_len = Some(skip_array(cursor)?);
            Ok(())
        }
        _ => unreachable!(),
    })
}

fn scan_code_expansions(
    cursor: &mut Cursor<'_>,
    table: u64,
    cluster: &mut ClusterFacts,
    total: &mut u64,
    limits: &ReaderLimits,
) -> Result<(), PackageError> {
    if cursor.read_nil_marker()? {
        cluster.expansions = None;
        return Ok(());
    }

    let mut expanded_codes = None;
    let mut canonical_indices = None;
    let result = scan_map(cursor, CODE_EXPANSION_KEYS, |cursor, key| match key {
        "expanded_codes" => {
            expanded_codes = Some(skip_array(cursor)?);
            Ok(())
        }
        "canonical_indices" => {
            canonical_indices = Some(skip_array(cursor)?);
            Ok(())
        }
        _ => unreachable!(),
    });
    result.map_err(|error| annotate_expansion_error(error, table))?;

    let expanded_codes = expanded_codes.expect("scan_map requires expanded_codes");
    let canonical_indices = canonical_indices.expect("scan_map requires canonical_indices");
    if expanded_codes != canonical_indices {
        return Err(expansion_field_error(
            table,
            format!(
                "expanded_codes has {expanded_codes} entries but canonical_indices has {canonical_indices}"
            ),
        ));
    }

    let cost = expanded_codes
        .checked_mul(8)
        .and_then(|code_bytes| {
            canonical_indices
                .checked_mul(4)
                .and_then(|index_bytes| code_bytes.checked_add(index_bytes))
        })
        .ok_or_else(|| {
            expansion_field_error(table, "the typed size overflows a 64-bit value".to_owned())
        })?;
    *total = total.checked_add(cost).ok_or_else(|| {
        expansion_field_error(table, "the cumulative typed size overflows".to_owned())
    })?;
    if *total > limits.max_cluster_expansion_bytes {
        return Err(PackageError::PayloadTooLarge {
            section: "model cluster expansion",
            declared: *total,
            limit: limits.max_cluster_expansion_bytes,
        });
    }

    cluster.expansions = Some((expanded_codes, canonical_indices));
    Ok(())
}

fn expansion_field_error(table: u64, detail: String) -> PackageError {
    PackageError::Scan {
        section: "model",
        detail: format!("energy.cluster_tables[{table}].code_expansions: {detail}"),
    }
}

fn annotate_expansion_error(error: PackageError, table: u64) -> PackageError {
    match error {
        PackageError::Scan { section, detail } => PackageError::Scan {
            section,
            detail: format!("energy.cluster_tables[{table}].code_expansions: {detail}"),
        },
        other => other,
    }
}

fn scan_transitions(
    cursor: &mut Cursor<'_>,
    facts: &mut ScanFacts,
    limits: &ReaderLimits,
) -> Result<(), PackageError> {
    scan_map(cursor, TRANSITION_KEYS, |cursor, key| match key {
        "jump_collections" => {
            let length = cursor.read_array_len()?;
            check_cap(
                cursor,
                "jump collections",
                length,
                u64::from(limits.max_collections),
            )?;
            for _ in 0..length {
                let mut kind = None;
                scan_map(cursor, COLLECTION_KEYS, |cursor, key| match key {
                    "kind" => {
                        kind = kind_name(cursor.read_str()?);
                        Ok(())
                    }
                    "transition_id" => {
                        let _ = cursor.read_str()?;
                        Ok(())
                    }
                    "mobile_particle_mask" => {
                        let _ = cursor.read_u64()?;
                        Ok(())
                    }
                    "rule_ids" | "direction_ids" => {
                        let _ = skip_array(cursor)?;
                        Ok(())
                    }
                    _ => unreachable!(),
                })?;
                facts.collection_kinds.push(kind);
            }
            cursor.leave();
            facts.collection_count = Some(length);
            Ok(())
        }
        "jump_directions" => {
            let length = cursor.read_array_len()?;
            check_cap(
                cursor,
                "jump directions",
                length,
                u64::from(limits.max_directions),
            )?;
            for _ in 0..length {
                scan_direction(cursor, facts, limits)?;
            }
            cursor.leave();
            facts.direction_count = Some(length);
            Ok(())
        }
        "jump_rules" => {
            let length = cursor.read_array_len()?;
            check_cap(cursor, "jump rules", length, u64::from(limits.max_rules))?;
            for _ in 0..length {
                scan_rule(cursor, facts, limits)?;
            }
            cursor.leave();
            facts.rule_count = Some(length);
            Ok(())
        }
        "jump_count" => {
            facts.jump_count_len = Some(skip_array(cursor)?);
            Ok(())
        }
        "jump_mapping" => {
            facts.jump_mapping_len = Some(skip_array(cursor)?);
            Ok(())
        }
        "static_tracker_mapping" => {
            facts.static_tracker_mapping_len = Some(skip_array(cursor)?);
            Ok(())
        }
        "global_tracker_mapping" => {
            facts.global_tracker_mapping_len = Some(skip_array(cursor)?);
            Ok(())
        }
        _ => unreachable!(),
    })
}

fn scan_direction(
    cursor: &mut Cursor<'_>,
    facts: &mut ScanFacts,
    limits: &ReaderLimits,
) -> Result<(), PackageError> {
    let mut collection = 0_u32;
    let mut path_length = 0_u64;
    let mut movement_length = 0_u64;

    scan_map(cursor, DIRECTION_KEYS, |cursor, key| match key {
        "object_id" => {
            let _ = cursor.read_str()?;
            Ok(())
        }
        "position_id" => {
            let _ = cursor.read_u32()?;
            Ok(())
        }
        "collection_id" => {
            collection = cursor.read_u32()?;
            Ok(())
        }
        "path" => {
            let length = cursor.read_array_len()?;
            check_cap(
                cursor,
                "path nodes",
                length,
                u64::from(limits.max_path_nodes),
            )?;
            for _ in 0..length {
                scan_fixed_value_array(cursor, 4)?;
            }
            cursor.leave();
            path_length = length;
            Ok(())
        }
        "movement" => {
            let length = cursor.read_array_len()?;
            check_cap(
                cursor,
                "movement nodes",
                length,
                u64::from(limits.max_path_nodes),
            )?;
            for _ in 0..length {
                scan_fixed_value_array(cursor, 3)?;
            }
            cursor.leave();
            movement_length = length;
            Ok(())
        }
        "path_vector" => scan_fixed_value_array(cursor, 3),
        "length" | "is_inverse" => cursor.skip_value(),
        _ => unreachable!(),
    })?;

    facts.direction_collections.push(collection);
    facts.direction_path_lengths.push(path_length);
    facts.direction_movement_lengths.push(movement_length);
    Ok(())
}

fn scan_rule(
    cursor: &mut Cursor<'_>,
    facts: &mut ScanFacts,
    limits: &ReaderLimits,
) -> Result<(), PackageError> {
    let mut collection = 0_u32;
    let mut tracker_length = 0_u64;

    scan_map(cursor, RULE_KEYS, |cursor, key| match key {
        "rule_id" => {
            let _ = cursor.read_u32()?;
            Ok(())
        }
        "collection_id" => {
            collection = cursor.read_u32()?;
            Ok(())
        }
        "state0" | "state1" | "state2" => {
            let _ = cursor.read_u64()?;
            Ok(())
        }
        "frequency_hz" => cursor.skip_value(),
        "charge_transport_vector" => scan_fixed_value_array(cursor, 3),
        "static_correction" => cursor.skip_value(),
        "tracker_order" => {
            let length = skip_array(cursor)?;
            check_cap(
                cursor,
                "tracker order entries",
                length,
                u64::from(limits.max_path_nodes),
            )?;
            tracker_length = length;
            Ok(())
        }
        _ => unreachable!(),
    })?;

    facts.rule_collections.push(collection);
    facts.rule_tracker_lengths.push(tracker_length);
    Ok(())
}

fn scan_simulations(
    cursor: &mut Cursor<'_>,
    facts: &mut ScanFacts,
    limits: &ReaderLimits,
) -> Result<(), PackageError> {
    let length = cursor.read_array_len()?;
    check_cap(
        cursor,
        "simulations",
        length,
        u64::from(limits.max_simulations),
    )?;

    for _ in 0..length {
        let mut kind = None;
        let mut used = Vec::new();
        scan_map(cursor, SIMULATION_KEYS, |cursor, key| match key {
            "id" => {
                let _ = cursor.read_str()?;
                Ok(())
            }
            "kind" => {
                kind = kind_name(cursor.read_str()?);
                Ok(())
            }
            "jump_collection_ids" => {
                let ids = cursor.read_array_len()?;
                check_cap(
                    cursor,
                    "simulation collection ids",
                    ids,
                    u64::from(limits.max_collections),
                )?;
                used.reserve(usize::try_from(ids).unwrap_or(0));
                for _ in 0..ids {
                    used.push(cursor.read_u32()?);
                }
                cursor.leave();
                Ok(())
            }
            "default_settings" => scan_settings(cursor),
            "tracker_models" => scan_tracker_models(cursor),
            _ => unreachable!(),
        })?;
        facts.simulation_kinds.push(kind);
        facts.simulation_collections.push(used);
    }

    cursor.leave();
    facts.simulation_count = Some(length);
    Ok(())
}

fn scan_settings(cursor: &mut Cursor<'_>) -> Result<(), PackageError> {
    scan_map(cursor, SETTINGS_KEYS, |cursor, _key| cursor.skip_value())
}

fn scan_tracker_models(cursor: &mut Cursor<'_>) -> Result<(), PackageError> {
    scan_map(cursor, TRACKER_KEYS, |cursor, _key| {
        let _ = skip_array(cursor)?;
        Ok(())
    })
}

/// Reads an array header of exactly `expected` values and skips the values.
fn scan_fixed_value_array(cursor: &mut Cursor<'_>, expected: u64) -> Result<(), PackageError> {
    let length = cursor.read_array_len()?;
    if length != expected {
        return Err(cursor.scan_error(format!(
            "expected an array of {expected} values, found {length}"
        )));
    }
    cursor.skip_n(length)?;
    cursor.leave();
    Ok(())
}
