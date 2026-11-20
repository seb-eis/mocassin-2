// SPDX-License-Identifier: MIT

//! Job and lattice mapping tests.

use super::*;

fn job_payload() -> JobPayload {
    decode_job_payload(
        &fs::read(fixture_path("msl-v1/payloads/job-1.bin")).expect("read the job payload"),
        &limits(),
    )
    .expect("decode the job payload")
}

fn lattice_payload() -> LatticePayload {
    decode_lattice_payload(
        &fs::read(fixture_path("msl-v1/payloads/lattice-1.bin")).expect("read the lattice"),
        &limits(),
    )
    .expect("decode the lattice")
}

fn fixture_model() -> Arc<ParameterizedModel> {
    Arc::new(map_ok(&model_dto()))
}

fn fixture_lattice(model: &ParameterizedModel) -> InitialLattice {
    lattice_from_payload(
        &lattice_payload(),
        model,
        "880b6eac79e9e920b6ea448c15f21b06be772f4bad774f12ea45c07420d25845".to_owned(),
    )
    .expect("the lattice maps")
}

fn base_job() -> (JobPayload, JobRow, Arc<ParameterizedModel>, InitialLattice) {
    let payload = job_payload();
    let model = fixture_model();
    let lattice = fixture_lattice(&model);
    let row = JobRow {
        job_id: 1,
        model_id: 1,
        lattice_id: 1,
        model_signature: model.model_signature.clone(),
        lattice_signature: lattice.signature.clone(),
        kind: payload.kind.clone(),
        collection_name: "T773".to_owned(),
        config_name: "x001".to_owned(),
        collection_index: 0,
        config_index: 0,
        job_index: 0,
        temperature_k: payload.temperature_k,
        electric_field_vm: payload.electric_field_vm,
        target_mcsp: payload.target_mcsp,
        write_out_count: payload.write_out_count,
        pre_run_mcsp: payload.pre_run_mcsp,
        time_limit_s: payload.time_limit_s,
        min_success_rate: payload.min_success_rate,
        lattice_size: lattice.lattice_size,
        doping_info: payload.doping_info.clone(),
        flags: payload.flags,
        seed: [0x11; 32],
        created_utc: "2026-11-15T12:00:00Z".to_owned(),
    };
    (payload, row, model, lattice)
}

fn rejected_job(
    mutate_payload: impl FnOnce(&mut JobPayload),
    mutate_row: impl FnOnce(&mut JobRow),
) -> PackageError {
    let (mut payload, mut row, model, lattice) = base_job();
    mutate_payload(&mut payload);
    mutate_row(&mut row);
    let error = job_from_payload(&payload, &row, model, lattice)
        .expect_err("the mutated job must be rejected");
    assert_eq!(error.kind(), ErrorClass::Model, "{error}");
    error
}

#[test]
fn the_fixture_job_maps() {
    let (payload, row, model, lattice) = base_job();
    let job = job_from_payload(&payload, &row, model, lattice).expect("the job maps");
    assert_eq!(job.job_id, 1);
    assert_eq!(job.settings.target_mcsp, 150);
    assert_eq!(job.optimizers.len(), 1);
    assert_eq!(job.seed, [0x11; 32]);
}

#[test]
fn job_row_and_payload_must_agree() {
    let error = rejected_job(
        |_| {},
        |row| {
            row.target_mcsp = 151;
        },
    );
    assert!(matches!(error, PackageError::ColumnMismatch { .. }));

    let error = rejected_job(
        |_| {},
        |row| {
            row.kind = "mmc".to_owned();
        },
    );
    assert!(matches!(error, PackageError::ColumnMismatch { .. }));

    let error = rejected_job(
        |_| {},
        |row| {
            row.lattice_size = [8, 8, 8];
        },
    );
    assert!(matches!(error, PackageError::ColumnMismatch { .. }));

    let error = rejected_job(
        |_| {},
        |row| {
            row.doping_info.push('x');
        },
    );
    assert!(matches!(error, PackageError::ColumnMismatch { .. }));

    let error = rejected_job(
        |_| {},
        |row| {
            row.flags = 0x73;
        },
    );
    assert!(matches!(error, PackageError::ColumnMismatch { .. }));
}

