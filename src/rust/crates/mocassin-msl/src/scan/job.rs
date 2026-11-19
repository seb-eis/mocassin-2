// SPDX-License-Identifier: MIT

//! The job-payload schema walk.

use mocassin_types::ReaderLimits;

use super::facts::JobFacts;
use super::{Cursor, check_cap, kind_name, scan_map};
use crate::error::PackageError;

const JOB_KEYS: &[&str] = &[
    "format_version",
    "simulation_index",
    "kind",
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
    "selection_optimizers",
    "extensions",
    "instruction",
    "doping_info",
];

const OPTIMIZER_KEYS: &[&str] = &["position_id", "particle_id"];

const EXTENSION_KEYS: &[&str] = &["id", "config_json"];

/// Scans a decompressed job payload against the job element budget and per-field caps.
pub(crate) fn scan_job(payload: &[u8], limits: &ReaderLimits) -> Result<JobFacts, PackageError> {
    let mut cursor = Cursor::new(
        payload,
        limits.max_job_elements,
        limits.max_messagepack_depth,
        "job",
    );
    let mut facts = JobFacts::default();

    scan_map(&mut cursor, JOB_KEYS, |cursor, key| match key {
        "format_version" => {
            facts.format_version = Some(cursor.read_u64()?);
            Ok(())
        }
        "kind" => {
            facts.kind = kind_name(cursor.read_str()?);
            Ok(())
        }
        "flags" => {
            facts.flags = Some(cursor.read_u64()?);
            Ok(())
        }
        "selection_optimizers" => {
            let length = cursor.read_array_len()?;
            check_cap(
                cursor,
                "selection optimizers",
                length,
                limits.max_job_elements,
            )?;
            for _ in 0..length {
                scan_optimizer(cursor)?;
            }
            cursor.leave();
            facts.optimizer_count = Some(length);
            Ok(())
        }
        "extensions" => {
            let length = cursor.read_array_len()?;
            check_cap(
                cursor,
                "extensions",
                length,
                u64::from(limits.max_extensions),
            )?;
            for _ in 0..length {
                scan_extension(cursor)?;
            }
            cursor.leave();
            facts.extension_count = Some(length);
            Ok(())
        }
        _ => cursor.skip_value(),
    })?;

    cursor.finish()?;
    Ok(facts)
}

fn scan_optimizer(cursor: &mut Cursor<'_>) -> Result<(), PackageError> {
    scan_map(cursor, OPTIMIZER_KEYS, |cursor, key| match key {
        "position_id" => {
            let _ = cursor.read_u32()?;
            Ok(())
        }
        "particle_id" => cursor.skip_value(),
        _ => unreachable!(),
    })
}

fn scan_extension(cursor: &mut Cursor<'_>) -> Result<(), PackageError> {
    scan_map(cursor, EXTENSION_KEYS, |cursor, key| match key {
        "id" | "config_json" => {
            let _ = cursor.read_str()?;
            Ok(())
        }
        _ => unreachable!(),
    })
}
