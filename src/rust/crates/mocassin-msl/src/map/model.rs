// SPDX-License-Identifier: MIT

//! The model-payload mapping: particles, structure, energy, and the section cross-checks.

use mocassin_types::payload::{EnergyPayload, ModelPayload, ParticlePayload, StructurePayload};
use mocassin_types::{
    ClusterLink, ClusterTable, EnergyModel, EnvironmentModel, ModelCounts, PairLink, PairTable,
    ParameterizedModel, ParticleTable, Position, PositionId, ReaderLimits, StructureModel,
};

use crate::error::PackageError;
use crate::package::SchemaInfo;
use crate::validate::{
    require_ascending_below, require_finite, require_hex64, require_mask_bits,
    require_strictly_ascending, validate_scientific,
};

use super::job::map_simulations;
use super::transition::map_transitions;
use super::u32_len;

/// Maps a decoded model payload onto the validated runtime model.
pub(crate) fn model_from_payload(
    payload: &ModelPayload,
    schema: &SchemaInfo,
    limits: &ReaderLimits,
) -> Result<ParameterizedModel, PackageError> {
    require_hex64("model.signature", &payload.signature)?;
    require_hex64("model.model_signature", &payload.model_signature)?;
    if payload.symmetry_contract_version != schema.symmetry_contract_version
        || payload.space_group_data_version != schema.space_group_data_version
        || payload.space_group_data_sha256 != schema.space_group_data_sha256
    {
        return Err(PackageError::InvalidValue {
            what: "model provenance",
            detail: "the payload symmetry/space-group provenance does not match schema_info"
                .to_owned(),
        });
    }

    let particle = map_particles(&payload.particle)?;
    let particle_count = payload.particle.count;

    let energy = map_energy(
        &payload.energy,
        particle_count,
        payload.structure.position_count,
    )?;
    let pair_table_count = u32_len(energy.pair_tables.len())?;
    let cluster_table_count = u32_len(energy.cluster_tables.len())?;

    let structure = map_structure(
        &payload.structure,
        particle_count,
        pair_table_count,
        cluster_table_count,
        limits,
    )?;
    let position_count = payload.structure.position_count;
    let static_trackers_per_cell = payload.structure.static_trackers_per_cell;
    let global_tracker_count = payload.structure.global_tracker_count;

    let transitions = map_transitions(
        &payload.transitions,
        particle_count,
        position_count,
        static_trackers_per_cell,
        global_tracker_count,
        limits,
    )?;

    let simulations = map_simulations(
        &payload.simulations,
        global_tracker_count,
        static_trackers_per_cell,
        &transitions.collections,
    )?;

    let model = ParameterizedModel {
        format_version: payload.format_version,
        signature: payload.signature.clone(),
        model_signature: payload.model_signature.clone(),
        symmetry_contract_version: payload.symmetry_contract_version.clone(),
        space_group_data_version: payload.space_group_data_version.clone(),
        space_group_data_sha256: payload.space_group_data_sha256.clone(),
        particle,
        structure,
        energy,
        transitions,
        simulations,
        counts: ModelCounts {
            particle_count,
            position_count,
            site_count: payload.structure.site_count,
            environment_count: position_count,
            pair_table_count,
            cluster_table_count,
            collection_count: u32_len(payload.transitions.jump_collections.len())?,
            direction_count: u32_len(payload.transitions.jump_directions.len())?,
            rule_count: u32_len(payload.transitions.jump_rules.len())?,
            simulation_count: u32_len(payload.simulations.len())?,
            global_tracker_count,
            static_trackers_per_cell,
        },
    };

    validate_scientific(&model)?;
    Ok(model)
}

fn map_particles(payload: &ParticlePayload) -> Result<ParticleTable, PackageError> {
    let count = payload.count;
    if count == 0 || count > 64 {
        return Err(PackageError::InvalidValue {
            what: "particle.count",
            detail: format!("the particle count {count} is outside 1..=64"),
        });
    }

    let expected = usize::try_from(count).expect("the count is at most 64");
    if payload.charges.len() != expected {
        return Err(PackageError::LengthMismatch {
            what: "particle.charges",
            expected: u64::from(count),
            found: payload.charges.len() as u64,
        });
    }
    if payload.symbols.len() != expected {
        return Err(PackageError::LengthMismatch {
            what: "particle.symbols",
            expected: u64::from(count),
            found: payload.symbols.len() as u64,
        });
    }
    if payload.is_vacancy.len() != expected {
        return Err(PackageError::LengthMismatch {
            what: "particle.is_vacancy",
            expected: u64::from(count),
            found: payload.is_vacancy.len() as u64,
        });
    }

    for charge in &payload.charges {
        require_finite("particle.charges", *charge)?;
    }
    if payload.charges[0] != 0.0 {
        return Err(PackageError::InvalidValue {
            what: "particle.charges",
            detail: "index 0 is void and must carry zero charge".to_owned(),
        });
    }
    for symbol in &payload.symbols {
        if symbol.is_empty() {
            return Err(PackageError::InvalidValue {
                what: "particle.symbols",
                detail: "a particle symbol is empty".to_owned(),
            });
        }
    }

    Ok(ParticleTable {
        charges: payload.charges.clone(),
        symbols: payload.symbols.clone(),
        is_vacancy: payload.is_vacancy.clone(),
    })
}

