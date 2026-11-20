// SPDX-License-Identifier: MIT

//! The job, lattice, and simulation mapping and the stored-column cross-checks.

use std::sync::Arc;

use mocassin_types::payload::{
    JobPayload, LatticePayload, SimulationPayload, SimulationSettingsPayload,
};
use mocassin_types::{
    ExtensionConfig, InitialLattice, JobPackage, JobSettings, JumpCollection, ParameterizedModel,
    ParticleId, PositionId, SelectionOptimizer, SimulationKind, SimulationModel,
    SimulationSettings, SolverJobFlags, TrackerModels,
};

use crate::error::PackageError;
use crate::package::JobRow;
use crate::validate::{SettingsCheck, require_strictly_ascending, require_unique};

use super::{
    parse_kind, require_column_f64, require_column_option_f64, require_column_option_u64,
    require_column_u32, require_column_u64,
};

pub(super) fn map_simulations(
    payload: &[SimulationPayload],
    global_tracker_count: u32,
    static_trackers_per_cell: u32,
    collections: &[JumpCollection],
) -> Result<Vec<SimulationModel>, PackageError> {
    let mut simulations = Vec::with_capacity(payload.len());
    for (index, simulation) in payload.iter().enumerate() {
        let kind = parse_kind("simulations.kind", &simulation.kind)?;
        let _ = index;
        SettingsCheck {
            what: "simulations.default_settings",
            kind,
            temperature_k: simulation.default_settings.temperature_k,
            target_mcsp: simulation.default_settings.target_mcsp,
            pre_run_mcsp: simulation.default_settings.pre_run_mcsp,
            time_limit_s: simulation.default_settings.time_limit_s,
            min_success_rate: simulation.default_settings.min_success_rate,
            electric_field_vm: simulation.default_settings.electric_field_vm,
            electric_field_direction: simulation.default_settings.electric_field_direction,
            normalization_energy_ev: simulation.default_settings.normalization_energy_ev,
            max_attempt_frequency_hz: simulation.default_settings.max_attempt_frequency_hz,
            break_tolerance: simulation.default_settings.break_tolerance,
            break_sample_length_mcsp: simulation.default_settings.break_sample_length_mcsp,
            break_sample_interval_mcsp: simulation.default_settings.break_sample_interval_mcsp,
            result_sample_mcsp: simulation.default_settings.result_sample_mcsp,
            authored_flags: Some(simulation.default_settings.flags),
        }
        .validate()?;

        require_unique(
            "simulations.jump_collection_ids",
            &simulation.jump_collection_ids,
        )?;
        for collection in &simulation.jump_collection_ids {
            if *collection >= collections.len() as u32 {
                return Err(PackageError::InvalidIndex {
                    what: "simulations.jump_collection_ids",
                    value: u64::from(*collection),
                    limit: u64::try_from(collections.len()).unwrap_or(u64::MAX),
                });
            }
            if collections[*collection as usize].kind != kind {
                return Err(PackageError::InvalidValue {
                    what: "simulations.jump_collection_ids",
                    detail: "the referenced collection has a different simulation kind".to_owned(),
                });
            }
        }

        let trackers = &simulation.tracker_models;
        require_strictly_ascending(
            "simulations.tracker_models.global_tracker_indices",
            &trackers.global_tracker_indices,
        )?;
        for tracker in &trackers.global_tracker_indices {
            if *tracker >= global_tracker_count {
                return Err(PackageError::InvalidIndex {
                    what: "simulations.tracker_models.global_tracker_indices",
                    value: u64::from(*tracker),
                    limit: u64::from(global_tracker_count),
                });
            }
        }
        require_strictly_ascending(
            "simulations.tracker_models.static_tracker_indices",
            &trackers.static_tracker_indices,
        )?;
        for tracker in &trackers.static_tracker_indices {
            if *tracker >= static_trackers_per_cell {
                return Err(PackageError::InvalidIndex {
                    what: "simulations.tracker_models.static_tracker_indices",
                    value: u64::from(*tracker),
                    limit: u64::from(static_trackers_per_cell),
                });
            }
        }
        if matches!(kind, SimulationKind::Mmc)
            && (!trackers.global_tracker_indices.is_empty()
                || !trackers.static_tracker_indices.is_empty())
        {
            return Err(PackageError::InvalidValue {
                what: "simulations.tracker_models",
                detail: "MMC simulations must have empty tracker index lists".to_owned(),
            });
        }

        simulations.push(SimulationModel {
            id: simulation.id.clone(),
            kind,
            jump_collection_ids: simulation.jump_collection_ids.clone(),
            default_settings: simulation_settings(&simulation.default_settings),
            tracker_models: TrackerModels {
                global_tracker_indices: trackers.global_tracker_indices.clone(),
                static_tracker_indices: trackers.static_tracker_indices.clone(),
            },
        });
    }
    Ok(simulations)
}

