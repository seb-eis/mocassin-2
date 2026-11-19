// SPDX-License-Identifier: MIT

//! The resolved job payload.

use serde::Deserialize;

/// The `.msl` job payload.
///
/// The job payload is authoritative for a job's resolved settings. It is not required to equal the referenced
/// simulation's `default_settings`.
#[derive(Debug, Clone, PartialEq, Deserialize)]
#[serde(rename_all = "snake_case")]
pub struct JobPayload {
    /// The payload-encoding version.
    pub format_version: u32,
    /// The index into the model payload's `simulations` array.
    pub simulation_index: u32,
    /// The job kind (`"kmc"` or `"mmc"`).
    pub kind: String,
    /// The temperature in kelvin.
    pub temperature_k: f64,
    /// The target successful steps per mobile particle.
    pub target_mcsp: u64,
    /// The intermediate data-out count; zero means the final state only.
    pub write_out_count: u32,
    /// The KMC pre-run MCSP; zero disables the pre-run.
    pub pre_run_mcsp: u64,
    /// The wall-clock limit in seconds, or `None`.
    pub time_limit_s: Option<u64>,
    /// The lower abort rate in MCS/s, or `None`.
    pub min_success_rate: Option<f64>,
    /// The electric-field modulus in V/m.
    pub electric_field_vm: f64,
    /// The normalized Cartesian field direction.
    pub electric_field_direction: [f64; 3],
    /// The normalization energy in eV, or `None`.
    pub normalization_energy_ev: Option<f64>,
    /// The job-level maximum attempt frequency in hertz, or `None`.
    pub max_attempt_frequency_hz: Option<f64>,
    /// The MMC break tolerance, or `None`.
    pub break_tolerance: Option<f64>,
    /// The MMC break sample length, or `None`.
    pub break_sample_length_mcsp: Option<u64>,
    /// The MMC break sample interval, or `None`.
    pub break_sample_interval_mcsp: Option<u64>,
    /// The MMC result sample length, or `None`.
    pub result_sample_mcsp: Option<u64>,
    /// The composed `SolverJobFlags` word.
    pub flags: u32,
    /// The KMC start options removed from the selection pool, sorted by position and particle index.
    pub selection_optimizers: Vec<SelectionOptimizerPayload>,
    /// The effective extension entries, sorted by id.
    pub extensions: Vec<ExtensionPayload>,
    /// The optional advanced instruction, or `None`.
    pub instruction: Option<String>,
    /// The canonical priority-ordered doping summary, or an empty string.
    pub doping_info: String,
}

/// One compiled KMC selection optimizer entry.
#[derive(Debug, Clone, PartialEq, Deserialize)]
#[serde(rename_all = "snake_case")]
pub struct SelectionOptimizerPayload {
    /// The canonical P1 position index.
    pub position_id: u32,
    /// The compiled particle index removed from the start pool.
    pub particle_id: u8,
}

/// One opaque solver-extension configuration.
#[derive(Debug, Clone, PartialEq, Deserialize)]
#[serde(rename_all = "snake_case")]
pub struct ExtensionPayload {
    /// The stable extension identifier.
    pub id: String,
    /// The canonical JSON text of the authored configuration.
    pub config_json: String,
}
