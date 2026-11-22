// SPDX-License-Identifier: MIT

//! Shared helpers for the Step 14 context acceptance tests.
//!
//! Every helper uses a real `.msl` fixture written by the .NET writer; mutations clone the validated runtime
//! model and never touch the package or the payload.

#![allow(dead_code)]

use std::path::Path;
use std::sync::Arc;

use mocassin_msl::Package;
use mocassin_solver::context::{ContextOptions, InitOutcome, SimulationContext};
use mocassin_types::{JobPackage, ParameterizedModel, ReaderLimits};

/// Opens a checked-in package fixture and loads one job.
pub fn fixture_job(package: &str, file: &str, job_id: u32) -> JobPackage {
    let path = Path::new(env!("CARGO_MANIFEST_DIR"))
        .join("../../../../tests/contracts/packages")
        .join(package)
        .join(file);
    Package::open_readonly(path, ReaderLimits::default())
        .expect("the fixture opens read-only")
        .load_job(job_id)
        .expect("the fixture job maps")
}

/// The populated KMC job.
pub fn populated_kmc() -> JobPackage {
    fixture_job("step-13-populated", "step-13-populated.msl", 1)
}

/// The populated MMC job.
pub fn populated_mmc() -> JobPackage {
    fixture_job("step-13-populated", "step-13-populated.msl", 2)
}

/// The C-centered fixture (8 P1 positions, 4 pair tables, no cluster).
pub fn centered() -> JobPackage {
    fixture_job("step-13-centered", "step-13-centered.msl", 1)
}

/// The amendment-2 two-member cluster fixture (6 canonical codes, 8 raw codes).
pub fn amend2() -> JobPackage {
    fixture_job("step-11-amend-2", "step-11-amend-2.msl", 1)
}

/// The pre-map two-member cluster fixture (no `code_expansions`).
pub fn pre_map() -> JobPackage {
    fixture_job("amend-2-pre-map", "amend-2-pre-map.msl", 1)
}

/// The table-free ceria fixture.
pub fn ceria() -> JobPackage {
    fixture_job("msl-v1", "ceria-temperature-sweep.msl", 1)
}

/// The pre-amendment `net10.0` compatibility copy of the ceria fixture (logically identical to [`ceria`]).
pub fn amend1_net10() -> JobPackage {
    fixture_job("amend-1-net10", "ceria-temperature-sweep.msl", 1)
}

/// Clones the validated model, applies a mutation, and returns the job.
pub fn with_model(mut job: JobPackage, mutate: impl FnOnce(&mut ParameterizedModel)) -> JobPackage {
    let mut model = (*job.model).clone();
    mutate(&mut model);
    job.model = Arc::new(model);
    job
}

/// Initializes a context with the default options.
pub fn initialize(job: JobPackage) -> SimulationContext {
    match SimulationContext::initialize(job, &ContextOptions::default(), None)
        .expect("the job initializes")
    {
        InitOutcome::Ready(context) => *context,
        InitOutcome::AlreadyCompleted { .. } => panic!("a fresh job is not already completed"),
    }
}

/// Initializes a context with explicit options, panicking on an error.
pub fn initialize_with(job: JobPackage, options: &ContextOptions) -> SimulationContext {
    match SimulationContext::initialize(job, options, None).expect("the job initializes") {
        InitOutcome::Ready(context) => *context,
        InitOutcome::AlreadyCompleted { .. } => panic!("a fresh job is not already completed"),
    }
}
