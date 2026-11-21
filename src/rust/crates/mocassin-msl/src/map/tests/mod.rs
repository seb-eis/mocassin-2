// SPDX-License-Identifier: MIT

//! Mapping and invariant tests over the real model DTO and synthetic mutations.

use std::fs;
use std::sync::Arc;

use mocassin_types::payload::{
    ClusterLinkPayload, ClusterTablePayload, JobPayload, LatticePayload, ModelPayload,
    PairLinkPayload, PairTablePayload,
};
use mocassin_types::{ErrorClass, InitialLattice, ParameterizedModel, ReaderLimits};

use crate::decode::{decode_job_payload, decode_lattice_payload, decode_model_payload};
use crate::error::PackageError;
use crate::package::{JobRow, SchemaInfo};

use super::{job_from_payload, lattice_from_payload, model_from_payload};

fn fixture_path(relative: &str) -> std::path::PathBuf {
    std::path::Path::new(env!("CARGO_MANIFEST_DIR"))
        .join("../../../../tests/contracts/packages")
        .join(relative)
}

fn limits() -> ReaderLimits {
    ReaderLimits::default()
}

fn schema() -> SchemaInfo {
    SchemaInfo {
        format_version: 1,
        app_version: "test".to_owned(),
        created_utc: "2026-11-19T00:00:00Z".to_owned(),
        project_id: "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9c00".to_owned(),
        generator: "test".to_owned(),
        job_selection: "all".to_owned(),
        symmetry_contract_version: "symmetry-1".to_owned(),
        space_group_data_version: "spglib-2.8.0/catalog-1".to_owned(),
        space_group_data_sha256: "1f3bf67d92b9258816ee27af2ef12f0035387fa8bad6527aff86f57a740e3395"
            .to_owned(),
    }
}

fn model_dto() -> ModelPayload {
    fixture_model("msl-v1/payloads/model-1.bin")
}

fn fixture_model(relative: &str) -> ModelPayload {
    decode_model_payload(
        &fs::read(fixture_path(relative)).expect("read the model payload"),
        &limits(),
    )
    .expect("decode the model payload")
}

fn map_ok(payload: &ModelPayload) -> ParameterizedModel {
    model_from_payload(payload, &schema(), &limits()).expect("the model maps")
}

fn rejected(mutate: impl FnOnce(&mut ModelPayload)) -> PackageError {
    let mut payload = model_dto();
    mutate(&mut payload);
    let error = model_from_payload(&payload, &schema(), &limits())
        .expect_err("the mutated model must be rejected");
    assert_eq!(error.kind(), ErrorClass::Model, "{error}");
    assert_eq!(error.exit_code(), mocassin_types::ExitCode::ModelError);
    error
}

fn add_pair_table(payload: &mut ModelPayload) {
    payload.energy.pair_tables.push(PairTablePayload {
        object_id: "a".repeat(64),
        energies: vec![0.0; 25],
    });
}

fn add_cluster_table(payload: &mut ModelPayload, codes: Vec<u64>, mapping: Vec<u8>) {
    let energies = vec![0.0; codes.len()];
    payload.energy.cluster_tables.push(ClusterTablePayload {
        object_id: "b".repeat(64),
        occupation_codes: codes,
        code_expansions: None,
        energies,
        particle_table_mapping: mapping,
    });
}

fn make_mmc(payload: &mut ModelPayload) {
    payload.transitions.jump_collections[0].kind = "mmc".to_owned();
    payload.simulations[0].kind = "mmc".to_owned();
    payload.simulations[0].default_settings.pre_run_mcsp = 0;
    payload.simulations[0].default_settings.break_tolerance = Some(0.0);
    payload.simulations[0]
        .default_settings
        .break_sample_length_mcsp = Some(0);
    payload.simulations[0]
        .default_settings
        .break_sample_interval_mcsp = Some(0);
    payload.simulations[0].default_settings.result_sample_mcsp = Some(0);
    payload.simulations[0]
        .tracker_models
        .global_tracker_indices
        .clear();
    payload.simulations[0]
        .tracker_models
        .static_tracker_indices
        .clear();
    for rule in &mut payload.transitions.jump_rules {
        rule.state1 = 0;
    }
}

mod cluster_expansions;
mod job;
mod model;
