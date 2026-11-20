// SPDX-License-Identifier: MIT

//! The compiled simulation encodings of the model payload.

use serde::{Deserialize, Serialize};

/// One compiled simulation encoding.
#[derive(Debug, Clone, PartialEq, Deserialize, Serialize)]
#[serde(rename_all = "snake_case")]
pub struct SimulationPayload {
    /// The stable project simulation identifier.
    pub id: String,
    /// The simulation kind (`"kmc"` or `"mmc"`).
    pub kind: String,
    /// The active compiled collection indices.
    pub jump_collection_ids: Vec<u32>,
    /// The compiled runtime-default settings.
    pub default_settings: SimulationSettingsPayload,
    /// The tracker index lists used by this simulation.
    pub tracker_models: TrackerModelPayload,
}

/// The compiled runtime-default settings of one simulation.
#[derive(Debug, Clone, PartialEq, Deserialize, Serialize)]
#[serde(rename_all = "snake_case")]
pub struct SimulationSettingsPayload {
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
    /// The authored flag bits (0–3).
    pub flags: u32,
}

/// The global and static tracker index lists of one compiled simulation.
#[derive(Debug, Clone, PartialEq, Deserialize, Serialize)]
#[serde(rename_all = "snake_case")]
pub struct TrackerModelPayload {
    /// The global tracker indices referenced by the simulation.
    pub global_tracker_indices: Vec<u32>,
    /// The static tracker indices referenced by the simulation.
    pub static_tracker_indices: Vec<u32>,
}