#[test]
fn job_flags_and_settings_are_checked() {
    let error = rejected_job(|payload| payload.flags |= 1 << 8, |_| {});
    assert!(error.to_string().contains("reserved"), "{error}");

    let error = rejected_job(|payload| payload.flags |= 1 << 7, |_| {});
    assert!(error.to_string().contains("kind"), "{error}");

    let error = rejected_job(|payload| payload.simulation_index = 9, |_| {});
    assert!(error.to_string().contains("simulation_index"), "{error}");

    let error = rejected_job(
        |payload| payload.temperature_k = 0.0,
        |row| row.temperature_k = 0.0,
    );
    assert!(error.to_string().contains("temperature"), "{error}");

    let error = rejected_job(
        |payload| payload.time_limit_s = Some(0),
        |row| row.time_limit_s = Some(0),
    );
    assert!(error.to_string().contains("time_limit_s"), "{error}");
}

#[test]
fn a_job_kind_that_does_not_match_its_simulation_is_rejected() {
    let error = rejected_job(
        |payload| payload.kind = "mmc".to_owned(),
        |row| row.kind = "mmc".to_owned(),
    );
    assert!(
        error.to_string().contains("referenced simulation"),
        "{error}"
    );
}

#[test]
fn optimizers_and_extensions_must_be_sorted_and_in_range() {
    let error = rejected_job(
        |payload| {
            payload.selection_optimizers = vec![
                mocassin_types::payload::SelectionOptimizerPayload {
                    position_id: 1,
                    particle_id: 1,
                },
                mocassin_types::payload::SelectionOptimizerPayload {
                    position_id: 0,
                    particle_id: 3,
                },
            ];
        },
        |_| {},
    );
    assert!(error.to_string().contains("optimizers"), "{error}");

    let error = rejected_job(
        |payload| {
            payload.selection_optimizers =
                vec![mocassin_types::payload::SelectionOptimizerPayload {
                    position_id: 99,
                    particle_id: 1,
                }];
        },
        |_| {},
    );
    assert!(error.to_string().contains("position_id"), "{error}");

    let error = rejected_job(
        |payload| {
            payload.extensions = vec![
                mocassin_types::payload::ExtensionPayload {
                    id: "b".to_owned(),
                    config_json: "{}".to_owned(),
                },
                mocassin_types::payload::ExtensionPayload {
                    id: "a".to_owned(),
                    config_json: "{}".to_owned(),
                },
            ];
        },
        |_| {},
    );
    assert!(error.to_string().contains("extensions"), "{error}");
}

#[test]
fn a_lattice_that_disagrees_with_the_model_is_rejected() {
    let model = fixture_model();
    let mut payload = lattice_payload();
    payload.position_count = 3;
    let error = lattice_from_payload(&payload, &model, String::new()).expect_err("position count");
    assert!(error.to_string().contains("position_count"), "{error}");

    let mut payload = lattice_payload();
    payload.lattice_size = [0, 6, 6];
    let error = lattice_from_payload(&payload, &model, String::new()).expect_err("zero size");
    assert!(error.to_string().contains("positive"), "{error}");

    let mut payload = lattice_payload();
    payload.lattice.pop();
    let error = lattice_from_payload(&payload, &model, String::new()).expect_err("length");
    assert!(error.to_string().contains("lattice"), "{error}");

    let mut payload = lattice_payload();
    payload.lattice[0] = 9;
    let error = lattice_from_payload(&payload, &model, String::new()).expect_err("particle");
    assert!(error.to_string().contains("lattice"), "{error}");
}