fn map_energy(
    payload: &EnergyPayload,
    particle_count: u32,
    position_count: u32,
) -> Result<EnergyModel, PackageError> {
    let particle_count_usize = usize::try_from(particle_count).expect("at most 64");
    let pair_length = particle_count_usize * particle_count_usize;

    let mut pair_tables = Vec::with_capacity(payload.pair_tables.len());
    for table in &payload.pair_tables {
        require_hex64("energy.pair_tables.object_id", &table.object_id)?;
        if table.energies.len() != pair_length {
            return Err(PackageError::LengthMismatch {
                what: "energy.pair_tables.energies",
                expected: pair_length as u64,
                found: table.energies.len() as u64,
            });
        }
        for value in &table.energies {
            require_finite("energy.pair_tables.energies", *value)?;
        }
        pair_tables.push(PairTable {
            object_id: table.object_id.clone(),
            energies: table.energies.clone(),
        });
    }

    let mut cluster_tables = Vec::with_capacity(payload.cluster_tables.len());
    for table in &payload.cluster_tables {
        require_hex64("energy.cluster_tables.object_id", &table.object_id)?;
        require_strictly_ascending(
            "energy.cluster_tables.occupation_codes",
            &table.occupation_codes,
        )?;
        if table.energies.len() != table.occupation_codes.len() {
            return Err(PackageError::LengthMismatch {
                what: "energy.cluster_tables.energies",
                expected: table.occupation_codes.len() as u64,
                found: table.energies.len() as u64,
            });
        }
        for code in &table.occupation_codes {
            if code >> 60 != 0 {
                return Err(PackageError::InvalidValue {
                    what: "energy.cluster_tables.occupation_codes",
                    detail: "bits above the ten six-bit slots must be zero".to_owned(),
                });
            }
            for slot in 0..10_u32 {
                let particle = ((code >> (6 * slot)) & 0x3F) as u32;
                if particle >= particle_count {
                    return Err(PackageError::InvalidIndex {
                        what: "energy.cluster_tables.occupation_codes",
                        value: u64::from(particle),
                        limit: u64::from(particle_count),
                    });
                }
            }
        }
        for value in &table.energies {
            require_finite("energy.cluster_tables.energies", *value)?;
        }
        require_strictly_ascending(
            "energy.cluster_tables.particle_table_mapping",
            &table.particle_table_mapping,
        )?;
        for particle in &table.particle_table_mapping {
            if u32::from(*particle) >= particle_count {
                return Err(PackageError::InvalidIndex {
                    what: "energy.cluster_tables.particle_table_mapping",
                    value: u64::from(*particle),
                    limit: u64::from(particle_count),
                });
            }
        }
        cluster_tables.push(ClusterTable {
            object_id: table.object_id.clone(),
            occupation_codes: table.occupation_codes.clone(),
            energies: table.energies.clone(),
            particle_table_mapping: table.particle_table_mapping.clone(),
        });
    }

    let expected_background = u64::from(position_count)
        .checked_mul(u64::from(particle_count))
        .ok_or_else(|| PackageError::InvalidValue {
            what: "energy.defect_background",
            detail: "position_count × particle_count overflows".to_owned(),
        })?;
    if payload.defect_background.len() as u64 != expected_background {
        return Err(PackageError::LengthMismatch {
            what: "energy.defect_background",
            expected: expected_background,
            found: payload.defect_background.len() as u64,
        });
    }
    for value in &payload.defect_background {
        require_finite("energy.defect_background", *value)?;
    }

    Ok(EnergyModel {
        pair_tables,
        cluster_tables,
        defect_background: payload.defect_background.clone(),
    })
}