fn simulation_settings(payload: &SimulationSettingsPayload) -> SimulationSettings {
    SimulationSettings {
        temperature_k: payload.temperature_k,
        target_mcsp: payload.target_mcsp,
        write_out_count: payload.write_out_count,
        pre_run_mcsp: payload.pre_run_mcsp,
        time_limit_s: payload.time_limit_s,
        min_success_rate: payload.min_success_rate,
        electric_field_vm: payload.electric_field_vm,
        electric_field_direction: payload.electric_field_direction,
        normalization_energy_ev: payload.normalization_energy_ev,
        max_attempt_frequency_hz: payload.max_attempt_frequency_hz,
        break_tolerance: payload.break_tolerance,
        break_sample_length_mcsp: payload.break_sample_length_mcsp,
        break_sample_interval_mcsp: payload.break_sample_interval_mcsp,
        result_sample_mcsp: payload.result_sample_mcsp,
        flags: payload.flags,
    }
}

/// Maps and validates the initial lattice against the model.
pub(crate) fn lattice_from_payload(
    payload: &LatticePayload,
    model: &ParameterizedModel,
    signature: String,
) -> Result<InitialLattice, PackageError> {
    if payload.position_count != model.counts.position_count {
        return Err(PackageError::LengthMismatch {
            what: "lattice.position_count",
            expected: u64::from(model.counts.position_count),
            found: u64::from(payload.position_count),
        });
    }
    if payload.lattice_size.contains(&0) {
        return Err(PackageError::InvalidValue {
            what: "lattice.lattice_size",
            detail: "every lattice size must be positive".to_owned(),
        });
    }

    let expected = payload
        .expected_particle_count()
        .ok_or_else(|| PackageError::InvalidValue {
            what: "lattice.lattice_size",
            detail: "a × b × c × position_count overflows".to_owned(),
        })?;
    if payload.lattice.len() as u64 != expected {
        return Err(PackageError::LengthMismatch {
            what: "lattice.lattice",
            expected,
            found: payload.lattice.len() as u64,
        });
    }

    let mut particles = Vec::with_capacity(payload.lattice.len());
    for particle in &payload.lattice {
        if u32::from(*particle) >= model.counts.particle_count {
            return Err(PackageError::InvalidIndex {
                what: "lattice.lattice",
                value: u64::from(*particle),
                limit: u64::from(model.counts.particle_count),
            });
        }
        particles.push(ParticleId::new(*particle));
    }

    Ok(InitialLattice {
        signature,
        lattice_size: payload.lattice_size,
        position_count: payload.position_count,
        particles,
    })
}

