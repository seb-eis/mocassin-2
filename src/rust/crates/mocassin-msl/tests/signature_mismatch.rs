// SPDX-License-Identifier: MIT

//! Stored-signature verification versus decode failure.

mod support;

use std::fs;

use mocassin_msl::{Package, PackageError};
use mocassin_types::{ErrorClass, ExitCode, ReaderLimits};
use rusqlite::Connection;

use support::{TempDir, copy_fixture, raw_payload};

fn open(path: &std::path::Path) -> Package {
    Package::open_readonly(path, ReaderLimits::default()).expect("the fixture opens")
}

fn update(label: &str, sql: &str) -> (TempDir, std::path::PathBuf) {
    let temp = TempDir::new(label);
    let path = copy_fixture("msl-v1", "ceria-temperature-sweep.msl", &temp);
    let connection = Connection::open(&path).expect("open the copy");
    connection.execute_batch(sql).expect("apply the update");
    drop(connection);
    (temp, path)
}

#[test]
fn a_wrong_model_row_signature_is_a_model_error() {
    let (_temp, path) = update(
        "model-row-signature",
        "UPDATE models SET signature = 'cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc';",
    );
    let package = open(&path);
    let error = package.load_job(1).expect_err("the signature must fail");
    assert!(matches!(
        error.source,
        PackageError::SignatureMismatch {
            what: "models.signature",
            ..
        }
    ));
    assert_eq!(error.kind(), ErrorClass::Model);
    assert_eq!(error.exit_code(), ExitCode::ModelError);
}

#[test]
fn a_changed_model_payload_signature_field_is_a_model_error() {
    let mut dto = mocassin_msl::decode_model_payload(
        &fs::read(raw_payload("model-1.bin")).expect("read the raw model"),
        &ReaderLimits::default(),
    )
    .expect("decode the model");
    dto.signature = "d".repeat(64);
    let encoded = rmp_serde::to_vec_named(&dto).expect("re-encode the model");
    let frame = zstd::bulk::compress(&encoded, 3).expect("compress the model");

    let temp = TempDir::new("model-payload-signature");
    let path = copy_fixture("msl-v1", "ceria-temperature-sweep.msl", &temp);
    let connection = Connection::open(&path).expect("open the copy");
    connection
        .execute("UPDATE models SET payload = ?1", rusqlite::params![frame])
        .expect("replace the model payload");
    drop(connection);

    let package = open(&path);
    let error = package
        .load_job(1)
        .expect_err("the payload signature must fail");
    assert!(matches!(
        error.source,
        PackageError::SignatureMismatch {
            what: "models payload signature",
            ..
        }
    ));
    assert_eq!(error.kind(), ErrorClass::Model);
}

#[test]
fn a_changed_defect_background_is_detected_by_the_parameterization_signature() {
    let mut dto = mocassin_msl::decode_model_payload(
        &fs::read(raw_payload("model-1.bin")).expect("read the raw model"),
        &ReaderLimits::default(),
    )
    .expect("decode the model");
    dto.energy.defect_background[0] = 1.0;
    let encoded = rmp_serde::to_vec_named(&dto).expect("re-encode the model");
    let frame = zstd::bulk::compress(&encoded, 3).expect("compress the model");

    let temp = TempDir::new("model-value-signature");
    let path = copy_fixture("msl-v1", "ceria-temperature-sweep.msl", &temp);
    let connection = Connection::open(&path).expect("open the copy");
    connection
        .execute("UPDATE models SET payload = ?1", rusqlite::params![frame])
        .expect("replace the model payload");
    drop(connection);

    let package = open(&path);
    let error = package
        .load_job(1)
        .expect_err("the changed value must fail");
    assert!(matches!(
        error.source,
        PackageError::SignatureMismatch { .. }
    ));
    assert_eq!(error.kind(), ErrorClass::Model);
}

#[test]
fn a_wrong_lattice_row_signature_is_a_model_error() {
    let (_temp, path) = update(
        "lattice-signature",
        "UPDATE lattices SET signature = 'eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee' WHERE lattice_id = 1;",
    );
    let package = open(&path);
    let error = package
        .load_job(1)
        .expect_err("the lattice signature must fail");
    assert!(matches!(
        error.source,
        PackageError::SignatureMismatch {
            what: "lattices.signature",
            ..
        }
    ));
    assert_eq!(error.kind(), ErrorClass::Model);
}

#[test]
fn a_signature_mismatch_is_distinct_from_a_decode_failure() {
    // A corrupt frame fails before any signature is computed and is a package error.
    let (_temp, path) = update("corrupt-frame", "UPDATE models SET payload = x'00';");
    let package = open(&path);
    let decode_error = package.load_job(1).expect_err("the frame must fail");
    assert!(matches!(
        decode_error.source,
        PackageError::InvalidFrame { .. }
    ));
    assert_eq!(decode_error.kind(), ErrorClass::Package);
    assert_eq!(decode_error.exit_code(), ExitCode::PackageError);

    // A corrupt signature decodes fine and fails in the signature comparison.
    let (_temp, path) = update(
        "signature-class",
        "UPDATE models SET signature = 'ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff';",
    );
    let package = open(&path);
    let signature_error = package.load_job(1).expect_err("the signature must fail");
    assert!(matches!(
        signature_error.source,
        PackageError::SignatureMismatch { .. }
    ));
    assert_eq!(signature_error.kind(), ErrorClass::Model);
    assert_eq!(signature_error.exit_code(), ExitCode::ModelError);
}