fn map_structure(
    payload: &StructurePayload,
    particle_count: u32,
    pair_table_count: u32,
    cluster_table_count: u32,
    limits: &ReaderLimits,
) -> Result<StructureModel, PackageError> {
    let position_count = payload.position_count;
    if position_count == 0 {
        return Err(PackageError::InvalidValue {
            what: "structure.position_count",
            detail: "a model must have at least one position".to_owned(),
        });
    }
    if payload.positions.len() != position_count as usize {
        return Err(PackageError::LengthMismatch {
            what: "structure.positions",
            expected: u64::from(position_count),
            found: payload.positions.len() as u64,
        });
    }
    if payload.environments.len() != position_count as usize {
        return Err(PackageError::LengthMismatch {
            what: "structure.environments",
            expected: u64::from(position_count),
            found: payload.environments.len() as u64,
        });
    }
    if payload.site_count > position_count {
        return Err(PackageError::InvalidValue {
            what: "structure.site_count",
            detail: "site_count exceeds position_count".to_owned(),
        });
    }
    if payload.interaction_range[0..3]
        .iter()
        .any(|value| *value < 0)
    {
        return Err(PackageError::InvalidValue {
            what: "structure.interaction_range",
            detail: "the three cell ranges must be non-negative".to_owned(),
        });
    }

    let expected_static = u64::from(position_count)
        .checked_mul(u64::from(particle_count - 1))
        .ok_or_else(|| PackageError::InvalidValue {
            what: "structure.static_trackers_per_cell",
            detail: "the product overflows".to_owned(),
        })?;
    if expected_static != u64::from(payload.static_trackers_per_cell) {
        return Err(PackageError::InvalidValue {
            what: "structure.static_trackers_per_cell",
            detail: format!("expected position_count × (particle_count - 1) = {expected_static}"),
        });
    }
    if payload.global_tracker_count > limits.max_positions {
        return Err(PackageError::InvalidValue {
            what: "structure.global_tracker_count",
            detail: "the count exceeds the reader limit".to_owned(),
        });
    }

    let mut positions = Vec::with_capacity(payload.positions.len());
    for (index, position) in payload.positions.iter().enumerate() {
        let expected_vector = [0, 0, 0, index as i32];
        if position.vector4 != expected_vector {
            return Err(PackageError::InvalidValue {
                what: "structure.positions.vector4",
                detail: format!("position {index} must be [0, 0, 0, {index}]"),
            });
        }
        if position.site_index >= payload.site_count {
            return Err(PackageError::InvalidIndex {
                what: "structure.positions.site_index",
                value: u64::from(position.site_index),
                limit: u64::from(payload.site_count),
            });
        }

        let mut mask = 0_u64;
        let mut previous: Option<u8> = None;
        for particle in &position.particle_ids {
            if let Some(previous) = previous
                && *particle <= previous
            {
                return Err(PackageError::InvalidValue {
                    what: "structure.positions.particle_ids",
                    detail: "the particle ids must be strictly ascending".to_owned(),
                });
            }
            previous = Some(*particle);
            if u32::from(*particle) >= particle_count {
                return Err(PackageError::InvalidIndex {
                    what: "structure.positions.particle_ids",
                    value: u64::from(*particle),
                    limit: u64::from(particle_count),
                });
            }
            mask |= 1_u64 << *particle;
        }

        positions.push(Position {
            vector4: position.vector4,
            site_index: position.site_index,
            stable: position.stable,
            allowed_mask: mask,
        });
    }

    let mut environments = Vec::with_capacity(payload.environments.len());
    for environment in &payload.environments {
        require_mask_bits(
            "structure.environments.selection_particle_mask",
            environment.selection_particle_mask,
            particle_count,
        )?;

        let mut pairs = Vec::with_capacity(environment.pairs.len());
        for pair in &environment.pairs {
            if pair.target_position >= position_count {
                return Err(PackageError::InvalidIndex {
                    what: "structure.environments.pairs.target_position",
                    value: u64::from(pair.target_position),
                    limit: u64::from(position_count),
                });
            }
            for axis in 0..3 {
                let bound = i64::from(payload.interaction_range[axis]).abs() + 1;
                if i64::from(pair.offset[axis]).abs() > bound {
                    return Err(PackageError::InvalidValue {
                        what: "structure.environments.pairs.offset",
                        detail: "the offset exceeds the interaction range".to_owned(),
                    });
                }
            }
            if pair.energy_id >= pair_table_count {
                return Err(PackageError::InvalidIndex {
                    what: "structure.environments.pairs.energy_id",
                    value: u64::from(pair.energy_id),
                    limit: u64::from(pair_table_count),
                });
            }
            pairs.push(PairLink {
                target_position: PositionId::new(pair.target_position),
                offset: pair.offset,
                pair_id: pair.energy_id,
            });
        }

        let mut clusters = Vec::with_capacity(environment.clusters.len());
        for cluster in &environment.clusters {
            if cluster.cluster_id >= cluster_table_count {
                return Err(PackageError::InvalidIndex {
                    what: "structure.environments.clusters.cluster_id",
                    value: u64::from(cluster.cluster_id),
                    limit: u64::from(cluster_table_count),
                });
            }
            if cluster.byte_position >= 10 {
                return Err(PackageError::InvalidValue {
                    what: "structure.environments.clusters.byte_position",
                    detail: "the byte position must be below 10".to_owned(),
                });
            }
            clusters.push(ClusterLink {
                cluster_id: cluster.cluster_id,
                byte_position: cluster.byte_position,
            });
        }

        require_ascending_below(
            "structure.environments.update_particle_ids",
            &environment.update_particle_ids,
            particle_count,
        )?;

        environments.push(EnvironmentModel {
            selection_particle_mask: environment.selection_particle_mask,
            pairs,
            clusters,
            update_particle_ids: environment.update_particle_ids.clone(),
        });
    }

    Ok(StructureModel {
        cell_vectors_ang: payload.cell_vectors_ang,
        positions,
        site_count: payload.site_count,
        environments,
        interaction_range: payload.interaction_range,
        static_trackers_per_cell: payload.static_trackers_per_cell,
        global_tracker_count: payload.global_tracker_count,
    })
}