/// Maps and validates one job payload against its row, model, and lattice.
pub(crate) fn job_from_payload(
    payload: &JobPayload,
    row: &JobRow,
    model: Arc<ParameterizedModel>,
    lattice: InitialLattice,
) -> Result<JobPackage, PackageError> {
    let kind = parse_kind("job.kind", &payload.kind)?;
    if row.kind != payload.kind {
        return Err(PackageError::ColumnMismatch {
            column: "jobs.kind",
            column_value: row.kind.clone(),
            payload_value: payload.kind.clone(),
        });
    }

    let simulation = model
        .simulations
        .get(payload.simulation_index as usize)
        .ok_or_else(|| PackageError::InvalidIndex {
            what: "job.simulation_index",
            value: u64::from(payload.simulation_index),
            limit: u64::try_from(model.simulations.len()).unwrap_or(u64::MAX),
        })?;
    if simulation.kind != kind {
        return Err(PackageError::InvalidValue {
            what: "job.kind",
            detail: "the job kind does not match the referenced simulation".to_owned(),
        });
    }

    SettingsCheck {
        what: "job.settings",
        kind,
        temperature_k: payload.temperature_k,
        target_mcsp: payload.target_mcsp,
        pre_run_mcsp: payload.pre_run_mcsp,
        time_limit_s: payload.time_limit_s,
        min_success_rate: payload.min_success_rate,
        electric_field_vm: payload.electric_field_vm,
        electric_field_direction: payload.electric_field_direction,
        normalization_energy_ev: payload.normalization_energy_ev,
        max_attempt_frequency_hz: payload.max_attempt_frequency_hz,
        break_tolerance: payload.break_tolerance,
        break_sample_length_mcsp: payload.break_sample_length_mcsp,
        break_sample_interval_mcsp: payload.break_sample_interval_mcsp,
        result_sample_mcsp: payload.result_sample_mcsp,
        authored_flags: None,
    }
    .validate()?;

    let flags = SolverJobFlags::from_bits(payload.flags);
    flags
        .validate(kind)
        .map_err(|error| PackageError::InvalidValue {
            what: "job.flags",
            detail: error.to_string(),
        })?;

    require_column_u64("jobs.target_mcsp", row.target_mcsp, payload.target_mcsp)?;
    require_column_u32(
        "jobs.write_out_count",
        row.write_out_count,
        payload.write_out_count,
    )?;
    require_column_u64("jobs.pre_run_mcsp", row.pre_run_mcsp, payload.pre_run_mcsp)?;
    require_column_option_u64("jobs.time_limit_s", row.time_limit_s, payload.time_limit_s)?;
    require_column_f64(
        "jobs.temperature_k",
        row.temperature_k,
        payload.temperature_k,
    )?;
    require_column_f64(
        "jobs.electric_field_vm",
        row.electric_field_vm,
        payload.electric_field_vm,
    )?;
    require_column_option_f64(
        "jobs.min_success_rate",
        row.min_success_rate,
        payload.min_success_rate,
    )?;
    require_column_u32("jobs.flags", row.flags, payload.flags)?;
    if row.doping_info != payload.doping_info {
        return Err(PackageError::ColumnMismatch {
            column: "jobs.doping_info",
            column_value: row.doping_info.clone(),
            payload_value: payload.doping_info.clone(),
        });
    }
    if row.lattice_size != lattice.lattice_size {
        return Err(PackageError::ColumnMismatch {
            column: "jobs.lattice_a/b/c",
            column_value: format!("{:?}", row.lattice_size),
            payload_value: format!("{:?}", lattice.lattice_size),
        });
    }

    let mut optimizers = Vec::with_capacity(payload.selection_optimizers.len());
    for optimizer in &payload.selection_optimizers {
        if optimizer.position_id >= model.counts.position_count {
            return Err(PackageError::InvalidIndex {
                what: "job.selection_optimizers.position_id",
                value: u64::from(optimizer.position_id),
                limit: u64::from(model.counts.position_count),
            });
        }
        if u32::from(optimizer.particle_id) >= model.counts.particle_count {
            return Err(PackageError::InvalidIndex {
                what: "job.selection_optimizers.particle_id",
                value: u64::from(optimizer.particle_id),
                limit: u64::from(model.counts.particle_count),
            });
        }
        optimizers.push(SelectionOptimizer {
            position_id: PositionId::new(optimizer.position_id),
            particle_id: ParticleId::new(optimizer.particle_id),
        });
    }
    for window in optimizers.windows(2) {
        let previous = (window[0].position_id, window[0].particle_id);
        let current = (window[1].position_id, window[1].particle_id);
        if previous > current {
            return Err(PackageError::InvalidValue {
                what: "job.selection_optimizers",
                detail: "the optimizers are not sorted by (position_id, particle_id)".to_owned(),
            });
        }
    }

    for window in payload.extensions.windows(2) {
        if window[0].id >= window[1].id {
            return Err(PackageError::InvalidValue {
                what: "job.extensions",
                detail: "the extensions are not strictly ascending by id".to_owned(),
            });
        }
    }
    let extensions = payload
        .extensions
        .iter()
        .map(|extension| ExtensionConfig {
            id: extension.id.clone(),
            config_json: extension.config_json.clone(),
        })
        .collect();

    Ok(JobPackage {
        job_id: row.job_id,
        kind,
        model,
        lattice,
        settings: JobSettings {
            simulation_index: payload.simulation_index,
            temperature_k: payload.temperature_k,
            target_mcsp: payload.target_mcsp,
            write_out_count: payload.write_out_count,
            pre_run_mcsp: payload.pre_run_mcsp,
            time_limit_s: payload.time_limit_s,
            min_success_rate: payload.min_success_rate,
            electric_field_vm: payload.electric_field_vm,
            electric_field_direction: payload.electric_field_direction,
            normalization_energy_ev: payload.normalization_energy_ev,
            max_attempt_frequency_hz: payload.max_attempt_frequency_hz,
            break_tolerance: payload.break_tolerance,
            break_sample_length_mcsp: payload.break_sample_length_mcsp,
            break_sample_interval_mcsp: payload.break_sample_interval_mcsp,
            result_sample_mcsp: payload.result_sample_mcsp,
            flags,
        },
        seed: row.seed,
        optimizers,
        extensions,
        instruction: payload.instruction.clone(),
        doping_info: payload.doping_info.clone(),
    })
}
