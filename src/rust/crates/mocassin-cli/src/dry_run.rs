// SPDX-License-Identifier: MIT

//! The stable, write-free dry-run summary.

use std::fmt::Write as _;
use std::path::Path;

use mocassin_msl::Package;
use mocassin_rng::{RNG_ALGORITHM, seed_hash};
use mocassin_types::JobPackage;

/// Builds the dry-run summary for one validated job.
///
/// The format is frozen by `docs/reference/cli.md`; build metadata (`app_version`, `created_utc`, `generator`)
/// is deliberately excluded. The `settings` line prints the resolved job settings, not the referenced
/// simulation's defaults.
#[must_use]
pub fn summary(
    path: &Path,
    package: &Package,
    job: &JobPackage,
    effective_seed: &[u8; 32],
) -> String {
    let schema = package.schema();
    let model = &job.model;
    let counts = &model.counts;
    let settings = &job.settings;

    let mut out = String::new();
    let _ = writeln!(out, "mocassin-sim {} dry-run", env!("CARGO_PKG_VERSION"));
    let _ = writeln!(out, "package: {}", path.display());
    let _ = writeln!(
        out,
        "schema: format_version={} project_id={} job_selection={}",
        schema.format_version, schema.project_id, schema.job_selection
    );
    let _ = writeln!(
        out,
        "model: signature={} model_signature={} particles={} positions={} sites={} environments={} pair_tables={} cluster_tables={}",
        model.signature,
        model.model_signature,
        counts.particle_count,
        counts.position_count,
        counts.site_count,
        counts.environment_count,
        counts.pair_table_count,
        counts.cluster_table_count
    );
    let _ = writeln!(
        out,
        "lattice: signature={} size={}x{}x{} length={}",
        job.lattice.signature,
        job.lattice.lattice_size[0],
        job.lattice.lattice_size[1],
        job.lattice.lattice_size[2],
        job.lattice.len()
    );
    let _ = writeln!(
        out,
        "job: id={} kind={} simulation_index={}",
        job.job_id,
        job.kind.as_str(),
        settings.simulation_index
    );
    let _ = writeln!(
        out,
        "settings: temperature_k={} target_mcsp={} write_out_count={} pre_run_mcsp={} time_limit_s={} min_success_rate={} electric_field_vm={} flags={}",
        settings.temperature_k,
        settings.target_mcsp,
        settings.write_out_count,
        settings.pre_run_mcsp,
        option_u64(settings.time_limit_s),
        option_f64(settings.min_success_rate),
        settings.electric_field_vm,
        settings.flags
    );
    let _ = writeln!(
        out,
        "transitions: collections={} directions={} rules={}",
        counts.collection_count, counts.direction_count, counts.rule_count
    );
    let _ = writeln!(
        out,
        "trackers: global={} static_per_cell={}",
        counts.global_tracker_count, counts.static_trackers_per_cell
    );
    let _ = writeln!(
        out,
        "rng: algorithm={} seed_hash={}",
        RNG_ALGORITHM,
        seed_hash(effective_seed)
    );
    let _ = writeln!(out, "validation: ok");
    out
}

fn option_u64(value: Option<u64>) -> String {
    value.map_or_else(|| "null".to_owned(), |value| value.to_string())
}

fn option_f64(value: Option<f64>) -> String {
    value.map_or_else(|| "null".to_owned(), |value| value.to_string())
}
